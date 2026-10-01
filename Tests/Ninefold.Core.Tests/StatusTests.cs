using System;
using System.IO;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Persistence;

internal static partial class Program
{
    private static (string Name, Action Run)[] StatusTests() => new (string, Action)[] {
        ("Passive activation grants once without spending turn resources", StatusActivation),
        ("Passive damage triggers after hit and never from previews", StatusDamage),
        ("Passive does not trigger on failed or fully blocked hits", StatusFailedHit),
        ("Passive does not trigger on lethal damage", StatusLethal),
        ("Status expires only on owner activation ends", StatusDuration),
        ("Status refresh replaces timer without stacking", StatusRefresh),
        ("Status stacks cap and refresh together", StatusStacks),
        ("Positive and negative modifiers sum with bounded totals", StatusBounds),
        ("Status armor is used by preview and commit", StatusArmor),
        ("Status healing power clamps to missing health", StatusHealing),
        ("Removing status restores baseline without cumulative rounding", StatusRemove),
        ("Status save preserves stacks duration and passive bindings", StatusSave),
        ("Reload does not repeat activation or damage passive", StatusReload),
        ("Legacy version one battle restores without effects", StatusLegacy),
        ("Corrupt status counters reject even with valid checksum", StatusCorrupt),
        ("Interrupted status checkpoint recovers previous whole event", StatusInterrupted),
        ("Ambiguous write restores one passive event", StatusAmbiguous),
        ("Catalog binds actual passives and rejects missing statuses", StatusCatalog),
        ("Flow clone save and resume preserve passive exactly once", StatusFlow),
        ("Status registration and terminal commands enforce lifecycle", StatusLifecycle),
        ("Status views are detached immutable snapshots", StatusViews),
        ("Enemy planning uses temporary power without mutating it", StatusEnemy)
    };
    private static BattleTurnController StatusBattle(PassiveTrigger? trigger = null, int duration = 2,
        StatusStacking stacking = StatusStacking.AddStackAndRefresh, int max = 3, int armor = 0, int power = 1000,
        string owner = "a", bool start = true)
    {
        var b = B(U("a",30),U("b",20),U("c",10));
        foreach (var id in new[] { "a","b","c" })
        {
            b.Health.RegisterHealth(id,new UnitHealthDefinition(id=="b" ? "enemy" : "ally",100,20),id=="c" ? 50 : 100);
            b.Abilities.RegisterKit(id,new UnitAbilityDefinition(2,SignatureReadiness.ReadyAtDeployment));
        }
        var field=b.ConfigureBattlefield(new BattlefieldMap(Box(-20,-20,20,20),Array.Empty<FieldObstacle>()));
        field.Register("a",P(0,0),Body(),Profiles(),new[] { "a","c" });
        field.Register("b",P(6,0),Body(),Profiles(),new[] { "b" });
        field.Register("c",P(0,6),Body(),Profiles(),new[] { "a","c" });
        b.Statuses.RegisterStatus(new StatusDefinition("effect",armor,power,duration,max,stacking));
        if(trigger.HasValue) b.Statuses.RegisterPassive(owner,new PassiveDefinition(trigger.Value,"effect"));
        if(start) { b.StartNextRound(); b.BeginNextActivation(); }
        return b;
    }
    private static void StatusNextRound(BattleTurnController b)
    { if (b.CurrentActivation != null) b.EndActivation(b.CurrentActivation.ActivationId); Drain(b); b.StartNextRound(); b.BeginNextActivation(); }
    private static HealthEffectPreview StatusHit(BattleTurnController b, AbilitySlot slot=AbilitySlot.NormalAttack)
    {
        Equal(true,b.Battlefield.TryApplyEffect(b.CurrentActivation.ActivationId,slot,"b",out var result,out _,out _)); return result;
    }
    private static void StatusActivation()
    {
        var b=StatusBattle(PassiveTrigger.OwnerActivationStarted); Equal(1,b.Statuses.GetStatuses("a").Single().Stacks);
        Equal(true,b.CurrentActivation.PrimaryActionAvailable); Equal(5m,b.CurrentActivation.MovementRemaining);
        Throws<InvalidOperationException>(()=>b.BeginNextActivation()); Equal(1,b.Statuses.GetStatuses("a").Single().Stacks);
        StatusNextRound(b); Equal(2,b.Statuses.GetStatuses("a").Single().Stacks); Equal(2L,b.GetActivationCount("a"));
    }
    private static void StatusDamage()
    {
        var b=StatusBattle(PassiveTrigger.OwnerSurvivedDamage,owner:"b");
        for(int i=0;i<3;i++) Equal(true,b.Battlefield.TryPreviewEffect(b.CurrentActivation.ActivationId,AbilitySlot.NormalAttack,"b",out _,out _,out _));
        Equal(0,b.Statuses.GetStatuses("b").Count); var hit=StatusHit(b); Equal(33,hit.HealthChanged);
        Equal(1,b.Statuses.GetStatuses("b").Single().Stacks);
        Equal(false,b.Battlefield.TryApplyEffect(b.CurrentActivation.ActivationId,AbilitySlot.NormalAttack,"b",out _,out _,out _));
        Equal(1,b.Statuses.GetStatuses("b").Single().Stacks);
    }
    private static void StatusFailedHit()
    {
        var b=StatusBattle(PassiveTrigger.OwnerSurvivedDamage,owner:"b");
        Equal(false,b.Battlefield.TryApplyEffect(999,AbilitySlot.NormalAttack,"b",out _,out _,out _));
        var other=new BattleTurnController(new[] { U("a"),U("b") },new DamageRules(100,1,false));
        foreach(string id in new[] { "a","b" }) { other.Health.RegisterHealth(id,new UnitHealthDefinition(id,100,0)); other.Abilities.RegisterKit(id,new UnitAbilityDefinition(1,SignatureReadiness.ReadyAtDeployment)); }
        other.Statuses.RegisterStatus(new StatusDefinition("effect",1000,0,1)); other.Statuses.RegisterPassive("b",new PassiveDefinition(PassiveTrigger.OwnerSurvivedDamage,"effect"));
        other.StartNextRound(); other.BeginNextActivation();
        Equal(true,other.Health.TryApply(other.CurrentActivation.ActivationId,new HealthAction(AbilitySlot.NormalAttack,HealthEffectKind.Damage,"b",10,TargetingVerdict.Legal,new DamageMitigation(1,0)),out _,out _));
        Equal(0,other.Statuses.GetStatuses("b").Count); Equal(0,b.Statuses.GetStatuses("b").Count);
    }
    private static void StatusLethal()
    {
        var b=StatusBattle(PassiveTrigger.OwnerSurvivedDamage,owner:"b"); b.Statuses.Apply("b","effect");
        StatusHit(b,AbilitySlot.Signature); Equal(0,b.Statuses.GetStatuses("b").Count); Equal(false,b.Statuses.Apply("b","effect"));
    }
    private static void StatusDuration()
    {
        var b=StatusBattle(duration:1); b.Statuses.Apply("c","effect");
        b.EndActivation(b.CurrentActivation.ActivationId); b.BeginNextActivation(); Equal(1,b.Statuses.GetStatuses("c").Single().RemainingOwnerActivations);
        b.EndActivation(b.CurrentActivation.ActivationId); b.BeginNextActivation(); Equal(1,b.Statuses.GetStatuses("c").Count);
        b.EndActivation(b.CurrentActivation.ActivationId); Equal(0,b.Statuses.GetStatuses("c").Count);
    }
    private static void StatusRefresh()
    {
        var b=StatusBattle(stacking:StatusStacking.Refresh,max:1); b.Statuses.Apply("a","effect"); StatusNextRound(b);
        Equal(1,b.Statuses.GetStatuses("a").Single().RemainingOwnerActivations); b.Statuses.Apply("a","effect");
        Equal(2,b.Statuses.GetStatuses("a").Single().RemainingOwnerActivations); Equal(1,b.Statuses.GetStatuses("a").Single().Stacks);
    }
    private static void StatusStacks()
    {
        var b=StatusBattle(); for(int i=0;i<12;i++) b.Statuses.Apply("a","effect"); Equal(3,b.Statuses.GetStatuses("a").Single().Stacks);
        Equal(130,b.Statuses.EffectivePower("a",100)); StatusNextRound(b); b.Statuses.Apply("a","effect"); Equal(2,b.Statuses.GetStatuses("a").Single().RemainingOwnerActivations);
    }
    private static void StatusBounds()
    {
        var b=StatusBattle(power:5000,start:false); b.Statuses.RegisterStatus(new StatusDefinition("negative",0,-5000,2,10,StatusStacking.AddStackAndRefresh));
        for(int i=0;i<3;i++) b.Statuses.Apply("a","effect"); Equal(150,b.Statuses.EffectivePower("a",100));
        b.Statuses.Apply("a","negative"); Equal(150,b.Statuses.EffectivePower("a",100));
        for(int i=0;i<9;i++) b.Statuses.Apply("a","negative"); Equal(50,b.Statuses.EffectivePower("a",100)); Equal(1,b.Statuses.EffectivePower("a",1));
        b.Statuses.Remove("a","negative"); Equal(int.MaxValue,b.Statuses.EffectivePower("a",int.MaxValue));
    }
    private static void StatusArmor()
    {
        var b=StatusBattle(armor:5000,power:0); b.Statuses.Apply("b","effect");
        Equal(true,b.Battlefield.TryPreviewEffect(b.CurrentActivation.ActivationId,AbilitySlot.NormalAttack,"b",out var p,out _,out _));
        Equal(31,p.ResolvedAmount); Equal(p.ResolvedAmount,StatusHit(b).ResolvedAmount); Equal(20,b.Health.GetState("b").Armor);
    }
    private static void StatusHealing()
    {
        var b=StatusBattle(power:5000); b.Statuses.Apply("a","effect");
        Equal(true,b.Battlefield.TryApplyEffect(b.CurrentActivation.ActivationId,AbilitySlot.Main,"c",out var p,out _,out _));
        Equal(45,p.HealthChanged); Equal(95,b.Health.GetState("c").CurrentHealth);
    }
    private static void StatusRemove()
    {
        var b=StatusBattle(); for(int i=0;i<5;i++) { b.Statuses.Apply("a","effect"); Equal(45,b.Statuses.EffectivePower("a",41)); b.Statuses.Remove("a","effect"); Equal(41,b.Statuses.EffectivePower("a",41)); }
        Equal(false,b.Statuses.Remove("a","effect"));
    }
    private static void StatusSave()
    {
        var b=StatusBattle(PassiveTrigger.OwnerActivationStarted); b.Statuses.Apply("a","effect"); var r=Reload(b); SameSave(b,r);
        StatusNextRound(b); StatusNextRound(r); SameSave(b,r); Equal(3,r.Statuses.GetStatuses("a").Single().Stacks);
    }
    private static void StatusReload()
    {
        var b=StatusBattle(PassiveTrigger.OwnerSurvivedDamage,owner:"b"); StatusHit(b); for(int i=0;i<5;i++) b=Reload(b);
        Equal(1,b.Statuses.GetStatuses("b").Single().Stacks); Equal(false,b.CurrentActivation.PrimaryActionAvailable);
        var a=StatusBattle(PassiveTrigger.OwnerActivationStarted); for(int i=0;i<5;i++) a=Reload(a); Equal(1,a.Statuses.GetStatuses("a").Single().Stacks);
    }
    private static void StatusLegacy()
    {
        var b=HealthBattle(); var bytes=BattleSave.Capture(b,SaveRevision);
        // Schema 1 is identical through Mission, without the three empty effect collections.
        bytes=bytes.Take(bytes.Length-44).Concat(new byte[32]).ToArray(); Array.Copy(BitConverter.GetBytes(1),0,bytes,4,4); Rehash(bytes);
        var restored=BattleSave.Restore(bytes,SaveRevision); Equal(0,restored.Statuses.GetStatuses("a").Count); SameSave(b,restored);
    }
    private static void StatusCorrupt()
    {
        var bytes=BattleSave.Capture(StatusBattle(PassiveTrigger.OwnerActivationStarted),SaveRevision);
        Array.Copy(BitConverter.GetBytes(0),0,bytes,bytes.Length-36,4); Rehash(bytes);
        Throws<InvalidDataException>(()=>BattleSave.Restore(bytes,SaveRevision));
    }
    private static void StatusInterrupted()
    {
        var b=StatusBattle(PassiveTrigger.OwnerSurvivedDamage,owner:"b"); var files=new FakeSaveFiles(); var store=new LocalBattleStore(files,SaveRevision); store.Save(b);
        StatusHit(b); files.Fault=2; Throws<IOException>(()=>store.Save(b)); files.Fault=0; var r=store.Load().Battle;
        Equal(0,r.Statuses.GetStatuses("b").Count); Equal(100,r.Health.GetState("b").CurrentHealth); Equal(true,r.CurrentActivation.PrimaryActionAvailable);
        StatusHit(r); store.Save(r); Equal(1,store.Load().Battle.Statuses.GetStatuses("b").Single().Stacks);
    }
    private static void StatusAmbiguous()
    {
        var b=StatusBattle(PassiveTrigger.OwnerSurvivedDamage,owner:"b"); var files=new FakeSaveFiles(); var store=new LocalBattleStore(files,SaveRevision); store.Save(b);
        StatusHit(b); files.Fault=3; Throws<IOException>(()=>store.Save(b)); files.Fault=0; var r=store.Load().Battle;
        Equal(1,r.Statuses.GetStatuses("b").Single().Stacks); Equal(false,r.CurrentActivation.PrimaryActionAvailable);
    }
    private static void StatusCatalog()
    {
        var c=AbstractContentPackage.Create(true); Equal(false,c.ContainsPassivePlaceholders);
        Throws<ArgumentException>(()=>new ContentCatalog(c.Manifest,c.Resources,c.Abilities.Values,c.Kits.Values,c.Units.Values,c.Missions.Values,c.Rewards.Values,c.Campaigns,c.StarterUnits));
    }
    private static void StatusFlow()
    {
        var c=AbstractContentPackage.Create(true); var p=new ProgressFiles(); var files=new FakeSaveFiles(); var f=c.CreateFlow(files,p,"p"); f.CreateProfile(c.StarterUnits);
        f.Start("fixture-opening",new[] { "fixture-a-1" }); Equal(1,f.ReadBattle().Statuses.GetStatuses("fixture-a-1").Single().Stacks);
        f.Open(); Equal(1,f.ReadBattle().Statuses.GetStatuses("fixture-a-1").Single().Stacks); WinFlow(f); f.ClaimRewards();
        Equal(1,f.Progress.Claims.Count);
    }
    private static void StatusLifecycle()
    {
        var b=StatusBattle(); Throws<InvalidOperationException>(()=>b.Statuses.RegisterStatus(new StatusDefinition("late",1,0,1)));
        Throws<ArgumentException>(()=>b.Statuses.Apply("a","missing")); b.Statuses.Apply("a","effect"); b.RemoveUnit("a"); Equal(0,b.Statuses.GetStatuses("a").Count);
        b.EndBattle(); Throws<InvalidOperationException>(()=>b.Statuses.Apply("b","effect")); Throws<InvalidOperationException>(()=>b.Statuses.Remove("b","effect"));
        Throws<ArgumentException>(()=>new StatusDefinition("invalid",1,0,1,2,StatusStacking.Refresh));
    }
    private static void StatusViews()
    {
        var b=StatusBattle(); b.Statuses.Apply("a","effect"); var before=b.Statuses.GetStatuses("a"); b.Statuses.Apply("a","effect"); Equal(1,before.Single().Stacks);
        Throws<NotSupportedException>(()=>((System.Collections.Generic.IList<StatusView>)before).Clear());
    }
    private static void StatusEnemy()
    {
        var b=StatusBattle(start:false); b.Battlefield.RegisterEnemy("b",new EnemyBehavior(EnemyStyle.Aggressive,6));
        b.Statuses.Apply("b","effect"); b.StartNextRound(); b.BeginNextActivation(); b.EndActivation(b.CurrentActivation.ActivationId); b.BeginNextActivation();
        var saved=BattleSave.Capture(b,SaveRevision);
        Equal(true,b.Battlefield.TryPlanEnemyTurn(b.CurrentActivation.ActivationId,new[] { "a" },out var plan,out _));
        Equal(true,saved.SequenceEqual(BattleSave.Capture(b,SaveRevision))); Equal(true,plan.Effect.ResolvedAmount>0);
        Equal(true,b.Battlefield.TryRunEnemyTurn(b.CurrentActivation.ActivationId,new[] { "a" },out var result,out _)); Equal(plan.Effect.ResolvedAmount,result.Effect.ResolvedAmount);
    }
}
