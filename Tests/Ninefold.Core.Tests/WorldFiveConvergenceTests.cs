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
using W=Ninefold.Core.Content.WorldFiveConvergence;

internal static partial class Program
{
    private static IEnumerable<(string Name,Action Run)> WorldFiveConvergenceTests()
    {
        yield return ("World Five 80-mission continuation preserves all existing IDs, rules and save revision",ConvergenceCompatibility);
        yield return ("All twenty new maps and required stages have connected footprints, distinct narrative layouts and matching art",ConvergenceAudit);
        yield return ("Four optional patrols vary safely across 192 seeds with unchanged opposition",ConvergencePatrols);
        yield return ("Three sequential stages gate interactions, preserve completed rounds and persist each transition",ConvergenceStageRules);
        yield return ("Every new stage is connected to the real campaign flow, supports reload and retry, with no premature Apex reward",ConvergenceFlow);
        yield return ("Mission 62 completes three route markers via real player commands and checkpoint reload",ConvergenceRealJourney);
        yield return ("Mission 63 defeats a live AI patrol through real movement and attacks",ConvergenceLivePatrol);
    }
    private static readonly Lazy<ContentCatalog> ConvergenceCatalog=new Lazy<ContentCatalog>(W.Create);
    private static ContentCatalog Convergence()=>ConvergenceCatalog.Value;
    private static void ConvergenceCompatibility()
    {
        var old=WorldFiveEscalation.Create();var c=Convergence();
        Equal(old.Manifest.SaveRevision,c.Manifest.SaveRevision);
        Equal(80,c.Campaigns.Single().Missions.Count);
        Equal(true,old.Campaigns.Single().Missions.Select(m=>m.MissionId)
            .SequenceEqual(c.Campaigns.Single().Missions.Take(60).Select(m=>m.MissionId)));
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
        var history=new LocalProgressStore(progress,profile);history.Create(old.StarterUnits);SeedRecoveryHistory(history,old,59);
        var prior=old.CreateFlow(files,progress,profile);prior.Open();prior.Start("w5-story-060",old.StarterUnits);
        var attempt=prior.ReadBattle().Mission.Definition.AttemptId;
        var next=c.CreateFlow(files,progress,profile);next.Open();
        Equal(MissionFlowPhase.Battle,next.Phase);Equal(attempt,next.ReadBattle().Mission.Definition.AttemptId);
        Equal(59,next.Progress.Missions.Count);
    }
    private static void ConvergenceAudit()
    {
        var c=Convergence();var bodies=ArtBodies(c);
        var report=CampaignProductionAudit.Review(c,c.Campaigns.Single().Id,environmentPrefix:"study-w5-");
        Equal(80,report.MissionCount);Equal(80,report.DistinctLayoutCount);Equal(6,report.DistinctPrimaryObjectiveCount);
        Equal(20,W.All.Select(x=>TerrainMapSignature(x.Mission.Map)).Distinct().Count());
        Equal(20,W.All.Select(x=>x.Title).Distinct().Count());
        Equal(10,W.All.Count(x=>x.Mission.Objectives.Any(o=>o.IsFollowupStage)));
        Equal(6,W.All.Count(x=>x.Mission.Objectives.Any(o=>o.Id=="stage-3")));
        Equal(4,W.All.Count(x=>x.Mission.ProceduralCover));
        foreach(var ch in W.All)
        {
            var m=ch.Mission;Equal(4,m.MaximumSquad);Equal(2,m.MinimumSquad);
            Equal(true,WorldFiveRouteRecovery.TryGet(m.Id,out var found));Equal(ch,found);
            Equal(ch.Mood,BattlefieldArtPlan.Choose(m.Id,m.EnvironmentKey));
            Equal("Act Four",WorldFiveStorySequence.StoryAct(m.Id));
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
        Console.WriteLine("INFO convergence: "+report.DistinctLayoutCount+" distinct full campaign layouts; 10 staged encounters, six triple-stage.");
    }
    private static void ConvergencePatrols()
    {
        var c=Convergence();var bodies=ArtBodies(c);
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
            Console.WriteLine("INFO convergence "+m.Id+": "+variants.Count+" layouts out of 48 attempts.");
        }
    }
    private static void ConvergenceStageRules()
    {
        var primary=ObjectiveDefinition.Stabilize("primary",1,InteractAt());
        var follow=ObjectiveDefinition.Stabilize("stage-2",1,InteractAt());
        var third=ObjectiveDefinition.Stabilize("stage-3",1,InteractAt());
        var b=MissionBattle(primary,new[]{follow,third});
        Equal(InteractionFailure.Unavailable,b.Mission.PreviewInteraction(b.CurrentActivation.ActivationId,"stage-2"));
        Equal(InteractionFailure.Unavailable,b.Mission.PreviewInteraction(b.CurrentActivation.ActivationId,"stage-3"));
        Interact(b,"primary");Equal<BattleResult>(null,b.Mission.Result);
        b=BattleSave.Restore(BattleSave.Capture(b,"convergence-test"),"convergence-test");
        Equal(0,b.Mission.GetObjective("stage-2").Progress);
        NextMissionRound(b);Interact(b,"stage-2");Equal<BattleResult>(null,b.Mission.Result);
        b=BattleSave.Restore(BattleSave.Capture(b,"convergence-test"),"convergence-test");
        Equal(ObjectiveStatus.Completed,b.Mission.GetObjective("stage-2").Status);
        Equal(0,b.Mission.GetObjective("stage-3").Progress);
        NextMissionRound(b);Interact(b,"stage-3");Equal(MissionOutcome.Victory,b.Mission.Result.Outcome);
        b=BattleSave.Restore(BattleSave.Capture(b,"convergence-test"),"convergence-test");
        Equal(3,b.Mission.Result.Objectives.Count(o=>o.Status==ObjectiveStatus.Completed));
        var round=MissionBattle(ObjectiveDefinition.Survive("primary",1),new[]{
            ObjectiveDefinition.Secure("stage-2",Box(-2,-2,2,2),new[]{"a"},Array.Empty<string>(),1,true),
            ObjectiveDefinition.Secure("stage-3",Box(-2,-2,2,2),new[]{"a"},Array.Empty<string>(),1,true)});
        NextMissionRound(round);Equal(0,round.Mission.GetObjective("stage-2").Progress);
        NextMissionRound(round);Equal(0,round.Mission.GetObjective("stage-3").Progress);
        FinishMissionRound(round);Equal(MissionOutcome.Victory,round.Mission.Result.Outcome);
    }
    private static void ConvergenceFlow()
    {
        var c=Convergence();var files=new FakeSaveFiles();var progress=new ProgressFiles();
        const string profile="convergence-flow";var store=new LocalProgressStore(progress,profile);
        store.Create(c.StarterUnits);SeedRecoveryHistory(store,c,60);
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
            store.Claim(Result("convergence-clear-"+m.Id,m.Id),c.Rewards[m.Id]);flow.Open();
        }
        Equal(80,flow.InspectCampaign(c.Campaigns.Single().Id).CompletedMissions);
        Equal(false,flow.Progress.Owns("fixture-a-5"));
    }
    private static void ConvergenceRealJourney()
    {
        var c=Convergence();var files=new FakeSaveFiles();var progress=new ProgressFiles();
        const string profile="convergence-real-62";var store=new LocalProgressStore(progress,profile);
        store.Create(c.StarterUnits);SeedRecoveryHistory(store,c,61);
        MissionPresentation Open(){var m=new MissionPresentation(c,files,progress,profile);m.Open();return m;}
        var menu=Open();var id="w5-story-062";var m=c.Missions[id];
        menu.SelectMission(id);menu.SetSquad(c.StarterUnits);var play=menu.Start(menu.Read().PlanId);
        int interactions=0,moves=0,reopened=0;
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
                if(current.Id!="stage-3")Equal(InputOutcome.Rejected,play.TapObjective("stage-3").Outcome);
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
            if(interactions>reopened&&interactions<=2&&menu.Read().Results==null)
            {
                menu=Open();play=menu.ResumeBattle();reopened++;
                Equal(ObjectiveStatus.Completed,play.Read().Battle.Objectives[interactions-1].Status);
                Equal(0,play.Read().Battle.Objectives[interactions].Progress);
            }
            if(menu.Read().Results==null&&play.Read().Battle.Phase==BattleSessionPhase.PlayerInput&&
                play.Read().Battle.Activation.ActivationId==v.Activation?.ActivationId)
            {
                var end=play.EndTurn(v.Activation.ActivationId);
                if(end.AnimationId!=null)Equal(true,play.CompleteAnimation(end.AnimationId));
            }
        }
        if(menu.Read().Results==null)throw new Exception("Mission 62 three-stage journey failed to finish.");
        Equal(MissionOutcome.Victory,menu.Read().Results.Result.Outcome);Equal(3,interactions);
        Equal(true,moves>0);Equal(2,reopened);
        string attempt=menu.Read().Results.Result.AttemptId;
        Equal(false,menu.ClaimRewards(attempt).AlreadyClaimed);Equal(true,menu.ClaimRewards(attempt).AlreadyClaimed);
        menu.ReturnToSelection(attempt);Equal("w5-story-063",Open().Read().NextCampaignMission.Definition.Id);
    }

    private static void ConvergenceLivePatrol()
    {
        var c=Convergence();var files=new FakeSaveFiles();var progress=new ProgressFiles();
        const string profile="convergence-live-63";var store=new LocalProgressStore(progress,profile);
        store.Create(c.StarterUnits);SeedRecoveryHistory(store,c,62);
        MissionPresentation Open(){var m=new MissionPresentation(c,files,progress,profile);m.Open();return m;}
        var menu=Open();var id="w5-story-063";menu.SelectMission(id);menu.SetSquad(c.StarterUnits);
        Equal(true,menu.Read().CanStart);var play=menu.Start(menu.Read().PlanId);
        int attacks=0,moves=0,enemyAdvances=0;bool resumed=false;
        for(int step=0;step<450&&menu.Read().Results==null;step++)
        {
            var v=play.Read().Battle;
            if(v.Phase!=BattleSessionPhase.PlayerInput)
            {
                // Match the real Unity host: the session intentionally waits
                // at EnemyInput until a perception frame is supplied.
                if(v.Phase==BattleSessionPhase.EnemyInput)enemyAdvances++;
                var advance=play.Advance(new EnemyPerception(
                    v.Units.Where(u=>u.IsEligible&&u.IsPlayerSquad).Select(u=>u.Id),
                    v.Units.Where(u=>u.IsEligible&&u.IsEnemyControlled).Select(u=>u.Id)));
                if(advance.AnimationId!=null)Equal(true,play.CompleteAnimation(advance.AnimationId));
                continue;
            }
            long activation=v.Activation.ActivationId;
            var actor=v.Units.Single(u=>u.Id==v.Activation.UnitId);
            bool acted=false;
            foreach(var slot in new[]{AbilitySlot.Signature,AbilitySlot.NormalAttack,AbilitySlot.Main})
            {
                if(play.SelectAbility(slot).Outcome!=InputOutcome.Selected)continue;
                var target=play.Read().Targets.Where(t=>t.IsValid&&v.Units.Any(u=>u.Id==t.TargetId&&u.IsEnemyControlled&&u.IsEligible))
                    .OrderBy(t=>v.Units.Single(u=>u.Id==t.TargetId).Health.CurrentHealth)
                    .ThenBy(t=>t.TargetId,StringComparer.Ordinal).FirstOrDefault();
                if(target==null)continue;
                Equal(InputOutcome.Previewed,play.TapUnit(target.TargetId).Outcome);
                var attack=play.TapUnit(target.TargetId);Equal(InputOutcome.Committed,attack.Outcome);
                if(attack.AnimationId!=null)Equal(true,play.CompleteAnimation(attack.AnimationId));
                attacks++;acted=true;break;
            }
            if(!acted)
            {
                // Request destination points around the nearest eligible foe,
                // using the same partial-route solver as the real player.
                // Never greedily step into an obstacle or occupied body.
                var p=actor.Position.Value;
                var enemies=v.Units.Where(u=>u.IsEnemyControlled&&u.IsEligible&&u.Position.HasValue)
                    .OrderBy(u=>DistanceSquared(p,u.Position.Value)).ToArray();
                var offsets=new[]{(0m,1.5m),(1.5m,0m),(0m,-1.5m),(-1.5m,0m),
                    (1.5m,1.5m),(-1.5m,1.5m),(1.5m,-1.5m),(-1.5m,-1.5m)};
                var destinations=enemies.SelectMany(e=>offsets.Select(o=>
                    new FieldPoint(e.Position.Value.X+o.Item1,0,e.Position.Value.Z+o.Item2)))
                    .OrderBy(g=>DistanceSquared(p,g));
                foreach(var goal in destinations)
                {
                    if(goal.Equals(p)||play.TapDestination(goal).Outcome!=InputOutcome.Previewed)continue;
                    var result=play.TapDestination(goal);Equal(InputOutcome.Committed,result.Outcome);
                    if(result.AnimationId!=null)Equal(true,play.CompleteAnimation(result.AnimationId));
                    moves++;break;
                }
            }
            if(!resumed&&moves>0&&menu.Read().Results==null)
            {menu=Open();play=menu.ResumeBattle();resumed=true;}
            if(menu.Read().Results==null&&play.Read().Battle.Phase==BattleSessionPhase.PlayerInput&&
                play.Read().Battle.Activation.ActivationId==activation)
            {
                var end=play.EndTurn(activation);
                if(end.AnimationId!=null)Equal(true,play.CompleteAnimation(end.AnimationId));
            }
        }
        if(menu.Read().Results==null)throw new Exception("Mission 63 patrol journey did not terminate: round "+play.Read().Battle.Round+"; attacks "+attacks+"; moves "+moves+"; enemy turns "+enemyAdvances+".");
        Equal(MissionOutcome.Victory,menu.Read().Results.Result.Outcome);
        Equal(true,attacks>=3&&moves>0&&enemyAdvances>0&&resumed);
        string attempt=menu.Read().Results.Result.AttemptId;
        Equal(false,menu.ClaimRewards(attempt).AlreadyClaimed);menu.ReturnToSelection(attempt);
        Equal("w5-story-064",Open().Read().NextCampaignMission.Definition.Id);
    }
}
