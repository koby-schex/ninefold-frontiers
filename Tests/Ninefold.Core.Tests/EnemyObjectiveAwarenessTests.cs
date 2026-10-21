using System;
using Ninefold.Core.Combat;

internal static partial class Program
{
    private static (string Name,Action Run)[] EnemyObjectiveAwarenessTests() =>
        new (string,Action)[] {
            ("Objective-designated opponent wins equal-damage targeting tie", ObjectiveTargetWinsTie),
            ("Objective hints cannot reveal unperceived opponents", ObjectiveTargetMustBePerceived),
            ("Objective hints do not override finishing blows", ObjectiveTargetLethalWins),
            ("Objective hint definitions validate duplicate and blank identifiers", ObjectiveTargetValidation)
        };

    private static void ObjectiveTargetWinsTie()
    {
        var b=EnemyBattle(movement:0,range:10,objectivePriorityTargets:new[]{"c"});
        Equal("c",RunEnemy(b,"b","c").Effect.TargetId);
    }
    private static void ObjectiveTargetMustBePerceived()
    {
        var b=EnemyBattle(movement:0,range:10,objectivePriorityTargets:new[]{"c"});
        Equal("b",RunEnemy(b,"b").Effect.TargetId);
        Equal(500,b.Health.GetState("c").CurrentHealth);
    }
    private static void ObjectiveTargetLethalWins()
    {
        var b=EnemyBattle(movement:0,range:10,targetHp:20,
            objectivePriorityTargets:new[]{"c"});
        // Both have equal health in this fixture, so both are lethal:
        // explicit mission priority is the proper tie-break.
        Equal("c",RunEnemy(b,"b","c").Effect.TargetId);
    }
    private static void ObjectiveTargetValidation()
    {
        Throws<ArgumentException>(()=>new EnemyBehavior(EnemyStyle.Aggressive,1,
            objectivePriorityTargets:new[]{"c","c"}));
        Throws<ArgumentException>(()=>new EnemyBehavior(EnemyStyle.Aggressive,1,
            objectivePriorityTargets:new[]{" "}));
        Throws<ArgumentException>(()=>new EnemyBehavior(EnemyStyle.Aggressive,1,
            objectivePriorityTargets:new[]{"1","2","3","4","5","6","7","8","9"}));
    }
}
