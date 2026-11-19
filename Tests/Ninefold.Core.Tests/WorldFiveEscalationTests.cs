using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Content;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Persistence;
using Ninefold.Core.Progression;
using Ninefold.Core.Views;
using W=Ninefold.Core.Content.WorldFiveEscalation;

internal static partial class Program
{
    private static IEnumerable<(string Name,Action Run)> WorldFiveEscalationTests()
    {
        yield return ("World Five 60-mission continuation preserves all existing IDs, rules and save revision",EscalationCompatibility);
        yield return ("All twenty-six new maps and required stages have connected footprints, distinct narrative layouts and matching art",EscalationAudit);
        yield return ("Five optional patrols vary safely across 240 seeds with unchanged opposition",EscalationPatrols);
        yield return ("Sequential required objectives gate interactions, don't count locked rounds, and persist stage completion",EscalationStageRules);
        yield return ("Every new stage is connected to the real campaign flow, supports reload and retry, with no premature Apex reward",EscalationFlow);
        yield return ("Mission 41 completes both actual sequential route markers via legal player commands and reload",EscalationRealJourney);
    }
    private static readonly Lazy<ContentCatalog> EscalationCatalog=new Lazy<ContentCatalog>(W.Create);
    private static ContentCatalog Escalation()=>EscalationCatalog.Value;
    private static void EscalationCompatibility()
    {
        var old=WorldFiveRouteRecovery.Create();var c=Escalation();
        Equal(old.Manifest.SaveRevision,c.Manifest.SaveRevision);
        Equal(60,c.Campaigns.Single().Missions.Count);
        Equal(true,old.Campaigns.Single().Missions.Select(m=>m.MissionId)
            .SequenceEqual(c.Campaigns.Single().Missions.Take(34).Select(m=>m.MissionId)));
        foreach(var m in old.Missions.Values)
        {
            var q=c.Missions[m.Id];
            Equal(TerrainMapSignature(m.Map),TerrainMapSignature(q.Map));
            Equal(m.ProceduralCover,q.ProceduralCover);Equal(m.Objectives.Count,q.Objectives.Count);
            Equal(true,m.Deployment.SequenceEqual(q.Deployment));
            Equal(true,m.Actors.Select(a=>a.Id+":"+a.UnitId).SequenceEqual(q.Actors.Select(a=>a.Id+":"+a.UnitId)));
            Equal(old.Rewards[m.Id].Revision,c.Rewards[m.Id].Revision);
        }
        Equal(true,old.Units.Keys.SequenceEqual(c.Units.Keys));Equal(true,old.StarterUnits.SequenceEqual(c.StarterUnits));
        var files=new FakeSaveFiles();var progress=new ProgressFiles();
        string profile=WorldFiveTutorialWildlifeV1.ProfileId;
        var history=new LocalProgressStore(progress,profile);history.Create(old.StarterUnits);SeedRecoveryHistory(history,old,33);
        var prior=old.CreateFlow(files,progress,profile);prior.Open();prior.Start("w5-story-034",old.StarterUnits);
        var attempt=prior.ReadBattle().Mission.Definition.AttemptId;
        var next=c.CreateFlow(files,progress,profile);next.Open();
        Equal(MissionFlowPhase.Battle,next.Phase);Equal(attempt,next.ReadBattle().Mission.Definition.AttemptId);
        Equal(33,next.Progress.Missions.Count);
    }
    private static void EscalationAudit()
    {
        var c=Escalation();var bodies=ArtBodies(c);
        var report=CampaignProductionAudit.Review(c,c.Campaigns.Single().Id,environmentPrefix:"study-w5-");
        Equal(60,report.MissionCount);Equal(6,report.DistinctPrimaryObjectiveCount);
        Equal(26,W.All.Select(x=>TerrainMapSignature(x.Mission.Map)).Distinct().Count());
        Equal(26,W.All.Select(x=>x.Title).Distinct().Count());
        Equal(8,W.All.Count(x=>x.Mission.Objectives.Any(o=>o.IsFollowupStage)));
        Equal(5,W.All.Count(x=>x.Mission.ProceduralCover));
        foreach(var ch in W.All)
        {
            var m=ch.Mission;Equal(4,m.MaximumSquad);Equal(2,m.MinimumSquad);
            Equal(true,WorldFiveRouteRecovery.TryGet(m.Id,out var found));Equal(ch,found);
            Equal(ch.Mood,BattlefieldArtPlan.Choose(m.Id,m.EnvironmentKey));
            Equal("Act Three",WorldFiveStorySequence.StoryAct(m.Id));
            RecoveryReachability(m,m.Map,bodies);
            CheckArtGeometry(m.Map,BattlefieldArtPlan.Build(m.Map,m.Id,m.EnvironmentKey,ArtAttempt(9),m.ProceduralCover));
            foreach(var actor in m.Actors.Where(a=>a.Behavior?.Style==EnemyStyle.Defensive))
            {
                var b=c.Units[actor.UnitId].Body;var p=actor.Position;
                var footprint=new FieldBox(new FieldPoint(p.X-b.HalfWidth,p.Y,p.Z-b.HalfDepth),
                    new FieldPoint(p.X+b.HalfWidth,p.Y+b.Height,p.Z+b.HalfDepth));
                var guard=actor.Behavior.GuardArea;
                Equal(true,guard.Min.X<=footprint.Min.X&&guard.Min.Y<=footprint.Min.Y&&guard.Min.Z<=footprint.Min.Z&&
                    guard.Max.X>=footprint.Max.X&&guard.Max.Y>=footprint.Max.Y&&guard.Max.Z>=footprint.Max.Z);
                Equal(true,actor.Behavior.GuardArea.Max.X-actor.Behavior.GuardArea.Min.X<=10m);
            }
            Equal(false,ch.Rewards.FirstClear.Any(g=>g.ResourceId=="fragment-fixture-a-5"));
        }
        Console.WriteLine("INFO escalation: "+report.DistinctLayoutCount+" distinct full campaign layouts; 8 sequential encounters.");
    }
    private static void EscalationPatrols()
    {
        var c=Escalation();var bodies=ArtBodies(c);
        foreach(var ch in W.All.Where(x=>x.Mission.ProceduralCover))
        {
            var m=ch.Mission;var variants=new HashSet<string>();
            for(int i=1;i<=48;i++)
            {
                var map=BattlefieldVariation.Resolve(m,ArtAttempt(i),bodies);
                Equal(TerrainMapSignature(map),TerrainMapSignature(BattlefieldVariation.Resolve(m,ArtAttempt(i),bodies)));
                Equal(m.Map.Bounds,map.Bounds);Equal(m.Map.Obstacles.Count,map.Obstacles.Count);
                for(int j=0;j<map.Obstacles.Count-1;j++)Equal(m.Map.Obstacles[j],map.Obstacles[j]);
                RecoveryReachability(m,map,bodies);variants.Add(TerrainMapSignature(map));
            }
            if(variants.Count<5)throw new Exception(m.Id+" insufficient safe generation "+variants.Count);
            Console.WriteLine("INFO escalation "+m.Id+": "+variants.Count+" layouts out of 48 attempts.");
        }
    }
    private static void EscalationStageRules()
    {
        var primary=ObjectiveDefinition.Stabilize("primary",1,InteractAt());
        var follow=ObjectiveDefinition.Stabilize("stage-2",1,InteractAt());
        var b=MissionBattle(primary,new[]{follow});
        Equal(InteractionFailure.Unavailable,b.Mission.PreviewInteraction(b.CurrentActivation.ActivationId,"stage-2"));
        Interact(b,"primary");
        Equal<BattleResult>(null,b.Mission.Result);Equal(ObjectiveStatus.Completed,b.Mission.GetObjective("primary").Status);
        Equal(0,b.Mission.GetObjective("stage-2").Progress);
        var restored=BattleSave.Restore(BattleSave.Capture(b,"escalation-test"),"escalation-test");
        Equal(InteractionFailure.ActionSpent,restored.Mission.PreviewInteraction(restored.CurrentActivation.ActivationId,"stage-2"));
        NextMissionRound(restored);Interact(restored,"stage-2");Equal(MissionOutcome.Victory,restored.Mission.Result.Outcome);
        var final=BattleSave.Restore(BattleSave.Capture(restored,"escalation-test"),"escalation-test");
        Equal(MissionOutcome.Victory,final.Mission.Result.Outcome);
        var guard=MissionBattle(ObjectiveDefinition.Survive("primary",1),new[]{
            ObjectiveDefinition.Secure("stage-2",Box(-2,-2,2,2),new[]{"a"},Array.Empty<string>(),1,true)});
        NextMissionRound(guard);
        Equal(ObjectiveStatus.Completed,guard.Mission.GetObjective("primary").Status);
        Equal(0,guard.Mission.GetObjective("stage-2").Progress);
        FinishMissionRound(guard);Equal(MissionOutcome.Victory,guard.Mission.Result.Outcome);
        Throws<ArgumentException>(()=>MissionBattle(primary,new[]{ObjectiveDefinition.Stabilize("stage-3",1,InteractAt())}));
        var failed=MissionBattle(primary,new[]{ObjectiveDefinition.Defeat("stage-2",new[]{"b"})});
        Interact(failed,"primary");failed.RemoveUnit("b");failed.Mission.Evaluate();
        Equal(MissionOutcome.Defeat,failed.Mission.Result.Outcome);
    }
    private static void EscalationFlow()
    {
        var c=Escalation();var files=new FakeSaveFiles();var progress=new ProgressFiles();
        const string profile="escalation-flow";var store=new LocalProgressStore(progress,profile);
        store.Create(c.StarterUnits);SeedRecoveryHistory(store,c,34);
        var flow=c.CreateFlow(files,progress,profile);flow.Open();
        foreach(var chapter in W.All)
        {
            var m=chapter.Mission;
            Equal(SquadFailure.None,flow.ValidateSquad(m.Id,c.StarterUnits));
            if(chapter.Number<W.Last)Equal(SquadFailure.MissionLocked,flow.ValidateSquad(WorldFiveRouteRecovery.Id(chapter.Number+1),c.StarterUnits));
            flow.Start(m.Id,c.StarterUnits);
            var before=flow.ReadBattle();flow=c.CreateFlow(files,progress,profile);flow.Open();
            var after=flow.ReadBattle();Equal(before.Mission.Definition.AttemptId,after.Mission.Definition.AttemptId);
            Equal(TerrainMapSignature(before.Battlefield.Map),TerrainMapSignature(after.Battlefield.Map));
            Equal(m.Objectives.Count,after.Mission.Definition.Objectives.Count);
            flow.Execute(b=>b.Mission.Forfeit());flow.ClaimRewards();flow.ReturnToSelection();
            Equal(SquadFailure.None,flow.ValidateSquad(m.Id,c.StarterUnits));
            // Seed test-only victory for chain checks, not an AI victory claim.
            store.Claim(Result("escalation-clear-"+m.Id,m.Id),c.Rewards[m.Id]);flow.Open();
        }
        Equal(60,flow.InspectCampaign(c.Campaigns.Single().Id).CompletedMissions);
        Equal(false,flow.Progress.Owns("fixture-a-5"));
    }
    private static void EscalationRealJourney()
    {
        var c=Escalation();var files=new FakeSaveFiles();var progress=new ProgressFiles();
        const string profile="escalation-real-41";var store=new LocalProgressStore(progress,profile);
        store.Create(c.StarterUnits);SeedRecoveryHistory(store,c,40);
        MissionPresentation Open(){var m=new MissionPresentation(c,files,progress,profile);m.Open();return m;}
        var menu=Open();var id="w5-story-041";var m=c.Missions[id];
        menu.SelectMission(id);menu.SetSquad(c.StarterUnits);var play=menu.Start(menu.Read().PlanId);
        int interactions=0,moves=0;bool reopened=false;
        for(int step=0;step<600&&menu.Read().Results==null;step++)
        {
            var v=play.Read().Battle;BattleInputResult command;
            if(v.Phase!=BattleSessionPhase.PlayerInput)
                command=play.Advance(new EnemyPerception(Array.Empty<string>(),Array.Empty<string>()));
            else if(v.Activation.UnitId!="fixture-a-1")command=play.EndTurn(v.Activation.ActivationId);
            else
            {
                var current=v.Objectives.First(o=>o.Status==ObjectiveStatus.Active);
                if(current.Id=="primary")Equal(InputOutcome.Rejected,play.TapObjective("stage-2").Outcome);
                if(play.TapObjective(current.Id).Outcome==InputOutcome.Previewed)
                {
                    command=play.TapObjective(current.Id);Equal(InputOutcome.Committed,command.Outcome);interactions++;
                }
                else
                {
                    var dest=m.Objectives.Single(o=>o.Id==current.Id).Interaction.Point;
                    if(play.TapDestination(dest).Outcome==InputOutcome.Previewed)
                    {command=play.TapDestination(dest);Equal(InputOutcome.Committed,command.Outcome);moves++;}
                    else command=play.EndTurn(v.Activation.ActivationId);
                }
            }
            if(command.AnimationId!=null)Equal(true,play.CompleteAnimation(command.AnimationId));
            if(!reopened&&interactions==2&&menu.Read().Results==null)
            {
                menu=Open();play=menu.ResumeBattle();reopened=true;
                Equal(ObjectiveStatus.Completed,play.Read().Battle.Objectives[0].Status);
                Equal(0,play.Read().Battle.Objectives[1].Progress);
            }
            if(menu.Read().Results==null&&play.Read().Battle.Phase==BattleSessionPhase.PlayerInput&&
                play.Read().Battle.Activation.ActivationId==v.Activation?.ActivationId)
            {
                var end=play.EndTurn(v.Activation.ActivationId);
                if(end.AnimationId!=null)Equal(true,play.CompleteAnimation(end.AnimationId));
            }
        }
        if(menu.Read().Results==null)throw new Exception("Mission 41 sequential journey failed to finish.");
        Equal(MissionOutcome.Victory,menu.Read().Results.Result.Outcome);Equal(4,interactions);
        Equal(true,moves>0);Equal(true,reopened);
        string attempt=menu.Read().Results.Result.AttemptId;
        Equal(false,menu.ClaimRewards(attempt).AlreadyClaimed);Equal(true,menu.ClaimRewards(attempt).AlreadyClaimed);
        menu.ReturnToSelection(attempt);Equal("w5-story-042",Open().Read().NextCampaignMission.Definition.Id);
    }
}
