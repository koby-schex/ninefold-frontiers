using System;
using Ninefold.Core.Combat;

internal static partial class Program
{
    private static (string Name, Action Run)[] AbilityTests() => new (string, Action)[]
    {
        ("Normal attack spends one action and preserves movement", NormalUse),
        ("Cooldown two: use at one, ready at three", MainCooldown),
        ("Only the owner activation start advances cooldown", OwnerOnlyCooldown),
        ("Cooldown one permits use next owner turn", CooldownOne),
        ("Forfeited activation advances owner cooldown", ForfeitedTurn),
        ("Deployment Signature can be used once per battle", DeploymentSignature),
        ("Main use latches readiness without granting a second action", MainReadiness),
        ("Normal attack readiness excludes contextual actions", AttackReadiness),
        ("External event latches readiness without spending action", ExternalReadiness),
        ("Repeated condition events never refund Signature use", NoSignatureRefund),
        ("Repeated previews do not mutate ability or turn state", PreviewOnly),
        ("Passive has no active-use command or action cost", PassiveSlot),
        ("Unknown slot is rejected without resource loss", UnknownSlot),
        ("Stale activation cannot consume another unit's ability", StaleAbilityCommand),
        ("No activation and ended battle reject ability commands", AbilityLifecycle),
        ("Unit without registered kit cannot use abilities", MissingKit),
        ("Kit cannot be replaced or registered after its first turn", KitRegistration),
        ("Invalid ability definitions are rejected", InvalidAbilityDefinitions),
        ("Removed unit retains ability history but cannot act", RemovedAbilityOwner),
        ("Fresh battle resets ability state", NewBattleAbilities),
        ("Units do not share cooldowns or Signature usage", IndependentKits),
        ("Reinforcement cooldown starts from its own first activation", ReinforcementKit),
        ("Ability snapshots remain immutable", ImmutableAbilityState),
        ("Large cooldown uses safe owner-counter arithmetic", LargeCooldown)
    };

