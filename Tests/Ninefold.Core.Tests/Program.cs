using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;

// Dependency-free executable tests: failures return nonzero to GitHub Actions.
// Compiles the actual Unity Core source through its netstandard2.1 project.
internal static class Program
{
    private static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            ("Initiative descending, one activation per unit", InitiativeOrder),
            ("Ties independent of enrollment order", StableTies),
            ("Initiative changes wait until next round", DeferredInitiative),
            ("Movement can split around action", SplitMovement),
            ("Action first still allows full movement", ActionBeforeMovement),
            ("Second action rejected without state change", OneAction),
            ("Overspending and invalid movement rejected atomically", InvalidMovement),
            ("No active-turn refresh or early round restart", NoRefresh),
            ("Stale commands cannot spend a new turn", StaleCommands),
            ("Unused resources forfeited and budgets reset", FreshBudgets),
            ("Reinforcements wait until next round", Reinforcements),
            ("Removal skips pending and ends active units", Removal),
            ("All units removed cannot create an empty round", EmptyRoster),
            ("Empty initial roster can enroll before first round", InitialEmptyRoster),
            ("Battle ending prevents further commands", TerminalBattle),
            ("Invalid definitions and duplicate IDs rejected", InvalidDefinitions),
            ("Zero movement unit retains primary action", ZeroMovement),
            ("Old activation views and round lists stay immutable", ImmutableViews),
            ("Explicit round lifecycle handles skipped final unit", RoundBoundary),
            ("Fractional path costs do not gain or lose allowance", FractionalMovement),
            ("Mixed abstract teams share the same scheduler", ManyRounds)
        };
        int failed = 0;
        foreach (var test in tests)
        {
            try { test.Run(); Console.WriteLine("PASS " + test.Name); }
            catch (Exception ex) { failed++; Console.Error.WriteLine("FAIL " + test.Name + ": " + ex); }
        }
        Console.WriteLine($"{tests.Length - failed}/{tests.Length} tests passed.");
        return failed == 0 ? 0 : 1;
    }

    private static UnitTurnDefinition U(string id, int initiative = 10, decimal movement = 5m)
        => new UnitTurnDefinition(id, initiative, movement);
    private static BattleTurnController B(params UnitTurnDefinition[] units)
        => new BattleTurnController(units);
    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected {expected}; got {actual}.");
    }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    private static string Drain(BattleTurnController battle)
    {
        var ids = new List<string>();
        ActivationView turn;
        while ((turn = battle.BeginNextActivation()) != null)
        {
            ids.Add(turn.UnitId);
            battle.EndActivation(turn.ActivationId);
        }
        return string.Join(",", ids);
    }
    private static void InitiativeOrder()
    {
        var b = B(U("slow", 1), U("fast", 100), U("middle", 50));
        b.StartNextRound(); Equal("fast,middle,slow", Drain(b));
        Equal(true, b.IsRoundComplete); Equal(1, b.RoundNumber);
    }
    private static void StableTies()
    {
        foreach (var roster in new[] { new[] { U("c"), U("a"), U("b") }, new[] { U("b"), U("c"), U("a") } })
        {
            var b = B(roster); b.StartNextRound(); Equal("a,b,c", Drain(b));
        }
    }
    private static void DeferredInitiative()
    {
        var b = B(U("a", 20), U("b", 10)); b.StartNextRound();
        b.SetInitiative("b", 100); Equal("a,b", Drain(b));
        b.StartNextRound(); Equal("b,a", Drain(b));
    }
    private static void SplitMovement()
    {
        var b = B(U("a", movement: 7m)); b.StartNextRound(); var t = b.BeginNextActivation();
        b.SpendMovement(t.ActivationId, 2.5m); b.SpendPrimaryAction(t.ActivationId);
        b.SpendMovement(t.ActivationId, 4.5m);
        Equal(0m, b.CurrentActivation.MovementRemaining); Equal(false, b.CurrentActivation.PrimaryActionAvailable);
    }
    private static void ActionBeforeMovement()
    {
        var b = B(U("a")); b.StartNextRound(); var t = b.BeginNextActivation();
        b.SpendPrimaryAction(t.ActivationId); b.SpendMovement(t.ActivationId, 5m);
        Equal(0m, b.CurrentActivation.MovementRemaining);
    }
    private static void OneAction()
    {
        var b = B(U("a")); b.StartNextRound(); var t = b.BeginNextActivation();
        b.SpendPrimaryAction(t.ActivationId);
        Throws<InvalidOperationException>(() => b.SpendPrimaryAction(t.ActivationId));
        Equal(5m, b.CurrentActivation.MovementRemaining); Equal(false, b.CurrentActivation.PrimaryActionAvailable);
    }
    private static void InvalidMovement()
    {
        var b = B(U("a")); b.StartNextRound(); var t = b.BeginNextActivation();
        foreach (var cost in new[] { 0m, -1m, decimal.MinValue })
            Throws<ArgumentOutOfRangeException>(() => b.SpendMovement(t.ActivationId, cost));
        Throws<InvalidOperationException>(() => b.SpendMovement(t.ActivationId, decimal.MaxValue));
        Equal(5m, b.CurrentActivation.MovementRemaining); Equal(true, b.CurrentActivation.PrimaryActionAvailable);
    }
    private static void NoRefresh()
    {
        var b = B(U("a")); b.StartNextRound(); var t = b.BeginNextActivation(); b.SpendMovement(t.ActivationId, 2m);
        Throws<InvalidOperationException>(() => b.BeginNextActivation());
        Throws<InvalidOperationException>(() => b.StartNextRound());
        Equal(3m, b.CurrentActivation.MovementRemaining); Equal(1, b.RoundNumber);
    }
    private static void StaleCommands()
    {
        var b = B(U("a")); b.StartNextRound(); var old = b.BeginNextActivation(); b.EndActivation(old.ActivationId);
        b.StartNextRound(); var current = b.BeginNextActivation();
        Throws<InvalidOperationException>(() => b.SpendPrimaryAction(old.ActivationId));
        Throws<InvalidOperationException>(() => b.SpendMovement(old.ActivationId, 1m));
        Throws<InvalidOperationException>(() => b.EndActivation(old.ActivationId));
        Equal(current.ActivationId, b.CurrentActivation.ActivationId);
        Equal(true, b.CurrentActivation.PrimaryActionAvailable); Equal(5m, b.CurrentActivation.MovementRemaining);
    }
    private static void FreshBudgets()
    {
        var b = B(U("a")); b.StartNextRound(); var t = b.BeginNextActivation();
        b.SpendMovement(t.ActivationId, 2m); b.SpendPrimaryAction(t.ActivationId); b.EndActivation(t.ActivationId);
        b.StartNextRound(); t = b.BeginNextActivation();
        Equal(5m, t.MovementRemaining); Equal(true, t.PrimaryActionAvailable);
    }
    private static void Reinforcements()
    {
        var b = B(U("a")); b.StartNextRound(); b.RegisterUnit(U("new", 99));
        Equal("a", Drain(b)); b.StartNextRound(); Equal("new,a", Drain(b));
    }
    private static void Removal()
    {
        var b = B(U("a", 30), U("b", 20), U("c", 10)); b.StartNextRound();
        b.RemoveUnit("b"); var t = b.BeginNextActivation(); Equal("a", t.UnitId);
        b.RemoveUnit("a"); Equal<ActivationView>(null, b.CurrentActivation);
        Throws<InvalidOperationException>(() => b.SpendPrimaryAction(t.ActivationId));
        Equal("c", Drain(b)); b.StartNextRound(); Equal("c", Drain(b));
        Throws<ArgumentException>(() => b.RegisterUnit(U("a")));
    }
    private static void EmptyRoster()
    {
        var b = B(U("a")); b.StartNextRound(); b.RemoveUnit("a");
        Equal(true, b.IsRoundComplete); Equal<ActivationView>(null, b.BeginNextActivation());
        Throws<InvalidOperationException>(() => b.StartNextRound()); Equal(1, b.RoundNumber);
    }
    private static void InitialEmptyRoster()
    {
        var b = B(); Equal(false, b.IsRoundComplete);
        Throws<InvalidOperationException>(() => b.StartNextRound()); Equal(0, b.RoundNumber);
        b.RegisterUnit(U("a")); b.StartNextRound(); Equal("a", Drain(b));
    }
    private static void TerminalBattle()
    {
        var b = B(U("a")); b.StartNextRound(); var t = b.BeginNextActivation(); b.EndBattle(); b.EndBattle();
        Equal(true, b.IsBattleEnded); Equal<ActivationView>(null, b.CurrentActivation);
        Throws<InvalidOperationException>(() => b.StartNextRound());
        Throws<InvalidOperationException>(() => b.BeginNextActivation());
        Throws<InvalidOperationException>(() => b.SpendMovement(t.ActivationId, 1m));
        Throws<InvalidOperationException>(() => b.SpendPrimaryAction(t.ActivationId));
        Throws<InvalidOperationException>(() => b.EndActivation(t.ActivationId));
        Throws<InvalidOperationException>(() => b.RegisterUnit(U("b")));
        Throws<InvalidOperationException>(() => b.SetInitiative("a", 20));
        Throws<InvalidOperationException>(() => b.RemoveUnit("a"));
    }
    private static void InvalidDefinitions()
    {
        Throws<ArgumentException>(() => U(" ")); Throws<ArgumentException>(() => U(null));
        Throws<ArgumentOutOfRangeException>(() => U("a", -1));
        Throws<ArgumentOutOfRangeException>(() => U("a", movement: -1m));
        Throws<ArgumentNullException>(() => new BattleTurnController(null));
        Throws<ArgumentException>(() => B(U("a"), U("a")));
        var b = B(U("a"));
        Throws<ArgumentNullException>(() => b.RegisterUnit(null));
        Throws<ArgumentException>(() => b.RemoveUnit("missing"));
        Throws<ArgumentOutOfRangeException>(() => b.SetInitiative("a", -1));
        Throws<ArgumentException>(() => b.SetInitiative("missing", 9));
        Throws<InvalidOperationException>(() => b.BeginNextActivation());
        Throws<InvalidOperationException>(() => b.SpendPrimaryAction(1));
    }
    private static void ZeroMovement()
    {
        var b = B(U("a", movement: 0m)); b.StartNextRound(); var t = b.BeginNextActivation();
        Throws<InvalidOperationException>(() => b.SpendMovement(t.ActivationId, 0.1m));
        b.SpendPrimaryAction(t.ActivationId); Equal(false, b.CurrentActivation.PrimaryActionAvailable);
    }
    private static void ImmutableViews()
    {
        var b = B(U("a", 20), U("b", 10)); b.StartNextRound(); var order = b.RoundOrder;
        Throws<NotSupportedException>(() => ((IList<string>)order)[0] = "b");
        var t = b.BeginNextActivation(); b.SpendMovement(t.ActivationId, 1m); b.SpendPrimaryAction(t.ActivationId);
        Equal(5m, t.MovementRemaining); Equal(true, t.PrimaryActionAvailable);
        b.EndActivation(t.ActivationId); Drain(b); b.SetInitiative("b", 30); b.StartNextRound();
        Equal("a,b", string.Join(",", order)); Equal("b,a", string.Join(",", b.RoundOrder));
    }
    private static void RoundBoundary()
    {
        var b = B(U("a", 20), U("b", 10)); b.StartNextRound();
        Throws<InvalidOperationException>(() => b.StartNextRound());
        var t = b.BeginNextActivation(); b.EndActivation(t.ActivationId); b.RemoveUnit("b");
        Equal(true, b.IsRoundComplete); Equal<ActivationView>(null, b.BeginNextActivation());
        Equal<ActivationView>(null, b.BeginNextActivation()); Equal(1, b.RoundNumber);
        b.StartNextRound(); Equal(2, b.RoundNumber);
    }
    private static void FractionalMovement()
    {
        var b = B(U("a", movement: 1m)); b.StartNextRound(); var t = b.BeginNextActivation();
        for (int i = 0; i < 10; i++) b.SpendMovement(t.ActivationId, 0.1m);
        Equal(0m, b.CurrentActivation.MovementRemaining);
        Throws<InvalidOperationException>(() => b.SpendMovement(t.ActivationId, 0.1m));
    }
    private static void ManyRounds()
    {
        var roster = Enumerable.Range(0, 20).Select(i => U("unit-" + i.ToString("D2"), i % 5, i % 4)).ToArray();
        var b = B(roster); var expected = string.Join(",", roster.OrderByDescending(u => u.Initiative).ThenBy(u => u.UnitId, StringComparer.Ordinal).Select(u => u.UnitId));
        for (int round = 1; round <= 50; round++)
        {
            b.StartNextRound(); Equal(round, b.RoundNumber); Equal(expected, Drain(b));
        }
    }
}
