using System;

namespace Ninefold.Core.Progression
{
    public sealed class UnitUnlockDefinition
    {
        public string UnitId { get; }
        public string FragmentResourceId { get; }
        public long Cost { get; }
        public string Revision { get; }
        public UnitUnlockDefinition(string unitId, string fragmentResourceId, long cost, string revision)
        {
            RewardRules.Id(unitId); RewardRules.Id(fragmentResourceId); RewardRules.Id(revision);
            if (cost <= 0) throw new ArgumentOutOfRangeException(nameof(cost));
            UnitId = unitId; FragmentResourceId = fragmentResourceId; Cost = cost; Revision = revision;
        }
    }
    public sealed class UnitUnlockReceipt
    {
        public string OperationId { get; }
        public string UnitId { get; }
        public string FragmentResourceId { get; }
        public long Cost { get; }
        public string DefinitionRevision { get; }
        internal UnitUnlockReceipt(string operationId, UnitUnlockDefinition definition)
        {
            RewardRules.Id(operationId); OperationId = operationId;
            UnitId = definition.UnitId; FragmentResourceId = definition.FragmentResourceId;
            Cost = definition.Cost; DefinitionRevision = definition.Revision;
        }
    }
    public sealed class UnlockResult
    {
        public LoadedProgress Saved { get; }
        public UnitUnlockReceipt Receipt { get; }
        public bool AlreadyApplied { get; }
        internal UnlockResult(LoadedProgress saved, UnitUnlockReceipt receipt, bool duplicate)
        { Saved = saved; Receipt = receipt; AlreadyApplied = duplicate; }
    }
}
