using System;
using System.Linq;
using Ninefold.Core.Content;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Views;

internal static partial class Program
{
    private static (string Name,Action Run)[] WorldFivePlayableSliceTests() =>
        new (string,Action)[] {
            ("World Five slice constructs six separate validated authored fields", SliceMaps),
            ("World Five slice isolates campaign progression and exposes proper mission prerequisites", SliceLocks),
            ("World Five launch entry exposes an enterable six-mission path and first briefing", SliceEntryPath),
            ("World Five slice saves independently of the original campaign profile", SliceCheckpoint),
            ("World Five rescue has a distinct extraction zone and chosen carrier", SliceRescue),
            ("World Five missions 004–006 teach distinct zone, defense and survival rules", SliceSecondAct),
            ("World Five six-mission arc is sequential with no recycled map geometry", SliceCampaignAudit)
        };

    private static void SliceMaps()
    {
        var c=WorldFivePlayableSlice.Create();
        Equal(ContentClassification.AbstractFixture,c.Manifest.Classification);
        Equal(WorldFivePlayableSlice.Revision,c.Manifest.ContentRevision);
        Equal(7,c.Missions.Count); // six authored stages + unrelated Quick Battle fixture
        Equal(1,c.Campaigns.Count);
        var missions=WorldFivePlayableSlice.MissionIds.Select(id=>c.Missions[id]).ToArray();
        Equal(6,missions.Length);
        Equal(true,missions.All(m=>m.IsCampaign && m.FactionId=="fixture-a" &&
            m.Encounter==EncounterKind.Standard && m.MaximumSquad==3));
        Equal(true,missions.All(m=>m.MinimumSquad>=1 && m.MinimumSquad<=3));
        Equal(true,missions.All(m=>m.EnvironmentKey.StartsWith("study-w5-")));
        Equal(2,missions.Count(m=>m.Objectives[0].Kind==ObjectiveKind.Stabilize));
        Equal(ObjectiveKind.RescueAndExtract,missions[2].Objectives[0].Kind);
        Equal(true,missions.Select(m=>m.Map.Bounds.Max.X-m.Map.Bounds.Min.X)
            .Distinct().Count() > 1);
        Equal(true,missions[0].Map.Obstacles.Count!=missions[2].Map.Obstacles.Count);
        foreach(var m in missions)
        {
            Equal(true,m.Deployment.All(p=>p.Z==-3m));
            Equal(true,m.Deployment.All(p=>!m.Map.Obstacles.Any(o=>o.BlocksMovement &&
                p.X>=o.Bounds.Min.X && p.X<=o.Bounds.Max.X &&
                p.Z>=o.Bounds.Min.Z && p.Z<=o.Bounds.Max.Z)));
            if (m.Objectives[0].Interaction != null)
                Equal(false,m.Deployment.Any(p=>p.Equals(m.Objectives[0].Interaction.Point)));
        }
        Equal(true,PlaytestContentPackage.Create().Missions.ContainsKey("fixture-opening"));
        Equal(false,PlaytestContentPackage.Create().Missions.ContainsKey(WorldFivePlayableSlice.MissionIds[0]));
    }

    private static void SliceLocks()
    {
        var p=new ProgressFiles(); var b=new FakeSaveFiles();
        var menu=WorldFivePlayableSlice.Open(b,p);
        var view=menu.Read();
        Equal(MissionFlowPhase.Selection,view.Phase);
        Equal(3,view.Progress.OwnedUnits.Count);
        Equal(true,view.Missions.Single(m=>m.Definition.Id==WorldFivePlayableSlice.MissionIds[0]).Available);
        Equal(false,view.Missions.Single(m=>m.Definition.Id==WorldFivePlayableSlice.MissionIds[1]).Available);
        Equal(false,view.Missions.Single(m=>m.Definition.Id==WorldFivePlayableSlice.MissionIds[2]).Available);
        for(int i=3;i<WorldFivePlayableSlice.MissionIds.Length;i++)
            Equal(false,view.Missions.Single(m=>m.Definition.Id==WorldFivePlayableSlice.MissionIds[i]).Available);
        menu.SelectMission(WorldFivePlayableSlice.MissionIds[1]);
        menu.SetSquad(new[]{"fixture-a-1"});
        Equal(false,menu.Read().CanStart);
        menu.SelectMission(WorldFivePlayableSlice.MissionIds[0]);
        menu.SetSquad(new[]{"fixture-a-1","fixture-a-2","fixture-a-3"});
        Equal(true,menu.Read().CanStart);
    }

