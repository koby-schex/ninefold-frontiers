using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ninefold.Core.Flow;
using Ninefold.Core.Progression;
using Ninefold.Core.Combat;

internal static partial class Program
{
    private static (string Name, Action Run)[] CustomizationTests() => new (string, Action)[]
    {
        ("Owned units start with neutral customization", CustomizationDefault),
        ("Customization persists without spending fragments", CustomizationSelect),
        ("Switching and resetting replace rather than stack choices", CustomizationReplace),
        ("Retrying an older choice returns its receipt without reverting current state", CustomizationStaleRetry),
        ("Customization request identity rejects different units or choices", CustomizationConflict),
        ("Unowned unknown and unauthored choices cannot be saved", CustomizationInvalidRequest),
        ("Customization definitions enforce bounded equal-point trade-offs", CustomizationDefinitions),
        ("Definition changes preserve saved choices until explicit reselection", CustomizationChangedContent),
        ("Reset remains available after authored choices are removed", CustomizationResetRemoved),
        ("Interrupted choice writes preserve the previous selection", CustomizationInterrupted),
        ("Lost customization acknowledgement retries without a new transaction", CustomizationAmbiguous),
        ("Corrupt latest customization restores a complete prior selection", CustomizationRecovery),
        ("Concurrent customization retries commit once", CustomizationConcurrent),
        ("Customization states and option inputs remain immutable", CustomizationImmutable),
        ("All other profile transactions retain customization", CustomizationOtherTransactions),
        ("Old schema four advancement survives customization migration", CustomizationSchemaFour),
        ("Combined modifiers enforce additive bounds", CustomizationCombined),
        ("Battle deployment applies positive and negative trade-offs", CustomizationDeployment),
        ("Advancement and customization are added once over base stats", CustomizationBattleCap),
        ("Reset removes only customization from future deployments", CustomizationBattleReset),
        ("Resumed battles retain original choices without stacking", CustomizationResume),
        ("Customization applies across faction and mixed mission entries", CustomizationModes),
        ("Flow blocks customization during battle and unclaimed results", CustomizationFlowGate),
        ("Flow recovers an ambiguous free customization request", CustomizationFlowRetry),
        ("Negative rounding preserves small positive stats", CustomizationRounding),
        ("Customization changes power without altering action costs", CustomizationPower),
        ("Real directory reopen retains selection and free reset", CustomizationDirectory)
    };
    private static UnitCustomizationDefinition Choices(string unit="a", int amount=500, string revision="test-v1")
        => new UnitCustomizationDefinition(unit,revision,new[] {
            new CustomizationOption("health",new CustomizationBonus(amount,-amount,0)),
            new CustomizationOption("armor",new CustomizationBonus(-amount,amount,0)),
            new CustomizationOption("support",new CustomizationBonus(-amount,0,amount)),
            new CustomizationOption("durable",new CustomizationBonus(amount,0,-amount)) });
    private static void CustomizationDefault()
    {
        var s=AdvancementStore(out _); var c=s.Load().Progress.GetCustomization("a"); Equal<string>(null,c.OptionId); Equal(true,c.Bonus.IsNeutral);
        Throws<ArgumentException>(()=>s.Load().Progress.GetCustomization("missing"));
    }
    private static void CustomizationSelect()
    {
        var s=AdvancementStore(out var files); s.Customize("op","a","health",Choices());
        var p=new LocalProgressStore(files,"advance-profile").Load().Progress;
        Equal("health",p.GetCustomization("a").OptionId); Equal(-500,p.GetCustomization("a").Bonus.Armor); Equal(10L,p.Balance("test-resource")); Equal(3L,p.Generation);
    }
    private static void CustomizationReplace()
    {
        var s=AdvancementStore(out _); s.Customize("one","a","health",Choices()); s.Customize("two","a","armor",Choices());
        s.Customize("three","a","armor",Choices()); Equal(-500,s.Load().Progress.GetCustomization("a").Bonus.Health);
        s.Customize("reset","a",null); Equal(true,s.Load().Progress.GetCustomization("a").Bonus.IsNeutral); Equal(10L,s.Load().Progress.Balance("test-resource"));
    }
    private static void CustomizationStaleRetry()
    {
        var s=AdvancementStore(out var files); s.Customize("one","a","health",Choices()); s.Customize("two","a","armor",Choices());
        files.Inner.Fault=1; var retry=s.Customize("one","a","health");
        Equal(true,retry.AlreadyApplied); Equal("health",retry.Receipt.OptionId); Equal("armor",retry.Saved.Progress.GetCustomization("a").OptionId); Equal(4,files.Writes);
    }
    private static void CustomizationConflict()
    {
        var s=AdvancementStore(out var f); s.Customize("one","a","health",Choices());
        Throws<InvalidOperationException>(()=>s.Customize("one","a","armor",Choices()));
        Throws<InvalidOperationException>(()=>s.Customize("one","c","health",Choices("c"))); Equal(3,f.Writes);
    }
    private static void CustomizationInvalidRequest()
    {
        var s=AdvancementStore(out var f);
        Throws<ArgumentException>(()=>s.Customize("one","missing",null));
        Throws<ArgumentException>(()=>s.Customize("one","a","unknown",Choices()));
        Throws<ArgumentException>(()=>s.Customize("one","a","health",Choices("c")));
        Throws<ArgumentException>(()=>s.Customize("one","a","health")); Equal(2,f.Writes);
    }
    private static void CustomizationDefinitions()
    {
        Throws<ArgumentOutOfRangeException>(()=>new CustomizationBonus(501,-501,0));
        Throws<ArgumentException>(()=>new CustomizationBonus(500,-200,0));
        Throws<ArgumentException>(()=>new CustomizationOption("neutral",CustomizationBonus.None));
        var option=new CustomizationOption("one",new CustomizationBonus(200,-200,0));
        Throws<ArgumentException>(()=>new UnitCustomizationDefinition("a","v",new[] { option,option }));
        Throws<ArgumentException>(()=>new RosterUnit("a","f",false,UnlockDef("a"),customization:Choices("c")));
    }
    private static void CustomizationChangedContent()
    {
        var s=AdvancementStore(out _); s.Customize("one","a","health",Choices());
        var old=s.Customize("one","a","health",Choices(amount:200,revision:"v2")); Equal(500,old.Receipt.Bonus.Health);
        s.Customize("two","a","health",Choices(amount:200,revision:"v2")); Equal(200,s.Load().Progress.GetCustomization("a").Bonus.Health);
    }
    private static void CustomizationResetRemoved()
    {
        var x=new AdvancementFixture(); var f=x.Flow(Choices()); f.Open(); f.CustomizeUnit("one","a","health");
        f=x.Flow(); f.Open(); f.CustomizeUnit("reset","a",null); Equal(true,f.Progress.GetCustomization("a").Bonus.IsNeutral);
    }
    private static void CustomizationInterrupted()
    {
        foreach(int fault in new[] { 1,2 })
        {
            var s=AdvancementStore(out var files); s.Customize("one","a","health",Choices()); files.Inner.Fault=fault;
            Throws<IOException>(()=>s.Customize("two","a","armor",Choices())); files.Inner.Fault=0;
            Equal("health",s.Load().Progress.GetCustomization("a").OptionId); Equal(10L,s.Load().Progress.Balance("test-resource"));
            s.Customize("two","a","armor",Choices()); Equal("armor",s.Load().Progress.GetCustomization("a").OptionId);
        }
    }
    private static void CustomizationAmbiguous()
    {
        var s=AdvancementStore(out var files); files.Inner.Fault=3; Throws<IOException>(()=>s.Customize("one","a","health",Choices())); files.Inner.Fault=0;
        Equal(true,s.Customize("one","a","health",Choices()).AlreadyApplied); Equal(3,files.Writes);
    }
    private static void CustomizationRecovery()
    {
        var s=AdvancementStore(out var files); s.Customize("one","a","health",Choices()); s.Customize("two","a","armor",Choices()); files.Inner.Slots[1][20]^=1;
        Equal(true,s.Load().Recovered); Equal("health",s.Load().Progress.GetCustomization("a").OptionId);
        s.Customize("two","a","armor",Choices()); Equal("armor",s.Load().Progress.GetCustomization("a").OptionId);
    }
    private static void CustomizationConcurrent()
    {
        var s=AdvancementStore(out var files); var def=Choices(); Parallel.For(0,8,_=>s.Customize("one","a","health",def)); Equal(3,files.Writes);
    }
    private static void CustomizationImmutable()
    {
        var s=AdvancementStore(out _); var before=s.Load().Progress; var after=s.Customize("one","a","health",Choices()).Saved.Progress;
        Equal(true,before.GetCustomization("a").Bonus.IsNeutral); Throws<NotSupportedException>(()=>((IList<CustomizationReceipt>)after.CustomizationReceipts).Clear());
        Throws<NotSupportedException>(()=>((IDictionary<string,UnitCustomizationState>)after.Customizations).Clear());
        var options=new[] { new CustomizationOption("one",new CustomizationBonus(200,-200,0)) }; var def=new UnitCustomizationDefinition("a","v",options); options[0]=null; Equal("one",def.Options[0].Id);
    }
    private static void CustomizationOtherTransactions()
    {
        var x=new CampaignFixture(); x.Finish(); x.Store.Customize("one","a","health",Choices());
        x.Store.Advance("advance",AdvanceDef()); x.Store.Unlock("unlock",UnlockDef("extra",cost:1)); x.Store.ClaimCampaign("starter",x.Catalog);
        x.Store.Claim(Result("loss","m1",false),Rewards("m1"));
        Equal("health",x.Store.Load().Progress.GetCustomization("a").OptionId); Equal(23L,x.Store.Load().Progress.Balance("test-resource"));
    }
    private static void CustomizationSchemaFour()
    {
        var s=AdvancementStore(out var files); s.Advance("advance",AdvanceDef());
        for(int i=0;i<2;i++)
        {
            var current=files.Inner.Slots[i]; var bytes=current.Take(current.Length-36).Concat(new byte[32]).ToArray();
            using var stream=new MemoryStream(bytes); using var r=new BinaryReader(stream); stream.Position=8; r.ReadString();
            Array.Copy(BitConverter.GetBytes(4),0,bytes,(int)stream.Position,4); Rehash(bytes); files.Inner.Slots[i]=bytes;
        }
        Equal(1,s.Load().Progress.GetAdvancement("a").Rank); Equal(true,s.Load().Progress.GetCustomization("a").Bonus.IsNeutral);
        s.Customize("one","a","health",Choices()); Equal(7L,s.Load().Progress.Balance("test-resource"));
    }
    private static void CustomizationCombined()
    {
        var mods=DeploymentModifiers.Combine(new AdvancementBonus(1000,1000,1000),new CustomizationBonus(500,-500,0));
        Equal(1500,mods.Health); Equal(500,mods.Armor); Equal(1000,mods.Power);
        var s=AdvancementStore(out _); s.Customize("one","a","health",Choices()); s.Advance("advance",AdvanceDef());
        Equal(1000,s.Load().Progress.GetDeploymentModifiers("a").Health); Equal(0,s.Load().Progress.GetDeploymentModifiers("a").Armor);
    }
    private static void CustomizationDeployment()
    {
        var x=new AdvancementFixture(); var f=x.Flow(Choices()); f.Open(); f.CustomizeUnit("one","a","health"); f.Start("mixed",new[] { "a" });
        Equal(105,f.ReadBattle().Health.GetState("a").MaximumHealth); Equal(19,f.ReadBattle().Health.GetState("a").Armor);
        Equal(20m,f.ReadBattle().CurrentActivation.MovementRemaining); Equal("a,b,c",string.Join(",",f.ReadBattle().RoundOrder));
        WinFlow(f); f.ClaimRewards(); f.ReturnToSelection(); f.CustomizeUnit("two","a","armor"); f.Start("mixed",new[] { "a" });
        Equal(95,f.ReadBattle().Health.GetState("a").MaximumHealth); Equal(21,f.ReadBattle().Health.GetState("a").Armor);
    }
    private static void CustomizationBattleCap()
    {
        var x=new AdvancementFixture { BaseHealth=1000 }; var f=x.Flow(Choices()); f.Open(); f.AdvanceUnit("a1","a"); f.AdvanceUnit("a2","a");
        f.CustomizeUnit("one","a","health"); f.Start("mixed",new[] { "a" }); Equal(1150,f.ReadBattle().Health.GetState("a").MaximumHealth); Equal(21,f.ReadBattle().Health.GetState("a").Armor);
    }
    private static void CustomizationBattleReset()
    {
        var x=new AdvancementFixture(); var f=x.Flow(Choices()); f.Open(); f.AdvanceUnit("a1","a"); f.CustomizeUnit("one","a","health");
        f.CustomizeUnit("reset","a",null); f.Start("mixed",new[] { "a" }); Equal(105,f.ReadBattle().Health.GetState("a").MaximumHealth); Equal(21,f.ReadBattle().Health.GetState("a").Armor);
    }
    private static void CustomizationResume()
    {
        var x=new AdvancementFixture(); var f=x.Flow(Choices()); f.Open(); f.CustomizeUnit("one","a","health"); f.Start("mixed",new[] { "a" });
        x.Store.Customize("two","a","armor",Choices()); f=x.Flow(Choices()); f.Open(); f.Open();
        Equal(105,f.ReadBattle().Health.GetState("a").MaximumHealth); WinFlow(f); f.ClaimRewards(); f.ReturnToSelection();
        f.Start("mixed",new[] { "a" }); Equal(95,f.ReadBattle().Health.GetState("a").MaximumHealth);
    }
    private static void CustomizationModes()
    {
        var x=new AdvancementFixture(); var f=x.Flow(Choices()); f.Open(); f.CustomizeUnit("one","a","health");
        foreach(string id in new[] { "mixed","campaign" })
        { f.Start(id,new[] { "a" }); Equal(105,f.ReadBattle().Health.GetState("a").MaximumHealth); WinFlow(f); f.ClaimRewards(); f.ReturnToSelection(); }
    }
    private static void CustomizationFlowGate()
    {
        var x=new AdvancementFixture(); var f=x.Flow(Choices()); f.Open(); f.Start("mixed",new[] { "a" });
        Throws<InvalidOperationException>(()=>f.CustomizeUnit("one","a","health")); WinFlow(f); Throws<InvalidOperationException>(()=>f.CustomizeUnit("one","a",null));
    }
    private static void CustomizationFlowRetry()
    {
        var x=new AdvancementFixture(); var f=x.Flow(Choices()); f.Open(); x.Files.Inner.Fault=3;
        Throws<IOException>(()=>f.CustomizeUnit("one","a","health")); Equal(true,f.NeedsReload); x.Files.Inner.Fault=0; f.Open();
        Equal(true,f.CustomizeUnit("one","a","health").AlreadyApplied); Equal(10L,f.Progress.Balance("test-resource"));
    }
    private static void CustomizationRounding()
    {
        var x=new AdvancementFixture { BaseDamage=1 }; var f=x.Flow(Choices()); f.Open(); f.CustomizeUnit("one","a","durable"); f.Start("mixed",new[] { "a" });
        f.Execute(b=>Equal(1,FieldHit(b).HealthChanged));
    }
    private static void CustomizationPower()
    {
        var x=new AdvancementFixture(); var f=x.Flow(Choices()); f.Open(); f.CustomizeUnit("one","a","durable"); f.Start("mixed",new[] { "a" });
        f.Execute(b=>Equal(38,FieldHit(b).HealthChanged)); Equal(false,f.ReadBattle().CurrentActivation.PrimaryActionAvailable); Equal(false,f.ReadBattle().Abilities.GetState("a").SignatureUsed);
    }
    private static void CustomizationDirectory()
    {
        string dir=Path.Combine(Path.GetTempPath(),"ninefold-custom-"+Guid.NewGuid().ToString("N"));
        try
        {
            var s=new LocalProgressStore(new DirectoryProgressSaveFiles(dir),"p"); s.Create(new[] { "a" }); s.Customize("one","a","health",Choices());
            s=new LocalProgressStore(new DirectoryProgressSaveFiles(dir),"p"); Equal("health",s.Load().Progress.GetCustomization("a").OptionId);
            s.Customize("reset","a",null); Equal(true,s.Load().Progress.GetCustomization("a").Bonus.IsNeutral); Equal(0,s.Load().Progress.Balances.Count);
        }
        finally { if(Directory.Exists(dir)) Directory.Delete(dir,true); }
    }
}
