using System;
using System.Collections.Generic;
using System.Linq;

namespace Ninefold.Core.Combat
{
    public enum PassiveTrigger { OwnerActivationStarted, OwnerSurvivedDamage }
    public enum StatusStacking { Refresh, AddStackAndRefresh }
    public sealed class StatusDefinition
    {
        public string Id { get; }
        public int ArmorBasisPoints { get; }
        public int PowerBasisPoints { get; }
        public int Duration { get; }
        public int MaximumStacks { get; }
        public StatusStacking Stacking { get; }
        public StatusDefinition(string id, int armorBasisPoints, int powerBasisPoints, int duration, int maximumStacks = 1,
            StatusStacking stacking = StatusStacking.Refresh)
        {
            StatusIds.Check(id);
            if (armorBasisPoints < -5000 || armorBasisPoints > 5000 || powerBasisPoints < -5000 || powerBasisPoints > 5000 || (armorBasisPoints == 0 && powerBasisPoints == 0))
                throw new ArgumentOutOfRangeException(nameof(armorBasisPoints));
            if (duration < 1 || duration > 100 || maximumStacks < 1 || maximumStacks > 10) throw new ArgumentOutOfRangeException(nameof(duration));
            if (!Enum.IsDefined(typeof(StatusStacking), stacking) || (stacking == StatusStacking.Refresh && maximumStacks != 1)) throw new ArgumentException("Invalid stacking rule.");
            Id = id; ArmorBasisPoints = armorBasisPoints; PowerBasisPoints = powerBasisPoints; Duration = duration; MaximumStacks = maximumStacks; Stacking = stacking;
        }
    }
    /// <summary>One self-targeted passive rule per owner. Effects never emit damage, healing or more passive events.</summary>
    public sealed class PassiveDefinition
    {
        public PassiveTrigger Trigger { get; }
        public string StatusId { get; }
        public PassiveDefinition(PassiveTrigger trigger, string statusId)
        {
            if (!Enum.IsDefined(typeof(PassiveTrigger), trigger)) throw new ArgumentOutOfRangeException(nameof(trigger));
            StatusIds.Check(statusId); Trigger = trigger; StatusId = statusId;
        }
    }
    public sealed class StatusView
    {
        public string Id { get; }
        public int Stacks { get; }
        public int RemainingOwnerActivations { get; }
        internal StatusView(string id, int stacks, int remaining) { Id = id; Stacks = stacks; RemainingOwnerActivations = remaining; }
    }
    internal static class StatusIds
    {
        internal static void Check(string id)
        { if (string.IsNullOrWhiteSpace(id) || id.Length > 1024) throw new ArgumentException("Bounded status ID required."); }
    }
    /// <summary>Trusted simulation rules; UI must not apply/cleanse arbitrary effects. Single simulation thread.</summary>
    public sealed partial class BattleStatusController
    {
        private sealed class State
        {
            internal int Stacks;
            internal int Remaining;
        }
        private readonly BattleTurnController turns;
        private readonly Dictionary<string, StatusDefinition> definitions = new Dictionary<string, StatusDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, PassiveDefinition> passives = new Dictionary<string, PassiveDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, State>> active = new Dictionary<string, Dictionary<string, State>>(StringComparer.Ordinal);
        internal BattleStatusController(BattleTurnController turns) { this.turns = turns; }
        public void RegisterStatus(StatusDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (turns.RoundNumber != 0 || turns.IsBattleEnded || definitions.Count >= 4096) throw new InvalidOperationException("Register bounded status definitions before battle starts.");
            definitions.Add(definition.Id, definition);
        }
        public void RegisterPassive(string owner, PassiveDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (turns.RoundNumber != 0 || turns.IsBattleEnded || !turns.IsUnitEligible(owner)) throw new InvalidOperationException("Register passive before battle starts.");
            if (!definitions.ContainsKey(definition.StatusId)) throw new ArgumentException("Unknown passive status.");
            if (!turns.Health.TryGetState(owner, out _)) throw new ArgumentException("Passive owner needs health.");
            passives.Add(owner, definition);
        }
        public IReadOnlyList<StatusView> GetStatuses(string owner)
        {
            turns.IsUnitEligible(owner); // Reject unknown owner even when it has no effects.
            return Array.AsReadOnly(active.TryGetValue(owner, out var list)
                ? list.OrderBy(e => e.Key, StringComparer.Ordinal).Select(e => new StatusView(e.Key, e.Value.Stacks, e.Value.Remaining)).ToArray()
                : Array.Empty<StatusView>());
        }
        /// <summary>Apply one stack; refresh duration. Return false for defeated/extracted owners. No action cost.</summary>
        public bool Apply(string owner, string statusId)
        {
            if (turns.IsBattleEnded) throw new InvalidOperationException("Battle ended.");
            if (statusId == null || !definitions.TryGetValue(statusId, out var definition)) throw new ArgumentException("Unknown status.");
            if (!turns.IsUnitEligible(owner)) return false;
            if (!turns.Health.TryGetState(owner, out var health)) throw new ArgumentException("Status owner needs health.");
            if (health.IsDefeated) return false;
            if (!active.TryGetValue(owner, out var list)) active.Add(owner, list = new Dictionary<string, State>(StringComparer.Ordinal));
            if (!list.TryGetValue(statusId, out var state)) list.Add(statusId, state = new State());
            state.Stacks = definition.Stacking == StatusStacking.Refresh ? 1 : Math.Min(definition.MaximumStacks, state.Stacks + 1);
            state.Remaining = definition.Duration; return true;
        }
        public bool Remove(string owner, string statusId)
        {
            if (turns.IsBattleEnded) throw new InvalidOperationException("Battle ended.");
            turns.IsUnitEligible(owner);
            if (statusId == null || !definitions.ContainsKey(statusId)) throw new ArgumentException("Unknown status.");
            if (!active.TryGetValue(owner, out var list)) return false;
            bool removed = list.Remove(statusId); if (list.Count == 0) active.Remove(owner); return removed;
        }
        internal void Clear(string owner) => active.Remove(owner);
        internal void Trigger(string owner, PassiveTrigger trigger)
        {
            if (passives.TryGetValue(owner, out var rule) && rule.Trigger == trigger) Apply(owner, rule.StatusId);
        }
        internal void EndActivation(string owner)
        {
            if (!active.TryGetValue(owner, out var list)) return;
            foreach (var pair in list.ToArray()) if (--pair.Value.Remaining == 0) list.Remove(pair.Key);
            if (list.Count == 0) active.Remove(owner);
        }
        public int EffectiveArmor(string owner, int baseArmor) => Scale(baseArmor, Total(owner, true), false);
        public int EffectivePower(string owner, int basePower) => Scale(basePower, Total(owner, false), true);
        private int Total(string owner, bool armor)
        {
            if (!active.TryGetValue(owner, out var list)) return 0;
            long total = list.Sum(e => (long)(armor ? definitions[e.Key].ArmorBasisPoints : definitions[e.Key].PowerBasisPoints) * e.Value.Stacks);
            return (int)Math.Max(-5000L, Math.Min(5000L, total));
        }
        private static int Scale(int value, int basisPoints, bool power)
        {
            if (value < (power ? 1 : 0)) throw new ArgumentOutOfRangeException(nameof(value));
            decimal scaled = value + decimal.Truncate(value * (decimal)basisPoints / 10000m);
            return (int)Math.Max(power ? 1m : 0m, Math.Min(int.MaxValue, scaled));
        }
    }
}
