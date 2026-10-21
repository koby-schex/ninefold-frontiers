using System;
using System.Linq;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;

internal static partial class Program
{
    private static (string Name,Action Run)[] CampaignJourneyReliabilityTests() =>
        new (string,Action)[] {
            ("Campaign next mission survives reward claim and profile reopen", JourneyNextMissionReload),
            ("Campaign locked finale remains unavailable before prerequisite", JourneyPrerequisiteGate),
            ("Campaign reward receipt is idempotent across reload", JourneyRewardIdempotent)
        };

    private static void JourneyNextMissionReload()
    {
        var x=new MenuFixture();
        Equal("fixture-opening",x.Menu.Read().NextCampaignMission.Definition.Id);
        x.Clear();
        Equal("fixture-finale",x.Menu.Read().NextCampaignMission.Definition.Id);
        x.Reopen();
        Equal(MissionFlowPhase.Selection,x.Menu.Read().Phase);
        Equal("fixture-finale",x.Menu.Read().NextCampaignMission.Definition.Id);
        Equal(true,Card(x,"fixture-finale").Available);
        Equal(1,x.Menu.Read().Progress.Claims.Count);
    }
    private static void JourneyPrerequisiteGate()
    {
        var x=new MenuFixture();
        Equal(false,Card(x,"fixture-finale").Available);
        Equal("fixture-opening",Card(x,"fixture-finale").MissingPrerequisites.Single());
        x.Prepare("fixture-finale");
        Equal(false,x.Menu.Read().CanStart);
        x.Reopen();
        Equal(false,Card(x,"fixture-finale").Available);
        Equal("fixture-opening",x.Menu.Read().NextCampaignMission.Definition.Id);
    }
    private static void JourneyRewardIdempotent()
    {
        var x=new MenuFixture();
        var attempt=x.Win();
        var first=x.Menu.ClaimRewards(attempt);
        Equal(false,first.AlreadyClaimed);
        Equal(10L,first.After.Balance("fixture-resource"));
        x.Reopen();
        var repeat=x.Menu.ClaimRewards(attempt);
        Equal(true,repeat.AlreadyClaimed);
        Equal(10L,repeat.After.Balance("fixture-resource"));
        Equal(1,x.Menu.Read().Progress.Claims.Count);
        x.Menu.ReturnToSelection(attempt);
        x.Reopen();
        Equal("fixture-finale",x.Menu.Read().NextCampaignMission.Definition.Id);
    }
}
