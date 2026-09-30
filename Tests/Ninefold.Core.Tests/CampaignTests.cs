using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Ninefold.Core.Campaigns;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Progression;

internal static partial class Program
{
    private static (string Name, Action Run)[] CampaignTests() => new (string, Action)[]
    {
        ("Campaign order gates future missions and preserves replays", CampaignOrder),
        ("Campaign branching requires every authored prerequisite", CampaignBranches),
        ("Campaign supports a hundred short missions with saved progress", CampaignHundred),
        ("Campaign definitions reject cycles duplicates and unknown prerequisites", CampaignInvalidGraph),
        ("Campaign catalog rejects wrong Apex and next-faction bundles", CampaignInvalidRewards),
        ("Campaign starter profile must match the opening faction", CampaignStarterFaction),
        ("Campaign cannot claim before all required victories", CampaignUnfinished),
        ("Campaign defeat is not completion", CampaignLoss),
        ("Campaign optional missions do not block completion", CampaignOptional),
        ("Starter completion unlocks its Apex and another playable campaign", CampaignFullFlow),
        ("Ordinary completion cannot repeat starter unit grants", CampaignOrdinary),
        ("Campaign retry returns original receipt even without content", CampaignDuplicate),
        ("Campaign starter skips already owned units without compensation", CampaignOwned),
        ("Starter selection is stable across candidate order and reload", CampaignStableSelection),
        ("Starter prefers a faction whose campaign is still locked", CampaignLockedPreference),
        ("Interrupted campaign claim commits no partial rewards", CampaignInterrupted),
        ("Lost campaign acknowledgement does not grant twice", CampaignAmbiguous),
        ("Campaign recovery rolls all grant components back together", CampaignRecovery),
        ("Later mission claims and unlocks retain campaign receipts", CampaignLaterTransactions),
        ("Starter bonus cannot be claimed through a second campaign ID", CampaignSingleStarter),
        ("Campaign resource overflow rejects the entire completion grant", CampaignOverflow),
        ("Schema two ownership and spending survive campaign migration", CampaignSchemaTwo),
        ("Flow campaign claims require selection after mission rewards", CampaignFlowGate),
        ("Flow ambiguous campaign claim restores ownership and receipt", CampaignFlowRetry),
        ("Campaign snapshots and definition inputs are immutable", CampaignImmutable),
        ("Starter claim rejects a target campaign with no available entry", CampaignBlockedEntry)
    };
    private sealed class CampaignFixture
    {
        internal readonly ProgressFiles Files = new ProgressFiles();
        internal readonly FakeSaveFiles Battles = new FakeSaveFiles();
        internal readonly RosterUnit[] Units;
        internal readonly CampaignDefinition[] Definitions;
        internal readonly MissionEntry[] Entries;
        internal readonly CampaignCatalog Catalog;
        internal readonly LocalProgressStore Store;
        internal CampaignFixture(int count = 2, bool optional = false, bool reverse = false, IEnumerable<string> initial = null,
            string starterId = "starter", ProgressFiles existing = null, long reward = 7, bool blocked = false)
        {
            if (existing != null) Files = existing;
            Units = FlowRoster().Concat(new[] { CampaignUnit("o2", "other"), CampaignUnit("o3", "other"),
                CampaignUnit("t1", "third"), CampaignUnit("t2", "third"), CampaignUnit("t3", "third") }).ToArray();
            var nodes = Enumerable.Range(1,count).Select(i => new CampaignMission("m"+i, i == 1 ? null : new[] { "m"+(i-1) })).ToList();
            if (optional) nodes.Add(new CampaignMission("optional", new[] { "m1" }, false));
            var candidates = new[] { new NextFactionBundle("next", new[] { "z", "o2", "o3" }), new NextFactionBundle("third", new[] { "t1", "t2", "t3" }) };
            if (reverse) Array.Reverse(candidates);
            Definitions = new[] {
                new CampaignDefinition(starterId, "faction", "test-v1", nodes, new[] { new ResourceGrant("test-resource", reward) }, new StarterCampaignBonus("x", candidates)),
                new CampaignDefinition("next", "other", "test-v1", new[] { new CampaignMission("next-m") }, new[] { new ResourceGrant("test-resource", 3) }),
                new CampaignDefinition("third", "third", "test-v1", new[] { new CampaignMission("third-m") }, Array.Empty<ResourceGrant>()) };
            Entries = Definitions.SelectMany(c => c.Missions.Select(m => CampaignEntry(m.MissionId, c.FactionId, blocked && c.Id != starterId ? (Func<PlayerProgress,bool>)(_ => false) : null))).ToArray();
            Catalog = new CampaignCatalog(Definitions, Entries, Units);
            Store = new LocalProgressStore(Files, "campaign-profile");
            if (existing == null) Store.Create(initial ?? new[] { "a", "c", "d" });
        }
        internal void Win(string mission) => Store.Claim(Result("attempt-"+mission, mission), Rewards(mission));
        internal void Finish() { foreach (var m in Definitions[0].Missions.Where(m => m.Required)) Win(m.MissionId); }
        internal MissionFlow Flow() => new MissionFlow(Battles, Files, "campaign-profile", "test-campaigns", Entries, Units, Definitions);
    }
    private static RosterUnit CampaignUnit(string id, string faction) => new RosterUnit(id, faction, false, new UnitUnlockDefinition(id, "fragment-"+id, 5, "v1"));
    private static MissionEntry CampaignEntry(string id, string faction, Func<PlayerProgress,bool> available = null)
        => new MissionEntry(id, 1, 3, faction, true, Rewards(id), (attempt,squad) =>
        {
            var b = B(squad.Select((u,i) => U(u, 30-i)).ToArray());
            var field = b.ConfigureBattlefield(new BattlefieldMap(Box(-20,-20,20,20,20), Array.Empty<FieldObstacle>()));
            for (int i=0;i<squad.Count;i++)
            {
                string unit = squad[i]; b.Abilities.RegisterKit(unit, new UnitAbilityDefinition(2, SignatureReadiness.ReadyAtDeployment));
                b.Health.RegisterHealth(unit, new UnitHealthDefinition("ally",100,0)); field.Register(unit, P(i*2,0), Body(), Profiles());
            }
            b.ConfigureMission(new MissionDefinition(id, attempt, squad, ObjectiveDefinition.Stabilize("primary", 1, InteractAt(actor:squad[0])), OutcomePriority.FailureFirst));
            b.StartNextRound(); b.BeginNextActivation(); return b;
        }, available);
    private static void CampaignOrder()
    {
        var x = new CampaignFixture(); var f = x.Flow(); f.Open();
        Equal(SquadFailure.MissionLocked, f.ValidateSquad("m2", new[] { "a" }));
        Throws<InvalidOperationException>(() => f.Start("m2", new[] { "a" }));
        x.Win("m1"); f.Open(); Equal(SquadFailure.None, f.ValidateSquad("m2", new[] { "a" }));
        Equal(SquadFailure.None, f.ValidateSquad("m1", new[] { "a" })); Equal(1, f.InspectCampaign("starter").CompletedMissions);
    }
    private static void CampaignBranches()
    {
        var x = new CampaignFixture();
        var d = new CampaignDefinition("branch", "faction", "v1", new[] { new CampaignMission("root"), new CampaignMission("left",new[] { "root" }), new CampaignMission("right",new[] { "root" }), new CampaignMission("join",new[] { "left","right" }) }, Array.Empty<ResourceGrant>());
        var c = new CampaignCatalog(new[] { d }, d.Missions.Select(m => CampaignEntry(m.MissionId,"faction")), x.Units);
        x.Win("root"); x.Win("left"); Equal(false, c.IsMissionAvailable("join", x.Store.Load().Progress));
        x.Win("right"); Equal(true, c.IsMissionAvailable("join", x.Store.Load().Progress));
    }
    private static void CampaignHundred()
    {
        var x = new CampaignFixture(100); x.Finish(); var p = x.Catalog.Inspect("starter", x.Store.Load().Progress);
        Equal(100, p.TotalMissions); Equal(100, p.CompletedMissions); Equal(true, p.IsComplete); Equal(100, p.AvailableMissions.Count);
        x.Store.ClaimCampaign("starter", x.Catalog); Equal(true, x.Store.Load().Progress.Owns("x"));
    }
    private static void CampaignInvalidGraph()
    {
        foreach (var nodes in new[] { new[] { new CampaignMission("a",new[] { "a" }) }, new[] { new CampaignMission("a",new[] { "b" }), new CampaignMission("b",new[] { "a" }) }, new[] { new CampaignMission("a"),new CampaignMission("a") } })
            Throws<ArgumentException>(() => new CampaignDefinition("c","f","v",nodes,Array.Empty<ResourceGrant>()));
    }
    private static void CampaignInvalidRewards()
    {
        var x = new CampaignFixture();
        foreach (string apex in new[] { "a", "y", "missing" })
        {
            var bad = new CampaignDefinition("starter","faction","v", x.Definitions[0].Missions, Array.Empty<ResourceGrant>(),new StarterCampaignBonus(apex,x.Definitions[0].StarterBonus.Candidates));
            Throws<ArgumentException>(() => new CampaignCatalog(new[] { bad }.Concat(x.Definitions.Skip(1)), x.Entries,x.Units));
        }
        var wrong = new CampaignDefinition("starter","faction","v",x.Definitions[0].Missions,Array.Empty<ResourceGrant>(),new StarterCampaignBonus("x",new[] { new NextFactionBundle("next",new[] { "z","o2","y" }) }));
        Throws<ArgumentException>(() => new CampaignCatalog(new[] { wrong }.Concat(x.Definitions.Skip(1)),x.Entries,x.Units));
    }
    private static void CampaignStarterFaction()
    {
        var x = new CampaignFixture(); var f = new MissionFlow(new FakeSaveFiles(),new ProgressFiles(),"new","v",x.Entries,x.Units,x.Definitions);
        Throws<ArgumentException>(() => f.CreateProfile(new[] { "z","o2","o3" })); f.CreateProfile(new[] { "a","c","d" }); Equal(3,f.Progress.OwnedUnits.Count);
    }
    private static void CampaignUnfinished()
    {
        var x = new CampaignFixture(); x.Win("m1"); int writes=x.Files.Writes;
        Throws<InvalidOperationException>(() => x.Store.ClaimCampaign("starter",x.Catalog)); Equal(writes,x.Files.Writes);
    }
    private static void CampaignLoss()
    {
        var x = new CampaignFixture(); x.Win("m1"); x.Store.Claim(Result("loss","m2",false),Rewards("m2"));
        Equal(false,x.Catalog.Inspect("starter",x.Store.Load().Progress).IsComplete);
    }
    private static void CampaignOptional()
    {
        var x = new CampaignFixture(optional:true); x.Finish(); x.Store.ClaimCampaign("starter",x.Catalog);
        Equal(2,x.Catalog.Inspect("starter",x.Store.Load().Progress).CompletedMissions);
    }
    private static void CampaignFullFlow()
    {
        var x = new CampaignFixture(); var f=x.Flow(); f.Open();
        foreach (string id in new[] { "m1","m2" }) { f.Start(id,new[] { "a" }); WinFlow(f); f.ClaimRewards(); f.ReturnToSelection(); }
        f=x.Flow(); f.Open(); Equal(true,f.InspectCampaign("starter").IsComplete); Equal(false,f.InspectCampaign("starter").RewardsClaimed);
        var receipt=f.ClaimCampaign("starter").Receipt; Equal(true,f.Progress.Owns("x")); Equal(4,receipt.GrantedUnits.Count);
        Equal(true,f.InspectCampaign(receipt.NextCampaignId).CanEnter); Equal(27L,f.Progress.Balance("test-resource"));
        f=x.Flow(); f.Open(); var target=x.Definitions.First(c=>c.Id==receipt.NextCampaignId);
        string unit=x.Units.First(u=>u.FactionId==target.FactionId && !u.IsApex && f.Progress.Owns(u.Id)).Id;
        f.Start(target.Missions[0].MissionId,new[] { unit }); Equal(MissionFlowPhase.Battle,f.Phase);
    }
    private static void CampaignOrdinary()
    {
        var x = new CampaignFixture(); x.Finish(); x.Store.ClaimCampaign("starter",x.Catalog); x.Win("next-m");
        var c=x.Store.ClaimCampaign("next",x.Catalog); Equal(false,c.Receipt.IsStarterBonus); Equal(0,c.Receipt.GrantedUnits.Count);
        Equal(false,c.Saved.Progress.Owns("y")); Equal(1,c.Saved.Progress.CampaignClaims.Count(r=>r.IsStarterBonus));
    }
    private static void CampaignDuplicate()
    {
        var x = new CampaignFixture(); x.Finish(); var first=x.Store.ClaimCampaign("starter",x.Catalog); int writes=x.Files.Writes;
        x.Files.Inner.Fault=1; var again=new LocalProgressStore(x.Files,"campaign-profile").ClaimCampaign("starter",null);
        Equal(true,again.AlreadyClaimed); Equal(first.Receipt.NextCampaignId,again.Receipt.NextCampaignId); Equal(writes,x.Files.Writes);
    }
    private static void CampaignOwned()
    {
        var x = new CampaignFixture(initial:new[] { "a","c","d","x","z","o2","o3","t1","t2","t3" }); x.Finish();
        var c=x.Store.ClaimCampaign("starter",x.Catalog); Equal(0,c.Receipt.GrantedUnits.Count); Equal(10,c.Saved.Progress.OwnedUnits.Count); Equal(27L,c.Saved.Progress.Balance("test-resource"));
    }
    private static void CampaignStableSelection()
    {
        var a=new CampaignFixture(); var b=new CampaignFixture(reverse:true); a.Finish(); b.Finish();
        Equal(a.Store.ClaimCampaign("starter",a.Catalog).Receipt.NextCampaignId,b.Store.ClaimCampaign("starter",b.Catalog).Receipt.NextCampaignId);
    }
    private static void CampaignLockedPreference()
    {
        var x=new CampaignFixture(initial:new[] { "a","c","d","z","o2","o3" }); x.Finish();
        Equal("third",x.Store.ClaimCampaign("starter",x.Catalog).Receipt.NextCampaignId);
    }
    private static void CampaignInterrupted()
    {
        foreach(int fault in new[] { 1,2 })
        {
            var x=new CampaignFixture(); x.Finish(); x.Files.Inner.Fault=fault;
            Throws<IOException>(()=>x.Store.ClaimCampaign("starter",x.Catalog)); x.Files.Inner.Fault=0;
            var p=x.Store.Load().Progress; Equal(false,p.Owns("x")); Equal(20L,p.Balance("test-resource")); Equal(0,p.CampaignClaims.Count);
            x.Store.ClaimCampaign("starter",x.Catalog); Equal(27L,x.Store.Load().Progress.Balance("test-resource"));
        }
    }
    private static void CampaignAmbiguous()
    {
        var x=new CampaignFixture(); x.Finish(); x.Files.Inner.Fault=3; Throws<IOException>(()=>x.Store.ClaimCampaign("starter",x.Catalog));
        x.Files.Inner.Fault=0; Equal(true,x.Store.ClaimCampaign("starter",x.Catalog).AlreadyClaimed); Equal(27L,x.Store.Load().Progress.Balance("test-resource"));
    }
    private static void CampaignRecovery()
    {
        var x=new CampaignFixture(); x.Finish(); x.Store.ClaimCampaign("starter",x.Catalog); x.Files.Inner.Slots[1][20]^=1;
        var p=x.Store.Load(); Equal(true,p.Recovered); Equal(false,p.Progress.Owns("x")); Equal(20L,p.Progress.Balance("test-resource"));
        x.Store.ClaimCampaign("starter",x.Catalog); Equal(27L,x.Store.Load().Progress.Balance("test-resource"));
    }
    private static void CampaignLaterTransactions()
    {
        var x=new CampaignFixture(); x.Finish(); x.Store.ClaimCampaign("starter",x.Catalog); x.Store.Unlock("op",UnlockDef("extra"));
        x.Store.Claim(Result("replay","m1"),Rewards("m1")); var p=x.Store.Load().Progress;
        Equal(true,p.Owns("x")); Equal(true,p.Owns("extra")); Equal(1,p.CampaignClaims.Count); Equal(24L,p.Balance("test-resource"));
    }
    private static void CampaignSingleStarter()
    {
        var x=new CampaignFixture(); x.Finish(); x.Store.ClaimCampaign("starter",x.Catalog);
        var changed=new CampaignFixture(starterId:"another-starter",existing:x.Files);
        Throws<InvalidOperationException>(()=>changed.Store.ClaimCampaign("another-starter",changed.Catalog));
    }
    private static void CampaignOverflow()
    {
        var x=new CampaignFixture(reward:long.MaxValue); x.Finish();
        Throws<OverflowException>(()=>x.Store.ClaimCampaign("starter",x.Catalog)); Equal(false,x.Store.Load().Progress.Owns("x")); Equal(0,x.Store.Load().Progress.CampaignClaims.Count);
    }
    private static byte[] SchemaTwo(byte[] current)
    {
        var bytes=current.Take(current.Length-40).Concat(new byte[32]).ToArray();
        using var stream=new MemoryStream(bytes); using var r=new BinaryReader(stream); stream.Position=8; r.ReadString();
        Array.Copy(BitConverter.GetBytes(2),0,bytes,(int)stream.Position,4); Rehash(bytes); return bytes;
    }
    private static void CampaignSchemaTwo()
    {
        var x=new CampaignFixture(); x.Finish(); x.Store.Unlock("op",UnlockDef("extra"));
        for(int i=0;i<2;i++) x.Files.Inner.Slots[i]=SchemaTwo(x.Files.Inner.Slots[i]);
        Equal(true,x.Store.Load().Progress.Owns("extra")); x.Store.ClaimCampaign("starter",x.Catalog);
        Equal(22L,x.Store.Load().Progress.Balance("test-resource")); Equal(true,x.Store.Load().Progress.Owns("extra"));
    }
    private static void CampaignFlowGate()
    {
        var x=new CampaignFixture(); var f=x.Flow(); f.Open(); f.Start("m1",new[] { "a" });
        Throws<InvalidOperationException>(()=>f.ClaimCampaign("starter")); WinFlow(f); Throws<InvalidOperationException>(()=>f.ClaimCampaign("starter"));
    }
    private static void CampaignFlowRetry()
    {
        var x=new CampaignFixture(); x.Finish(); var f=x.Flow(); f.Open(); x.Files.Inner.Fault=3;
        Throws<IOException>(()=>f.ClaimCampaign("starter")); Equal(true,f.NeedsReload); x.Files.Inner.Fault=0; f.Open();
        Equal(true,f.InspectCampaign("starter").RewardsClaimed); Equal(true,f.Progress.Owns("x")); Equal(true,f.ClaimCampaign("starter").AlreadyClaimed);
    }
    private static void CampaignImmutable()
    {
        var prerequisite=new[] { "a" }; var node=new CampaignMission("b",prerequisite); prerequisite[0]="edited"; Equal("a",node.Prerequisites[0]);
        var x=new CampaignFixture(); x.Finish(); var before=x.Store.Load().Progress; var r=x.Store.ClaimCampaign("starter",x.Catalog).Receipt;
        Equal(false,before.Owns("x")); Throws<NotSupportedException>(()=>((IList<string>)r.GrantedUnits).Clear());
    }
    private static void CampaignBlockedEntry()
    {
        var x=new CampaignFixture(blocked:true); x.Finish();
        Throws<InvalidOperationException>(()=>x.Store.ClaimCampaign("starter",x.Catalog)); Equal(false,x.Store.Load().Progress.Owns("x"));
    }
}
