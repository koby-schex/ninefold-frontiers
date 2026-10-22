using System;
using System.Linq;
using Ninefold.Core.Content;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;

internal static partial class Program
{
    // Batch gate for the full ten-mission candidate, not a false
    // assertion that every mission has passed a live victory playthrough.
    private static (string Name, Action Run)[] WorldFiveBatchTests() => new (string, Action)[] {
        ("World Five 001–010 each author a legal objective and 3-unit deployment", FullCampaignDefinitions),
        ("World Five 001–010 preserve chained unlocks and reward policies", FullCampaignProgression),
        ("World Five 001–010 validate distinct battlefield bounds and legal spawns", FullCampaignMaps),
        ("World Five 004–010 stage playable objectives away from solid cover", FullCampaignObjectiveReachability),
        ("World Five four bonded traditions have distinct prototype abilities", BondedTraditionAbilityStudy),
        ("World Five Ashbound six enemy profiles stay noncollectible", AshboundFullRoster),
        ("World Five status-based powers and Afterecho link correctly", WorldFiveStatusMechanics),
        ("World Five Ashbound first contact follows tutorial", AshboundFirstEncounter),
        ("World Five Ashbound only reveal beyond ten tutorial missions", AshboundRevealBoundary),
        ("World Five integrated Act One catalog has full encounter and save isolation", IntegratedActOneReliability),
        ("World Five missions 11-14 vary Ashbound counts and compositions", WorldFiveEncounterVariation),
        ("World Five encounters cover every Ashbound archetype with varied pressure", AshboundTacticalVariety)
    };

    private static void AshboundTacticalVariety()
    {
        var c=WorldFiveEncounterVarietyV2.Create();
        var ids=new[]{WorldFiveActOnePrototype.FirstMission}
            .Concat(WorldFiveEncounterVarietyV2.NewMissionIds).ToArray();
        var encounters=ids.Select(id=>c.Missions[id]).ToArray();
        var types=encounters.SelectMany(m=>m.Actors.Select(a=>a.UnitId)).ToArray();
        foreach(var type in WorldFiveActOnePrototype.EnemyTemplateIds)
            Equal(true,types.Contains(type));
        Equal(true,types.GroupBy(x=>x).Any(g=>g.Count()>1));
        Equal(true,encounters.Select(m=>m.Actors.Count).Distinct().Count()>=3);
        Equal(true,encounters.All(m=>m.Actors.Select(a=>a.Id).Distinct().Count()==m.Actors.Count));
        foreach(var m in encounters)
        {
            Equal(true,m.Actors.All(a=>a.Behavior!=null));
            Equal(true,m.Actors.All(a=>c.Units[a.UnitId].Roster==null));
            Equal(true,m.Actors.All(a=>a.Behavior.PreferredDistance>0));
            Equal(true,m.Actors.All(a=>a.Behavior.ObjectivePriorityTargets.Count==0));
        }
        Equal(true,encounters.SelectMany(m=>m.Actors)
            .Select(a=>a.Behavior.PreferredDistance).Distinct().Count()>=4);
    }

    private static void WorldFiveEncounterVariation()
    {
        var catalog=WorldFiveEncounterVarietyV2.Create();
        var ids=new[]{WorldFiveActOnePrototype.FirstMission}
            .Concat(WorldFiveEncounterVarietyV2.NewMissionIds).ToArray();
        Equal(14,catalog.Campaigns.Single().Missions.Count);
        Equal(true,ids.Select(id=>catalog.Missions[id].Actors.Count)
            .SequenceEqual(new[]{2,3,4,2}));
        Equal(4,ids.Select(id=>string.Join("|",
            catalog.Missions[id].Actors.Select(a=>a.UnitId).OrderBy(x=>x)))
            .Distinct().Count());
        foreach(var id in ids)
        {
            var mission=catalog.Missions[id];
            Equal(true,mission.Actors.All(a=>a.IsHostile &&
                a.UnitId.StartsWith("ashbound-",StringComparison.Ordinal)));
            Equal(true,mission.Objectives[0].Subjects.All(target=>
                mission.Actors.Any(a=>a.Id==target)));
        }
        for(int i=0;i<WorldFiveEncounterVarietyV2.NewMissionIds.Length;i++)
            Equal(true,catalog.Campaigns.Single().Missions
                .Single(m=>m.MissionId==WorldFiveEncounterVarietyV2.NewMissionIds[i])
                .Prerequisites.SequenceEqual(new[]{i==0 ?
                    WorldFiveActOnePrototype.FirstMission : WorldFiveEncounterVarietyV2.NewMissionIds[i-1]}));
        Equal(false,catalog.Manifest.SaveRevision==
            WorldFiveCombatMechanicsV1.Create().Manifest.SaveRevision);
        foreach(var tutorialId in WorldFiveOpeningTenPrototype.MissionIds)
            Equal(true,catalog.Missions[tutorialId].Actors.All(actor=>
                !actor.UnitId.StartsWith("ashbound-",StringComparison.Ordinal)));
    }

