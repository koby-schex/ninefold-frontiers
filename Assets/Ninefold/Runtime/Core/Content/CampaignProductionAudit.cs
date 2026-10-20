using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ninefold.Core.Campaigns;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;

namespace Ninefold.Core.Content
{
    /// <summary>
    /// Editor/CI authoring guidance only. No production mission, battle save,
    /// game UI, balance, progression or canon is changed by this review.
    /// </summary>
    public enum CampaignReviewStage { Draft, Release }
    public enum CampaignReviewSeverity { Advisory, Blocking }

    public sealed class CampaignReviewFinding
    {
        public string Code { get; }
        public string MissionId { get; }
        public string Detail { get; }
        public CampaignReviewSeverity Severity { get; }
        internal CampaignReviewFinding(string code, string missionId, string detail,
            CampaignReviewSeverity severity)
        {
            Code=code; MissionId=missionId; Detail=detail; Severity=severity;
        }
    }

    public sealed class CampaignProductionReport
    {
        public string CampaignId { get; }
        public CampaignReviewStage Stage { get; }
        public int MissionCount { get; }
        public int DistinctLayoutCount { get; }
        public int DistinctEnvironmentCount { get; }
        public int DistinctPrimaryObjectiveCount { get; }
        public IReadOnlyList<CampaignReviewFinding> Findings { get; }
        public bool PassesGate => Findings.All(f => f.Severity != CampaignReviewSeverity.Blocking);

        internal CampaignProductionReport(string campaignId, CampaignReviewStage stage, int missionCount,
            int layouts, int environments, int objectives, IEnumerable<CampaignReviewFinding> findings)
        {
            CampaignId=campaignId; Stage=stage; MissionCount=missionCount;
            DistinctLayoutCount=layouts; DistinctEnvironmentCount=environments;
            DistinctPrimaryObjectiveCount=objectives;
            Findings=Array.AsReadOnly(findings.ToArray());
        }
    }

    public static class CampaignProductionAudit
    {
        public const int LaunchMissionMinimum = 100;

        /// <summary>
        /// Release gate is intentionally stricter than Draft. It provides an
        /// authoring checklist, NOT a new runtime validation or auto-release.
        /// A true result does not certify canon, Unity visuals or device QA.
        /// </summary>
        public static CampaignProductionReport Review(ContentCatalog catalog, string campaignId,
            CampaignReviewStage stage = CampaignReviewStage.Draft,
            string environmentPrefix = null, int minimumReleaseMissions = LaunchMissionMinimum)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (!Enum.IsDefined(typeof(CampaignReviewStage),stage))
                throw new ArgumentOutOfRangeException(nameof(stage));
            if (minimumReleaseMissions < 1 || minimumReleaseMissions > 4096)
                throw new ArgumentOutOfRangeException(nameof(minimumReleaseMissions));
            if (environmentPrefix != null && string.IsNullOrWhiteSpace(environmentPrefix))
                throw new ArgumentException("Environment prefix must be nonblank.", nameof(environmentPrefix));
            var campaign=catalog.Campaigns.FirstOrDefault(c => c.Id == campaignId);
            if (campaign == null) throw new ArgumentException("Unknown campaign.",nameof(campaignId));
            bool release=stage==CampaignReviewStage.Release;
            var missions=campaign.Missions.Select(c => catalog.Missions[c.MissionId]).ToArray();
            var findings=new List<CampaignReviewFinding>();
            void Flag(string code, string id, string detail, bool blockingOnRelease=true)
                => findings.Add(new CampaignReviewFinding(code,id,detail,
                    release && blockingOnRelease ? CampaignReviewSeverity.Blocking : CampaignReviewSeverity.Advisory));

            if (release && catalog.Manifest.Classification == ContentClassification.AbstractFixture)
                Flag("ABSTRACT_FIXTURE",null,"The fixture catalog is not commercial campaign content.");
            if (release && missions.Length < minimumReleaseMissions)
                Flag("CAMPAIGN_LENGTH",null,"Release requires at least " + minimumReleaseMissions + " authored missions; found " + missions.Length + ".");

