using System;

namespace Ninefold.Core.Combat
{
    /// <summary>Battle-local identity and turn stats, independent of faction or visuals.</summary>
    public sealed class UnitTurnDefinition
    {
        public string UnitId { get; }
        public int Initiative { get; }
        public decimal MovementAllowance { get; }

        public UnitTurnDefinition(string unitId, int initiative, decimal movementAllowance)
        {
            if (string.IsNullOrWhiteSpace(unitId))
                throw new ArgumentException("A stable battle-local unit ID is required.", nameof(unitId));
            if (initiative < 0)
                throw new ArgumentOutOfRangeException(nameof(initiative));
            if (movementAllowance < 0)
                throw new ArgumentOutOfRangeException(nameof(movementAllowance));
            UnitId = unitId;
            Initiative = initiative;
            MovementAllowance = movementAllowance;
        }
    }
}