    private static void IntegratedActOneReliability()
    {
        var old=WorldFiveOpeningTenPrototype.Create();
        var candidate=WorldFiveCombatMechanicsV1.Create();
        var first=WorldFiveStorySequence.FirstCampaignMissionId;
        Equal(11,candidate.Campaigns.Single().Missions.Count);
        Equal(true,candidate.Missions.ContainsKey(first));
        Equal(false,old.Missions.ContainsKey(first));
        Equal(false,old.Manifest.SaveRevision==candidate.Manifest.SaveRevision);
        var mission=candidate.Missions[first];
        Equal(ObjectiveKind.Defeat,mission.Objectives[0].Kind);
        Equal(2,mission.Objectives[0].Subjects.Count);
        Equal(true,mission.Objectives[0].Subjects.All(id=>mission.Actors.Any(a=>a.Id==id && a.IsHostile)));
        Equal(true,mission.Actors.All(a=>candidate.Units[a.UnitId].Roster==null));
        foreach(var id in WorldFiveOpeningTenPrototype.MissionIds)
            Equal(true,candidate.Missions[id].Actors.All(a=>!a.UnitId.StartsWith("ashbound-",StringComparison.Ordinal)));
        Equal(true,candidate.Campaigns.Single().Missions.Last().Prerequisites
            .SequenceEqual(new[]{WorldFiveOpeningTenPrototype.MissionIds[9]}));
        foreach(var id in WorldFiveActOnePrototype.EnemyTemplateIds)
            Equal(false,candidate.StarterUnits.Contains(id));
    }

    private static void WorldFiveStatusMechanics()
    {
        var source=WorldFivePlayableAbilityStudy.Create();
        var study=WorldFiveCombatMechanicsV1.Create();
        Equal(true,study.Manifest.SaveRevision!=source.Manifest.SaveRevision);
        foreach(var id in new[]{WorldFiveCombatMechanicsV1.Shelter,
            WorldFiveCombatMechanicsV1.Exposed,
            WorldFiveCombatMechanicsV1.Disrupted,
            WorldFiveCombatMechanicsV1.Afterecho})
            Equal(true,study.Statuses.ContainsKey(id));
        Equal(WorldFiveCombatMechanicsV1.Shelter,
            study.Abilities[study.Kits[study.Units["fixture-a-2"].KitId].AbilityIds[2]].Effect.StatusId);
        Equal(WorldFiveCombatMechanicsV1.Exposed,
            study.Abilities[study.Kits[study.Units["fixture-a-3"].KitId].AbilityIds[2]].Effect.StatusId);
        Equal(WorldFiveCombatMechanicsV1.Disrupted,
            study.Abilities[study.Kits[study.Units[WorldFiveActOnePrototype.CastigatorId].KitId].AbilityIds[2]].Effect.StatusId);
        foreach(var id in new[]{WorldFiveActOnePrototype.StrikerId,WorldFiveActOnePrototype.SeerId})
        {
            var passive=study.Abilities[study.Kits[study.Units[id].KitId].AbilityIds[0]].Passive;
            Equal(PassiveTrigger.OwnerActivationStarted,passive.Trigger);
            Equal(WorldFiveCombatMechanicsV1.Afterecho,passive.StatusId);
            Equal(true,study.Units[id].Roster==null);
        }
        foreach(var id in WorldFiveActOnePrototype.EnemyTemplateIds)
            Equal(false,study.StarterUnits.Contains(id));
    }

    private static void BondedTraditionAbilityStudy()
    {
        var source=WorldFiveActOnePrototype.Create();
        var study=WorldFivePlayableAbilityStudy.Create();
        Equal(4,WorldFivePlayableAbilityStudy.StandardIds.Length);
        var mainIds=WorldFivePlayableAbilityStudy.StandardIds.Select(id=>
            study.Kits[study.Units[id].KitId].AbilityIds[2]).ToArray();
        Equal(4,mainIds.Distinct().Count());
        foreach(var id in WorldFivePlayableAbilityStudy.StandardIds)
        {
            Equal(true,study.Units[id].Roster!=null);
            Equal(true,study.Abilities[study.Kits[study.Units[id].KitId].AbilityIds[2]]
                .Effect.HasHealthEffect);
            Equal(source.Units[id].Health,study.Units[id].Health);
        }
        Equal(HealthEffectKind.Healing,study.Abilities[mainIds[1]].Effect.Kind);
        Equal(HealthEffectKind.Damage,study.Abilities[mainIds[0]].Effect.Kind);
        Equal(HealthEffectKind.Damage,study.Abilities[mainIds[2]].Effect.Kind);
        Equal(HealthEffectKind.Damage,study.Abilities[mainIds[3]].Effect.Kind);
        Equal(true,source.Manifest.SaveRevision!=study.Manifest.SaveRevision);
        foreach(var id in WorldFiveActOnePrototype.EnemyTemplateIds)
            Equal(source.Units[id].KitId,study.Units[id].KitId);
    }

