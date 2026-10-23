using System;
using System.Linq;
using Ninefold.Core.Campaigns;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Progression;
using Ninefold.Core.Views;

internal static partial class Program
{
    private static (string Name, Action Run)[] WorldFiveMissionElevenJourneyTests() =>
        new (string, Action)[] {
            ("World Five real 14-mission profile locks Ashbound debut until tutorial completion", RealWorldFiveElevenGate),
            ("World Five Mission 11 plays through a real battle and claims rewards exactly once", MissionElevenCombatVictory)
        };

    private static void RealWorldFiveElevenGate()
    {
        var content=WorldFiveTutorialWildlifeV1.Create();
        var battles=new FakeSaveFiles();
        var profiles=new ProgressFiles();
        var menu=new MissionPresentation(content,battles,profiles,WorldFiveTutorialWildlifeV1.ProfileId);
        menu.CreateProfile();
        var ids=WorldFiveOpeningTenPrototype.MissionIds;
        Equal(ids[0],menu.Read().NextCampaignMission.Definition.Id);
        var eleven=WorldFiveStorySequence.FirstCampaignMissionId;
        menu.SelectMission(eleven);
        menu.SetSquad(new[]{"fixture-a-1","fixture-a-2"});
        Equal(false,menu.Read().CanStart);
        Equal(true,menu.Read().Missions.Single(m=>m.Definition.Id==eleven).MissingPrerequisites.Contains(ids[9]));
        Throws<InvalidOperationException>(()=>menu.Start(menu.Read().PlanId));
        menu=new MissionPresentation(content,battles,profiles,WorldFiveTutorialWildlifeV1.ProfileId);
        menu.Open();
        Equal(false,menu.Read().Missions.Single(m=>m.Definition.Id==eleven).Available);
        Equal(ids[0],menu.Read().NextCampaignMission.Definition.Id);
    }

    // Deliberately unlock only Mission 11 in a test-exclusive campaign.
    // The production 14-mission prerequisite chain is independently checked
    // above; no synthetic progress/reward claim is ever written to player saves.
    private static ContentCatalog ElevenBattleHarness()
    {
        var source=WorldFiveTutorialWildlifeV1.Create();
        var missionId=WorldFiveStorySequence.FirstCampaignMissionId;
        var campaign=new CampaignDefinition("w5-m11-test-campaign","fixture-a",
            "m11-test-campaign-01",new[]{new CampaignMission(missionId)},
            new[]{new ResourceGrant("fixture-resource",10)});
        return new ContentCatalog(
            new ContentManifest("w5-m11-test","m11-test-v1","balance-v1",ContentClassification.AbstractFixture),
            source.Resources,source.Abilities.Values,source.Kits.Values,source.Units.Values,
            source.Missions.Values.Where(m=>!m.IsCampaign || m.Id==missionId),
            source.Rewards.Values.Where(policy=>!source.Missions[policy.MissionId].IsCampaign ||
                policy.MissionId==missionId),
            new[]{campaign},source.StarterUnits,source.Statuses.Values);
    }

