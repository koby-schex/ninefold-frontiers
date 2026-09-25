namespace Ninefold.Core.Combat
{
    /// <summary>Read-only snapshot; inspection cannot tick cooldowns or grant readiness.</summary>
    public sealed class AbilityStateView
    {
        public string UnitId { get; }
        public int MainCooldownRemaining { get; }
        public bool SignatureRequirementMet { get; }
        public bool SignatureUsed { get; }
        public bool SignatureReady => SignatureRequirementMet && !SignatureUsed;

        internal AbilityStateView(string unitId, int mainCooldownRemaining,
            bool signatureRequirementMet, bool signatureUsed)
        {
            UnitId = unitId;
            MainCooldownRemaining = mainCooldownRemaining;
            SignatureRequirementMet = signatureRequirementMet;
            SignatureUsed = signatureUsed;
        }
    }
}