    private static void SliceEntryPath()
    {
        var store=new ProgressFiles();
        var battles=new FakeSaveFiles();
        var catalog=WorldFivePlayableSlice.Create();
        var menu=WorldFivePlayableSlice.Open(catalog,battles,store);
        var view=menu.Read();

        // The Home entry point should always load exactly one real
        // World Five campaign with six visible mission cards. Only the
        // opening mission is playable before claiming its predecessor.
        Equal(MissionFlowPhase.Selection,view.Phase);
        var campaign=view.Campaigns.Single(c=>c.CampaignId==WorldFivePlayableSlice.CampaignId);
        Equal(true,campaign.CanEnter);
        Equal(6,campaign.TotalMissions);
        Equal(6,catalog.Campaigns.Single().Missions.Count);
        var first=view.Missions.Single(m=>m.Definition.Id==WorldFivePlayableSlice.MissionIds[0]);
        Equal(true,first.Available);
        Equal(false,first.FactionRosterLocked);
        menu.SelectMission(first.Definition.Id);
        var selected=menu.Read();
        Equal(WorldFivePlayableSlice.MissionIds[0],selected.SelectedMissionId);
        menu.SetSquad(new[]{"fixture-a-1","fixture-a-2","fixture-a-3"});
        Equal(true,menu.Read().CanStart);
    }

    private static void SliceCheckpoint()
    {
        var oldBattle=new FakeSaveFiles();var oldProgress=new ProgressFiles();
        var original=new MissionPresentation(PlaytestContentPackage.Create(),
            oldBattle,oldProgress,PlaytestProfile.ProfileId);
        original.CreateProfile();
        var sliceBattle=new FakeSaveFiles();var sliceProgress=new ProgressFiles();
        var menu=WorldFivePlayableSlice.Open(sliceBattle,sliceProgress);
        menu.SelectMission(WorldFivePlayableSlice.MissionIds[0]);
        menu.SetSquad(new[]{"fixture-a-1"});
        var interaction=menu.Start(menu.Read().PlanId);
        Equal(InteractionFailure.OutOfReach,interaction.ReadPlanning().Objectives.Single().Failure);
        Equal(MissionFlowPhase.Battle,menu.Read().Phase);
        Equal(MissionFlowPhase.Selection,original.Read().Phase);
        Equal(true,oldBattle.Slots.All(b=>b==null));
        var resumed=WorldFivePlayableSlice.Open(sliceBattle,sliceProgress);
        Equal(MissionFlowPhase.Battle,resumed.Read().Phase);
        Equal(WorldFivePlayableSlice.MissionIds[0],resumed.Read().Resume.MissionId);
    }

