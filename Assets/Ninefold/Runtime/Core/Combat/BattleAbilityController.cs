using System;
using System.Collections.Generic;

namespace Ninefold.Core.Combat
{
    /// <summary>
    /// One instance owned by each battle. Availability/cost commit only: callers must
    /// validate targets and effect prerequisites before TryUse. Single-threaded.
    /// </summary>
    public sealed class BattleAbilityController
    {
        private sealed class AbilityState
        {
            internal readonly UnitAbilityDefinition Definition;
            internal long MainReadyAtActivation;
            internal bool SignatureRequirementMet;
            internal bool SignatureUsed;

            internal AbilityState(UnitAbilityDefinition definition)
            {
                Definition = definition;
                SignatureRequirementMet = definition.SignatureRequirement == SignatureReadiness.ReadyAtDeployment;
            }
        }

        private readonly BattleTurnController turns;
        private readonly Dictionary<string, AbilityState> kits =
            new Dictionary<string, AbilityState>(StringComparer.Ordinal);

        internal BattleAbilityController(BattleTurnController turns)
        {
            this.turns = turns;
        }

        /// <summary>Attach once before the unit's first activation. Never resets a used kit.</summary>
        public void RegisterKit(string unitId, UnitAbilityDefinition definition)
        {
            EnsureBattleOpen();
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (!turns.IsUnitEligible(unitId))
                throw new InvalidOperationException("Removed units cannot register a kit.");
            if (kits.ContainsKey(unitId))
                throw new InvalidOperationException("This unit already has a kit.");
            if (turns.GetActivationCount(unitId) != 0)
                throw new InvalidOperationException("Register a kit before its first activation.");
            kits.Add(unitId, new AbilityState(definition));
        }

        public AbilityStateView GetState(string unitId)
        {
            var state = FindKit(unitId);
            long remaining = Math.Max(0L, state.MainReadyAtActivation - turns.GetActivationCount(unitId));
            return new AbilityStateView(unitId, checked((int)remaining),
                state.SignatureRequirementMet, state.SignatureUsed);
        }

        /// <summary>Trusted battle-event hook, not player input. Readiness latches; never refunds use.</summary>
        public void MarkSignatureConditionMet(string unitId)
        {
            EnsureBattleOpen();
            var state = FindKit(unitId);
            if (!turns.IsUnitEligible(unitId))
                throw new InvalidOperationException("Removed units cannot receive readiness events.");
            if (state.Definition.SignatureRequirement != SignatureReadiness.ExternalCondition)
                throw new InvalidOperationException("This kit requires a different readiness trigger.");
            state.SignatureRequirementMet = true;
        }

        /// <summary>Pure preview. None means costs/readiness allow use, not that a target is legal.</summary>
        public AbilityUseFailure GetAvailability(long activationId, AbilitySlot slot)
        {
            if (turns.IsBattleEnded)
                return AbilityUseFailure.BattleEnded;
            var turn = turns.CurrentActivation;
            if (turn == null)
                return AbilityUseFailure.NoActiveActivation;
            if (turn.ActivationId != activationId)
                return AbilityUseFailure.StaleActivation;
            if (!Enum.IsDefined(typeof(AbilitySlot), slot))
                return AbilityUseFailure.UnsupportedSlot;
            if (slot == AbilitySlot.Passive)
                return AbilityUseFailure.PassiveCannotBeActivated;
            if (!kits.TryGetValue(turn.UnitId, out var state))
                return AbilityUseFailure.NoRegisteredKit;
            if (!turn.PrimaryActionAvailable)
                return AbilityUseFailure.PrimaryActionSpent;
            if (slot == AbilitySlot.Main && state.MainReadyAtActivation > turns.GetActivationCount(turn.UnitId))
                return AbilityUseFailure.MainOnCooldown;
            if (slot == AbilitySlot.Signature)
            {
                if (state.SignatureUsed)
                    return AbilityUseFailure.SignatureAlreadyUsed;
                if (!state.SignatureRequirementMet)
                    return AbilityUseFailure.SignatureNotReady;
            }
            return AbilityUseFailure.None;
        }

        /// <summary>Accepted use spends one action, starts cooldown/marks use, and latches prerequisites.</summary>
        public bool TryUse(long activationId, AbilitySlot slot, out AbilityUseFailure failure)
        {
            failure = GetAvailability(activationId, slot);
            if (failure != AbilityUseFailure.None)
                return false;
            var turn = turns.CurrentActivation;
            var state = kits[turn.UnitId];
            // Calculate before spending, so arithmetic overflow cannot partially consume an action.
            long readyAt = slot == AbilitySlot.Main
                ? checked(turns.GetActivationCount(turn.UnitId) + state.Definition.MainCooldownTurns)
                : state.MainReadyAtActivation;
            turns.SpendPrimaryAction(activationId);
            state.MainReadyAtActivation = readyAt;
            if (slot == AbilitySlot.Signature)
                state.SignatureUsed = true;
            if ((slot == AbilitySlot.NormalAttack && state.Definition.SignatureRequirement == SignatureReadiness.AfterNormalAttack)
                || (slot == AbilitySlot.Main && state.Definition.SignatureRequirement == SignatureReadiness.AfterMainAbility))
                state.SignatureRequirementMet = true;
            return true;
        }

        private AbilityState FindKit(string unitId)
        {
            if (unitId == null)
                throw new ArgumentNullException(nameof(unitId));
            if (!kits.TryGetValue(unitId, out var state))
                throw new ArgumentException("No registered kit for this unit.", nameof(unitId));
            return state;
        }

        private void EnsureBattleOpen()
        {
            if (turns.IsBattleEnded)
                throw new InvalidOperationException("The battle has ended.");
        }
    }
}
