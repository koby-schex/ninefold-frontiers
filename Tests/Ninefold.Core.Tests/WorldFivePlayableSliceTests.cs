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
            ("World Five slice constructs three separate validated authored fields", SliceMaps),
            ("World Five slice isolates campaign progression and exposes proper mission prerequisites", SliceLocks),
            ("World Five slice saves independently of the original campaign profile", SliceCheckpoint),
            ("World Five rescue has an actual eligible friendly proxy and reachable zone", SliceRescue)
        };

    private static void SliceMaps()
    {
        var c=WorldFivePlayableSlice.Create();
        Equal(ContentClassification.AbstractFixture,c.Manifest.Classification);
        Equal(WorldFivePlayableSlice.Revision,c.Manifest.ContentRevision);
        Equal(4,c.Missions.Count); // three authored stages + unrelated Quick Battle fixture
        Equal(1,c.Campaigns.Count);
        var missions=WorldFivePlayableSlice.MissionIds.Select(id=>c.Missions[id]).ToArray();
        Equal(3,missions.Length);
        Equal(true,missions.All(m=>m.IsCampaign && m.FactionId=="fixture-a" &&
            m.Encounter==EncounterKind.Standard && m.MaximumSquad==3));
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
        menu.SelectMission(WorldFivePlayableSlice.MissionIds[1]);
        menu.SetSquad(new[]{"fixture-a-1"});
        Equal(false,menu.Read().CanStart);
        menu.SelectMission(WorldFivePlayableSlice.MissionIds[0]);
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

    private static void SliceRescue()
    {
        var c=WorldFivePlayableSlice.Create();
        var m=c.Missions[WorldFivePlayableSlice.MissionIds[2]];
        var obj=m.Objectives[0]; var actor=m.Actors.Single();
        Equal(false,actor.IsHostile);
        Equal(actor.Id,obj.Subjects.Single());
        Equal(true,obj.Zone.Contains(c.Units[actor.UnitId].Body.At(actor.Position)));
        Equal(actor.Position,obj.Interaction.Point);
        Equal(true,m.Deployment.All(p=>p.Z<actor.Position.Z));
        Equal(true,obj.Interaction.AllowedActors.Contains("$squad"));
        Equal(true,m.Deployment.All(p=>
            FieldPoint.Distance(p,obj.Interaction.Point)>obj.Interaction.Reach));
    }
}