            var layouts=new HashSet<string>(StringComparer.Ordinal);
            var scenes=new HashSet<string>(StringComparer.Ordinal);
            var objectives=new HashSet<ObjectiveKind>();
            var encounteredLayouts=new Dictionary<string,string>(StringComparer.Ordinal);
            string previous=null;
            foreach (var mission in missions)
            {
                if (!mission.IsCampaign || mission.FactionId != campaign.FactionId)
                    Flag("CAMPAIGN_FACTION",mission.Id,"Mission belongs to a different mode or faction.");
                if (mission.Encounter == EncounterKind.WorldCollision && mission.MaximumSquad <= 5)
                    Flag("COLLISION_SQUAD",mission.Id,"World Collision requires an authored larger squad.");
                if (mission.EnvironmentKey == null)
                    Flag("MISSING_ENVIRONMENT",mission.Id,"Assign a reviewed world-specific scene key before release.");
                else
                {
                    scenes.Add(mission.EnvironmentKey);
                    if (environmentPrefix != null &&
                        !mission.EnvironmentKey.StartsWith(environmentPrefix,StringComparison.Ordinal))
                        Flag("CROSS_WORLD_ENVIRONMENT",mission.Id,"Scene key does not match the requested world prefix.");
                }

                var primary=mission.Objectives[0];
                objectives.Add(primary.Kind);
                if (primary.Kind == ObjectiveKind.Stabilize && primary.Interaction != null &&
                    mission.Deployment.Any(p =>
                        FieldPoint.Distance(p,primary.Interaction.Point) <= primary.Interaction.Reach))
                    Flag("OBJECTIVE_AT_DEPLOYMENT",mission.Id,
                        "A starting unit can reach the primary stabilization objective immediately.");

                string layout=LayoutFingerprint(mission.Map);
                layouts.Add(layout);
                if (previous == layout)
                    Flag("CONSECUTIVE_MAP_REPEAT",mission.Id,
                        "The previous campaign mission uses identical field bounds, cover and slow terrain.");
                else if (encounteredLayouts.TryGetValue(layout,out var first))
                    Flag("REVISITED_MAP",mission.Id,
                        "This arena layout was already used in " + first + "; confirm that the return is intentional.",
                        blockingOnRelease:false);
                if (!encounteredLayouts.ContainsKey(layout))
                    encounteredLayouts.Add(layout,mission.Id);
                previous=layout;
            }

            if (release && layouts.Count < 3)
                Flag("MAP_VARIETY",null,"Release campaign needs at least three distinct authored field layouts.");
            if (release && scenes.Count < 3)
                Flag("ENVIRONMENT_VARIETY",null,"Release campaign needs at least three distinct world-specific scene treatments.");
            if (release && objectives.Count < 3)
                Flag("OBJECTIVE_VARIETY",null,"Release campaign needs at least three distinct primary objective types.");

            return new CampaignProductionReport(campaign.Id,stage,missions.Length,
                layouts.Count,scenes.Count,objectives.Count,findings);
        }

        // Invariant deterministic geometry signature for production review.
        // Crucially exclude actors, spawn positions and objective points:
        // swapping only spawn positions does not constitute a new battlefield.
        private static string LayoutFingerprint(BattlefieldMap map)
        {
            string N(decimal n)=>n.ToString(CultureInfo.InvariantCulture);
            string Box(FieldBox b)=>string.Join(",",new[] {
                N(b.Min.X),N(b.Min.Z),N(b.Max.X),N(b.Max.Z)
            });
            string obstacles=string.Join(";",map.Obstacles.Select(o =>
                Box(o.Bounds)+":"+(o.BlocksMovement?"1":"0")+
                (o.BlocksShots?"1":"0")+":"+N(o.CoverReduction))
                .OrderBy(x=>x,StringComparer.Ordinal));
            string terrain=string.Join(";",map.Ground.Select(g =>
                Box(g.Bounds)+":"+N(g.CostPerMeter))
                .OrderBy(x=>x,StringComparer.Ordinal));
            return Box(map.Bounds)+"|"+obstacles+"|"+terrain;
        }
    }
}
