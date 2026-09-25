using System;
using System.Collections.Generic;
using System.Linq;

namespace Ninefold.Core.Combat
{
    /// <summary>
    /// Single-threaded battle turn authority. No clocks, Unity types, damage or geometry.
    /// Round transitions are explicit so objective/event resolution can happen between rounds.
    /// </summary>
    public sealed class BattleTurnController
    {
        private sealed class UnitState
        {
            internal readonly UnitTurnDefinition Definition;
            internal int Initiative;
            internal bool Eligible = true;

            internal UnitState(UnitTurnDefinition definition)
            {
                Definition = definition;
                Initiative = definition.Initiative;
            }
        }

        private readonly Dictionary<string, UnitState> units =
            new Dictionary<string, UnitState>(StringComparer.Ordinal);
        private string[] roundOrder = Array.Empty<string>();
        private int nextIndex;
        private long activationSequence;
        private ActivationView active;

        public int RoundNumber { get; private set; }
        public bool IsBattleEnded { get; private set; }
        public ActivationView CurrentActivation => active;
        public IReadOnlyList<string> RoundOrder => Array.AsReadOnly(roundOrder);

        public bool IsRoundComplete
        {
            get
            {
                if (RoundNumber == 0 || active != null)
                    return false;
                for (int i = nextIndex; i < roundOrder.Length; i++)
                    if (units[roundOrder[i]].Eligible)
                        return false;
                return true;
            }
        }

        public BattleTurnController(IEnumerable<UnitTurnDefinition> initialUnits)
        {
            if (initialUnits == null)
                throw new ArgumentNullException(nameof(initialUnits));
            foreach (var unit in initialUnits)
                RegisterUnit(unit);
        }

        /// <summary>New units enter the next round snapshot, never the current queue.</summary>
        public void RegisterUnit(UnitTurnDefinition unit)
        {
            EnsureBattleOpen();
            if (unit == null)
                throw new ArgumentNullException(nameof(unit));
            if (units.ContainsKey(unit.UnitId))
                throw new ArgumentException("Battle-local IDs cannot be reused.", nameof(unit));
            units.Add(unit.UnitId, new UnitState(unit));
        }

        /// <summary>Changes take effect only when the next round order is constructed.</summary>
        public void SetInitiative(string unitId, int initiative)
        {
            EnsureBattleOpen();
            if (initiative < 0)
                throw new ArgumentOutOfRangeException(nameof(initiative));
            FindUnit(unitId).Initiative = initiative;
        }

        /// <summary>Defeat/extraction removes future eligibility and ends its active turn.</summary>
        public void RemoveUnit(string unitId)
        {
            EnsureBattleOpen();
            var unit = FindUnit(unitId);
            unit.Eligible = false;
            if (active != null && active.UnitId == unitId)
                active = null;
        }

        public void StartNextRound()
        {
            EnsureBattleOpen();
            if (RoundNumber != 0 && !IsRoundComplete)
                throw new InvalidOperationException("Finish the current round first.");
            var nextOrder = units.Values.Where(unit => unit.Eligible)
                .OrderByDescending(unit => unit.Initiative)
                .ThenBy(unit => unit.Definition.UnitId, StringComparer.Ordinal)
                .Select(unit => unit.Definition.UnitId).ToArray();
            if (nextOrder.Length == 0)
                throw new InvalidOperationException("A round needs at least one eligible unit.");
            int nextRound = checked(RoundNumber + 1);
            roundOrder = nextOrder;
            nextIndex = 0;
            RoundNumber = nextRound;
        }

        /// <summary>
        /// Returns null at the end of a round. Never refreshes an already active turn.
        /// The caller ends the current activation before requesting another.
        /// </summary>
        public ActivationView BeginNextActivation()
        {
            EnsureBattleOpen();
            if (RoundNumber == 0)
                throw new InvalidOperationException("Start a round first.");
            if (active != null)
                throw new InvalidOperationException("An activation is already in progress.");
            while (nextIndex < roundOrder.Length)
            {
                var unit = units[roundOrder[nextIndex]];
                if (!unit.Eligible)
                {
                    nextIndex++;
                    continue;
                }
                long nextActivationId = checked(activationSequence + 1);
                active = new ActivationView(nextActivationId, unit.Definition.UnitId,
                    RoundNumber, unit.Definition.MovementAllowance, true);
                activationSequence = nextActivationId;
                nextIndex++;
                return active;
            }
            return null;
        }

        /// <summary>Spend validated path cost, before or after the primary action.</summary>
        public void SpendMovement(long activationId, decimal cost)
        {
            var turn = RequireActivation(activationId);
            if (cost <= 0)
                throw new ArgumentOutOfRangeException(nameof(cost));
            if (cost > turn.MovementRemaining)
                throw new InvalidOperationException("Insufficient movement allowance.");
            active = new ActivationView(turn.ActivationId, turn.UnitId, turn.RoundNumber,
                turn.MovementRemaining - cost, turn.PrimaryActionAvailable);
        }

        /// <summary>Attack, main, Signature or interaction all spend this same action slot.</summary>
        public void SpendPrimaryAction(long activationId)
        {
            var turn = RequireActivation(activationId);
            if (!turn.PrimaryActionAvailable)
                throw new InvalidOperationException("The primary action is already spent.");
            active = new ActivationView(turn.ActivationId, turn.UnitId, turn.RoundNumber,
                turn.MovementRemaining, false);
        }

        /// <summary>Forfeits unused resources; they never carry over to another activation.</summary>
        public void EndActivation(long activationId)
        {
            RequireActivation(activationId);
            active = null;
        }

        /// <summary>Terminal for this controller. Victory and defeat are decided by missions.</summary>
        public void EndBattle()
        {
            if (IsBattleEnded)
                return;
            IsBattleEnded = true;
            active = null;
        }

        private UnitState FindUnit(string unitId)
        {
            if (unitId == null)
                throw new ArgumentNullException(nameof(unitId));
            if (!units.TryGetValue(unitId, out var unit))
                throw new ArgumentException("Unknown battle-local unit ID.", nameof(unitId));
            return unit;
        }

        private ActivationView RequireActivation(long activationId)
        {
            EnsureBattleOpen();
            if (active == null || active.ActivationId != activationId)
                throw new InvalidOperationException("Command does not match the active turn.");
            return active;
        }

        private void EnsureBattleOpen()
        {
            if (IsBattleEnded)
                throw new InvalidOperationException("The battle has ended.");
        }
    }
}
