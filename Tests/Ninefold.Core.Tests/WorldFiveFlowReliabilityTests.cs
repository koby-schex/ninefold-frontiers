using System;
using System.Linq;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;

internal static partial class Program
{
    private static (string Name,Action Run)[] WorldFiveFlowReliabilityTests() => new (string,Action)[] {
        ("World Five initial availability and prerequisites", FiveOpeningGates),
        ("World Five locked stage survives profile reopen", FiveLockedReload),
        ("World Five deployed mission survives profile reopen", FiveResume)
    };

    private static void FiveOpeningGates()
    {
        var m=WorldFiveOpeningTenPrototype.Open(new FakeSaveFiles(),new ProgressFiles());
        var ids=WorldFiveOpeningTenPrototype.MissionIds;
        Equal(ids[0],m.Read().NextCampaignMission.Definition.Id);
        Equal(true,m.Read().Missions.Single(x=>x.Definition.Id==ids[0]).Available);
        foreach(var id in ids.Skip(1))
            Equal(false,m.Read().Missions.Single(x=>x.Definition.Id==id).Available);
    }
    private static void FiveLockedReload()
    {
        var b=new FakeSaveFiles(); var p=new ProgressFiles();
        var m=WorldFiveOpeningTenPrototype.Open(b,p);
        var second=WorldFiveOpeningTenPrototype.MissionIds[1];
        m.SelectMission(second);
        Equal(false,m.Read().CanStart);
        m=WorldFiveOpeningTenPrototype.Open(b,p);
        Equal(MissionFlowPhase.Selection,m.Read().Phase);
        Equal(false,m.Read().Missions.Single(x=>x.Definition.Id==second).Available);
    }
    private static void FiveResume()
    {
        var b=new FakeSaveFiles(); var p=new ProgressFiles();
        var m=WorldFiveOpeningTenPrototype.Open(b,p);
        var first=WorldFiveOpeningTenPrototype.MissionIds[0];
        m.SelectMission(first);
        m.SetSquad(new[]{"fixture-a-1","fixture-a-2","fixture-a-3"});
        Equal(true,m.Read().CanStart);
        m.Start(m.Read().PlanId);
        m=WorldFiveOpeningTenPrototype.Open(b,p);
        Equal(MissionFlowPhase.Battle,m.Read().Phase);
        Equal(first,m.Read().Resume.MissionId);
        Equal(false,m.Read().CanStart);
    }
}