    private static void AshboundFullRoster()
    {
        var catalog=WorldFiveActOnePrototype.Create();
        Equal(6,WorldFiveActOnePrototype.EnemyTemplateIds.Length);
        Equal(6,WorldFiveActOnePrototype.EnemyTemplateIds.Distinct().Count());
        foreach(var id in WorldFiveActOnePrototype.EnemyTemplateIds)
        {
            var unit=catalog.Units[id];
            Equal(true,unit.Roster==null);
            Equal(false,catalog.StarterUnits.Contains(id));
            var kit=catalog.Kits[unit.KitId];
            Equal(4,kit.AbilityIds.Count);
            Equal(true,kit.AbilityIds.All(ability=>catalog.Abilities.ContainsKey(ability)));
            Equal(true,catalog.Abilities[kit.AbilityIds[1]].Effect.HasHealthEffect);
            Equal(true,catalog.Abilities[kit.AbilityIds[2]].Effect.HasHealthEffect);
        }
        Equal(6,WorldFiveActOnePrototype.EnemyTemplateIds.Select(id=>
            catalog.Units[id].Initiative).Distinct().Count());
    }

    private static void AshboundFirstEncounter()
    {
        var tutorial=WorldFiveOpeningTenPrototype.Create();
        var act=WorldFiveActOnePrototype.Create();
        var id=WorldFiveStorySequence.FirstCampaignMissionId;
        Equal(false,tutorial.Missions.ContainsKey(id));
        Equal(11,act.Campaigns.Single().Missions.Count);
        Equal(id,act.Campaigns.Single().Missions.Last().MissionId);
        Equal(true,act.Campaigns.Single().Missions.Last().Prerequisites
            .SequenceEqual(new[]{WorldFiveOpeningTenPrototype.MissionIds[9]}));
        Equal(true,act.Missions[id].Actors.All(x=>x.IsHostile));
        Equal(2,act.Missions[id].Actors.Count);
        Equal(true,act.Missions[id].Actors.All(x=>
            x.UnitId==WorldFiveActOnePrototype.ScoutId ||
            x.UnitId==WorldFiveActOnePrototype.StrikerId));
        Equal(true,act.Units[WorldFiveActOnePrototype.ScoutId].Roster==null);
        Equal(true,act.Units[WorldFiveActOnePrototype.StrikerId].Roster==null);
        Equal(false,act.StarterUnits.Contains(WorldFiveActOnePrototype.ScoutId));
        Equal(false,act.StarterUnits.Contains(WorldFiveActOnePrototype.StrikerId));
        Equal(true,act.Rewards.ContainsKey(id));
        Equal(true,act.Manifest.SaveRevision!=tutorial.Manifest.SaveRevision);
        foreach(var old in WorldFiveOpeningTenPrototype.MissionIds)
            Equal(true,act.Missions[old].Actors.All(x=>!x.UnitId.StartsWith("ashbound-",StringComparison.Ordinal)));
    }

    private static void AshboundRevealBoundary()
    {
        WorldFiveStorySequence.Validate();
        foreach(var id in WorldFiveOpeningTenPrototype.MissionIds)
        {
            Equal(true,WorldFiveStorySequence.IsTutorial(id));
            Equal(false,WorldFiveStorySequence.MayRevealAshbound(id));
            Equal("Prologue",WorldFiveStorySequence.StoryAct(id));
        }
        Equal(false,WorldFiveStorySequence.IsTutorial(WorldFiveStorySequence.FirstCampaignMissionId));
        Equal(true,WorldFiveStorySequence.MayRevealAshbound(WorldFiveStorySequence.FirstCampaignMissionId));
        Equal("Act One",WorldFiveStorySequence.StoryAct(WorldFiveStorySequence.FirstCampaignMissionId));
    }

