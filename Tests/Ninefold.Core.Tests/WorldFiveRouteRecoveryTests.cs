using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Content;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Progression;
using Ninefold.Core.Views;
using W=Ninefold.Core.Content.WorldFiveRouteRecovery;

internal static partial class Program
{
    private static IEnumerable<(string Name,Action Run)> WorldFiveRouteRecoveryTests()
    {
        yield return ("Twenty appended authored missions preserve original campaign, reward, template and save compatibility",RecoveryAdditive);
        yield return ("All twenty missions have distinct validated tactical fields, accessible objectives, six enemy roles and narrative beats",RecoveryMapAndNarrativeAudit);
        yield return ("Four patrols have repeatable legal procedural variants; all sixteen narrative layouts remain authored",RecoveryProcedural);
        yield return ("Old active checkpoint and completed chapter receipt reopen without reset, duplicate rewards or premature completion",RecoveryOldSaves);
        yield return ("Every new mission starts, saves, reloads, locks its next stage and supports defeat retry through the real campaign flow",RecoveryEveryEntry);
        yield return ("New stabilize and carried rescue objectives complete through actual player movement, interactions and save reload",RecoveryPlayableObjectives);
    }
    private static readonly Lazy<ContentCatalog> RecoveryCatalog=new Lazy<ContentCatalog>(W.Create);
    private static ContentCatalog Expanded()=>RecoveryCatalog.Value;
    private static string[] RecoveryOldIds(ContentCatalog c)=>c.Campaigns.Single().Missions.Select(n=>n.MissionId).Take(14).ToArray();
    private static void RecoveryAdditive()
    {
        var old=WorldFiveTutorialWildlifeV1.Create();var c=Expanded();
        Equal(old.Manifest.SaveRevision,c.Manifest.SaveRevision);
        var oldIds=RecoveryOldIds(old);var ids=c.Campaigns.Single().Missions.Select(x=>x.MissionId).ToArray();
        Equal(34,ids.Length);Equal(true,ids.Take(14).SequenceEqual(oldIds));
        Equal(true,ids.Skip(14).SequenceEqual(Enumerable.Range(15,20).Select(W.Id)));
        for(int i=1;i<ids.Length;i++)
            Equal(true,c.Campaigns.Single().Missions[i].Prerequisites.SequenceEqual(new[]{ids[i-1]}));
        foreach(var m in old.Missions.Values)
        {
            var next=c.Missions[m.Id];
            Equal(TerrainMapSignature(m.Map),TerrainMapSignature(next.Map));
            Equal(true,m.Deployment.SequenceEqual(next.Deployment));
            Equal(true,m.Actors.Select(a=>a.Id+":"+a.UnitId).SequenceEqual(next.Actors.Select(a=>a.Id+":"+a.UnitId)));
            Equal(m.Objectives[0].Kind,next.Objectives[0].Kind);
            Equal(m.Objectives[0].Required,next.Objectives[0].Required);
            Equal(old.Rewards[m.Id].Revision,c.Rewards[m.Id].Revision);
            Equal(true,old.Rewards[m.Id].FirstClear.Select(g=>g.ResourceId+":"+g.Amount)
                .SequenceEqual(c.Rewards[m.Id].FirstClear.Select(g=>g.ResourceId+":"+g.Amount)));
        }
        Equal(true,old.Units.Keys.SequenceEqual(c.Units.Keys));
        Equal(true,old.StarterUnits.SequenceEqual(c.StarterUnits));
        Equal(3,c.StarterUnits.Count);
    }

