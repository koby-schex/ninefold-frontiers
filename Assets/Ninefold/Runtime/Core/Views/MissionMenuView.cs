using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Campaigns;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Progression;

namespace Ninefold.Core.Views
{
    public sealed class MissionCard
    {
        public MissionContent Definition { get; }
        public string CampaignId { get; }
        public bool Available { get; }
        public bool FactionRosterLocked { get; }
        public int OwnedFactionStandards { get; }
        public IReadOnlyList<string> MissingPrerequisites { get; }
        public bool IsReplay { get; }
        public IReadOnlyList<ResourceGrant> VictoryRewards { get; }
        internal MissionCard(ContentCatalog catalog, MissionFlow flow, MissionContent mission)
        {
            Definition = mission; var p = flow.Progress;
            var campaign = catalog.Campaigns.SingleOrDefault(c => c.Missions.Any(m => m.MissionId == mission.Id));
            CampaignId = campaign?.Id; IsReplay = p.Missions.ContainsKey(mission.Id);
            OwnedFactionStandards = catalog.Units.Values.Count(u => u.Roster != null && !u.Roster.IsApex && u.Roster.FactionId == mission.FactionId && p.Owns(u.Id));
            FactionRosterLocked = mission.IsCampaign && OwnedFactionStandards < 3;
            MissingPrerequisites = Array.AsReadOnly(campaign == null || IsReplay ? Array.Empty<string>() :
                campaign.Missions.Single(m => m.MissionId == mission.Id).Prerequisites.Where(id => !p.Missions.ContainsKey(id)).ToArray());
            Available = !FactionRosterLocked && flow.CampaignCatalog.IsMissionAvailable(mission.Id,p);
            VictoryRewards = IsReplay ? catalog.Rewards[mission.Id].Replay : catalog.Rewards[mission.Id].FirstClear;
        }
    }
    public sealed class PreparationAbility
    {
        public string Id { get; }
        public AbilitySlot Slot { get; }
        public FieldAbility Effect { get; }
        public PassiveDefinition Passive { get; }
        public bool IsPassivePlaceholder { get; }
        internal PreparationAbility(AbilityContent ability, DeploymentModifiers bonus)
        {
            Id = ability.Id; Slot = ability.Slot; Passive = ability.Passive; IsPassivePlaceholder = ability.IsPassivePlaceholder;
            Effect = ability.Effect == null ? null : ability.Effect.WithAmount(ability.Effect.HasHealthEffect ? DeploymentModifiers.Scale(ability.Effect.Amount,bonus.Power) : 0);
        }
    }
    public sealed class PreparationUnit
    {
        public UnitContent Definition { get; }
        public bool Owned { get; }
        // Eligibility ignores slot count and other selected Apex units; squad validation handles composition.
        public bool EligibleForMission { get; }
        public int Health { get; }
        public int Armor { get; }
        public int Initiative => Definition.Initiative;
        public decimal Movement => Definition.Movement;
        public int Rank { get; }
        public string CustomizationId { get; }
        public IReadOnlyList<PreparationAbility> Abilities { get; }
        public UnitAbilityDefinition Availability { get; }
        internal PreparationUnit(ContentCatalog catalog, UnitContent unit, PlayerProgress progress, MissionContent selected)
        {
            Definition = unit; Owned = progress.Owns(unit.Id);
            EligibleForMission = Owned && selected != null && (selected.FactionId == null || selected.FactionId == unit.Roster.FactionId);
            var bonus = Owned ? progress.GetDeploymentModifiers(unit.Id) : DeploymentModifiers.Combine(AdvancementBonus.None,CustomizationBonus.None);
            Health = DeploymentModifiers.Scale(unit.Health,bonus.Health); Armor = DeploymentModifiers.Scale(unit.Armor,bonus.Armor);
            Rank = Owned ? progress.GetAdvancement(unit.Id).Rank : 0; CustomizationId = Owned ? progress.GetCustomization(unit.Id).OptionId : null;
            var kit = catalog.Kits[unit.KitId]; Availability = kit.Availability;
            Abilities = Array.AsReadOnly(kit.AbilityIds.Select(id => new PreparationAbility(catalog.Abilities[id],bonus)).ToArray());
        }
    }
    public sealed class MissionResultsView
    {
        public BattleResult Result { get; }
        public MissionClaim Receipt { get; }
        public bool Claimed => Receipt != null;
        public bool CanClaim => !Claimed;
        public bool IsFirstClear { get; }
        public IReadOnlyList<ResourceGrant> Grants { get; }
        public long SavedVictoryCount { get; }
        internal MissionResultsView(BattleResult result, MissionRewardPolicy policy, PlayerProgress progress)
        {
            Result = result; Receipt = progress.Claims.SingleOrDefault(c => c.AttemptId == result.AttemptId);
            IsFirstClear = Receipt?.IsFirstClear ?? (result.Outcome == MissionOutcome.Victory && !progress.Missions.ContainsKey(result.MissionId));
            Grants = Receipt?.Grants ?? (result.Outcome == MissionOutcome.Defeat ? Array.AsReadOnly(Array.Empty<ResourceGrant>()) : IsFirstClear ? policy.FirstClear : policy.Replay);
            SavedVictoryCount = progress.Missions.TryGetValue(result.MissionId,out var mission) ? mission.VictoryCount : 0;
        }
    }
    public sealed class MissionRewardCollection
    {
        public MissionClaim Receipt { get; }
        public PlayerProgress Before { get; }
        public PlayerProgress After { get; }
        public bool AlreadyClaimed { get; }
        internal MissionRewardCollection(MissionClaim receipt, PlayerProgress before, PlayerProgress after, bool duplicate)
        { Receipt = receipt; Before = before; After = after; AlreadyClaimed = duplicate; }
    }
    public sealed class MissionMenuView
    {
        public MissionFlowPhase Phase { get; }
        public bool Recovered { get; }
        public PlayerProgress Progress { get; }
        public IReadOnlyList<MissionCard> Missions { get; }
        public IReadOnlyList<PreparationUnit> Units { get; }
        public IReadOnlyList<CampaignProgress> Campaigns { get; }
        public string SelectedMissionId { get; }
        public IReadOnlyList<string> Squad { get; }
        public string PlanId { get; }
        public SquadFailure? SquadFailure { get; }
        public bool CanStart => Phase == MissionFlowPhase.Selection && SelectedMissionId != null && SquadFailure == Ninefold.Core.Flow.SquadFailure.None;
        public BattleView Resume { get; }
        public bool CanResume => Resume != null;
        public MissionResultsView Results { get; }
        internal MissionMenuView(MissionFlowPhase phase, bool recovered, PlayerProgress progress, MissionCard[] missions,
            PreparationUnit[] units, string selected, string[] squad, string plan, SquadFailure? failure, BattleView resume,
            MissionResultsView results, CampaignProgress[] campaigns)
        {
            Phase = phase; Recovered = recovered; Progress = progress; Missions = Array.AsReadOnly(missions); Units = Array.AsReadOnly(units);
            SelectedMissionId = selected; Squad = Array.AsReadOnly(squad.ToArray()); PlanId = plan; SquadFailure = failure;
            Resume = resume; Results = results; Campaigns = Array.AsReadOnly(campaigns);
        }
    }
}
