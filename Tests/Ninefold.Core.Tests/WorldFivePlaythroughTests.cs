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
        ("World Five missions 001 and 002 complete consecutively and unlock 003", FiveTwoMissionJourney),
        ("World Five 003 rescue approach checkpoints and resumes safely", FiveRescuePickup)
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

    private static void FiveTwoMissionJourney() { FiveCompleteOpeningTwo(); }

    private static (FakeSaveFiles Battles,ProgressFiles Progress) FiveCompleteOpeningTwo()
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
        return (battles,progress);
    }

    private static void FiveRescuePickup()
    {
        var stores=FiveCompleteOpeningTwo();
        var ids=WorldFiveOpeningTenPrototype.MissionIds;
        var menu=WorldFiveOpeningTenPrototype.Open(stores.Battles,stores.Progress);
        Equal(ids[2],menu.Read().NextCampaignMission.Definition.Id);
        menu.SelectMission(ids[2]);
        menu.SetSquad(new[]{"fixture-a-1"});
        Equal(true,menu.Read().CanStart);
        var play=menu.Start(menu.Read().PlanId);
        var objective=WorldFiveOpeningTenPrototype.Create().Missions[ids[2]].Objectives[0];
        Equal(Ninefold.Core.Missions.ObjectiveKind.RescueAndExtract,objective.Kind);
        Equal(false,play.ReadPlanning().Objectives.Single(x=>x.Id=="primary").CanInteract);
        var pickup=objective.Interaction.Point;
        // Validate one legal approach move and checkpoint recovery. The
        // complete pickup/extraction route is not yet certified by this test.
        var planning=play.ReadPlanning();
        Equal(false,planning.Objectives.Single(x=>x.Id=="primary").CanInteract);
        var sample=planning.Movement.OrderBy(x=>
            (x.Position.X-pickup.X)*(x.Position.X-pickup.X)+
            (x.Position.Z-pickup.Z)*(x.Position.Z-pickup.Z))
            .ThenBy(x=>x.Cost).First();
        Equal(InputOutcome.Previewed,play.TapDestination(sample.Position).Outcome);
        var moved=play.TapDestination(sample.Position);
        Equal(InputOutcome.Committed,moved.Outcome);
        if(moved.AnimationId!=null) play.CompleteAnimation(moved.AnimationId);
        Equal(true,menu.Read().Results==null);
        // Rescue movement must remain possible beyond the initial approach.
        // The map's central cover blocks direct paths; use legal sampled
        // movement and a north-side waypoint instead of greedy straight-line
        // pursuit that may repeatedly collide with the same obstacle.
        var route=new[] { new FieldPoint(0,0,4), new FieldPoint(5,0,4), pickup };
        int waypoint=0;
        int committedMoves=0;
        for(int step=0;step<140 && waypoint<route.Length;step++)
        {
            var battle=play.Read().Battle;
            if(battle.Phase!=BattleSessionPhase.PlayerInput)
            {
                var advanced=play.Advance(new EnemyPerception(Array.Empty<string>(),Array.Empty<string>()));
                if(advanced.AnimationId!=null) play.CompleteAnimation(advanced.AnimationId);
                continue;
            }
            var options=play.ReadPlanning();
            if(options.Objectives.Single(x=>x.Id=="primary").CanInteract)
                break;
            var current=route[waypoint];
            var samples=options.Movement.OrderBy(x=>
                (x.Position.X-current.X)*(x.Position.X-current.X)+
                (x.Position.Z-current.Z)*(x.Position.Z-current.Z)).ToArray();
            if(samples.Length==0)
            {
                var ended=play.EndTurn(battle.Activation.ActivationId);
                if(ended.AnimationId!=null) play.CompleteAnimation(ended.AnimationId);
                continue;
            }
            var best=samples[0];
            if((best.Position.X-current.X)*(best.Position.X-current.X)+
                (best.Position.Z-current.Z)*(best.Position.Z-current.Z)<2m &&
                waypoint<route.Length-1) waypoint++;
            var preview=play.TapDestination(best.Position);
            Equal(InputOutcome.Previewed,preview.Outcome);
            var committed=play.TapDestination(best.Position);
            Equal(InputOutcome.Committed,committed.Outcome);
            committedMoves++;
            if(committed.AnimationId!=null) play.CompleteAnimation(committed.AnimationId);
            var after=play.Read().Battle;
            if(after.Phase==BattleSessionPhase.PlayerInput)
            {
                var ended=play.EndTurn(after.Activation.ActivationId);
                if(ended.AnimationId!=null) play.CompleteAnimation(ended.AnimationId);
            }
        }
        Equal(true,committedMoves>1);
        Equal(true,menu.Read().Results==null);
        menu=WorldFiveOpeningTenPrototype.Open(stores.Battles,stores.Progress);
        Equal(MissionFlowPhase.Battle,menu.Read().Phase);
        Equal(ids[2],menu.Read().Resume.MissionId);
    }
}
