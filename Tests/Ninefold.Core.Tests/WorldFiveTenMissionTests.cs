using System;
using System.Linq;
using Ninefold.Core.Content;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Views;

internal static partial class Program
{
    private static (string Name,Action Run)[] WorldFiveTenMissionTests() =>
        new (string,Action)[] {
            ("World Five V4 validates ten sequential prototype mission definitions", TenMissionCatalog),
            ("World Five 007–010 add combat, terrain control, recovery and optional finale goals", TenMissionObjectives),
            ("World Five 001–006 content stays unchanged in the isolated V4 package", TenMissionPreservesPriorStages),
            ("World Five V4 profile and saved battle remain independent of V3", TenMissionProfileIsolation),
            ("World Five V4 quality audit recognizes ten distinct battlefield layouts", TenMissionVariety)
        };

    private static void TenMissionCatalog()
    {
        var c=WorldFiveOpeningTenPrototype.Create();
        Equal(ContentClassification.AbstractFixture,c.Manifest.Classification);
        Equal(WorldFiveOpeningTenPrototype.Revision,c.Manifest.ContentRevision);
        Equal(11,c.Missions.Count); // ten authored missions + the existing quick battle
        Equal(1,c.Campaigns.Count);
        Equal(10,c.Campaigns.Single().Missions.Count);
        var links=c.Campaigns.Single().Missions;
        Equal(true,links.Select(x=>x.MissionId).SequenceEqual(WorldFiveOpeningTenPrototype.MissionIds));
        for(int i=1;i<links.Count;i++)
            Equal(true,links[i].Prerequisites.SequenceEqual(
                new[]{WorldFiveOpeningTenPrototype.MissionIds[i-1]}));
        Equal(true,WorldFiveOpeningTenPrototype.MissionIds
            .SequenceEqual(WorldFiveOpeningCampaignDraft.Missions.Select(m=>m.DraftId)));
        Equal(true,c.Missions.Values.Where(m=>m.IsCampaign).All(m=>
            m.FactionId=="fixture-a" && m.MinimumSquad<=3 && m.MaximumSquad==3 &&
            m.Encounter==EncounterKind.Standard &&
            m.EnvironmentKey.StartsWith("study-w5-")));
    }

    private static void TenMissionObjectives()
    {
        var c=WorldFiveOpeningTenPrototype.Create();
        var ids=WorldFiveOpeningTenPrototype.MissionIds;
        // Mission 4 is one contiguous, physically present capture zone,
        // not a tap-to-activate objective. Rendering must represent the same
        // immutable footprint defined by combat Core.
        var fourth=c.Missions[ids[3]];
        Equal(ObjectiveKind.Secure,fourth.Objectives[0].Kind);
        Equal(2,fourth.Objectives[0].Required);
        Equal(true,fourth.Objectives[0].Consecutive);
        Equal(true,fourth.Objectives[0].Zone != null);
        Equal(2m,fourth.Objectives[0].Zone.Min.X);
        Equal(5m,fourth.Objectives[0].Zone.Max.X);
        Equal(2m,fourth.Objectives[0].Zone.Min.Z);
        Equal(6m,fourth.Objectives[0].Zone.Max.Z);
        Equal(true,fourth.Objectives[0].Interaction == null);
        Equal(true,fourth.Map.Obstacles.All(o=>
            !o.Bounds.Overlaps(fourth.Objectives[0].Zone)));
        var seven=c.Missions[ids[6]];
        Equal(ObjectiveKind.Defeat,seven.Objectives[0].Kind);
        Equal(2,seven.Objectives[0].Subjects.Count);
        Equal(true,seven.Actors.All(a=>a.IsHostile));
        Equal(true,seven.Objectives[0].Subjects.All(id=>seven.Actors.Any(a=>a.Id==id)));

        var eight=c.Missions[ids[7]];
        Equal(ObjectiveKind.Secure,eight.Objectives[0].Kind);
        Equal(false,eight.Objectives[0].Consecutive);
        Equal(2,eight.Objectives[0].Required);
        Equal(true,eight.Map.Obstacles.Count>=4);
        Equal(true,eight.Objectives[0].Contestants.Single() == eight.Actors.Single().Id);

        var nine=c.Missions[ids[8]];
        Equal(ObjectiveKind.RescueAndExtract,nine.Objectives[0].Kind);
        Equal("$leader",nine.Objectives[0].ExtractionCarrier);
        Equal(true,nine.Objectives[0].Subjects.All(id=>nine.Actors.Any(a=>
            a.Id==id && !a.IsHostile)));
        Equal(true,nine.Actors.Any(a=>a.IsHostile));
        var pickup=nine.Objectives[0].Interaction.Point;
        var exit=nine.Objectives[0].Zone;
        Equal(false,pickup.X>=exit.Min.X && pickup.X<=exit.Max.X &&
            pickup.Z>=exit.Min.Z && pickup.Z<=exit.Max.Z);

        var ten=c.Missions[ids[9]];
        Equal(ObjectiveKind.Stabilize,ten.Objectives[0].Kind);
        Equal(2,ten.Objectives[0].Required);
        Equal(3,ten.Objectives.Count);
        Equal("primary",ten.Objectives[0].Id);
        Equal(ObjectiveKind.Defeat,ten.Objectives[1].Kind);
        Equal(ObjectiveKind.Survive,ten.Objectives[2].Kind);
        Equal(true,ten.Objectives[1].Subjects.All(id=>ten.Actors.Any(a=>a.Id==id)));
        Equal(2,ten.Objectives[2].Required);
    }

