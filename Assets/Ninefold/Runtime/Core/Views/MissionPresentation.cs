using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Persistence;
using Ninefold.Core.Progression;

namespace Ninefold.Core.Views
{
    /// <summary>Owns one catalog-bound flow and transient preparation choices. Single UI/simulation thread only.</summary>
    public sealed class MissionPresentation
    {
        private readonly ContentCatalog catalog;
        private readonly MissionFlow flow;
        private string missionId, planId;
        private string[] squad = Array.Empty<string>();
        private PlayerProgress planProgress;
        private BattleInteraction interaction;
        public bool NeedsReload => flow.NeedsReload;
        public MissionPresentation(ContentCatalog catalog, IBattleSaveFiles battles, IProgressSaveFiles profiles, string profileId)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            flow = catalog.CreateFlow(battles,profiles,profileId);
        }
        public void CreateProfile() { flow.CreateProfile(catalog.StarterUnits); Reset(); }
        public void Open() { flow.Open(); Reset(); }
        private void Reset() { missionId = null; squad = Array.Empty<string>(); planId = null; planProgress = null; interaction = null; }
        private void Ready() { if (flow.NeedsReload) throw new InvalidOperationException("Open/recover the profile before using mission screens."); }
        private void Selection()
        { Ready(); if (flow.Phase != MissionFlowPhase.Selection) throw new InvalidOperationException("Finish and claim the current battle before preparing another."); }
        private void NewPlan() { planId = Guid.NewGuid().ToString("N"); planProgress = flow.Progress; }
        public void SelectMission(string id)
        {
            Selection();
            if (id == null || !catalog.Missions.ContainsKey(id)) throw new ArgumentException("Unknown mission.");
            missionId = id; squad = Array.Empty<string>(); NewPlan();
        }
        public void SetSquad(IEnumerable<string> units)
        {
            Selection(); if (missionId == null) throw new InvalidOperationException("Select a mission first.");
            var copy = units?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(units));
            if (copy.Length > 8) throw new ArgumentException("At most eight selection entries allowed.");
            squad = copy; NewPlan(); // Invalid drafts are displayed with the authoritative validation reason.
        }
        public MissionMenuView Read()
        {
            Ready();
            if (missionId != null && !ReferenceEquals(planProgress,flow.Progress)) NewPlan();
            var cards = catalog.Missions.Values.OrderBy(m => m.Id,StringComparer.Ordinal).Select(m => new MissionCard(catalog,flow,m)).ToArray();
            var selected = missionId == null ? null : catalog.Missions[missionId];
            var units = catalog.Units.Values.Where(u => u.Roster != null).OrderBy(u => u.Id,StringComparer.Ordinal)
                .Select(u => new PreparationUnit(catalog,u,flow.Progress,selected)).ToArray();
            var battle = flow.ReadBattle();
            var resume = battle != null && flow.Phase == MissionFlowPhase.Battle
                ? new BattlePresentation(new BattleSession(flow)).Read() : null;
            var results = battle?.Mission?.Result == null ? null : new MissionResultsView(battle.Mission.Result,catalog.Rewards[battle.Mission.Definition.MissionId],flow.Progress);
            return new MissionMenuView(flow.Phase,flow.Recovered,flow.Progress,cards,units,missionId,squad,planId,
                missionId == null ? (SquadFailure?)null : flow.ValidateSquad(missionId,squad),resume,results,
                catalog.Campaigns.Select(c => flow.InspectCampaign(c.Id)).ToArray());
        }
        public BattleInteraction Start(string displayedPlanId)
        {
            Selection();
            if (missionId == null || displayedPlanId == null || displayedPlanId != planId || !ReferenceEquals(planProgress,flow.Progress))
                throw new InvalidOperationException("Preparation changed; refresh before starting.");
            if (flow.ValidateSquad(missionId,squad) != SquadFailure.None) throw new InvalidOperationException("Invalid squad or locked mission.");
            flow.Start(missionId,squad); Reset(); return ResumeBattle();
        }
        public BattleInteraction ResumeBattle()
        {
            Ready(); if (flow.Phase != MissionFlowPhase.Battle) throw new InvalidOperationException("No active battle to resume.");
            return interaction ?? (interaction = new BattleInteraction(new BattlePresentation(new BattleSession(flow))));
        }
        public MissionRewardCollection ClaimRewards(string attemptId)
        {
            Ready(); var result = flow.ReadBattle()?.Mission?.Result;
            if (flow.Phase != MissionFlowPhase.Results || result == null || result.AttemptId != attemptId)
                throw new InvalidOperationException("Claim must match the displayed terminal attempt.");
            var before = flow.Progress;
            bool duplicate = before.Claims.Any(c => c.AttemptId == attemptId);
            var receipt = flow.ClaimRewards();
            return new MissionRewardCollection(receipt,before,flow.Progress,duplicate);
        }
        public void ReturnToSelection(string attemptId)
        {
            Ready();
            if (flow.Receipt == null || flow.Receipt.AttemptId != attemptId) throw new InvalidOperationException("Claim this attempt before leaving results.");
            flow.ReturnToSelection(); Reset();
        }
        // Completion rewards remain an explicit, separately persisted campaign claim.
        public CampaignClaimResult ClaimCampaign(string campaignId)
        { Selection(); var result = flow.ClaimCampaign(campaignId); NewPlan(); return result; }
    }
}
