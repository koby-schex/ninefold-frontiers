using System;

namespace Ninefold.Core.Combat
{
    public enum AbilitySlot { Passive, NormalAttack, Main, Signature }

    public enum SignatureReadiness
    {
        ReadyAtDeployment,
        AfterNormalAttack,
        AfterMainAbility,
        ExternalCondition
    }

    public enum AbilityUseFailure
    {
        None,
        BattleEnded,
        NoActiveActivation,
        StaleActivation,
        UnsupportedSlot,
        PassiveCannotBeActivated,
        NoRegisteredKit,
        PrimaryActionSpent,
        MainOnCooldown,
        SignatureNotReady,
        SignatureAlreadyUsed
    }

    /// <summary>Availability rules only. Each kit has all four slots; no effects or balance stats.</summary>
    public sealed class UnitAbilityDefinition
    {
        public int MainCooldownTurns { get; }
        public SignatureReadiness SignatureRequirement { get; }

        public UnitAbilityDefinition(int mainCooldownTurns, SignatureReadiness signatureRequirement)
        {
            if (mainCooldownTurns < 1)
                throw new ArgumentOutOfRangeException(nameof(mainCooldownTurns));
            if (!Enum.IsDefined(typeof(SignatureReadiness), signatureRequirement))
                throw new ArgumentOutOfRangeException(nameof(signatureRequirement));
            MainCooldownTurns = mainCooldownTurns;
            SignatureRequirement = signatureRequirement;
        }
    }
}
