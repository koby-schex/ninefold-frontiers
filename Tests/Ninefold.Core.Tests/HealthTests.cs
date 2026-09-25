using System;
using Ninefold.Core.Combat;

internal static partial class Program
{
    private static (string Name, Action Run)[] HealthTests() => new (string, Action)[]
    {
        ("Armor uses predictable diminishing returns", ArmorDamage),
        ("Cover and temporary reductions multiply under one cap", MitigationStack),
        ("Damage rounds halves up and handles minimum/zero", DamageRounding),
        ("Damage tuning is configurable per battle", ConfigurableDamage),
        ("Large damage/armor values stay within integer bounds", LargeDamage),
        ("Invalid mitigation and rules are rejected", InvalidDamageRules),
        ("Repeated calculations are deterministic", DeterministicDamage),
        ("Damage preview equals result and preview spends nothing", DamagePreview),
        ("Overkill clamps health and skips defeated target turns", DefeatResolution),
        ("Healing caps at target maximum", HealingCap),
        ("Self healing uses the same cost and health rules", SelfHealing),
        ("Wrong-team damage/healing preserves budgets", TeamValidation),
        ("Range obstruction and compatibility failures preserve costs", TargetingValidation),
        ("Healing cannot revive a defeated ally", NoRevival),
        ("Full-health healing is rejected without cost", FullHealthHealing),
        ("Invalid effect requests cannot spend resources", InvalidHealthRequests),
        ("Missing target/actor health rejected before costs", MissingHealth),
        ("Stale health commands cannot spend a new activation", StaleHealthAction),
        ("Signature spends only after target validation", SignatureHealthAction),
        ("Main cooldown begins only after a valid effect", MainHealthCooldown),
        ("Rejected healing cannot satisfy Signature readiness", HealingReadiness),
        ("Rejected attack cannot satisfy Signature readiness", DamageReadiness),
        ("Extracted living targets cannot be affected", ExtractedTarget),
        ("Health registration cannot reset damage", HealthRegistration),
        ("Closed battles reject health changes", EndedHealthBattle),
        ("Old health snapshots stay immutable and effects recompute", HealthSnapshots),
        ("Large healing clamps without overflowing", LargeHealing),
        ("Zero mitigated damage still costs an action", ZeroResolvedDamage)
    };

