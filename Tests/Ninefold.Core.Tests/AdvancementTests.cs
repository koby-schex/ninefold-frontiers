using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Progression;

internal static partial class Program
{
    private static (string Name, Action Run)[] AdvancementTests() => new (string, Action)[]
    {
        ("Owned units start at advancement rank zero", AdvancementBase),
        ("Advancement commits rank bonus and fragment spending together", AdvancementCommit),
        ("Sequential advancement uses cumulative bonuses and a cap", AdvancementCap),
        ("Advancement retry retains original cost and grants no extra rank", AdvancementRetry),
        ("Advancement rejects conflicting operation reuse", AdvancementConflict),
        ("Unowned units and wrong fragments cannot advance", AdvancementEligibility),
        ("Advancement definitions reject excessive decreasing or duplicate bonuses", AdvancementDefinitions),
        ("Changed prior advancement bonuses require explicit migration", AdvancementChangedDefinition),
        ("Failed and torn advancement writes preserve prior rank and fragments", AdvancementInterrupted),
        ("Lost advancement acknowledgement cannot double spending", AdvancementAmbiguous),
        ("Advancement recovery rolls rank and spend back together", AdvancementRecovery),
        ("Concurrent retry advances once on a single store", AdvancementConcurrent),
        ("Advancement snapshots remain immutable", AdvancementImmutable),
        ("Defeat rewards and later unlocks preserve advancement", AdvancementOtherTransactions),
        ("Campaign grants preserve advancement and permit advancing an awarded Apex", AdvancementCampaign),
        ("Legacy schema three progresses to schema four without resetting rewards", AdvancementSchemaThree),
        ("Deployment applies bounded health armor and damage bonuses", AdvancementDeployment),
        ("Deployment increases healing power without changing ability costs", AdvancementHealing),
        ("Resuming a battle neither stacks bonuses nor applies newer ranks", AdvancementResume),
        ("Shared progression applies across faction and mixed mission entries", AdvancementModes),
        ("Flow permits advancement only from mission selection", AdvancementFlowGate),
        ("Flow reload resolves a lost advancement acknowledgement", AdvancementFlowRetry),
        ("Integer rounding never creates stats above the proportional cap", AdvancementRounding),
        ("Unsafe deployment scaling aborts before saving a battle", AdvancementOverflow),
        ("Real directory reopening retains progression and spending", AdvancementDirectory)
    };
    private static UnitAdvancementDefinition AdvanceDef(string unit = "a", string resource = "test-resource", long firstCost = 3, int firstBonus = 500)
        => new UnitAdvancementDefinition(unit,resource,"test-v1",new AdvancementBonus(1000,1000,1000),new[] {
            new AdvancementStep(firstCost,new AdvancementBonus(firstBonus,firstBonus,firstBonus)),
            new AdvancementStep(4,new AdvancementBonus(1000,1000,1000)) });
    private static LocalProgressStore AdvancementStore(out ProgressFiles files)
    {
        files=new ProgressFiles(); var s=new LocalProgressStore(files,"advance-profile"); s.Create(new[] { "a","c","d" }); s.Claim(Result(),Rewards()); return s;
    }
    private static void AdvancementBase()
    {
        var s=AdvancementStore(out _); var a=s.Load().Progress.GetAdvancement("a"); Equal(0,a.Rank); Equal(0,a.Bonus.Health);
        Throws<ArgumentException>(()=>s.Load().Progress.GetAdvancement("unknown"));
    }
    private static void AdvancementCommit()
    {
        var s=AdvancementStore(out _); var r=s.Advance("op",AdvanceDef());
        Equal(false,r.AlreadyApplied); Equal(1,r.Saved.Progress.GetAdvancement("a").Rank); Equal(500,r.Receipt.TotalBonus.Health);
        Equal(7L,r.Saved.Progress.Balance("test-resource")); Equal(3L,r.Saved.Generation);
    }
    private static void AdvancementCap()
    {
        var s=AdvancementStore(out var files); s.Advance("op1",AdvanceDef()); s.Advance("op2",AdvanceDef());
        Equal(2,s.Load().Progress.GetAdvancement("a").Rank); Equal(1000,s.Load().Progress.GetAdvancement("a").Bonus.Power);
        Equal(3L,s.Load().Progress.Balance("test-resource")); Throws<InvalidOperationException>(()=>s.Advance("op3",AdvanceDef())); Equal(4,files.Writes);
    }
    private static void AdvancementRetry()
    {
        var s=AdvancementStore(out var files); s.Advance("op",AdvanceDef()); files.Inner.Fault=1;
        var r=new LocalProgressStore(files,"advance-profile").Advance("op",AdvanceDef(firstCost:99,firstBonus:400));
        Equal(true,r.AlreadyApplied); Equal(3L,r.Receipt.Cost); Equal(500,r.Receipt.TotalBonus.Power); Equal(3,files.Writes);
    }
    private static void AdvancementConflict()
    {
        var s=AdvancementStore(out var files); s.Advance("op",AdvanceDef());
        Throws<InvalidOperationException>(()=>s.Advance("op",AdvanceDef("c"))); Equal(3,files.Writes);
    }
    private static void AdvancementEligibility()
    {
        var s=AdvancementStore(out var files); Throws<ArgumentException>(()=>s.Advance("op",AdvanceDef("unknown")));
        Throws<InvalidOperationException>(()=>s.Advance("op",AdvanceDef(resource:"wrong")));
        Throws<InvalidOperationException>(()=>s.Advance("op",AdvanceDef(firstCost:11))); Equal(2,files.Writes);
    }
    private static void AdvancementDefinitions()
    {
        Throws<ArgumentOutOfRangeException>(()=>new AdvancementBonus(1001,0,0)); Throws<ArgumentOutOfRangeException>(()=>new AdvancementBonus(0,-1,0));
        Throws<ArgumentOutOfRangeException>(()=>new AdvancementStep(0,AdvancementBonus.None));
        foreach(var steps in new[] {
            new[] { new AdvancementStep(1,AdvancementBonus.None) },
            new[] { new AdvancementStep(1,new AdvancementBonus(500,0,0)),new AdvancementStep(1,new AdvancementBonus(400,0,0)) },
            new[] { new AdvancementStep(1,new AdvancementBonus(500,0,0)),new AdvancementStep(1,new AdvancementBonus(500,0,0)) } })
            Throws<ArgumentException>(()=>new UnitAdvancementDefinition("a","r","v",new AdvancementBonus(1000,0,0),steps));
        Throws<ArgumentException>(()=>new UnitAdvancementDefinition("a","r","v",new AdvancementBonus(200,0,0),new[] { new AdvancementStep(1,new AdvancementBonus(300,0,0)) }));
        Throws<ArgumentException>(()=>new RosterUnit("a","f",false,UnlockDef("a"),AdvanceDef(resource:"wrong")));
    }
    private static void AdvancementChangedDefinition()
    {
        var s=AdvancementStore(out var files); s.Advance("op1",AdvanceDef());
        Throws<InvalidOperationException>(()=>s.Advance("op2",AdvanceDef(firstBonus:400))); Equal(3,files.Writes);
    }
    private static void AdvancementInterrupted()
    {
        foreach(int fault in new[] { 1,2 })
        {
            var s=AdvancementStore(out var files); files.Inner.Fault=fault; Throws<IOException>(()=>s.Advance("op",AdvanceDef())); files.Inner.Fault=0;
            var p=new LocalProgressStore(files,"advance-profile").Load().Progress; Equal(0,p.GetAdvancement("a").Rank); Equal(10L,p.Balance("test-resource"));
            s.Advance("op",AdvanceDef()); Equal(7L,s.Load().Progress.Balance("test-resource"));
        }
    }
    private static void AdvancementAmbiguous()
    {
        var s=AdvancementStore(out var files); files.Inner.Fault=3; Throws<IOException>(()=>s.Advance("op",AdvanceDef())); files.Inner.Fault=0;
        Equal(true,s.Advance("op",AdvanceDef()).AlreadyApplied); Equal(7L,s.Load().Progress.Balance("test-resource")); Equal(3,files.Writes);
    }
    private static void AdvancementRecovery()
    {
        var s=AdvancementStore(out var files); s.Advance("op",AdvanceDef()); files.Inner.Slots[0][20]^=1;
        var p=s.Load(); Equal(true,p.Recovered); Equal(0,p.Progress.GetAdvancement("a").Rank); Equal(10L,p.Progress.Balance("test-resource"));
        s.Advance("op",AdvanceDef()); Equal(7L,s.Load().Progress.Balance("test-resource"));
    }
    private static void AdvancementConcurrent()
    {
        var s=AdvancementStore(out var files); var d=AdvanceDef(); Parallel.For(0,8,_=>s.Advance("op",d)); Equal(3,files.Writes); Equal(1,s.Load().Progress.GetAdvancement("a").Rank);
    }
    private static void AdvancementImmutable()
    {
        var s=AdvancementStore(out _); var before=s.Load().Progress; var after=s.Advance("op",AdvanceDef()).Saved.Progress;
        Equal(0,before.GetAdvancement("a").Rank); Throws<NotSupportedException>(()=>((IList<AdvancementReceipt>)after.AdvancementReceipts).Clear());
        Throws<NotSupportedException>(()=>((IDictionary<string,UnitAdvancementState>)after.Advancements).Clear());
    }
    private static void AdvancementOtherTransactions()
    {
        var s=AdvancementStore(out _); s.Advance("op",AdvanceDef()); s.Claim(Result("loss",victory:false),Rewards());
        s.Claim(Result("replay"),Rewards()); s.Unlock("unlock",UnlockDef("new-unit",cost:1));
        Equal(1,s.Load().Progress.GetAdvancement("a").Rank); Equal(8L,s.Load().Progress.Balance("test-resource"));
    }
    private static void AdvancementCampaign()
    {
        var x=new CampaignFixture(); x.Finish(); x.Store.Advance("op",AdvanceDef()); x.Store.ClaimCampaign("starter",x.Catalog);
        x.Store.Advance("apex",AdvanceDef("x")); var p=x.Store.Load().Progress;
        Equal(1,p.GetAdvancement("a").Rank); Equal(1,p.GetAdvancement("x").Rank); Equal(21L,p.Balance("test-resource"));
    }
    private static void AdvancementSchemaThree()
    {
        var x=new CampaignFixture(); x.Finish(); x.Store.ClaimCampaign("starter",x.Catalog);
        for(int slot=0;slot<2;slot++)
        {
            var current=x.Files.Inner.Slots[slot]; var bytes=current.Take(current.Length-40).Concat(new byte[32]).ToArray();
            using var stream=new MemoryStream(bytes); using var r=new BinaryReader(stream); stream.Position=8; r.ReadString();
            Array.Copy(BitConverter.GetBytes(3),0,bytes,(int)stream.Position,4); Rehash(bytes); x.Files.Inner.Slots[slot]=bytes;
        }
        Equal(0,x.Store.Load().Progress.GetAdvancement("x").Rank); x.Store.Advance("op",AdvanceDef("x"));
        Equal(1,x.Store.Load().Progress.CampaignClaims.Count); Equal(24L,x.Store.Load().Progress.Balance("test-resource"));
    }
    private sealed class AdvancementFixture
    {
        internal readonly FakeSaveFiles Battles=new FakeSaveFiles();
        internal readonly ProgressFiles Files=new ProgressFiles();
        internal readonly LocalProgressStore Store;
        internal int BaseHealth=100, BaseArmor=20, BaseDamage=40;
        internal AdvancementFixture() { Store=new LocalProgressStore(Files,"advance-profile"); Store.Create(new[] { "a","c","d" }); Store.Claim(Result(),Rewards()); }
        internal MissionFlow Flow(UnitCustomizationDefinition customization = null)
        {
            var roster=FlowRoster().Select(u=>u.Id=="a" ? new RosterUnit("a","faction",false,UnlockDef("a"),AdvanceDef(),customization) : u).ToArray();
            return new MissionFlow(Battles,Files,"advance-profile","advance-content",new[] { Entry("mixed",false),Entry("campaign",true) },roster);
        }
        private MissionEntry Entry(string id,bool campaign)
            => new MissionEntry(id,1,3,campaign ? "faction" : null,campaign,Rewards(id),(attempt,squad)=>
            {
                var b=B(U("a",30,20),U("b",20),U("c",10));
                foreach(string unit in new[] { "a","b","c" })
                {
                    b.Abilities.RegisterKit(unit,new UnitAbilityDefinition(2,SignatureReadiness.ReadyAtDeployment));
                    b.Health.RegisterHealth(unit,new UnitHealthDefinition(unit=="b" ? "enemy" : "ally",unit=="b" ? 1000 : BaseHealth,unit=="a" ? BaseArmor : 0),unit=="c" ? 50 : (int?)null);
                }
                var field=b.ConfigureBattlefield(new BattlefieldMap(Box(-20,-20,20,20,20),Array.Empty<FieldObstacle>()));
                var profiles=new[] { new FieldAbility(AbilitySlot.NormalAttack,HealthEffectKind.Damage,BaseDamage,10,true),new FieldAbility(AbilitySlot.Main,HealthEffectKind.Healing,30,10,false),new FieldAbility(AbilitySlot.Signature,HealthEffectKind.Damage,200,10,true) };
                field.Register("a",P(0,0),Body(),profiles,new[] { "a","c" }); field.Register("b",P(6,0),Body(),Profiles()); field.Register("c",P(0,6),Body(),Profiles());
                b.ConfigureMission(new MissionDefinition(id,attempt,squad,ObjectiveDefinition.Stabilize("primary",1,InteractAt()),OutcomePriority.FailureFirst));
                b.StartNextRound(); b.BeginNextActivation(); return b;
            });
    }
    private static void AdvancementDeployment()
    {
        var x=new AdvancementFixture(); var f=x.Flow(); f.Open(); f.AdvanceUnit("op","a"); f.Start("mixed",new[] { "a" });
        var b=f.ReadBattle(); Equal(105,b.Health.GetState("a").MaximumHealth); Equal(105,b.Health.GetState("a").CurrentHealth); Equal(21,b.Health.GetState("a").Armor);
        Equal(20m,b.CurrentActivation.MovementRemaining); Equal("a,b,c",string.Join(",",b.RoundOrder));
        f.Execute(d=>Equal(42,FieldHit(d).HealthChanged)); Equal(false,f.ReadBattle().CurrentActivation.PrimaryActionAvailable);
        Equal(1000,f.ReadBattle().Health.GetState("b").MaximumHealth); Equal(100,f.ReadBattle().Health.GetState("c").MaximumHealth);
    }
    private static void AdvancementHealing()
    {
        var x=new AdvancementFixture(); var f=x.Flow(); f.Open(); f.AdvanceUnit("op","a"); f.Start("mixed",new[] { "a" });
        f.Execute(b=> { Equal(true,b.Battlefield.TryApplyEffect(b.CurrentActivation.ActivationId,AbilitySlot.Main,"c",out var result,out _,out _)); Equal(31,result.HealthChanged); });
        Equal(2,f.ReadBattle().Abilities.GetState("a").MainCooldownRemaining); Equal(false,f.ReadBattle().Abilities.GetState("a").SignatureUsed);
    }
    private static void AdvancementResume()
    {
        var x=new AdvancementFixture(); var f=x.Flow(); f.Open(); f.AdvanceUnit("op1","a"); f.Start("mixed",new[] { "a" });
        // Simulate a newer profile checkpoint; a saved battle must retain its original deployment.
        x.Store.Advance("op2",AdvanceDef()); f=x.Flow(); f.Open(); Equal(105,f.ReadBattle().Health.GetState("a").MaximumHealth);
        f.Open(); Equal(105,f.ReadBattle().Health.GetState("a").MaximumHealth); WinFlow(f); f.ClaimRewards(); f.ReturnToSelection();
        f.Start("mixed",new[] { "a" }); Equal(110,f.ReadBattle().Health.GetState("a").MaximumHealth);
    }
    private static void AdvancementModes()
    {
        var x=new AdvancementFixture(); var f=x.Flow(); f.Open(); f.AdvanceUnit("op","a");
        foreach(string mission in new[] { "mixed","campaign" })
        {
            f.Start(mission,new[] { "a" }); Equal(105,f.ReadBattle().Health.GetState("a").MaximumHealth);
            WinFlow(f); f.ClaimRewards(); f.ReturnToSelection();
        }
        Equal(1,f.Progress.GetAdvancement("a").Rank);
    }
    private static void AdvancementFlowGate()
    {
        var x=new AdvancementFixture(); var f=x.Flow(); f.Open(); Throws<ArgumentException>(()=>f.AdvanceUnit("op","c")); f.Start("mixed",new[] { "a" });
        Throws<InvalidOperationException>(()=>f.AdvanceUnit("op","a")); Equal(10L,f.Progress.Balance("test-resource"));
    }
    private static void AdvancementFlowRetry()
    {
        var x=new AdvancementFixture(); var f=x.Flow(); f.Open(); x.Files.Inner.Fault=3;
        Throws<IOException>(()=>f.AdvanceUnit("op","a")); Equal(true,f.NeedsReload); x.Files.Inner.Fault=0; f.Open();
        Equal(true,f.AdvanceUnit("op","a").AlreadyApplied); Equal(1,f.Progress.GetAdvancement("a").Rank);
    }
    private static void AdvancementRounding()
    {
        var x=new AdvancementFixture { BaseArmor=0,BaseDamage=1 }; var f=x.Flow(); f.Open(); f.AdvanceUnit("op","a");
        f.Start("mixed",new[] { "a" }); Equal(0,f.ReadBattle().Health.GetState("a").Armor);
        f.Execute(b=>Equal(1,FieldHit(b).HealthChanged));
    }
    private static void AdvancementOverflow()
    {
        var x=new AdvancementFixture { BaseHealth=int.MaxValue }; var f=x.Flow(); f.Open(); f.AdvanceUnit("op","a");
        Throws<OverflowException>(()=>f.Start("mixed",new[] { "a" })); Equal<byte[]>(null,x.Battles.Slots[0]); Equal(MissionFlowPhase.Selection,f.Phase);
    }
    private static void AdvancementDirectory()
    {
        string dir=Path.Combine(Path.GetTempPath(),"ninefold-advance-"+Guid.NewGuid().ToString("N"));
        try
        {
            var s=new LocalProgressStore(new DirectoryProgressSaveFiles(dir),"p"); s.Create(new[] { "a" }); s.Claim(Result(),Rewards()); s.Advance("op",AdvanceDef());
            var p=new LocalProgressStore(new DirectoryProgressSaveFiles(dir),"p").Load().Progress; Equal(1,p.GetAdvancement("a").Rank); Equal(7L,p.Balance("test-resource"));
        }
        finally { if(Directory.Exists(dir)) Directory.Delete(dir,true); }
    }
}
