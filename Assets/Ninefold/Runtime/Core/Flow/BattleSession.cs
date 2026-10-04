using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;

namespace Ninefold.Core.Flow
{
    public enum BattleSessionPhase { Selection, AdvanceRequired, PlayerInput, EnemyInput, Results }
    public sealed class EnemyPerception
    {
        public IReadOnlyList<string> Opponents { get; }
        public IReadOnlyList<string> Allies { get; }
        public EnemyPerception(IEnumerable<string> opponents, IEnumerable<string> allies)
        { Opponents = Copy(opponents); Allies = Copy(allies); }
        private static IReadOnlyList<string> Copy(IEnumerable<string> ids)
        {
            var a = ids?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(ids));
            if (a.Length > 8 || a.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Perception needs at most eight nonblank IDs per list.");
            return Array.AsReadOnly(a);
        }
    }
    public sealed class BattleSessionView
    {
        public BattleSessionPhase Phase { get; }
        public int Round { get; }
        public ActivationView Activation { get; }
        public BattleResult Result { get; }
        internal BattleSessionView(BattleSessionPhase phase, BattleTurnController battle)
        { Phase = phase; Round = battle?.RoundNumber ?? 0; Activation = battle?.CurrentActivation; Result = battle?.Mission?.Result; }
    }
    /// <summary>Single-threaded saved session adapter. No timers or automatic loops; each Advance commits at most one step.</summary>
    public sealed class BattleSession
    {
        private readonly MissionFlow flow;
        private string attemptId;
        public BattleSession(MissionFlow flow) { this.flow = flow ?? throw new ArgumentNullException(nameof(flow)); }
        public BattleSessionView Read()
        {
            var b = ReadSnapshot();
            return new BattleSessionView(Phase(b), b);
        }
        internal BattleTurnController ReadSnapshot()
        {
            var b = flow.ReadBattle(); Bind(b); Phase(b); return b;
        }
        internal BattleTurnController ReadPlayerSnapshot(long activationId)
        { var b = ReadSnapshot(); if (b == null) throw new InvalidOperationException("No battle."); RequirePlayer(b,activationId); return b; }
        private void Bind(BattleTurnController b)
        {
            if (b == null) return;
            string id = b.Mission?.Definition.AttemptId ?? throw new InvalidOperationException("Session requires a mission.");
            if (attemptId != null && attemptId != id) throw new InvalidOperationException("Create a new session for a new mission attempt.");
            attemptId = id;
        }
        internal BattleSessionPhase Phase(BattleTurnController b)
        {
            if (b == null) return BattleSessionPhase.Selection;
            if (b.Mission?.Result != null) return BattleSessionPhase.Results;
            if (b.Mission == null || b.Battlefield == null) throw new InvalidOperationException("Session requires a mission and battlefield.");
            var active = b.CurrentActivation;
            if (active == null) return BattleSessionPhase.AdvanceRequired;
            bool enemy = b.Battlefield.IsEnemyControlled(active.UnitId);
            if (b.Mission.Definition.Squad.Contains(active.UnitId))
            {
                if (enemy) throw new InvalidOperationException("A player squad unit cannot also be AI-controlled.");
                return BattleSessionPhase.PlayerInput;
            }
            return enemy ? BattleSessionPhase.EnemyInput : BattleSessionPhase.AdvanceRequired;
        }
        /// <summary>Wait for players; run one perceived enemy turn, pass an uncontrolled NPC, or advance one scheduler boundary.</summary>
        public BattleSessionView Advance(EnemyPerception perception = null) => AdvanceDetailed(perception,out _);
        internal BattleSessionView AdvanceDetailed(EnemyPerception perception, out EnemyTurnResult enemyResult)
        {
            enemyResult = null; EnemyTurnResult completed = null;
            var view = Read();
            if (view.Phase == BattleSessionPhase.Selection || view.Phase == BattleSessionPhase.Results || view.Phase == BattleSessionPhase.PlayerInput ||
                (view.Phase == BattleSessionPhase.EnemyInput && perception == null)) return view;
            flow.Execute(b => {
                if (b.Mission.Evaluate() != null) return;
                if (b.CurrentActivation != null)
                {
                    if (Phase(b) == BattleSessionPhase.EnemyInput)
                    {
                        if (!b.Battlefield.TryRunEnemyTurn(b.CurrentActivation.ActivationId, perception.Opponents, perception.Allies, out completed, out var failure))
                            throw new InvalidOperationException("Enemy command failed: " + failure);
                    }
                    else b.EndActivation(b.CurrentActivation.ActivationId); // Friendly/non-squad NPCs hold position in this adapter.
                }
                else if (b.RoundNumber == 0) b.StartNextRound();
                else if (b.IsRoundComplete)
                {
                    if (b.Mission.LastResolvedRound != b.RoundNumber) b.Mission.ResolveRoundEnd(b.RoundNumber);
                    else b.StartNextRound();
                }
                else b.BeginNextActivation();
                b.Mission.Evaluate();
            });
            enemyResult = completed;
            return Read();
        }
        private void RequirePlayer(BattleTurnController b, long activationId)
        {
            Bind(b);
            if (Phase(b) != BattleSessionPhase.PlayerInput || b.CurrentActivation.ActivationId != activationId)
                throw new InvalidOperationException("Command must match the active player activation.");
        }
        private sealed class RejectedCommand : Exception { }
        public bool TryMove(long activationId, IEnumerable<FieldPoint> path, out MovementPreview result, out FieldFailure failure)
        {
            MovementPreview value = null; FieldFailure reason = FieldFailure.None;
            // Bound/copy the caller's route before entering the save transaction.
            var route = path?.Take(257).ToArray();
            bool success = PlayerCommand(activationId, b => b.Battlefield.TryMove(activationId, route, out value, out reason));
            result = value; failure = reason; return success;
        }
        public bool TryUseAbility(long activationId, AbilitySlot slot, string target,
            out AbilityEffectPreview result, out FieldFailure failure, out HealthActionFailure healthFailure)
        {
            AbilityEffectPreview value = null; FieldFailure reason = FieldFailure.None; HealthActionFailure health = HealthActionFailure.None;
            bool success = PlayerCommand(activationId, b => b.Battlefield.TryUseAbility(activationId, slot, target, out value, out reason, out health));
            result = value; failure = reason; healthFailure = health; return success;
        }
        public bool TryInteract(long activationId, string objective, out InteractionFailure failure)
        {
            InteractionFailure reason = InteractionFailure.None;
            bool success = PlayerCommand(activationId, b => b.Mission.TryInteract(activationId, objective, out reason));
            failure = reason; return success;
        }
        public BattleSessionView EndPlayerTurn(long activationId)
        {
            flow.Execute(b => { RequirePlayer(b, activationId); b.EndActivation(activationId); b.Mission.Evaluate(); });
            return Read();
        }
        private bool PlayerCommand(long activationId, Func<BattleTurnController, bool> command)
        {
            try
            {
                flow.Execute(b => { RequirePlayer(b, activationId); if (!command(b)) throw new RejectedCommand(); });
                return true;
            }
            catch (RejectedCommand) { return false; } // Invalid commands never produce a checkpoint.
        }
    }
}
