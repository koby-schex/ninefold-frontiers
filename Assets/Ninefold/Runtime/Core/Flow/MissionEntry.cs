using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Progression;

namespace Ninefold.Core.Flow
{
    public sealed class RosterUnit
    {
        public string Id { get; }
        public string FactionId { get; }
        public bool IsApex { get; }
        public UnitUnlockDefinition Unlock { get; }
        public UnitAdvancementDefinition Advancement { get; }
        public UnitCustomizationDefinition Customization { get; }
        public RosterUnit(string id, string factionId, bool isApex, UnitUnlockDefinition unlock, UnitAdvancementDefinition advancement = null, UnitCustomizationDefinition customization = null)
        {
            RewardRules.Id(id); RewardRules.Id(factionId);
            if (unlock == null || unlock.UnitId != id) throw new ArgumentException("Matching unlock definition required.");
            if (advancement != null && (advancement.UnitId != id || advancement.FragmentResourceId != unlock.FragmentResourceId))
                throw new ArgumentException("Advancement must match this unit and its fragment resource.");
            if (customization != null && customization.UnitId != id) throw new ArgumentException("Customization must match this unit.");
            Id = id; FactionId = factionId; IsApex = isApex; Unlock = unlock; Advancement = advancement; Customization = customization;
        }
    }
    public enum SquadFailure { None, MissionLocked, Size, UnknownUnit, DuplicateUnit, LockedUnit, MultipleApex, WrongFaction, CampaignRoster }

    /// <summary>Trusted authored entry. Null faction permits mixed squads; a campaign requires a faction.</summary>
    public sealed class MissionEntry
    {
        public string Id { get; }
        public int MinimumSquad { get; }
        public int MaximumSquad { get; }
        public string FactionId { get; }
        public bool IsCampaign { get; }
        public MissionRewardPolicy Rewards { get; }
        private readonly Func<PlayerProgress, bool> available;
        private readonly Func<string, IReadOnlyList<string>, BattleTurnController> factory;
        public MissionEntry(string id, int minimumSquad, int maximumSquad, string factionId, bool isCampaign,
            MissionRewardPolicy rewards, Func<string, IReadOnlyList<string>, BattleTurnController> factory,
            Func<PlayerProgress, bool> available = null)
        {
            RewardRules.Id(id);
            if (minimumSquad < 1 || maximumSquad < minimumSquad || maximumSquad > 4096) throw new ArgumentOutOfRangeException(nameof(maximumSquad));
            if (factionId != null) RewardRules.Id(factionId);
            if (isCampaign && factionId == null) throw new ArgumentException("Campaign faction required.");
            if (rewards == null || rewards.MissionId != id) throw new ArgumentException("Matching reward policy required.");
            Id = id; MinimumSquad = minimumSquad; MaximumSquad = maximumSquad; FactionId = factionId; IsCampaign = isCampaign;
            Rewards = rewards; this.factory = factory ?? throw new ArgumentNullException(nameof(factory)); this.available = available ?? (_ => true);
        }
        public bool IsAvailable(PlayerProgress progress) => available(progress ?? throw new ArgumentNullException(nameof(progress)));
        internal BattleTurnController Build(string attempt, IReadOnlyList<string> squad) => factory(attempt, squad);
        internal SquadFailure Validate(PlayerProgress progress, IReadOnlyDictionary<string, RosterUnit> roster, string[] squad, bool replay = false)
        {
            if (!replay && !IsAvailable(progress)) return SquadFailure.MissionLocked;
            if (squad.Length < MinimumSquad || squad.Length > MaximumSquad) return SquadFailure.Size;
            if (squad.Any(id => id == null || !roster.ContainsKey(id))) return SquadFailure.UnknownUnit;
            if (squad.Distinct(StringComparer.Ordinal).Count() != squad.Length) return SquadFailure.DuplicateUnit;
            var units = squad.Select(id => roster[id]).ToArray();
            if (units.Any(u => !progress.Owns(u.Id))) return SquadFailure.LockedUnit;
            if (units.Count(u => u.IsApex) > 1) return SquadFailure.MultipleApex;
            if (FactionId != null && units.Any(u => u.FactionId != FactionId)) return SquadFailure.WrongFaction;
            // Entry gate uses owned standard units, not the size of an individual mission's squad.
            if (IsCampaign && roster.Values.Count(u => progress.Owns(u.Id) && !u.IsApex && u.FactionId == FactionId) < 3) return SquadFailure.CampaignRoster;
            return SquadFailure.None;
        }
    }
}
