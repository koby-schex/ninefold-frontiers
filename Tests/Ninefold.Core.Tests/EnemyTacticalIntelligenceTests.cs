using System;
using Ninefold.Core.Combat;

internal static partial class Program
{
    private static (string Name,Action Run)[] EnemyTacticalIntelligenceTests() =>
        new (string,Action)[] {
            ("AI prefers greater offensive threat when attacks deal equal impact", EnemyThreatPriority),
            ("AI lethal opportunity overrides threat tie-break", EnemyLethalBeforeThreat),
            ("Threat-aware planning remains pure", EnemyThreatPreviewPure),
            ("Enemy does not attack invisible or unsupplied opponents", EnemyRequiresPerceivedTarget)
        };

    private static BattleTurnController ThreatFixture(int vulnerableHealth)
    {
        var battle=B(U("enemy",30,0m),U("low",20,0m),U("high",10,0m));
        foreach(var id in new[]{"enemy","low","high"})
        {
            battle.Abilities.RegisterKit(id,new UnitAbilityDefinition(2,SignatureReadiness.AfterNormalAttack));
            battle.Health.RegisterHealth(id,
                new UnitHealthDefinition(id=="enemy"?"foe":"hero",100,0),
                id=="low"?vulnerableHealth:100);
        }
        var field=battle.ConfigureBattlefield(
            new BattlefieldMap(Box(-15,-15,15,15,10),Array.Empty<FieldObstacle>()));
        field.Register("enemy",P(0,0),Body(),
            new[]{new FieldAbility(AbilitySlot.NormalAttack,HealthEffectKind.Damage,20,10,true)});
        field.Register("low",P(4,0),Body(),
            new[]{new FieldAbility(AbilitySlot.NormalAttack,HealthEffectKind.Damage,10,2,true)});
        field.Register("high",P(0,4),Body(),
            new[]{new FieldAbility(AbilitySlot.NormalAttack,HealthEffectKind.Damage,80,2,true)});
        field.RegisterEnemy("enemy",new EnemyBehavior(EnemyStyle.Aggressive,1));
        battle.StartNextRound();
        battle.BeginNextActivation();
        return battle;
    }

    private static void EnemyThreatPriority()
    {
        var battle=ThreatFixture(100);
        var result=RunEnemy(battle,"low","high");
        Equal("high",result.Effect.TargetId);
        Equal(80,battle.Health.GetState("high").CurrentHealth);
        Equal(100,battle.Health.GetState("low").CurrentHealth);
    }

    private static void EnemyLethalBeforeThreat()
    {
        var battle=ThreatFixture(15);
        var result=RunEnemy(battle,"high","low");
        Equal("low",result.Effect.TargetId);
        Equal(true,battle.Health.GetState("low").IsDefeated);
        Equal(100,battle.Health.GetState("high").CurrentHealth);
    }

    private static void EnemyRequiresPerceivedTarget()
    {
        var battle=ThreatFixture(100);
        var turn=battle.CurrentActivation;
        Equal(true,battle.Battlefield.TryPlanEnemyTurn(
            turn.ActivationId,Array.Empty<string>(),out var hidden,out var failure));
        Equal(EnemyTurnFailure.None,failure);
        Equal(null,hidden.TargetId);
        Equal(true,battle.Battlefield.TryPlanEnemyTurn(
            turn.ActivationId,new[]{"low"},out var visible,out failure));
        Equal(EnemyTurnFailure.None,failure);
        Equal("low",visible.TargetId);
        Equal(100,battle.Health.GetState("low").CurrentHealth);
        Equal(100,battle.Health.GetState("high").CurrentHealth);
    }

    private static void EnemyThreatPreviewPure()
    {
        var battle=ThreatFixture(100);
        var turn=battle.CurrentActivation;
        Equal(true,battle.Battlefield.TryPlanEnemyTurn(
            turn.ActivationId,new[]{"low","high"},out var plan,out var failure));
        Equal(EnemyTurnFailure.None,failure);
        Equal("high",plan.TargetId);
        Equal(100,battle.Health.GetState("high").CurrentHealth);
        Equal(true,battle.CurrentActivation.PrimaryActionAvailable);
        Equal(0L,battle.GetActivationCount("enemy")-1L);
    }
}
