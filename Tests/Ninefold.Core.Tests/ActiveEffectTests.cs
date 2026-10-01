using System;
using System.IO;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Persistence;
using Ninefold.Core.Progression;

internal static partial class Program
{
    private static (string Name,Action Run)[] ActiveEffectTests() => new (string,Action)[] {
        ("Compound preview is pure and predicts health and status", ActivePreview),
        ("Compound damage commits one action and one attached status", ActiveDamage),
        ("Standalone support spends main cooldown without changing health", ActiveSupport),
        ("Standalone Signature remains once per battle after reload", ActiveSignature),
        ("Healing with status works at full health", ActiveFullHealth),
        ("Status-only targets enforce ally self and enemy rules", ActiveTargets),
        ("Allied support checks authored compatibility", ActiveCompatibility),
        ("Unknown attached status rejects before costs or damage", ActiveUnknownStatus),
        ("Blocked out-of-range stale and spent commands are atomic", ActiveInvalid),
        ("Lethal damage skips status and evaluates victory after full commit", ActiveLethal),
        ("Damage passive and attached status stack preview matches commit", ActivePassiveOrder),
        ("Attached debuff affects following hits not its own hit", ActiveDebuffOrder),
        ("Fresh execution recalculates preview after state changes", ActiveRevalidate),
        ("Health-shaped compatibility API rejects standalone effects", ActiveLegacyApi),
        ("Compound ability profiles survive snapshot round trip", ActiveSave),
        ("Schema two battlefield profiles migrate with default targeting", ActiveSchemaTwo),
        ("Schema two effects survive upgrade without replay", ActiveSchemaTwoStatuses),
        ("Torn compound save recovers costs health and status together", ActiveInterrupted),
        ("Lost write acknowledgement restores a single compound cast", ActiveAmbiguous),
        ("Catalog rejects missing active status references", ActiveCatalogInvalid),
        ("Mixed-faction support works through content and flow", ActiveMixedFlow),
        ("Progression keeps status attachment and target rules", ActiveProgression),
        ("Enemy health-based planning executes compound effects", ActiveEnemy),
        ("Definitions reject contradictory health and target rules", ActiveDefinitions)
    };
    private static FieldAbility Attached(AbilitySlot slot=AbilitySlot.NormalAttack,int amount=40,string status="effect")
        => new FieldAbility(slot,HealthEffectKind.Damage,amount,10,true,status);
    private static BattleTurnController ActiveBattle(FieldAbility ability=null, bool passive=false, bool compatible=true,
        FieldObstacle[] walls=null, int targetHealth=100, int allyHealth=100, bool start=true)
    {
        var b=B(U("a",30),U("b",20),U("c",10));
        foreach(string id in new[] { "a","b","c" })
        {
            b.Health.RegisterHealth(id,new UnitHealthDefinition(id=="b"?"enemy":"ally",100,20),id=="b"?targetHealth:id=="c"?allyHealth:100);
            b.Abilities.RegisterKit(id,new UnitAbilityDefinition(2,SignatureReadiness.ReadyAtDeployment));
        }
        b.Statuses.RegisterStatus(new StatusDefinition("effect",-1000,0,2,3,StatusStacking.AddStackAndRefresh));
        if(passive) b.Statuses.RegisterPassive("b",new PassiveDefinition(PassiveTrigger.OwnerSurvivedDamage,"effect"));
        var f=b.ConfigureBattlefield(new BattlefieldMap(Box(-20,-20,20,20),walls??Array.Empty<FieldObstacle>()));
        var profile=ability??Attached();
        f.Register("a",P(0,0),Body(),Profiles().Where(p=>p.Slot!=profile.Slot).Concat(new[] { profile }),compatible?new[] { "a","c" }:Array.Empty<string>());
        f.Register("b",P(6,0),Body(),new[] { Attached(),new FieldAbility(AbilitySlot.Main,HealthEffectKind.Healing,30,10,false),new FieldAbility(AbilitySlot.Signature,HealthEffectKind.Damage,60,10,true,"effect") },new[] { "b" });
        f.Register("c",P(0,6),Body(),Profiles(),new[] { "a","c" });
        if(start) { b.StartNextRound(); b.BeginNextActivation(); } return b;
    }
    private static AbilityEffectPreview Cast(BattleTurnController b, AbilitySlot slot=AbilitySlot.NormalAttack,string target="b")
    { Equal(true,b.Battlefield.TryUseAbility(b.CurrentActivation.ActivationId,slot,target,out var r,out _,out _)); return r; }
    private static void ActivePreview()
    {
        var b=ActiveBattle(); var before=BattleSave.Capture(b,SaveRevision);
        Equal(true,b.Battlefield.TryPreviewAbility(b.CurrentActivation.ActivationId,AbilitySlot.NormalAttack,"b",out var p,out _,out _));
        Equal(33,p.Health.HealthChanged); Equal(1,p.Status.StacksAfter); Equal(2,p.Status.RemainingOwnerActivations);
        Equal(true,before.SequenceEqual(BattleSave.Capture(b,SaveRevision)));
    }
    private static void ActiveDamage()
    {
        var b=ActiveBattle(); var p=Cast(b); Equal(p.Health.HealthAfter,b.Health.GetState("b").CurrentHealth);
        Equal(p.Status.StacksAfter,b.Statuses.GetStatuses("b").Single().Stacks); Equal(false,b.CurrentActivation.PrimaryActionAvailable);
        Equal(5m,b.CurrentActivation.MovementRemaining); Equal(false,b.Abilities.GetState("a").SignatureUsed);
    }
    private static void ActiveSupport()
    {
        var b=ActiveBattle(new FieldAbility(AbilitySlot.Main,"effect",AbilityTarget.Ally,10)); var r=Cast(b,AbilitySlot.Main,"c");
        Equal<HealthEffectPreview>(null,r.Health); Equal(100,b.Health.GetState("c").CurrentHealth);
        Equal(2,b.Abilities.GetState("a").MainCooldownRemaining); Equal(1,b.Statuses.GetStatuses("c").Count);
    }
    private static void ActiveSignature()
    {
        var b=ActiveBattle(new FieldAbility(AbilitySlot.Signature,"effect",AbilityTarget.Self,0)); Cast(b,AbilitySlot.Signature,"a");
        b=Reload(b); StatusNextRound(b); Equal(AbilityUseFailure.SignatureAlreadyUsed,b.Abilities.GetAvailability(b.CurrentActivation.ActivationId,AbilitySlot.Signature));
        Equal(false,b.Battlefield.TryUseAbility(b.CurrentActivation.ActivationId,AbilitySlot.Signature,"a",out _,out _,out _));
    }
    private static void ActiveFullHealth()
    {
        var b=ActiveBattle(new FieldAbility(AbilitySlot.Main,HealthEffectKind.Healing,30,10,false,"effect")); var p=Cast(b,AbilitySlot.Main,"c");
        Equal(0,p.Health.HealthChanged); Equal(1,p.Status.StacksAfter); Equal(false,b.CurrentActivation.PrimaryActionAvailable);
        var plain=ActiveBattle(new FieldAbility(AbilitySlot.Main,HealthEffectKind.Healing,30,10,false));
        Equal(false,plain.Battlefield.TryUseAbility(plain.CurrentActivation.ActivationId,AbilitySlot.Main,"c",out _,out _,out var failure)); Equal(HealthActionFailure.AlreadyFullHealth,failure);
    }
    private static void ActiveTargets()
    {
        foreach(var rule in new[] { AbilityTarget.Enemy,AbilityTarget.Ally,AbilityTarget.Self,AbilityTarget.AllyOrSelf })
            foreach(string target in new[] { "a","b","c" })
            {
                var b=ActiveBattle(new FieldAbility(AbilitySlot.Main,"effect",rule,10));
                bool expected=rule==AbilityTarget.Enemy?target=="b":rule==AbilityTarget.Ally?target=="c":rule==AbilityTarget.Self?target=="a":target!="b";
                Equal(expected,b.Battlefield.TryUseAbility(b.CurrentActivation.ActivationId,AbilitySlot.Main,target,out _,out _,out _));
                Equal(!expected,b.CurrentActivation.PrimaryActionAvailable);
            }
    }
    private static void ActiveCompatibility()
    {
        var b=ActiveBattle(new FieldAbility(AbilitySlot.Main,"effect",AbilityTarget.Ally,10),compatible:false);
        Equal(false,b.Battlefield.TryUseAbility(b.CurrentActivation.ActivationId,AbilitySlot.Main,"c",out _,out _,out var failure)); Equal(HealthActionFailure.Incompatible,failure);
        var self=ActiveBattle(new FieldAbility(AbilitySlot.Main,"effect",AbilityTarget.Self,0),compatible:false); Cast(self,AbilitySlot.Main,"a");
    }
    private static void ActiveUnknownStatus()
    {
        var b=ActiveBattle(Attached(status:"missing")); Equal(false,b.Battlefield.TryUseAbility(b.CurrentActivation.ActivationId,AbilitySlot.NormalAttack,"b",out _,out _,out _));
        Equal(100,b.Health.GetState("b").CurrentHealth); Equal(true,b.CurrentActivation.PrimaryActionAvailable); Equal(0,b.Statuses.GetStatuses("b").Count);
        Throws<InvalidDataException>(()=>BattleSave.Capture(b,SaveRevision));
    }
    private static void ActiveInvalid()
    {
        foreach(var b in new[] { ActiveBattle(new FieldAbility(AbilitySlot.NormalAttack,HealthEffectKind.Damage,40,1,true,"effect")),
            ActiveBattle(walls:new[] { new FieldObstacle(Box(2,-1,3,1),false,true) }) })
        {
            var before=BattleSave.Capture(b,SaveRevision); Equal(false,b.Battlefield.TryUseAbility(b.CurrentActivation.ActivationId,AbilitySlot.NormalAttack,"b",out _,out _,out _));
            Equal(true,before.SequenceEqual(BattleSave.Capture(b,SaveRevision)));
        }
        var battle=ActiveBattle(); Equal(false,battle.Battlefield.TryUseAbility(999,AbilitySlot.NormalAttack,"b",out _,out _,out _)); Cast(battle);
        var saved=BattleSave.Capture(battle,SaveRevision); Equal(false,battle.Battlefield.TryUseAbility(battle.CurrentActivation.ActivationId,AbilitySlot.NormalAttack,"b",out _,out _,out _)); Equal(true,saved.SequenceEqual(BattleSave.Capture(battle,SaveRevision)));
    }
    private static void ActiveLethal()
    {
        var b=ActiveBattle(Attached(amount:200),passive:true,start:false);
        b.ConfigureMission(new MissionDefinition("m","attempt",new[] { "a" },ObjectiveDefinition.Defeat("primary",new[] { "b" }),OutcomePriority.FailureFirst));
        b.StartNextRound(); b.BeginNextActivation(); var p=Cast(b); Equal(false,p.Status.Applies); Equal(0,b.Statuses.GetStatuses("b").Count);
        Equal(MissionOutcome.Victory,b.Mission.Result.Outcome); Equal(true,b.IsBattleEnded);
    }
    private static void ActivePassiveOrder()
    { var b=ActiveBattle(passive:true); var r=Cast(b); Equal(2,r.Status.StacksAfter); Equal(2,b.Statuses.GetStatuses("b").Single().Stacks); }
    private static void ActiveDebuffOrder()
    {
        var b=ActiveBattle(); Equal(33,Cast(b).Health.HealthChanged); StatusNextRound(b); Equal(34,Cast(b).Health.HealthChanged);
    }
    private static void ActiveRevalidate()
    {
        var b=ActiveBattle(); b.Battlefield.TryPreviewAbility(b.CurrentActivation.ActivationId,AbilitySlot.NormalAttack,"b",out var p,out _,out _);
        b.Statuses.Apply("b","effect"); var result=Cast(b); Equal(33,p.Health.HealthChanged); Equal(34,result.Health.HealthChanged); Equal(2,result.Status.StacksAfter);
    }
    private static void ActiveLegacyApi()
    {
        var b=ActiveBattle(new FieldAbility(AbilitySlot.Main,"effect",AbilityTarget.Self,0));
        Equal(false,b.Battlefield.TryApplyEffect(b.CurrentActivation.ActivationId,AbilitySlot.Main,"a",out _,out _,out _));
        Equal(true,b.CurrentActivation.PrimaryActionAvailable); Equal(0,b.Statuses.GetStatuses("a").Count); Cast(b,AbilitySlot.Main,"a");
    }
    private static void ActiveSave()
    {
        foreach(var profile in new[] { Attached(),new FieldAbility(AbilitySlot.Main,"effect",AbilityTarget.Ally,10),new FieldAbility(AbilitySlot.Main,HealthEffectKind.Healing,20,10,false,"effect",AbilityTarget.Ally) })
        { var b=ActiveBattle(profile); var r=Reload(b); SameSave(b,r); Cast(b,profile.Slot,profile.Target==AbilityTarget.Enemy?"b":"c"); Cast(r,profile.Slot,profile.Target==AbilityTarget.Enemy?"b":"c"); SameSave(b,r); }
    }
    private static void ActiveSchemaTwo()
    {
        // Explicit historical v2 wire fixture includes a battlefield health-only profile.
        using var stream=new MemoryStream(); using var w=new BinaryWriter(stream);
        w.Write(0x4E465356); w.Write(2); w.Write(SaveRevision); w.Write(100); w.Write(.70m); w.Write(true);
        w.Write(1); w.Write("a"); w.Write(10); w.Write(5m); w.Write(10); w.Write(0L); w.Write(true);
        w.Write(0); w.Write(false); w.Write(0); w.Write(0L); w.Write(0); w.Write(false);
        w.Write(0); w.Write(1); w.Write("a"); w.Write("ally"); w.Write(100); w.Write(0); w.Write(100);
        void Point(decimal x,decimal y,decimal z) { w.Write(x); w.Write(y); w.Write(z); }
        w.Write(true); w.Write(true); Point(-20,0,-20); Point(20,5,20); w.Write(0); w.Write(0); w.Write(1); w.Write("a"); Point(0,0,0);
        w.Write(.25m); w.Write(.25m); w.Write(2m); Point(0,1,0); Point(0,1,0);
        w.Write(1); w.Write((int)AbilitySlot.NormalAttack); w.Write((int)HealthEffectKind.Damage); w.Write(40); w.Write(10m); w.Write(true);
        w.Write(0); w.Write(0); w.Write(false); w.Write(0); w.Write(0); w.Write(0); w.Write(new byte[32]); w.Flush();
        var bytes=stream.ToArray(); Rehash(bytes); var r=BattleSave.Restore(bytes,SaveRevision);
        var expected=B(U("a",10,5)); expected.Health.RegisterHealth("a",new UnitHealthDefinition("ally",100,0));
        expected.ConfigureBattlefield(new BattlefieldMap(Box(-20,-20,20,20),Array.Empty<FieldObstacle>())).Register("a",P(0,0),Body(),new[] { new FieldAbility(AbilitySlot.NormalAttack,HealthEffectKind.Damage,40,10,true) });
        SameSave(expected,r);
    }
    private static void ActiveSchemaTwoStatuses()
    {
        var b=B(U("a")); b.Health.RegisterHealth("a",new UnitHealthDefinition("ally",100,0));
        b.Statuses.RegisterStatus(new StatusDefinition("saved-effect",1000,0,2)); b.Statuses.RegisterPassive("a",new PassiveDefinition(PassiveTrigger.OwnerActivationStarted,"saved-effect"));
        b.StartNextRound(); b.BeginNextActivation();
        // No battlefield layout changes in this fixture; version two appended the same status section.
        var bytes=BattleSave.Capture(b,SaveRevision); Array.Copy(BitConverter.GetBytes(2),0,bytes,4,4); Rehash(bytes); SameSave(b,BattleSave.Restore(bytes,SaveRevision));
    }
    private static void ActiveInterrupted()
    {
        var b=ActiveBattle(); var files=new FakeSaveFiles(); var store=new LocalBattleStore(files,SaveRevision); store.Save(b); Cast(b); files.Fault=2;
        Throws<IOException>(()=>store.Save(b)); files.Fault=0; var r=store.Load().Battle;
        Equal(100,r.Health.GetState("b").CurrentHealth); Equal(0,r.Statuses.GetStatuses("b").Count); Equal(true,r.CurrentActivation.PrimaryActionAvailable);
    }
    private static void ActiveAmbiguous()
    {
        var b=ActiveBattle(); var files=new FakeSaveFiles(); var store=new LocalBattleStore(files,SaveRevision); store.Save(b); Cast(b); files.Fault=3;
        Throws<IOException>(()=>store.Save(b)); files.Fault=0; var r=store.Load().Battle;
        Equal(67,r.Health.GetState("b").CurrentHealth); Equal(1,r.Statuses.GetStatuses("b").Single().Stacks); Equal(false,r.CurrentActivation.PrimaryActionAvailable);
    }
    private static ContentCatalog ActiveCatalog(bool definitions=true, bool health=false)
    {
        var c=AbstractContentPackage.Create(); var abilities=c.Abilities.Values.Where(a=>a.Id!="fixture-main").Concat(new[] {
            new AbilityContent("fixture-main",health ? new FieldAbility(AbilitySlot.Main,HealthEffectKind.Healing,30,12,false,"support",AbilityTarget.Ally)
            : new FieldAbility(AbilitySlot.Main,"support",AbilityTarget.Ally,12)) });
        return new ContentCatalog(new ContentManifest("active-fixture","v1","v1",ContentClassification.AbstractFixture),c.Resources,abilities,c.Kits.Values,c.Units.Values,c.Missions.Values,c.Rewards.Values,c.Campaigns,c.StarterUnits,
            definitions?new[] { new StatusDefinition("support",1000,0,2) }:Array.Empty<StatusDefinition>());
    }
    private static void ActiveCatalogInvalid() => Throws<ArgumentException>(()=>ActiveCatalog(false));
    private static void ActiveMixedFlow()
    {
        var c=ActiveCatalog(); var f=ContentFlow(c); ContentClear(f,"fixture-opening","fixture-a-1"); ContentClear(f,"fixture-finale","fixture-a-1"); f.ClaimCampaign("fixture-starter");
        f.Start("fixture-mixed",new[] { "fixture-a-1","fixture-b-1" }); f.Execute(b=>Cast(b,AbilitySlot.Main,"fixture-b-1"));
        Equal(1,f.ReadBattle().Statuses.GetStatuses("fixture-b-1").Count); f.Open(); Equal(false,f.ReadBattle().CurrentActivation.PrimaryActionAvailable);
    }
    private static void ActiveProgression()
    {
        foreach(bool health in new[] { true,false })
        {
            var c=ActiveCatalog(health:health); var f=ContentFlow(c); ContentClear(f,"fixture-opening","fixture-a-1"); f.AdvanceUnit("advance","fixture-a-1");
            f.Start("fixture-mixed",new[] { "fixture-a-1","fixture-a-2" }); f.Execute(b=>Cast(b,AbilitySlot.Main,"fixture-a-2")); Equal(1,f.ReadBattle().Statuses.GetStatuses("fixture-a-2").Count);
        }
    }
    private static void ActiveEnemy()
    {
        var b=ActiveBattle(start:false); b.Battlefield.RegisterEnemy("b",new EnemyBehavior(EnemyStyle.Aggressive,6)); b.StartNextRound(); b.BeginNextActivation(); b.EndActivation(b.CurrentActivation.ActivationId); b.BeginNextActivation();
        Equal(true,b.Battlefield.TryRunEnemyTurn(b.CurrentActivation.ActivationId,new[] { "a" },out _,out _)); Equal(1,b.Statuses.GetStatuses("a").Count);
    }
    private static void ActiveDefinitions()
    {
        Throws<ArgumentException>(()=>new FieldAbility(AbilitySlot.Main,HealthEffectKind.Damage,10,5,true,"s",AbilityTarget.Ally));
        Throws<ArgumentException>(()=>new FieldAbility(AbilitySlot.Main,HealthEffectKind.Healing,10,5,false,"s",AbilityTarget.Enemy));
        Throws<ArgumentException>(()=>new FieldAbility(AbilitySlot.Main," ",AbilityTarget.Self,0));
    }
}
