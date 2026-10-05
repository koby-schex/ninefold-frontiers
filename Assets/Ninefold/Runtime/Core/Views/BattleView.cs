using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;

namespace Ninefold.Core.Views
{
    public enum ActionBlock { None, NotPlayerTurn, MissingProfile, Rules }
    public sealed class BattleActionView
    {
        public AbilitySlot Slot { get; }
        public ActionBlock Block { get; }
        public AbilityUseFailure RuleFailure { get; }
        public bool CanSelect => Block == ActionBlock.None;
        internal BattleActionView(AbilitySlot slot, ActionBlock block, AbilityUseFailure failure)
        { Slot = slot; Block = block; RuleFailure = failure; }
    }
    public sealed class BattleUnitView
    {
        public string Id { get; }
        public bool IsPlayerSquad { get; }
        public bool IsEnemyControlled { get; }
        public bool IsEligible { get; }
        public FieldPoint? Position { get; }
        public FieldBody Body { get; }
        public HealthStateView Health { get; }
        public int? EffectiveArmor { get; }
        public AbilityStateView Kit { get; }
        public IReadOnlyList<FieldAbility> Abilities { get; }
        public IReadOnlyList<StatusView> Statuses { get; }
        internal BattleUnitView(BattleTurnController b, string id)
        {
            Id = id; IsPlayerSquad = b.Mission.Definition.Squad.Contains(id); IsEnemyControlled = b.Battlefield.IsEnemyControlled(id); IsEligible = b.IsUnitEligible(id);
            if (b.Battlefield.InspectPlacement(id,out var p,out var body,out var profiles)) { Position = p; Body = body; }
            Abilities = profiles; b.Health.TryGetState(id,out var health); Health = health;
            EffectiveArmor = health == null ? (int?)null : b.Statuses.EffectiveArmor(id,health.Armor);
            Kit = b.Abilities.InspectKit(id); Statuses = b.Statuses.GetStatuses(id);
        }
    }
    public sealed class BattleView
    {
        internal object Version { get; }
        public string AttemptId { get; }
        public string MissionId { get; }
        public BattleSessionPhase Phase { get; }
        public int Round { get; }
        public ActivationView Activation { get; }
        public BattlefieldMap Map { get; }
        public IReadOnlyList<string> UpcomingTurns { get; }
        public IReadOnlyList<BattleUnitView> Units { get; }
        public IReadOnlyList<UnitHealthOverlay> HealthOverlays { get; }
        public IReadOnlyList<BattleActionView> Actions { get; }
        public IReadOnlyList<ObjectiveView> Objectives { get; }
        public BattleResult Result { get; }
        internal BattleView(BattleTurnController b, BattleSessionPhase phase, object version)
        {
            Version = version;
            Phase = phase; Round = b?.RoundNumber ?? 0; Activation = b?.CurrentActivation; Map = b?.Battlefield?.Map; Result = b?.Mission?.Result;
            AttemptId = b?.Mission?.Definition.AttemptId; MissionId = b?.Mission?.Definition.MissionId;
            UpcomingTurns = Array.AsReadOnly(b?.InspectUpcoming() ?? Array.Empty<string>());
            Units = Array.AsReadOnly(b == null ? Array.Empty<BattleUnitView>() : b.InspectUnitIds().Select(id => new BattleUnitView(b,id)).ToArray());
            HealthOverlays = Array.AsReadOnly(Units.Where(u => u.IsEligible && u.Position.HasValue && u.Health != null && !u.Health.IsDefeated).Select(u => new UnitHealthOverlay(u)).ToArray());
            Objectives = Array.AsReadOnly(b == null ? Array.Empty<ObjectiveView>() : b.Mission.Definition.Objectives.Select(o => b.Mission.GetObjective(o.Id)).ToArray());
            var actor = Units.FirstOrDefault(u => u.Id == Activation?.UnitId);
            Actions = Array.AsReadOnly(Enum.GetValues(typeof(AbilitySlot)).Cast<AbilitySlot>().Select(slot => {
                if (phase != BattleSessionPhase.PlayerInput) return new BattleActionView(slot,ActionBlock.NotPlayerTurn,AbilityUseFailure.NoActiveActivation);
                var failure = b.Abilities.GetAvailability(Activation.ActivationId,slot);
                if (failure != AbilityUseFailure.None) return new BattleActionView(slot,ActionBlock.Rules,failure);
                return new BattleActionView(slot,actor.Abilities.Any(a => a.Slot == slot) ? ActionBlock.None : ActionBlock.MissingProfile,AbilityUseFailure.None);
            }).ToArray());
        }
    }
    /// <summary>Always available without selection. Screen placement and accessible styling belong to the renderer.</summary>
    public sealed class UnitHealthOverlay
    {
        public string UnitId { get; }
        public string TeamId { get; }
        public FieldPoint Position { get; }
        public decimal Height { get; }
        public int CurrentHealth { get; }
        public int MaximumHealth { get; }
        public decimal Fraction => (decimal)CurrentHealth / MaximumHealth;
        public bool IsPlayerSquad { get; }
        public bool IsEnemyControlled { get; }
        internal UnitHealthOverlay(BattleUnitView unit)
        {
            UnitId = unit.Id; TeamId = unit.Health.TeamId; Position = unit.Position.Value; Height = unit.Body.Height;
            CurrentHealth = unit.Health.CurrentHealth; MaximumHealth = unit.Health.MaximumHealth;
            IsPlayerSquad = unit.IsPlayerSquad; IsEnemyControlled = unit.IsEnemyControlled;
        }
    }
    public sealed class TargetPreview
    {
        public string TargetId { get; }
        public bool IsValid { get; }
        public AbilityEffectPreview Effect { get; }
        public FieldFailure FieldFailure { get; }
        public HealthActionFailure HealthFailure { get; }
        internal TargetPreview(string target, bool valid, AbilityEffectPreview effect, FieldFailure field, HealthActionFailure health)
        { TargetId = target; IsValid = valid; Effect = effect; FieldFailure = field; HealthFailure = health; }
    }
    public enum BattleEventKind { Movement, AbilityUsed, Interaction, HealthChanged, StatusChanged, UnitRemoved, ActivationEnded, RoundStarted, ActivationStarted, ObjectiveChanged, BattleEnded }
    /// <summary>Post-commit animation hint. Before/after views remain the authority for net state.</summary>
    public sealed class BattleEvent
    {
        public BattleEventKind Kind { get; }
        public string UnitId { get; }
        public string TargetId { get; }
        public string ObjectiveId { get; }
        public AbilitySlot? Slot { get; }
        public MovementPreview Movement { get; }
        public AbilityEffectPreview AbilityEffect { get; }
        internal BattleEvent(BattleEventKind kind, string unit = null, string target = null, string objective = null,
            AbilitySlot? slot = null, MovementPreview movement = null, AbilityEffectPreview effect = null)
        { Kind = kind; UnitId = unit; TargetId = target; ObjectiveId = objective; Slot = slot; Movement = movement; AbilityEffect = effect; }
    }
    public sealed class BattleUpdate
    {
        public BattleView Before { get; }
        public BattleView After { get; }
        public IReadOnlyList<BattleEvent> Events { get; }
        internal BattleUpdate(BattleView before, BattleView after, IEnumerable<BattleEvent> events)
        { Before = before; After = after; Events = Array.AsReadOnly(events.ToArray()); }
    }
}
