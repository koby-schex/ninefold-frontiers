using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;
using Ninefold.Core.Persistence;
using Ninefold.Core.Progression;

internal static partial class Program
{
    private static (string Name, Action Run)[] ProgressTests() => new (string, Action)[]
    {
        ("Progress requires explicit creation and cannot reset existing history", ProgressCreate),
        ("First clear records completion and exact authored bundle", ProgressFirstClear),
        ("Replay grants its bundle without repeating first-clear reward", ProgressReplay),
        ("Another mission receives its own first clear", ProgressOtherMission),
        ("Reopened result grants nothing twice and performs no write", ProgressDuplicate),
        ("Restored terminal battle retains reward claim identity", ProgressRestoredBattle),
        ("Duplicate receipt survives removed or changed reward policy", ProgressPolicyChange),
        ("Reused attempt with conflicting outcome or mission rejects", ProgressConflict),
        ("Defeat preserves balances and completion and consumes no first clear", ProgressDefeat),
        ("Defeat after victory preserves earned progress", ProgressLaterDefeat),
        ("Failed pre-write leaves no partial completion or reward", ProgressBeforeWrite),
        ("Torn write restores previous complete transaction", ProgressTornWrite),
        ("Lost write acknowledgement retry cannot double rewards", ProgressAfterWrite),
        ("Corrupt newest checkpoint rolls receipt and grants back together", ProgressRecovery),
        ("Corrupt older checkpoint does not roll newest state back", ProgressOldCorrupt),
        ("All corrupt files fail closed without resetting", ProgressAllCorrupt),
        ("Unsupported schema blocks both reads and writes", ProgressVersion),
        ("Wrong profile identity preserves both files", ProgressIdentity),
        ("Divergent valid histories fail closed", ProgressDivergent),
        ("Resource overflow aborts transaction before writing", ProgressOverflow),
        ("Reward definitions copy arrays and reject invalid grants", ProgressDefinitions),
        ("Published progress snapshots remain immutable", ProgressImmutable),
        ("Wrong mission and incomplete results cannot claim", ProgressInvalidClaim),
        ("Simultaneous duplicate claims serialize on one store", ProgressConcurrent),
        ("Profile files coexist with battle saves across process-style reload", ProgressDirectory)
    };
    private sealed class ProgressFiles : IProgressSaveFiles
    {
        internal readonly FakeSaveFiles Inner = new FakeSaveFiles();
        internal int Writes;
        public byte[] Read(int slot) => Inner.Read(slot);
        public void WriteDurable(int slot, byte[] bytes) { Writes++; Inner.WriteDurable(slot, bytes); }
    }
    private static LocalProgressStore NewProgress(out ProgressFiles files)
    {
        files = new ProgressFiles(); var store = new LocalProgressStore(files, "test-profile"); store.Create(); return store;
    }
    private static MissionRewardPolicy Rewards(string mission = "test-mission", long first = 10, long replay = 2, string revision = "test-v1")
        => new MissionRewardPolicy(mission, revision, new[] { new ResourceGrant("test-resource", first) }, new[] { new ResourceGrant("test-resource", replay) });
    private static BattleTurnController FinishedBattle(string attempt = "attempt-1", string mission = "test-mission", bool victory = true)
    {
        var b = FieldBattle(start:false);
        b.ConfigureMission(new MissionDefinition(mission, attempt, new[] { "a", "c" }, ObjectiveDefinition.Stabilize("primary", 1, InteractAt()), OutcomePriority.FailureFirst));
        b.StartNextRound(); b.BeginNextActivation();
        if (victory) Interact(b, "primary");
        else { b.RemoveUnit("a"); b.RemoveUnit("c"); b.Mission.Evaluate(); }
        return b;
    }
    private static BattleResult Result(string attempt = "attempt-1", string mission = "test-mission", bool victory = true)
        => FinishedBattle(attempt, mission, victory).Mission.Result;
    private static void ProgressCreate()
    {
        var files = new ProgressFiles(); var s = new LocalProgressStore(files, "test-profile");
        Equal<LoadedProgress>(null, s.Load()); Throws<InvalidOperationException>(() => s.Claim(Result(), Rewards()));
        Equal(1L, s.Create().Generation); Equal(0, s.Load().Progress.Claims.Count);
        Throws<InvalidOperationException>(() => s.Create()); Equal(1, files.Writes);
    }
    private static void ProgressFirstClear()
    {
        var s = NewProgress(out _); var c = s.Claim(Result(), Rewards());
        Equal(false, c.AlreadyClaimed); Equal(true, c.Receipt.IsFirstClear); Equal(10L, c.Saved.Progress.Balance("test-resource"));
        Equal(1L, c.Saved.Progress.Missions["test-mission"].VictoryCount); Equal("attempt-1", c.Saved.Progress.Missions["test-mission"].FirstClearAttemptId);
        Equal(2L, s.Load().Generation); Equal(0L, c.Saved.Progress.Balance("absent"));
    }
    private static void ProgressReplay()
    {
        var s = NewProgress(out _); s.Claim(Result(), Rewards()); var c = s.Claim(Result("attempt-2"), Rewards());
        Equal(false, c.Receipt.IsFirstClear); Equal(2L, c.Receipt.Grants[0].Amount); Equal(12L, s.Load().Progress.Balance("test-resource"));
        Equal(2L, s.Load().Progress.Missions["test-mission"].VictoryCount);
    }
    private static void ProgressOtherMission()
    {
        var s = NewProgress(out _); s.Claim(Result(), Rewards()); var c = s.Claim(Result("attempt-2", "other"), Rewards("other"));
        Equal(true, c.Receipt.IsFirstClear); Equal(20L, c.Saved.Progress.Balance("test-resource")); Equal(2, c.Saved.Progress.Missions.Count);
    }
    private static void ProgressDuplicate()
    {
        var s = NewProgress(out var f); var result = Result(); s.Claim(result, Rewards());
        f.Inner.Fault = 1; var c = new LocalProgressStore(f, "test-profile").Claim(result, Rewards());
        Equal(true, c.AlreadyClaimed); Equal(10L, c.Saved.Progress.Balance("test-resource")); Equal(2, f.Writes);
    }
    private static void ProgressRestoredBattle()
    {
        var s = NewProgress(out var f); var b = FinishedBattle(); var snapshot = BattleSave.Capture(b, SaveRevision);
        s.Claim(b.Mission.Result, Rewards());
        var restored = BattleSave.Restore(snapshot, SaveRevision);
        Equal(true, new LocalProgressStore(f, "test-profile").Claim(restored.Mission.Result, Rewards()).AlreadyClaimed);
        Equal(10L, s.Load().Progress.Balance("test-resource"));
    }
    private static void ProgressPolicyChange()
    {
        var s = NewProgress(out _); s.Claim(Result(), Rewards());
        Equal(10L, s.Claim(Result(), Rewards(first:999, revision:"test-v2")).Receipt.Grants[0].Amount);
        Equal("test-v1", s.Claim(Result(), null).Receipt.PolicyRevision);
        Equal(3L, s.Claim(Result("attempt-2"), Rewards(replay:3, revision:"test-v2")).Receipt.Grants[0].Amount);
    }
    private static void ProgressConflict()
    {
        var s = NewProgress(out var f); s.Claim(Result(), Rewards());
        Throws<InvalidOperationException>(() => s.Claim(Result(victory:false), Rewards()));
        Throws<InvalidOperationException>(() => s.Claim(Result(mission:"other"), Rewards("other"))); Equal(2, f.Writes);
    }
    private static void ProgressDefeat()
    {
        var s = NewProgress(out _); var c = s.Claim(Result(victory:false), Rewards());
        Equal(0, c.Receipt.Grants.Count); Equal(false, c.Receipt.IsFirstClear); Equal(0, c.Saved.Progress.Missions.Count);
        Equal(true, s.Claim(Result(victory:false), Rewards()).AlreadyClaimed);
        Equal(true, s.Claim(Result("attempt-2"), Rewards()).Receipt.IsFirstClear);
    }
    private static void ProgressLaterDefeat()
    {
        var s = NewProgress(out _); s.Claim(Result(), Rewards()); s.Claim(Result("attempt-2", victory:false), Rewards());
        Equal(10L, s.Load().Progress.Balance("test-resource")); Equal(1L, s.Load().Progress.Missions["test-mission"].VictoryCount);
    }
    private static void FailedProgressWrite(int fault)
    {
        var s = NewProgress(out var f); var before = s.Load(); f.Inner.Fault = fault;
        Throws<IOException>(() => s.Claim(Result(), Rewards()));
        var loaded = new LocalProgressStore(f, "test-profile").Load();
        Equal(0, loaded.Progress.Claims.Count); Equal(0, loaded.Progress.Missions.Count); Equal(0L, loaded.Progress.Balance("test-resource"));
        Equal(0, before.Progress.Claims.Count); f.Inner.Fault = 0;
        Equal(false, s.Claim(Result(), Rewards()).AlreadyClaimed); Equal(10L, s.Load().Progress.Balance("test-resource"));
    }
    private static void ProgressBeforeWrite() => FailedProgressWrite(1);
    private static void ProgressTornWrite() => FailedProgressWrite(2);
    private static void ProgressAfterWrite()
    {
        var s = NewProgress(out var f); f.Inner.Fault = 3; Throws<IOException>(() => s.Claim(Result(), Rewards())); f.Inner.Fault = 0;
        var c = new LocalProgressStore(f, "test-profile").Claim(Result(), Rewards());
        Equal(true, c.AlreadyClaimed); Equal(10L, c.Saved.Progress.Balance("test-resource")); Equal(2, f.Writes);
    }
    private static void ProgressRecovery()
    {
        var s = NewProgress(out var f); s.Claim(Result(), Rewards()); s.Claim(Result("attempt-2"), Rewards()); f.Inner.Slots[0][20] ^= 1;
        var loaded = s.Load(); Equal(true, loaded.Recovered); Equal(1, loaded.Progress.Claims.Count); Equal(10L, loaded.Progress.Balance("test-resource"));
        s.Claim(Result("attempt-2"), Rewards()); Equal(12L, s.Load().Progress.Balance("test-resource"));
        Equal(true, s.Claim(Result("attempt-2"), Rewards()).AlreadyClaimed); Equal(false, s.Load().Recovered);
    }
    private static void ProgressOldCorrupt()
    {
        var s = NewProgress(out var f); s.Claim(Result(), Rewards()); f.Inner.Slots[0][20] ^= 1;
        Equal(true, s.Load().Recovered); Equal(10L, s.Load().Progress.Balance("test-resource"));
        Equal(true, s.Claim(Result(), Rewards()).AlreadyClaimed);
        s.Claim(Result("attempt-2"), Rewards()); Equal(false, s.Load().Recovered);
    }
    private static void ProgressAllCorrupt()
    {
        var s = NewProgress(out var f); f.Inner.Slots[0] = new byte[12]; f.Inner.Slots[1] = new byte[20];
        Throws<InvalidDataException>(() => s.Load()); Throws<InvalidDataException>(() => s.Create());
        Throws<InvalidDataException>(() => s.Claim(Result(), Rewards())); Equal(1, f.Writes);
    }
    private static void ProgressVersion()
    {
        var s = NewProgress(out var f); s.Claim(Result(), Rewards()); var bytes = f.Inner.Slots[0];
        using var stream = new MemoryStream(bytes); using var r = new BinaryReader(stream);
        stream.Position = 8; r.ReadString(); int offset = (int)stream.Position;
        Array.Copy(BitConverter.GetBytes(999), 0, bytes, offset, 4); Rehash(bytes);
        Throws<IncompatibleSaveException>(() => s.Load()); Throws<IncompatibleSaveException>(() => s.Claim(Result("attempt-2"), Rewards())); Equal(2, f.Writes);
    }
    private static void ProgressIdentity()
    {
        NewProgress(out var f); var other = new LocalProgressStore(f, "wrong-profile");
        Throws<IncompatibleSaveException>(() => other.Load()); Throws<IncompatibleSaveException>(() => other.Create()); Equal(1, f.Writes);
    }
    private static void ProgressDivergent()
    {
        var a = NewProgress(out var fa); a.Claim(Result(), Rewards(first:100));
        var b = NewProgress(out var fb); b.Claim(Result(), Rewards()); b.Claim(Result("attempt-2"), Rewards());
        fb.Inner.Slots[1] = fa.Inner.Slots[1].ToArray();
        Throws<InvalidDataException>(() => b.Load()); Throws<InvalidDataException>(() => b.Claim(Result("attempt-3"), Rewards()));
    }
    private static void ProgressOverflow()
    {
        var s = NewProgress(out var f); s.Claim(Result(), Rewards(first:long.MaxValue));
        Throws<OverflowException>(() => s.Claim(Result("attempt-2"), Rewards()));
        Equal(long.MaxValue, s.Load().Progress.Balance("test-resource")); Equal(2, f.Writes); Equal(1, s.Load().Progress.Claims.Count);
    }
    private static void ProgressDefinitions()
    {
        var grants = new[] { new ResourceGrant("test-resource", 10) };
        var policy = new MissionRewardPolicy("m", "v", grants, Array.Empty<ResourceGrant>()); grants[0] = new ResourceGrant("changed", 100);
        Equal("test-resource", policy.FirstClear[0].ResourceId);
        Throws<ArgumentOutOfRangeException>(() => new ResourceGrant("r", 0)); Throws<ArgumentException>(() => new ResourceGrant(" ", 1));
        Throws<ArgumentException>(() => new MissionRewardPolicy("m", "v", new[] { grants[0], grants[0] }, grants));
        Throws<ArgumentException>(() => new MissionRewardPolicy("m", "v", new ResourceGrant[] { null }, grants));
        var s = NewProgress(out _); var c = s.Claim(Result(), new MissionRewardPolicy("test-mission", "empty", Array.Empty<ResourceGrant>(), Array.Empty<ResourceGrant>()));
        Equal(true, c.Receipt.IsFirstClear); Equal(0, c.Saved.Progress.Balances.Count);
    }
    private static void ProgressImmutable()
    {
        var s = NewProgress(out _); var first = s.Claim(Result(), Rewards()); s.Claim(Result("attempt-2"), Rewards());
        Equal(10L, first.Saved.Progress.Balance("test-resource")); Equal(1, first.Saved.Progress.Claims.Count);
        Throws<NotSupportedException>(() => ((IDictionary<string,long>)first.Saved.Progress.Balances)["test-resource"] = 999);
        Throws<NotSupportedException>(() => ((IList<MissionClaim>)first.Saved.Progress.Claims).Clear());
        Throws<NotSupportedException>(() => ((IList<ResourceGrant>)first.Receipt.Grants).Clear());
    }
    private static void ProgressInvalidClaim()
    {
        var s = NewProgress(out var f); Throws<ArgumentException>(() => s.Claim(Result(), Rewards("other")));
        Throws<ArgumentNullException>(() => s.Claim(null, Rewards())); Throws<ArgumentNullException>(() => s.Claim(Result(), null)); Equal(1, f.Writes);
    }
    private static void ProgressConcurrent()
    {
        var s = NewProgress(out var f); var result = Result(); var policy = Rewards();
        Parallel.For(0, 8, _ => s.Claim(result, policy)); Equal(2, f.Writes); Equal(10L, s.Load().Progress.Balance("test-resource"));
    }
    private static void ProgressDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "ninefold-progress-" + Guid.NewGuid().ToString("N"));
        try
        {
            var battle = FinishedBattle(); new LocalBattleStore(new DirectoryBattleSaveFiles(directory), SaveRevision).Save(battle);
            var s = new LocalProgressStore(new DirectoryProgressSaveFiles(directory), "test-profile"); s.Create(); s.Claim(battle.Mission.Result, Rewards());
            var restored = new LocalBattleStore(new DirectoryBattleSaveFiles(directory), SaveRevision).Load().Battle;
            var c = new LocalProgressStore(new DirectoryProgressSaveFiles(directory), "test-profile").Claim(restored.Mission.Result, Rewards());
            Equal(true, c.AlreadyClaimed); Equal(10L, c.Saved.Progress.Balance("test-resource"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
