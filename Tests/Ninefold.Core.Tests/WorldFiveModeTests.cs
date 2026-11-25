using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Progression;
using Ninefold.Core.Views;

internal static partial class Program
{
    private static IEnumerable<(string Name,Action Run)> WorldFiveModeTests()
    {
        yield return ("All 100 World Five stages and their profile/save revision remain byte-compatible with the mode add-on",ModeSaveCompatibility);
        yield return ("Mode catalog exposes nine world slots, six Horde stages, five branching Pathweave encounters and two large Collision maps",ModeAuthoredGeometry);
        yield return ("Horde wave progression uses durable first victories, allows replays and rejects skipping waves",ModeHordeGate);
        yield return ("Pathweave records an exclusive permanent shelter or containment route and gates different finales",ModePathBranches);
        yield return ("World Collision requires the real 100-mission reward, supports mixed seven-unit squads and resumes offline",ModeCollisionGate);
        yield return ("Horde wave one reaches a real victory with active Ashbound AI, movement, double-tap attacks and a saved checkpoint",ModeHordeLive);
        yield return ("The campaign balance report audits ten windows without modifying stats or certifying production art",ModeQualityAudit);
        yield return ("Mode gate definitions reject hidden campaign overrides and invalid IDs",ModeGateValidation);
    }
    private static readonly Lazy<ContentCatalog> ModeCatalog=new Lazy<ContentCatalog>(WorldFiveModes.Create);
    private static ContentCatalog ModeContent()=>ModeCatalog.Value;
    private static void ClearMode(LocalProgressStore store,ContentCatalog c,string id)
        =>store.Claim(Result("mode-clear-"+id,id),c.Rewards[id]);
    private static void ModeSaveCompatibility()
    {
        var old=WorldFiveFinale.Create();var current=ModeContent();
        Equal(old.Manifest.SaveRevision,current.Manifest.SaveRevision);
        Equal(old.Campaigns.Count,current.Campaigns.Count);
        Equal(true,old.Campaigns.Select(c=>c.Id).SequenceEqual(current.Campaigns.Select(c=>c.Id)));
        Equal(13,WorldFiveModes.Stages.Count);
        Equal(old.Missions.Count+13,current.Missions.Count);
        Equal(old.Rewards.Count+13,current.Rewards.Count);
        foreach(var m in old.Missions.Values)
        {
            var n=current.Missions[m.Id];
            Equal(TerrainMapSignature(m.Map),TerrainMapSignature(n.Map));
            Equal(m.MinimumSquad,n.MinimumSquad);Equal(m.Encounter,n.Encounter);
            Equal(m.Objectives.Count,n.Objectives.Count);
            Equal(true,m.Deployment.SequenceEqual(n.Deployment));
            Equal(old.Rewards[m.Id].Revision,current.Rewards[m.Id].Revision);
        }
        var files=new FakeSaveFiles();var progress=new ProgressFiles();
        const string profile="mode-save-compat";
        var store=new LocalProgressStore(progress,profile);store.Create(old.StarterUnits);
        var existing=old.CreateFlow(files,progress,profile);existing.Open();
        existing.Start(WorldFiveOpeningTenPrototype.MissionIds[0],old.StarterUnits);
        string attempt=existing.ReadBattle().Mission.Definition.AttemptId;
        var upgraded=current.CreateFlow(files,progress,profile);upgraded.Open();
        Equal(MissionFlowPhase.Battle,upgraded.Phase);
        Equal(attempt,upgraded.ReadBattle().Mission.Definition.AttemptId);
        Equal(true,upgraded.Progress.OwnedUnits.SequenceEqual(old.StarterUnits));
    }
    private static void ModeAuthoredGeometry()
    {
        var c=ModeContent();var bodies=ArtBodies(c);
        Equal(9,WorldFiveModes.WorldCampaigns.Count);
        Equal(1,WorldFiveModes.WorldCampaigns.Count(w=>w.Authored));
        Equal("The Brightfall Reach",WorldFiveModes.WorldCampaigns[0].LockedName);
        Equal("The Shattered Heights",WorldFiveModes.WorldCampaigns[1].LockedName);
        Equal("The Fractured Meridian",WorldFiveModes.WorldCampaigns[4].LockedName);
        Equal(6,WorldFiveModes.Stages.Count(s=>s.Mode==FrontierMode.Horde));
        Equal(5,WorldFiveModes.Stages.Count(s=>s.Mode==FrontierMode.Pathweave));
        Equal(2,WorldFiveModes.Stages.Count(s=>s.Mode==FrontierMode.WorldCollision));
        Equal(13,WorldFiveModes.Stages.Select(s=>TerrainMapSignature(c.Missions[s.Id].Map)).Distinct().Count());
        foreach(var stage in WorldFiveModes.Stages)
        {
            var m=c.Missions[stage.Id];Equal(false,m.IsCampaign);
            Equal(true,m.EnvironmentKey.StartsWith("study-w5-",StringComparison.Ordinal));
            Equal(true,m.Map.Obstacles.Count>=3);
            Equal(true,m.Actors.Any(x=>x.IsHostile));
            Equal(true,c.Rewards[m.Id].FirstClear.Any(g=>g.Amount>0));
            RecoveryReachability(m,m.Map,bodies);
            if(stage.Mode==FrontierMode.WorldCollision)
            {
                Equal(EncounterKind.WorldCollision,m.Encounter);
                Equal(7,m.MaximumSquad);Equal(6,m.MinimumSquad);
                Equal(true,m.Map.Bounds.Max.X-m.Map.Bounds.Min.X>35);
            }
            else
            {
                Equal(EncounterKind.Standard,m.Encounter);
                Equal(4,m.MaximumSquad);
            }
        }
        for(int w=1;w<=6;w++)
        {
            var m=c.Missions[WorldFiveModes.HordeId(w)];
            Equal(w+1,m.Actors.Count(a=>a.IsHostile));
            Equal(ObjectiveKind.Defeat,m.Objectives[0].Kind);
        }
        Equal(3,c.Missions[WorldFiveModes.PathShelterEnd].Objectives.Count);
        Equal(3,c.Missions[WorldFiveModes.PathSealEnd].Objectives.Count);
        Equal(0,WorldFiveModes.Stages.Count(s=>s.Name.ToLowerInvariant().Contains("challenge")));
    }
    private static void ModeHordeGate()
    {
        var c=ModeContent();var progress=new ProgressFiles();
        const string profile="mode-horde-gate";
        var store=new LocalProgressStore(progress,profile);store.Create(c.StarterUnits);
        var flow=c.CreateFlow(new FakeSaveFiles(),progress,profile);flow.Open();
        Equal(SquadFailure.None,flow.ValidateSquad(WorldFiveModes.HordeId(1),c.StarterUnits));
        Equal(SquadFailure.MissionLocked,flow.ValidateSquad(WorldFiveModes.HordeId(2),c.StarterUnits));
        Throws<InvalidOperationException>(()=>flow.Start(WorldFiveModes.HordeId(6),c.StarterUnits));
        for(int w=1;w<=6;w++)
        {
            var id=WorldFiveModes.HordeId(w);
            flow.Open();Equal(SquadFailure.None,flow.ValidateSquad(id,c.StarterUnits));
            if(w<6)Equal(SquadFailure.MissionLocked,flow.ValidateSquad(WorldFiveModes.HordeId(w+1),c.StarterUnits));
            ClearMode(store,c,id);
        }
        flow.Open();Equal(SquadFailure.None,flow.ValidateSquad(WorldFiveModes.HordeId(1),c.StarterUnits));
        Equal(6,WorldFiveModes.Stages.Count(x=>x.Mode==FrontierMode.Horde&&flow.Progress.Missions.ContainsKey(x.Id)));
        Equal(0,flow.Progress.CampaignClaims.Count);
    }
    private static void ModePathBranches()
    {
        var c=ModeContent();
        foreach(bool shelter in new[]{true,false})
        {
            var progress=new ProgressFiles();string profile="mode-branch-"+shelter;
            var store=new LocalProgressStore(progress,profile);store.Create(c.StarterUnits);
            var flow=c.CreateFlow(new FakeSaveFiles(),progress,profile);flow.Open();
            Equal(SquadFailure.MissionLocked,flow.ValidateSquad(WorldFiveModes.PathShelter,c.StarterUnits));
            Equal(SquadFailure.MissionLocked,flow.ValidateSquad(WorldFiveModes.PathSeal,c.StarterUnits));
            ClearMode(store,c,WorldFiveModes.PathEntry);
            flow.Open();Equal(SquadFailure.None,flow.ValidateSquad(WorldFiveModes.PathShelter,c.StarterUnits));
            Equal(SquadFailure.None,flow.ValidateSquad(WorldFiveModes.PathSeal,c.StarterUnits));
            string chosen=shelter?WorldFiveModes.PathShelter:WorldFiveModes.PathSeal;
            string other=shelter?WorldFiveModes.PathSeal:WorldFiveModes.PathShelter;
            string end=shelter?WorldFiveModes.PathShelterEnd:WorldFiveModes.PathSealEnd;
            string otherEnd=shelter?WorldFiveModes.PathSealEnd:WorldFiveModes.PathShelterEnd;
            ClearMode(store,c,chosen);flow.Open();
            Equal(shelter?"SHELTER":"CONTAINMENT",WorldFiveModes.ChosenPath(flow.Progress));
            Equal(SquadFailure.None,flow.ValidateSquad(chosen,c.StarterUnits));
            Equal(SquadFailure.MissionLocked,flow.ValidateSquad(other,c.StarterUnits));
            Equal(SquadFailure.None,flow.ValidateSquad(end,c.StarterUnits));
            Equal(SquadFailure.MissionLocked,flow.ValidateSquad(otherEnd,c.StarterUnits));
            ClearMode(store,c,end);
            var next=c.CreateFlow(new FakeSaveFiles(),progress,profile);next.Open();
            Equal(false,next.CampaignCatalog.IsMissionAvailable(other,next.Progress));
            Equal(true,next.CampaignCatalog.IsMissionAvailable(end,next.Progress));
        }
    }
    private static void ModeCollisionGate()
    {
        var c=ModeContent();var progress=new ProgressFiles();const string profile="mode-collision-gate";
        var store=new LocalProgressStore(progress,profile);store.Create(c.StarterUnits);
        var saves=new FakeSaveFiles();var flow=c.CreateFlow(saves,progress,profile);flow.Open();
        var unearned=new[]{"fixture-a-1","fixture-a-2","fixture-a-3"};
        Equal(SquadFailure.MissionLocked,flow.ValidateSquad(WorldFiveModes.CollisionEntry,unearned));
        var main=c.Campaigns.Single(x=>x.Id==WorldFivePlayableSlice.CampaignId);
        foreach(var node in main.Missions)
            store.Claim(Result("collision-history-"+node.MissionId,node.MissionId),c.Rewards[node.MissionId]);
        flow.Open();
        Equal(false,flow.Progress.Owns("fixture-a-5"));
        Equal(SquadFailure.MissionLocked,flow.ValidateSquad(WorldFiveModes.CollisionEntry,unearned));
        var receipt=flow.ClaimCampaign(main.Id);
        Equal(WorldFiveFinale.CompletionReceiptId,receipt.Receipt.CampaignId);
        flow.Open();Equal(true,flow.Progress.Owns("fixture-a-5"));
        Equal(true,flow.Progress.Owns("fixture-b-3"));
        Equal(true,flow.CampaignCatalog.IsMissionAvailable(WorldFiveModes.CollisionEntry,flow.Progress));
        string[] squad={"fixture-a-1","fixture-a-2","fixture-a-3","fixture-a-5","fixture-b-1","fixture-b-2","fixture-b-3"};
        Equal(SquadFailure.None,flow.ValidateSquad(WorldFiveModes.CollisionEntry,squad));
        Equal(SquadFailure.Size,flow.ValidateSquad(WorldFiveModes.CollisionEntry,squad.Take(5).ToArray()));
        Equal(SquadFailure.MissionLocked,flow.ValidateSquad(WorldFiveModes.CollisionEnd,squad));
        flow.Start(WorldFiveModes.CollisionEntry,squad);
        var before=flow.ReadBattle();Equal(7,before.Mission.Definition.Squad.Count);
        var resumed=c.CreateFlow(saves,progress,profile);resumed.Open();
        Equal(MissionFlowPhase.Battle,resumed.Phase);
        Equal(before.Mission.Definition.AttemptId,resumed.ReadBattle().Mission.Definition.AttemptId);
        resumed.Execute(b=>b.Mission.Forfeit());resumed.ClaimRewards();resumed.ReturnToSelection();
        Equal(SquadFailure.MissionLocked,resumed.ValidateSquad(WorldFiveModes.CollisionEnd,squad));
        ClearMode(store,c,WorldFiveModes.CollisionEntry);resumed.Open();
        Equal(SquadFailure.None,resumed.ValidateSquad(WorldFiveModes.CollisionEnd,squad));
        resumed.Start(WorldFiveModes.CollisionEnd,squad);
        Equal(EncounterKind.WorldCollision,c.Missions[WorldFiveModes.CollisionEnd].Encounter);
    }
    private static void ModeHordeLive()
    {
        var c=ModeContent();var files=new FakeSaveFiles();var progress=new ProgressFiles();
        const string profile="mode-horde-live";
        MissionPresentation Open()
        {
            var m=new MissionPresentation(c,files,progress,profile);
            if(progress.Read(0)==null&&progress.Read(1)==null)m.CreateProfile();
            else m.Open();
            return m;
        }
        var menu=Open();var id=WorldFiveModes.HordeId(1);
        menu.SelectMission(id);menu.SetSquad(c.StarterUnits);
        Equal(true,menu.Read().CanStart);
        var play=menu.Start(menu.Read().PlanId);
        int attacks=0,moves=0,enemyAdvances=0;bool reopened=false;
        for(int step=0;step<500&&menu.Read().Results==null;step++)
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
                var position=actor.Position.Value;
                var offsets=new[]{(0m,1.5m),(1.5m,0m),(0m,-1.5m),(-1.5m,0m),
                    (1.5m,1.5m),(-1.5m,1.5m),(1.5m,-1.5m),(-1.5m,-1.5m)};
                var destinations=v.Units.Where(u=>u.IsEnemyControlled&&u.IsEligible&&u.Position.HasValue)
                    .SelectMany(e=>offsets.Select(o=>new FieldPoint(e.Position.Value.X+o.Item1,0,e.Position.Value.Z+o.Item2)))
                    .OrderBy(g=>DistanceSquared(position,g));
                foreach(var goal in destinations)
                {
                    if(goal.Equals(position)||play.TapDestination(goal).Outcome!=InputOutcome.Previewed)continue;
                    var moved=play.TapDestination(goal);Equal(InputOutcome.Committed,moved.Outcome);
                    if(moved.AnimationId!=null)Equal(true,play.CompleteAnimation(moved.AnimationId));moves++;break;
                }
            }
            if(!reopened&&moves>0&&menu.Read().Results==null){menu=Open();play=menu.ResumeBattle();reopened=true;}
            if(menu.Read().Results==null&&play.Read().Battle.Phase==BattleSessionPhase.PlayerInput&&
                play.Read().Battle.Activation.ActivationId==activation)
            {
                var end=play.EndTurn(activation);
                if(end.AnimationId!=null)Equal(true,play.CompleteAnimation(end.AnimationId));
            }
        }
        if(menu.Read().Results==null)throw new Exception("Horde wave one did not terminate through real play.");
        Equal(MissionOutcome.Victory,menu.Read().Results.Result.Outcome);
        Equal(true,attacks>=2&&moves>0&&enemyAdvances>0&&reopened);
        var attempt=menu.Read().Results.Result.AttemptId;
        Equal(false,menu.ClaimRewards(attempt).AlreadyClaimed);
        menu.ReturnToSelection(attempt);
        var resumed=Open();Equal(true,resumed.Read().Missions.Single(m=>m.Definition.Id==id).IsReplay);
        Equal(true,resumed.Read().Missions.Single(m=>m.Definition.Id==WorldFiveModes.HordeId(2)).Available);
        Equal(0,resumed.Read().Progress.CampaignClaims.Count);
    }
    private static void ModeQualityAudit()
    {
        var c=ModeContent();
        var q=WorldFiveBalanceAudit.Review(c);
        Equal(100,q.AuthoredMissions);Equal(10,q.Windows.Count);
        Equal(1,q.Windows[0].From);Equal(100,q.Windows[9].To);
        Equal(true,q.Windows.All(w=>w.TotalHostiles>=0&&w.HighestOpposition<=7));
        Equal(true,q.Windows.Any(w=>w.MultiStage>0));
        Equal(true,q.Windows.Any(w=>w.ProtectedMissions>0));
        Equal(false,q.ReleaseReady);
        Equal(true,q.ReleaseBlockers.Any(b=>b.Contains("fixture")));
        Console.WriteLine("INFO Balance: "+string.Join(", ",q.Windows.Select(w=>
            w.From+"–"+w.To+" enemies "+w.TotalHostiles+" multi-stage "+w.MultiStage+" first-clear "+w.FirstClearSupplies)));
        Console.WriteLine("INFO Release blockers: "+string.Join(" | ",q.ReleaseBlockers));
    }
    private static void ModeGateValidation()
    {
        var c=ModeContent();
        var invalid=new Dictionary<string,Func<PlayerProgress,bool>>{{"w5-story-100",_=>true}};
        Throws<ArgumentException>(()=>new ContentCatalog(c.Manifest,c.Resources,c.Abilities.Values,c.Kits.Values,
            c.Units.Values,c.Missions.Values,c.Rewards.Values,c.Campaigns,c.StarterUnits,c.Statuses.Values,invalid));
        var missing=new Dictionary<string,Func<PlayerProgress,bool>>{{"missing-mode",_=>true}};
        Throws<ArgumentException>(()=>new ContentCatalog(c.Manifest,c.Resources,c.Abilities.Values,c.Kits.Values,
            c.Units.Values,c.Missions.Values,c.Rewards.Values,c.Campaigns,c.StarterUnits,c.Statuses.Values,missing));
        Throws<ArgumentOutOfRangeException>(()=>WorldFiveModes.HordeId(7));
    }
}
