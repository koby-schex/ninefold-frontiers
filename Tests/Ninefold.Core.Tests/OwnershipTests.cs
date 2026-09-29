using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ninefold.Core.Flow;
using Ninefold.Core.Progression;

internal static partial class Program
{
    private static (string Name, Action Run)[] OwnershipTests() => new (string, Action)[]
    {
        ("Initial owned units persist and copy the bootstrap input", OwnershipInitial),
        ("Unlock atomically spends fragments and grants ownership", OwnershipUnlock),
        ("Unlock retries across reload return original receipt without spending", OwnershipRetry),
        ("Unlock operation cannot target a different unit", OwnershipConflict),
        ("Owned unit cannot consume fragments again", OwnershipAlreadyOwned),
        ("Insufficient and unrelated fragments cannot unlock", OwnershipInsufficient),
        ("Claim replay preserves unlocks and leftover fragments", OwnershipEarnAfterUnlock),
        ("Defeat preserves saved ownership and fragment balance", OwnershipDefeat),
        ("Failed or torn unlock publishes no spending or ownership", OwnershipInterrupted),
        ("Lost unlock acknowledgement cannot double spending", OwnershipAmbiguous),
        ("Corrupt latest unlock rolls spending and ownership back together", OwnershipRecovery),
        ("Concurrent unlock requests on one store spend once", OwnershipConcurrent),
        ("Unlock snapshots and receipts cannot be mutated", OwnershipImmutable),
        ("Legacy schema one receipts migrate without invented ownership", OwnershipLegacy),
        ("Conflicting bootstrap ownership fails closed", OwnershipConflictHistory),
        ("Fragment amounts can be earned again after spending at long limit", OwnershipLargeTotals),
        ("Flow starter roster requires three standard units of one faction", OwnershipStarter),
        ("Flow ownership survives reopening without external unlocked flags", OwnershipFlowReload),
        ("Flow unlock immediately enables squad and campaign entry", OwnershipCampaign),
        ("Apex unlock does not count as a campaign standard unit", OwnershipApexGate),
        ("Flow cannot unlock during an active battle", OwnershipBattleGate),
        ("Flow lost unlock acknowledgement reloads saved ownership", OwnershipFlowFailure),
        ("Real profile directory persists unlock cost and ownership", OwnershipDirectory)
    };
    private static UnitUnlockDefinition UnlockDef(string unit = "unit-a", long cost = 5, string resource = "test-resource", string revision = "test-v1")
        => new UnitUnlockDefinition(unit, resource, cost, revision);
    private static LocalProgressStore Funded(out ProgressFiles files)
    { var s = NewProgress(out files); s.Claim(Result(), Rewards()); return s; }
    private static void OwnershipInitial()
    {
        var files = new ProgressFiles(); var units = new[] { "a", "b", "c" };
        var s = new LocalProgressStore(files, "test-profile"); s.Create(units); units[0] = "edited";
        var p = new LocalProgressStore(files, "test-profile").Load().Progress;
        Equal(true, p.Owns("a")); Equal(false, p.Owns("edited")); Equal(3, p.OwnedUnits.Count); Equal(1L, p.Generation);
        Throws<InvalidOperationException>(() => s.Create(new[] { "extra" }));
    }
    private static void OwnershipUnlock()
    {
        var s = Funded(out _); var r = s.Unlock("op-1", UnlockDef());
        Equal(false, r.AlreadyApplied); Equal(true, r.Saved.Progress.Owns("unit-a")); Equal(5L, r.Saved.Progress.Balance("test-resource"));
        Equal(3L, r.Saved.Generation); Equal(1L, r.Saved.Progress.Missions["test-mission"].VictoryCount);
    }
    private static void OwnershipRetry()
    {
        var s = Funded(out var f); s.Unlock("op-1", UnlockDef()); f.Inner.Fault = 1;
        var r = new LocalProgressStore(f, "test-profile").Unlock("op-1", UnlockDef(cost:99, revision:"changed"));
        Equal(true, r.AlreadyApplied); Equal(5L, r.Receipt.Cost); Equal("test-v1", r.Receipt.DefinitionRevision);
        Equal(5L, r.Saved.Progress.Balance("test-resource")); Equal(3, f.Writes);
    }
    private static void OwnershipConflict()
    {
        var s = Funded(out var f); s.Unlock("op-1", UnlockDef());
        Throws<InvalidOperationException>(() => s.Unlock("op-1", UnlockDef("unit-b"))); Equal(3, f.Writes);
    }
    private static void OwnershipAlreadyOwned()
    {
        var s = Funded(out var f); s.Unlock("op-1", UnlockDef());
        Throws<InvalidOperationException>(() => s.Unlock("op-2", UnlockDef())); Equal(5L, s.Load().Progress.Balance("test-resource")); Equal(3, f.Writes);
        var initial = new LocalProgressStore(new ProgressFiles(), "initial"); initial.Create(new[] { "unit-a" });
        Throws<InvalidOperationException>(() => initial.Unlock("op", UnlockDef()));
    }
    private static void OwnershipInsufficient()
    {
        var s = Funded(out var f);
        Throws<InvalidOperationException>(() => s.Unlock("op", UnlockDef(cost:11)));
        Throws<InvalidOperationException>(() => s.Unlock("op", UnlockDef(resource:"other-fragments")));
        Equal(2, f.Writes); Equal(false, s.Load().Progress.Owns("unit-a"));
        Throws<ArgumentOutOfRangeException>(() => UnlockDef(cost:0));
    }
    private static void OwnershipEarnAfterUnlock()
    {
        var s = Funded(out _); s.Unlock("op", UnlockDef()); s.Claim(Result("attempt-2"), Rewards());
        Equal(7L, s.Load().Progress.Balance("test-resource")); Equal(true, s.Load().Progress.Owns("unit-a"));
        s.Claim(Result("attempt-2"), Rewards()); Equal(7L, s.Load().Progress.Balance("test-resource"));
    }
    private static void OwnershipDefeat()
    {
        var s = Funded(out _); s.Unlock("op", UnlockDef()); s.Claim(Result("loss", victory:false), Rewards());
        Equal(true, s.Load().Progress.Owns("unit-a")); Equal(5L, s.Load().Progress.Balance("test-resource"));
    }
    private static void OwnershipInterrupted()
    {
        foreach (int fault in new[] { 1, 2 })
        {
            var s = Funded(out var f); f.Inner.Fault = fault;
            Throws<IOException>(() => s.Unlock("op", UnlockDef())); f.Inner.Fault = 0;
            var p = new LocalProgressStore(f, "test-profile").Load().Progress;
            Equal(false, p.Owns("unit-a")); Equal(10L, p.Balance("test-resource")); Equal(0, p.Unlocks.Count);
            s.Unlock("op", UnlockDef()); Equal(5L, s.Load().Progress.Balance("test-resource"));
        }
    }
    private static void OwnershipAmbiguous()
    {
        var s = Funded(out var f); f.Inner.Fault = 3; Throws<IOException>(() => s.Unlock("op", UnlockDef())); f.Inner.Fault = 0;
        var r = new LocalProgressStore(f, "test-profile").Unlock("op", UnlockDef());
        Equal(true, r.AlreadyApplied); Equal(5L, r.Saved.Progress.Balance("test-resource")); Equal(3, f.Writes);
    }
    private static void OwnershipRecovery()
    {
        var s = Funded(out var f); s.Unlock("op", UnlockDef()); f.Inner.Slots[0][20] ^= 1;
        var loaded = s.Load(); Equal(true, loaded.Recovered); Equal(false, loaded.Progress.Owns("unit-a")); Equal(10L, loaded.Progress.Balance("test-resource"));
        s.Unlock("op", UnlockDef()); Equal(5L, s.Load().Progress.Balance("test-resource")); Equal(true, s.Load().Progress.Owns("unit-a"));
    }
    private static void OwnershipConcurrent()
    {
        var s = Funded(out var f); var d = UnlockDef(); Parallel.For(0,8,_ => s.Unlock("op", d));
        Equal(3, f.Writes); Equal(1, s.Load().Progress.Unlocks.Count);
    }
    private static void OwnershipImmutable()
    {
        var s = Funded(out _); var before = s.Load().Progress; var after = s.Unlock("op", UnlockDef()).Saved.Progress;
        Equal(false, before.Owns("unit-a")); Equal(10L, before.Balance("test-resource"));
        Throws<NotSupportedException>(() => ((IList<string>)after.OwnedUnits).Clear());
        Throws<NotSupportedException>(() => ((IList<UnitUnlockReceipt>)after.Unlocks).Clear());
    }
    private static byte[] LegacyProfile(byte[] current)
    {
        // Fixture conversion is valid only with empty ownership, unlocks and campaign receipts.
        var bytes = current.Take(current.Length - 44).Concat(new byte[32]).ToArray();
        using var stream = new MemoryStream(bytes); using var r = new BinaryReader(stream);
        stream.Position = 8; r.ReadString(); Array.Copy(BitConverter.GetBytes(1), 0, bytes, (int)stream.Position, 4); Rehash(bytes); return bytes;
    }
    private static void OwnershipLegacy()
    {
        var s = Funded(out var f); f.Inner.Slots[0] = LegacyProfile(f.Inner.Slots[0]); f.Inner.Slots[1] = LegacyProfile(f.Inner.Slots[1]);
        var p = s.Load().Progress; Equal(0, p.OwnedUnits.Count); Equal(10L, p.Balance("test-resource")); Equal(1, p.Claims.Count);
        s.Unlock("op", UnlockDef()); Equal(true, s.Load().Progress.Owns("unit-a")); Equal(5L, s.Load().Progress.Balance("test-resource"));
    }
    private static void OwnershipConflictHistory()
    {
        var f = new ProgressFiles(); var a = new LocalProgressStore(f, "test-profile"); a.Create(new[] { "seed-a" }); a.Claim(Result(), Rewards());
        var other = new ProgressFiles(); new LocalProgressStore(other, "test-profile").Create(new[] { "seed-b" });
        f.Inner.Slots[0] = other.Inner.Slots[0]; Throws<InvalidDataException>(() => a.Load());
    }
    private static void OwnershipLargeTotals()
    {
        var s = NewProgress(out _); s.Claim(Result(), Rewards(first:long.MaxValue)); s.Unlock("op", UnlockDef(cost:long.MaxValue));
        s.Claim(Result("attempt-2"), Rewards(replay:long.MaxValue)); Equal(long.MaxValue, s.Load().Progress.Balance("test-resource"));
    }
    private static void OwnershipStarter()
    {
        var x = new FlowFixture(); var f = x.Make();
        foreach (var units in new[] { new[] { "a", "c" }, new[] { "a", "a", "c" }, new[] { "a", "c", "x" }, new[] { "a", "c", "z" }, new[] { "a", "c", "missing" } })
            Throws<ArgumentException>(() => f.CreateProfile(units));
        Equal(0, x.Profiles.Writes); f.CreateProfile(new[] { "a", "c", "d" });
        Equal(3, f.Progress.OwnedUnits.Count); Equal(false, f.Progress.Owns("x"));
    }
    private static void OwnershipFlowReload()
    {
        var x = new FlowFixture(); var f = x.Make(); f.CreateProfile(new[] { "a", "c", "d" }); f = x.Reopen();
        Equal(SquadFailure.None, f.ValidateSquad("test-mission", new[] { "a", "c" }));
        Equal(SquadFailure.LockedUnit, f.ValidateSquad("test-mission", new[] { "x" }));
    }
    private static MissionFlow UnlockFlow(FlowFixture x, bool apex = false)
    {
        var target = new RosterUnit("target", "other", apex, new UnitUnlockDefinition("target", "target-fragments", 5, "v1"));
        var roster = FlowRoster().Concat(new[] { target, new RosterUnit("other-2", "other", false, UnlockDef("other-2", resource:"other-2-fragments")) }).ToArray();
        var p = new LocalProgressStore(x.Profiles, "flow-profile"); p.Create(new[] { "a", "c", "d", "z", "other-2" });
        p.Claim(Result(), new MissionRewardPolicy("test-mission", "v1", new[] { new ResourceGrant("target-fragments", 10) }, Array.Empty<ResourceGrant>()));
        var f = x.Make(new[] { FlowEntry(), FlowEntry("other-campaign", "other", true) }, roster); f.Open(); return f;
    }
    private static void OwnershipCampaign()
    {
        var x = new FlowFixture(); var f = UnlockFlow(x);
        Equal(SquadFailure.CampaignRoster, f.ValidateSquad("other-campaign", new[] { "z" }));
        f.UnlockUnit("op", "target"); Equal(SquadFailure.None, f.ValidateSquad("other-campaign", new[] { "z" }));
        Equal(SquadFailure.None, f.ValidateSquad("test-mission", new[] { "target" }));
        f.Open(); Equal(SquadFailure.None, f.ValidateSquad("other-campaign", new[] { "z" })); Equal(5L, f.Progress.Balance("target-fragments"));
    }
    private static void OwnershipApexGate()
    {
        var x = new FlowFixture(); var f = UnlockFlow(x, true); f.UnlockUnit("op", "target");
        Equal(SquadFailure.CampaignRoster, f.ValidateSquad("other-campaign", new[] { "z" }));
    }
    private static void OwnershipBattleGate()
    {
        var x = new FlowFixture(); var f = UnlockFlow(x); StartFlow(f);
        Throws<InvalidOperationException>(() => f.UnlockUnit("op", "target")); Equal(10L, f.Progress.Balance("target-fragments"));
    }
    private static void OwnershipFlowFailure()
    {
        var x = new FlowFixture(); var f = UnlockFlow(x); x.Profiles.Inner.Fault = 3;
        Throws<IOException>(() => f.UnlockUnit("op", "target")); Equal(true, f.NeedsReload); x.Profiles.Inner.Fault = 0; f.Open();
        Equal(true, f.Progress.Owns("target")); Equal(5L, f.Progress.Balance("target-fragments")); Equal(true, f.UnlockUnit("op", "target").AlreadyApplied);
    }
    private static void OwnershipDirectory()
    {
        string dir = Path.Combine(Path.GetTempPath(), "ninefold-ownership-" + Guid.NewGuid().ToString("N"));
        try
        {
            var s = new LocalProgressStore(new DirectoryProgressSaveFiles(dir), "p"); s.Create(new[] { "starter" });
            s.Claim(Result(), Rewards()); s.Unlock("op", UnlockDef());
            var p = new LocalProgressStore(new DirectoryProgressSaveFiles(dir), "p").Load().Progress;
            Equal(true, p.Owns("starter")); Equal(true, p.Owns("unit-a")); Equal(5L, p.Balance("test-resource"));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }
}
