using System;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Persistence;
using Ninefold.Core.Progression;

internal static partial class Program
{
    private static (string Name, Action Run)[] FlowTests() => new (string, Action)[]
    {
        ("Flow explicitly creates a profile and opens mission selection", FlowCreate),
        ("Flow connects selection battle result claim and selection", FlowComplete),
        ("Flow resumes active battle without resetting command costs", FlowResume),
        ("Flow reopens unclaimed terminal results", FlowResults),
        ("Flow reopens claimed result at selection without another grant", FlowClaimed),
        ("Flow replay gets a fresh attempt and replay rewards", FlowReplay),
        ("Flow defeat preserves progress and returns to selection", FlowDefeat),
        ("Flow rejects replacing a pending or unclaimed battle", FlowPending),
        ("Flow enforces unknown locked duplicate and sized squads", FlowSquad),
        ("Flow enforces one Apex even in mixed-faction modes", FlowApex),
        ("Flow campaign gate counts three owned standard units", FlowCampaign),
        ("Flow accepts mixed squads only for unrestricted missions", FlowFactions),
        ("Flow evaluates authored mission availability against progress", FlowAvailability),
        ("Flow rejects a mismatched battle factory before saving", FlowBadFactory),
        ("Flow snapshots and retained command references cannot mutate authority", FlowDetached),
        ("Flow command failure publishes no partial changes", FlowCommandFailure),
        ("Flow failed checkpoint requires reload before any further action", FlowWriteFailure),
        ("Flow torn command checkpoint resumes prior complete command", FlowTorn),
        ("Flow lost checkpoint acknowledgement reloads committed command", FlowAmbiguousBattle),
        ("Flow interrupted reward claim returns to unclaimed results", FlowClaimFailure),
        ("Flow lost reward acknowledgement reopens selection once", FlowAmbiguousClaim),
        ("Flow ignores recovered active checkpoint of claimed attempt", FlowClaimedFallback),
        ("Flow reports recovery and retries a rolled-back reward transaction", FlowProfileRecovery),
        ("Flow binds battle saves to the profile identity", FlowIdentity),
        ("Flow blocks removed active mission without overwriting saves", FlowMissingContent),
        ("Flow rejects nested state-changing callbacks", FlowReentrant)
    };
    private sealed class FlowFixture
    {
        internal readonly FakeSaveFiles Battles = new FakeSaveFiles();
        internal readonly ProgressFiles Profiles = new ProgressFiles();
        internal MissionFlow Make(MissionEntry[] entries = null, RosterUnit[] roster = null, string profile = "flow-profile")
            => new MissionFlow(Battles, Profiles, profile, "abstract-flow-v1", entries ?? new[] { FlowEntry() }, roster ?? FlowRoster());
        internal MissionFlow Create()
        {
            new LocalProgressStore(Profiles, "flow-profile").Create(FlowRoster().Where(u => u.Id != "locked").Select(u => u.Id));
            var f = Make(); f.Open(); return f;
        }
        internal MissionFlow Reopen() { var f = Make(); f.Open(); return f; }
    }
    private static RosterUnit[] FlowRoster() => new[]
    {
        new RosterUnit("a", "faction", false, new UnitUnlockDefinition("a", "fragment-a", 5, "test-v1")), new RosterUnit("c", "faction", false, new UnitUnlockDefinition("c", "fragment-c", 5, "test-v1")),
        new RosterUnit("d", "faction", false, new UnitUnlockDefinition("d", "fragment-d", 5, "test-v1")), new RosterUnit("locked", "faction", false, new UnitUnlockDefinition("locked", "fragment-locked", 5, "test-v1")),
        new RosterUnit("x", "faction", true, new UnitUnlockDefinition("x", "fragment-x", 5, "test-v1")), new RosterUnit("y", "other", true, new UnitUnlockDefinition("y", "fragment-y", 5, "test-v1")),
        new RosterUnit("z", "other", false, new UnitUnlockDefinition("z", "fragment-z", 5, "test-v1"))
    };
    private static MissionEntry FlowEntry(string id = "test-mission", string faction = null, bool campaign = false,
        Func<PlayerProgress, bool> available = null)
        => new MissionEntry(id, 1, 3, faction, campaign, Rewards(id), (attempt, squad) =>
        {
            var b = FieldBattle(start:false);
            b.ConfigureMission(new MissionDefinition(id, attempt, squad, ObjectiveDefinition.Stabilize("primary", 1, InteractAt()), OutcomePriority.FailureFirst));
            b.StartNextRound(); b.BeginNextActivation(); return b;
        }, available);
    private static void StartFlow(MissionFlow f) => f.Start("test-mission", new[] { "a", "c" });
    private static void WinFlow(MissionFlow f) => f.Execute(b => Interact(b, "primary"));
    private static void FlowCreate()
    {
        var x = new FlowFixture(); var f = x.Make(); Equal(true, f.NeedsReload);
        Throws<InvalidOperationException>(() => f.Open()); f.CreateProfile(new[] { "a", "c", "d" }); Equal(MissionFlowPhase.Selection, f.Phase);
        Equal<BattleTurnController>(null, f.ReadBattle()); Equal(0, f.Progress.Claims.Count);
        Throws<InvalidOperationException>(() => f.CreateProfile(new[] { "a", "c", "d" })); f.Open(); Equal(false, f.NeedsReload);
    }
    private static void FlowComplete()
    {
        var f = new FlowFixture().Create(); StartFlow(f); Equal(MissionFlowPhase.Battle, f.Phase);
        WinFlow(f); Equal(MissionFlowPhase.Results, f.Phase); Equal(0, f.Progress.Claims.Count);
        Equal(true, f.ClaimRewards().IsFirstClear); Equal(10L, f.Progress.Balance("test-resource"));
        f.ClaimRewards(); Equal(1, f.Progress.Claims.Count); f.ReturnToSelection(); Equal(MissionFlowPhase.Selection, f.Phase);
    }
    private static void FlowResume()
    {
        var x = new FlowFixture(); var f = x.Create(); StartFlow(f); string attempt = f.ReadBattle().Mission.Definition.AttemptId;
        f.Execute(b => Move(b, P(.5m, 0))); var reopened = x.Reopen();
        Equal(attempt, reopened.ReadBattle().Mission.Definition.AttemptId); Equal(19.5m, reopened.ReadBattle().CurrentActivation.MovementRemaining);
        Equal(P(.5m,0), reopened.ReadBattle().Battlefield.GetPosition("a"));
    }
    private static void FlowResults()
    {
        var x = new FlowFixture(); var f = x.Create(); StartFlow(f); WinFlow(f);
        f = x.Reopen(); Equal(MissionFlowPhase.Results, f.Phase); Equal(0, f.Progress.Claims.Count); f.ClaimRewards(); Equal(10L, f.Progress.Balance("test-resource"));
    }
    private static void FlowClaimed()
    {
        var x = new FlowFixture(); var f = x.Create(); StartFlow(f); WinFlow(f); f.ClaimRewards();
        f = x.Reopen(); Equal(MissionFlowPhase.Selection, f.Phase); Equal(10L, f.Progress.Balance("test-resource"));
        Throws<InvalidOperationException>(() => f.ClaimRewards()); Equal(2, x.Profiles.Writes);
    }
    private static void FlowReplay()
    {
        var x = new FlowFixture(); var f = x.Create(); StartFlow(f); WinFlow(f); string first = f.ClaimRewards().AttemptId;
        f.ReturnToSelection(); f = x.Reopen(); StartFlow(f); WinFlow(f); var receipt = f.ClaimRewards();
        Equal(false, first == receipt.AttemptId); Equal(false, receipt.IsFirstClear); Equal(12L, f.Progress.Balance("test-resource"));
    }
    private static void FlowDefeat()
    {
        var f = new FlowFixture().Create(); StartFlow(f); WinFlow(f); f.ClaimRewards(); f.ReturnToSelection(); StartFlow(f);
        f.Execute(b => { b.RemoveUnit("a"); b.RemoveUnit("c"); b.Mission.Evaluate(); });
        Equal(MissionOutcome.Defeat, f.ClaimRewards().Outcome); Equal(10L, f.Progress.Balance("test-resource")); f.ReturnToSelection();
    }
    private static void FlowPending()
    {
        var f = new FlowFixture().Create(); StartFlow(f); Throws<InvalidOperationException>(() => StartFlow(f));
        Throws<InvalidOperationException>(() => f.ReturnToSelection()); Throws<InvalidOperationException>(() => f.ClaimRewards());
        WinFlow(f); Throws<InvalidOperationException>(() => StartFlow(f)); Throws<InvalidOperationException>(() => f.ReturnToSelection());
        Throws<InvalidOperationException>(() => f.Execute(b => { }));
    }
    private static void FlowSquad()
    {
        var x = new FlowFixture(); var f = x.Create();
        Equal(SquadFailure.Size, f.ValidateSquad("test-mission", Array.Empty<string>()));
        Equal(SquadFailure.Size, f.ValidateSquad("test-mission", new[] { "a", "c", "d", "x" }));
        Equal(SquadFailure.UnknownUnit, f.ValidateSquad("test-mission", new[] { "unknown" }));
        Equal(SquadFailure.DuplicateUnit, f.ValidateSquad("test-mission", new[] { "a", "a" }));
        Equal(SquadFailure.LockedUnit, f.ValidateSquad("test-mission", new[] { "locked" }));
        Throws<InvalidOperationException>(() => f.Start("test-mission", new[] { "locked" })); Equal<byte[]>(null, x.Battles.Slots[0]);
    }
    private static void FlowApex()
    {
        var f = new FlowFixture().Create(); Equal(SquadFailure.MultipleApex, f.ValidateSquad("test-mission", new[] { "x", "y" }));
        Equal(SquadFailure.None, f.ValidateSquad("test-mission", new[] { "a", "x" }));
    }
    private static void FlowCampaign()
    {
        var x = new FlowFixture(); x.Create(); var entry = FlowEntry(faction:"faction", campaign:true);
        var f = x.Make(new[] { entry }, FlowRoster().Where(u => u.Id != "d").ToArray()); f.Open();
        Equal(SquadFailure.CampaignRoster, f.ValidateSquad("test-mission", new[] { "a", "x" }));
        f = x.Make(new[] { entry }); f.Open(); Equal(SquadFailure.None, f.ValidateSquad("test-mission", new[] { "a" }));
    }
    private static void FlowFactions()
    {
        var x = new FlowFixture(); x.Create(); var f = x.Make(new[] { FlowEntry(faction:"faction", campaign:true) }); f.Open();
        Equal(SquadFailure.WrongFaction, f.ValidateSquad("test-mission", new[] { "a", "z" }));
        f = x.Reopen(); Equal(SquadFailure.None, f.ValidateSquad("test-mission", new[] { "a", "z" }));
    }
    private static void FlowAvailability()
    {
        var x = new FlowFixture(); x.Create(); var f = x.Make(new[] { FlowEntry(), FlowEntry("next", available:p => p.Missions.ContainsKey("test-mission")) }); f.Open();
        Equal(SquadFailure.MissionLocked, f.ValidateSquad("next", new[] { "a" }));
        StartFlow(f); WinFlow(f); f.ClaimRewards(); f.ReturnToSelection(); Equal(SquadFailure.None, f.ValidateSquad("next", new[] { "a" }));
        f.Start("next", new[] { "a" }); Equal("next", f.ReadBattle().Mission.Definition.MissionId);
    }
    private static void FlowBadFactory()
    {
        var x = new FlowFixture(); x.Create();
        var bad = new MissionEntry("test-mission", 1, 3, null, false, Rewards(), (_, __) => FieldBattle());
        var f = x.Make(new[] { bad }); f.Open(); Throws<InvalidOperationException>(() => StartFlow(f));
        Equal<byte[]>(null, x.Battles.Slots[0]); Equal(MissionFlowPhase.Selection, f.Phase);
    }
    private static void FlowDetached()
    {
        var f = new FlowFixture().Create(); StartFlow(f); Move(f.ReadBattle(), P(2,0)); Equal(P(0,0), f.ReadBattle().Battlefield.GetPosition("a"));
        BattleTurnController retained = null; f.Execute(b => { retained = b; Move(b, P(1,0)); }); Move(retained, P(2,0));
        Equal(P(1,0), f.ReadBattle().Battlefield.GetPosition("a"));
    }
    private static void FlowCommandFailure()
    {
        var f = new FlowFixture().Create(); StartFlow(f);
        Throws<ArgumentException>(() => f.Execute(b => { Move(b, P(1,0)); throw new ArgumentException("Rejected composite command"); }));
        Equal(P(0,0), f.ReadBattle().Battlefield.GetPosition("a")); Equal(false, f.NeedsReload);
        Throws<InvalidOperationException>(() => f.Execute(b => b.EndBattle())); Equal(false, f.ReadBattle().IsBattleEnded);
    }
    private static void FlowWriteFailure()
    {
        var x = new FlowFixture(); var f = x.Create(); StartFlow(f); x.Battles.Fault = 1;
        Throws<System.IO.IOException>(() => WinFlow(f)); Equal(true, f.NeedsReload);
        Throws<InvalidOperationException>(() => f.ReadBattle()); Throws<InvalidOperationException>(() => WinFlow(f));
        Throws<InvalidOperationException>(() => f.ClaimRewards()); x.Battles.Fault = 0; f.Open(); Equal(MissionFlowPhase.Battle, f.Phase); WinFlow(f);
    }
    private static void FlowTorn()
    {
        var x = new FlowFixture(); var f = x.Create(); StartFlow(f); x.Battles.Fault = 2;
        Throws<System.IO.IOException>(() => f.Execute(b => Move(b, P(1,0)))); x.Battles.Fault = 0; f = x.Reopen();
        Equal(true, f.Recovered); Equal(P(0,0), f.ReadBattle().Battlefield.GetPosition("a"));
    }
    private static void FlowAmbiguousBattle()
    {
        var x = new FlowFixture(); var f = x.Create(); StartFlow(f); x.Battles.Fault = 3;
        Throws<System.IO.IOException>(() => WinFlow(f)); x.Battles.Fault = 0; f.Open(); Equal(MissionFlowPhase.Results, f.Phase);
        f.ClaimRewards(); Equal(10L, f.Progress.Balance("test-resource"));
    }
    private static void FlowClaimFailure()
    {
        foreach (int fault in new[] { 1, 2 })
        {
            var x = new FlowFixture(); var f = x.Create(); StartFlow(f); WinFlow(f); x.Profiles.Inner.Fault = fault;
            Throws<System.IO.IOException>(() => f.ClaimRewards()); Equal(true, f.NeedsReload); x.Profiles.Inner.Fault = 0;
            f.Open(); Equal(MissionFlowPhase.Results, f.Phase); Equal(0L, f.Progress.Balance("test-resource")); f.ClaimRewards();
            Equal(10L, f.Progress.Balance("test-resource"));
        }
    }
    private static void FlowAmbiguousClaim()
    {
        var x = new FlowFixture(); var f = x.Create(); StartFlow(f); WinFlow(f); x.Profiles.Inner.Fault = 3;
        Throws<System.IO.IOException>(() => f.ClaimRewards()); x.Profiles.Inner.Fault = 0; f.Open();
        Equal(MissionFlowPhase.Selection, f.Phase); Equal(10L, f.Progress.Balance("test-resource")); Equal(2, x.Profiles.Writes);
    }
    private static void FlowClaimedFallback()
    {
        var x = new FlowFixture(); var f = x.Create(); StartFlow(f); WinFlow(f); f.ClaimRewards(); x.Battles.Slots[1][20] ^= 1;
        f.Open(); Equal(true, f.Recovered); Equal(MissionFlowPhase.Selection, f.Phase); Equal(10L, f.Progress.Balance("test-resource"));
        StartFlow(f); Equal(MissionFlowPhase.Battle, f.Phase);
    }
    private static void FlowProfileRecovery()
    {
        var x = new FlowFixture(); var f = x.Create(); StartFlow(f); WinFlow(f); f.ClaimRewards(); x.Profiles.Inner.Slots[1][20] ^= 1;
        f.Open(); Equal(true, f.Recovered); Equal(MissionFlowPhase.Results, f.Phase); f.ClaimRewards(); Equal(10L, f.Progress.Balance("test-resource"));
    }
    private static void FlowIdentity()
    {
        var x = new FlowFixture(); var f = x.Create(); StartFlow(f);
        var otherProfiles = new ProgressFiles(); new LocalProgressStore(otherProfiles, "other-profile").Create();
        var other = new MissionFlow(x.Battles, otherProfiles, "other-profile", "abstract-flow-v1", new[] { FlowEntry() }, FlowRoster());
        Throws<IncompatibleSaveException>(() => other.Open()); Equal(true, other.NeedsReload);
    }
    private static void FlowMissingContent()
    {
        var x = new FlowFixture(); var f = x.Create(); StartFlow(f); var bytes = x.Battles.Slots[0].ToArray();
        f = x.Make(Array.Empty<MissionEntry>()); Throws<ArgumentException>(() => f.Open()); Equal(true, f.NeedsReload);
        Equal(true, bytes.SequenceEqual(x.Battles.Slots[0]));
    }
    private static void FlowReentrant()
    {
        var f = new FlowFixture().Create(); StartFlow(f);
        Throws<InvalidOperationException>(() => f.Execute(b => f.Open())); Equal(MissionFlowPhase.Battle, f.Phase);
    }
}
