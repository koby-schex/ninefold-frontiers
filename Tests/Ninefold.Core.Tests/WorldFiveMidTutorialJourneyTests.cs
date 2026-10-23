using System;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Views;

internal static partial class Program
{
    private static (string Name,Action Run)[] WorldFiveMidTutorialJourneyTests() =>
        new (string,Action)[] {
            ("World Five 004–006 complete after rescue, persist and unlock 007", WorldFiveMidTutorialJourney),
            ("World Five Mission 004 zone control stays locked before Mission 003 rescue", WorldFiveMidTutorialCannotSkipRescue)
        };

    private static bool InsideZone(FieldPoint point,FieldBox zone)
        => point.X>=zone.Min.X && point.X<=zone.Max.X &&
           point.Z>=zone.Min.Z && point.Z<=zone.Max.Z;

    private static void WorldFiveMidTutorialCannotSkipRescue()
    {
        var battles=new FakeSaveFiles();
        var progress=new ProgressFiles();
        var menu=WorldFiveOpeningTenPrototype.Open(battles,progress);
        var four=WorldFiveOpeningTenPrototype.MissionIds[3];
        menu.SelectMission(four);
        menu.SetSquad(new[]{"fixture-a-1","fixture-a-2"});
        Equal(false,menu.Read().CanStart);
        Throws<InvalidOperationException>(()=>menu.Start(menu.Read().PlanId));
        menu=WorldFiveOpeningTenPrototype.Open(battles,progress);
        Equal(false,menu.Read().Missions.Single(m=>m.Definition.Id==four).Available);
    }

    private static void WorldFiveMidTutorialJourney()
    {
        // Reuse the independently certified 001–003 player journey. This
        // test must not mark progress with a fake claim or bypass Mission 003.
        var stores=FiveCompleteRescue();
        var ids=WorldFiveOpeningTenPrototype.MissionIds;
        var catalog=WorldFiveOpeningTenPrototype.Create();
        var menu=WorldFiveOpeningTenPrototype.Open(stores.Battles,stores.Progress);
        Equal(ids[3],menu.Read().NextCampaignMission.Definition.Id);
        int claimsBefore=menu.Read().Progress.Claims.Count;
        Equal(3,claimsBefore);

        for(int stage=3;stage<=5;stage++)
        {
            string id=ids[stage];
            Equal(id,menu.Read().NextCampaignMission.Definition.Id);
            menu.SelectMission(id);
            menu.SetSquad(new[]{"fixture-a-1","fixture-a-2","fixture-a-3"});
            Equal(true,menu.Read().CanStart);
            var play=menu.Start(menu.Read().PlanId);
            Equal(id,play.Read().Battle.MissionId);
            var objective=catalog.Missions[id].Objectives[0];
            Equal(stage==3 ? ObjectiveKind.Secure :
                stage==4 ? ObjectiveKind.Protect : ObjectiveKind.Survive,
                objective.Kind);

            int movements=0;
            bool resumed=false;
            for(int step=0;step<500 && menu.Read().Results==null;step++)
            {
                var battle=play.Read().Battle;
                BattleInputResult command;
                if(battle.Phase!=BattleSessionPhase.PlayerInput)
                    command=play.Advance(new EnemyPerception(Array.Empty<string>(),Array.Empty<string>()));
                else if(stage==3 && !InsideZone(
                    battle.Units.Single(u=>u.Id==battle.Activation.UnitId).Position.Value,
                    objective.Zone))
                {
                    var actor=battle.Units.Single(u=>u.Id==battle.Activation.UnitId);
                    var from=actor.Position.Value;
                    var goal=new FieldPoint(3,0,4);
                    var waypoint=from.X<2m && from.Z<1m
                        ? new FieldPoint(3,0,-2)
                        : goal;
                    var move=play.ReadPlanning().Movement.Where(m=>!m.Position.Equals(from))
                        .OrderBy(m=>DistanceSquared(m.Position,waypoint))
                        .ThenBy(m=>m.Cost).FirstOrDefault();
                    if(move==null)
                        command=play.EndTurn(battle.Activation.ActivationId);
                    else
                    {
                        Equal(InputOutcome.Previewed,play.TapDestination(move.Position).Outcome);
                        command=play.TapDestination(move.Position);
                        Equal(InputOutcome.Committed,command.Outcome);
                        movements++;
                    }
                }
                else command=play.EndTurn(battle.Activation.ActivationId);
                if(command.AnimationId!=null)
                    Equal(true,play.CompleteAnimation(command.AnimationId));
                if(!resumed && stage==3 && movements>0 && menu.Read().Results==null)
                {
                    menu=WorldFiveOpeningTenPrototype.Open(stores.Battles,stores.Progress);
                    Equal(MissionFlowPhase.Battle,menu.Read().Phase);
                    Equal(id,menu.Read().Resume.MissionId);
                    play=menu.ResumeBattle();
                    resumed=true;
                }
                // A player may move and then spend the remainder of the same
                // activation. Waiting until the next turn is also legal, but
                // deterministic advancement must never create a soft lock.
                if(menu.Read().Results==null &&
                    play.Read().Battle.Phase==BattleSessionPhase.PlayerInput &&
                    (stage!=3 || movements>0))
                {
                    var current=play.Read().Battle;
                    var finish=play.EndTurn(current.Activation.ActivationId);
                    if(finish.AnimationId!=null)
                        Equal(true,play.CompleteAnimation(finish.AnimationId));
                }
            }
            if(menu.Read().Results==null)
                throw new Exception("World Five "+id+" failed to reach an objective result after 500 legal steps.");
            Equal(MissionOutcome.Victory,menu.Read().Results.Result.Outcome);
            if(stage==3) { Equal(true,movements>0); Equal(true,resumed); }
            var attempt=menu.Read().Results.Result.AttemptId;
            Equal(false,menu.ClaimRewards(attempt).AlreadyClaimed);
            Equal(true,menu.ClaimRewards(attempt).AlreadyClaimed);
            menu.ReturnToSelection(attempt);
            menu=WorldFiveOpeningTenPrototype.Open(stores.Battles,stores.Progress);
            Equal(ids[stage+1],menu.Read().NextCampaignMission.Definition.Id);
            Equal(true,menu.Read().Missions.Single(x=>x.Definition.Id==ids[stage+1]).Available);
            Equal(claimsBefore+(stage-2),menu.Read().Progress.Claims.Count);
        }
        Equal(ids[6],menu.Read().NextCampaignMission.Definition.Id);
        Equal(false,menu.Read().Missions.Single(x=>x.Definition.Id==ids[9]).Available);
    }
}
