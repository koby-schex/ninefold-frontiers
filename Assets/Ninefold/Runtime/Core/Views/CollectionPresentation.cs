using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
using Ninefold.Core.Progression;

namespace Ninefold.Core.Views
{
    public enum CollectionOperation { Unlock, Advance, Customize }
    public enum CollectionBlock { None, NotAtSelection, AlreadyOwned, NotOwned, Unsupported, MaximumRank, InsufficientFragments, UnknownOption, NoChange, MigrationRequired }
    public sealed class CollectionStats
    {
        public int Health { get; }
        public int Armor { get; }
        public int Initiative { get; }
        public decimal Movement { get; }
        public IReadOnlyList<PreparationAbility> Abilities { get; }
        internal CollectionStats(ContentCatalog catalog, UnitContent unit, AdvancementBonus advancement, CustomizationBonus customization)
        {
            var bonus = DeploymentModifiers.Combine(advancement,customization);
            Health = DeploymentModifiers.Scale(unit.Health,bonus.Health); Armor = DeploymentModifiers.Scale(unit.Armor,bonus.Armor);
            Initiative = unit.Initiative; Movement = unit.Movement;
            Abilities = Array.AsReadOnly(catalog.Kits[unit.KitId].AbilityIds.Select(id => new PreparationAbility(catalog.Abilities[id],bonus)).ToArray());
        }
    }
    public sealed class CollectionOffer
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public CollectionOperation Operation { get; }
        public string UnitId { get; }
        public string OptionId { get; }
        public CollectionBlock Block { get; }
        public bool CanConfirm => Block == CollectionBlock.None;
        public string ResourceId { get; }
        public long Cost { get; }
        public long Balance { get; }
        public long MissingFragments => Math.Max(0,Cost-Balance);
        public int RankBefore { get; }
        public int RankAfter { get; }
        public CollectionStats Before { get; }
        public CollectionStats After { get; }
        internal CollectionOffer(CollectionOperation operation, string unit, string option, CollectionBlock block,
            string resource, long cost, long balance, int rank, int nextRank, CollectionStats before, CollectionStats after)
        { Operation = operation; UnitId = unit; OptionId = option; Block = block; ResourceId = resource; Cost = cost; Balance = balance; RankBefore = rank; RankAfter = nextRank; Before = before; After = after; }
    }
    public sealed class CollectionUnit
    {
        public PreparationUnit Unit { get; }
        public string FragmentResourceId { get; }
        public long Fragments { get; }
        public CollectionOffer Unlock { get; }
        public CollectionOffer Advance { get; }
        public IReadOnlyList<CustomizationOption> Customizations { get; }
        internal CollectionUnit(PreparationUnit unit, long fragments, CollectionOffer unlock, CollectionOffer advance)
        {
            Unit = unit; FragmentResourceId = unit.Definition.Roster.Unlock.FragmentResourceId; Fragments = fragments;
            Unlock = unlock; Advance = advance;
            Customizations = unit.Definition.Roster.Customization?.Options ?? Array.AsReadOnly(Array.Empty<CustomizationOption>());
        }
    }
    public sealed class CollectionFaction
    {
        public string Id { get; }
        public IReadOnlyList<CollectionUnit> Units { get; }
        internal CollectionFaction(string id, IEnumerable<CollectionUnit> units) { Id = id; Units = Array.AsReadOnly(units.ToArray()); }
    }
    public sealed class CollectionView
    {
        public IReadOnlyList<CollectionFaction> Factions { get; }
        public CollectionOffer Pending { get; }
        internal CollectionView(IEnumerable<CollectionFaction> factions, CollectionOffer pending) { Factions = Array.AsReadOnly(factions.ToArray()); Pending = pending; }
    }
    public sealed class CollectionChange
    {
        public CollectionOffer Offer { get; }
        public PlayerProgress Before { get; }
        public PlayerProgress After { get; }
        internal CollectionChange(CollectionOffer offer, PlayerProgress before, PlayerProgress after) { Offer = offer; Before = before; After = after; }
    }
    /// <summary>Shares the mission menu's flow. Preview tokens are transient; receipts and spending belong to that flow.</summary>
    public sealed class CollectionPresentation
    {
        private readonly ContentCatalog catalog;
        private readonly MissionFlow flow;
        private CollectionOffer pending;
        private PlayerProgress previewProgress;
        private object previewVersion;
        internal CollectionPresentation(ContentCatalog catalog, MissionFlow flow) { this.catalog = catalog; this.flow = flow; }
        private void Ready() { if (flow.NeedsReload) throw new InvalidOperationException("Open/recover before using the collection."); }
        public void Cancel() { pending = null; previewProgress = null; previewVersion = null; }
        public CollectionView Read()
        {
            Ready();
            if (pending != null && (!ReferenceEquals(previewProgress,flow.Progress) || !ReferenceEquals(previewVersion,flow.BattleVersion))) Cancel();
            var units = catalog.Units.Values.Where(u => u.Roster != null).OrderBy(u => u.Id,StringComparer.Ordinal).Select(u =>
                new CollectionUnit(new PreparationUnit(catalog,u,flow.Progress,null),flow.Progress.Balance(u.Roster.Unlock.FragmentResourceId),
                    Build(CollectionOperation.Unlock,u.Id,null),Build(CollectionOperation.Advance,u.Id,null)));
            return new CollectionView(units.GroupBy(u => u.Unit.Definition.Roster.FactionId).OrderBy(g => g.Key,StringComparer.Ordinal).Select(g => new CollectionFaction(g.Key,g)),pending);
        }
        public CollectionOffer Preview(CollectionOperation operation, string unitId, string optionId = null)
        {
            Ready(); Cancel();
            pending = Build(operation,unitId,optionId); previewProgress = flow.Progress; previewVersion = flow.BattleVersion; return pending;
        }
        public CollectionChange Confirm(string displayedOfferId)
        {
            Ready();
            if (pending == null || displayedOfferId != pending.Id) throw new InvalidOperationException("No matching displayed offer.");
            var offer = pending; var before = previewProgress; var version = previewVersion; Cancel();
            if (!ReferenceEquals(before,flow.Progress) || !ReferenceEquals(version,flow.BattleVersion) || flow.Phase != MissionFlowPhase.Selection)
                throw new InvalidOperationException("Progress or battle state changed; preview again.");
            if (!offer.CanConfirm) throw new InvalidOperationException("Blocked collection operation: " + offer.Block);
            switch (offer.Operation)
            {
                case CollectionOperation.Unlock: flow.UnlockUnit(offer.Id,offer.UnitId); break;
                case CollectionOperation.Advance: flow.AdvanceUnit(offer.Id,offer.UnitId); break;
                default: flow.CustomizeUnit(offer.Id,offer.UnitId,offer.OptionId); break;
            }
            return new CollectionChange(offer,before,flow.Progress);
        }
        private CollectionOffer Build(CollectionOperation operation, string id, string optionId)
        {
            if (!Enum.IsDefined(typeof(CollectionOperation),operation)) throw new ArgumentOutOfRangeException(nameof(operation));
            if (id == null || !catalog.Units.TryGetValue(id,out var unit) || unit.Roster == null) throw new ArgumentException("Unknown collectible unit.");
            if (operation != CollectionOperation.Customize && optionId != null) throw new ArgumentException("Only customization accepts an option.");
            var p = flow.Progress; bool owned = p.Owns(id); var roster = unit.Roster;
            var advancement = owned ? p.GetAdvancement(id).Bonus : AdvancementBonus.None;
            var customization = owned ? p.GetCustomization(id).Bonus : CustomizationBonus.None;
            int rank = owned ? p.GetAdvancement(id).Rank : 0, nextRank = rank;
            var before = new CollectionStats(catalog,unit,advancement,customization);
            long cost = 0; string resource = roster.Unlock.FragmentResourceId; var block = CollectionBlock.None;
            switch (operation)
            {
                case CollectionOperation.Unlock:
                    cost = roster.Unlock.Cost; if (owned) block = CollectionBlock.AlreadyOwned; break;
                case CollectionOperation.Advance:
                    if (!owned) block = CollectionBlock.NotOwned;
                    else if (roster.Advancement == null) block = CollectionBlock.Unsupported;
                    else if (rank >= roster.Advancement.Steps.Count) block = CollectionBlock.MaximumRank;
                    else if (rank > 0 && !advancement.Same(roster.Advancement.Steps[rank-1].TotalBonus)) block = CollectionBlock.MigrationRequired;
                    else { var step = roster.Advancement.Steps[rank]; cost = step.FragmentCost; advancement = step.TotalBonus; nextRank++; }
                    break;
                case CollectionOperation.Customize:
                    resource = null;
                    if (!owned) block = CollectionBlock.NotOwned;
                    else if (optionId == null) customization = CustomizationBonus.None;
                    else if (roster.Customization == null) block = CollectionBlock.Unsupported;
                    else
                    {
                        var option = roster.Customization.Options.FirstOrDefault(o => o.Id == optionId);
                        if (option == null) block = CollectionBlock.UnknownOption; else customization = option.Bonus;
                    }
                    if (block == CollectionBlock.None && p.GetCustomization(id).OptionId == optionId) block = CollectionBlock.NoChange;
                    break;
            }
            long balance = resource == null ? 0 : p.Balance(resource);
            if (block == CollectionBlock.None && cost > balance) block = CollectionBlock.InsufficientFragments;
            if (flow.Phase != MissionFlowPhase.Selection) block = CollectionBlock.NotAtSelection;
            return new CollectionOffer(operation,id,optionId,block,resource,cost,balance,rank,nextRank,before,new CollectionStats(catalog,unit,advancement,customization));
        }
    }
}
