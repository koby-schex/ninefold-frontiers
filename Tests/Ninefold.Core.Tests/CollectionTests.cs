using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Views;

internal static partial class Program
{
    private static (string Name,Action Run)[] CollectionTests() => new (string,Action)[] {
        ("Collection groups collectible units by faction", CollectionGroups),
        ("Collection reads previews and cancel never save", CollectionPure),
        ("Collection shows exact fragment shortage", CollectionShortage),
        ("Collection unlock spends only matching fragments", CollectionUnlock),
        ("Collection advance preview matches saved and deployed stats", CollectionAdvance),
        ("Collection customization previews tradeoffs and free reset", CollectionCustomize),
        ("Collection combines rank and customization once", CollectionCombined),
        ("Collection duplicate confirmation cannot spend twice", CollectionDuplicate),
        ("Collection replacement rejects old confirmation ID", CollectionReplacement),
        ("Collection card offers are not armed confirmations", CollectionUnarmed),
        ("Collection handles locked owned capped and unknown choices", CollectionBlocks),
        ("Collection forbids changes during battle and results", CollectionBattle),
        ("Collection changes invalidate prepared launch tokens", CollectionPlan),
        ("Collection reload clears pending offer", CollectionReload),
        ("Collection views remain immutable and detached", CollectionDetached),
        ("Collection failed writes recover without spending", CollectionFailures),
        ("Collection lost acknowledgement restores each operation once", CollectionAmbiguous),
        ("Collection cannot be read before opening profile", CollectionClosed)
    };
    private static CollectionUnit CUnit(MenuFixture x,string id)=>x.Menu.Collection.Read().Factions.SelectMany(f=>f.Units).Single(u=>u.Unit.Definition.Id==id);
    private static void CollectionGroups()
    {
        var x=new MenuFixture(); var v=x.Menu.Collection.Read(); Equal(2,v.Factions.Count); Equal(5,v.Factions[0].Units.Count);
        Equal(3,v.Factions.SelectMany(f=>f.Units).Count(u=>u.Unit.Owned)); Equal(2,v.Factions.SelectMany(f=>f.Units).Count(u=>u.Unit.Definition.Roster.IsApex));
    }
    private static void CollectionPure()
    {
        var x=new MenuFixture(); string before=x.Bytes(); var c=x.Menu.Collection; c.Read(); var o=c.Preview(CollectionOperation.Customize,"fixture-a-1","fixture-health");
        Equal(105,o.After.Health); Equal(19,o.After.Armor); c.Cancel(); Equal<CollectionOffer>(null,c.Read().Pending); Equal(before,x.Bytes());
    }
    private static void CollectionShortage()
    {
        var x=new MenuFixture(); var o=x.Menu.Collection.Preview(CollectionOperation.Unlock,"fixture-a-4"); Equal(5L,o.Cost); Equal(0L,o.Balance); Equal(5L,o.MissingFragments);
        Equal(CollectionBlock.InsufficientFragments,o.Block); Throws<InvalidOperationException>(()=>x.Menu.Collection.Confirm(o.Id)); Equal(0L,CUnit(x,"fixture-a-4").Fragments);
    }
    private static void CollectionUnlock()
    {
        var x=new MenuFixture(); x.Clear(); var c=x.Menu.Collection; var o=c.Preview(CollectionOperation.Unlock,"fixture-a-4"); Equal(true,o.CanConfirm);
        var r=c.Confirm(o.Id); Equal(false,r.Before.Owns("fixture-a-4")); Equal(true,r.After.Owns("fixture-a-4")); Equal(0L,r.After.Balance(o.ResourceId)); Equal(5L,r.After.Balance("fragment-fixture-a-1"));
        x.Prepare("fixture-opening","fixture-a-4"); Equal(true,x.Menu.Read().CanStart); x.Reopen(); Equal(true,CUnit(x,"fixture-a-4").Unit.Owned);
    }
    private static void CollectionAdvance()
    {
        var x=new MenuFixture(); x.Clear(); var c=x.Menu.Collection; var o=c.Preview(CollectionOperation.Advance,"fixture-a-1");
        Equal(5L,o.Cost); Equal(0,o.RankBefore); Equal(1,o.RankAfter); Equal(105,o.After.Health); Equal(21,o.After.Armor);
        Equal(42,o.After.Abilities.Single(a=>a.Slot==AbilitySlot.NormalAttack).Effect.Amount); c.Confirm(o.Id); Equal(CollectionBlock.MaximumRank,CUnit(x,"fixture-a-1").Advance.Block);
        x.Prepare(); var deployed=x.Menu.Start(x.Menu.Read().PlanId).Read().Battle.Units.Single(u=>u.Id=="fixture-a-1"); Equal(o.After.Health,deployed.Health.MaximumHealth); Equal(o.After.Armor,deployed.Health.Armor);
    }
    private static void CollectionCustomize()
    {
        var x=new MenuFixture(); var c=x.Menu.Collection; var o=c.Preview(CollectionOperation.Customize,"fixture-a-1","fixture-health"); Equal(0L,o.Cost); Equal<string>(null,o.ResourceId);
        c.Confirm(o.Id); Equal(105,CUnit(x,"fixture-a-1").Unit.Health); Equal(19,CUnit(x,"fixture-a-1").Unit.Armor);
        Equal(CollectionBlock.NoChange,c.Preview(CollectionOperation.Customize,"fixture-a-1","fixture-health").Block);
        o=c.Preview(CollectionOperation.Customize,"fixture-a-1"); Equal(100,o.After.Health); c.Confirm(o.Id); Equal<string>(null,CUnit(x,"fixture-a-1").Unit.CustomizationId);
    }
    private static void CollectionCombined()
    {
        var x=new MenuFixture(); x.Clear(); var c=x.Menu.Collection; c.Confirm(c.Preview(CollectionOperation.Advance,"fixture-a-1").Id);
        var o=c.Preview(CollectionOperation.Customize,"fixture-a-1","fixture-health"); Equal(110,o.After.Health); Equal(20,o.After.Armor); c.Confirm(o.Id);
        Equal(110,CUnit(x,"fixture-a-1").Unit.Health); Equal(42,CUnit(x,"fixture-a-1").Unit.Abilities.Single(a=>a.Slot==AbilitySlot.NormalAttack).Effect.Amount);
    }
    private static void CollectionDuplicate()
    {
        var x=new MenuFixture(); x.Clear(); var c=x.Menu.Collection; var o=c.Preview(CollectionOperation.Advance,"fixture-a-1"); c.Confirm(o.Id); int writes=x.Profiles.Writes;
        Throws<InvalidOperationException>(()=>c.Confirm(o.Id)); Equal(writes,x.Profiles.Writes); Equal(1,CUnit(x,"fixture-a-1").Unit.Rank);
    }
    private static void CollectionReplacement()
    {
        var x=new MenuFixture(); var c=x.Menu.Collection; var a=c.Preview(CollectionOperation.Customize,"fixture-a-1","fixture-health"); var b=c.Preview(CollectionOperation.Customize,"fixture-a-2","fixture-health");
        Throws<InvalidOperationException>(()=>c.Confirm(a.Id)); Equal(b.Id,c.Read().Pending.Id); c.Confirm(b.Id); Equal(100,CUnit(x,"fixture-a-1").Unit.Health); Equal(105,CUnit(x,"fixture-a-2").Unit.Health);
    }
    private static void CollectionUnarmed()
    { var x=new MenuFixture(); x.Clear(); Throws<InvalidOperationException>(()=>x.Menu.Collection.Confirm(CUnit(x,"fixture-a-4").Unlock.Id)); Equal(false,CUnit(x,"fixture-a-4").Unit.Owned); }
    private static void CollectionBlocks()
    {
        var x=new MenuFixture(); var c=x.Menu.Collection; Equal(CollectionBlock.AlreadyOwned,c.Preview(CollectionOperation.Unlock,"fixture-a-1").Block);
        Equal(CollectionBlock.NotOwned,c.Preview(CollectionOperation.Advance,"fixture-a-4").Block); Equal(CollectionBlock.NotOwned,c.Preview(CollectionOperation.Customize,"fixture-a-4","fixture-health").Block);
        Equal(CollectionBlock.UnknownOption,c.Preview(CollectionOperation.Customize,"fixture-a-1","bad").Block); Equal(CollectionBlock.NoChange,c.Preview(CollectionOperation.Customize,"fixture-a-1").Block);
        Throws<ArgumentException>(()=>c.Preview(CollectionOperation.Unlock,"fixture-npc")); Throws<ArgumentException>(()=>c.Preview(CollectionOperation.Unlock,"missing"));
        Throws<ArgumentException>(()=>c.Preview(CollectionOperation.Unlock,"fixture-a-4","wrong")); Equal<CollectionOffer>(null,c.Read().Pending);
    }
    private static void CollectionBattle()
    {
        var x=new MenuFixture(); var c=x.Menu.Collection; var o=c.Preview(CollectionOperation.Customize,"fixture-a-1","fixture-health"); x.Prepare(); var b=x.Menu.Start(x.Menu.Read().PlanId);
        Throws<InvalidOperationException>(()=>c.Confirm(o.Id)); Equal(CollectionBlock.NotAtSelection,c.Preview(CollectionOperation.Customize,"fixture-a-1","fixture-health").Block);
        b.TapObjective("primary"); b.TapObjective("primary"); o=c.Preview(CollectionOperation.Customize,"fixture-a-1","fixture-health"); Equal(CollectionBlock.NotAtSelection,o.Block); Throws<InvalidOperationException>(()=>c.Confirm(o.Id));
    }
    private static void CollectionPlan()
    {
        var x=new MenuFixture(); x.Prepare(); var plan=x.Menu.Read().PlanId; var c=x.Menu.Collection; c.Confirm(c.Preview(CollectionOperation.Customize,"fixture-a-1","fixture-health").Id);
        Throws<InvalidOperationException>(()=>x.Menu.Start(plan)); Equal(105,x.Menu.Read().Units.Single(u=>u.Definition.Id=="fixture-a-1").Health); x.Menu.Start(x.Menu.Read().PlanId);
    }
    private static void CollectionReload()
    {
        var x=new MenuFixture(); var c=x.Menu.Collection; var o=c.Preview(CollectionOperation.Customize,"fixture-a-1","fixture-health"); x.Menu.Open();
        Throws<InvalidOperationException>(()=>c.Confirm(o.Id)); Equal<CollectionOffer>(null,c.Read().Pending);
    }
    private static void CollectionDetached()
    {
        var x=new MenuFixture(); var v=x.Menu.Collection.Read(); Throws<NotSupportedException>(()=>((IList<CollectionFaction>)v.Factions).Clear());
        var o=x.Menu.Collection.Preview(CollectionOperation.Customize,"fixture-a-1","fixture-health"); x.Menu.Collection.Confirm(o.Id);
        Equal(100,v.Factions[0].Units[0].Unit.Health); Equal(100,o.Before.Health); Equal(105,o.After.Health);
    }
    private static void CollectionFailures()
    {
        foreach(var op in new[] { CollectionOperation.Unlock,CollectionOperation.Advance,CollectionOperation.Customize })
        foreach(int fault in new[] { 1,2 })
        {
            var x=new MenuFixture(); x.Clear(); var c=x.Menu.Collection; string unit=op==CollectionOperation.Unlock?"fixture-a-4":"fixture-a-1";
            var o=c.Preview(op,unit,op==CollectionOperation.Customize?"fixture-health":null); x.Profiles.Inner.Fault=fault;
            Throws<IOException>(()=>c.Confirm(o.Id)); Equal(true,x.Menu.NeedsReload); Throws<InvalidOperationException>(()=>c.Read()); x.Profiles.Inner.Fault=0; x.Reopen();
            Equal(5L,CUnit(x,unit).Fragments); Equal(0,CUnit(x,unit).Unit.Rank); Equal<string>(null,CUnit(x,unit).Unit.CustomizationId); if(op==CollectionOperation.Unlock) Equal(false,CUnit(x,unit).Unit.Owned);
            c=x.Menu.Collection; c.Confirm(c.Preview(op,unit,op==CollectionOperation.Customize?"fixture-health":null).Id);
        }
    }
    private static void CollectionAmbiguous()
    {
        foreach(var op in new[] { CollectionOperation.Unlock,CollectionOperation.Advance,CollectionOperation.Customize })
        {
            var x=new MenuFixture(); x.Clear(); var c=x.Menu.Collection; string unit=op==CollectionOperation.Unlock?"fixture-a-4":"fixture-a-1";
            var o=c.Preview(op,unit,op==CollectionOperation.Customize?"fixture-health":null); x.Profiles.Inner.Fault=3; Throws<IOException>(()=>c.Confirm(o.Id)); x.Profiles.Inner.Fault=0; x.Reopen();
            int writes=x.Profiles.Writes; Throws<InvalidOperationException>(()=>x.Menu.Collection.Confirm(o.Id)); Equal(writes,x.Profiles.Writes);
            var p=x.Menu.Read().Progress; Equal(1,p.Unlocks.Count+p.AdvancementReceipts.Count+p.CustomizationReceipts.Count); Equal(op==CollectionOperation.Customize?5L:0L,CUnit(x,unit).Fragments);
        }
    }
    private static void CollectionClosed()
    { var x=new MenuFixture(create:false); Throws<InvalidOperationException>(()=>x.Menu.Collection.Read()); Throws<InvalidOperationException>(()=>x.Menu.Collection.Preview(CollectionOperation.Unlock,"fixture-a-4")); }
}
