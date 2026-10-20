using System;
using System.Linq;
using Ninefold.Core.Content;
using Ninefold.Core.Missions;

internal static partial class Program
{
    private static (string Name, Action Run)[] WorldFiveOpeningTests() =>
        new (string, Action)[] {
            ("World Five opening beat sequence is complete and explicitly provisional", OpeningBlueprintValid),
            ("World Five opening teaches varied goals and clear region-specific wayfinding", OpeningBlueprintVariety),
            ("World Five opening plan is immutable and does not replace existing playable campaign fixtures", OpeningBlueprintNonPlayable)
        };

    private static void OpeningBlueprintValid()
    {
        WorldFiveOpeningCampaignDraft.Validate();
        var plan=WorldFiveOpeningCampaignDraft.Missions;
        Equal(10,plan.Count);
        Equal("w5-opening-ten-draft-01",WorldFiveOpeningCampaignDraft.DraftRevision);
        Equal(true,WorldFiveOpeningCampaignDraft.Status.Contains("NOT PLAYABLE"));
        Equal(true,plan.All(b => b.Order >= 1 && b.Order <= 10 &&
            b.DraftId == "w5-draft-" + b.Order.ToString("D3")));
        Equal(10,plan.Select(b=>b.DraftId).Distinct().Count());
        Equal(true,plan.All(b=>b.EnvironmentKey.StartsWith("w5-") &&
            b.AuthoringIntent.Length > 20 && b.PlayerClarityCue.Length > 20));
    }

    private static void OpeningBlueprintVariety()
    {
        var plan=WorldFiveOpeningCampaignDraft.Missions;
        Equal(ObjectiveKind.Stabilize,plan[0].PrimaryGoal);
        Equal(OpeningLesson.Movement,plan[0].Lesson);
        Equal(ObjectiveKind.Stabilize,plan[9].PrimaryGoal);
        Equal(true,plan.Select(b=>b.PrimaryGoal).Distinct().Count() >= 5);
        Equal(10,plan.Select(b=>b.Lesson).Distinct().Count());
        Equal(10,plan.Select(b=>b.EnvironmentKey).Distinct().Count());
        Equal(true,plan.All(b=>b.Region=="Pale Corridors" ||
            b.Region=="Anchor Basins" || b.Region=="Driftlands"));
        Equal(true,plan.Take(7).All(b=>b.Region!="Driftlands"));
        Equal(true,plan.All(b=>b.PlayerClarityCue.IndexOf("size selector",StringComparison.OrdinalIgnoreCase)<0));
    }

    private static void OpeningBlueprintNonPlayable()
    {
        var catalog=PlaytestContentPackage.Create();
        var revision=catalog.Manifest.SaveRevision;
        WorldFiveOpeningCampaignDraft.Validate();
        Equal(4,catalog.Missions.Count);
        Equal(2,catalog.Campaigns.Single(x=>x.Id=="fixture-starter").Missions.Count);
        Equal(true,WorldFiveOpeningCampaignDraft.Missions.All(b=>
            !catalog.Missions.ContainsKey(b.DraftId)));
        Equal(revision,catalog.Manifest.SaveRevision);
        var beats=WorldFiveOpeningCampaignDraft.Missions;
        Throws<NotSupportedException>(()=>
            ((System.Collections.Generic.IList<WorldFiveOpeningBeat>)beats)[0]=beats[1]);
    }
}
