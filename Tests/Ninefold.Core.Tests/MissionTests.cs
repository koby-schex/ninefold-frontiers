using System;
using System.Collections.Generic;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;

internal static partial class Program
{
    private static (string Name, Action Run)[] MissionTests() => new (string, Action)[]
    {
        ("Designated defeat finishes immediately with one result", MissionDefeat),
        ("Alive extraction is not a kill", MissionRemoval),
        ("Primary stabilization succeeds with enemies remaining", MissionStabilize),
        ("Optional failure cannot defeat a primary success", MissionOptionalFailure),
        ("Optional completion cannot end primary mission", MissionOptionalSuccess),
        ("Protect requires target alive across resolved rounds", MissionProtect),
        ("Protected target loss defeats mission", MissionProtectLoss),
        ("Survive grants progress once per completed round", MissionSurvive),
        ("Round gate forbids skipping resolution", MissionRoundGate),
        ("Active stale and future round checks cannot grant progress", MissionBadRound),
        ("Rescue requires interaction before full-footprint extraction", MissionRescue),
        ("Rescued target loss fails before extraction", MissionRescueLoss),
        ("Secure awards uncontested full-body presence", MissionSecure),
        ("Contest pauses cumulative hold and resets consecutive hold", MissionContest),
        ("Interaction preview spends nothing and keeps snapshots immutable", MissionPreview),
        ("Ineligible stale unknown and spent interactions preserve costs", MissionInvalidInteraction),
        ("Interaction reach and blockers reject before spending", MissionInteractionGeometry),
        ("Interaction preserves movement cooldown and Signature readiness", MissionInteractionResources),
        ("Interaction confirms current position rather than old preview", MissionInteractionRevalidation),
        ("Failure-first simultaneous outcome is explicit", MissionFailureFirst),
        ("Success-first simultaneous outcome is explicit", MissionSuccessFirst),
        ("Squad loss ends run without deleting health records", MissionSquadLoss),
        ("Result and optional snapshots stay immutable after battle", MissionResultSnapshot),
        ("Invalid mission setup cannot partially attach", MissionInvalidSetup),
        ("Objective definitions copy inputs and reject ambiguity", MissionDefinitions),
        ("Externally closed battle invents no mission result", MissionExternalEnd)
    };
    private static InteractionDefinition InteractAt(decimal x = 0m, decimal z = 0m, decimal reach = 1m, string actor = "a")
        => new InteractionDefinition(P(x,z,1),reach,new[] { actor });
    private static BattleTurnController MissionBattle(ObjectiveDefinition primary, ObjectiveDefinition[] optional = null,
        OutcomePriority priority = OutcomePriority.FailureFirst, FieldObstacle[] walls = null, string[] squad = null)
    {
        var b = FieldBattle(walls,start:false);
        b.ConfigureMission(new MissionDefinition("test-mission","test-attempt",squad ?? new[] { "a","c" },primary,priority,optional));
        b.StartNextRound(); b.BeginNextActivation(); return b;
    }
    private static void FinishMissionRound(BattleTurnController b)
    {
        if (b.CurrentActivation != null) b.EndActivation(b.CurrentActivation.ActivationId);
        Drain(b); b.Mission.ResolveRoundEnd(b.RoundNumber);
    }
    private static void NextMissionRound(BattleTurnController b)
    { FinishMissionRound(b); b.StartNextRound(); b.BeginNextActivation(); }
    private static void Interact(BattleTurnController b, string id)
    {
        Equal(true,b.Mission.TryInteract(b.CurrentActivation.ActivationId,id,out var failure)); Equal(InteractionFailure.None,failure);
    }
    private static void MissionDefeat()
    {
        var b = MissionBattle(ObjectiveDefinition.Defeat("primary",new[] { "b" }));
        FieldHit(b,AbilitySlot.Signature); var result = b.Mission.Result;
        Equal(MissionOutcome.Victory,result.Outcome); Equal(true,b.IsBattleEnded); Equal<ActivationView>(null,b.CurrentActivation);
        Equal(true,ReferenceEquals(result,b.Mission.Evaluate())); Equal(1,result.Objectives[0].Progress);
        Equal(false,b.Battlefield.TryMoveTo(1,P(1,0),out _,out _));
    }
    private static void MissionRemoval()
    {
        var b = MissionBattle(ObjectiveDefinition.Defeat("primary",new[] { "b" }));
        b.RemoveUnit("b"); b.Mission.Evaluate(); Equal(MissionOutcome.Defeat,b.Mission.Result.Outcome);
        Equal(0,b.Mission.Result.Objectives[0].Progress); Equal(false,b.Health.GetState("b").IsDefeated);
    }
    private static void MissionStabilize()
    {
        var b = MissionBattle(ObjectiveDefinition.Stabilize("primary",1,InteractAt()));
        Interact(b,"primary"); Equal(MissionOutcome.Victory,b.Mission.Result.Outcome); Equal(true,b.IsUnitEligible("b"));
    }
    private static void MissionOptionalFailure()
    {
        var b = MissionBattle(ObjectiveDefinition.Stabilize("primary",1,InteractAt()),new[] { ObjectiveDefinition.Protect("bonus","c",2) });
        b.RemoveUnit("c"); Equal<BattleResult>(null,b.Mission.Evaluate());
        Interact(b,"primary"); Equal(MissionOutcome.Victory,b.Mission.Result.Outcome); Equal(ObjectiveStatus.Failed,b.Mission.GetObjective("bonus").Status);
    }
    private static void MissionOptionalSuccess()
    {
        var b = MissionBattle(ObjectiveDefinition.Survive("primary",3),new[] { ObjectiveDefinition.Stabilize("bonus",1,InteractAt()) });
        Interact(b,"bonus"); Equal<BattleResult>(null,b.Mission.Result); Equal(false,b.IsBattleEnded);
        Equal(ObjectiveStatus.Completed,b.Mission.GetObjective("bonus").Status);
    }
    private static void MissionProtect()
    {
        var b = MissionBattle(ObjectiveDefinition.Protect("primary","c",2));
        NextMissionRound(b); Equal(1,b.Mission.GetObjective("primary").Progress);
        FinishMissionRound(b); Equal(MissionOutcome.Victory,b.Mission.Result.Outcome);
    }
    private static void MissionProtectLoss()
    {
        var b = MissionBattle(ObjectiveDefinition.Protect("primary","c",1)); b.RemoveUnit("c");
        FinishMissionRound(b); Equal(MissionOutcome.Defeat,b.Mission.Result.Outcome); Equal(0,b.Mission.GetObjective("primary").Progress);
    }
    private static void MissionSurvive()
    {
        var b = MissionBattle(ObjectiveDefinition.Survive("primary",2));
        FinishMissionRound(b); b.Mission.ResolveRoundEnd(1); b.Mission.Evaluate(); Equal(1,b.Mission.GetObjective("primary").Progress);
        b.StartNextRound(); b.BeginNextActivation(); FinishMissionRound(b); Equal(MissionOutcome.Victory,b.Mission.Result.Outcome);
    }
    private static void MissionRoundGate()
    {
        var b = MissionBattle(ObjectiveDefinition.Survive("primary",3)); b.EndActivation(b.CurrentActivation.ActivationId); Drain(b);
        Throws<InvalidOperationException>(() => b.StartNextRound()); Equal(1,b.RoundNumber);
        b.Mission.ResolveRoundEnd(1); b.StartNextRound(); Equal(2,b.RoundNumber);
    }
    private static void MissionBadRound()
    {
        var b = MissionBattle(ObjectiveDefinition.Survive("primary",3));
        Throws<InvalidOperationException>(() => b.Mission.ResolveRoundEnd(1));
        b.EndActivation(b.CurrentActivation.ActivationId); Drain(b);
        Throws<InvalidOperationException>(() => b.Mission.ResolveRoundEnd(0)); Throws<InvalidOperationException>(() => b.Mission.ResolveRoundEnd(2));
        Equal(0,b.Mission.GetObjective("primary").Progress);
    }
    private static void MissionRescue()
    {
        var exit = Box(-1,5,1,7); var b = MissionBattle(ObjectiveDefinition.Rescue("primary","c",exit,InteractAt()));
        Equal<BattleResult>(null,b.Mission.Evaluate()); // Already inside exit still needs rescue.
        Interact(b,"primary"); Equal(MissionOutcome.Victory,b.Mission.Result.Outcome);
        var move = MissionBattle(ObjectiveDefinition.Rescue("primary","c",Box(-1,7,1,9),InteractAt()));
        Interact(move,"primary"); Equal<BattleResult>(null,move.Mission.Result);
        move.EndActivation(move.CurrentActivation.ActivationId); var enemy = move.BeginNextActivation(); move.EndActivation(enemy.ActivationId);
        var traveler = move.BeginNextActivation(); Equal("c",traveler.UnitId);
        Move(move,P(0,7)); Equal<BattleResult>(null,move.Mission.Result); // Center alone is insufficient.
        Move(move,P(0,8)); Equal(MissionOutcome.Victory,move.Mission.Result.Outcome);
    }
    private static void MissionRescueLoss()
    {
        var b = MissionBattle(ObjectiveDefinition.Rescue("primary","c",Box(-1,7,1,9),InteractAt()));
        Interact(b,"primary"); b.RemoveUnit("c"); b.Mission.Evaluate(); Equal(MissionOutcome.Defeat,b.Mission.Result.Outcome);
    }
    private static ObjectiveDefinition Hold(bool consecutive = false)
        => ObjectiveDefinition.Secure("primary",Box(-1,-1,1,1),new[] { "a" },new[] { "b" },2,consecutive);
    private static void MissionSecure()
    {
        var b = MissionBattle(Hold()); NextMissionRound(b); Equal(1,b.Mission.GetObjective("primary").Progress);
        FinishMissionRound(b); Equal(MissionOutcome.Victory,b.Mission.Result.Outcome);
    }
    private static void MissionContest()
    {
        foreach (bool consecutive in new[] { false,true })
        {
            var b = MissionBattle(Hold(consecutive)); NextMissionRound(b);
            b.EndActivation(b.CurrentActivation.ActivationId); b.BeginNextActivation(); Move(b,P(1,0));
            FinishMissionRound(b); Equal(consecutive ? 0 : 1,b.Mission.GetObjective("primary").Progress);
            Equal<BattleResult>(null,b.Mission.Result);
        }
    }
    private static void MissionPreview()
    {
        var b = MissionBattle(ObjectiveDefinition.Stabilize("primary",2,InteractAt())); var before = b.Mission.GetObjective("primary");
        for (int i=0;i<4;i++) Equal(InteractionFailure.None,b.Mission.PreviewInteraction(1,"primary"));
        Equal(true,b.CurrentActivation.PrimaryActionAvailable); Equal(0,before.Progress);
        Interact(b,"primary"); Equal(0,before.Progress); Equal(1,b.Mission.GetObjective("primary").Progress);
    }
    private static void MissionInvalidInteraction()
    {
        var b = MissionBattle(ObjectiveDefinition.Stabilize("primary",2,InteractAt(actor:"c")));
        Equal(false,b.Mission.TryInteract(1,"primary",out var f)); Equal(InteractionFailure.IneligibleActor,f); Equal(true,b.CurrentActivation.PrimaryActionAvailable);
        Equal(false,b.Mission.TryInteract(99,"primary",out f)); Equal(InteractionFailure.InactiveTurn,f);
        Equal(false,b.Mission.TryInteract(1,"unknown",out f)); Equal(InteractionFailure.UnknownObjective,f);
        var spent = MissionBattle(ObjectiveDefinition.Stabilize("primary",2,InteractAt())); Interact(spent,"primary");
        Equal(false,spent.Mission.TryInteract(1,"primary",out f)); Equal(InteractionFailure.ActionSpent,f); Equal(1,spent.Mission.GetObjective("primary").Progress);
    }
    private static void MissionInteractionGeometry()
    {
        var b = MissionBattle(ObjectiveDefinition.Stabilize("primary",1,InteractAt(4,0,1)));
        Equal(false,b.Mission.TryInteract(1,"primary",out var f)); Equal(InteractionFailure.OutOfReach,f); Equal(true,b.CurrentActivation.PrimaryActionAvailable);
        var wall = MissionBattle(ObjectiveDefinition.Stabilize("primary",1,InteractAt(4,0,5)),walls:new[] { Wall() });
        Equal(false,wall.Mission.TryInteract(1,"primary",out f)); Equal(InteractionFailure.Obstructed,f); Equal(true,wall.CurrentActivation.PrimaryActionAvailable);
    }
    private static void MissionInteractionResources()
    {
        var fresh = B(U("a")); fresh.Abilities.RegisterKit("a",new UnitAbilityDefinition(2,SignatureReadiness.AfterMainAbility));
        fresh.Health.RegisterHealth("a",new UnitHealthDefinition("team",100,0));
        fresh.ConfigureBattlefield(new BattlefieldMap(Box(-10,-10,10,10),Array.Empty<FieldObstacle>())).Register("a",P(0,0),Body(),Profiles());
        fresh.ConfigureMission(new MissionDefinition("m","attempt",new[] { "a" },ObjectiveDefinition.Stabilize("primary",2,InteractAt()),OutcomePriority.FailureFirst));
        fresh.StartNextRound(); fresh.BeginNextActivation(); Move(fresh,P(.5m,0)); Interact(fresh,"primary"); Move(fresh,P(1,0));
        Equal(4m,fresh.CurrentActivation.MovementRemaining); Equal(0,fresh.Abilities.GetState("a").MainCooldownRemaining);
        Equal(false,fresh.Abilities.GetState("a").SignatureRequirementMet); Equal(false,fresh.Abilities.GetState("a").SignatureUsed);
    }
    private static void MissionInteractionRevalidation()
    {
        var b = MissionBattle(ObjectiveDefinition.Stabilize("primary",1,InteractAt())); Equal(InteractionFailure.None,b.Mission.PreviewInteraction(1,"primary"));
        Move(b,P(2,0)); Equal(false,b.Mission.TryInteract(1,"primary",out var f)); Equal(InteractionFailure.OutOfReach,f);
        Equal(true,b.CurrentActivation.PrimaryActionAvailable); Equal(0,b.Mission.GetObjective("primary").Progress);
    }
    private static BattleTurnController Simultaneous(OutcomePriority priority)
    {
        var b = MissionBattle(ObjectiveDefinition.Defeat("primary",new[] { "b" }),priority:priority,squad:new[] { "a" });
        b.RemoveUnit("a"); var enemy = b.BeginNextActivation(); b.EndActivation(enemy.ActivationId); b.BeginNextActivation();
        FieldHit(b,AbilitySlot.Signature); return b;
    }
    private static void MissionFailureFirst() { Equal(MissionOutcome.Defeat,Simultaneous(OutcomePriority.FailureFirst).Mission.Result.Outcome); }
    private static void MissionSuccessFirst() { Equal(MissionOutcome.Victory,Simultaneous(OutcomePriority.SuccessFirst).Mission.Result.Outcome); }
    private static void MissionSquadLoss()
    {
        var b = MissionBattle(ObjectiveDefinition.Survive("primary",3)); b.RemoveUnit("a"); b.RemoveUnit("c");
        b.Mission.Evaluate(); Equal(MissionOutcome.Defeat,b.Mission.Result.Outcome); Equal("SquadUnavailable",b.Mission.Result.Reason);
        Equal(100,b.Health.GetState("a").CurrentHealth); Equal(50,b.Health.GetState("c").CurrentHealth);
    }
    private static void MissionResultSnapshot()
    {
        var b = MissionBattle(ObjectiveDefinition.Stabilize("primary",1,InteractAt()),new[] { ObjectiveDefinition.Survive("bonus",4) });
        Interact(b,"primary"); var result = b.Mission.Result;
        Equal("test-attempt",result.AttemptId); Equal("test-mission",result.MissionId); Equal(1,result.Round);
        Equal(false,b.Mission.TryInteract(1,"primary",out var f)); Equal(InteractionFailure.MissionClosed,f);
        Equal(true,ReferenceEquals(result,b.Mission.ResolveRoundEnd(1))); Equal(0,result.Objectives[1].Progress);
        Throws<NotSupportedException>(() => ((IList<ObjectiveView>)result.Objectives)[0] = null);
    }
    private static void MissionInvalidSetup()
    {
        var b = FieldBattle(start:false);
        Throws<ArgumentException>(() => b.ConfigureMission(new MissionDefinition("m","a",new[] { "unknown" },ObjectiveDefinition.Survive("primary",1),OutcomePriority.FailureFirst)));
        Equal<MissionController>(null,b.Mission);
        var definition = new MissionDefinition("m","a",new[] { "a" },ObjectiveDefinition.Survive("primary",1),OutcomePriority.FailureFirst);
        b.ConfigureMission(definition); Throws<InvalidOperationException>(() => b.ConfigureMission(definition));
        var late = FieldBattle(); Throws<InvalidOperationException>(() => late.ConfigureMission(definition));
    }
    private static void MissionDefinitions()
    {
        var actors = new[] { "a" }; var interaction = new InteractionDefinition(P(0,0),1,actors); actors[0] = "b"; Equal("a",interaction.AllowedActors[0]);
        Throws<ArgumentException>(() => ObjectiveDefinition.Defeat("p",Array.Empty<string>()));
        Throws<ArgumentOutOfRangeException>(() => ObjectiveDefinition.Survive("p",0));
        Throws<ArgumentException>(() => new MissionDefinition("m","a",new[] { "a" },ObjectiveDefinition.Survive("p",1),OutcomePriority.FailureFirst,new[] { ObjectiveDefinition.Survive("p",2) }));
        Throws<ArgumentException>(() => new MissionDefinition("m","a",new[] { "a" },ObjectiveDefinition.Survive("p",1),OutcomePriority.FailureFirst,new[] { ObjectiveDefinition.Survive("1",2),ObjectiveDefinition.Survive("2",2),ObjectiveDefinition.Survive("3",2) }));
        Throws<ArgumentOutOfRangeException>(() => new MissionDefinition("m","a",new[] { "a" },ObjectiveDefinition.Survive("p",1),(OutcomePriority)99));
    }
    private static void MissionExternalEnd()
    {
        var b = MissionBattle(ObjectiveDefinition.Survive("primary",1)); b.EndBattle();
        Equal<BattleResult>(null,b.Mission.Evaluate()); Throws<InvalidOperationException>(() => b.Mission.ResolveRoundEnd(1));
    }
}
