using System;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
using Ninefold.Core.Views;

internal static partial class Program
{
    private static (string Name,Action Run)[] WorldFivePlaythroughTests() => new (string,Action)[] {
        ("World Five mission 001 reaches victory and unlocks mission 002", FiveFirstActualVictory),
        ("World Five missions 001 and 002 complete consecutively and unlock 003", FiveTwoMissionJourney)
    };

    private static void FiveFirstActualVictory()
    {
        var battles=new FakeSaveFiles(); var progress=new ProgressFiles();
        var menu=WorldFiveOpeningTenPrototype.Open(battles,progress);
        var ids=WorldFiveOpeningTenPrototype.MissionIds;
        menu.SelectMission(ids[0]);
        menu.SetSquad(new[]{"fixture-a-1"});
        var play=menu.Start(menu.Read().PlanId);
        var marker=WorldFiveOpeningTenPrototype.Create().Missions[ids[0]].Objectives[0].Interaction.Point;
        for(int step=0;step<60 && menu.Read().Results==null;step++)
        {
            var view=play.Read().Battle;
            BattleInputResult result;
            if(view.Phase==BattleSessionPhase.PlayerInput)
            {
                var planning=play.ReadPlanning();
                var objective=planning.Objectives.Single(x=>x.Id=="primary");
                if(objective.CanInteract)
                {
                    Equal(InputOutcome.Previewed,play.TapObjective("primary").Outcome);
                    result=play.TapObjective("primary");
                    Equal(InputOutcome.Committed,result.Outcome);
                }
                else if(planning.Movement.Count>0)
                {
                    // Select a genuinely reachable sample rather than hardcoding
                    // a coordinate which the pathfinder may clip behind cover.
                    var sample=planning.Movement
                        .OrderBy(x=>(x.Position.X-marker.X)*(x.Position.X-marker.X)
                                   +(x.Position.Z-marker.Z)*(x.Position.Z-marker.Z))
                        .ThenBy(x=>x.Cost).First();
                    Equal(InputOutcome.Previewed,play.TapDestination(sample.Position).Outcome);
                    result=play.TapDestination(sample.Position);
                    Equal(InputOutcome.Committed,result.Outcome);
                }
                else
                    result=play.EndTurn(view.Activation.ActivationId);
            }
            else result=play.Advance(new EnemyPerception(Array.Empty<string>(),Array.Empty<string>()));
            if(result.AnimationId!=null) play.CompleteAnimation(result.AnimationId);
            if(menu.Read().Results==null && play.Read().Battle.Phase==BattleSessionPhase.PlayerInput
                && play.Read().Battle.Activation.MovementRemaining==0m)
            {
                var end=play.EndTurn(play.Read().Battle.Activation.ActivationId);
                if(end.AnimationId!=null) play.CompleteAnimation(end.AnimationId);
            }
        }
        if(menu.Read().Results==null)
            throw new Exception("World Five mission 001 did not complete after 60 legal movement/interaction steps.");
        Equal(Ninefold.Core.Missions.MissionOutcome.Victory,menu.Read().Results.Result.Outcome);
        var attempt=menu.Read().Results.Result.AttemptId;
        menu.ClaimRewards(attempt);
        menu.ReturnToSelection(attempt);
        menu=WorldFiveOpeningTenPrototype.Open(battles,progress);
        Equal(ids[1],menu.Read().NextCampaignMission.Definition.Id);
        Equal(true,menu.Read().Missions.Single(m=>m.Definition.Id==ids[1]).Available);
    }

    private static void FiveTwoMissionJourney()
    {
        var battles=new FakeSaveFiles(); var progress=new ProgressFiles();
        var ids=WorldFiveOpeningTenPrototype.MissionIds;
        var catalog=WorldFiveOpeningTenPrototype.Create();
        var menu=WorldFiveOpeningTenPrototype.Open(battles,progress);
        for(int missionIndex=0;missionIndex<2;missionIndex++)
        {
            string missionId=ids[missionIndex];
            Equal(missionId,menu.Read().NextCampaignMission.Definition.Id);
            menu.SelectMission(missionId);
            menu.SetSquad(new[]{"fixture-a-1"});
            var play=menu.Start(menu.Read().PlanId);
            var marker=catalog.Missions[missionId].Objectives[0].Interaction.Point;
            for(int step=0;step<90 && menu.Read().Results==null;step++)
            {
                var view=play.Read().Battle;
                BattleInputResult result;
                if(view.Phase==BattleSessionPhase.PlayerInput)
                {
                    var planning=play.ReadPlanning();
                    if(planning.Objectives.Single(x=>x.Id=="primary").CanInteract)
                    {
                        Equal(InputOutcome.Previewed,play.TapObjective("primary").Outcome);
                        result=play.TapObjective("primary");
                        Equal(InputOutcome.Committed,result.Outcome);
                    }
                    else if(planning.Movement.Count>0)
                    {
                        var sample=planning.Movement
                            .OrderBy(x=>(x.Position.X-marker.X)*(x.Position.X-marker.X)
                                       +(x.Position.Z-marker.Z)*(x.Position.Z-marker.Z))
                            .ThenBy(x=>x.Cost).First();
                        Equal(InputOutcome.Previewed,play.TapDestination(sample.Position).Outcome);
                        result=play.TapDestination(sample.Position);
                        Equal(InputOutcome.Committed,result.Outcome);
                    }
                    else result=play.EndTurn(view.Activation.ActivationId);
                }
                else result=play.Advance(new EnemyPerception(Array.Empty<string>(),Array.Empty<string>()));
                if(result.AnimationId!=null) play.CompleteAnimation(result.AnimationId);
                if(menu.Read().Results==null && play.Read().Battle.Phase==BattleSessionPhase.PlayerInput
                    && play.Read().Battle.Activation.MovementRemaining==0m)
                {
                    var end=play.EndTurn(play.Read().Battle.Activation.ActivationId);
                    if(end.AnimationId!=null) play.CompleteAnimation(end.AnimationId);
                }
            }
            if(menu.Read().Results==null)
                throw new Exception("World Five "+missionId+" did not reach a result within 90 legal actions.");
            Equal(Ninefold.Core.Missions.MissionOutcome.Victory,menu.Read().Results.Result.Outcome);
            var attempt=menu.Read().Results.Result.AttemptId;
            menu.ClaimRewards(attempt);
            menu.ReturnToSelection(attempt);
            menu=WorldFiveOpeningTenPrototype.Open(battles,progress);
            Equal(ids[missionIndex+1],menu.Read().NextCampaignMission.Definition.Id);
            Equal(true,menu.Read().Missions.Single(x=>x.Definition.Id==ids[missionIndex+1]).Available);
        }
        Equal(2,menu.Read().Progress.Claims.Count);
    }
}