    private static void SliceSecondAct()
    {
        var content=WorldFivePlayableSlice.Create();
        var fourth=content.Missions[WorldFivePlayableSlice.MissionIds[3]];
        var fifth=content.Missions[WorldFivePlayableSlice.MissionIds[4]];
        var sixth=content.Missions[WorldFivePlayableSlice.MissionIds[5]];
        var secure=fourth.Objectives[0];
        Equal(ObjectiveKind.Secure,secure.Kind);
        Equal(2,secure.Required);
        Equal(true,secure.Consecutive);
        Equal("$squad",secure.Subjects.Single());
        Equal("w5-zone-test-threat",secure.Contestants.Single());
        Equal(true,fourth.Actors.Single().IsHostile);

        var protect=fifth.Objectives[0];
        Equal(ObjectiveKind.Protect,protect.Kind);
        Equal(2,protect.Required);
        Equal("w5-shelter-test-party",protect.Subjects.Single());
        Equal(false,fifth.Actors.Single(a=>a.Id==protect.Subjects[0]).IsHostile);
        Equal(true,fifth.Actors.Any(a=>a.IsHostile));

        var survive=sixth.Objectives[0];
        Equal(ObjectiveKind.Survive,survive.Kind);
        Equal(3,survive.Required);
        Equal<ObjectiveDefinition>(survive,sixth.Objectives.Single());
        Equal(true,sixth.Actors.All(a=>a.IsHostile));
        Equal(2,sixth.Actors.Count);
        Equal(2,sixth.MinimumSquad);
        Equal(true,new[]{fourth,fifth,sixth}.All(m=>m.Deployment.Count==3 &&
            m.EnvironmentKey.StartsWith("study-w5-")));

        // A survive encounter should never imply that players must run
        // to a physical waypoint. No interaction or capture zone is authored.
        Equal<InteractionDefinition>(null,survive.Interaction);
        Equal<FieldBox>(null,survive.Zone);
    }

    private static void SliceCampaignAudit()
    {
        var c=WorldFivePlayableSlice.Create();
        var campaign=c.Campaigns.Single();
        Equal(6,campaign.Missions.Count);
        Equal(6,WorldFivePlayableSlice.MissionIds.Length);
        var linked=campaign.Missions.Select(x=>x.MissionId).ToArray();
        Equal(true,linked.SequenceEqual(WorldFivePlayableSlice.MissionIds));
        for(int i=1;i<linked.Length;i++)
            Equal(true,campaign.Missions[i].Prerequisites.SequenceEqual(new[]{linked[i-1]}));
        var report=CampaignProductionAudit.Review(c,WorldFivePlayableSlice.CampaignId,
            environmentPrefix:"study-w5-");
        Equal(true,report.PassesGate);
        Equal(6,report.MissionCount);
        Equal(6,report.DistinctLayoutCount);
        Equal(5,report.DistinctPrimaryObjectiveCount);
        Equal(false,report.Findings.Any(f=>f.Code=="CONSECUTIVE_MAP_REPEAT" ||
            f.Code=="CROSS_WORLD_ENVIRONMENT"));
    }

    private static void SliceRescue()
    {
        var c=WorldFivePlayableSlice.Create();
        var m=c.Missions[WorldFivePlayableSlice.MissionIds[2]];
        var obj=m.Objectives[0]; var actor=m.Actors.Single();
        Equal(false,actor.IsHostile);
        Equal(actor.Id,obj.Subjects.Single());
        Equal(false,actor.Position.X >= obj.Zone.Min.X && actor.Position.X <= obj.Zone.Max.X &&
            actor.Position.Z >= obj.Zone.Min.Z && actor.Position.Z <= obj.Zone.Max.Z);
        Equal(actor.Position,obj.Interaction.Point);
        Equal(true,m.Deployment.All(p=>p.Z<actor.Position.Z));
        Equal(true,obj.Interaction.AllowedActors.Contains("$leader"));
        Equal("$leader",obj.ExtractionCarrier);
        Equal(true,m.Deployment.Any(p=>p.X >= obj.Zone.Min.X &&
            p.X <= obj.Zone.Max.X && p.Z >= obj.Zone.Min.Z &&
            p.Z <= obj.Zone.Max.Z));
        Equal(true,m.Deployment.All(p =>
            Math.Abs(p.X-obj.Interaction.Point.X) + Math.Abs(p.Z-obj.Interaction.Point.Z) >
                obj.Interaction.Reach*2));
    }
}
