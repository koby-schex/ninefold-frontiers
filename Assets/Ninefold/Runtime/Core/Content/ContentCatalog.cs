using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Ninefold.Core.Campaigns;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Progression;
using Ninefold.Core.Persistence;

namespace Ninefold.Core.Content
{
    /// <summary>Immutable, typed content package. Construction validates references before any save I/O.</summary>
    public sealed class ContentCatalog
    {
        public ContentManifest Manifest { get; }
        public IReadOnlyList<string> Resources { get; }
        public IReadOnlyDictionary<string, StatusDefinition> Statuses { get; }
        public IReadOnlyDictionary<string, AbilityContent> Abilities { get; }
        public IReadOnlyDictionary<string, KitContent> Kits { get; }
        public IReadOnlyDictionary<string, UnitContent> Units { get; }
        public IReadOnlyDictionary<string, MissionContent> Missions { get; }
        public IReadOnlyDictionary<string, MissionRewardPolicy> Rewards { get; }
        public IReadOnlyList<CampaignDefinition> Campaigns { get; }
        public IReadOnlyList<string> StarterUnits { get; }
        public IReadOnlyList<MissionEntry> Entries { get; }
        public bool ContainsPassivePlaceholders => Abilities.Values.Any(a => a.IsPassivePlaceholder);

        public ContentCatalog(ContentManifest manifest, IEnumerable<string> resources, IEnumerable<AbilityContent> abilities,
            IEnumerable<KitContent> kits, IEnumerable<UnitContent> units, IEnumerable<MissionContent> missions,
            IEnumerable<MissionRewardPolicy> rewards, IEnumerable<CampaignDefinition> campaigns, IEnumerable<string> starterUnits, IEnumerable<StatusDefinition> statuses = null)
        {
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            Statuses = Index(statuses ?? Array.Empty<StatusDefinition>(), d => d.Id);
            Resources = ContentIds.Copy(resources); Abilities = Index(abilities, a => a.Id); Kits = Index(kits, k => k.Id);
            Units = Index(units, u => u.Id); Missions = Index(missions, m => m.Id); Rewards = Index(rewards, r => r.MissionId);
            Campaigns = Array.AsReadOnly(ContentIds.Items(campaigns, c => c.Id)); StarterUnits = ContentIds.Copy(starterUnits);
            foreach (var a in Abilities.Values.Where(a => a.Passive != null))
                if (!Statuses.ContainsKey(a.Passive.StatusId)) throw new ArgumentException("Ability " + a.Id + " references unknown passive status.");
            foreach (var k in Kits.Values)
                InContext("kit " + k.Id, () => {
                    for (int slot = 0; slot < 4; slot++)
                        if (!Abilities.TryGetValue(k.AbilityIds[slot], out var a) || (int)a.Slot != slot)
                            throw new ArgumentException("Missing ability or incorrect slot: " + k.AbilityIds[slot]);
                });
            foreach (var a in Abilities.Values.Where(a => a.Effect?.StatusId != null))
                if (!Statuses.ContainsKey(a.Effect.StatusId)) throw new ArgumentException("Ability " + a.Id + " references unknown active status.");
            foreach (var a in Abilities.Values.Where(a => a.Effect?.HasHealthEffect == true))
                InContext("ability " + a.Id, () => DeploymentModifiers.Scale(a.Effect.Amount, DeploymentModifiers.ProvisionalMaximumBasisPoints));
            foreach (var u in Units.Values)
                InContext("unit " + u.Id, () => {
                    if (!Kits.ContainsKey(u.KitId)) throw new ArgumentException("Unknown kit: " + u.KitId);
                    if (u.Roster != null) RequireResource(u.Roster.Unlock.FragmentResourceId);
                });
            var roster = Units.Values.Where(u => u.Roster != null).Select(u => u.Roster).ToArray();
            if (roster.Select(u => u.Unlock.FragmentResourceId).Distinct(StringComparer.Ordinal).Count() != roster.Length)
                throw new ArgumentException("Playable units must have distinct fragment resources.");
            foreach (var r in Rewards.Values)
                InContext("reward " + r.MissionId, () => {
                    if (!Missions.ContainsKey(r.MissionId)) throw new ArgumentException("Reward has no mission.");
                    foreach (var grant in r.FirstClear.Concat(r.Replay)) RequireResource(grant.ResourceId);
                });
            foreach (var c in Campaigns)
                InContext("campaign " + c.Id, () => { foreach (var grant in c.CompletionRewards) RequireResource(grant.ResourceId); });
            var entries = new List<MissionEntry>();
            foreach (var m in Missions.Values)
                InContext("mission " + m.Id, () => {
                    if (!Rewards.ContainsKey(m.Id)) throw new ArgumentException("Missing mission reward policy.");
                    ValidateMission(m);
                    entries.Add(new MissionEntry(m.Id, m.MinimumSquad, m.MaximumSquad, m.FactionId, m.IsCampaign, Rewards[m.Id],
                        (attempt, squad) => Build(m, attempt, squad)));
                });
            Entries = entries.AsReadOnly();
            _ = new CampaignCatalog(Campaigns, Entries, roster);
            if (StarterUnits.Count != 3 || StarterUnits.Any(id => !Units.ContainsKey(id) || Units[id].Roster == null || Units[id].Roster.IsApex)
                || StarterUnits.Select(id => Units[id].Roster.FactionId).Distinct(StringComparer.Ordinal).Count() != 1)
                throw new ArgumentException("Starter roster requires three standard units of one faction.");
            var starter = Campaigns.SingleOrDefault(c => c.StarterBonus != null);
            if (starter != null && starter.FactionId != Units[StarterUnits[0]].Roster.FactionId)
                throw new ArgumentException("Starter roster does not match starter campaign.");
        }
        public MissionFlow CreateFlow(IBattleSaveFiles battles, IProgressSaveFiles progress, string profileId)
            => new MissionFlow(battles, progress, profileId, Manifest.SaveRevision, Entries,
                Units.Values.Where(u => u.Roster != null).Select(u => u.Roster), Campaigns);

