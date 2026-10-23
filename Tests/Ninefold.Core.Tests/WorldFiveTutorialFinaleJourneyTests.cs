using System;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Views;

internal static partial class Program
{
    private static (string Name,Action Run)[] WorldFiveTutorialFinaleJourneyTests() =>
        new (string,Action)[] {
            ("World Five wildlife profile completes all ten tutorial missions and unlocks Ashbound Mission 11", WildlifeTutorialFinaleJourney)
        };

    private static void WildlifeTutorialFinaleJourney()
    {
        // This is the real single-profile journey, not synthetic unlocks.
        // Missions 1–8 are won and rewarded by their existing Core input tests.
        var catalog=WorldFiveTutorialWildlifeV1.Create();
        var stores=FiveCompleteWildlifeEight();
        var ids=WorldFiveOpeningTenPrototype.MissionIds;
        MissionPresentation Open()
        {
            var session=new MissionPresentation(catalog,stores.Battles,stores.Progress,
                WorldFiveTutorialWildlifeV1.ProfileId);
            session.Open();
            return session;
        }
        var menu=Open();
        Equal(8,menu.Read().Progress.Claims.Count);
        Equal(ids[8],menu.Read().NextCampaignMission.Definition.Id);
        Equal(false,menu.Read().Missions.Single(m=>
            m.Definition.Id==WorldFiveStorySequence.FirstCampaignMissionId).Available);

        for(int stage=8;stage<=9;stage++)
        {
            string id=ids[stage];
            var authored=catalog.Missions[id];
            Equal(stage==8?ObjectiveKind.RescueAndExtract:ObjectiveKind.Stabilize,
                authored.Objectives[0].Kind);
            Equal(true,authored.Actors.All(a=>!a.UnitId.StartsWith(
                "ashbound-",StringComparison.Ordinal)));
            menu.SelectMission(id);
            menu.SetSquad(new[]{"fixture-a-1","fixture-a-2","fixture-a-3"});
            Equal(true,menu.Read().CanStart);
            var play=menu.Start(menu.Read().PlanId);
            Equal(id,play.Read().Battle.MissionId);
            if(stage==8) Equal("fixture-a-1",play.Read().Battle.RescueCarrierId);
            else
            {
                Equal(3,play.Read().Battle.Objectives.Count);
                Equal(2,authored.Objectives[0].Required);
            }

            bool checkpoint=false;
            bool pickup=false;
            int interactions=0, moves=0, turns=0;
            int outward=0, returnIndex=0, stabilization=0;
            var outbound=new[]{
                new FieldPoint(3,0,-3), new FieldPoint(7,0,-3),
                new FieldPoint(8,0,3), new FieldPoint(6,0,5)
            };
            var homebound=new[]{
                new FieldPoint(8,0,2), new FieldPoint(7,0,-3),
                new FieldPoint(0,0,-3), new FieldPoint(-2,0,-3)
            };
            var finalRoute=new[]{
                new FieldPoint(3,0,-4), new FieldPoint(6,0,-2),
                new FieldPoint(6,0,3)
            };
            for(int step=0;step<1100 && menu.Read().Results==null;step++)
            {
                var battle=play.Read().Battle;
                BattleInputResult action;
                bool moved=false, interacted=false;
                if(battle.Phase!=BattleSessionPhase.PlayerInput)
                    action=play.Advance(new EnemyPerception(Array.Empty<string>(),Array.Empty<string>()));
                else if(battle.Activation.UnitId!="fixture-a-1")
                    action=play.EndTurn(battle.Activation.ActivationId);
                else
                {
                    var planning=play.ReadPlanning();
                    var primary=planning.Objectives.Single(o=>o.Id=="primary");
                    bool rescued=stage==8 && battle.Objectives.Single(o=>o.Id=="primary").Rescued;
                    if(primary.CanInteract)
                    {
                        Equal(InputOutcome.Previewed,play.TapObjective("primary").Outcome);
                        action=play.TapObjective("primary");
                        Equal(InputOutcome.Committed,action.Outcome);
                        interactions++;
                        interacted=true;
                        if(stage==8) pickup=true;
                        else stabilization++;
                    }
                    else
                    {
                        var position=battle.Units.Single(u=>u.Id=="fixture-a-1").Position.Value;
                        FieldPoint[] route=stage==9?finalRoute:
                            rescued?homebound:outbound;
                        int waypoint=stage==9?stabilization==0?outward:outward:
                            rescued?returnIndex:outward;
                        // Move each intermediate waypoint into range before
                        // selecting the next; a straight diagonal would collide
                        // with the real cover placements in the authored maps.
                        while(waypoint<route.Length-1 &&
                            DistanceSquared(position,route[waypoint])<1.5m)
                            waypoint++;
                        if(stage==8 && rescued)returnIndex=waypoint;
                        else outward=waypoint;
                        var destination=route[waypoint];
                        var sample=planning.Movement.Where(m=>!m.Position.Equals(position))
                            .OrderBy(m=>DistanceSquared(m.Position,destination))
                            .ThenBy(m=>m.Cost).FirstOrDefault();
                        if(sample==null)
                            action=play.EndTurn(battle.Activation.ActivationId);
                        else
                        {
                            Equal(InputOutcome.Previewed,play.TapDestination(sample.Position).Outcome);
                            action=play.TapDestination(sample.Position);
                            Equal(InputOutcome.Committed,action.Outcome);
                            moved=true;
                            moves++;
                        }
                    }
                }
                if(action.AnimationId!=null)
                    Equal(true,play.CompleteAnimation(action.AnimationId));
                turns++;
                if(!checkpoint && menu.Read().Results==null &&
                    (stage==8 && pickup || stage==9 && stabilization>0))
                {
                    menu=Open();
                    Equal(MissionFlowPhase.Battle,menu.Read().Phase);
                    Equal(id,menu.Read().Resume.MissionId);
                    play=menu.ResumeBattle();
                    if(stage==8)
                        Equal(true,play.Read().Battle.Objectives.Single(o=>o.Id=="primary").Rescued);
                    else
                        Equal(true,play.Read().Battle.Objectives.Single(o=>o.Id=="primary").Progress>0);
                    checkpoint=true;
                }
                // End only the SAME activation that performed a movement or
                // interaction, never a newly advanced allied activation.
                if(menu.Read().Results==null && (moved || interacted) &&
                    play.Read().Battle.Phase==BattleSessionPhase.PlayerInput)
                {
                    var current=play.Read().Battle;
                    var end=play.EndTurn(current.Activation.ActivationId);
                    if(end.AnimationId!=null) Equal(true,play.CompleteAnimation(end.AnimationId));
                }
            }
            if(menu.Read().Results==null)
            {
                var current=play.Read().Battle;
                var actor=current.Units.FirstOrDefault(u=>u.Id=="fixture-a-1");
                throw new Exception(id+" failed after 1100 steps: round "+current.Round+
                    ", objective "+current.Objectives[0].Progress+
                    ", rescued "+current.Objectives[0].Rescued+
                    ", carrier position "+actor?.Position?.X+","+actor?.Position?.Z+
                    ", moves "+moves+", interactions "+interactions+", turns "+turns);
            }
            Equal(MissionOutcome.Victory,menu.Read().Results.Result.Outcome);
            Equal(true,checkpoint);
            Equal(true,moves>0);
            if(stage==8) { Equal(true,pickup); Equal(1,interactions); }
            else Equal(true,interactions>=2);
            string attempt=menu.Read().Results.Result.AttemptId;
            Equal(false,menu.ClaimRewards(attempt).AlreadyClaimed);
            Equal(true,menu.ClaimRewards(attempt).AlreadyClaimed);
            menu.ReturnToSelection(attempt);
            menu=Open();
            Equal(stage+1,menu.Read().Progress.Claims.Count);
            Equal(stage==8?ids[9]:WorldFiveStorySequence.FirstCampaignMissionId,
                menu.Read().NextCampaignMission.Definition.Id);
            Equal(true,menu.Read().Missions.Single(m=>m.Definition.Id==
                (stage==8?ids[9]:WorldFiveStorySequence.FirstCampaignMissionId)).Available);
            if(stage==8)
                Equal(false,menu.Read().Missions.Single(m=>
                    m.Definition.Id==WorldFiveStorySequence.FirstCampaignMissionId).Available);
        }

        // The very first Ashbound encounter is now available because all ten
        // genuine tutorial victories and claims are on the saved profile.
        var eleven=WorldFiveStorySequence.FirstCampaignMissionId;
        Equal(10,menu.Read().Progress.Claims.Count);
        Equal(true,catalog.Missions[eleven].Actors.All(a=>
            a.UnitId.StartsWith("ashbound-",StringComparison.Ordinal)));
        menu.SelectMission(eleven);
        menu.SetSquad(new[]{"fixture-a-1","fixture-a-2","fixture-a-3"});
        Equal(true,menu.Read().CanStart);
        var firstContact=menu.Start(menu.Read().PlanId);
        Equal(eleven,firstContact.Read().Battle.MissionId);
        Equal(2,firstContact.Read().Battle.Units.Count(u=>u.IsEnemyControlled));
        menu=Open();
        Equal(MissionFlowPhase.Battle,menu.Read().Phase);
        Equal(eleven,menu.Read().Resume.MissionId);
    }
}
