using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Progression;
using Ninefold.Core.Views;

internal static partial class Program
{
    private static (string Name,Action Run)[] MissionMenuTests() => new (string,Action)[] {
        ("Mission menu requires explicit profile creation or open", MenuOpen),
        ("Mission cards explain prerequisites and faction roster locks", MenuLocks),
        ("Mission selection and reads never checkpoint", MenuPure),
        ("Preparation shows collectible stats and four abilities", MenuUnits),
        ("Preparation matches upgraded deployed stats and power", MenuStats),
        ("Squad drafts report size duplicate unknown and locked units", MenuSquads),
        ("Squads enforce faction and one Apex including mixed modes", MenuFactions),
        ("Old launch token cannot launch replacement squad", MenuStale),
        ("Prepared squad inputs and views are immutable", MenuDetached),
        ("Full menu battle results claim and selection flow", MenuComplete),
        ("Repeated reward collection returns receipt without new write", MenuDuplicate),
        ("Claim and exit reject wrong attempt and premature exit", MenuClaimGuard),
        ("Resume preserves movement and returns one interaction controller", MenuResume),
        ("Reloaded unclaimed result remains claimable", MenuUnclaimed),
        ("Claimed reload opens selection and preserves receipt history", MenuClaimed),
        ("Replay displays and grants replay rewards", MenuReplay),
        ("Defeat preserves owned units and balances with empty rewards", MenuDefeat),
        ("Interrupted reward collection recovers unclaimed result", MenuClaimFailure),
        ("Lost reward acknowledgement reloads exactly one grant", MenuAmbiguous),
        ("Start checkpoint failures recover without replacing attempt", MenuStartFailure),
        ("Pending battle blocks preparation and campaign claims", MenuPending),
        ("Starter campaign claim unlocks next faction only once", MenuCampaign),
        ("Selection change clears prior squad and rejects unknown mission", MenuChange)
    };
    private sealed class MenuFixture
    {
        internal readonly ContentCatalog Catalog=AbstractContentPackage.Create();
        internal readonly FakeSaveFiles Battles=new FakeSaveFiles();
        internal readonly ProgressFiles Profiles=new ProgressFiles();
        internal MissionPresentation Menu;
        internal MenuFixture(bool all=false,bool create=true)
        {
            Menu=Make();
            if(all) { new LocalProgressStore(Profiles,"menu-profile").Create(Catalog.Units.Values.Where(u=>u.Roster!=null).Select(u=>u.Id)); Menu.Open(); }
            else if(create) Menu.CreateProfile();
        }
        internal MissionPresentation Make()=>new MissionPresentation(Catalog,Battles,Profiles,"menu-profile");
        internal void Reopen() { Menu=Make(); Menu.Open(); }
        internal string Bytes()=>string.Join("|",Battles.Slots.Concat(Profiles.Inner.Slots).Select(b=>b==null?"null":Convert.ToBase64String(b)));
        internal void Prepare(string id="fixture-opening",params string[] squad)
        { Menu.SelectMission(id); Menu.SetSquad(squad.Length==0?new[] { "fixture-a-1" }:squad); }
        internal string Win(string id="fixture-opening")
        {
            Prepare(id); var b=Menu.Start(Menu.Read().PlanId); b.TapObjective("primary"); var r=b.TapObjective("primary");
            Equal(InputOutcome.Committed,r.Outcome); b.CompleteAnimation(r.AnimationId); return Menu.Read().Results.Result.AttemptId;
        }
        internal void Clear(string id="fixture-opening") { var attempt=Win(id); Menu.ClaimRewards(attempt); Menu.ReturnToSelection(attempt); }
    }
    private static MissionCard Card(MenuFixture x,string id)=>x.Menu.Read().Missions.Single(m=>m.Definition.Id==id);
    private static void MenuOpen()
    {
        var x=new MenuFixture(create:false); Equal(true,x.Menu.NeedsReload); Throws<InvalidOperationException>(()=>x.Menu.Read());
        Throws<InvalidOperationException>(()=>x.Menu.Open()); x.Menu.CreateProfile(); Equal(MissionFlowPhase.Selection,x.Menu.Read().Phase); Equal(3,x.Menu.Read().Progress.OwnedUnits.Count);
    }
    private static void MenuLocks()
    {
        var x=new MenuFixture(); Equal(true,Card(x,"fixture-opening").Available); Equal(false,Card(x,"fixture-finale").Available);
        Equal("fixture-opening",Card(x,"fixture-finale").MissingPrerequisites.Single()); Equal(true,Card(x,"fixture-next").FactionRosterLocked);
        Equal(0,Card(x,"fixture-next").OwnedFactionStandards); Equal(ObjectiveKind.Stabilize,Card(x,"fixture-opening").Definition.Objectives.Single().Kind);
        x.Prepare("fixture-finale"); Equal(SquadFailure.MissionLocked,x.Menu.Read().SquadFailure.Value); Equal(false,x.Menu.Read().CanStart);
        x.Clear(); Equal(true,Card(x,"fixture-finale").Available);
    }
    private static void MenuPure()
    {
        var x=new MenuFixture(); string before=x.Bytes(); x.Prepare(); for(int n=0;n<4;n++) { x.Menu.Read(); x.Menu.SetSquad(new[] { "fixture-a-2" }); }
        Equal(before,x.Bytes()); Equal(false,x.Menu.Read().CanResume);
    }
    private static void MenuUnits()
    {
        var x=new MenuFixture(); x.Prepare(); var v=x.Menu.Read(); Equal(10,v.Units.Count);
        var a=v.Units.Single(u=>u.Definition.Id=="fixture-a-1"); Equal(100,a.Health); Equal(20,a.Armor); Equal(29,a.Initiative); Equal(5m,a.Movement); Equal(4,a.Abilities.Count);
        Equal(true,a.EligibleForMission); Equal(true,a.Abilities.Single(b=>b.Slot==AbilitySlot.Passive).IsPassivePlaceholder);
        Equal(false,v.Units.Single(u=>u.Definition.Id=="fixture-b-1").Owned);
    }
    private static void MenuStats()
    {
        var x=new MenuFixture(); x.Clear(); var flow=x.Catalog.CreateFlow(x.Battles,x.Profiles,"menu-profile"); flow.Open();
        flow.AdvanceUnit("rank","fixture-a-1"); flow.CustomizeUnit("custom","fixture-a-1","fixture-health"); x.Reopen(); x.Prepare("fixture-finale");
        var u=x.Menu.Read().Units.Single(a=>a.Definition.Id=="fixture-a-1"); Equal(110,u.Health); Equal(20,u.Armor); Equal(1,u.Rank); Equal("fixture-health",u.CustomizationId);
        Equal(42,u.Abilities.Single(a=>a.Slot==AbilitySlot.NormalAttack).Effect.Amount);
        var b=x.Menu.Start(x.Menu.Read().PlanId).Read().Battle.Units.Single(a=>a.Id=="fixture-a-1"); Equal(u.Health,b.Health.MaximumHealth); Equal(u.Armor,b.Health.Armor);
        Equal(42,b.Abilities.Single(a=>a.Slot==AbilitySlot.NormalAttack).Amount);
    }
    private static void MenuSquads()
    {
        var x=new MenuFixture(); x.Menu.SelectMission("fixture-opening"); Equal(SquadFailure.Size,x.Menu.Read().SquadFailure.Value);
        foreach(var row in new[] { (new[] { "bad" },SquadFailure.UnknownUnit),(new[] { "fixture-a-1","fixture-a-1" },SquadFailure.DuplicateUnit),(new[] { "fixture-a-4" },SquadFailure.LockedUnit) })
        { x.Menu.SetSquad(row.Item1); Equal(row.Item2,x.Menu.Read().SquadFailure.Value); Throws<InvalidOperationException>(()=>x.Menu.Start(x.Menu.Read().PlanId)); }
        Throws<ArgumentException>(()=>x.Menu.SetSquad(Enumerable.Repeat("fixture-a-1",9))); Equal<byte[]>(null,x.Battles.Slots[0]);
    }
    private static void MenuFactions()
    {
        var x=new MenuFixture(all:true); x.Prepare("fixture-opening","fixture-b-1"); Equal(SquadFailure.WrongFaction,x.Menu.Read().SquadFailure.Value);
        x.Prepare("fixture-mixed","fixture-a-5","fixture-b-5"); Equal(SquadFailure.MultipleApex,x.Menu.Read().SquadFailure.Value);
        x.Menu.SetSquad(new[] { "fixture-a-5","fixture-b-1" }); Equal(true,x.Menu.Read().CanStart);
    }
    private static void MenuStale()
    {
        var x=new MenuFixture(); x.Prepare(); var old=x.Menu.Read().PlanId; x.Menu.SetSquad(new[] { "fixture-a-2" });
        Throws<InvalidOperationException>(()=>x.Menu.Start(old)); Equal(MissionFlowPhase.Selection,x.Menu.Read().Phase);
        x.Menu.Start(x.Menu.Read().PlanId); Throws<InvalidOperationException>(()=>x.Menu.Start(old));
    }
    private static void MenuDetached()
    {
        var x=new MenuFixture(); x.Prepare(); var a=new[] { "fixture-a-1" }; x.Menu.SetSquad(a); a[0]="fixture-a-5"; var v=x.Menu.Read();
        Throws<NotSupportedException>(()=>((IList<string>)v.Squad)[0]="fixture-a-5"); x.Menu.SetSquad(new[] { "fixture-a-2" }); Equal("fixture-a-1",v.Squad.Single()); Equal(true,v.CanStart);
    }
    private static void MenuComplete()
    {
        var x=new MenuFixture(); var attempt=x.Win(); var v=x.Menu.Read(); Equal(false,v.CanResume); Equal(MissionOutcome.Victory,v.Results.Result.Outcome);
        Equal(true,v.Results.CanClaim); Equal(0L,v.Progress.Balance("fixture-resource")); Equal(10L,v.Results.Grants.Single(g=>g.ResourceId=="fixture-resource").Amount);
        var r=x.Menu.ClaimRewards(attempt); Equal(0L,r.Before.Balance("fixture-resource")); Equal(10L,r.After.Balance("fixture-resource")); Equal(false,r.AlreadyClaimed);
        Equal(true,x.Menu.Read().Results.Claimed); Equal(1L,x.Menu.Read().Results.SavedVictoryCount); x.Menu.ReturnToSelection(attempt);
        Equal(MissionFlowPhase.Selection,x.Menu.Read().Phase); Equal<MissionResultsView>(null,x.Menu.Read().Results);
    }
    private static void MenuDuplicate()
    {
        var x=new MenuFixture(); string attempt=x.Win(); x.Menu.ClaimRewards(attempt); int writes=x.Profiles.Writes;
        var r=x.Menu.ClaimRewards(attempt); Equal(true,r.AlreadyClaimed); Equal(writes,x.Profiles.Writes); Equal(10L,r.Before.Balance("fixture-resource")); Equal(10L,r.After.Balance("fixture-resource"));
    }
    private static void MenuClaimGuard()
    {
        var x=new MenuFixture(); string attempt=x.Win(); Throws<InvalidOperationException>(()=>x.Menu.ClaimRewards("wrong")); Throws<InvalidOperationException>(()=>x.Menu.ReturnToSelection(attempt));
        x.Menu.ClaimRewards(attempt); Throws<InvalidOperationException>(()=>x.Menu.ReturnToSelection("wrong")); Equal(MissionFlowPhase.Results,x.Menu.Read().Phase);
    }
    private static void MenuResume()
    {
        var x=new MenuFixture(); x.Prepare(); var b=x.Menu.Start(x.Menu.Read().PlanId); b.TapDestination(P(1,0)); var moved=b.TapDestination(P(1,0));
        Equal(true,ReferenceEquals(b,x.Menu.ResumeBattle())); Equal(true,x.Menu.ResumeBattle().Read().IsAnimating);
        string attempt=x.Menu.Read().Resume.AttemptId; x.Reopen(); Equal(true,x.Menu.Read().CanResume); Equal(attempt,x.Menu.Read().Resume.AttemptId);
        Equal(4m,x.Menu.ResumeBattle().Read().Battle.Activation.MovementRemaining); Equal(false,x.Menu.ResumeBattle().Read().IsAnimating);
    }
    private static void MenuUnclaimed()
    { var x=new MenuFixture(); var attempt=x.Win(); x.Reopen(); Equal(attempt,x.Menu.Read().Results.Result.AttemptId); Equal(true,x.Menu.Read().Results.CanClaim); x.Menu.ClaimRewards(attempt); Equal(10L,x.Menu.Read().Progress.Balance("fixture-resource")); }
    private static void MenuClaimed()
    { var x=new MenuFixture(); var attempt=x.Win(); x.Menu.ClaimRewards(attempt); x.Reopen(); Equal(MissionFlowPhase.Selection,x.Menu.Read().Phase); Equal(attempt,x.Menu.Read().Progress.Claims.Single().AttemptId); Throws<InvalidOperationException>(()=>x.Menu.ClaimRewards(attempt)); }
    private static void MenuReplay()
    {
        var x=new MenuFixture(); x.Clear(); Equal(true,Card(x,"fixture-opening").IsReplay); Equal(2L,Card(x,"fixture-opening").VictoryRewards.Single().Amount);
        var attempt=x.Win(); Equal(false,x.Menu.Read().Results.IsFirstClear); Equal(2L,x.Menu.Read().Results.Grants.Single().Amount); x.Menu.ClaimRewards(attempt); Equal(12L,x.Menu.Read().Progress.Balance("fixture-resource"));
    }
    private static void MenuDefeat()
    {
        var x=new MenuFixture(); x.Clear(); var f=x.Catalog.CreateFlow(x.Battles,x.Profiles,"menu-profile"); f.Open(); f.Start("fixture-opening",new[] { "fixture-a-1" });
        f.Execute(b=> { b.RemoveUnit("fixture-a-1"); b.Mission.Evaluate(); }); x.Reopen(); var v=x.Menu.Read(); Equal(MissionOutcome.Defeat,v.Results.Result.Outcome); Equal(0,v.Results.Grants.Count);
        x.Menu.ClaimRewards(v.Results.Result.AttemptId); Equal(10L,x.Menu.Read().Progress.Balance("fixture-resource")); Equal(3,x.Menu.Read().Progress.OwnedUnits.Count);
    }
    private static void MenuClaimFailure()
    {
        foreach(int fault in new[] { 1,2 }) { var x=new MenuFixture(); var attempt=x.Win(); x.Profiles.Inner.Fault=fault;
            Throws<IOException>(()=>x.Menu.ClaimRewards(attempt)); Equal(true,x.Menu.NeedsReload); Throws<InvalidOperationException>(()=>x.Menu.Read()); x.Profiles.Inner.Fault=0; x.Reopen();
            Equal(true,x.Menu.Read().Results.CanClaim); x.Menu.ClaimRewards(attempt); Equal(10L,x.Menu.Read().Progress.Balance("fixture-resource")); }
    }
    private static void MenuAmbiguous()
    {
        var x=new MenuFixture(); var attempt=x.Win(); x.Profiles.Inner.Fault=3; Throws<IOException>(()=>x.Menu.ClaimRewards(attempt)); x.Profiles.Inner.Fault=0; x.Reopen();
        Equal(MissionFlowPhase.Selection,x.Menu.Read().Phase); Equal(1,x.Menu.Read().Progress.Claims.Count); Equal(10L,x.Menu.Read().Progress.Balance("fixture-resource")); Throws<InvalidOperationException>(()=>x.Menu.ClaimRewards(attempt));
    }
    private static void MenuStartFailure()
    {
        foreach(int fault in new[] { 1,2,3 }) { var x=new MenuFixture(); x.Prepare(); var plan=x.Menu.Read().PlanId; x.Battles.Fault=fault;
            Throws<IOException>(()=>x.Menu.Start(plan)); Equal(true,x.Menu.NeedsReload); x.Battles.Fault=0;
            if(fault==2) { /* A sole torn first checkpoint has no valid fallback; fail closed. */ Throws<InvalidDataException>(()=>x.Menu.Open()); continue; }
            x.Reopen(); Equal(fault==3,x.Menu.Read().CanResume); Throws<InvalidOperationException>(()=>x.Menu.Start(plan)); }
    }
    private static void MenuPending()
    {
        var x=new MenuFixture(); x.Prepare(); x.Menu.Start(x.Menu.Read().PlanId); Throws<InvalidOperationException>(()=>x.Menu.SelectMission("fixture-mixed"));
        Throws<InvalidOperationException>(()=>x.Menu.SetSquad(new[] { "fixture-a-2" })); Throws<InvalidOperationException>(()=>x.Menu.ClaimCampaign("fixture-starter"));
    }
    private static void MenuCampaign()
    {
        var x=new MenuFixture(); x.Clear(); x.Clear("fixture-finale"); Equal(true,x.Menu.Read().Campaigns.Single(c=>c.CampaignId=="fixture-starter").IsComplete);
        var c=x.Menu.ClaimCampaign("fixture-starter"); Equal(true,c.Saved.Progress.Owns("fixture-a-5")); Equal(true,Card(x,"fixture-next").Available);
        int writes=x.Profiles.Writes; Equal(true,x.Menu.ClaimCampaign("fixture-starter").AlreadyClaimed); Equal(writes,x.Profiles.Writes); Equal(false,x.Menu.Read().Progress.Owns("fixture-b-5"));
    }
    private static void MenuChange()
    {
        var x=new MenuFixture(); x.Prepare(); string old=x.Menu.Read().PlanId; x.Menu.SelectMission("fixture-mixed"); Equal(0,x.Menu.Read().Squad.Count);
        Throws<ArgumentException>(()=>x.Menu.SelectMission("missing")); Equal("fixture-mixed",x.Menu.Read().SelectedMissionId); Throws<InvalidOperationException>(()=>x.Menu.Start(old));
    }
}