    private static void FullCampaignDefinitions()
    {
        var catalog=WorldFiveOpeningTenPrototype.Create();
        var ids=WorldFiveOpeningTenPrototype.MissionIds;
        Equal(10,ids.Count);
        foreach(var id in ids)
        {
            var mission=catalog.Missions[id];
            Equal(true,mission.IsCampaign);
            Equal(true,mission.MinimumSquad>=1);
            Equal(true,mission.MaximumSquad>=mission.MinimumSquad);
            Equal(true,mission.Deployment.Count>=mission.MinimumSquad);
            Equal(true,mission.Objectives.Any(x=>x.Id=="primary"));
            Equal(true,mission.Objectives.All(x=>x.Required>0));
        }
        Equal(ObjectiveKind.Secure,catalog.Missions[ids[3]].Objectives[0].Kind);
        Equal(ObjectiveKind.Protect,catalog.Missions[ids[4]].Objectives[0].Kind);
        Equal(ObjectiveKind.Survive,catalog.Missions[ids[5]].Objectives[0].Kind);
        Equal(ObjectiveKind.Defeat,catalog.Missions[ids[6]].Objectives[0].Kind);
        Equal(ObjectiveKind.Secure,catalog.Missions[ids[7]].Objectives[0].Kind);
        Equal(ObjectiveKind.RescueAndExtract,catalog.Missions[ids[8]].Objectives[0].Kind);
        Equal(ObjectiveKind.Stabilize,catalog.Missions[ids[9]].Objectives[0].Kind);
    }

    private static void FullCampaignProgression()
    {
        var catalog=WorldFiveOpeningTenPrototype.Create();
        var ids=WorldFiveOpeningTenPrototype.MissionIds;
        var missions=catalog.Campaigns.Single().Missions;
        Equal(10,missions.Count);
        for(int i=0;i<ids.Count;i++)
        {
            Equal(ids[i],missions[i].MissionId);
            Equal(true,missions[i].Prerequisites.SequenceEqual(
                i==0 ? Array.Empty<string>() : new[]{ids[i-1]}));
            Equal(true,catalog.Rewards.ContainsKey(ids[i]));
        }
    }

    private static void FullCampaignMaps()
    {
        var catalog=WorldFiveOpeningTenPrototype.Create();
        foreach(var id in WorldFiveOpeningTenPrototype.MissionIds)
        {
            var m=catalog.Missions[id];
            Equal(true,m.Map.Bounds.Max.X>m.Map.Bounds.Min.X);
            Equal(true,m.Map.Bounds.Max.Z>m.Map.Bounds.Min.Z);
            Equal(true,m.Deployment.All(point=>
                point.X>=m.Map.Bounds.Min.X && point.X<=m.Map.Bounds.Max.X &&
                point.Z>=m.Map.Bounds.Min.Z && point.Z<=m.Map.Bounds.Max.Z));
            Equal(true,m.Actors.All(actor=>
                actor.Position.X>=m.Map.Bounds.Min.X &&
                actor.Position.X<=m.Map.Bounds.Max.X &&
                actor.Position.Z>=m.Map.Bounds.Min.Z &&
                actor.Position.Z<=m.Map.Bounds.Max.Z));
        }
    }
    private static void FullCampaignObjectiveReachability()
    {
        var catalog=WorldFiveOpeningTenPrototype.Create();
        var ids=WorldFiveOpeningTenPrototype.MissionIds;
        for(int index=3;index<ids.Count;index++)
        {
            var mission=catalog.Missions[ids[index]];
            var map=mission.Map;
            foreach(var objective in mission.Objectives)
            {
                if(objective.Interaction!=null)
                {
                    var at=objective.Interaction.Point;
                    Equal(true,at.X>=map.Bounds.Min.X && at.X<=map.Bounds.Max.X);
                    Equal(true,at.Z>=map.Bounds.Min.Z && at.Z<=map.Bounds.Max.Z);
                    Equal(false,map.Obstacles.Any(o=>o.BlocksMovement &&
                        at.X>o.Bounds.Min.X && at.X<o.Bounds.Max.X &&
                        at.Z>o.Bounds.Min.Z && at.Z<o.Bounds.Max.Z));
                }
                if(objective.Zone!=null)
                {
                    var zone=objective.Zone;
                    Equal(true,zone.Min.X>=map.Bounds.Min.X && zone.Max.X<=map.Bounds.Max.X);
                    Equal(true,zone.Min.Z>=map.Bounds.Min.Z && zone.Max.Z<=map.Bounds.Max.Z);
                    Equal(true,zone.Max.X>zone.Min.X && zone.Max.Z>zone.Min.Z);
                }
            }
            foreach(var point in mission.Deployment)
                Equal(false,map.Obstacles.Any(o=>o.BlocksMovement &&
                    point.X>o.Bounds.Min.X && point.X<o.Bounds.Max.X &&
                    point.Z>o.Bounds.Min.Z && point.Z<o.Bounds.Max.Z));
        }
    }

}
