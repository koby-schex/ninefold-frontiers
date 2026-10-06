using System;
using System.IO;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
using Ninefold.Core.Persistence;
using Ninefold.Core.Progression;
using Ninefold.Core.Views;

internal static partial class Program
{
    private static (string Name,Action Run)[] IntegrationReviewTests() => new (string,Action)[] {
        ("Integration unused session cannot adopt a later attempt", IntegrationUnusedSession),
        ("Integration selection session cannot adopt newly started battle", IntegrationSelectionSession),
        ("Integration reload invalidates direct player and enemy commands", IntegrationExpiredCommands),
        ("Integration old screen cannot command reloaded menu battle", IntegrationOldScreen),
        ("Integration full campaign reward collection upgrade and next faction journey", IntegrationJourney),
        ("Integration combat resumes spent action and completes through public input", IntegrationCombatResume),
        ("Integration real directory saves reopen battle reward and upgrades", IntegrationDirectory)
    };
    private static void IntegrationUnusedSession()
    {
        var x=new SessionFixture(stabilize:true); var unused=x.Session; var active=new BattleSession(x.Flow);
        active.TryInteract(active.Read().Activation.ActivationId,"primary",out _); x.Flow.ClaimRewards(); x.Flow.ReturnToSelection(); x.Flow.Start("session-m",new[] { "a" });
        string before=x.Bytes(); Throws<InvalidOperationException>(()=>unused.Read()); Throws<InvalidOperationException>(()=>unused.TryInteract(1,"primary",out _)); Equal(before,x.Bytes());
    }
    private static void IntegrationSelectionSession()
    {
        var f=new FlowFixture().Create(); var old=new BattleSession(f); StartFlow(f); Throws<InvalidOperationException>(()=>old.Read());
        Equal(BattleSessionPhase.PlayerInput,new BattleSession(f).Read().Phase);
    }
    private static void IntegrationExpiredCommands()
    {
        var x=new SessionFixture(); var old=x.Session; long id=x.Id; x.Flow.Open(); string before=x.Bytes();
        Throws<InvalidOperationException>(()=>old.TryMove(id,new[] { P(1,0) },out _,out _));
        Throws<InvalidOperationException>(()=>old.TryUseAbility(id,AbilitySlot.NormalAttack,"b",out _,out _,out _));
        Throws<InvalidOperationException>(()=>old.EndPlayerTurn(id)); Equal(before,x.Bytes());
        x.Session=new BattleSession(x.Flow); x.ToEnemy(); old=x.Session; x.Flow.Open(); before=x.Bytes();
        Throws<InvalidOperationException>(()=>old.Advance(Seen())); Equal(before,x.Bytes());
        Equal(BattleSessionPhase.EnemyInput,new BattleSession(x.Flow).Read().Phase);
    }
    private static void IntegrationOldScreen()
    {
        var x=new MenuFixture(); x.Prepare(); var old=x.Menu.Start(x.Menu.Read().PlanId); // No Read before reload.
        x.Menu.Open(); var current=x.Menu.ResumeBattle(); var id=current.Read().Battle.Activation.ActivationId; string before=x.Bytes();
        Throws<InvalidOperationException>(()=>old.EndTurn(id)); Throws<InvalidOperationException>(()=>old.TapDestination(P(1,0))); Equal(before,x.Bytes());
        Equal(InputOutcome.Previewed,current.TapDestination(P(1,0)).Outcome);
    }
    private static void Acknowledge(BattleInteraction b,BattleInputResult result)
    { if(result.AnimationId!=null) Equal(true,b.CompleteAnimation(result.AnimationId)); }
    private static string StabilizeThroughInput(MissionPresentation menu,BattleInteraction battle)
    {
        Equal(InputOutcome.Previewed,battle.TapObjective("primary").Outcome); var result=battle.TapObjective("primary"); Equal(InputOutcome.Committed,result.Outcome);
        Acknowledge(battle,result); return menu.Read().Results.Result.AttemptId;
    }
    private static void IntegrationJourney()
    {
        var x=new MenuFixture(); x.Prepare("fixture-opening","fixture-a-1","fixture-a-2","fixture-a-3");
        var battle=x.Menu.Start(x.Menu.Read().PlanId); Equal(3,battle.Read().Battle.HealthOverlays.Count);
        battle.TapDestination(P(0,1)); Acknowledge(battle,battle.TapDestination(P(0,1))); x.Menu.Open(); battle=x.Menu.ResumeBattle();
        Equal<string>(null,battle.Read().SelectedUnitId); Equal(4m,battle.Read().Battle.Activation.MovementRemaining);
        string attempt=StabilizeThroughInput(x.Menu,battle); x.Menu.ClaimRewards(attempt); x.Menu.ReturnToSelection(attempt);
        var c=x.Menu.Collection; c.Confirm(c.Preview(CollectionOperation.Unlock,"fixture-a-4").Id);
        c.Confirm(c.Preview(CollectionOperation.Advance,"fixture-a-1").Id); c.Confirm(c.Preview(CollectionOperation.Customize,"fixture-a-1","fixture-health").Id);
        x.Menu.Open(); x.Prepare("fixture-finale","fixture-a-1","fixture-a-2","fixture-a-4"); battle=x.Menu.Start(x.Menu.Read().PlanId);
        Equal(110,battle.Read().Battle.HealthOverlays.Single(h=>h.UnitId=="fixture-a-1").MaximumHealth);
        attempt=StabilizeThroughInput(x.Menu,battle); x.Menu.ClaimRewards(attempt); x.Menu.ReturnToSelection(attempt); x.Menu.ClaimCampaign("fixture-starter");
        x.Menu.Open(); x.Prepare("fixture-next","fixture-b-1","fixture-b-2","fixture-b-3"); Equal(true,x.Menu.Read().CanStart);
        battle=x.Menu.Start(x.Menu.Read().PlanId); attempt=StabilizeThroughInput(x.Menu,battle); x.Menu.ClaimRewards(attempt); x.Menu.ReturnToSelection(attempt);
        x.Menu.ClaimCampaign("fixture-followup"); x.Menu.Open(); var p=x.Menu.Read().Progress;
        Equal(3,p.Claims.Count); Equal(2,p.CampaignClaims.Count); Equal(true,p.Owns("fixture-a-5")); Equal(false,p.Owns("fixture-b-5"));
        Equal(1,p.GetAdvancement("fixture-a-1").Rank); Equal("fixture-health",p.GetCustomization("fixture-a-1").OptionId);
    }
    private static void IntegrationCombatResume()
    {
        var x=new MenuFixture(); x.Prepare("fixture-mixed"); var b=x.Menu.Start(x.Menu.Read().PlanId);
        b.SelectAbility(AbilitySlot.NormalAttack); b.TapUnit("fixture-enemy-1"); Acknowledge(b,b.TapUnit("fixture-enemy-1"));
        Equal(40,b.Read().Battle.HealthOverlays.Single(h=>h.UnitId=="fixture-enemy-1").CurrentHealth);
        x.Menu.Open(); b=x.Menu.ResumeBattle(); Equal(false,b.Read().Battle.Activation.PrimaryActionAvailable);
        Equal(InputOutcome.Rejected,b.SelectAbility(AbilitySlot.NormalAttack).Outcome);
        for(int step=0;step<30 && x.Menu.Read().Results==null;step++)
        {
            var v=b.Read().Battle; BattleInputResult result;
            if(v.Phase==BattleSessionPhase.PlayerInput)
            {
                if(v.Activation.PrimaryActionAvailable) { b.SelectAbility(AbilitySlot.NormalAttack); Equal(InputOutcome.Previewed,b.TapUnit("fixture-enemy-1").Outcome); result=b.TapUnit("fixture-enemy-1"); }
                else result=b.EndTurn(v.Activation.ActivationId);
            }
            else result=b.Advance(new EnemyPerception(new[] { "fixture-a-1" },Array.Empty<string>()));
            Acknowledge(b,result);
        }
        var terminal=x.Menu.Read().Results; Equal(Ninefold.Core.Missions.MissionOutcome.Victory,terminal.Result.Outcome);
        var claimed=x.Menu.ClaimRewards(terminal.Result.AttemptId); Equal(1,claimed.After.Claims.Count); x.Menu.ReturnToSelection(terminal.Result.AttemptId);
    }
    private static void IntegrationDirectory()
    {
        string directory=Path.Combine(Path.GetTempPath(),"ninefold-integration-"+Guid.NewGuid().ToString("N"));
        try
        {
            var catalog=AbstractContentPackage.Create();
            MissionPresentation Make()=>new MissionPresentation(catalog,new DirectoryBattleSaveFiles(directory),new DirectoryProgressSaveFiles(directory),"disk-profile");
            var menu=Make(); menu.CreateProfile(); menu.SelectMission("fixture-opening"); menu.SetSquad(new[] { "fixture-a-1" });
            var battle=menu.Start(menu.Read().PlanId); battle.TapDestination(P(1,0)); Acknowledge(battle,battle.TapDestination(P(1,0)));
            menu=Make(); menu.Open(); battle=menu.ResumeBattle(); Equal(4m,battle.Read().Battle.Activation.MovementRemaining);
            var attempt=StabilizeThroughInput(menu,battle); menu.ClaimRewards(attempt);
            menu=Make(); menu.Open(); Equal(MissionFlowPhase.Selection,menu.Read().Phase);
            var c=menu.Collection; c.Confirm(c.Preview(CollectionOperation.Unlock,"fixture-a-4").Id); c.Confirm(c.Preview(CollectionOperation.Advance,"fixture-a-1").Id);
            menu=Make(); menu.Open(); Equal(true,menu.Read().Progress.Owns("fixture-a-4")); Equal(1,menu.Read().Progress.GetAdvancement("fixture-a-1").Rank);
            Equal(1,menu.Read().Progress.Claims.Count); Equal(0L,menu.Read().Progress.Balance("fragment-fixture-a-1"));
            Equal(4,Directory.GetFiles(directory,"*.save").Length);
        }
        finally { if(Directory.Exists(directory)) Directory.Delete(directory,true); }
    }
}
