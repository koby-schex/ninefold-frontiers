using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Progression;

namespace Ninefold.Core.Content
{
    // Neither classification asserts canon approval. Canon review is a separate human gate.
    public enum ContentClassification { AbstractFixture, CanonCandidate }
    public sealed class ContentManifest
    {
        public const int SchemaVersion = 1;
        public string PackageId { get; }
        public string ContentRevision { get; }
        public string BalanceRevision { get; }
        public ContentClassification Classification { get; }
        public string CanonSource { get; }
        public string SaveRevision { get; }
        public ContentManifest(string packageId, string contentRevision, string balanceRevision,
            ContentClassification classification, string canonSource = null)
        {
            RewardRules.Id(packageId); RewardRules.Id(contentRevision); RewardRules.Id(balanceRevision);
            if (!Enum.IsDefined(typeof(ContentClassification), classification)) throw new ArgumentOutOfRangeException(nameof(classification));
            if (classification == ContentClassification.CanonCandidate && string.IsNullOrWhiteSpace(canonSource))
                throw new ArgumentException("A canon candidate needs a source reference, not an inferred approval.");
            if (canonSource != null) RewardRules.Id(canonSource);
            PackageId = packageId; ContentRevision = contentRevision; BalanceRevision = balanceRevision;
            Classification = classification; CanonSource = canonSource;
            // Length-framed fields avoid delimiter collisions; revision discipline remains the author's responsibility.
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            { writer.Write(SchemaVersion); writer.Write(packageId); writer.Write(contentRevision); writer.Write(balanceRevision); writer.Write((int)classification); writer.Write(canonSource ?? ""); }
            using var hash = SHA256.Create();
            SaveRevision = "catalog-" + BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
        }
    }
    public sealed class AbilityContent
    {
        public string Id { get; }
        public AbilitySlot Slot { get; }
        public FieldAbility Effect { get; }
        public PassiveDefinition Passive { get; }
        public bool IsPassivePlaceholder => Slot == AbilitySlot.Passive && Passive == null;
        public AbilityContent(string id, PassiveDefinition passive)
        { ContentIds.Check(id); Id = id; Passive = passive ?? throw new ArgumentNullException(nameof(passive)); Slot = AbilitySlot.Passive; }
        public AbilityContent(string id, FieldAbility effect)
        { ContentIds.Check(id); Id = id; Effect = effect ?? throw new ArgumentNullException(nameof(effect)); Slot = effect.Slot; }
        private AbilityContent(string id) { ContentIds.Check(id); Id = id; Slot = AbilitySlot.Passive; }
        public static AbilityContent PassivePlaceholder(string id) => new AbilityContent(id);
    }
    public sealed class KitContent
    {
        public string Id { get; }
        // Index equals AbilitySlot. Inputs are copied; callers cannot change a validated catalog.
        public IReadOnlyList<string> AbilityIds { get; }
        public UnitAbilityDefinition Availability { get; }
        public KitContent(string id, string passive, string normal, string main, string signature, UnitAbilityDefinition availability)
        {
            ContentIds.Check(id); Id = id;
            AbilityIds = ContentIds.Copy(new[] { passive, normal, main, signature });
            Availability = availability ?? throw new ArgumentNullException(nameof(availability));
            if (availability.SignatureRequirement == SignatureReadiness.ExternalCondition)
                throw new ArgumentException("Catalog v1 has no external signature-condition binding.");
        }
    }
    public sealed class UnitContent
    {
        public string Id { get; }
        public string KitId { get; }
        public int Health { get; }
        public int Armor { get; }
        public int Initiative { get; }
        public decimal Movement { get; }
        public FieldBody Body { get; }
        public RosterUnit Roster { get; } // null: NPC template, never collectible
        public UnitContent(string id, string kitId, int health, int armor, int initiative, decimal movement, FieldBody body, RosterUnit roster = null)
        {
            ContentIds.Check(id); ContentIds.Check(kitId);
            _ = new UnitHealthDefinition("validation", health, armor); _ = new UnitTurnDefinition(id, initiative, movement);
            if (roster != null && roster.Id != id) throw new ArgumentException("Roster ID must match unit content.");
            // Prevent any supported deployment modifier overflowing the integer stats.
            foreach (int value in new[] { health, armor }) DeploymentModifiers.Scale(value, DeploymentModifiers.ProvisionalMaximumBasisPoints);
            Id = id; KitId = kitId; Health = health; Armor = armor; Initiative = initiative; Movement = movement;
            Body = body ?? throw new ArgumentNullException(nameof(body)); Roster = roster;
        }
    }
    public sealed class ActorSpawn
    {
        public string Id { get; }
        public string UnitId { get; }
        public FieldPoint Position { get; }
        public bool IsHostile { get; }
        public EnemyBehavior Behavior { get; }
        public ActorSpawn(string id, string unitId, FieldPoint position, bool isHostile, EnemyBehavior behavior = null)
        {
            ContentIds.Check(id); ContentIds.Check(unitId);
            if (isHostile != (behavior != null)) throw new ArgumentException("Hostiles require a brain; friendly NPCs cannot have an enemy brain.");
            Id = id; UnitId = unitId; Position = position; IsHostile = isHostile; Behavior = behavior;
        }
    }
    public sealed class MissionContent
    {
        public string Id { get; }
        public string FactionId { get; }
        public bool IsCampaign { get; }
        public int MinimumSquad { get; }
        public int MaximumSquad => Deployment.Count;
        public BattlefieldMap Map { get; }
        public IReadOnlyList<FieldPoint> Deployment { get; }
        public IReadOnlyList<ActorSpawn> Actors { get; }
        public IReadOnlyList<ObjectiveDefinition> Objectives { get; }
        public OutcomePriority Priority { get; }
        public MissionContent(string id, string factionId, bool isCampaign, int minimumSquad, BattlefieldMap map,
            IEnumerable<FieldPoint> deployment, IEnumerable<ActorSpawn> actors, ObjectiveDefinition primary,
            OutcomePriority priority = OutcomePriority.FailureFirst, IEnumerable<ObjectiveDefinition> optional = null)
        {
            ContentIds.Check(id); if (factionId != null) ContentIds.Check(factionId);
            if (isCampaign && factionId == null) throw new ArgumentException("Campaign mission requires a faction.");
            var points = deployment?.ToArray() ?? throw new ArgumentNullException(nameof(deployment));
            if (minimumSquad < 1 || points.Length < minimumSquad || points.Length > 8) throw new ArgumentException("Catalog squads must have 1–8 deployment slots.");
            var spawns = ContentIds.Items(actors, a => a.Id);
            var definition = new MissionDefinition(id, "validation", new[] { "$leader" }, primary, priority, optional);
            Id = id; FactionId = factionId; IsCampaign = isCampaign; MinimumSquad = minimumSquad;
            Map = map ?? throw new ArgumentNullException(nameof(map)); Deployment = Array.AsReadOnly(points);
            Actors = Array.AsReadOnly(spawns); Objectives = definition.Objectives; Priority = priority;
        }
    }
    internal static class ContentIds
    {
        internal static void Check(string id)
        { RewardRules.Id(id); if (id.StartsWith("$", StringComparison.Ordinal)) throw new ArgumentException("Content IDs cannot use reserved objective aliases."); }
        internal static IReadOnlyList<string> Copy(IEnumerable<string> values)
        {
            var a = values?.ToArray() ?? throw new ArgumentNullException(nameof(values));
            foreach (var id in a) Check(id);
            if (a.Length > 4096 || a.Distinct(StringComparer.Ordinal).Count() != a.Length) throw new ArgumentException("Duplicate or excessive content IDs.");
            return Array.AsReadOnly(a);
        }
        internal static T[] Items<T>(IEnumerable<T> source, Func<T, string> id) where T : class
        {
            var a = source?.ToArray() ?? throw new ArgumentNullException(nameof(source));
            if (a.Any(x => x == null)) throw new ArgumentException("Null content entry.");
            Copy(a.Select(id)); return a;
        }
    }
}
