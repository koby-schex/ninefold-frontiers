using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Content;
using Ninefold.Core.Campaigns;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Persistence;
using Ninefold.Core.Progression;
using Ninefold.Core.Views;
using W=Ninefold.Core.Content.WorldFiveFinale;

internal static partial class Program
{
    private static IEnumerable<(string Name,Action Run)> WorldFiveFinaleTests()
    {
        yield return ("World Five 100-mission continuation preserves all existing IDs, rules and save revision",FinaleCompatibility);
        yield return ("All twenty new maps and required stages have connected footprints, distinct narrative layouts and matching art",FinaleAudit);
        yield return ("Three patrols vary safely across 144 seeds with unchanged opposition",FinalePatrols);
        yield return ("Three sequential stages gate interactions, preserve completed rounds and persist each transition",FinaleStageRules);
        yield return ("Every new stage is connected to the real campaign flow, supports reload and retry, with no premature Apex reward",FinaleFlow);
        yield return ("Mission 94 completes three route markers via real player commands and checkpoint reload",FinaleRealJourney);
        yield return ("Mission 100 defeats a live final defense, secures the route, restores the reference and unlocks the next faction",FinaleLivePatrol);
        yield return ("Final completion milestone is atomic and repeatable without duplicate units, even with the earlier prototype receipt",FinaleCompletionMigration);
    }
    private static readonly Lazy<ContentCatalog> FinaleCatalog=new Lazy<ContentCatalog>(W.Create);
    private static ContentCatalog Finale()=>FinaleCatalog.Value;
    private static CampaignDefinition MainCampaign(ContentCatalog c)=>c.Campaigns.Single(x=>x.Id==WorldFivePlayableSlice.CampaignId);
    private static void SeedFinaleHistory(LocalProgressStore store,ContentCatalog c,int count)
    {
        foreach(var id in MainCampaign(c).Missions.Take(count).Select(n=>n.MissionId))
            store.Claim(Result("finale-history-"+id,id),c.Rewards[id]);
    }
    private static void FinaleCompatibility()
    {
        var old=WorldFiveConvergence.Create();var c=Finale();
        Equal(old.Manifest.SaveRevision,c.Manifest.SaveRevision);
        Equal(100,MainCampaign(c).Missions.Count);
        Equal(true,old.Campaigns.Single().Missions.Select(m=>m.MissionId)
            .SequenceEqual(MainCampaign(c).Missions.Take(80).Select(m=>m.MissionId)));
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
        var history=new LocalProgressStore(progress,profile);history.Create(old.StarterUnits);SeedRecoveryHistory(history,old,79);
        var prior=old.CreateFlow(files,progress,profile);prior.Open();prior.Start("w5-story-080",old.StarterUnits);
        var attempt=prior.ReadBattle().Mission.Definition.AttemptId;
        var next=c.CreateFlow(files,progress,profile);next.Open();
        Equal(MissionFlowPhase.Battle,next.Phase);Equal(attempt,next.ReadBattle().Mission.Definition.AttemptId);
        Equal(79,next.Progress.Missions.Count);
    }
    private static void FinaleAudit()
    {
        var c=Finale();var bodies=ArtBodies(c);
        var report=CampaignProductionAudit.Review(c,MainCampaign(c).Id,environmentPrefix:"study-w5-");
        Equal(100,report.MissionCount);Equal(100,report.DistinctLayoutCount);Equal(6,report.DistinctPrimaryObjectiveCount);
        Equal(20,W.All.Select(x=>TerrainMapSignature(x.Mission.Map)).Distinct().Count());
        Equal(20,W.All.Select(x=>x.Title).Distinct().Count());
        Equal(11,W.All.Count(x=>x.Mission.Objectives.Any(o=>o.IsFollowupStage)));
        Equal(6,W.All.Count(x=>x.Mission.Objectives.Any(o=>o.Id=="stage-3")));
        Equal(3,W.All.Count(x=>x.Mission.ProceduralCover));
        foreach(var ch in W.All)
        {
            var m=ch.Mission;Equal(4,m.MaximumSquad);Equal(2,m.MinimumSquad);
            Equal(true,WorldFiveRouteRecovery.TryGet(m.Id,out var found));Equal(ch,found);
            Equal(ch.Mood,BattlefieldArtPlan.Choose(m.Id,m.EnvironmentKey));
            Equal("Act Five",WorldFiveStorySequence.StoryAct(m.Id));
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
        Console.WriteLine("INFO finale: "+report.DistinctLayoutCount+" distinct full campaign layouts; 11 staged encounters, six triple-stage.");
    }
    private static void FinalePatrols()
    {
        var c=Finale();var bodies=ArtBodies(c);
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
            Console.WriteLine("INFO finale "+m.Id+": "+variants.Count+" layouts out of 48 attempts.");
        }
    }
    private static void FinaleStageRules()
    {
        var primary=ObjectiveDefinition.Stabilize("primary",1,InteractAt());
        var follow=ObjectiveDefinition.Stabilize("stage-2",1,InteractAt());
        var third=ObjectiveDefinition.Stabilize("stage-3",1,InteractAt());
        var b=MissionBattle(primary,new[]{follow,third});
        Equal(InteractionFailure.Unavailable,b.Mission.PreviewInteraction(b.CurrentActivation.ActivationId,"stage-2"));
        Equal(InteractionFailure.Unavailable,b.Mission.PreviewInteraction(b.CurrentActivation.ActivationId,"stage-3"));
        Interact(b,"primary");Equal<BattleResult>(null,b.Mission.Result);
        b=BattleSave.Restore(BattleSave.Capture(b,"finale-test"),"finale-test");
        Equal(0,b.Mission.GetObjective("stage-2").Progress);
        NextMissionRound(b);Interact(b,"stage-2");Equal<BattleResult>(null,b.Mission.Result);
        b=BattleSave.Restore(BattleSave.Capture(b,"finale-test"),"finale-test");
        Equal(ObjectiveStatus.Completed,b.Mission.GetObjective("stage-2").Status);
        Equal(0,b.Mission.GetObjective("stage-3").Progress);
        NextMissionRound(b);Interact(b,"stage-3");Equal(MissionOutcome.Victory,b.Mission.Result.Outcome);
        b=BattleSave.Restore(BattleSave.Capture(b,"finale-test"),"finale-test");
        Equal(3,b.Mission.Result.Objectives.Count(o=>o.Status==ObjectiveStatus.Completed));
        var round=MissionBattle(ObjectiveDefinition.Survive("primary",1),new[]{
            ObjectiveDefinition.Secure("stage-2",Box(-2,-2,2,2),new[]{"a"},Array.Empty<string>(),1,true),
            ObjectiveDefinition.Secure("stage-3",Box(-2,-2,2,2),new[]{"a"},Array.Empty<string>(),1,true)});
        NextMissionRound(round);Equal(0,round.Mission.GetObjective("stage-2").Progress);
        NextMissionRound(round);Equal(0,round.Mission.GetObjective("stage-3").Progress);
        FinishMissionRound(round);Equal(MissionOutcome.Victory,round.Mission.Result.Outcome);
    }
    private static void FinaleFlow()
    {
        var c=Finale();var files=new FakeSaveFiles();var progress=new ProgressFiles();
        const string profile="finale-flow";var store=new LocalProgressStore(progress,profile);
        store.Create(c.StarterUnits);SeedFinaleHistory(store,c,80);
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
            store.Claim(Result("finale-clear-"+m.Id,m.Id),c.Rewards[m.Id]);flow.Open();
        }
        Equal(100,flow.InspectCampaign(MainCampaign(c).Id).CompletedMissions);
        Equal(false,flow.Progress.Owns("fixture-a-5"));
    }
    private static void FinaleRealJourney()
    {
        var c=Finale();var files=new FakeSaveFiles();var progress=new ProgressFiles();
        const string profile="finale-real-94";var store=new LocalProgressStore(progress,profile);
        store.Create(c.StarterUnits);SeedFinaleHistory(store,c,93);
        MissionPresentation Open(){var m=new MissionPresentation(c,files,progress,profile);m.Open();return m;}
        var menu=Open();var id="w5-story-094";var m=c.Missions[id];
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
        if(menu.Read().Results==null)throw new Exception("Mission 94 three-stage journey failed to finish.");
        Equal(MissionOutcome.Victory,menu.Read().Results.Result.Outcome);Equal(3,interactions);
        Equal(true,moves>0);Equal(2,reopened);
        string attempt=menu.Read().Results.Result.AttemptId;
        Equal(false,menu.ClaimRewards(attempt).AlreadyClaimed);Equal(true,menu.ClaimRewards(attempt).AlreadyClaimed);
        menu.ReturnToSelection(attempt);Equal("w5-story-095",Open().Read().NextCampaignMission.Definition.Id);
    }

    private static void FinaleLivePatrol()
    {
        var c=Finale();var files=new FakeSaveFiles();var progress=new ProgressFiles();
        const string profile="finale-live-100";var store=new LocalProgressStore(progress,profile);
        store.Create(c.StarterUnits);SeedFinaleHistory(store,c,99);
        // Normal earned fragments unlock the fourth standard unit and buy
        // the available rank, not an Apex or artificially inflated stats.
        var squad=new[]{"fixture-a-1","fixture-a-2","fixture-a-3","fixture-a-4"};
        store.Unlock("finale-fourth-standard",c.Units[squad[3]].Roster.Unlock);
        foreach(var id in squad)store.Advance("finale-advance-"+id,c.Units[id].Roster.Advancement);
        MissionPresentation Open(){var m=new MissionPresentation(c,files,progress,profile);m.Open();return m;}
        var menu=Open();var id100="w5-story-100";var mission=c.Missions[id100];
        Equal(false,menu.Read().Progress.Owns("fixture-a-5"));
        menu.SelectMission(id100);menu.SetSquad(squad);
        Equal(true,menu.Read().CanStart);var play=menu.Start(menu.Read().PlanId);
        int attacks=0,moves=0,enemyAdvances=0,interactions=0;bool resumed=false,held=false;
        for(int step=0;step<1000&&menu.Read().Results==null;step++)
        {
            var v=play.Read().Battle;
            if(v.Phase!=BattleSessionPhase.PlayerInput)
            {
                if(v.Phase==BattleSessionPhase.EnemyInput)enemyAdvances++;
                var advance=play.Advance(new EnemyPerception(
                    v.Units.Where(u=>u.IsEligible&&u.IsPlayerSquad).Select(u=>u.Id),
                    v.Units.Where(u=>u.IsEligible&&u.IsEnemyControlled).Select(u=>u.Id)));
                if(advance.AnimationId!=null)Equal(true,play.CompleteAnimation(advance.AnimationId));
                continue;
            }
            long activation=v.Activation.ActivationId;
            var actor=v.Units.Single(u=>u.Id==v.Activation.UnitId);
            var current=v.Objectives.First(o=>o.Status==ObjectiveStatus.Active);
            bool acted=false;
            if(current.Id=="primary")
            {
                Equal(InputOutcome.Rejected,play.TapObjective("stage-3").Outcome);
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
                    var p=actor.Position.Value;
                    var offsets=new[]{(0m,1.5m),(1.5m,0m),(0m,-1.5m),(-1.5m,0m),
                        (1.5m,1.5m),(-1.5m,1.5m),(1.5m,-1.5m),(-1.5m,-1.5m)};
                    var destinations=v.Units.Where(u=>u.IsEnemyControlled&&u.IsEligible&&u.Position.HasValue)
                        .SelectMany(e=>offsets.Select(o=>new FieldPoint(e.Position.Value.X+o.Item1,0,e.Position.Value.Z+o.Item2)))
                        .OrderBy(g=>DistanceSquared(p,g));
                    foreach(var goal in destinations)
                    {
                        if(goal.Equals(p)||play.TapDestination(goal).Outcome!=InputOutcome.Previewed)continue;
                        var result=play.TapDestination(goal);Equal(InputOutcome.Committed,result.Outcome);
                        if(result.AnimationId!=null)Equal(true,play.CompleteAnimation(result.AnimationId));
                        moves++;break;
                    }
                }
            }
            else
            {
                if(!resumed){menu=Open();play=menu.ResumeBattle();resumed=true;}
                if(current.Id=="stage-2")
                {
                    Equal(InputOutcome.Rejected,play.TapObjective("stage-3").Outcome);
                    // Choose one living squad member for the capture, leaving
                    // other units out of the route and avoiding body crowding.
                    string holder=v.Units.First(u=>u.IsPlayerSquad&&u.IsEligible).Id;
                    if(actor.Id==holder)
                    {
                        var goal=new FieldPoint(5,0,4);
                        if(!actor.Position.Value.Equals(goal)&&play.TapDestination(goal).Outcome==InputOutcome.Previewed)
                        {
                            var moved=play.TapDestination(goal);Equal(InputOutcome.Committed,moved.Outcome);
                            if(moved.AnimationId!=null)Equal(true,play.CompleteAnimation(moved.AnimationId));
                            moves++;
                        }
                    }
                }
                else
                {
                    held=true;
                    if(play.TapObjective("stage-3").Outcome==InputOutcome.Previewed)
                    {
                        var action=play.TapObjective("stage-3");Equal(InputOutcome.Committed,action.Outcome);
                        if(action.AnimationId!=null)Equal(true,play.CompleteAnimation(action.AnimationId));
                        interactions++;
                    }
                    else if(actor.Id==v.Units.First(u=>u.IsPlayerSquad&&u.IsEligible).Id)
                    {
                        var goal=mission.Objectives[2].Interaction.Point;
                        if(play.TapDestination(goal).Outcome==InputOutcome.Previewed)
                        {
                            var moved=play.TapDestination(goal);Equal(InputOutcome.Committed,moved.Outcome);
                            if(moved.AnimationId!=null)Equal(true,play.CompleteAnimation(moved.AnimationId));moves++;
                        }
                    }
                }
            }
            if(menu.Read().Results==null&&play.Read().Battle.Phase==BattleSessionPhase.PlayerInput&&play.Read().Battle.Activation.ActivationId==activation)
            {
                var end=play.EndTurn(activation);
                if(end.AnimationId!=null)Equal(true,play.CompleteAnimation(end.AnimationId));
            }
        }
        if(menu.Read().Results==null)throw new Exception("Finale did not terminate: "+play.Read().Battle.Round+"; attacks "+attacks+"; moves "+moves+"; enemy turns "+enemyAdvances+"; interactions "+interactions+".");
        Equal(MissionOutcome.Victory,menu.Read().Results.Result.Outcome);
        Equal(true,attacks>=5&&moves>0&&enemyAdvances>0&&resumed&&held);
        Equal(3,interactions);
        string attempt=menu.Read().Results.Result.AttemptId;
        Equal(false,menu.ClaimRewards(attempt).AlreadyClaimed);menu.ReturnToSelection(attempt);
        var final=Open();Equal(MainCampaign(c).Id,final.Read().ClaimableCampaignId);
        Equal(false,final.Read().Progress.Owns("fixture-a-5"));
        var claim=final.ClaimCampaign(MainCampaign(c).Id);
        Equal(true,claim.Saved.Progress.Owns("fixture-a-5"));
        Equal(W.TransferMissionId,Open().Read().NextCampaignMission.Definition.Id);
    }
    private static void FinaleCompletionMigration()
    {
        var old=WorldFiveConvergence.Create();var c=Finale();
        var files=new FakeSaveFiles();var progress=new ProgressFiles();const string profile="finale-old-receipt";
        var store=new LocalProgressStore(progress,profile);store.Create(old.StarterUnits);
        SeedRecoveryHistory(store,old,80);
        var original=old.CreateFlow(files,progress,profile);original.Open();
        var prior=original.ClaimCampaign(MainCampaign(old).Id).Receipt;
        Equal(MainCampaign(old).Id,prior.CampaignId);
        var flow=c.CreateFlow(files,progress,profile);flow.Open();
        Equal(false,flow.InspectCampaign(MainCampaign(c).Id).IsComplete);
        Equal(false,flow.InspectCampaign(MainCampaign(c).Id).RewardsClaimed);
        foreach(var m in MainCampaign(c).Missions.Skip(80).Take(19))
            store.Claim(Result("finale-migration-"+m.MissionId,m.MissionId),c.Rewards[m.MissionId]);
        flow.Open();Throws<InvalidOperationException>(()=>flow.ClaimCampaign(MainCampaign(c).Id));
        flow.Open();Equal(false,flow.Progress.Owns("fixture-a-5"));
        Equal(false,flow.InspectCampaign(W.TransferCampaignId).CanEnter);
        store.Claim(Result("finale-migration-last","w5-story-100"),c.Rewards["w5-story-100"]);
        flow.Open();var before=flow.Progress.Balance("fixture-resource");
        Equal(true,flow.InspectCampaign(MainCampaign(c).Id).IsComplete);
        Equal(false,flow.InspectCampaign(MainCampaign(c).Id).RewardsClaimed);
        // Durable write failure before commit must grant nothing.
        progress.Inner.Fault=1;
        Throws<System.IO.IOException>(()=>flow.ClaimCampaign(MainCampaign(c).Id));progress.Inner.Fault=0;flow.Open();
        Equal(before,flow.Progress.Balance("fixture-resource"));Equal(false,flow.Progress.Owns("fixture-a-5"));
        // Lost acknowledgement after write must reload the single full grant.
        progress.Inner.Fault=3;
        Throws<System.IO.IOException>(()=>flow.ClaimCampaign(MainCampaign(c).Id));progress.Inner.Fault=0;flow.Open();
        Equal(before+600L,flow.Progress.Balance("fixture-resource"));
        Equal(true,flow.InspectCampaign(MainCampaign(c).Id).RewardsClaimed);
        Equal(2,flow.Progress.CampaignClaims.Count);
        var repeat=flow.ClaimCampaign(MainCampaign(c).Id);
        Equal(true,repeat.AlreadyClaimed);Equal(W.CompletionReceiptId,repeat.Receipt.CampaignId);
        Equal(100,repeat.Receipt.RequiredMissions.Count);Equal(4,repeat.Receipt.GrantedUnits.Count);
        foreach(var id in new[]{"fixture-a-5","fixture-b-1","fixture-b-2","fixture-b-3"})Equal(true,flow.Progress.Owns(id));
        Equal(true,flow.InspectCampaign(W.TransferCampaignId).CanEnter);
        Equal(SquadFailure.None,flow.ValidateSquad(W.TransferMissionId,new[]{"fixture-b-1","fixture-b-2","fixture-b-3"}));
        flow.Start(W.TransferMissionId,new[]{"fixture-b-1","fixture-b-2","fixture-b-3"});
        var transfer=flow.ReadBattle();Equal(W.TransferMissionId,transfer.Mission.Definition.MissionId);
        flow=c.CreateFlow(files,progress,profile);flow.Open();Equal(MissionFlowPhase.Battle,flow.Phase);
        Equal(transfer.Mission.Definition.AttemptId,flow.ReadBattle().Mission.Definition.AttemptId);
        Equal(2,flow.Progress.CampaignClaims.Count);
    }
}
