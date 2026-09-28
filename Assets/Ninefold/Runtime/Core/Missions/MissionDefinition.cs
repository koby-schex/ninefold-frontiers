using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;

namespace Ninefold.Core.Missions
{
    public enum ObjectiveKind { Defeat, Protect, RescueAndExtract, Secure, Stabilize, Survive }
    public enum ObjectiveStatus { Active, Completed, Failed }
    public enum OutcomePriority { FailureFirst, SuccessFirst }
    public enum MissionOutcome { Victory, Defeat }
    public enum InteractionFailure { None, MissionClosed, InactiveTurn, UnknownObjective, Unavailable, IneligibleActor, ActionSpent, OutOfReach, Obstructed }

    public sealed class InteractionDefinition
    {
        public FieldPoint Point { get; }
        public decimal Reach { get; }
        public IReadOnlyList<string> AllowedActors { get; }
        // This slice supports exactly one primary action, never an ability-slot use.
        public int PrimaryActionCost => 1;
        public InteractionDefinition(FieldPoint point, decimal reach, IEnumerable<string> allowedActors)
        {
            if (reach < 0m || reach > 10000m) throw new ArgumentOutOfRangeException(nameof(reach));
            Point = point; Reach = reach; AllowedActors = ObjectiveDefinition.Ids(allowedActors, false);
        }
    }

    public sealed class ObjectiveDefinition
    {
        public string Id { get; }
        public ObjectiveKind Kind { get; }
        public int Required { get; }
        public IReadOnlyList<string> Subjects { get; }
        public IReadOnlyList<string> Contestants { get; }
        public FieldBox Zone { get; }
        public InteractionDefinition Interaction { get; }
        public bool Consecutive { get; }
        private ObjectiveDefinition(string id, ObjectiveKind kind, int required, IEnumerable<string> subjects = null,
            FieldBox zone = null, InteractionDefinition interaction = null, IEnumerable<string> contestants = null, bool consecutive = false)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Objective ID required.");
            if (required < 1) throw new ArgumentOutOfRangeException(nameof(required));
            Id = id; Kind = kind; Required = required;
            Subjects = Ids(subjects ?? Array.Empty<string>(),true); Contestants = Ids(contestants ?? Array.Empty<string>(),true);
            Zone = zone; Interaction = interaction; Consecutive = consecutive;
        }
        internal static IReadOnlyList<string> Ids(IEnumerable<string> ids, bool emptyAllowed)
        {
            var copy = (ids ?? throw new ArgumentNullException(nameof(ids))).ToArray();
            if ((!emptyAllowed && copy.Length == 0) || copy.Any(string.IsNullOrWhiteSpace)
                || copy.Distinct(StringComparer.Ordinal).Count() != copy.Length) throw new ArgumentException("IDs must be nonblank and unique.");
            return Array.AsReadOnly(copy);
        }
        public static ObjectiveDefinition Defeat(string id, IEnumerable<string> targets)
        {
            var copy = Ids(targets,false); return new ObjectiveDefinition(id,ObjectiveKind.Defeat,copy.Count,copy);
        }
        public static ObjectiveDefinition Protect(string id, string target, int rounds)
            => new ObjectiveDefinition(id,ObjectiveKind.Protect,rounds,new[] { target });
        public static ObjectiveDefinition Rescue(string id, string target, FieldBox exit, InteractionDefinition rescue)
            => new ObjectiveDefinition(id,ObjectiveKind.RescueAndExtract,1,new[] { target },
                exit ?? throw new ArgumentNullException(nameof(exit)),rescue ?? throw new ArgumentNullException(nameof(rescue)));
        public static ObjectiveDefinition Secure(string id, FieldBox zone, IEnumerable<string> holders,
            IEnumerable<string> contestants, int rounds, bool consecutive)
        {
            var copy = Ids(holders,false); var opposition = Ids(contestants,true);
            if (copy.Intersect(opposition,StringComparer.Ordinal).Any()) throw new ArgumentException("Holder cannot also contest.");
            return new ObjectiveDefinition(id,ObjectiveKind.Secure,rounds,copy,zone ?? throw new ArgumentNullException(nameof(zone)),
                contestants:opposition,consecutive:consecutive);
        }
        public static ObjectiveDefinition Stabilize(string id, int interactions, InteractionDefinition interaction)
            => new ObjectiveDefinition(id,ObjectiveKind.Stabilize,interactions,interaction:interaction ?? throw new ArgumentNullException(nameof(interaction)));
        public static ObjectiveDefinition Survive(string id, int rounds) => new ObjectiveDefinition(id,ObjectiveKind.Survive,rounds);
    }

    public sealed class MissionDefinition
    {
        public string MissionId { get; }
        public string AttemptId { get; }
        public IReadOnlyList<string> Squad { get; }
        public IReadOnlyList<ObjectiveDefinition> Objectives { get; }
        public OutcomePriority Priority { get; }
        public MissionDefinition(string missionId, string attemptId, IEnumerable<string> squad, ObjectiveDefinition primary,
            OutcomePriority priority, IEnumerable<ObjectiveDefinition> optional = null)
        {
            if (string.IsNullOrWhiteSpace(missionId) || string.IsNullOrWhiteSpace(attemptId)) throw new ArgumentException("Mission and attempt IDs required.");
            if (!Enum.IsDefined(typeof(OutcomePriority),priority)) throw new ArgumentOutOfRangeException(nameof(priority));
            if (primary == null) throw new ArgumentNullException(nameof(primary));
            var bonuses = (optional ?? Array.Empty<ObjectiveDefinition>()).ToArray();
            if (bonuses.Length > 2 || bonuses.Any(x => x == null)) throw new ArgumentException("At most two non-null optional objectives.");
            var all = new[] { primary }.Concat(bonuses).ToArray();
            if (all.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != all.Length) throw new ArgumentException("Duplicate objective ID.");
            MissionId = missionId; AttemptId = attemptId; Squad = ObjectiveDefinition.Ids(squad,false);
            Objectives = Array.AsReadOnly(all); Priority = priority;
        }
    }

    public sealed class ObjectiveView
    {
        public string Id { get; }
        public ObjectiveKind Kind { get; }
        public ObjectiveStatus Status { get; }
        public int Progress { get; }
        public int Required { get; }
        public bool Rescued { get; }
        internal ObjectiveView(ObjectiveDefinition definition, ObjectiveStatus status, int progress, bool rescued)
        { Id = definition.Id; Kind = definition.Kind; Required = definition.Required; Status = status; Progress = progress; Rescued = rescued; }
    }
    public sealed class BattleResult
    {
        public string MissionId { get; }
        public string AttemptId { get; }
        public MissionOutcome Outcome { get; }
        public int Round { get; }
        public string Reason { get; }
        public IReadOnlyList<ObjectiveView> Objectives { get; }
        internal BattleResult(MissionDefinition definition, MissionOutcome outcome, int round, string reason, ObjectiveView[] objectives)
        {
            MissionId = definition.MissionId; AttemptId = definition.AttemptId; Outcome = outcome; Round = round;
            Reason = reason; Objectives = Array.AsReadOnly(objectives);
        }
    }
}