    // Conservative 0.5m four-neighbor grid on the flat field, inflated for
    // the largest eligible body. This is a topology/authoring gate, not a
    // claim that a perfect route exists with every live opponent occupying it.
    private static void RecoveryReachability(MissionContent m,BattlefieldMap map,FieldBody[] bodies)
    {
        decimal hw=bodies.Max(b=>b.HalfWidth),hd=bodies.Max(b=>b.HalfDepth);
        bool Legal(FieldPoint p)=>p.X>=map.Bounds.Min.X+hw&&p.X<=map.Bounds.Max.X-hw&&
            p.Z>=map.Bounds.Min.Z+hd&&p.Z<=map.Bounds.Max.Z-hd&&!map.Obstacles.Any(o=>o.BlocksMovement&&
                p.X+hw>=o.Bounds.Min.X&&p.X-hw<=o.Bounds.Max.X&&p.Z+hd>=o.Bounds.Min.Z&&p.Z-hd<=o.Bounds.Max.Z);
        var initial=m.Deployment[0];
        var visited=new HashSet<FieldPoint>{initial};
        var q=new Queue<FieldPoint>();q.Enqueue(initial);
        while(q.Count>0)
        {
            var p=q.Dequeue();
            foreach(var d in new[]{(-.5m,0m),(.5m,0m),(0m,-.5m),(0m,.5m)})
            {
                var next=new FieldPoint(p.X+d.Item1,0,p.Z+d.Item2);
                if(!Legal(next)||!Legal(new FieldPoint((next.X+p.X)/2,0,(next.Z+p.Z)/2)))continue;
                if(visited.Add(next))q.Enqueue(next);
            }
        }
        foreach(var p in m.Deployment.Concat(m.Actors.Select(a=>a.Position)))
            if(!visited.Contains(p))throw new Exception(m.Id+" unreachable spawn "+p.X+","+p.Z);
        var o=m.Objectives[0];
        if(o.Zone!=null&&!visited.Any(p=>p.X-hw>=o.Zone.Min.X&&p.X+hw<=o.Zone.Max.X&&
            p.Z-hd>=o.Zone.Min.Z&&p.Z+hd<=o.Zone.Max.Z))
            throw new Exception(m.Id+" unreachable objective zone");
        if(o.Interaction!=null&&!visited.Any(p=>p.X==o.Interaction.Point.X&&p.Z==o.Interaction.Point.Z))
            throw new Exception(m.Id+" unreachable interaction point");
    }
    private static void RecoveryMapAndNarrativeAudit()
    {
        var c=Expanded();var bodies=ArtBodies(c);
        var report=CampaignProductionAudit.Review(c,c.Campaigns.Single().Id,environmentPrefix:"study-w5-");
        Equal(34,report.MissionCount);Equal(6,report.DistinctPrimaryObjectiveCount);
        Equal(20,W.All.Select(s=>TerrainMapSignature(s.Mission.Map)).Distinct().Count());
        Equal(20,W.All.Select(s=>s.Title).Distinct().Count());
        var types=W.All.SelectMany(s=>s.Mission.Actors.Select(a=>a.UnitId)).ToArray();
        foreach(var id in WorldFiveActOnePrototype.EnemyTemplateIds)Equal(true,types.Contains(id));
        Equal(true,types.Contains(WorldFiveTutorialWildlifeV1.SeamstalkerId));
        Equal(true,types.Contains(WorldFiveTutorialWildlifeV1.SillrunnerId));
        foreach(var s in W.All)
        {
            var m=s.Mission;Equal(true,m.IsCampaign);Equal(2,m.MinimumSquad);
            Equal(true,m.MaximumSquad>=3&&m.MaximumSquad<=4);
            Equal(true,s.Briefing.Length>40&&s.Success.Length>30&&s.Instructions.Length>25);
            Equal(s.Mood,BattlefieldArtPlan.Choose(m.Id,m.EnvironmentKey));
            RecoveryReachability(m,m.Map,bodies);
            CheckArtGeometry(m.Map,BattlefieldArtPlan.Build(m.Map,m.Id,m.EnvironmentKey,ArtAttempt(1),false));
            if(m.Objectives[0].Kind==ObjectiveKind.RescueAndExtract)
                Equal("$leader",m.Objectives[0].ExtractionCarrier);
        }
        Console.WriteLine("INFO continuation: 20 authored maps, "+report.DistinctLayoutCount+
            " distinct full campaign layouts, "+report.DistinctPrimaryObjectiveCount+" primary objective kinds.");
    }
    private static void RecoveryProcedural()
    {
        var c=Expanded();var bodies=ArtBodies(c);
        Equal(4,W.All.Count(s=>s.Mission.ProceduralCover));
        var ids=W.All.Where(s=>s.Mission.ProceduralCover).Select(s=>s.Number).ToArray();
        Equal(true,ids.SequenceEqual(new[]{17,21,26,31}));
        foreach(var ch in W.All)
        {
            var mission=ch.Mission;
            if(!mission.ProceduralCover)
            {Equal(mission.Map,BattlefieldVariation.Resolve(mission,ArtAttempt(23),bodies));continue;}
            var variants=new HashSet<string>();
            for(int i=1;i<=48;i++)
            {
                string attempt=ArtAttempt(i);
                var map=BattlefieldVariation.Resolve(mission,attempt,bodies);
                Equal(TerrainMapSignature(map),TerrainMapSignature(BattlefieldVariation.Resolve(mission,attempt,bodies)));
                Equal(mission.Map.Bounds,map.Bounds);Equal(mission.Map.Obstacles.Count,map.Obstacles.Count);
                for(int j=0;j<map.Obstacles.Count-1;j++)Equal(mission.Map.Obstacles[j],map.Obstacles[j]);
                RecoveryReachability(mission,map,bodies);
                variants.Add(TerrainMapSignature(map));
            }
            if(variants.Count<5)throw new Exception(mission.Id+" insufficient optional geometry variation "+variants.Count);
            Console.WriteLine("INFO "+mission.Id+": "+variants.Count+" safe distinct layouts / 48 seed samples.");
        }
    }

