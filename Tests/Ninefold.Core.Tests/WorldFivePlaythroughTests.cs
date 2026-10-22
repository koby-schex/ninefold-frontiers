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
        ("World Five 003 rescue approach checkpoints and resumes safely", FiveRescuePickup),
        ("World Five mission 004 secure objective starts after rescue victory", FiveSecureMissionStart)
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

    private static void FiveRescuePickup() { FiveCompleteRescue(); }

    private static (FakeSaveFiles Battles,ProgressFiles Progress) FiveCompleteRescue()
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
        var route=new[] { new FieldPoint(0,0,-3), new FieldPoint(5,0,-3), new FieldPoint(6,0,2), pickup };
        int waypoint=0;
        int committedMoves=0;
        for(int step=0;step<300 && waypoint<route.Length;step++)
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
            var position=battle.Units.Single(u=>u.Id==battle.Activation.UnitId).Position.Value;
            var samples=options.Movement.Where(x=>!x.Position.Equals(position)).OrderBy(x=>
                (x.Position.X-current.X)*(x.Position.X-current.X)+
                (x.Position.Z-current.Z)*(x.Position.Z-current.Z)).ToArray();
            if(samples.Length==0)
            {
                var ended=play.EndTurn(battle.Activation.ActivationId);
                if(ended.AnimationId!=null) play.CompleteAnimation(ended.AnimationId);
                continue;
            }
            var best=samples[0];
            if((position.X-current.X)*(position.X-current.X)+
                (position.Z-current.Z)*(position.Z-current.Z)<4m &&
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
        // Explicit pickup after a legal, routed approach. Fail with a
        // descriptive message if movement has not actually reached the NPC.
        bool reached=false;
        for(int step=0;step<180 && !reached;step++)
        {
            var phase=play.Read().Battle;
            if(phase.Phase!=BattleSessionPhase.PlayerInput)
            {
                var advanced=play.Advance(new EnemyPerception(Array.Empty<string>(),Array.Empty<string>()));
                if(advanced.AnimationId!=null) play.CompleteAnimation(advanced.AnimationId);
                continue;
            }
            var plan=play.ReadPlanning();
            if(plan.Objectives.Any(x=>x.Id=="primary" && x.CanInteract))
            {
                Equal(InputOutcome.Previewed,play.TapObjective("primary").Outcome);
                var pickupAction=play.TapObjective("primary");
                Equal(InputOutcome.Committed,pickupAction.Outcome);
                if(pickupAction.AnimationId!=null) play.CompleteAnimation(pickupAction.AnimationId);
                reached=true;
                break;
            }
            var position=phase.Units.Single(u=>u.Id==phase.Activation.UnitId).Position.Value;
            var nearby=plan.Movement.Where(x=>!x.Position.Equals(position)).OrderBy(x=>
                (x.Position.X-pickup.X)*(x.Position.X-pickup.X)+
                (x.Position.Z-pickup.Z)*(x.Position.Z-pickup.Z)).FirstOrDefault();
            if(nearby!=null)
            {
                Equal(InputOutcome.Previewed,play.TapDestination(nearby.Position).Outcome);
                var move=play.TapDestination(nearby.Position);
                Equal(InputOutcome.Committed,move.Outcome);
                if(move.AnimationId!=null) play.CompleteAnimation(move.AnimationId);
            }
            var current=play.Read().Battle;
            if(current.Phase==BattleSessionPhase.PlayerInput)
            {
                var end=play.EndTurn(current.Activation.ActivationId);
                if(end.AnimationId!=null) play.CompleteAnimation(end.AnimationId);
            }
        }
        if(!reached) throw new Exception("Rescue pickup remains unreachable after legal waypoint traversal. Final phase: "+play.Read().Battle.Phase+"; position: "+play.Read().Battle.Units.Single(u=>u.Id=="fixture-a-1").Position?.X+","+play.Read().Battle.Units.Single(u=>u.Id=="fixture-a-1").Position?.Z+"; waypoint "+waypoint);
        // Resume after actual pickup, then carry the recovered target back
        // through the authored southern corridor to the extraction zone.
        Equal(true,play.Read().Battle.Objectives.Single(x=>x.Id=="primary").Rescued);
        menu=WorldFiveOpeningTenPrototype.Open(stores.Battles,stores.Progress);
        Equal(MissionFlowPhase.Battle,menu.Read().Phase);
        Equal(ids[2],menu.Read().Resume.MissionId);
        play=menu.ResumeBattle();
        Equal(true,play.Read().Battle.Objectives.Single(x=>x.Id=="primary").Rescued);
        var returnWaypoints=new[] {
            new FieldPoint(6,0,-3),new FieldPoint(0,0,-3),
            new FieldPoint(-2,0,-3)
        };
        int returnIndex=0;
        for(int step=0;step<240 && menu.Read().Results==null;step++)
        {
            var battle=play.Read().Battle;
            if(battle.Phase!=BattleSessionPhase.PlayerInput)
            {
                var update=play.Advance(new EnemyPerception(Array.Empty<string>(),Array.Empty<string>()));
                if(update.AnimationId!=null) play.CompleteAnimation(update.AnimationId);
                continue;
            }
            var actor=battle.Units.Single(x=>x.Id==battle.Activation.UnitId);
            if(returnIndex<returnWaypoints.Length-1 &&
                (actor.Position.Value.X-returnWaypoints[returnIndex].X)*(actor.Position.Value.X-returnWaypoints[returnIndex].X)+
                (actor.Position.Value.Z-returnWaypoints[returnIndex].Z)*(actor.Position.Value.Z-returnWaypoints[returnIndex].Z)<4m)
                returnIndex++;
            var target=returnWaypoints[returnIndex];
            var extractionStep=play.ReadPlanning().Movement
                .Where(x=>!x.Position.Equals(actor.Position.Value))
                .OrderBy(x=>(x.Position.X-target.X)*(x.Position.X-target.X)+
                    (x.Position.Z-target.Z)*(x.Position.Z-target.Z)).FirstOrDefault();
            if(extractionStep!=null)
            {
                Equal(InputOutcome.Previewed,play.TapDestination(extractionStep.Position).Outcome);
                var result=play.TapDestination(extractionStep.Position);
                Equal(InputOutcome.Committed,result.Outcome);
                if(result.AnimationId!=null) play.CompleteAnimation(result.AnimationId);
            }
            var after=play.Read().Battle;
            if(after.Phase==BattleSessionPhase.PlayerInput)
            {
                var ended=play.EndTurn(after.Activation.ActivationId);
                if(ended.AnimationId!=null) play.CompleteAnimation(ended.AnimationId);
            }
        }
        if(menu.Read().Results==null)
            throw new Exception("Rescue extraction did not reach victory after legal return movement.");
        Equal(Ninefold.Core.Missions.MissionOutcome.Victory,menu.Read().Results.Result.Outcome);
        var attempt=menu.Read().Results.Result.AttemptId;
        menu.ClaimRewards(attempt);
        menu.ReturnToSelection(attempt);
        menu=WorldFiveOpeningTenPrototype.Open(stores.Battles,stores.Progress);
        Equal(ids[3],menu.Read().NextCampaignMission.Definition.Id);
        Equal(true,menu.Read().Missions.Single(x=>x.Definition.Id==ids[3]).Available);
        return stores;
    }

    private static void FiveSecureMissionStart()
    {
        var stores=FiveCompleteRescue();
        var ids=WorldFiveOpeningTenPrototype.MissionIds;
        var menu=WorldFiveOpeningTenPrototype.Open(stores.Battles,stores.Progress);
        Equal(ids[3],menu.Read().NextCampaignMission.Definition.Id);
        menu.SelectMission(ids[3]);
        menu.SetSquad(new[]{"fixture-a-1"});
        Equal(true,menu.Read().CanStart);
        var play=menu.Start(menu.Read().PlanId);
        var objective=play.Read().Battle.Objectives.Single(x=>x.Id=="primary");
        Equal(Ninefold.Core.Missions.ObjectiveKind.Secure,objective.Kind);
        Equal(Ninefold.Core.Missions.ObjectiveStatus.Active,objective.Status);
        Equal(true,play.Read().Battle.Units.Any(u=>u.IsEnemyControlled && u.IsEligible));
        var planning=play.ReadPlanning();
        Equal(true,planning.Movement.Count>0);
        var sample=planning.Movement.First();
        Equal(InputOutcome.Previewed,play.TapDestination(sample.Position).Outcome);
        var movement=play.TapDestination(sample.Position);
        Equal(InputOutcome.Committed,movement.Outcome);
        if(movement.AnimationId!=null) play.CompleteAnimation(movement.AnimationId);
        menu=WorldFiveOpeningTenPrototype.Open(stores.Battles,stores.Progress);
        Equal(MissionFlowPhase.Battle,menu.Read().Phase);
        Equal(ids[3],menu.Read().Resume.MissionId);
    }
}
