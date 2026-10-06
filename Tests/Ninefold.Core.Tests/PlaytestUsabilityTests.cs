using System;
using System.IO;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
using Ninefold.Core.Views;

internal static partial class Program
{
    private static (string Name, Action Run)[] PlaytestUsabilityTests() => new (string, Action)[] {
        ("Reachable path truncates a distant click without spending preview resources", ReachableStraight),
        ("Reachable path retains a shorter requested destination", ReachableShort),
        ("Reachable path respects exact difficult-ground cost", ReachableTerrain),
        ("Reachable path follows a legal obstacle detour", ReachableWall),
        ("Reachable path cannot escape bounds or approach an illegal occupied destination", ReachableIllegal),
        ("Reachable path respects zero budget and stale activation", ReachableInactive),
        ("Reachable routing does not cross disconnected terrain", ReachableDisconnected),
        ("Repeated distant tap confirms one affordable saved movement", ReachableInput),
        ("Ability selection exposes target verdicts without save mutation", SelectedTargets),
        ("Playtest maps have distinct geometry and all deployable missions validate", DistinctPlaytestMaps),
        ("Playtest layout upgrade preserves ongoing battle and earned progression", PreservePlaytestProfile),
        ("Playtest layout upgrade fails closed for missing or corrupt profiles", RejectBrokenPlaytestProfile),
        ("Planning samples are affordable legal destinations and do not save", PlanningSamples),
        ("Planning cache updates after movement and is hidden during playback", PlanningInvalidation),
        ("Planning is absent during enemy turns", PlanningEnemy),
        ("Objective readiness follows real action and reach rules", PlanningObjectives),
        ("Planning samples respect authored map obstacles", PlanningTerrain)
    };
    private static MovementPreview Reachable(BattleTurnController b, FieldPoint goal)
    {
        var before=b.CurrentActivation; var start=b.Battlefield.GetPosition(before.UnitId);
        Equal(true,b.Battlefield.TryFindReachablePath(before.ActivationId,goal,out var route,out var failure));
        Equal(FieldFailure.None,failure); Equal(start,b.Battlefield.GetPosition(before.UnitId));
        Equal(before.MovementRemaining,b.CurrentActivation.MovementRemaining);
        Equal(true,b.Battlefield.TryPreviewMovement(before.ActivationId,route.Path,out var checkedRoute,out _));
        Equal(route.Cost,checkedRoute.Cost); Equal(true,route.Cost<=before.MovementRemaining); return route;
    }
    private static void ReachableStraight()
    {
        var b=FieldBattle(budget:3); var p=Reachable(b,P(5,0)); Equal(P(3,0),p.Path.Last()); Equal(3m,p.Cost);
        Equal(true,b.Battlefield.TryMove(b.CurrentActivation.ActivationId,p.Path,out _,out _));
        Equal(0m,b.CurrentActivation.MovementRemaining); Equal(true,b.CurrentActivation.PrimaryActionAvailable);
    }
    private static void ReachableShort() { var p=Reachable(FieldBattle(budget:3),P(1.123456m,0)); Equal(P(1.123456m,0),p.Path.Last()); Equal(1.123456m,p.Cost); }
    private static void ReachableTerrain()
    {
        var p=Reachable(FieldBattle(ground:new[]{new DifficultGround(Box(-20,-20,20,20),2)},budget:5),P(5,0));
        Equal(P(2.5m,0),p.Path.Last()); Equal(5m,p.Cost);
    }
    private static void ReachableWall()
    {
        var b=FieldBattle(new[]{Wall()},budget:3); var p=Reachable(b,P(4,0));
        Equal(true,p.Cost>2.99m); Equal(false,p.Path.Last().Equals(P(4,0))); Equal(true,p.Path.Count>1);
    }
    private static void ReachableIllegal()
    {
        var b=FieldBattle(budget:3);
        foreach(var goal in new[]{P(20,0),P(6,0)}) {
            Equal(false,b.Battlefield.TryFindReachablePath(b.CurrentActivation.ActivationId,goal,out _,out var f)); Equal(FieldFailure.IllegalDestination,f);
        }
        Equal(P(0,0),b.Battlefield.GetPosition("a")); Equal(3m,b.CurrentActivation.MovementRemaining);
    }
    private static void ReachableInactive()
    {
        var b=FieldBattle(budget:0); Equal(false,b.Battlefield.TryFindReachablePath(b.CurrentActivation.ActivationId,P(1,0),out _,out var f)); Equal(FieldFailure.InsufficientMovement,f);
        Equal(false,b.Battlefield.TryFindReachablePath(999,P(1,0),out _,out f)); Equal(FieldFailure.InactiveTurn,f);
    }
    private static void ReachableDisconnected()
    {
        var b=FieldBattle(new[]{new FieldObstacle(Box(2,-20,3,20),true,true)},budget:1);
        Equal(false,b.Battlefield.TryFindReachablePath(b.CurrentActivation.ActivationId,P(5,0),out _,out var f)); Equal(FieldFailure.PathNotFound,f);
    }
    private static void ReachableInput()
    {
        var x=new SessionFixture(); var i=Input(x); string before=x.Bytes(); var goal=P(-9,0);
        Equal(InputOutcome.Previewed,i.TapDestination(goal).Outcome); Equal(before,x.Bytes()); Equal(goal,i.Read().Pending.RequestedDestination.Value);
        Equal(InputOutcome.Committed,i.TapDestination(goal).Outcome); Equal(P(-5,0),i.Read().Battle.Units.Single(u=>u.Id=="a").Position.Value);
        Equal(InputOutcome.Locked,i.TapDestination(goal).Outcome);
        x.Reopen(); Equal(P(-5,0),Input(x).Read().Battle.Units.Single(u=>u.Id=="a").Position.Value);
    }
    private static void SelectedTargets()
    {
        var x=new SessionFixture(); var i=Input(x); string before=x.Bytes(); i.SelectAbility(AbilitySlot.NormalAttack);
        Equal(true,i.Read().Targets.Count>0); Equal(true,i.Read().Targets.Any(t=>t.IsValid));
        Equal(before,x.Bytes()); i.Cancel(); Equal(0,i.Read().Targets.Count);
    }
    private static void DistinctPlaytestMaps()
    {
        var c=PlaytestContentPackage.Create(); Equal(4,c.Missions.Count);
        string Signature(BattlefieldMap m)=>m.Bounds.Min.X+","+m.Bounds.Min.Z+","+m.Bounds.Max.X+","+m.Bounds.Max.Z+"/"+string.Join(";",m.Obstacles.Select(o=>o.Bounds.Min.X+","+o.Bounds.Min.Z+","+o.Bounds.Max.X+","+o.Bounds.Max.Z));
        Equal(4,c.Missions.Values.Select(m=>Signature(m.Map)).Distinct().Count());
        Equal(false,c.Manifest.SaveRevision==AbstractContentPackage.Create().Manifest.SaveRevision);
    }
    private static void PreservePlaytestProfile()
    {
        var oldFiles=new FakeSaveFiles(); var newFiles=new FakeSaveFiles(); var progress=new ProgressFiles();
        var menu=new MissionPresentation(AbstractContentPackage.Create(),oldFiles,progress,PlaytestProfile.ProfileId);
        menu.CreateProfile(); menu.SelectMission("fixture-opening"); menu.SetSquad(new[]{"fixture-a-1"}); menu.Start(menu.Read().PlanId);
        var opened=PlaytestProfile.Open(oldFiles,newFiles,progress); Equal(true,opened.IsLegacy);
        var input=opened.Menu.ResumeBattle(); input.TapObjective("primary"); var win=input.TapObjective("primary"); input.CompleteAnimation(win.AnimationId);
        var attempt=opened.Menu.Read().Results.Result.AttemptId; opened.Menu.ClaimRewards(attempt);
        var upgraded=PlaytestProfile.Open(oldFiles,newFiles,progress); Equal(false,upgraded.IsLegacy);
        Equal(10L,upgraded.Menu.Read().Progress.Balance("fixture-resource")); Equal(true,upgraded.Menu.Read().Progress.Owns("fixture-a-1"));
        Equal(1,upgraded.Menu.Read().Progress.Claims.Count);
        upgraded.Menu.SelectMission("fixture-mixed"); upgraded.Menu.SetSquad(new[]{"fixture-a-1"}); upgraded.Menu.Start(upgraded.Menu.Read().PlanId);
        var again=PlaytestProfile.Open(oldFiles,newFiles,progress); Equal(false,again.IsLegacy); Equal(MissionFlowPhase.Battle,again.Menu.Read().Phase);
        Equal(10L,again.Menu.Read().Progress.Balance("fixture-resource"));
    }
    private static void RejectBrokenPlaytestProfile()
    {
        var oldFiles=new FakeSaveFiles(); var current=new FakeSaveFiles(); var progress=new ProgressFiles();
        oldFiles.Slots[0]=new byte[]{1}; Throws<InvalidOperationException>(()=>PlaytestProfile.Open(oldFiles,current,progress));
        oldFiles.Slots[0]=null; progress.Inner.Slots[0]=new byte[]{1};
        Throws<InvalidDataException>(()=>PlaytestProfile.Open(oldFiles,current,progress)); Equal(1,progress.Inner.Slots[0].Length);
    }
    private static void PlanningSamples()
    {
        var x=new SessionFixture(); var input=Input(x); string bytes=x.Bytes();
        var plan=input.ReadPlanning(); Equal(true,plan.Movement.Count>0); Equal(true,plan.Movement.Count<=168);
        Equal(true,ReferenceEquals(plan,input.ReadPlanning()));
        var presentation=new BattlePresentation(x.Session);
        foreach(var point in plan.Movement)
        {
            Equal(true,presentation.TryPreviewDestination(x.Id,point.Position,out var path,out _));
            Equal(path.Cost,point.Cost); Equal(true,point.Cost<=5);
        }
        Equal(bytes,x.Bytes()); Equal(5m,input.Read().Battle.Activation.MovementRemaining);
    }
    private static void PlanningInvalidation()
    {
        var x=new SessionFixture(); var input=Input(x); var before=input.ReadPlanning();
        input.TapDestination(P(-5,0)); var move=input.TapDestination(P(-5,0));
        Equal(InputOutcome.Committed,move.Outcome); Equal(0,input.ReadPlanning().Movement.Count);
        input.CompleteAnimation(move.AnimationId); var after=input.ReadPlanning();
        Equal(false,ReferenceEquals(before,after)); Equal(0,after.Movement.Count);
        x.Reopen(); Equal(0,Input(x).ReadPlanning().Movement.Count);
    }
    private static void PlanningEnemy()
    {
        var x=new SessionFixture(); x.ToEnemy(); var plan=Input(x).ReadPlanning();
        Equal(0,plan.Movement.Count); Equal(0,plan.Objectives.Count);
    }
    private static void PlanningObjectives()
    {
        var x=new SessionFixture(stabilize:true); var input=Input(x);
        var presentation=new BattlePresentation(x.Session);
        var objective=input.ReadPlanning().Objectives.Single();
        Equal(presentation.PreviewInteraction(x.Id,objective.Id),objective.Failure);
        Equal(true,objective.CanInteract);
        input.SelectAbility(AbilitySlot.NormalAttack); input.TapUnit("b"); var attack=input.TapUnit("b");
        Equal(InputOutcome.Committed,attack.Outcome); input.CompleteAnimation(attack.AnimationId);
        Equal(Ninefold.Core.Missions.InteractionFailure.ActionSpent,input.ReadPlanning().Objectives.Single().Failure);
        var other=new SessionFixture(stabilize:true); var moved=Input(other);
        moved.TapDestination(P(-3,0)); var result=moved.TapDestination(P(-3,0)); moved.CompleteAnimation(result.AnimationId);
        Equal(Ninefold.Core.Missions.InteractionFailure.OutOfReach,moved.ReadPlanning().Objectives.Single().Failure);
    }
    private static void PlanningTerrain()
    {
        var catalog=PlaytestContentPackage.Create();
        var menu=new MissionPresentation(catalog,new FakeSaveFiles(),new ProgressFiles(),"planning-test");
        menu.CreateProfile(); menu.SelectMission("fixture-mixed"); menu.SetSquad(new[]{"fixture-a-1"});
        var input=menu.Start(menu.Read().PlanId); var view=input.Read().Battle;
        Equal(true,input.ReadPlanning().Movement.Count>0);
        foreach(var sample in input.ReadPlanning().Movement)
        {
            Equal(InputOutcome.Previewed,input.TapDestination(sample.Position).Outcome);
            Equal(sample.Position,input.Read().Pending.Movement.Path.Last());
            Equal(sample.Cost,input.Read().Pending.Movement.Cost); input.Cancel();
        }
        Equal(5m,input.Read().Battle.Activation.MovementRemaining);
    }
}
