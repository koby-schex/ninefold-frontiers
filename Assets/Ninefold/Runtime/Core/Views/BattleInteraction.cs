using System;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;

namespace Ninefold.Core.Views
{
    public enum IntentKind { Movement, Ability, Interaction }
    public enum InputOutcome { Selected, Previewed, Committed, Cancelled, Waiting, Locked, Rejected, Stale, NoPreview }

    public sealed class BattleIntent
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public IntentKind Kind { get; }
        public long ActivationId { get; }
        public string ActorId { get; }
        public MovementPreview Movement { get; }
        public AbilitySlot? Slot { get; }
        public TargetPreview Target { get; }
        public string ObjectiveId { get; }
        internal object Version { get; }
        internal BattleIntent(BattleView view, IntentKind kind, MovementPreview movement = null,
            AbilitySlot? slot = null, TargetPreview target = null, string objective = null)
        {
            Version = view.Version; ActivationId = view.Activation.ActivationId; ActorId = view.Activation.UnitId;
            Kind = kind; Movement = movement; Slot = slot; Target = target; ObjectiveId = objective;
        }
    }
    public sealed class InteractionView
    {
        public BattleView Battle { get; }
        public string SelectedUnitId { get; }
        public AbilitySlot? SelectedAbility { get; }
        public BattleIntent Pending { get; }
        public string AnimationId { get; }
        public bool IsAnimating => AnimationId != null;
        public bool CanConfirm => Pending != null && !IsAnimating;
        internal InteractionView(BattleView battle, string unit, AbilitySlot? ability, BattleIntent pending, string animation)
        { Battle = battle; SelectedUnitId = unit; SelectedAbility = ability; Pending = pending; AnimationId = animation; }
    }
    public sealed class BattleInputResult
    {
        public InputOutcome Outcome { get; }
        public BattleUpdate Update { get; }
        public string AnimationId { get; }
        public FieldFailure FieldFailure { get; }
        public HealthActionFailure HealthFailure { get; }
        public InteractionFailure InteractionFailure { get; }
        internal BattleInputResult(InputOutcome outcome, BattleUpdate update = null, string animation = null,
            FieldFailure field = FieldFailure.None, HealthActionFailure health = HealthActionFailure.None,
            InteractionFailure interaction = InteractionFailure.None)
        { Outcome = outcome; Update = update; AnimationId = animation; FieldFailure = field; HealthFailure = health; InteractionFailure = interaction; }
    }

    /// <summary>Single-threaded input state machine. Own one per session; renderer supplies picking and animation completion.</summary>
    public sealed class BattleInteraction
    {
        private readonly BattlePresentation presentation;
        private string selectedUnit, animationId;
        private AbilitySlot? selectedAbility;
        private object choiceVersion;
        private BattleIntent pending;
        public BattleInteraction(BattlePresentation presentation)
        { this.presentation = presentation ?? throw new ArgumentNullException(nameof(presentation)); }

        public InteractionView Read()
        {
            var view = Synchronize();
            return new InteractionView(view,selectedUnit,selectedAbility,pending,animationId);
        }
        private BattleView Synchronize()
        {
            var view = presentation.Read();
            if (choiceVersion != null && !ReferenceEquals(choiceVersion,view.Version)) ClearChoices();
            if (!view.Units.Any(u => u.Id == selectedUnit && u.IsEligible)) selectedUnit = null;
            return view;
        }
        private void ClearChoices() { pending = null; selectedAbility = null; choiceVersion = null; }
        private static BattleInputResult Result(InputOutcome outcome) => new BattleInputResult(outcome);
        private static bool Player(BattleView view) => view.Phase == BattleSessionPhase.PlayerInput;

        // Inspection is separate from targeting; selecting an enemy never grants control of it.
        public BattleInputResult InspectUnit(string unitId)
        {
            if (animationId != null) return Result(InputOutcome.Locked);
            var view = Synchronize(); ClearChoices();
            if (!view.Units.Any(u => u.Id == unitId && u.IsEligible)) return Result(InputOutcome.Rejected);
            selectedUnit = unitId; return Result(InputOutcome.Selected);
        }
        public BattleInputResult SelectAbility(AbilitySlot slot)
        {
            if (animationId != null) return Result(InputOutcome.Locked);
            var view = Synchronize(); ClearChoices();
            if (!Player(view) || !view.Actions.Any(a => a.Slot == slot && a.CanSelect)) return Result(InputOutcome.Rejected);
            selectedUnit = view.Activation.UnitId; selectedAbility = slot; choiceVersion = view.Version;
            return Result(InputOutcome.Selected);
        }
        public BattleInputResult TapUnit(string unitId)
        {
            if (animationId != null) return Result(InputOutcome.Locked);
            var view = Synchronize();
            if (!selectedAbility.HasValue) return InspectUnit(unitId);
            if (!Player(view)) return Result(InputOutcome.Rejected);
            if (pending?.Kind == IntentKind.Ability && pending.Target.TargetId == unitId) return Confirm(pending.Id);
            pending = null;
            var preview = presentation.PreviewTarget(view.Activation.ActivationId,selectedAbility.Value,unitId);
            if (!preview.IsValid) return new BattleInputResult(InputOutcome.Rejected,field:preview.FieldFailure,health:preview.HealthFailure);
            pending = new BattleIntent(view,IntentKind.Ability,slot:selectedAbility,target:preview);
            return Result(InputOutcome.Previewed);
        }
        public BattleInputResult TapDestination(FieldPoint destination)
        {
            if (animationId != null) return Result(InputOutcome.Locked);
            var view = Synchronize();
            if (!Player(view)) return Result(InputOutcome.Rejected);
            if (pending?.Kind == IntentKind.Movement && pending.Movement.Path.Last().Equals(destination)) return Confirm(pending.Id);
            ClearChoices();
            if (!presentation.TryPreviewDestination(view.Activation.ActivationId,destination,out var movement,out var failure))
                return new BattleInputResult(InputOutcome.Rejected,field:failure);
            selectedUnit = view.Activation.UnitId; choiceVersion = view.Version;
            pending = new BattleIntent(view,IntentKind.Movement,movement:movement); return Result(InputOutcome.Previewed);
        }
        public BattleInputResult TapObjective(string objectiveId)
        {
            if (animationId != null) return Result(InputOutcome.Locked);
            var view = Synchronize();
            if (!Player(view)) return Result(InputOutcome.Rejected);
            if (pending?.Kind == IntentKind.Interaction && pending.ObjectiveId == objectiveId) return Confirm(pending.Id);
            ClearChoices();
            var failure = presentation.PreviewInteraction(view.Activation.ActivationId,objectiveId);
            if (failure != InteractionFailure.None) return new BattleInputResult(InputOutcome.Rejected,interaction:failure);
            selectedUnit = view.Activation.UnitId; choiceVersion = view.Version;
            pending = new BattleIntent(view,IntentKind.Interaction,objective:objectiveId); return Result(InputOutcome.Previewed);
        }
        // Buttons must carry the ID of the preview they display; an old click cannot confirm a replacement preview.
        public BattleInputResult Confirm(string previewId)
        {
            if (animationId != null) return Result(InputOutcome.Locked);
            if (pending == null) return Result(InputOutcome.NoPreview);
            if (previewId != pending.Id) return Result(InputOutcome.Stale);
            var intent = pending; var current = presentation.Read();
            ClearChoices(); // Consume before executing, including on save exceptions.
            if (!ReferenceEquals(intent.Version,current.Version) || !Player(current) || current.Activation.ActivationId != intent.ActivationId)
                return Result(InputOutcome.Stale);
            BattleUpdate update; bool success; FieldFailure field = FieldFailure.None;
            HealthActionFailure health = HealthActionFailure.None; InteractionFailure interaction = InteractionFailure.None;
            switch (intent.Kind)
            {
                case IntentKind.Movement:
                    success = presentation.TryMove(intent.ActivationId,intent.Movement.Path,out update,out field); break;
                case IntentKind.Ability:
                    success = presentation.TryUseAbility(intent.ActivationId,intent.Slot.Value,intent.Target.TargetId,out update,out field,out health); break;
                default:
                    success = presentation.TryInteract(intent.ActivationId,intent.ObjectiveId,out update,out interaction); break;
            }
            return success ? Committed(update) : new BattleInputResult(InputOutcome.Rejected,field:field,health:health,interaction:interaction);
        }
        public BattleInputResult Cancel()
        {
            if (animationId != null) return Result(InputOutcome.Locked);
            Synchronize(); ClearChoices(); selectedUnit = null; return Result(InputOutcome.Cancelled);
        }
        public BattleInputResult EndTurn(long activationId)
        {
            if (animationId != null) return Result(InputOutcome.Locked);
            var view = Synchronize();
            if (!Player(view) || view.Activation.ActivationId != activationId) return Result(InputOutcome.Stale);
            ClearChoices(); return Committed(presentation.EndPlayerTurn(activationId));
        }
        public BattleInputResult Advance(EnemyPerception perception = null)
        {
            if (animationId != null) return Result(InputOutcome.Locked);
            var view = Synchronize();
            if (Player(view)) return Result(InputOutcome.Waiting); // Never erase a player's preview while waiting.
            ClearChoices(); var update = presentation.Advance(perception);
            return update.Events.Count == 0 ? new BattleInputResult(InputOutcome.Waiting,update) : Committed(update);
        }
        private BattleInputResult Committed(BattleUpdate update)
        {
            animationId = update.Events.Count == 0 ? null : Guid.NewGuid().ToString("N");
            return new BattleInputResult(InputOutcome.Committed,update,animationId);
        }
        public bool CompleteAnimation(string id)
        {
            if (animationId == null || id != animationId) return false;
            animationId = null; return true;
        }
    }
}
