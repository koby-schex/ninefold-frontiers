using System;
using System.Linq;
using Ninefold.Core.Combat;

internal static partial class Program
{
    private static (string Name,Action Run)[] SupportEnemyTests() => new (string,Action)[] {
        ("Support AI heals a perceived compatible wounded ally", SupportHeal),
        ("Support AI cannot discover unperceived allies", SupportPerception),
        ("Support AI filters wrong-team removed and unknown allies", SupportFilter),
        ("Support AI rejects invalid ally perception without changing state", SupportInvalid),
        ("Support AI applies standalone ally buffs through normal costs", SupportBuff),
        ("Support AI applies useful enemy debuffs", SupportDebuff),
        ("Support AI skips capped full-duration effects", SupportNoRefresh),
        ("Support AI can add stacks or refresh expiring effects", SupportRefresh),
        ("Support AI skips zero-stat and aggregate-cap waste", SupportCaps),
        ("Support AI avoids self effects expiring at activation end", SupportSelfExpiry),
        ("Support AI skips harmful allied or beneficial hostile status", SupportWrongSign),
        ("Support AI chooses highest useful target independent of input order", SupportTies),
        ("Support AI observes compatibility range and obstruction", SupportTargeting),
        ("Support AI moves into support range within guard bounds", SupportMovement),
        ("Support AI preserves aggressive and defensive priorities", SupportPriorities),
        ("Support AI plans are pure and identical after reload", SupportSave),
        ("Support AI replans after ally state changes", SupportReplan),
        ("Support AI obeys main cooldown and spent action", SupportCooldown),
        ("Support AI Signature remains spent across resume", SupportSignature),
        ("Support AI compound preview exposes health and attached status", SupportCompound)
    };
    private static BattleTurnController SupportBattle(FieldAbility ability=null, StatusDefinition status=null,
        EnemyStyle style=EnemyStyle.Defensive,int bHealth=50,int cHealth=80, bool compatible=true,
        decimal movement=0, FieldObstacle[] walls=null, FieldBox guard=null,bool attack=false,int armor=20)
    {
        var b=B(U("a",40,movement),U("b",30),U("c",20),U("d",10));
        foreach(string id in new[] { "a","b","c","d" })
        {
            b.Health.RegisterHealth(id,new UnitHealthDefinition(id=="d"?"opponent":"ally",100,armor),id=="b"?bHealth:id=="c"?cHealth:100);
            b.Abilities.RegisterKit(id,new UnitAbilityDefinition(2,SignatureReadiness.ReadyAtDeployment));
        }
        b.Statuses.RegisterStatus(status??new StatusDefinition("support",1000,0,3,2,StatusStacking.AddStackAndRefresh));
        b.Statuses.RegisterStatus(new StatusDefinition("cap",5000,5000,3));
        var field=b.ConfigureBattlefield(new BattlefieldMap(Box(-20,-20,20,20),walls??Array.Empty<FieldObstacle>()));
        var profile=ability??new FieldAbility(AbilitySlot.Main,HealthEffectKind.Healing,30,10,false,target:AbilityTarget.Ally);
        var profiles=attack?new[] { profile,new FieldAbility(AbilitySlot.NormalAttack,HealthEffectKind.Damage,40,10,true) }:new[] { profile };
        field.Register("a",P(0,0),Body(),profiles,compatible?new[] { "a","b","c" }:Array.Empty<string>());
        field.Register("b",P(4,0),Body(),Profiles(),new[] { "a","b","c" }); field.Register("c",P(0,4),Body(),Profiles(),new[] { "a","b","c" }); field.Register("d",P(6,6),Body(),Profiles(),new[] { "d" });
        field.RegisterEnemy("a",new EnemyBehavior(style,1,guard??(style==EnemyStyle.Defensive?Box(-10,-10,10,10):null)));
        b.StartNextRound(); b.BeginNextActivation(); return b;
    }
    private static FieldAbility Buff(AbilityTarget target=AbilityTarget.Ally,AbilitySlot slot=AbilitySlot.Main,decimal range=10)
        => new FieldAbility(slot,"support",target,range);
    private static EnemyPlan SupportPlan(BattleTurnController b,string[] allies=null,string[] opponents=null)
    {
        Equal(true,b.Battlefield.TryPlanEnemyTurn(b.CurrentActivation.ActivationId,opponents??new[] { "d" },allies??new[] { "b","c" },out var plan,out _)); return plan;
    }
    private static EnemyTurnResult SupportRun(BattleTurnController b,string[] allies=null,string[] opponents=null)
    {
        Equal(true,b.Battlefield.TryRunEnemyTurn(b.CurrentActivation.ActivationId,opponents??new[] { "d" },allies??new[] { "b","c" },out var result,out _)); return result;
    }
    private static void SupportHeal()
    { var b=SupportBattle(); Equal("b",SupportPlan(b).TargetId); var r=SupportRun(b); Equal(30,r.Effect.HealthChanged); Equal(80,b.Health.GetState("b").CurrentHealth); Equal(AbilitySlot.Main,r.UsedSlot.Value); Equal<ActivationView>(null,b.CurrentActivation); }
    private static void SupportPerception()
    {
        var b=SupportBattle(); Equal<AbilitySlot?>(null,SupportPlan(b,Array.Empty<string>()).Slot);
        Equal(true,b.Battlefield.TryPlanEnemyTurn(b.CurrentActivation.ActivationId,new[] { "d" },out var legacy,out _)); Equal<AbilitySlot?>(null,legacy.Slot);
        Equal("c",SupportPlan(b,new[] { "c" }).TargetId);
    }
    private static void SupportFilter()
    {
        var b=SupportBattle(); b.RemoveUnit("b"); Equal("c",SupportPlan(b,new[] { "missing","b","d","c" }).TargetId);
        Equal<AbilitySlot?>(null,SupportPlan(b,new[] { "missing","b","d" }).Slot);
    }
    private static void SupportInvalid()
    {
        var b=SupportBattle(); var bytes=Ninefold.Core.Persistence.BattleSave.Capture(b,SaveRevision);
        foreach(var allies in new[] { null,new[] { " " },Enumerable.Repeat("b",9).ToArray() })
        { Equal(false,b.Battlefield.TryPlanEnemyTurn(b.CurrentActivation.ActivationId,new[] { "d" },allies,out _,out var f)); Equal(EnemyTurnFailure.InvalidPerception,f); }
        Equal(true,bytes.SequenceEqual(Ninefold.Core.Persistence.BattleSave.Capture(b,SaveRevision)));
    }
    private static void SupportBuff()
    {
        var b=SupportBattle(Buff()); var p=SupportPlan(b); Equal("b",p.TargetId); Equal<HealthEffectPreview>(null,p.Effect); Equal(true,p.AbilityEffect.Status.Applies);
        var r=SupportRun(b); Equal(1,r.AbilityEffect.Status.StacksAfter); Equal(1,b.Statuses.GetStatuses("b").Single().Stacks); Equal(2,b.Abilities.GetState("a").MainCooldownRemaining);
    }
    private static void SupportDebuff()
    { var b=SupportBattle(Buff(AbilityTarget.Enemy),new StatusDefinition("support",0,-1000,3)); Equal("d",SupportPlan(b).TargetId); SupportRun(b); Equal(1,b.Statuses.GetStatuses("d").Count); }
    private static void SupportNoRefresh()
    {
        var b=SupportBattle(Buff()); foreach(string id in new[] { "b","c" }) { b.Statuses.Apply(id,"support"); b.Statuses.Apply(id,"support"); }
        Equal<AbilitySlot?>(null,SupportPlan(b).Slot); var r=SupportRun(b); Equal<AbilitySlot?>(null,r.UsedSlot); Equal(0,b.Abilities.GetState("a").MainCooldownRemaining);
    }
    private static void SupportRefresh()
    {
        var b=SupportBattle(Buff()); b.Statuses.Apply("b","support"); Equal("b",SupportPlan(b,new[] { "b" }).TargetId); SupportRun(b,new[] { "b" });
        AdvanceEnemyRound(b); // Ally activation reduced remaining duration. Main is cooling down.
        SupportRun(b); AdvanceEnemyRound(b); Equal("b",SupportPlan(b,new[] { "b" }).TargetId); SupportRun(b,new[] { "b" }); Equal(3,b.Statuses.GetStatuses("b").Single().RemainingOwnerActivations);
    }
    private static void SupportCaps()
    {
        var zero=SupportBattle(Buff(),armor:0); Equal<AbilitySlot?>(null,SupportPlan(zero).Slot);
        var capped=SupportBattle(Buff()); capped.Statuses.Apply("b","cap"); capped.Statuses.Apply("c","cap"); Equal<AbilitySlot?>(null,SupportPlan(capped).Slot);
    }
    private static void SupportSelfExpiry()
    {
        var b=SupportBattle(Buff(AbilityTarget.Self),new StatusDefinition("support",1000,0,1)); Equal<AbilitySlot?>(null,SupportPlan(b).Slot);
        var useful=SupportBattle(Buff(AbilityTarget.Self),new StatusDefinition("support",1000,0,2)); Equal("a",SupportPlan(useful).TargetId); SupportRun(useful); Equal(1,useful.Statuses.GetStatuses("a").Single().RemainingOwnerActivations);
    }
    private static void SupportWrongSign()
    {
        var harm=SupportBattle(Buff(),new StatusDefinition("support",-1000,0,2)); Equal<AbilitySlot?>(null,SupportPlan(harm).Slot);
        var help=SupportBattle(Buff(AbilityTarget.Enemy)); Equal<AbilitySlot?>(null,SupportPlan(help).Slot);
    }
    private static void SupportTies()
    {
        var b=SupportBattle(bHealth:50,cHealth:50); Equal("b",SupportPlan(b,new[] { "c","b" }).TargetId); Equal("b",SupportPlan(b,new[] { "b","c","b" }).TargetId);
        var different=SupportBattle(bHealth:90,cHealth:50); Equal("c",SupportPlan(different).TargetId);
    }
    private static void SupportTargeting()
    {
        var incompatible=SupportBattle(compatible:false); Equal<AbilitySlot?>(null,SupportPlan(incompatible).Slot);
        var shortRange=SupportBattle(Buff(range:1)); Equal<AbilitySlot?>(null,SupportPlan(shortRange).Slot);
        var blocked=SupportBattle(walls:new[] { new FieldObstacle(Box(1,-1,2,1),false,true) }); Equal("c",SupportPlan(blocked).TargetId);
    }
    private static void SupportMovement()
    {
        var b=SupportBattle(Buff(range:1),movement:5); var p=SupportPlan(b,new[] { "b" }); Equal("b",p.TargetId); Equal(true,p.Movement.Cost<=5); SupportRun(b,new[] { "b" }); Equal(1,b.Statuses.GetStatuses("b").Count);
        var guard=SupportBattle(Buff(range:1),movement:5,guard:Box(-1,-1,1,1)); Equal<AbilitySlot?>(null,SupportPlan(guard).Slot);
    }
    private static void SupportPriorities()
    {
        var defense=SupportBattle(attack:true); Equal("b",SupportPlan(defense).TargetId);
        var offense=SupportBattle(attack:true,style:EnemyStyle.Aggressive); Equal("d",SupportPlan(offense).TargetId);
    }
    private static void SupportSave()
    {
        var b=SupportBattle(Buff()); var r=Reload(b); var p=SupportPlan(b); SameSave(b,r); var q=SupportPlan(r,new[] { "c","b" }); Equal(p.TargetId,q.TargetId); Equal(p.AbilityEffect.Status.StacksAfter,q.AbilityEffect.Status.StacksAfter);
        SupportRun(b); SupportRun(r); SameSave(b,r); var resume=Reload(r); Equal(1,resume.Statuses.GetStatuses("b").Single().Stacks);
    }
    private static void SupportReplan()
    {
        var b=SupportBattle(Buff()); Equal("b",SupportPlan(b).TargetId); b.Statuses.Apply("b","support"); b.Statuses.Apply("b","support"); var r=SupportRun(b); Equal("c",r.AbilityEffect.TargetId);
    }
    private static void SupportCooldown()
    {
        var b=SupportBattle(Buff()); b.SpendPrimaryAction(b.CurrentActivation.ActivationId); Equal<AbilitySlot?>(null,SupportPlan(b).Slot); SupportRun(b);
        AdvanceEnemyRound(b); SupportRun(b); AdvanceEnemyRound(b); Equal<AbilitySlot?>(null,SupportPlan(b).Slot); Equal(1,b.Abilities.GetState("a").MainCooldownRemaining);
    }
    private static void SupportSignature()
    {
        var b=SupportBattle(Buff(slot:AbilitySlot.Signature)); SupportRun(b); b=Reload(b); AdvanceEnemyRound(b); Equal<AbilitySlot?>(null,SupportPlan(b).Slot); Equal(true,b.Abilities.GetState("a").SignatureUsed);
    }
    private static void SupportCompound()
    {
        var b=SupportBattle(new FieldAbility(AbilitySlot.Main,HealthEffectKind.Healing,30,10,false,"support",AbilityTarget.Ally)); var p=SupportPlan(b); Equal(30,p.Effect.HealthChanged); Equal(1,p.AbilityEffect.Status.StacksAfter);
        var r=SupportRun(b); Equal(p.Effect.HealthChanged,r.Effect.HealthChanged); Equal(p.AbilityEffect.Status.StacksAfter,r.AbilityEffect.Status.StacksAfter);
    }
}
