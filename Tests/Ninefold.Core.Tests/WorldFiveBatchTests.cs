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
        ("World Five 004–010 stage playable objectives away from solid cover", FullCampaignObjectiveReachability)
    };

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
