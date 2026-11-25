using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Persistence;
using Ninefold.Core.Progression;
using Ninefold.Core.Views;

internal static partial class Program
{
    private static IEnumerable<(string Name,Action Run)> WorldFiveModeExpansionTests()
    {
        yield return ("Expanded modes preserve all 100 original missions, prior 13 mode IDs, rewards and exact save revision",ExpandedSaveCompatibility);
        yield return ("Six-wave Horde is ONE battle with 11 sequential stages and 19 initially dormant reinforcements",HordeRunStructure);
        yield return ("Horde prevents early wave actions, saves dormant actors and never spawns wave enemies before preparation",HordeDormantPersistence);
        yield return ("Horde wave one transitions to rally phase through real AI combat without resetting squad health or positions",HordeLiveTransition);
        yield return ("Pathweave unlocks two further exclusive outcomes with persistent, asymmetric route consequences",PathSecondChoices);
        yield return ("World Collision adds three genuine multi-front seven-unit encounters with enforced prerequisite chain",CollisionMultiFront);
        yield return ("Mode expansion retains previously locked routes and old Horde wave gates in the authoritative core",ExpandedLegacyGates);
        yield return ("All eight new arenas validate real actor positions, route connectivity and distinct battlefield cover",ExpandedGeometry);
        yield return ("Horde score is deterministic and cannot count incomplete waves as cleared",HordeScoring);
    }