    // Earlier victory receipts are intentionally seeded only in isolated
    // test storage to exercise compatibility/entry gates, never production.
    private static void SeedRecoveryHistory(LocalProgressStore store,ContentCatalog c,int count)
    {
        foreach(var id in c.Campaigns.Single().Missions.Take(count).Select(n=>n.MissionId))
            store.Claim(Result("recovery-history-"+id,id),c.Rewards[id]);
    }
    private static void RecoveryOldSaves()
    {
        var old=WorldFiveTutorialWildlifeV1.Create();var next=Expanded();
        string id=WorldFiveTutorialWildlifeV1.ProfileId;
        var battles=new FakeSaveFiles();var progress=new ProgressFiles();
        var flow=old.CreateFlow(battles,progress,id);flow.CreateProfile(old.StarterUnits);
        flow.Start(RecoveryOldIds(old)[0],old.StarterUnits);
        var saved=flow.ReadBattle();
        var expanded=next.CreateFlow(battles,progress,id);expanded.Open();
        Equal(MissionFlowPhase.Battle,expanded.Phase);
        Equal(saved.Mission.Definition.AttemptId,expanded.ReadBattle().Mission.Definition.AttemptId);
        Equal(TerrainMapSignature(saved.Battlefield.Map),TerrainMapSignature(expanded.ReadBattle().Battlefield.Map));
        Equal(true,expanded.Progress.OwnedUnits.SequenceEqual(flow.Progress.OwnedUnits));
        var profiles=new ProgressFiles();var store=new LocalProgressStore(profiles,id);
        store.Create(old.StarterUnits);SeedRecoveryHistory(store,old,14);
        var previous=old.CreateFlow(new FakeSaveFiles(),profiles,id);previous.Open();
        var receipt=previous.ClaimCampaign(old.Campaigns.Single().Id);
        Equal(false,receipt.AlreadyClaimed);
        long balance=previous.Progress.Balance("fixture-resource");
        var menu=new MissionPresentation(next,new FakeSaveFiles(),profiles,id);menu.Open();
        var view=menu.Read();Equal(34,view.Campaigns.Single().TotalMissions);
        Equal(14,view.Campaigns.Single().CompletedMissions);
        Equal(true,view.Campaigns.Single().RewardsClaimed);Equal(false,view.Campaigns.Single().IsComplete);
        Equal(W.Id(15),view.NextCampaignMission.Definition.Id);
        Equal(balance,view.Progress.Balance("fixture-resource"));
        Equal(true,menu.ClaimCampaign(old.Campaigns.Single().Id).AlreadyClaimed);
        Equal(balance,menu.Read().Progress.Balance("fixture-resource"));
        Equal(false,menu.Read().Progress.Owns("fixture-a-5"));
    }
    private static void RecoveryEveryEntry()
    {
        var c=Expanded();var files=new FakeSaveFiles();var progress=new ProgressFiles();
        const string profile="recovery-every-entry";
        var store=new LocalProgressStore(progress,profile);store.Create(c.StarterUnits);
        SeedRecoveryHistory(store,c,14);
        var flow=c.CreateFlow(files,progress,profile);flow.Open();
        foreach(var ch in W.All)
        {
            string id=ch.Mission.Id;
            Equal(SquadFailure.None,flow.ValidateSquad(id,c.StarterUnits));
            if(ch.Number<W.Last)
                Equal(SquadFailure.MissionLocked,flow.ValidateSquad(W.Id(ch.Number+1),c.StarterUnits));
            flow.Start(id,c.StarterUnits);
            var expected=flow.ReadBattle();
            flow=c.CreateFlow(files,progress,profile);flow.Open();
            var resumed=flow.ReadBattle();
            Equal(id,resumed.Mission.Definition.MissionId);Equal(expected.Mission.Definition.AttemptId,resumed.Mission.Definition.AttemptId);
            Equal(TerrainMapSignature(expected.Battlefield.Map),TerrainMapSignature(resumed.Battlefield.Map));
            Equal(ch.Mission.Objectives[0].Kind,resumed.Mission.Definition.Objectives[0].Kind);
            flow.Execute(b=>b.Mission.Forfeit());
            Equal(MissionFlowPhase.Results,flow.Phase);flow.ClaimRewards();flow.ReturnToSelection();
            Equal(SquadFailure.None,flow.ValidateSquad(id,c.StarterUnits));
            // Now model a hypothetical clear for *next-stage availability*;
            // full playthrough is separately verified for selected objectives.
            store.Claim(Result("recovery-cleared-"+id,id),c.Rewards[id]);flow.Open();
        }
        Equal(34,flow.InspectCampaign(c.Campaigns.Single().Id).CompletedMissions);
        Equal(true,flow.InspectCampaign(c.Campaigns.Single().Id).IsComplete);
        Equal(false,flow.Progress.Owns("fixture-a-5"));
    }
    private static void RecoveryPlayableObjectives()
    {
        var c=Expanded();const string profile="recovery-real-interactions";
        var files=new FakeSaveFiles();var progress=new ProgressFiles();
        var store=new LocalProgressStore(progress,profile);store.Create(c.StarterUnits);
        SeedRecoveryHistory(store,c,14);
        MissionPresentation Open()
        {var m=new MissionPresentation(c,files,progress,profile);m.Open();return m;}
        var menu=Open();
        foreach(int number in new[]{15,16})
        {
            var id=W.Id(number);var objective=c.Missions[id].Objectives[0];
            menu.SelectMission(id);menu.SetSquad(c.StarterUnits);
            Equal(true,menu.Read().CanStart);var play=menu.Start(menu.Read().PlanId);
            int moves=0,interactions=0;bool reopened=false;
            for(int step=0;step<450&&menu.Read().Results==null;step++)
            {
                var view=play.Read().Battle;
                BattleInputResult command;
                if(view.Phase!=BattleSessionPhase.PlayerInput)
                    // Controlled no-perception scenario tests geometry and
                    // interactions, not the full live enemy difficulty.
                    command=play.Advance(new EnemyPerception(Array.Empty<string>(),Array.Empty<string>()));
                else if(view.Activation.UnitId!="fixture-a-1")
                    command=play.EndTurn(view.Activation.ActivationId);
                else
                {
                    bool rescued=view.Objectives[0].Rescued;
                    bool interacted=play.TapObjective("primary").Outcome==InputOutcome.Previewed;
                    if(interacted)
                    {
                        command=play.TapObjective("primary");Equal(InputOutcome.Committed,command.Outcome);
                        interactions++;
                    }
                    else
                    {
                        var destination=number==15?objective.Interaction.Point:
                            rescued?new FieldPoint(-6,0,-3):new FieldPoint(3,0,4);
                        if(play.TapDestination(destination).Outcome==InputOutcome.Previewed)
                        {command=play.TapDestination(destination);Equal(InputOutcome.Committed,command.Outcome);moves++;}
                        else command=play.EndTurn(view.Activation.ActivationId);
                    }
                }
                if(command.AnimationId!=null)Equal(true,play.CompleteAnimation(command.AnimationId));
                if(!reopened&&interactions>0&&menu.Read().Results==null)
                {menu=Open();play=menu.ResumeBattle();reopened=true;}
                if(menu.Read().Results==null&&play.Read().Battle.Phase==BattleSessionPhase.PlayerInput&&
                    play.Read().Battle.Activation.ActivationId==view.Activation?.ActivationId)
                {
                    var end=play.EndTurn(view.Activation.ActivationId);
                    if(end.AnimationId!=null)Equal(true,play.CompleteAnimation(end.AnimationId));
                }
            }
            if(menu.Read().Results==null)throw new Exception(id+" did not finish controlled objective journey.");
            Equal(MissionOutcome.Victory,menu.Read().Results.Result.Outcome);
            Equal(true,moves>0);Equal(true,reopened);Equal(number==15?2:1,interactions);
            var attempt=menu.Read().Results.Result.AttemptId;
            Equal(false,menu.ClaimRewards(attempt).AlreadyClaimed);Equal(true,menu.ClaimRewards(attempt).AlreadyClaimed);
            menu.ReturnToSelection(attempt);menu=Open();
            Equal(W.Id(number+1),menu.Read().NextCampaignMission.Definition.Id);
        }
        Equal(true,menu.Read().Progress.Missions.ContainsKey(W.Id(16)));
    }
}
