using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Missions;

namespace Ninefold.Core.Content
{
    /// <summary>Non-mutating campaign balance instrumentation. Reports hard
    /// evidence to designers; it does NOT silently retune old fights or break
    /// in-progress checkpoints. Release approval stays a human gate.</summary>
    public sealed class CampaignBalanceWindow
    {
        public int From { get; }
        public int To { get; }
        public int TotalHostiles { get; }
        public int HighestOpposition { get; }
        public int ProtectedMissions { get; }
        public int MultiStage { get; }
        public int VariableFields { get; }
        public long FirstClearSupplies { get; }
        public long ReplaySupplies { get; }
        public IReadOnlyList<ObjectiveKind> ObjectiveKinds { get; }
        internal CampaignBalanceWindow(int from,int to,IReadOnlyList<MissionContent> missions,ContentCatalog catalog)
        {
            From=from;To=to;
            TotalHostiles=missions.Sum(m=>m.Actors.Count(a=>a.IsHostile));
            HighestOpposition=missions.Max(m=>m.Actors.Count(a=>a.IsHostile));
            ProtectedMissions=missions.Count(m=>m.Objectives.Any(o=>o.Kind==ObjectiveKind.Protect||o.Kind==ObjectiveKind.RescueAndExtract));
            MultiStage=missions.Count(m=>m.Objectives.Any(o=>o.IsFollowupStage));
            VariableFields=missions.Count(m=>m.ProceduralCover);
            ObjectiveKinds=Array.AsReadOnly(missions.Select(m=>m.Objectives[0].Kind).Distinct().OrderBy(x=>x).ToArray());
            FirstClearSupplies=missions.Sum(m=>catalog.Rewards[m.Id].FirstClear.Where(g=>g.ResourceId=="fixture-resource").Sum(g=>g.Amount));
            ReplaySupplies=missions.Sum(m=>catalog.Rewards[m.Id].Replay.Where(g=>g.ResourceId=="fixture-resource").Sum(g=>g.Amount));
        }
    }
    public sealed class WorldFiveBalanceReport
    {
        public IReadOnlyList<CampaignBalanceWindow> Windows { get; }
        public IReadOnlyList<string> ReleaseBlockers { get; }
        public int AuthoredMissions=>Windows.Sum(w=>w.To-w.From+1);
        internal WorldFiveBalanceReport(IReadOnlyList<CampaignBalanceWindow> windows,IEnumerable<string> blockers)
        {Windows=windows;ReleaseBlockers=Array.AsReadOnly(blockers.ToArray());}
        public bool ReleaseReady=>ReleaseBlockers.Count==0;
    }
    public static class WorldFiveBalanceAudit
    {
        public static WorldFiveBalanceReport Review(ContentCatalog catalog)
        {
            if(catalog==null)throw new ArgumentNullException(nameof(catalog));
            var campaign=catalog.Campaigns.Single(c=>c.Id==WorldFivePlayableSlice.CampaignId);
            var windows=new List<CampaignBalanceWindow>();
            var all=campaign.Missions.Select(n=>catalog.Missions[n.MissionId]).ToArray();
            for(int first=0;first<all.Length;first+=10)
                windows.Add(new CampaignBalanceWindow(first+1,Math.Min(all.Length,first+10),
                    all.Skip(first).Take(10).ToArray(),catalog));
            var blockers=new List<string>();
            if(catalog.Manifest.Classification==ContentClassification.AbstractFixture)
                blockers.Add("Combat and character statistics still use abstract fixture templates.");
            if(all.Length!=100)blockers.Add("World Five needs exactly 100 authored missions.");
            if(all.Any(m=>m.EnvironmentKey==null))blockers.Add("Every battlefield needs an environment direction.");
            if(all.Any(m=>m.Objectives.Any(o=>o.IsFollowupStage))==false)
                blockers.Add("Multi-stage objective variety is missing.");
            var roster=catalog.Units.Values.Where(u=>u.Roster?.FactionId=="fixture-a"&&!u.Roster.IsApex).ToArray();
            if(roster.Select(u=>u.Health).Distinct().Count()!=roster.Length ||
                roster.Select(u=>u.Armor).Distinct().Count()!=roster.Length)
                blockers.Add("Distinct playable-unit health and armor progression is not yet live in this saved catalog.");
            if(all.GroupBy(m=>m.Objectives[0].Kind).Count()<5)
                blockers.Add("Primary objective variety should reach at least five types.");
            // Every later battle is evaluated in ten-mission windows instead of
            // inferring difficulty from chapter labels. No auto-nerf or inflation.
            return new WorldFiveBalanceReport(windows,blockers);
        }
    }
}
