using System;
using System.Linq;
using Ninefold.Core.Content;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;

internal static partial class Program
{
    private static (string Name,Action Run)[] EnemyMissionIntegrationTests() =>
        new (string,Action)[] {
            ("World Five early training missions do not add objective targeting hints", TrainingHintsAbsent),
            ("World Five rescue encounter opts into a bounded carrier hint", RescueHintAuthored),
            ("Carrier hint resolves only an active mission-designated carrier", CarrierHintSelection),
            ("Carrier hint never introduces an unseen target", CarrierHintNoPerception)
        };

    private static MissionDefinition CarrierMission() =>
        new MissionDefinition("carrier-fixture","attempt",new[]{"c"},
            ObjectiveDefinition.RescueCarried("primary","b",
                Box(-5,-5,5,5),new InteractionDefinition(P(6,0),1,new[]{"c"}),"c"),
            OutcomePriority.FailureFirst);

    private static void TrainingHintsAbsent()
    {
        var catalog=WorldFiveOpeningTenPrototype.Create();
        foreach(var id in WorldFiveOpeningTenPrototype.MissionIds.Take(8))
            Equal(true,catalog.Missions[id].Actors
                .Where(a=>a.IsHostile && a.Behavior!=null)
                .All(a=>a.Behavior.ObjectivePriorityTargets.Count==0));
    }
    private static void RescueHintAuthored()
    {
        var catalog=WorldFiveOpeningTenPrototype.Create();
        var rescue=catalog.Missions[WorldFiveOpeningTenPrototype.MissionIds[8]];
        var threat=rescue.Actors.Single(a=>a.IsHostile);
        Equal("$carrier",threat.Behavior.ObjectivePriorityTargets.Single());
        Equal("w5-opening-ten-layout-04",WorldFiveOpeningTenPrototype.Revision);
        Equal("w5-opening-ten-profile-v4",WorldFiveOpeningTenPrototype.ProfileId);
    }
    private static void CarrierHintSelection()
    {
        var baseline=EnemyBattle(movement:0,range:10,mission:CarrierMission());
        Equal("b",RunEnemy(baseline,"b","c").Effect.TargetId);
        var aware=EnemyBattle(movement:0,range:10,mission:CarrierMission(),
            objectivePriorityTargets:new[]{"$carrier"});
        Equal("c",RunEnemy(aware,"b","c").Effect.TargetId);
    }
    private static void CarrierHintNoPerception()
    {
        var aware=EnemyBattle(movement:0,range:10,mission:CarrierMission(),
            objectivePriorityTargets:new[]{"$carrier"});
        Equal("b",RunEnemy(aware,"b").Effect.TargetId);
        Equal(500,aware.Health.GetState("c").CurrentHealth);
    }
}