    private static void TenMissionPreservesPriorStages()
    {
        var v3=WorldFivePlayableSlice.Create();
        var v4=WorldFiveOpeningTenPrototype.Create();
        foreach(var id in WorldFivePlayableSlice.MissionIds)
        {
            var a=v3.Missions[id];var b=v4.Missions[id];
            Equal(a.Id,b.Id);
            Equal(a.EnvironmentKey,b.EnvironmentKey);
            Equal(a.Map.Bounds.Min,b.Map.Bounds.Min);
            Equal(a.Map.Bounds.Max,b.Map.Bounds.Max);
            Equal(a.Map.Obstacles.Count,b.Map.Obstacles.Count);
            Equal(a.Map.Ground.Count,b.Map.Ground.Count);
            Equal(a.Objectives[0].Kind,b.Objectives[0].Kind);
            Equal(a.Deployment.Count,b.Deployment.Count);
        }
        Equal(true,v4.Missions.ContainsKey("fixture-mixed"));
        Equal(false,v3.Missions.ContainsKey(WorldFiveOpeningTenPrototype.MissionIds[6]));
        Equal(false,PlaytestContentPackage.Create().Missions.ContainsKey(
            WorldFiveOpeningTenPrototype.MissionIds[9]));
        Equal(true,WorldFiveOpeningTenPrototype.ProfileId != WorldFivePlayableSlice.ProfileId);
        Equal(true,WorldFiveOpeningTenPrototype.Revision != WorldFivePlayableSlice.Revision);
    }

    private static void TenMissionProfileIsolation()
    {
        var oldBattle=new FakeSaveFiles();
        var oldProgress=new ProgressFiles();
        var original=WorldFivePlayableSlice.Open(oldBattle,oldProgress);
        Equal(MissionFlowPhase.Selection,original.Read().Phase);

        var newBattle=new FakeSaveFiles();
        var newProgress=new ProgressFiles();
        var menu=WorldFiveOpeningTenPrototype.Open(newBattle,newProgress);
        var view=menu.Read();
        Equal(MissionFlowPhase.Selection,view.Phase);
        Equal(10,view.Missions.Count(m=>m.Definition.IsCampaign));
        Equal(true,view.Missions.Single(m=>m.Definition.Id==
            WorldFiveOpeningTenPrototype.MissionIds[0]).Available);
        Equal(false,view.Missions.Single(m=>m.Definition.Id==
            WorldFiveOpeningTenPrototype.MissionIds[9]).Available);
        menu.SelectMission(WorldFiveOpeningTenPrototype.MissionIds[0]);
        menu.SetSquad(new[]{"fixture-a-1","fixture-a-2","fixture-a-3"});
        Equal(true,menu.Read().CanStart);
        menu.Start(menu.Read().PlanId);
        Equal(MissionFlowPhase.Battle,menu.Read().Phase);
        Equal(MissionFlowPhase.Selection,original.Read().Phase);
        var resumed=WorldFiveOpeningTenPrototype.Open(newBattle,newProgress);
        Equal(MissionFlowPhase.Battle,resumed.Read().Phase);
        Equal(WorldFiveOpeningTenPrototype.MissionIds[0],resumed.Read().Resume.MissionId);
    }

    private static void TenMissionVariety()
    {
        var c=WorldFiveOpeningTenPrototype.Create();
        var report=CampaignProductionAudit.Review(c,WorldFiveOpeningTenPrototype.CampaignId,
            environmentPrefix:"study-w5-");
        Equal(true,report.PassesGate);
        Equal(10,report.MissionCount);
        Equal(10,report.DistinctLayoutCount);
        Equal(6,report.DistinctPrimaryObjectiveCount);
        Equal(false,report.Findings.Any(f=>f.Code=="CONSECUTIVE_MAP_REPEAT" ||
            f.Code=="CROSS_WORLD_ENVIRONMENT"));
        // Still fails release: abstract unit proxies, only ten missions,
        // and too few distinct environment studies. Never claim otherwise.
        var release=CampaignProductionAudit.Review(c,WorldFiveOpeningTenPrototype.CampaignId,
            CampaignReviewStage.Release,environmentPrefix:"study-w5-");
        Equal(false,release.PassesGate);
        Equal(true,release.Findings.Any(f=>f.Code=="ABSTRACT_FIXTURE"));
        Equal(true,release.Findings.Any(f=>f.Code=="CAMPAIGN_LENGTH"));
        Equal(true,release.Findings.Any(f=>f.Code=="ENVIRONMENT_VARIETY"));
    }
}
