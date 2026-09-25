namespace Ninefold.Core.Combat
{
    /// <summary>Immutable snapshot. Re-read CurrentActivation after a successful command.</summary>
    public sealed class ActivationView
    {
        public long ActivationId { get; }
        public string UnitId { get; }
        public int RoundNumber { get; }
        public decimal MovementRemaining { get; }
        public bool PrimaryActionAvailable { get; }

        internal ActivationView(long activationId, string unitId, int roundNumber,
            decimal movementRemaining, bool primaryActionAvailable)
        {
            ActivationId = activationId;
            UnitId = unitId;
            RoundNumber = roundNumber;
            MovementRemaining = movementRemaining;
            PrimaryActionAvailable = primaryActionAvailable;
        }
    }
}
