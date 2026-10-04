using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Views;

internal static partial class Program
{
    private static (string Name,Action Run)[] PresentationTests() => new (string,Action)[] {
        ("Presentation snapshots expose battlefield and turn queue", ViewSnapshot),
        ("Presentation collections and prior snapshots cannot mutate battle", ViewDetached),
        ("Presentation previews never checkpoint or spend resources", ViewPure),
        ("Presentation target list includes validity and resolved effect", ViewTargets),
        ("Presentation rejects stale and nonplayer previews", ViewOwnership),
        ("Presentation movement emits committed route", ViewMovement),
        ("Presentation rejected commands return no batch", ViewRejected),
        ("Presentation action availability follows spent action", ViewSpent),
        ("Presentation signature consumption survives reload", ViewSignature),
        ("Presentation victory emits health removal objective and result", ViewVictory),
        ("Presentation interaction preview and committed events agree", ViewInteraction),
        ("Presentation enemy action emits effect and activation end", ViewEnemy),
        ("Presentation waiting emits no events or checkpoint", ViewWait),
        ("Presentation scheduler emits separate round and activation events", ViewScheduler),
        ("Presentation duration changes emit status refresh hint", ViewStatus),
        ("Presentation torn save returns no event batch", ViewTorn),
        ("Presentation ambiguous save reload does not replay action", ViewAmbiguous),
        ("Presentation selection is empty after explicit reward claim", ViewSelection)
    };
    private static BattleUnitView VUnit(BattleView v,string id)=>v.Units.Single(u=>u.Id==id);
    private static bool Has(BattleUpdate u,BattleEventKind k)=>u.Events.Any(e=>e.Kind==k);
    private static void ViewSnapshot()
    {
        var x=new SessionFixture(); var v=new BattlePresentation(x.Session).Read();
        Equal("session-m",v.MissionId); Equal(3,v.Units.Count); Equal("a,b,c",string.Join(",",v.UpcomingTurns));
        Equal(P(0,0),VUnit(v,"a").Position.Value); Equal(100,VUnit(v,"b").Health.CurrentHealth);
        Equal(true,VUnit(v,"a").IsPlayerSquad); Equal(true,VUnit(v,"b").IsEnemyControlled);
        Equal(4,v.Actions.Count); Equal(false,v.Actions.Single(a=>a.Slot==AbilitySlot.Passive).CanSelect);
        Equal(true,v.Actions.Single(a=>a.Slot==AbilitySlot.Signature).CanSelect);
    }
    private static void ViewDetached()
    {
        var x=new SessionFixture(); var p=new BattlePresentation(x.Session); var v=p.Read();
        Throws<NotSupportedException>(()=>((IList<string>)v.UpcomingTurns)[0]="b");
        p.TryMove(x.Id,new[] { P(1,0) },out _,out _); Equal(P(0,0),VUnit(v,"a").Position.Value); Equal(5m,v.Activation.MovementRemaining);
    }
    private static void ViewPure()
    {
        var x=new SessionFixture(); var p=new BattlePresentation(x.Session); string before=x.Bytes();
        for(int i=0;i<3;i++) { p.Read(); Equal(true,p.TryPreviewDestination(x.Id,P(1,0),out _,out _)); Equal(true,p.TryPreviewPath(x.Id,new[] { P(1,0) },out _,out _)); p.PreviewTargets(x.Id,AbilitySlot.NormalAttack); p.PreviewInteraction(x.Id,"primary"); }
        Equal(before,x.Bytes()); Equal(5m,p.Read().Activation.MovementRemaining); Equal(true,p.Read().Activation.PrimaryActionAvailable);
    }
    private static void ViewTargets()
    {
        var x=new SessionFixture(); var p=new BattlePresentation(x.Session); var targets=p.PreviewTargets(x.Id,AbilitySlot.NormalAttack);
        Equal(3,targets.Count); Equal(false,targets.Single(t=>t.TargetId=="a").IsValid); var b=targets.Single(t=>t.TargetId=="b");
        Equal(true,b.IsValid); Equal(60,b.Effect.Health.HealthAfter); Equal(false,p.PreviewTarget(x.Id,AbilitySlot.NormalAttack,"missing").IsValid);
    }
    private static void ViewOwnership()
    {
        var x=new SessionFixture(); var p=new BattlePresentation(x.Session); Throws<InvalidOperationException>(()=>p.PreviewTarget(999,AbilitySlot.NormalAttack,"b"));
        x.ToEnemy(); Throws<InvalidOperationException>(()=>p.TryPreviewDestination(x.Id,P(1,0),out _,out _)); Equal(true,p.Read().Actions.All(a=>a.Block==ActionBlock.NotPlayerTurn));
    }
    private static void ViewMovement()
    {
        var x=new SessionFixture(); var p=new BattlePresentation(x.Session); Equal(true,p.TryMove(x.Id,new[] { P(1,0),P(2,0) },out var u,out _));
        Equal(1,u.Events.Count); Equal(BattleEventKind.Movement,u.Events[0].Kind); Equal(2,u.Events[0].Movement.Path.Count);
        Equal(P(2,0),VUnit(u.After,"a").Position.Value); x.Reopen(); Equal(P(2,0),VUnit(new BattlePresentation(x.Session).Read(),"a").Position.Value);
    }
    private static void ViewRejected()
    {
        var x=new SessionFixture(); var p=new BattlePresentation(x.Session); string before=x.Bytes();
        Equal(false,p.TryMove(x.Id,new[] { P(19,19) },out var m,out _)); Equal<BattleUpdate>(null,m);
        Equal(false,p.TryUseAbility(x.Id,AbilitySlot.NormalAttack,"a",out var a,out _,out _)); Equal<BattleUpdate>(null,a);
        Equal(false,p.TryInteract(x.Id,"missing",out var i,out _)); Equal<BattleUpdate>(null,i); Equal(before,x.Bytes());
    }
    private static void ViewSpent()
    {
        var x=new SessionFixture(); var p=new BattlePresentation(x.Session); p.TryUseAbility(x.Id,AbilitySlot.NormalAttack,"b",out var u,out _,out _);
        Equal(true,Has(u,BattleEventKind.AbilityUsed)); Equal(true,Has(u,BattleEventKind.HealthChanged)); Equal(false,Has(u,BattleEventKind.ActivationEnded));
        Equal(AbilityUseFailure.PrimaryActionSpent,u.After.Actions.Single(a=>a.Slot==AbilitySlot.Main).RuleFailure);
        Equal(100,VUnit(u.Before,"b").Health.CurrentHealth); Equal(60,VUnit(u.After,"b").Health.CurrentHealth);
    }
    private static void ViewSignature()
    {
        var x=new SessionFixture(); var p=new BattlePresentation(x.Session); Equal(true,p.TryUseAbility(x.Id,AbilitySlot.Signature,"b",out _,out _,out _));
        x.Reopen(); Equal(true,VUnit(new BattlePresentation(x.Session).Read(),"a").Kit.SignatureUsed);
    }
    private static void ViewVictory()
    {
        var x=new SessionFixture(rounds:0,enemyHp:20); var p=new BattlePresentation(x.Session); p.TryUseAbility(x.Id,AbilitySlot.NormalAttack,"b",out var u,out _,out _);
        foreach(var k in new[] { BattleEventKind.HealthChanged,BattleEventKind.UnitRemoved,BattleEventKind.ObjectiveChanged,BattleEventKind.BattleEnded }) Equal(true,Has(u,k));
        Equal(MissionOutcome.Victory,u.After.Result.Outcome); Equal(0,u.After.UpcomingTurns.Count); Equal(0,p.Advance().Events.Count);
    }
    private static void ViewInteraction()
    {
        var x=new SessionFixture(stabilize:true); var p=new BattlePresentation(x.Session); Equal(InteractionFailure.None,p.PreviewInteraction(x.Id,"primary"));
        Equal(true,p.TryInteract(x.Id,"primary",out var u,out _)); Equal("primary",u.Events[0].ObjectiveId); Equal(true,Has(u,BattleEventKind.BattleEnded));
    }
    private static void ViewEnemy()
    {
        var x=new SessionFixture(); x.ToEnemy(); var p=new BattlePresentation(x.Session); var u=p.Advance(Seen());
        Equal(true,Has(u,BattleEventKind.AbilityUsed)); Equal(true,Has(u,BattleEventKind.ActivationEnded)); Equal(80,VUnit(u.After,"a").Health.CurrentHealth);
        Equal("a",u.Events.Single(e=>e.Kind==BattleEventKind.AbilityUsed).AbilityEffect.TargetId);
    }
    private static void ViewWait()
    {
        var x=new SessionFixture(); var p=new BattlePresentation(x.Session); string before=x.Bytes(); Equal(0,p.Advance().Events.Count); Equal(before,x.Bytes());
        x.ToEnemy(); before=x.Bytes(); Equal(0,p.Advance().Events.Count); Equal(before,x.Bytes());
    }
    private static void ViewScheduler()
    {
        var x=new SessionFixture(started:false); var p=new BattlePresentation(x.Session);
        Equal(true,Has(p.Advance(),BattleEventKind.RoundStarted)); Equal(true,Has(p.Advance(),BattleEventKind.ActivationStarted));
        Equal(true,Has(p.EndPlayerTurn(x.Id),BattleEventKind.ActivationEnded)); Equal("b,c",string.Join(",",p.Read().UpcomingTurns));
    }
    private static void ViewStatus()
    {
        var x=new SessionFixture(status:true); var p=new BattlePresentation(x.Session); var before=p.Read(); var u=p.EndPlayerTurn(x.Id);
        Equal(true,Has(u,BattleEventKind.StatusChanged)); Equal(2,VUnit(before,"a").Statuses.Single().RemainingOwnerActivations); Equal(1,VUnit(u.After,"a").Statuses.Single().RemainingOwnerActivations);
    }
    private static void ViewTorn()
    {
        var x=new SessionFixture(); var p=new BattlePresentation(x.Session); long id=x.Id; BattleUpdate u=null; x.Files.Fault=2;
        Throws<IOException>(()=>p.TryMove(id,new[] { P(1,0) },out u,out _)); Equal<BattleUpdate>(null,u); Throws<InvalidOperationException>(()=>p.Read());
        x.Files.Fault=0; x.Reopen(); Equal(P(0,0),VUnit(new BattlePresentation(x.Session).Read(),"a").Position.Value);
    }
    private static void ViewAmbiguous()
    {
        var x=new SessionFixture(); var p=new BattlePresentation(x.Session); long id=x.Id; BattleUpdate u=null; x.Files.Fault=3;
        Throws<IOException>(()=>p.TryUseAbility(id,AbilitySlot.NormalAttack,"b",out u,out _,out _)); Equal<BattleUpdate>(null,u);
        x.Files.Fault=0; x.Reopen(); p=new BattlePresentation(x.Session); Equal(60,VUnit(p.Read(),"b").Health.CurrentHealth); Equal(0,p.Advance().Events.Count);
    }
    private static void ViewSelection()
    {
        var x=new SessionFixture(stabilize:true); var p=new BattlePresentation(x.Session); p.TryInteract(x.Id,"primary",out _,out _); x.Flow.ClaimRewards(); x.Flow.ReturnToSelection();
        var v=p.Read(); Equal(BattleSessionPhase.Selection,v.Phase); Equal(0,v.Units.Count); Equal(0,v.UpcomingTurns.Count); Equal(0,p.Advance().Events.Count);
    }
}
