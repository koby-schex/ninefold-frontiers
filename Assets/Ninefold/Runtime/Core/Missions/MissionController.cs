using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;

namespace Ninefold.Core.Missions
{
    /// <summary>Finite mission contracts. All mutations occur on the battle simulation thread.</summary>
    public sealed class MissionController
    {
        private sealed class State
        {
            internal readonly ObjectiveDefinition Definition;
            internal ObjectiveStatus Status;
            internal int Progress;
            internal bool Rescued;
            internal State(ObjectiveDefinition definition) { Definition = definition; }
            internal ObjectiveView View() => new ObjectiveView(Definition,Status,Progress,Rescued);
        }
        private readonly BattleTurnController turns;
        private readonly State[] states;
        public MissionDefinition Definition { get; }
        public BattleResult Result { get; private set; }
        public int LastResolvedRound { get; private set; }
        internal MissionController(BattleTurnController turns, MissionDefinition definition)
        {
            this.turns = turns; Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (turns.Battlefield == null) throw new InvalidOperationException("Configure battlefield first.");
            states = definition.Objectives.Select(x => new State(x)).ToArray();
            var referenced = definition.Squad.Concat(states.SelectMany(s => s.Definition.Subjects.Concat(s.Definition.Contestants)
                .Concat(s.Definition.Interaction?.AllowedActors ?? Array.Empty<string>()))).Distinct(StringComparer.Ordinal);
            foreach (string id in referenced)
            {
                if (!turns.IsUnitEligible(id) || turns.Health.GetState(id).IsDefeated) throw new ArgumentException("Mission actors must start alive and eligible.");
                turns.Battlefield.GetPosition(id); // Fail setup before publishing a partially configured mission.
            }
        }
        public ObjectiveView GetObjective(string id) => states.First(s => s.Definition.Id == id).View();

        public InteractionFailure PreviewInteraction(long activationId, string objectiveId)
        {
            if (Result != null || turns.IsBattleEnded) return InteractionFailure.MissionClosed;
            var turn = turns.CurrentActivation;
            if (turn == null || turn.ActivationId != activationId) return InteractionFailure.InactiveTurn;
            var state = states.FirstOrDefault(s => s.Definition.Id == objectiveId);
            if (state == null) return InteractionFailure.UnknownObjective;
            var interaction = state.Definition.Interaction;
            if (interaction == null || state.Status != ObjectiveStatus.Active || state.Rescued) return InteractionFailure.Unavailable;
            if (!interaction.AllowedActors.Contains(turn.UnitId) || !Alive(turn.UnitId)) return InteractionFailure.IneligibleActor;
            if (state.Definition.Kind == ObjectiveKind.RescueAndExtract && !Alive(state.Definition.Subjects[0])) return InteractionFailure.Unavailable;
            if (!turn.PrimaryActionAvailable) return InteractionFailure.ActionSpent;
            return turns.Battlefield.CheckInteraction(turn.UnitId,interaction.Point,interaction.Reach);
        }
        public bool TryInteract(long activationId, string objectiveId, out InteractionFailure failure)
        {
            failure = PreviewInteraction(activationId,objectiveId);
            if (failure != InteractionFailure.None) return false;
            var state = states.First(s => s.Definition.Id == objectiveId);
            turns.SpendPrimaryAction(activationId);
            if (state.Definition.Kind == ObjectiveKind.RescueAndExtract) state.Rescued = true;
            else state.Progress++;
            Evaluate();
            return true;
        }

        /// <summary>Call after each complete scripted event batch; ordinary moves/health effects call automatically.</summary>
        public BattleResult Evaluate()
        {
            if (Result != null) return Result;
            if (turns.IsBattleEnded || turns.RoundNumber == 0) return null;
            Refresh(false);
            return ResolveOutcome();
        }

        /// <summary>Caller resolves all due round-end effects first. Duplicate round checks are harmless.</summary>
        public BattleResult ResolveRoundEnd(int round)
        {
            if (Result != null) return Result;
            if (turns.IsBattleEnded) throw new InvalidOperationException("Battle was ended outside mission resolution.");
            if (round != turns.RoundNumber || round == 0 || !turns.IsRoundComplete)
                throw new InvalidOperationException("Only the current completed round can resolve.");
            if (LastResolvedRound == round) return Result;
            Refresh(true);
            LastResolvedRound = round;
            return ResolveOutcome();
        }
        private bool Alive(string id) => turns.IsUnitEligible(id) && !turns.Health.GetState(id).IsDefeated;
        private void Refresh(bool roundEnd)
        {
            foreach (var state in states)
            {
                if (state.Status != ObjectiveStatus.Active) continue;
                var d = state.Definition;
                switch (d.Kind)
                {
                    case ObjectiveKind.Defeat:
                        state.Progress = d.Subjects.Count(id => turns.Health.GetState(id).IsDefeated);
                        if (d.Subjects.Any(id => !turns.IsUnitEligible(id) && !turns.Health.GetState(id).IsDefeated)) state.Status = ObjectiveStatus.Failed;
                        break;
                    case ObjectiveKind.Protect:
                    case ObjectiveKind.RescueAndExtract:
                        if (!Alive(d.Subjects[0])) { state.Status = ObjectiveStatus.Failed; break; }
                        if (d.Kind == ObjectiveKind.Protect && roundEnd) state.Progress++;
                        if (d.Kind == ObjectiveKind.RescueAndExtract && state.Rescued && turns.Battlefield.IsInside(d.Subjects[0],d.Zone)) state.Progress = 1;
                        break;
                    case ObjectiveKind.Secure:
                        if (roundEnd)
                        {
                            bool held = d.Subjects.Any(id => Alive(id) && turns.Battlefield.IsInside(id,d.Zone));
                            bool contested = d.Contestants.Any(id => Alive(id) && turns.Battlefield.OverlapsZone(id,d.Zone));
                            if (held && !contested) state.Progress++;
                            else if (d.Consecutive) state.Progress = 0;
                        }
                        break;
                    case ObjectiveKind.Survive:
                        if (roundEnd && Definition.Squad.Any(Alive)) state.Progress++;
                        break;
                }
                if (state.Status == ObjectiveStatus.Active && state.Progress >= d.Required) state.Status = ObjectiveStatus.Completed;
            }
        }
        private BattleResult ResolveOutcome()
        {
            bool squadLost = !Definition.Squad.Any(Alive);
            bool failed = squadLost || states[0].Status == ObjectiveStatus.Failed;
            bool won = states[0].Status == ObjectiveStatus.Completed;
            if (!won && !failed) return null;
            bool victory = won && (!failed || Definition.Priority == OutcomePriority.SuccessFirst);
            Result = new BattleResult(Definition,victory ? MissionOutcome.Victory : MissionOutcome.Defeat,turns.RoundNumber,
                victory ? "PrimaryCompleted" : squadLost ? "SquadUnavailable" : "PrimaryFailed",states.Select(s => s.View()).ToArray());
            turns.EndBattle();
            return Result;
        }
    }
}