        private static IReadOnlyDictionary<string, T> Index<T>(IEnumerable<T> values, Func<T, string> id) where T : class
            => new ReadOnlyDictionary<string, T>(ContentIds.Items(values, id).ToDictionary(id, StringComparer.Ordinal));
        private void RequireResource(string id)
        { if (!Resources.Contains(id)) throw new ArgumentException("Undeclared resource: " + id); }
        private static void InContext(string context, Action action)
        {
            try { action(); }
            catch (Exception e) when (e is ArgumentException || e is InvalidOperationException || e is OverflowException)
            { throw new ArgumentException(context + ": " + e.Message, e); }
        }
        private void ValidateMission(MissionContent m)
        {
            var eligible = Units.Values.Where(u => u.Roster != null && (m.FactionId == null || u.Roster.FactionId == m.FactionId)).ToArray();
            if (eligible.Count(u => !u.Roster.IsApex) + Math.Min(1, eligible.Count(u => u.Roster.IsApex)) < m.MaximumSquad)
                throw new ArgumentException("Not enough eligible units to fill deployment slots with at most one Apex.");
            if (m.IsCampaign && eligible.Count(u => !u.Roster.IsApex) < 3) throw new ArgumentException("Campaign needs three standard units.");
            foreach (var a in m.Actors)
                if (!Units.ContainsKey(a.UnitId) || Units.ContainsKey(a.Id))
                    throw new ArgumentException("Actor has unknown template or its battle ID collides with a unit ID: " + a.Id);
            // Conservative envelope: every eligible body must fit every selectable slot, for every squad ordering.
            var envelope = new FieldBody(eligible.Max(u => u.Body.HalfWidth), eligible.Max(u => u.Body.HalfDepth),
                eligible.Max(u => u.Body.Height), new FieldPoint(0, 0, 0), new FieldPoint(0, 0, 0));
            var boxes = m.Deployment.Select(p => envelope.At(p)).Concat(m.Actors.Select(a => Units[a.UnitId].Body.At(a.Position))).ToArray();
            for (int i = 0; i < boxes.Length; i++)
            {
                if (!m.Map.Bounds.Contains(boxes[i]) || m.Map.Obstacles.Any(o => o.BlocksMovement && o.Bounds.Overlaps(boxes[i])))
                    throw new ArgumentException("Deployment slot or actor is outside the map or intersects an obstacle.");
                for (int j = 0; j < i; j++) if (boxes[i].Overlaps(boxes[j])) throw new ArgumentException("Deployment envelopes overlap.");
            }
            foreach (var a in m.Actors)
                if (a.Behavior?.GuardArea != null && !a.Behavior.GuardArea.Contains(Units[a.UnitId].Body.At(a.Position)))
                    throw new ArgumentException("Enemy starts outside its guard area: " + a.Id);
            foreach (var o in m.Objectives)
            {
                var ids = o.Subjects.Concat(o.Contestants).Concat(o.Interaction?.AllowedActors ?? Array.Empty<string>());
                if (ids.Any(id => id != "$squad" && id != "$leader" && !m.Actors.Any(a => a.Id == id)))
                    throw new ArgumentException("Unknown objective actor in " + o.Id);
                if ((o.Kind == ObjectiveKind.Protect || o.Kind == ObjectiveKind.RescueAndExtract) && o.Subjects.Contains("$squad"))
                    throw new ArgumentException("Single-target objectives must use $leader or a fixed actor.");
                if (o.Zone != null && !m.Map.Bounds.Contains(o.Zone)) throw new ArgumentException("Objective zone is outside map: " + o.Id);
                if (o.Interaction != null && !m.Map.Bounds.Contains(o.Interaction.Point)) throw new ArgumentException("Interaction is outside map: " + o.Id);
            }
            // Exercise binding with all slots; geometry above covers bodies not present in this representative squad.
            var squad = eligible.Where(u => !u.Roster.IsApex).Concat(eligible.Where(u => u.Roster.IsApex).Take(1)).Take(m.MaximumSquad).Select(u => u.Id).ToArray();
            Build(m, "catalog-validation", squad);
        }
        private BattleTurnController Build(MissionContent m, string attempt, IReadOnlyList<string> squad)
        {
            var deployed = squad.Select((id, i) => (Id: id, Unit: Units[id], Position: m.Deployment[i], Hostile: false))
                .Concat(m.Actors.Select(a => (Id: a.Id, Unit: Units[a.UnitId], Position: a.Position, Hostile: a.IsHostile))).ToArray();
            var battle = new BattleTurnController(deployed.Select(a => new UnitTurnDefinition(a.Id, a.Unit.Initiative, a.Unit.Movement)));
            var field = battle.ConfigureBattlefield(m.Map);
            foreach (var status in Statuses.Values) battle.Statuses.RegisterStatus(status);
            foreach (var a in deployed)
            {
                var kit = Kits[a.Unit.KitId];
                battle.Abilities.RegisterKit(a.Id, kit.Availability);
                battle.Health.RegisterHealth(a.Id, new UnitHealthDefinition(a.Hostile ? "hostile" : "friendly", a.Unit.Health, a.Unit.Armor));
                field.Register(a.Id, a.Position, a.Unit.Body, kit.AbilityIds.Skip(1).Select(id => Abilities[id].Effect),
                    deployed.Where(other => other.Hostile == a.Hostile).Select(other => other.Id));
            }
            foreach (var a in deployed)
            {
                var passive = Abilities[Kits[a.Unit.KitId].AbilityIds[0]].Passive;
                if (passive != null) battle.Statuses.RegisterPassive(a.Id, passive);
            }
            foreach (var a in m.Actors.Where(a => a.IsHostile)) field.RegisterEnemy(a.Id, a.Behavior);
            var objectives = m.Objectives.Select(o => Bind(o, squad)).ToArray();
            battle.ConfigureMission(new MissionDefinition(m.Id, attempt, squad, objectives[0], m.Priority, objectives.Skip(1)));
            battle.StartNextRound(); battle.BeginNextActivation(); return battle;
        }
        private static ObjectiveDefinition Bind(ObjectiveDefinition o, IReadOnlyList<string> squad)
        {
            string[] Expand(IEnumerable<string> values) => values.SelectMany(id => id == "$squad" ? squad :
                (IEnumerable<string>)new[] { id == "$leader" ? squad[0] : id }).ToArray();
            var subjects = Expand(o.Subjects); var contestants = Expand(o.Contestants);
            var interaction = o.Interaction == null ? null : new InteractionDefinition(o.Interaction.Point, o.Interaction.Reach, Expand(o.Interaction.AllowedActors));
            switch (o.Kind)
            {
                case ObjectiveKind.Defeat: return ObjectiveDefinition.Defeat(o.Id, subjects);
                case ObjectiveKind.Protect: return ObjectiveDefinition.Protect(o.Id, subjects.Single(), o.Required);
                case ObjectiveKind.RescueAndExtract: return ObjectiveDefinition.Rescue(o.Id, subjects.Single(), o.Zone, interaction);
                case ObjectiveKind.Secure: return ObjectiveDefinition.Secure(o.Id, o.Zone, subjects, contestants, o.Required, o.Consecutive);
                case ObjectiveKind.Stabilize: return ObjectiveDefinition.Stabilize(o.Id, o.Required, interaction);
                case ObjectiveKind.Survive: return ObjectiveDefinition.Survive(o.Id, o.Required);
                default: throw new ArgumentException("Unsupported objective kind.");
            }
        }
    }
}
