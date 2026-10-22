using System;
using System.Collections.Generic;
using System.Linq;

namespace Ninefold.Core.Content
{
    /// <summary>
    /// Authored story boundary for World Five's existing ten-mission tutorial.
    /// Does not silently rewrite the v4 campaign or saved battles.
    /// </summary>
    public static class WorldFiveStorySequence
    {
        public const int TutorialMissionCount = 10;
        public const string FirstCampaignMissionId = "w5-story-011";
        public const string MainAntagonist = "Ashbound";
        public static bool IsTutorial(string id) =>
            id != null && WorldFiveOpeningTenPrototype.MissionIds.Contains(id);
        public static bool MayRevealAshbound(string missionId) =>
            String.Equals(missionId,FirstCampaignMissionId,StringComparison.Ordinal);
        public static string StoryAct(string missionId)
        {
            if(IsTutorial(missionId)) return "Prologue";
            if(MayRevealAshbound(missionId)) return "Act One";
            return null;
        }
        public static void Validate()
        {
            WorldFiveOpeningCampaignDraft.Validate();
            if(WorldFiveOpeningTenPrototype.MissionIds.Count!=TutorialMissionCount)
                throw new InvalidOperationException("Story boundary moved without a migration plan.");
            if(WorldFiveOpeningTenPrototype.MissionIds.Contains(FirstCampaignMissionId))
                throw new InvalidOperationException("First main campaign mission overlaps tutorial.");
        }
    }
}