    private static BattleTurnController HealthBattle(int enemyHealth = 100, int enemyArmor = 0,
        int allyHealth = 60, int actorHealth = 100,
        SignatureReadiness readiness = SignatureReadiness.ReadyAtDeployment, DamageRules rules = null)
    {
        var b = new BattleTurnController(new[] { U("a", 30), U("b", 20), U("c", 10) }, rules);
        foreach (var id in new[] { "a", "b", "c" })
            b.Abilities.RegisterKit(id, new UnitAbilityDefinition(2, readiness));
        b.Health.RegisterHealth("a", new UnitHealthDefinition("allies", 100, 0), actorHealth);
        b.Health.RegisterHealth("b", new UnitHealthDefinition("opponents", 100, enemyArmor), enemyHealth);
        b.Health.RegisterHealth("c", new UnitHealthDefinition("allies", 100, 0), allyHealth);
        b.StartNextRound(); b.BeginNextActivation();
        return b;
    }
    private static HealthAction Hit(string target = "b", int amount = 20, AbilitySlot slot = AbilitySlot.NormalAttack,
        TargetingVerdict verdict = TargetingVerdict.Legal, DamageMitigation mitigation = null)
        => new HealthAction(slot, HealthEffectKind.Damage, target, amount, verdict, mitigation);
    private static HealthAction Heal(string target = "c", int amount = 20, AbilitySlot slot = AbilitySlot.Main,
        TargetingVerdict verdict = TargetingVerdict.Legal)
        => new HealthAction(slot, HealthEffectKind.Healing, target, amount, verdict);
    private static HealthEffectPreview Apply(BattleTurnController b, HealthAction action)
    {
        Equal(true, b.Health.TryApply(b.CurrentActivation.ActivationId, action, out var result, out var failure));
        Equal(HealthActionFailure.None, failure);
        return result;
    }
    private static void RejectHealth(BattleTurnController b, HealthAction action, HealthActionFailure expected)
    {
        var turn = b.CurrentActivation;
        var before = b.Abilities.GetState(turn.UnitId);
        var actorHp = b.Health.GetState(turn.UnitId).CurrentHealth;
        Equal(false, b.Health.TryApply(turn.ActivationId, action, out var result, out var failure));
        Equal<HealthEffectPreview>(null, result); Equal(expected, failure);
        Equal(turn.PrimaryActionAvailable, b.CurrentActivation.PrimaryActionAvailable);
        Equal(turn.MovementRemaining, b.CurrentActivation.MovementRemaining);
        var after = b.Abilities.GetState(turn.UnitId);
        Equal(before.MainCooldownRemaining, after.MainCooldownRemaining);
        Equal(before.SignatureRequirementMet, after.SignatureRequirementMet);
        Equal(before.SignatureUsed, after.SignatureUsed);
        Equal(actorHp, b.Health.GetState(turn.UnitId).CurrentHealth);
    }
    private static void NextHealthRound(BattleTurnController b)
    {
        if (b.CurrentActivation != null) b.EndActivation(b.CurrentActivation.ActivationId);
        Drain(b); b.StartNextRound(); Equal("a", b.BeginNextActivation().UnitId);
    }
    private static void ArmorDamage()
    {
        var r = DamageRules.Provisional;
        Equal(33, r.Calculate(40, 20, DamageMitigation.None));
        Equal(36, r.Calculate(40, 10, DamageMitigation.None));
        Equal(30, r.Calculate(40, 35, DamageMitigation.None));
        Equal(40, r.Calculate(40, 0, DamageMitigation.None));
    }
    private static void MitigationStack()
    {
        Equal(24, DamageRules.Provisional.Calculate(40, 0, new DamageMitigation(.25m, .20m)));
        Equal(12, DamageRules.Provisional.Calculate(40, 100, new DamageMitigation(.25m, .50m)));
        Equal(12, DamageRules.Provisional.Calculate(40, int.MaxValue, new DamageMitigation(1m, 1m)));
        Equal(8, new DamageRules(100, .99m, true).Calculate(40, 100, new DamageMitigation(.25m, .50m)));
    }
    private static void DamageRounding()
    {
        Equal(3, DamageRules.Provisional.Calculate(5, 100, DamageMitigation.None));
        Equal(1, DamageRules.Provisional.Calculate(1, 900, DamageMitigation.None));
        Equal(0, DamageRules.Provisional.Calculate(0, 900, DamageMitigation.None));
        Equal(0, new DamageRules(100, 1m, false).Calculate(1, 900, DamageMitigation.None));
    }
    private static void ConfigurableDamage()
    {
        var r = new DamageRules(50, .90m, false);
        Equal(20, r.Calculate(40, 50, DamageMitigation.None));
        Equal(27, DamageRules.Provisional.Calculate(40, 50, DamageMitigation.None));
        Equal(40, new DamageRules(100, 0m, true).Calculate(40, 999, new DamageMitigation(1m, 1m)));
        var b = HealthBattle(enemyArmor: 50, rules: r); Equal(10, Apply(b, Hit()).ResolvedAmount);
    }
    private static void LargeDamage()
    {
        Equal(int.MaxValue, DamageRules.Provisional.Calculate(int.MaxValue, 0, DamageMitigation.None));
        int reduced = DamageRules.Provisional.Calculate(int.MaxValue, int.MaxValue, DamageMitigation.None);
        Equal(true, reduced > 0 && reduced < int.MaxValue);
    }
    private static void InvalidDamageRules()
    {
        Throws<ArgumentOutOfRangeException>(() => new DamageRules(0, .7m, true));
        Throws<ArgumentOutOfRangeException>(() => new DamageRules(100, 1.1m, true));
        Throws<ArgumentOutOfRangeException>(() => new DamageRules(100, -.1m, true));
        Throws<ArgumentOutOfRangeException>(() => new DamageMitigation(-1m, 0m));
        Throws<ArgumentOutOfRangeException>(() => new DamageMitigation(0m, 2m));
        Throws<ArgumentOutOfRangeException>(() => DamageRules.Provisional.Calculate(-1, 0, DamageMitigation.None));
        Throws<ArgumentOutOfRangeException>(() => DamageRules.Provisional.Calculate(1, -1, DamageMitigation.None));
        Throws<ArgumentNullException>(() => DamageRules.Provisional.Calculate(1, 0, null));
    }
    private static void DeterministicDamage()
    {
        for (int i = 0; i < 1000; i++) Equal(23, DamageRules.Provisional.Calculate(36, 20, new DamageMitigation(.25m, 0m)));
        // 36 / 1.2 * .75 = 22.5, rounded away from zero is 23.
    }
    private static void DamagePreview()
    {
        var b = HealthBattle(enemyArmor: 20); var id = b.CurrentActivation.ActivationId;
        b.SpendMovement(id, 2m); var request = Hit(amount: 40);
        Equal(true, b.Health.TryPreview(id, request, out var preview, out var failure));
        Equal(HealthActionFailure.None, failure); Equal(33, preview.HealthChanged);
        Equal(100, b.Health.GetState("b").CurrentHealth); Equal(true, b.CurrentActivation.PrimaryActionAvailable);
        var actual = Apply(b, request);
        Equal(preview.HealthAfter, actual.HealthAfter); Equal(preview.ResolvedAmount, actual.ResolvedAmount);
        Equal(67, b.Health.GetState("b").CurrentHealth); Equal(3m, b.CurrentActivation.MovementRemaining);
        Equal(false, b.CurrentActivation.PrimaryActionAvailable);
        RejectHealth(b, request, HealthActionFailure.AbilityUnavailable); Equal(67, b.Health.GetState("b").CurrentHealth);
    }
    private static void DefeatResolution()
    {
        var b = HealthBattle(enemyHealth: 10); var result = Apply(b, Hit(amount: 999));
        Equal(999, result.ResolvedAmount); Equal(10, result.HealthChanged); Equal(0, result.HealthAfter);
        Equal(true, b.Health.GetState("b").IsDefeated); Equal(false, b.IsUnitEligible("b"));
        b.EndActivation(b.CurrentActivation.ActivationId); Equal("c", Drain(b));
        b.StartNextRound(); Equal("a,c", Drain(b));
    }
    private static void HealingCap()
    {
        var b = HealthBattle(allyHealth: 80); var result = Apply(b, Heal(amount: 50));
        Equal(20, result.ResolvedAmount); Equal(100, result.HealthAfter);
        Equal(2, b.Abilities.GetState("a").MainCooldownRemaining);
    }
    private static void SelfHealing()
    {
        var b = HealthBattle(actorHealth: 70); Equal(30, Apply(b, Heal("a", 100)).HealthChanged);
        Equal(100, b.Health.GetState("a").CurrentHealth);
    }
    private static void TeamValidation()
    {
        var b = HealthBattle(); RejectHealth(b, Hit("a"), HealthActionFailure.WrongTeam);
        RejectHealth(b, Hit("c"), HealthActionFailure.WrongTeam);
        RejectHealth(b, Heal("b"), HealthActionFailure.WrongTeam);
        Equal(100, b.Health.GetState("b").CurrentHealth); Equal(60, b.Health.GetState("c").CurrentHealth);
    }
    private static void TargetingValidation()
    {
        var b = HealthBattle();
        foreach (var pair in new[] { (TargetingVerdict.OutOfRange, HealthActionFailure.OutOfRange),
            (TargetingVerdict.Obstructed, HealthActionFailure.Obstructed), (TargetingVerdict.Incompatible, HealthActionFailure.Incompatible) })
        {
            RejectHealth(b, Hit(slot: AbilitySlot.Signature, verdict: pair.Item1), pair.Item2);
            RejectHealth(b, Heal(verdict: pair.Item1), pair.Item2);
        }
        Equal(100, b.Health.GetState("b").CurrentHealth); Equal(60, b.Health.GetState("c").CurrentHealth);
    }
    private static void NoRevival()
    {
        var b = HealthBattle(); b.EndActivation(b.CurrentActivation.ActivationId); Equal("b", b.BeginNextActivation().UnitId);
        Apply(b, Hit("c", 100)); NextHealthRound(b);
        RejectHealth(b, Heal("c"), HealthActionFailure.TargetInactive);
        Equal(0, b.Health.GetState("c").CurrentHealth);
    }
    private static void FullHealthHealing()
    {
        var b = HealthBattle(allyHealth: 100); RejectHealth(b, Heal(), HealthActionFailure.AlreadyFullHealth);
        Apply(b, Hit()); Equal(80, b.Health.GetState("b").CurrentHealth);
    }
    private static void InvalidHealthRequests()
    {
        var b = HealthBattle(); RejectHealth(b, null, HealthActionFailure.InvalidRequest);
        Throws<ArgumentOutOfRangeException>(() => Hit(amount: 0));
        Throws<ArgumentOutOfRangeException>(() => Heal(amount: -1));
        Throws<ArgumentException>(() => Hit(" "));
        Throws<ArgumentOutOfRangeException>(() => Hit(slot: AbilitySlot.Passive));
        Throws<ArgumentOutOfRangeException>(() => Hit(verdict: (TargetingVerdict)99));
        Throws<ArgumentOutOfRangeException>(() => new HealthAction(AbilitySlot.Main, (HealthEffectKind)99, "b", 1, TargetingVerdict.Legal));
        Equal(true, b.CurrentActivation.PrimaryActionAvailable);
    }
    private static void MissingHealth()
    {
        var b = HealthBattle(); RejectHealth(b, Hit("missing"), HealthActionFailure.TargetHealthMissing);
        var missing = B(U("a", 20), U("b", 10));
        missing.Abilities.RegisterKit("a", new UnitAbilityDefinition(2, SignatureReadiness.ReadyAtDeployment));
        missing.Health.RegisterHealth("b", new UnitHealthDefinition("other", 100, 0));
        missing.StartNextRound(); var t = missing.BeginNextActivation();
        Equal(false, missing.Health.TryApply(t.ActivationId, Hit(), out _, out var failure));
        Equal(HealthActionFailure.ActorHealthMissing, failure); Equal(true, missing.CurrentActivation.PrimaryActionAvailable);
    }
    private static void StaleHealthAction()
    {
        var b = HealthBattle(); long old = b.CurrentActivation.ActivationId; NextHealthRound(b);
        Equal(false, b.Health.TryApply(old, Hit(), out _, out var failure));
        Equal(HealthActionFailure.AbilityUnavailable, failure); Equal(100, b.Health.GetState("b").CurrentHealth);
        Equal(true, b.CurrentActivation.PrimaryActionAvailable);
    }
    private static void SignatureHealthAction()
    {
        var b = HealthBattle(); RejectHealth(b, Hit("missing", slot: AbilitySlot.Signature), HealthActionFailure.TargetHealthMissing);
        Apply(b, Hit(slot: AbilitySlot.Signature)); Equal(true, b.Abilities.GetState("a").SignatureUsed);
        NextHealthRound(b); RejectHealth(b, Hit(slot: AbilitySlot.Signature), HealthActionFailure.AbilityUnavailable);
        Equal(80, b.Health.GetState("b").CurrentHealth);
    }
    private static void MainHealthCooldown()
    {
        var b = HealthBattle(); RejectHealth(b, Hit(slot: AbilitySlot.Main, verdict: TargetingVerdict.OutOfRange), HealthActionFailure.OutOfRange);
        Apply(b, Hit(slot: AbilitySlot.Main)); Equal(2, b.Abilities.GetState("a").MainCooldownRemaining);
        NextHealthRound(b); RejectHealth(b, Hit(slot: AbilitySlot.Main), HealthActionFailure.AbilityUnavailable);
        Equal(80, b.Health.GetState("b").CurrentHealth);
    }
    private static void HealingReadiness()
    {
        var b = HealthBattle(readiness: SignatureReadiness.AfterMainAbility);
        RejectHealth(b, Heal("a"), HealthActionFailure.AlreadyFullHealth);
        Equal(false, b.Abilities.GetState("a").SignatureReady);
        Apply(b, Heal()); Equal(true, b.Abilities.GetState("a").SignatureReady);
    }
    private static void DamageReadiness()
    {
        var b = HealthBattle(readiness: SignatureReadiness.AfterNormalAttack);
        RejectHealth(b, Hit("c"), HealthActionFailure.WrongTeam);
        Equal(false, b.Abilities.GetState("a").SignatureReady);
        Apply(b, Hit()); Equal(true, b.Abilities.GetState("a").SignatureReady);
    }
    private static void ExtractedTarget()
    {
        var b = HealthBattle(); b.RemoveUnit("b"); RejectHealth(b, Hit(), HealthActionFailure.TargetInactive);
        Equal(100, b.Health.GetState("b").CurrentHealth); Equal(false, b.Health.GetState("b").IsDefeated);
    }
    private static void HealthRegistration()
    {
        var b = HealthBattle(); Apply(b, Hit());
        Throws<InvalidOperationException>(() => b.Health.RegisterHealth("b", new UnitHealthDefinition("opponents", 100, 0)));
        Equal(80, b.Health.GetState("b").CurrentHealth);
        var fresh = B(U("a"));
        Throws<ArgumentOutOfRangeException>(() => new UnitHealthDefinition("x", 0, 0));
        Throws<ArgumentOutOfRangeException>(() => new UnitHealthDefinition("x", 1, -1));
        Throws<ArgumentException>(() => new UnitHealthDefinition(" ", 1, 0));
        Throws<ArgumentOutOfRangeException>(() => fresh.Health.RegisterHealth("a", new UnitHealthDefinition("x", 10, 0), 11));
        Throws<ArgumentOutOfRangeException>(() => fresh.Health.RegisterHealth("a", new UnitHealthDefinition("x", 10, 0), 0));
        fresh.StartNextRound(); fresh.BeginNextActivation();
        Throws<InvalidOperationException>(() => fresh.Health.RegisterHealth("a", new UnitHealthDefinition("x", 10, 0)));
    }
    private static void EndedHealthBattle()
    {
        var b = HealthBattle(); long id = b.CurrentActivation.ActivationId; b.EndBattle();
        Equal(false, b.Health.TryApply(id, Hit(), out _, out var failure));
        Equal(HealthActionFailure.AbilityUnavailable, failure); Equal(100, b.Health.GetState("b").CurrentHealth);
        Throws<InvalidOperationException>(() => b.Health.RegisterHealth("b", new UnitHealthDefinition("x", 10, 0)));
    }
    private static void HealthSnapshots()
    {
        var b = HealthBattle(); var old = b.Health.GetState("b"); var request = Hit();
        b.Health.TryPreview(b.CurrentActivation.ActivationId, request, out var preview, out _);
        Apply(b, Hit(amount: 10)); NextHealthRound(b); var result = Apply(b, request);
        Equal(100, old.CurrentHealth); Equal(80, preview.HealthAfter);
        Equal(90, result.HealthBefore); Equal(70, result.HealthAfter);
    }
    private static void LargeHealing()
    {
        var b = B(U("a")); b.Abilities.RegisterKit("a", new UnitAbilityDefinition(2, SignatureReadiness.ReadyAtDeployment));
        b.Health.RegisterHealth("a", new UnitHealthDefinition("same", int.MaxValue, 0), 1);
        b.StartNextRound(); b.BeginNextActivation();
        var result = Apply(b, Heal("a", int.MaxValue));
        Equal(int.MaxValue - 1, result.HealthChanged); Equal(int.MaxValue, result.HealthAfter);
    }
    private static void ZeroResolvedDamage()
    {
        var b = HealthBattle(enemyArmor: 900, rules: new DamageRules(100, 1m, false));
        var result = Apply(b, Hit(amount: 1));
        Equal(0, result.HealthChanged); Equal(false, b.CurrentActivation.PrimaryActionAvailable);
        Equal(true, b.IsUnitEligible("b"));
    }
}
