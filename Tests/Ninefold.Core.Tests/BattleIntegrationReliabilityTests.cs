using System;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;

internal static partial class Program
{
    private static (string Name,Action Run)[] BattleIntegrationReliabilityTests() =>
        new (string,Action)[] {
            ("Integration finishing action persists its result through reload", ReliabilityTerminalReload),
            ("Integration spent ability is not restored after reloading", ReliabilitySpentAbility),
            ("Integration interrupted enemy turn resumes without double damage", ReliabilityEnemyReplay),
            ("Integration finished mission cannot run another enemy attack", ReliabilityTerminalEnemy)
        };

    private static void ReliabilityTerminalReload()
    {
        var x=new SessionFixture(rounds:0,enemyHp:20);
        Equal(true,x.Session.TryUseAbility(x.Id,AbilitySlot.NormalAttack,"b",out _,out _,out _));
        Equal(BattleSessionPhase.Results,x.Session.Read().Phase);
        Equal(MissionOutcome.Victory,x.Session.Read().Result.Outcome);
        var before=x.Bytes();
        x.Reopen();
        Equal(BattleSessionPhase.Results,x.Session.Read().Phase);
        Equal(MissionOutcome.Victory,x.Session.Read().Result.Outcome);
        x.Session.Advance(new EnemyPerception(new[]{"a"},Array.Empty<string>()));
        Equal(before,x.Bytes());
        Equal(0L,x.Flow.ReadBattle().GetActivationCount("b"));
    }
    private static void ReliabilitySpentAbility()
    {
        var x=new SessionFixture();
        long activation=x.Id;
        Equal(true,x.Session.TryUseAbility(activation,AbilitySlot.NormalAttack,"b",out _,out _,out _));
        Equal(80,x.Flow.ReadBattle().Health.GetState("b").CurrentHealth);
        x.Reopen();
        Equal(activation,x.Id);
        Equal(false,x.Session.Read().Activation.PrimaryActionAvailable);
        var before=x.Bytes();
        Equal(false,x.Session.TryUseAbility(activation,AbilitySlot.NormalAttack,"b",out _,out _,out _));
        Equal(before,x.Bytes());
        Equal(80,x.Flow.ReadBattle().Health.GetState("b").CurrentHealth);
    }
    private static void ReliabilityEnemyReplay()
    {
        var x=new SessionFixture();
        x.ToEnemy();
        Equal(BattleSessionPhase.EnemyInput,x.Session.Read().Phase);
        x.Session.Advance(new EnemyPerception(new[]{"a"},Array.Empty<string>()));
        Equal(80,x.Flow.ReadBattle().Health.GetState("a").CurrentHealth);
        var before=x.Bytes();
        x.Reopen();
        x.Session.Advance(new EnemyPerception(new[]{"a"},Array.Empty<string>()));
        Equal(before,x.Bytes());
        Equal(80,x.Flow.ReadBattle().Health.GetState("a").CurrentHealth);
        Equal(1L,x.Flow.ReadBattle().GetActivationCount("b"));
    }
    private static void ReliabilityTerminalEnemy()
    {
        var x=new SessionFixture(playerHp:10);
        x.ToEnemy();
        x.Session.Advance(new EnemyPerception(new[]{"a"},Array.Empty<string>()));
        Equal(BattleSessionPhase.Results,x.Session.Read().Phase);
        Equal(MissionOutcome.Defeat,x.Session.Read().Result.Outcome);
        var before=x.Bytes();
        x.Session.Advance(new EnemyPerception(new[]{"a"},Array.Empty<string>()));
        Equal(before,x.Bytes());
        x.Reopen();
        Equal(BattleSessionPhase.Results,x.Session.Read().Phase);
        Equal(0L,x.Flow.ReadBattle().GetActivationCount("c"));
    }
}