    private static BattleTurnController Solo(SignatureReadiness readiness = SignatureReadiness.ReadyAtDeployment, int cooldown = 2)
    {
        var b = B(U("a"));
        b.Abilities.RegisterKit("a", new UnitAbilityDefinition(cooldown, readiness));
        b.StartNextRound(); b.BeginNextActivation();
        return b;
    }
    private static void NextSolo(BattleTurnController b)
    {
        b.EndActivation(b.CurrentActivation.ActivationId);
        b.StartNextRound(); b.BeginNextActivation();
    }
    private static void Use(BattleTurnController b, AbilitySlot slot)
    {
        Equal(true, b.Abilities.TryUse(b.CurrentActivation.ActivationId, slot, out var failure));
        Equal(AbilityUseFailure.None, failure);
    }
    private static void Reject(BattleTurnController b, AbilitySlot slot, AbilityUseFailure expected)
    {
        var before = b.CurrentActivation;
        var state = b.Abilities.GetState(before.UnitId);
        Equal(false, b.Abilities.TryUse(before.ActivationId, slot, out var failure));
        Equal(expected, failure);
        Equal(before.MovementRemaining, b.CurrentActivation.MovementRemaining);
        Equal(before.PrimaryActionAvailable, b.CurrentActivation.PrimaryActionAvailable);
        var after = b.Abilities.GetState(before.UnitId);
        Equal(state.MainCooldownRemaining, after.MainCooldownRemaining);
        Equal(state.SignatureRequirementMet, after.SignatureRequirementMet);
        Equal(state.SignatureUsed, after.SignatureUsed);
    }
    private static void NormalUse()
    {
        var b = Solo(); var id = b.CurrentActivation.ActivationId;
        b.SpendMovement(id, 2m); Use(b, AbilitySlot.NormalAttack);
        Equal(3m, b.CurrentActivation.MovementRemaining);
        Equal(false, b.CurrentActivation.PrimaryActionAvailable);
        Reject(b, AbilitySlot.Main, AbilityUseFailure.PrimaryActionSpent);
        b.SpendMovement(id, 3m); NextSolo(b); Use(b, AbilitySlot.NormalAttack);
    }
    private static void MainCooldown()
    {
        var b = Solo(); Equal(0, b.Abilities.GetState("a").MainCooldownRemaining);
        Use(b, AbilitySlot.Main); Equal(2, b.Abilities.GetState("a").MainCooldownRemaining);
        NextSolo(b); Equal(1, b.Abilities.GetState("a").MainCooldownRemaining);
        Reject(b, AbilitySlot.Main, AbilityUseFailure.MainOnCooldown);
        Use(b, AbilitySlot.NormalAttack);
        NextSolo(b); Equal(0, b.Abilities.GetState("a").MainCooldownRemaining);
        Use(b, AbilitySlot.Main); Equal(2, b.Abilities.GetState("a").MainCooldownRemaining);
    }
    private static void OwnerOnlyCooldown()
    {
        var b = B(U("a", 20), U("b", 10));
        b.Abilities.RegisterKit("a", new UnitAbilityDefinition(2, SignatureReadiness.ReadyAtDeployment));
        b.StartNextRound(); var a = b.BeginNextActivation(); Use(b, AbilitySlot.Main);
        b.EndActivation(a.ActivationId); var other = b.BeginNextActivation();
        Equal(2, b.Abilities.GetState("a").MainCooldownRemaining);
        b.SetInitiative("b", 30); b.EndActivation(other.ActivationId); b.StartNextRound();
        Equal(2, b.Abilities.GetState("a").MainCooldownRemaining);
        other = b.BeginNextActivation(); Equal("b", other.UnitId);
        Equal(2, b.Abilities.GetState("a").MainCooldownRemaining);
        b.EndActivation(other.ActivationId); a = b.BeginNextActivation();
        Equal("a", a.UnitId); Equal(1, b.Abilities.GetState("a").MainCooldownRemaining);
        Throws<InvalidOperationException>(() => b.BeginNextActivation());
        Equal(1, b.Abilities.GetState("a").MainCooldownRemaining);
    }
    private static void CooldownOne()
    {
        var b = Solo(cooldown: 1); Use(b, AbilitySlot.Main);
        NextSolo(b); Equal(0, b.Abilities.GetState("a").MainCooldownRemaining); Use(b, AbilitySlot.Main);
    }
    private static void ForfeitedTurn()
    {
        var b = Solo(); Use(b, AbilitySlot.Main); NextSolo(b);
        // Status layer can end a scheduled activation without an action. No stun system is claimed.
        Equal(1, b.Abilities.GetState("a").MainCooldownRemaining);
        NextSolo(b); Equal(3L, b.GetActivationCount("a")); Use(b, AbilitySlot.Main);
    }
    private static void DeploymentSignature()
    {
        var b = Solo(); Equal(true, b.Abilities.GetState("a").SignatureReady);
        Use(b, AbilitySlot.Signature); Equal(true, b.Abilities.GetState("a").SignatureUsed);
        Equal(false, b.Abilities.GetState("a").SignatureReady);
        NextSolo(b); Reject(b, AbilitySlot.Signature, AbilityUseFailure.SignatureAlreadyUsed);
        Use(b, AbilitySlot.Main); NextSolo(b);
        Reject(b, AbilitySlot.Signature, AbilityUseFailure.SignatureAlreadyUsed);
    }
    private static void MainReadiness()
    {
        var b = Solo(SignatureReadiness.AfterMainAbility);
        Reject(b, AbilitySlot.Signature, AbilityUseFailure.SignatureNotReady);
        Use(b, AbilitySlot.Main); Equal(true, b.Abilities.GetState("a").SignatureReady);
        Reject(b, AbilitySlot.Signature, AbilityUseFailure.PrimaryActionSpent);
        NextSolo(b); Use(b, AbilitySlot.Signature);
        NextSolo(b); Use(b, AbilitySlot.Main); NextSolo(b);
        Reject(b, AbilitySlot.Signature, AbilityUseFailure.SignatureAlreadyUsed);
    }
    private static void AttackReadiness()
    {
        var b = Solo(SignatureReadiness.AfterNormalAttack);
        // A contextual survey spends an action but is not a normal attack.
        b.SpendPrimaryAction(b.CurrentActivation.ActivationId);
        Equal(false, b.Abilities.GetState("a").SignatureReady);
        NextSolo(b); Use(b, AbilitySlot.Main); Equal(false, b.Abilities.GetState("a").SignatureReady);
        NextSolo(b); Use(b, AbilitySlot.NormalAttack); Equal(true, b.Abilities.GetState("a").SignatureReady);
        NextSolo(b); Use(b, AbilitySlot.Signature);
    }
    private static void ExternalReadiness()
    {
        var b = Solo(SignatureReadiness.ExternalCondition);
        Reject(b, AbilitySlot.Signature, AbilityUseFailure.SignatureNotReady);
        b.Abilities.MarkSignatureConditionMet("a");
        Equal(true, b.CurrentActivation.PrimaryActionAvailable);
        Equal(5m, b.CurrentActivation.MovementRemaining); Use(b, AbilitySlot.Signature);
        var other = Solo(SignatureReadiness.AfterNormalAttack);
        Throws<InvalidOperationException>(() => other.Abilities.MarkSignatureConditionMet("a"));
        Equal(false, other.Abilities.GetState("a").SignatureReady);
    }
    private static void NoSignatureRefund()
    {
        var b = Solo(SignatureReadiness.ExternalCondition);
        b.Abilities.MarkSignatureConditionMet("a"); Use(b, AbilitySlot.Signature);
        for (int i = 0; i < 5; i++)
        {
            NextSolo(b); b.Abilities.MarkSignatureConditionMet("a");
            Reject(b, AbilitySlot.Signature, AbilityUseFailure.SignatureAlreadyUsed);
        }
    }
    private static void PreviewOnly()
    {
        var b = Solo(SignatureReadiness.AfterMainAbility); var id = b.CurrentActivation.ActivationId;
        for (int i = 0; i < 100; i++)
        {
            Equal(AbilityUseFailure.None, b.Abilities.GetAvailability(id, AbilitySlot.Main));
            Equal(AbilityUseFailure.SignatureNotReady, b.Abilities.GetAvailability(id, AbilitySlot.Signature));
        }
        Equal(0, b.Abilities.GetState("a").MainCooldownRemaining);
        Equal(false, b.Abilities.GetState("a").SignatureRequirementMet);
        Equal(true, b.CurrentActivation.PrimaryActionAvailable);
        Use(b, AbilitySlot.Main);
        for (int i = 0; i < 100; i++) b.Abilities.GetState("a");
        Equal(2, b.Abilities.GetState("a").MainCooldownRemaining);
    }
    private static void PassiveSlot()
    {
        var b = Solo(); Reject(b, AbilitySlot.Passive, AbilityUseFailure.PassiveCannotBeActivated);
        Use(b, AbilitySlot.NormalAttack);
    }
    private static void UnknownSlot()
    {
        var b = Solo(); Reject(b, (AbilitySlot)99, AbilityUseFailure.UnsupportedSlot);
    }
    private static void StaleAbilityCommand()
    {
        var b = B(U("a", 20), U("b", 10));
        foreach (var id in new[] { "a", "b" }) b.Abilities.RegisterKit(id, new UnitAbilityDefinition(2, SignatureReadiness.ReadyAtDeployment));
        b.StartNextRound(); var old = b.BeginNextActivation(); b.EndActivation(old.ActivationId); b.BeginNextActivation();
        Equal(false, b.Abilities.TryUse(old.ActivationId, AbilitySlot.Signature, out var failure));
        Equal(AbilityUseFailure.StaleActivation, failure);
        Equal(false, b.Abilities.GetState("a").SignatureUsed); Equal(false, b.Abilities.GetState("b").SignatureUsed);
        Equal(true, b.CurrentActivation.PrimaryActionAvailable);
        b.EndActivation(b.CurrentActivation.ActivationId); b.StartNextRound(); b.BeginNextActivation();
        Equal(false, b.Abilities.TryUse(old.ActivationId, AbilitySlot.Main, out failure));
        Equal(AbilityUseFailure.StaleActivation, failure); Equal(0, b.Abilities.GetState("a").MainCooldownRemaining);
    }
    private static void AbilityLifecycle()
    {
        var b = Solo(); var id = b.CurrentActivation.ActivationId; b.EndActivation(id);
        Equal(false, b.Abilities.TryUse(id, AbilitySlot.Main, out var failure));
        Equal(AbilityUseFailure.NoActiveActivation, failure);
        b.EndBattle(); Equal(false, b.Abilities.TryUse(id, AbilitySlot.Signature, out failure));
        Equal(AbilityUseFailure.BattleEnded, failure);
        Throws<InvalidOperationException>(() => b.Abilities.MarkSignatureConditionMet("a"));
        Throws<InvalidOperationException>(() => b.Abilities.RegisterKit("a", new UnitAbilityDefinition(2, SignatureReadiness.ReadyAtDeployment)));
    }
    private static void MissingKit()
    {
        var b = B(U("a")); b.StartNextRound(); var t = b.BeginNextActivation();
        Equal(false, b.Abilities.TryUse(t.ActivationId, AbilitySlot.NormalAttack, out var failure));
        Equal(AbilityUseFailure.NoRegisteredKit, failure); Equal(true, b.CurrentActivation.PrimaryActionAvailable);
        Throws<ArgumentException>(() => b.Abilities.GetState("a"));
    }
    private static void KitRegistration()
    {
        var b = Solo(); Use(b, AbilitySlot.Signature);
        Throws<InvalidOperationException>(() => b.Abilities.RegisterKit("a", new UnitAbilityDefinition(1, SignatureReadiness.ReadyAtDeployment)));
        Equal(true, b.Abilities.GetState("a").SignatureUsed);
        var late = B(U("a")); late.StartNextRound(); late.BeginNextActivation();
        Throws<InvalidOperationException>(() => late.Abilities.RegisterKit("a", new UnitAbilityDefinition(1, SignatureReadiness.ReadyAtDeployment)));
        var empty = B(U("a"));
        Throws<ArgumentException>(() => empty.Abilities.RegisterKit("unknown", new UnitAbilityDefinition(1, SignatureReadiness.ReadyAtDeployment)));
        Throws<ArgumentNullException>(() => empty.Abilities.RegisterKit("a", null));
    }
    private static void InvalidAbilityDefinitions()
    {
        Throws<ArgumentOutOfRangeException>(() => new UnitAbilityDefinition(0, SignatureReadiness.ReadyAtDeployment));
        Throws<ArgumentOutOfRangeException>(() => new UnitAbilityDefinition(-1, SignatureReadiness.AfterMainAbility));
        Throws<ArgumentOutOfRangeException>(() => new UnitAbilityDefinition(2, (SignatureReadiness)99));
    }
    private static void RemovedAbilityOwner()
    {
        var b = Solo(SignatureReadiness.ExternalCondition); var id = b.CurrentActivation.ActivationId;
        b.Abilities.MarkSignatureConditionMet("a"); Use(b, AbilitySlot.Signature); b.RemoveUnit("a");
        Equal(true, b.Abilities.GetState("a").SignatureUsed);
        Equal(false, b.Abilities.TryUse(id, AbilitySlot.NormalAttack, out var failure));
        Equal(AbilityUseFailure.NoActiveActivation, failure);
        Throws<InvalidOperationException>(() => b.Abilities.MarkSignatureConditionMet("a"));
        Throws<InvalidOperationException>(() => b.Abilities.RegisterKit("a", new UnitAbilityDefinition(1, SignatureReadiness.ReadyAtDeployment)));
    }
    private static void NewBattleAbilities()
    {
        var first = Solo(); Use(first, AbilitySlot.Signature); first.EndBattle();
        var next = Solo(); Equal(false, next.Abilities.GetState("a").SignatureUsed);
        Equal(0, next.Abilities.GetState("a").MainCooldownRemaining); Use(next, AbilitySlot.Signature);
    }
    private static void IndependentKits()
    {
        var b = B(U("a", 20), U("b", 10));
        foreach (var id in new[] { "a", "b" }) b.Abilities.RegisterKit(id, new UnitAbilityDefinition(2, SignatureReadiness.ReadyAtDeployment));
        b.StartNextRound(); var a = b.BeginNextActivation(); Use(b, AbilitySlot.Main); b.EndActivation(a.ActivationId);
        var other = b.BeginNextActivation(); Equal(0, b.Abilities.GetState("b").MainCooldownRemaining);
        Use(b, AbilitySlot.Signature); b.EndActivation(other.ActivationId); b.StartNextRound(); b.BeginNextActivation();
        Equal(false, b.Abilities.GetState("a").SignatureUsed); Use(b, AbilitySlot.Signature);
    }
    private static void ReinforcementKit()
    {
        var b = Solo(); b.RegisterUnit(U("new", 100));
        b.Abilities.RegisterKit("new", new UnitAbilityDefinition(2, SignatureReadiness.ReadyAtDeployment));
        Equal(0L, b.GetActivationCount("new")); b.EndActivation(b.CurrentActivation.ActivationId);
        b.StartNextRound(); Equal("new", b.BeginNextActivation().UnitId);
        Equal(1L, b.GetActivationCount("new")); Use(b, AbilitySlot.Main);
        Equal(2, b.Abilities.GetState("new").MainCooldownRemaining);
    }
    private static void ImmutableAbilityState()
    {
        var b = Solo(SignatureReadiness.AfterMainAbility); var old = b.Abilities.GetState("a");
        Use(b, AbilitySlot.Main);
        Equal(0, old.MainCooldownRemaining); Equal(false, old.SignatureReady);
        Equal(2, b.Abilities.GetState("a").MainCooldownRemaining); Equal(true, b.Abilities.GetState("a").SignatureReady);
    }
    private static void LargeCooldown()
    {
        var b = Solo(cooldown: int.MaxValue); Use(b, AbilitySlot.Main);
        Equal(int.MaxValue, b.Abilities.GetState("a").MainCooldownRemaining);
        NextSolo(b); Equal(int.MaxValue - 1, b.Abilities.GetState("a").MainCooldownRemaining);
        Reject(b, AbilitySlot.Main, AbilityUseFailure.MainOnCooldown);
    }
}
