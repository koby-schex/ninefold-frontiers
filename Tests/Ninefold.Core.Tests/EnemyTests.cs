using System;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;

internal static partial class Program
{
    private static (string Name, Action Run)[] EnemyTests() => new (string, Action)[]
    {
        ("Enemy plans are pure and execution finishes one activation", EnemyPreview),
        ("Aggressive enemy moves into attack range using legal route", EnemyAdvanceAttack),
        ("Aggressive enemy approaches when no attack is reachable", EnemyApproach),
        ("Defensive enemy holds when no in-area attack exists", EnemyGuard),
        ("Defensive route never leaves its guard volume", EnemyGuardRoute),
        ("Defensive injured enemy prioritizes useful self healing", EnemyHeal),
        ("Aggressive injured enemy prioritizes legal damage", EnemyAggressiveHeal),
        ("Enemy main cooldown uses ordinary owner turns", EnemyCooldown),
        ("Enemy Signature readiness and once-only limit stay enforced", EnemySignature),
        ("Blocked zero-movement enemy ends without attacking through wall", EnemyBlocked),
        ("Unknown removed allied and unperceived actors are not targets", EnemyPerception),
        ("Invalid invocation preserves the active player turn", EnemyInvalidCalls),
        ("Spent action never grants an enemy second attack", EnemySpentAction),
        ("Equal target choices ignore supplied order", EnemyTies),
        ("Search-limit failure does not stall enemy activation", EnemySearchLimit),
        ("Successful enemy attack triggers terminal mission outcome", EnemyMissionOutcome),
        ("Complete multi-round encounter respects mission round gate", EnemyEncounter),
        ("Enemy execution replans after a target is removed", EnemyReplan),
        ("Enemy setup cannot be replaced or attached late", EnemySetup),
        ("Invalid behavior and excessive perception are rejected", EnemyInvalidDefinitions)
    };
    private static BattleTurnController EnemyBattle(EnemyStyle style = EnemyStyle.Aggressive, decimal movement = 5,
        decimal range = 2, int actorHp = 100, int targetHp = 500, FieldAbility[] abilities = null,
        SignatureReadiness readiness = SignatureReadiness.AfterNormalAttack, FieldObstacle[] walls = null,
        FieldBox guard = null, int nodes = 64, bool start = true, MissionDefinition mission = null)
    {
        var b = B(U("a",30,movement),U("b",20),U("c",10));
        foreach (string id in new[] { "a","b","c" })
        {
            b.Abilities.RegisterKit(id,new UnitAbilityDefinition(2,readiness));
            b.Health.RegisterHealth(id,new UnitHealthDefinition(id == "a" ? "enemy" : "player",500,0),id == "a" ? actorHp : targetHp);
        }
        var field = b.ConfigureBattlefield(new BattlefieldMap(Box(-20,-20,20,20,20),walls ?? Array.Empty<FieldObstacle>()));
        field.Register("a",P(0,0),Body(),abilities ?? new[] { new FieldAbility(AbilitySlot.NormalAttack,HealthEffectKind.Damage,40,range,true) },new[] { "a" });
        field.Register("b",P(6,0),Body(),Profiles()); field.Register("c",P(0,6),Body(),Profiles());
        field.RegisterEnemy("a",new EnemyBehavior(style,1,guard ?? (style == EnemyStyle.Defensive ? Box(-2,-2,2,2) : null),nodes));
        if (mission != null) b.ConfigureMission(mission);
        if (start) { b.StartNextRound(); b.BeginNextActivation(); }
        return b;
    }
    private static EnemyTurnResult RunEnemy(BattleTurnController b, params string[] targets)
    {
        Equal(true,b.Battlefield.TryRunEnemyTurn(b.CurrentActivation.ActivationId,targets,out var result,out var failure));
        Equal(EnemyTurnFailure.None,failure); Equal<ActivationView>(null,b.CurrentActivation); return result;
    }
    private static void AdvanceEnemyRound(BattleTurnController b)
    { Drain(b); b.Mission?.ResolveRoundEnd(b.RoundNumber); b.StartNextRound(); b.BeginNextActivation(); }
    private static void EnemyPreview()
    {
        var b = EnemyBattle(); var turn = b.CurrentActivation;
        Equal(true,b.Battlefield.TryPlanEnemyTurn(turn.ActivationId,new[] { "b" },out var plan,out _));
        Equal(P(0,0),b.Battlefield.GetPosition("a")); Equal(500,b.Health.GetState("b").CurrentHealth);
        Equal(5m,b.CurrentActivation.MovementRemaining); Equal(true,b.CurrentActivation.PrimaryActionAvailable);
        Equal(false,b.Abilities.GetState("a").SignatureRequirementMet);
        var result = RunEnemy(b,"b"); Equal(plan.Effect.HealthChanged,result.Effect.HealthChanged);
        Equal(1L,b.GetActivationCount("a")); Equal("b",b.BeginNextActivation().UnitId);
    }
    private static void EnemyAdvanceAttack()
    {
        var b = EnemyBattle(); var r = RunEnemy(b,"b"); Equal(40,r.Effect.HealthChanged);
        Equal(true,r.Movement.Cost <= 5m); Equal(460,b.Health.GetState("b").CurrentHealth);
        Equal(AbilitySlot.NormalAttack,r.UsedSlot.Value);
    }
    private static void EnemyApproach()
    {
        var b = EnemyBattle(movement:1,range:.1m); var r = RunEnemy(b,"b");
        Equal<HealthEffectPreview>(null,r.Effect); Equal(true,r.Movement.Cost <= 1m);
        var position = b.Battlefield.GetPosition("a");
        Equal(true,(position.X-6m)*(position.X-6m)+position.Z*position.Z < 36m);
    }
    private static void EnemyGuard()
    {
        var b = EnemyBattle(style:EnemyStyle.Defensive,range:1); var r = RunEnemy(b,"b");
        Equal<HealthEffectPreview>(null,r.Effect); Equal<MovementPreview>(null,r.Movement); Equal(P(0,0),b.Battlefield.GetPosition("a"));
    }
    private static void EnemyGuardRoute()
    {
        var guard = Box(-1,-1,5.5m,1); var b = EnemyBattle(style:EnemyStyle.Defensive,guard:guard,walls:new[] { Wall() });
        var r = RunEnemy(b,"b"); Equal<MovementPreview>(null,r.Movement); Equal<HealthEffectPreview>(null,r.Effect);
    }
    private static FieldAbility[] HealKit() => new[]
    {
        new FieldAbility(AbilitySlot.NormalAttack,HealthEffectKind.Damage,40,10,true),
        new FieldAbility(AbilitySlot.Main,HealthEffectKind.Healing,30,10,false)
    };
    private static void EnemyHeal()
    {
        var b = EnemyBattle(style:EnemyStyle.Defensive,abilities:HealKit(),actorHp:480); var r = RunEnemy(b,"b");
        Equal(HealthEffectKind.Healing,r.Effect.Kind); Equal(20,r.Effect.HealthChanged); Equal(500,b.Health.GetState("a").CurrentHealth);
        Equal(500,b.Health.GetState("b").CurrentHealth); Equal(2,b.Abilities.GetState("a").MainCooldownRemaining);
        AdvanceEnemyRound(b); Equal(HealthEffectKind.Damage,RunEnemy(b,"b").Effect.Kind);
    }
    private static void EnemyAggressiveHeal()
    {
        var b = EnemyBattle(abilities:HealKit(),actorHp:480); var r = RunEnemy(b,"b");
        Equal(HealthEffectKind.Damage,r.Effect.Kind); Equal(480,b.Health.GetState("a").CurrentHealth);
    }
    private static void EnemyCooldown()
    {
        var b = EnemyBattle(movement:0,abilities:new[] { new FieldAbility(AbilitySlot.NormalAttack,HealthEffectKind.Damage,20,10,true),new FieldAbility(AbilitySlot.Main,HealthEffectKind.Damage,50,10,true) });
        Equal(AbilitySlot.Main,RunEnemy(b,"b").UsedSlot.Value); AdvanceEnemyRound(b);
        Equal(AbilitySlot.NormalAttack,RunEnemy(b,"b").UsedSlot.Value); AdvanceEnemyRound(b);
        Equal(AbilitySlot.Main,RunEnemy(b,"b").UsedSlot.Value);
    }
    private static void EnemySignature()
    {
        var b = EnemyBattle(movement:0,abilities:new[] { new FieldAbility(AbilitySlot.NormalAttack,HealthEffectKind.Damage,20,10,true),new FieldAbility(AbilitySlot.Signature,HealthEffectKind.Damage,60,10,true) });
        Equal(AbilitySlot.NormalAttack,RunEnemy(b,"b").UsedSlot.Value); AdvanceEnemyRound(b);
        Equal(AbilitySlot.Signature,RunEnemy(b,"b").UsedSlot.Value); AdvanceEnemyRound(b);
        Equal(AbilitySlot.NormalAttack,RunEnemy(b,"b").UsedSlot.Value); Equal(true,b.Abilities.GetState("a").SignatureUsed);
    }
    private static void EnemyBlocked()
    {
        var b = EnemyBattle(movement:0,range:10,walls:new[] { Wall() }); var r = RunEnemy(b,"b");
        Equal<HealthEffectPreview>(null,r.Effect); Equal(500,b.Health.GetState("b").CurrentHealth); Equal<MovementPreview>(null,r.Movement);
    }
    private static void EnemyPerception()
    {
        var b = EnemyBattle(range:10); var r = RunEnemy(b); Equal<HealthEffectPreview>(null,r.Effect); Equal<MovementPreview>(null,r.Movement);
        AdvanceEnemyRound(b); b.RemoveUnit("b"); r = RunEnemy(b,"unknown","a","b"); Equal<HealthEffectPreview>(null,r.Effect);
        Equal(500,b.Health.GetState("c").CurrentHealth);
    }
    private static void EnemyInvalidCalls()
    {
        var b = EnemyBattle(); Equal(false,b.Battlefield.TryRunEnemyTurn(999,new[] { "b" },out _,out var failure));
        Equal(EnemyTurnFailure.InactiveTurn,failure); Equal("a",b.CurrentActivation.UnitId);
        b.EndActivation(1); var player = b.BeginNextActivation();
        Equal(false,b.Battlefield.TryRunEnemyTurn(player.ActivationId,new[] { "a" },out _,out failure)); Equal(EnemyTurnFailure.UnregisteredActor,failure);
        Equal(player.ActivationId,b.CurrentActivation.ActivationId); Equal(true,b.CurrentActivation.PrimaryActionAvailable);
    }
    private static void EnemySpentAction()
    {
        var b = EnemyBattle(range:10); b.SpendPrimaryAction(1); var r = RunEnemy(b,"b");
        Equal<HealthEffectPreview>(null,r.Effect); Equal<MovementPreview>(null,r.Movement); Equal(500,b.Health.GetState("b").CurrentHealth);
    }
    private static void EnemyTies()
    {
        var a = EnemyBattle(range:10,movement:0); var b = EnemyBattle(range:10,movement:0);
        Equal("b",RunEnemy(a,"c","b").Effect.TargetId); Equal("b",RunEnemy(b,"b","c").Effect.TargetId);
    }
    private static void EnemySearchLimit()
    {
        var b = EnemyBattle(walls:new[] { Wall() },nodes:2); RunEnemy(b,"b"); Equal<ActivationView>(null,b.CurrentActivation);
    }
    private static MissionDefinition EnemyMission() => new MissionDefinition("abstract","attempt",new[] { "b" },ObjectiveDefinition.Defeat("primary",new[] { "a" }),OutcomePriority.FailureFirst);
    private static void EnemyMissionOutcome()
    {
        var b = EnemyBattle(targetHp:20,range:10,mission:EnemyMission()); var r = RunEnemy(b,"b");
        Equal(true,r.BattleEnded); Equal(MissionOutcome.Defeat,b.Mission.Result.Outcome); Equal(true,b.Health.GetState("b").IsDefeated);
        Equal(false,b.Battlefield.TryRunEnemyTurn(1,new[] { "c" },out _,out _));
    }
    private static void EnemyEncounter()
    {
        var b = EnemyBattle(targetHp:80,range:10,mission:EnemyMission()); RunEnemy(b,"b");
        Equal<BattleResult>(null,b.Mission.Result); AdvanceEnemyRound(b); RunEnemy(b,"b");
        Equal(MissionOutcome.Defeat,b.Mission.Result.Outcome); Equal(2,b.Mission.Result.Round);
        Equal(2L,b.GetActivationCount("a")); Equal(80,b.Health.GetState("c").CurrentHealth);
    }
    private static void EnemyReplan()
    {
        var b = EnemyBattle(range:10); b.Battlefield.TryPlanEnemyTurn(1,new[] { "b" },out var plan,out _);
        Equal("b",plan.TargetId); b.RemoveUnit("b"); var r = RunEnemy(b,"b"); Equal<HealthEffectPreview>(null,r.Effect);
    }
    private static void EnemySetup()
    {
        var b = EnemyBattle(start:false); Throws<InvalidOperationException>(() => b.Battlefield.RegisterEnemy("a",new EnemyBehavior(EnemyStyle.Aggressive,1)));
        b.StartNextRound(); b.BeginNextActivation(); b.EndActivation(1); b.BeginNextActivation();
        Throws<InvalidOperationException>(() => b.Battlefield.RegisterEnemy("b",new EnemyBehavior(EnemyStyle.Aggressive,1)));
    }
    private static void EnemyInvalidDefinitions()
    {
        Throws<ArgumentNullException>(() => new EnemyBehavior(EnemyStyle.Defensive,1));
        Throws<ArgumentOutOfRangeException>(() => new EnemyBehavior(EnemyStyle.Aggressive,0));
        Throws<ArgumentOutOfRangeException>(() => new EnemyBehavior(EnemyStyle.Aggressive,1,pathNodeLimit:1));
        var b = EnemyBattle(); Equal(false,b.Battlefield.TryRunEnemyTurn(1,Enumerable.Repeat("b",9),out _,out var f)); Equal(EnemyTurnFailure.InvalidPerception,f);
        Equal(false,b.Battlefield.TryRunEnemyTurn(1,null,out _,out f)); Equal(EnemyTurnFailure.InvalidPerception,f);
        Equal(true,b.CurrentActivation.PrimaryActionAvailable);
    }
}
