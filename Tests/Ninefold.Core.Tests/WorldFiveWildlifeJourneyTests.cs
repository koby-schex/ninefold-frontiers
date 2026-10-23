using System;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Views;

internal static partial class Program
{
    private static (string Name,Action Run)[] WorldFiveWildlifeJourneyTests() =>
        new (string,Action)[] {
            ("World Five wildlife profile completes Missions 1–8, fights Seamstalkers and unlocks 009", WildlifeMissionSevenEightJourney)
        };

    private static void WildlifeMissionSevenEightJourney()
    {
        var catalog=WorldFiveTutorialWildlifeV1.Create();
        MissionPresentation Open(FakeSaveFiles battle,ProgressFiles progress)
        {
            var menu=new MissionPresentation(catalog,battle,progress,WorldFiveTutorialWildlifeV1.ProfileId);
            if(progress.Read(0)!=null || progress.Read(1)!=null) menu.Open();
            else menu.CreateProfile();
            return menu;
        }
        var stores=FiveCompleteOpeningSix(Open,catalog);
        var ids=WorldFiveOpeningTenPrototype.MissionIds;
        var menu=Open(stores.Battles,stores.Progress);
        Equal(6,menu.Read().Progress.Claims.Count);
        Equal(ids[6],menu.Read().NextCampaignMission.Definition.Id);
        Equal(false,menu.Read().Missions.Single(m=>m.Definition.Id==ids[8]).Available);
        Equal(false,menu.Read().Missions.Single(m=>
            m.Definition.Id==WorldFiveStorySequence.FirstCampaignMissionId).Available);

        for(int stage=6;stage<=7;stage++)
        {
            var missionId=ids[stage];
            menu.SelectMission(missionId);
            menu.SetSquad(new[]{"fixture-a-1","fixture-a-2","fixture-a-3"});
            Equal(true,menu.Read().CanStart);
            var play=menu.Start(menu.Read().PlanId);
            var authored=catalog.Missions[missionId];
            Equal(stage==6 ? ObjectiveKind.Defeat : ObjectiveKind.Secure,
                authored.Objectives[0].Kind);
            Equal(stage==6?2:1,authored.Actors.Count);
            Equal(true,authored.Actors.All(a=>a.UnitId ==
                (stage==6 ? WorldFiveTutorialWildlifeV1.SeamstalkerId :
                    WorldFiveTutorialWildlifeV1.SillrunnerId)));
            int attacks=0,moves=0,enemyTurns=0;
            bool resumed=false;
            for(int step=0;step<850 && menu.Read().Results==null;step++)
            {
                var view=play.Read().Battle;
                BattleInputResult action;
                bool moved=false, attacked=false;
                if(view.Phase!=BattleSessionPhase.PlayerInput)
                {
                    if(view.Phase==BattleSessionPhase.EnemyInput)enemyTurns++;
                    action=play.Advance(new EnemyPerception(Array.Empty<string>(),Array.Empty<string>()));
                }
                else
                {
                    var actor=view.Units.Single(u=>u.Id==view.Activation.UnitId);
                    var opponents=view.Units.Where(u=>u.IsEnemyControlled && u.IsEligible &&
                        u.Position.HasValue).OrderBy(u=>u.Position.Value.Z).ToArray();
                    bool didAttack=false;
                    action=null;
                    if(stage==6)
                    {
                        // Ordinary combat UI: select, preview and second tap
                        // the same enemy to commit an authoritative attack.
                        foreach(var slot in new[]{AbilitySlot.Main,AbilitySlot.NormalAttack,AbilitySlot.Signature})
                        {
                            if(play.SelectAbility(slot).Outcome!=InputOutcome.Selected)continue;
                            var target=play.Read().Targets.Where(t=>t.IsValid &&
                                opponents.Any(o=>o.Id==t.TargetId)).FirstOrDefault();
                            if(target==null)continue;
                            Equal(InputOutcome.Previewed,play.TapUnit(target.TargetId).Outcome);
                            action=play.TapUnit(target.TargetId);
                            Equal(InputOutcome.Committed,action.Outcome);
                            attacks++;
                            didAttack=true;
                            attacked=true;
                            break;
                        }
                    }
                    if(!didAttack)
                    {
                        bool needsTravel=stage==6 ? opponents.Length>0 :
                            !InsideZone(actor.Position.Value,authored.Objectives[0].Zone);
                        if(needsTravel)
                        {
                            FieldPoint destination;
                            if(stage==6)
                            {
                                var enemy=opponents[0].Position.Value;
                                // Eastern lower corridor clears the central cover,
                                // then approach the northern Seamstalker from east.
                                destination=actor.Position.Value.X<6m ?
                                    new FieldPoint(7,-0m,-3m) :
                                    new FieldPoint(enemy.X,0,enemy.Z);
                            }
                            else
                                destination=actor.Position.Value.X<3m && actor.Position.Value.Z<1m
                                    ? new FieldPoint(3,0,-3)
                                    : new FieldPoint(5.75m,0,2m);
                            var current=actor.Position.Value;
                            var sample=play.ReadPlanning().Movement
                                .Where(m=>!m.Position.Equals(current))
                                .OrderBy(m=>DistanceSquared(m.Position,destination))
                                .ThenBy(m=>m.Cost).FirstOrDefault();
                            if(sample!=null)
                            {
                                Equal(InputOutcome.Previewed,play.TapDestination(sample.Position).Outcome);
                                action=play.TapDestination(sample.Position);
                                Equal(InputOutcome.Committed,action.Outcome);
                                moves++;
                                moved=true;
                            }
                        }
                        action ??= play.EndTurn(view.Activation.ActivationId);
                    }
                }
                if(action.AnimationId!=null)Equal(true,play.CompleteAnimation(action.AnimationId));
                if(!resumed && (moves>0 || (stage==6 && attacks>0)) && menu.Read().Results==null)
                {
                    menu=Open(stores.Battles,stores.Progress);
                    Equal(MissionFlowPhase.Battle,menu.Read().Phase);
                    Equal(missionId,menu.Read().Resume.MissionId);
                    play=menu.ResumeBattle();
                    resumed=true;
                }
                if(menu.Read().Results==null && (moved || attacked) &&
                    play.Read().Battle.Phase==BattleSessionPhase.PlayerInput)
                {
                    var now=play.Read().Battle;
                    var end=play.EndTurn(now.Activation.ActivationId);
                    if(end.AnimationId!=null) Equal(true,play.CompleteAnimation(end.AnimationId));
                }
            }
            if(menu.Read().Results==null)
                throw new Exception(missionId+" did not reach victory within 850 steps. "+
                    "Moves "+moves+" attacks "+attacks+" rounds "+play.Read().Battle.Round+
                    " objective "+play.Read().Battle.Objectives[0].Progress);
            Equal(MissionOutcome.Victory,menu.Read().Results.Result.Outcome);
            Equal(true,resumed);
            if(stage==6)Equal(true,attacks>=2);
            else { Equal(true,moves>0); Equal(true,enemyTurns>0); }
            var attempt=menu.Read().Results.Result.AttemptId;
            Equal(false,menu.ClaimRewards(attempt).AlreadyClaimed);
            Equal(true,menu.ClaimRewards(attempt).AlreadyClaimed);
            menu.ReturnToSelection(attempt);
            menu=Open(stores.Battles,stores.Progress);
            Equal(ids[stage+1],menu.Read().NextCampaignMission.Definition.Id);
            Equal(stage+1,menu.Read().Progress.Claims.Count);
        }
        Equal(ids[8],menu.Read().NextCampaignMission.Definition.Id);
        Equal(false,menu.Read().Missions.Single(m=>
            m.Definition.Id==WorldFiveStorySequence.FirstCampaignMissionId).Available);
    }
}