    private static readonly Lazy<ContentCatalog> ExpandedCatalog=new Lazy<ContentCatalog>(WorldFiveModeExpansion.Create);
    private static ContentCatalog Expanded()=>ExpandedCatalog.Value;
    private static void ExpansionClear(LocalProgressStore store,ContentCatalog c,string id)
        =>store.Claim(Result("expanded-clear-"+id,id),c.Rewards[id]);
    private static void ExpandedSaveCompatibility()
    {
        var old=WorldFiveModes.Create();var c=Expanded();
        Equal(old.Manifest.SaveRevision,c.Manifest.SaveRevision);
        Equal(8,WorldFiveModeExpansion.Stages.Count);
        Equal(old.Missions.Count+8,c.Missions.Count);
        Equal(old.Rewards.Count+8,c.Rewards.Count);
        Equal(true,old.Campaigns.Select(x=>x.Id).SequenceEqual(c.Campaigns.Select(x=>x.Id)));
        foreach(var mission in old.Missions.Values)
        {
            var next=c.Missions[mission.Id];
            Equal(TerrainMapSignature(mission.Map),TerrainMapSignature(next.Map));
            Equal(true,mission.Deployment.SequenceEqual(next.Deployment));
            Equal(mission.Objectives.Count,next.Objectives.Count);
            Equal(old.Rewards[mission.Id].Revision,c.Rewards[mission.Id].Revision);
        }
        var saves=new FakeSaveFiles();var progress=new ProgressFiles();const string profile="expansion-compat";
        var previous=new LocalProgressStore(progress,profile);previous.Create(old.StarterUnits);
        var flow=old.CreateFlow(saves,progress,profile);flow.Open();
        flow.Start(WorldFiveOpeningTenPrototype.MissionIds[0],old.StarterUnits);
        string attempt=flow.ReadBattle().Mission.Definition.AttemptId;
        var current=c.CreateFlow(saves,progress,profile);current.Open();
        Equal(MissionFlowPhase.Battle,current.Phase);
        Equal(attempt,current.ReadBattle().Mission.Definition.AttemptId);
    }
    private static void HordeRunStructure()
    {
        var c=Expanded();var mission=c.Missions[WorldFiveModeExpansion.HordeRunId];
        Equal(false,mission.IsCampaign);Equal(4,mission.MaximumSquad);
        Equal(11,mission.Objectives.Count);
        Equal(6,mission.Objectives.Count(o=>o.Kind==ObjectiveKind.Defeat));
        Equal(5,mission.Objectives.Count(o=>o.Kind==ObjectiveKind.Stabilize));
        for(int i=1;i<mission.Objectives.Count;i++)
            Equal("stage-"+(i+1),mission.Objectives[i].Id);
        Equal(21,mission.Actors.Count);Equal(21,mission.Actors.Select(a=>a.Id).Distinct().Count());
        Equal(0,mission.Actors.Count(a=>!a.IsHostile));
        Equal(11,c.Rewards[mission.Id].Replay.Single().Amount); // rewards deliberately bounded
    }
    private static void HordeDormantPersistence()
    {
        var c=Expanded();var saves=new FakeSaveFiles();var progress=new ProgressFiles();
        const string profile="horde-dormant-save";
        var store=new LocalProgressStore(progress,profile);store.Create(c.StarterUnits);
        ExpansionClear(store,c,WorldFiveModes.HordeId(1));
        var flow=c.CreateFlow(saves,progress,profile);flow.Open();
        var id=WorldFiveModeExpansion.HordeRunId;
        Equal(SquadFailure.None,flow.ValidateSquad(id,c.StarterUnits));
        flow.Start(id,c.StarterUnits);
        var b=flow.ReadBattle();
        Equal(2,b.Mission.Definition.Objectives[0].Subjects.Count);
        var ids=b.Mission.Definition.Objectives.SelectMany(o=>o.Subjects).Distinct().ToArray();
        var future=b.Mission.Definition.Objectives.Skip(1)
            .Where(o=>o.Kind==ObjectiveKind.Defeat).SelectMany(o=>o.Subjects).ToArray();
        Equal(19,future.Length);
        foreach(var enemy in future)
        {
            Equal(false,b.IsUnitEligible(enemy));
            Equal(true,b.Health.GetState(enemy).CurrentHealth>0);
            Equal(0L,b.GetActivationCount(enemy));
        }
        Equal(2,b.Battlefield.InspectEnemyIds().Count(enemy=>b.IsUnitEligible(enemy)));
        Equal(InteractionFailure.Unavailable,
            b.Mission.PreviewInteraction(b.CurrentActivation.ActivationId,"stage-2"));
        var restored=c.CreateFlow(saves,progress,profile);restored.Open();
        Equal(MissionFlowPhase.Battle,restored.Phase);
        var again=restored.ReadBattle();
        Equal(b.Mission.Definition.AttemptId,again.Mission.Definition.AttemptId);
        Equal(19,future.Count(enemy=>!again.IsUnitEligible(enemy)&&again.Health.GetState(enemy).CurrentHealth>0));
        Equal(11,again.Mission.Definition.Objectives.Count);
        var packed=BattleSave.Capture(again,c.Manifest.SaveRevision);
        var fromBytes=BattleSave.Restore(packed,c.Manifest.SaveRevision);
        Equal(11,fromBytes.Mission.Definition.Objectives.Count);
        Equal(19,future.Count(enemy=>!fromBytes.IsUnitEligible(enemy)));
    }
    private static void HordeLiveTransition()
    {
        var c=Expanded();var files=new FakeSaveFiles();var progress=new ProgressFiles();
        const string profile="horde-live-phase";
        var store=new LocalProgressStore(progress,profile);store.Create(c.StarterUnits);
        ExpansionClear(store,c,WorldFiveModes.HordeId(1));
        MissionPresentation Open(){var m=new MissionPresentation(c,files,progress,profile);m.Open();return m;}
        var menu=Open();var id=WorldFiveModeExpansion.HordeRunId;
        menu.SelectMission(id);menu.SetSquad(c.StarterUnits);
        var play=menu.Start(menu.Read().PlanId);
        int moves=0,attacks=0,enemyTurns=0;
        for(int step=0;step<700;step++)
        {
            var v=play.Read().Battle;
            if(v.Objectives[0].Status==ObjectiveStatus.Completed)break;
            if(v.Phase!=BattleSessionPhase.PlayerInput)
            {
                if(v.Phase==BattleSessionPhase.EnemyInput)enemyTurns++;
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
                var target=play.Read().Targets.Where(t=>t.IsValid&&
                    v.Units.Any(u=>u.Id==t.TargetId&&u.IsEnemyControlled&&u.IsEligible))
                    .OrderBy(t=>v.Units.Single(u=>u.Id==t.TargetId).Health.CurrentHealth)
                    .ThenBy(t=>t.TargetId,StringComparer.Ordinal).FirstOrDefault();
                if(target==null)continue;
                Equal(InputOutcome.Previewed,play.TapUnit(target.TargetId).Outcome);
                var a=play.TapUnit(target.TargetId);Equal(InputOutcome.Committed,a.Outcome);
                if(a.AnimationId!=null)Equal(true,play.CompleteAnimation(a.AnimationId));
                attacks++;acted=true;break;
            }
            if(!acted)
            {
                var p=actor.Position.Value;
                var goals=v.Units.Where(u=>u.IsEnemyControlled&&u.IsEligible&&u.Position.HasValue)
                    .SelectMany(u=>new[]{new FieldPoint(u.Position.Value.X+1.5m,0,u.Position.Value.Z),
                        new FieldPoint(u.Position.Value.X,0,u.Position.Value.Z-1.5m),
                        new FieldPoint(u.Position.Value.X-1.5m,0,u.Position.Value.Z)})
                    .OrderBy(g=>DistanceSquared(p,g));
                foreach(var goal in goals)
                {
                    if(play.TapDestination(goal).Outcome!=InputOutcome.Previewed)continue;
                    var move=play.TapDestination(goal);Equal(InputOutcome.Committed,move.Outcome);
                    if(move.AnimationId!=null)Equal(true,play.CompleteAnimation(move.AnimationId));moves++;break;
                }
            }
            if(menu.Read().Results==null&&play.Read().Battle.Phase==BattleSessionPhase.PlayerInput&&
                play.Read().Battle.Activation.ActivationId==activation)
            {
                var end=play.EndTurn(activation);
                if(end.AnimationId!=null)Equal(true,play.CompleteAnimation(end.AnimationId));
            }
        }
        var before=play.Read().Battle;
        if(before.Objectives[0].Status!=ObjectiveStatus.Completed)
            throw new Exception("Wave one was not cleared by active AI play. Attacks "+attacks+" moves "+moves);
        Equal(true,attacks>=2 && moves>0 && enemyTurns>0);
        Equal<BattleResult>(null,before.Result);
        Equal(0,before.Units.Count(u=>u.IsEnemyControlled&&u.IsEligible));
        Equal(19,before.Units.Count(u=>u.IsEnemyControlled&&!u.IsEligible&&u.Health.CurrentHealth>0));
        var health=before.Units.Where(u=>u.IsPlayerSquad).ToDictionary(u=>u.Id,u=>u.Health.CurrentHealth);
        var positions=before.Units.Where(u=>u.IsPlayerSquad).ToDictionary(u=>u.Id,u=>u.Position.Value);
        menu=Open();play=menu.ResumeBattle();
        var after=play.Read().Battle;
        Equal(19,after.Units.Count(u=>u.IsEnemyControlled&&!u.IsEligible&&u.Health.CurrentHealth>0));
        foreach(var u in after.Units.Where(u=>u.IsPlayerSquad))
        {Equal(health[u.Id],u.Health.CurrentHealth);Equal(positions[u.Id],u.Position.Value);}
        // Preparation requires deliberate player input. No automatic respawn
        // or restoration of spent signatures between waves.
        Equal(ObjectiveStatus.Active,after.Objectives[1].Status);
        Equal(ObjectiveStatus.Active,after.Objectives[2].Status);
        Equal(0,after.Objectives[2].Progress);
        var raw=c.CreateFlow(files,progress,profile);raw.Open();
        var dormant=raw.ReadBattle().Mission.Definition.Objectives[2].Subjects;
        Equal(true,dormant.All(unit=>!raw.ReadBattle().IsUnitEligible(unit)));
    }
    private static void PathSecondChoices()
    {
        var c=Expanded();
        foreach(var shelter in new[]{true,false})
        {
            var progress=new ProgressFiles();string profile="expanded-path-"+shelter;
            var store=new LocalProgressStore(progress,profile);store.Create(c.StarterUnits);
            var flow=c.CreateFlow(new FakeSaveFiles(),progress,profile);flow.Open();
            var chosen=shelter?WorldFiveModeExpansion.ShelterWitness:WorldFiveModeExpansion.SealRecord;
            var other=shelter?WorldFiveModeExpansion.ShelterPassage:WorldFiveModeExpansion.SealGuard;
            var first=shelter?WorldFiveModes.PathShelter:WorldFiveModes.PathSeal;
            var end=shelter?WorldFiveModes.PathShelterEnd:WorldFiveModes.PathSealEnd;
            Equal(SquadFailure.MissionLocked,flow.ValidateSquad(chosen,c.StarterUnits));
            foreach(var id in new[]{WorldFiveModes.PathEntry,first,end})ExpansionClear(store,c,id);
            flow.Open();
            Equal(SquadFailure.None,flow.ValidateSquad(chosen,c.StarterUnits));
            Equal(SquadFailure.None,flow.ValidateSquad(other,c.StarterUnits));
            Equal(shelter?"Shelter · second decision pending":"Containment · second decision pending",
                WorldFiveModeExpansion.Consequence(flow.Progress));
            ExpansionClear(store,c,chosen);flow.Open();
            Equal(SquadFailure.MissionLocked,flow.ValidateSquad(other,c.StarterUnits));
            Equal(SquadFailure.None,flow.ValidateSquad(chosen,c.StarterUnits));
            var expected=shelter?"Shelter · testimony preserved":"Containment · record verified";
            Equal(expected,WorldFiveModeExpansion.Consequence(flow.Progress));
            var reloaded=c.CreateFlow(new FakeSaveFiles(),progress,profile);reloaded.Open();
            Equal(SquadFailure.MissionLocked,reloaded.ValidateSquad(other,c.StarterUnits));
        }
    }
    private static void CollisionMultiFront()
    {
        var c=Expanded();var progress=new ProgressFiles();const string profile="expanded-collision";
        var files=new FakeSaveFiles();var store=new LocalProgressStore(progress,profile);
        store.Create(c.StarterUnits);
        var flow=c.CreateFlow(files,progress,profile);flow.Open();
        string[] squad={"fixture-a-1","fixture-a-2","fixture-a-3","fixture-a-5","fixture-b-1","fixture-b-2","fixture-b-3"};
        Equal(SquadFailure.MissionLocked,flow.ValidateSquad(WorldFiveModeExpansion.CollisionFlanks,squad));
        var main=c.Campaigns.Single(x=>x.Id==WorldFivePlayableSlice.CampaignId);
        foreach(var n in main.Missions)store.Claim(Result("expanded-history-"+n.MissionId,n.MissionId),c.Rewards[n.MissionId]);
        flow.Open();flow.ClaimCampaign(main.Id);
        foreach(var id in new[]{WorldFiveModes.CollisionEntry,WorldFiveModes.CollisionEnd})
            ExpansionClear(store,c,id);
        flow.Open();
        foreach(var id in new[]{WorldFiveModeExpansion.CollisionFlanks,WorldFiveModeExpansion.CollisionRelay,WorldFiveModeExpansion.CollisionFinal})
        {
            Equal(SquadFailure.None,flow.ValidateSquad(id,squad));
            var m=c.Missions[id];
            Equal(EncounterKind.WorldCollision,m.Encounter);Equal(7,m.MaximumSquad);
            Equal(3,m.Objectives.Count);
            Equal(8,m.Actors.Count(a=>a.IsHostile));
            if(id!=WorldFiveModeExpansion.CollisionFinal)
            {
                flow.Start(id,squad);Equal(7,flow.ReadBattle().Mission.Definition.Squad.Count);
                var saved=c.CreateFlow(files,progress,profile);saved.Open();
                Equal(MissionFlowPhase.Battle,saved.Phase);
                saved.Execute(b=>b.Mission.Forfeit());saved.ClaimRewards();saved.ReturnToSelection();
                ExpansionClear(store,c,id);flow.Open();
            }
        }
        Equal(SquadFailure.None,flow.ValidateSquad(WorldFiveModeExpansion.CollisionFinal,squad));
        Equal(100,flow.InspectCampaign(main.Id).CompletedMissions);
    }
    private static void ExpandedLegacyGates()
    {
        var c=Expanded();var progress=new ProgressFiles();
        const string profile="expanded-old-gates";
        var store=new LocalProgressStore(progress,profile);store.Create(c.StarterUnits);
        var flow=c.CreateFlow(new FakeSaveFiles(),progress,profile);flow.Open();
        Equal(SquadFailure.MissionLocked,flow.ValidateSquad(WorldFiveModes.HordeId(6),c.StarterUnits));
        Equal(SquadFailure.MissionLocked,flow.ValidateSquad(WorldFiveModes.PathShelter,c.StarterUnits));
        Equal(SquadFailure.MissionLocked,flow.ValidateSquad(WorldFiveModes.CollisionEntry,c.StarterUnits));
        Equal(SquadFailure.MissionLocked,flow.ValidateSquad(WorldFiveModeExpansion.HordeRunId,c.StarterUnits));
        ExpansionClear(store,c,WorldFiveModes.HordeId(1));flow.Open();
        Equal(SquadFailure.None,flow.ValidateSquad(WorldFiveModeExpansion.HordeRunId,c.StarterUnits));
    }
    private static void ExpandedGeometry()
    {
        var c=Expanded();var bodies=ArtBodies(c);
        Equal(8,WorldFiveModeExpansion.Stages.Select(x=>TerrainMapSignature(c.Missions[x.Id].Map)).Distinct().Count());
        foreach(var stage in WorldFiveModeExpansion.Stages)
        {
            var m=c.Missions[stage.Id];
            RecoveryReachability(m,m.Map,bodies);
            Equal(true,m.EnvironmentKey.StartsWith("study-w5-",StringComparison.Ordinal));
            Equal(true,m.Deployment.Count>=4);
            Equal(true,m.Actors.Count>=3||m.Id==WorldFiveModeExpansion.HordeRunId);
            Equal(true,c.Rewards[m.Id].Replay.Sum(g=>g.Amount)<=12);
        }
        Equal(100,c.Campaigns.Single(x=>x.Id==WorldFivePlayableSlice.CampaignId).Missions.Count);
    }
    private static void HordeScoring()
    {
        var c=Expanded();var progress=new ProgressFiles();const string profile="horde-scoring";
        var store=new LocalProgressStore(progress,profile);store.Create(c.StarterUnits);
        ExpansionClear(store,c,WorldFiveModes.HordeId(1));
        var flow=c.CreateFlow(new FakeSaveFiles(),progress,profile);flow.Open();
        flow.Start(WorldFiveModeExpansion.HordeRunId,c.StarterUnits);
        var terminal=flow.Execute(b=>b.Mission.Forfeit()).Mission.Result;
        Equal(0,WorldFiveModeExpansion.HordeWave(terminal));
        Equal(0L,WorldFiveModeExpansion.HordeScore(terminal));
        Throws<ArgumentException>(()=>WorldFiveModeExpansion.HordeWave(null));
        Equal(false,flow.Progress.Missions.ContainsKey(WorldFiveModeExpansion.HordeRunId));
    }
}
