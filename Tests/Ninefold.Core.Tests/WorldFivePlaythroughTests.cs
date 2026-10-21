using System;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
using Ninefold.Core.Views;

internal static partial class Program
{
    private static (string Name,Action Run)[] WorldFivePlaythroughTests() => new (string,Action)[] {
        ("World Five mission 001 reaches victory and unlocks mission 002", FiveFirstActualVictory)
    };

    private static void FiveFirstActualVictory()
    {
        var battles=new FakeSaveFiles(); var progress=new ProgressFiles();
        var menu=WorldFiveOpeningTenPrototype.Open(battles,progress);
        var ids=WorldFiveOpeningTenPrototype.MissionIds;
        menu.SelectMission(ids[0]);
        menu.SetSquad(new[]{"fixture-a-1"});
        var play=menu.Start(menu.Read().PlanId);
        for(int step=0;step<24 && menu.Read().Results==null;step++)
        {
            var v=play.Read().Battle;
            if(v.Phase==BattleSessionPhase.PlayerInput)
            {
                var at=v.Activation;
                var objective=play.ReadPlanning().Objectives.Single(x=>x.Id=="primary");
                if(objective.Failure==Ninefold.Core.Missions.InteractionFailure.None)
                {
                    var preview=play.TapObjective("primary");
                    Equal(InputOutcome.Previewed,preview.Outcome);
                    var done=play.TapObjective("primary");
                    Equal(InputOutcome.Committed,done.Outcome);
                    if(done.AnimationId!=null) play.CompleteAnimation(done.AnimationId);
                }
                else
                {
                    // Two reachable legs on the actual authored World Five field.
                    var target=v.Round==1?P(1,0):P(3,2);
                    var preview=play.TapDestination(target);
                    Equal(InputOutcome.Previewed,preview.Outcome);
                    var moved=play.TapDestination(target);
                    Equal(InputOutcome.Committed,moved.Outcome);
                    if(moved.AnimationId!=null) play.CompleteAnimation(moved.AnimationId);
                    play.EndTurn(play.Read().Battle.Activation.ActivationId);
                }
            }
            else
            {
                var next=play.Advance(new EnemyPerception(Array.Empty<string>(),Array.Empty<string>()));
                if(next.AnimationId!=null) play.CompleteAnimation(next.AnimationId);
            }
        }
        Equal(Ninefold.Core.Missions.MissionOutcome.Victory,menu.Read().Results.Result.Outcome);
        var attempt=menu.Read().Results.Result.AttemptId;
        menu.ClaimRewards(attempt);
        menu.ReturnToSelection(attempt);
        menu=WorldFiveOpeningTenPrototype.Open(battles,progress);
        Equal(ids[1],menu.Read().NextCampaignMission.Definition.Id);
        Equal(true,menu.Read().Missions.Single(m=>m.Definition.Id==ids[1]).Available);
    }
}