    private static void MissionElevenCombatVictory()
    {
        var catalog=ElevenBattleHarness();
        var battles=new FakeSaveFiles();
        var profiles=new ProgressFiles();
        var menu=new MissionPresentation(catalog,battles,profiles,"m11-real-battle-test");
        menu.CreateProfile();
        var id=WorldFiveStorySequence.FirstCampaignMissionId;
        menu.SelectMission(id);
        menu.SetSquad(new[]{"fixture-a-1","fixture-a-2","fixture-a-3"});
        Equal(true,menu.Read().CanStart);
        var play=menu.Start(menu.Read().PlanId);
        Equal(id,play.Read().Battle.MissionId);
        Equal(2,play.Read().Battle.Units.Count(u=>u.IsEnemyControlled));
        int committedAttacks=0,committedMoves=0;
        bool resumed=false;
        for(int step=0;step<400 && menu.Read().Results==null;step++)
        {
            var view=play.Read().Battle;
            if(view.Phase!=BattleSessionPhase.PlayerInput)
            {
                var result=play.Advance(new EnemyPerception(Array.Empty<string>(),Array.Empty<string>()));
                if(result.AnimationId!=null) Equal(true,play.CompleteAnimation(result.AnimationId));
                continue;
            }

            // Normal game input: select an available ability, preview a
            // visible target, then confirm the same unit with a second tap.
            bool acted=false;
            foreach(var slot in new[]{AbilitySlot.Main,AbilitySlot.NormalAttack,AbilitySlot.Signature})
            {
                if(play.SelectAbility(slot).Outcome!=InputOutcome.Selected) continue;
                var target=play.Read().Targets
                    .Where(t=>t.IsValid && view.Units.Any(u=>u.Id==t.TargetId && u.IsEnemyControlled && u.IsEligible))
                    .OrderBy(t=>t.TargetId,StringComparer.Ordinal).FirstOrDefault();
                if(target==null) continue;
                Equal(InputOutcome.Previewed,play.TapUnit(target.TargetId).Outcome);
                var attack=play.TapUnit(target.TargetId);
                Equal(InputOutcome.Committed,attack.Outcome);
                if(attack.AnimationId!=null) Equal(true,play.CompleteAnimation(attack.AnimationId));
                committedAttacks++;
                acted=true;
                break;
            }
            if(!acted)
            {
                var position=view.Units.Single(u=>u.Id==view.Activation.UnitId).Position.Value;
                // Two bends deliberately avoid the existing cover islands.
                FieldPoint goal=position.X < 2m
                    ? new FieldPoint(2,0,-3)
                    : position.X < 7m ? new FieldPoint(7,0,-3)
                    : new FieldPoint(7,0,4);
                var candidate=play.ReadPlanning().Movement
                    .Where(m=>!m.Position.Equals(position))
                    .OrderBy(m=>DistanceSquared(m.Position,goal)).ThenBy(m=>m.Cost)
                    .FirstOrDefault();
                if(candidate!=null)
                {
                    Equal(InputOutcome.Previewed,play.TapDestination(candidate.Position).Outcome);
                    var moved=play.TapDestination(candidate.Position);
                    Equal(InputOutcome.Committed,moved.Outcome);
                    if(moved.AnimationId!=null) Equal(true,play.CompleteAnimation(moved.AnimationId));
                    committedMoves++;
                }
            }
            if(!resumed && committedMoves>0)
            {
                menu=new MissionPresentation(catalog,battles,profiles,"m11-real-battle-test");
                menu.Open();
                Equal(MissionFlowPhase.Battle,menu.Read().Phase);
                Equal(id,menu.Read().Resume.MissionId);
                play=menu.ResumeBattle();
                resumed=true;
            }
            var updated=play.Read().Battle;
            if(updated.Phase==BattleSessionPhase.PlayerInput && menu.Read().Results==null)
            {
                var ended=play.EndTurn(updated.Activation.ActivationId);
                if(ended.AnimationId!=null) Equal(true,play.CompleteAnimation(ended.AnimationId));
            }
        }
        if(menu.Read().Results==null)
            throw new Exception("Mission 11 did not reach a result through legal double-tap attacks and route movement.");
        Equal(MissionOutcome.Victory,menu.Read().Results.Result.Outcome);
        Equal(true,resumed);
        Equal(true,committedMoves>0);
        Equal(true,committedAttacks>=2);
        string attempt=menu.Read().Results.Result.AttemptId;
        var first=menu.ClaimRewards(attempt);
        Equal(false,first.AlreadyClaimed);
        Equal(true,menu.ClaimRewards(attempt).AlreadyClaimed);
        menu.ReturnToSelection(attempt);
        menu=new MissionPresentation(catalog,battles,profiles,"m11-real-battle-test");
        menu.Open();
        Equal(MissionFlowPhase.Selection,menu.Read().Phase);
        Equal(1,menu.Read().Progress.Claims.Count);
        Equal(true,menu.Read().Missions.Single(m=>m.Definition.Id==id).IsReplay);
    }

    private static decimal DistanceSquared(FieldPoint a,FieldPoint b)
    {
        decimal x=a.X-b.X,z=a.Z-b.Z;
        return x*x+z*z;
    }
}
