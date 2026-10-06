using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Views;

internal static partial class Program
{
    private static (string Name,Action Run)[] InteractionTests() => new (string,Action)[] {
        ("Health overlays include every active team without selection", InputHealth),
        ("Health overlays are immutable and detached", InputHealthDetached),
        ("Health overlays follow damage healing and removal", InputHealthChanges),
        ("Health overlays track committed movement and reload", InputHealthPosition),
        ("Inspection never grants control or spends resources", InputInspection),
        ("First destination tap previews second tap commits", InputMove),
        ("Confirmation button commits the displayed preview", InputButton),
        ("Replacement preview rejects old button token", InputReplacement),
        ("Invalid destination clears previous preview without save", InputInvalidMove),
        ("Cancel clears selection and intent without spending", InputCancel),
        ("Ability target previews are pure then commit on second tap", InputAttack),
        ("Invalid ability and target cannot leave an armed old preview", InputInvalidAttack),
        ("Inspection can exit targeting without firing", InputExitTargeting),
        ("All battle commands are locked during animation", InputAnimation),
        ("Old animation acknowledgements cannot unlock a new batch", InputOldAnimation),
        ("Duplicate confirmations do not repeat commands", InputDuplicate),
        ("Changed same-activation checkpoint invalidates preview", InputStaleCheckpoint),
        ("Changed activation invalidates preview", InputStaleTurn),
        ("Reload on same flow invalidates in-memory preview", InputSameFlowReload),
        ("Reading after external change clears targeting state", InputRefresh),
        ("Waiting preserves player preview indefinitely", InputWait),
        ("Enemy advance updates health without selecting player", InputEnemy),
        ("Objective uses preview and confirmation", InputObjective),
        ("End turn requires current activation and clears preview", InputEnd),
        ("Torn checkpoint cannot return committed input result", InputTorn),
        ("Ambiguous checkpoint reload cannot replay confirmation", InputAmbiguous),
        ("Result and selection phases reject battle input", InputTerminal)
    };
    private static BattleInteraction Input(SessionFixture x)=>new BattleInteraction(new BattlePresentation(x.Session));
    private static UnitHealthOverlay Bar(BattleInteraction i,string id)=>i.Read().Battle.HealthOverlays.Single(h=>h.UnitId==id);
    private static void InputHealth()
    {
        var x=new SessionFixture(playerHp:75,enemyHp:40); var i=Input(x); string before=x.Bytes(); var v=i.Read();
        Equal<string>(null,v.SelectedUnitId); Equal(3,v.Battle.HealthOverlays.Count); Equal(0.75m,Bar(i,"a").Fraction);
        Equal(100,Bar(i,"b").MaximumHealth); Equal(40,Bar(i,"b").CurrentHealth); Equal("enemy",Bar(i,"b").TeamId);
        Equal(true,Bar(i,"a").IsPlayerSquad); Equal(true,Bar(i,"b").IsEnemyControlled); Equal(false,Bar(i,"c").IsPlayerSquad);
        Equal(before,x.Bytes());
    }
    private static void InputHealthDetached()
    {
        var x=new SessionFixture(); var i=Input(x); var bars=i.Read().Battle.HealthOverlays;
        Throws<NotSupportedException>(()=>((IList<UnitHealthOverlay>)bars).Clear());
        x.Session.TryUseAbility(x.Id,AbilitySlot.NormalAttack,"b",out _,out _,out _);
        Equal(100,bars.Single(h=>h.UnitId=="b").CurrentHealth); Equal(60,Bar(i,"b").CurrentHealth);
    }
    private static void InputHealthChanges()
    {
        var x=new SessionFixture(playerHp:50); var i=Input(x); i.SelectAbility(AbilitySlot.Main); i.TapUnit("a"); var healed=i.Confirm(i.Read().Pending.Id);
        Equal(InputOutcome.Committed,healed.Outcome); Equal(80,Bar(i,"a").CurrentHealth); Equal(0.8m,Bar(i,"a").Fraction);
        var y=new SessionFixture(enemyHp:20); var j=Input(y); j.SelectAbility(AbilitySlot.NormalAttack); j.TapUnit("b"); var hit=j.TapUnit("b");
        Equal(3,hit.Update.Before.HealthOverlays.Count); Equal(2,hit.Update.After.HealthOverlays.Count); Equal(false,hit.Update.After.HealthOverlays.Any(h=>h.UnitId=="b"));
    }
    private static void InputHealthPosition()
    {
        var x=new SessionFixture(); var i=Input(x); i.TapDestination(P(1,0)); Equal(P(0,0),Bar(i,"a").Position);
        i.TapDestination(P(1,0)); Equal(P(1,0),Bar(i,"a").Position); x.Reopen(); i=Input(x);
        Equal(P(1,0),Bar(i,"a").Position); Equal<string>(null,i.Read().SelectedUnitId); Equal(false,i.Read().IsAnimating);
    }
    private static void InputInspection()
    {
        var x=new SessionFixture(); var i=Input(x); string before=x.Bytes(); Equal(InputOutcome.Selected,i.TapUnit("b").Outcome);
        Equal("b",i.Read().SelectedUnitId); Equal("a",i.Read().Battle.Activation.UnitId); Equal(InputOutcome.Rejected,i.InspectUnit("missing").Outcome); Equal(before,x.Bytes());
    }
    private static void InputMove()
    {
        var x=new SessionFixture(); var i=Input(x); string before=x.Bytes(); Equal(InputOutcome.Previewed,i.TapDestination(P(1,0)).Outcome);
        Equal(true,i.Read().CanConfirm); Equal(1m,i.Read().Pending.Movement.Cost); Equal(before,x.Bytes());
        var r=i.TapDestination(P(1,0)); Equal(InputOutcome.Committed,r.Outcome); Equal(4m,r.Update.After.Activation.MovementRemaining); Equal(true,i.Read().IsAnimating);
    }
    private static void InputButton()
    {
        var x=new SessionFixture(); var i=Input(x); i.TapDestination(P(2,0)); var id=i.Read().Pending.Id;
        Equal(InputOutcome.Committed,i.Confirm(id).Outcome); Equal(P(2,0),Bar(i,"a").Position);
    }
    private static void InputReplacement()
    {
        var x=new SessionFixture(); var i=Input(x); i.TapDestination(P(1,0)); string old=i.Read().Pending.Id;
        Equal(InputOutcome.Previewed,i.TapDestination(P(2,0)).Outcome); string current=i.Read().Pending.Id; string before=x.Bytes();
        Equal(InputOutcome.Stale,i.Confirm(old).Outcome); Equal(current,i.Read().Pending.Id); Equal(before,x.Bytes()); Equal(InputOutcome.Committed,i.Confirm(current).Outcome);
    }
    private static void InputInvalidMove()
    {
        var x=new SessionFixture(); var i=Input(x); i.TapDestination(P(1,0)); string before=x.Bytes(); var r=i.TapDestination(P(19,19));
        Equal(InputOutcome.Rejected,r.Outcome); Equal(false,r.FieldFailure==FieldFailure.None); Equal(false,i.Read().CanConfirm); Equal(before,x.Bytes());
    }
    private static void InputCancel()
    {
        var x=new SessionFixture(); var i=Input(x); string before=x.Bytes(); i.TapDestination(P(1,0)); var id=i.Read().Pending.Id; i.Cancel();
        Equal<string>(null,i.Read().SelectedUnitId); Equal(false,i.Read().CanConfirm); Equal(InputOutcome.NoPreview,i.Confirm(id).Outcome); Equal(before,x.Bytes()); Equal(3,i.Read().Battle.HealthOverlays.Count);
    }
    private static void InputAttack()
    {
        var x=new SessionFixture(); var i=Input(x); string before=x.Bytes(); Equal(InputOutcome.Selected,i.SelectAbility(AbilitySlot.NormalAttack).Outcome);
        Equal(InputOutcome.Previewed,i.TapUnit("b").Outcome); Equal(60,i.Read().Pending.Target.Effect.Health.HealthAfter); Equal(100,Bar(i,"b").CurrentHealth); Equal(before,x.Bytes());
        Equal(InputOutcome.Committed,i.TapUnit("b").Outcome); Equal(60,Bar(i,"b").CurrentHealth);
    }
    private static void InputInvalidAttack()
    {
        var x=new SessionFixture(); var i=Input(x); i.SelectAbility(AbilitySlot.NormalAttack); i.TapUnit("b"); string before=x.Bytes();
        Equal(InputOutcome.Rejected,i.TapUnit("a").Outcome); Equal(false,i.Read().CanConfirm);
        i.TapUnit("b"); Equal(InputOutcome.Rejected,i.SelectAbility(AbilitySlot.Passive).Outcome); Equal(false,i.Read().CanConfirm); Equal(before,x.Bytes());
    }
    private static void InputExitTargeting()
    {
        var x=new SessionFixture(); var i=Input(x); i.SelectAbility(AbilitySlot.NormalAttack); i.TapUnit("b"); string before=x.Bytes();
        Equal(InputOutcome.Selected,i.InspectUnit("b").Outcome); Equal<AbilitySlot?>(null,i.Read().SelectedAbility); Equal(false,i.Read().CanConfirm); Equal(before,x.Bytes());
    }
    private static void InputAnimation()
    {
        var x=new SessionFixture(); var i=Input(x); i.TapDestination(P(1,0)); var r=i.TapDestination(P(1,0)); string before=x.Bytes();
        Equal(InputOutcome.Locked,i.TapDestination(P(2,0)).Outcome); Equal(InputOutcome.Locked,i.SelectAbility(AbilitySlot.Main).Outcome);
        Equal(InputOutcome.Locked,i.TapUnit("b").Outcome); Equal(InputOutcome.Locked,i.TapObjective("primary").Outcome);
        Equal(InputOutcome.Locked,i.Cancel().Outcome); Equal(InputOutcome.Locked,i.EndTurn(x.Id).Outcome); Equal(InputOutcome.Locked,i.Advance(Seen()).Outcome);
        Equal(3,i.Read().Battle.HealthOverlays.Count); Equal(false,i.CompleteAnimation("wrong")); Equal(before,x.Bytes()); Equal(true,i.CompleteAnimation(r.AnimationId));
    }
    private static void InputOldAnimation()
    {
        var x=new SessionFixture(); var i=Input(x); i.TapDestination(P(1,0)); var first=i.TapDestination(P(1,0)); i.CompleteAnimation(first.AnimationId);
        i.TapDestination(P(2,0)); var second=i.TapDestination(P(2,0)); Equal(false,i.CompleteAnimation(first.AnimationId)); Equal(true,i.Read().IsAnimating); Equal(true,i.CompleteAnimation(second.AnimationId));
    }
    private static void InputDuplicate()
    {
        var x=new SessionFixture(); var i=Input(x); i.TapDestination(P(1,0)); string id=i.Read().Pending.Id; var r=i.Confirm(id); string before=x.Bytes();
        Equal(InputOutcome.Locked,i.Confirm(id).Outcome); i.CompleteAnimation(r.AnimationId); Equal(InputOutcome.NoPreview,i.Confirm(id).Outcome); Equal(before,x.Bytes());
    }
    private static void InputStaleCheckpoint()
    {
        var x=new SessionFixture(); var i=Input(x); i.TapDestination(P(2,0)); string id=i.Read().Pending.Id;
        x.Session.TryMove(x.Id,new[] { P(1,0) },out _,out _); string before=x.Bytes(); Equal(InputOutcome.Stale,i.Confirm(id).Outcome); Equal(before,x.Bytes()); Equal(P(1,0),Bar(i,"a").Position);
    }
    private static void InputStaleTurn()
    {
        var x=new SessionFixture(); var i=Input(x); i.SelectAbility(AbilitySlot.NormalAttack); i.TapUnit("b"); string id=i.Read().Pending.Id;
        x.ToEnemy(); string before=x.Bytes(); Equal(InputOutcome.Stale,i.Confirm(id).Outcome); Equal(before,x.Bytes());
    }
    private static void InputSameFlowReload()
    {
        var x=new SessionFixture(); var i=Input(x); i.TapDestination(P(1,0)); string id=i.Read().Pending.Id;
        x.Flow.Open(); Throws<InvalidOperationException>(()=>i.Confirm(id));
        x.Session=new BattleSession(x.Flow); i=Input(x); Equal(P(0,0),Bar(i,"a").Position); Equal(InputOutcome.NoPreview,i.Confirm(id).Outcome);
    }
    private static void InputRefresh()
    {
        var x=new SessionFixture(); var i=Input(x); i.SelectAbility(AbilitySlot.NormalAttack); i.TapUnit("b");
        x.Session.TryMove(x.Id,new[] { P(1,0) },out _,out _); Equal(false,i.Read().CanConfirm); Equal<AbilitySlot?>(null,i.Read().SelectedAbility);
    }
    private static void InputWait()
    {
        var x=new SessionFixture(); var i=Input(x); i.TapDestination(P(1,0)); string id=i.Read().Pending.Id; string before=x.Bytes();
        for(int n=0;n<5;n++) Equal(InputOutcome.Waiting,i.Advance().Outcome);
        Equal(id,i.Read().Pending.Id); Equal(before,x.Bytes());
    }
    private static void InputEnemy()
    {
        var x=new SessionFixture(); x.ToEnemy(); var i=Input(x); Equal(InputOutcome.Rejected,i.TapDestination(P(1,0)).Outcome);
        Equal(InputOutcome.Rejected,i.SelectAbility(AbilitySlot.NormalAttack).Outcome); Equal(InputOutcome.Waiting,i.Advance().Outcome);
        Equal(InputOutcome.Committed,i.Advance(Seen()).Outcome); Equal(80,Bar(i,"a").CurrentHealth); Equal<string>(null,i.Read().SelectedUnitId);
    }
    private static void InputObjective()
    {
        var x=new SessionFixture(stabilize:true); var i=Input(x); string before=x.Bytes(); Equal(InputOutcome.Previewed,i.TapObjective("primary").Outcome); Equal(before,x.Bytes());
        Equal(InputOutcome.Committed,i.TapObjective("primary").Outcome); Equal(BattleSessionPhase.Results,i.Read().Battle.Phase);
    }
    private static void InputEnd()
    {
        var x=new SessionFixture(); var i=Input(x); i.TapDestination(P(1,0)); Equal(InputOutcome.Stale,i.EndTurn(999).Outcome); Equal(true,i.Read().CanConfirm);
        Equal(InputOutcome.Committed,i.EndTurn(x.Id).Outcome); Equal(false,i.Read().CanConfirm); Equal(P(0,0),Bar(i,"a").Position);
    }
    private static void InputTorn()
    {
        var x=new SessionFixture(); var i=Input(x); i.TapDestination(P(1,0)); string id=i.Read().Pending.Id; x.Files.Fault=2;
        BattleInputResult result=null; Throws<IOException>(()=>result=i.Confirm(id)); Equal<BattleInputResult>(null,result); Equal(true,x.Flow.NeedsReload);
        x.Files.Fault=0; x.Reopen(); i=Input(x); Equal(InputOutcome.NoPreview,i.Confirm(id).Outcome); Equal(P(0,0),Bar(i,"a").Position);
    }
    private static void InputAmbiguous()
    {
        var x=new SessionFixture(); var i=Input(x); i.SelectAbility(AbilitySlot.NormalAttack); i.TapUnit("b"); string id=i.Read().Pending.Id; x.Files.Fault=3;
        Throws<IOException>(()=>i.Confirm(id)); Throws<InvalidOperationException>(()=>i.Read()); x.Files.Fault=0; x.Reopen(); i=Input(x);
        Equal(60,Bar(i,"b").CurrentHealth); Equal(InputOutcome.NoPreview,i.Confirm(id).Outcome); Equal(false,i.Read().IsAnimating);
    }
    private static void InputTerminal()
    {
        var x=new SessionFixture(stabilize:true); var i=Input(x); i.TapObjective("primary"); var r=i.TapObjective("primary"); i.CompleteAnimation(r.AnimationId);
        Equal(InputOutcome.Rejected,i.TapDestination(P(1,0)).Outcome); Equal(InputOutcome.Waiting,i.Advance().Outcome); x.Flow.ClaimRewards(); x.Flow.ReturnToSelection();
        Equal(0,i.Read().Battle.HealthOverlays.Count); Equal(InputOutcome.Rejected,i.TapUnit("a").Outcome);
    }
}
