using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Content;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Progression;

internal static partial class Program
{
    private static (string Name, Action Run)[] ContentTests() => new (string, Action)[] {
        ("Content fixture drives campaign handoff and shared progression", ContentFullFlow),
        ("Content battle resumes without rebuilding or duplicating rewards", ContentResume),
        ("Content enemy templates are not collectible", ContentEnemy),
        ("Content permits allied healing across factions", ContentMixedHealing),
        ("Content rejects duplicate and reserved IDs", ContentIdsInvalid),
        ("Content rejects missing kits and slot mismatches", ContentKitsInvalid),
        ("Content rejects unknown resource references", ContentResourcesInvalid),
        ("Content rejects reward policies without missions", ContentRewardsInvalid),
        ("Content rejects unknown actor templates and colliding IDs", ContentActorsInvalid),
        ("Content rejects unknown objective actors", ContentObjectivesInvalid),
        ("Content rejects ambiguous aliases and single-target squad alias", ContentAliasesInvalid),
        ("Content binds all six objective kinds", ContentObjectiveKinds),
        ("Content rejects overlapping and out-of-bounds spawns", ContentGeometryInvalid),
        ("Content checks every eligible body not just starter bodies", ContentLargeBody),
        ("Content rejects blocked deployment and invalid guard area", ContentBlocked),
        ("Content rejects objectives outside battlefield", ContentObjectiveGeometry),
        ("Content rejects impossible squad and starter configurations", ContentSquadsInvalid),
        ("Content revision binding is framed and stable", ContentRevisions),
        ("Content requires canon source without implying canon approval", ContentCanon),
        ("Content definition collections cannot be changed after validation", ContentImmutable),
        ("Content rejects unsupported signature bindings", ContentExternalSignature),
        ("Content reports which mission failed validation", ContentDiagnostic)
    };
    private static ContentCatalog Repackage(ContentCatalog c, IEnumerable<AbilityContent> abilities = null,
        IEnumerable<KitContent> kits = null, IEnumerable<UnitContent> units = null, IEnumerable<MissionContent> missions = null,
        IEnumerable<MissionRewardPolicy> rewards = null, IEnumerable<string> resources = null, IEnumerable<string> starters = null,
        ContentManifest manifest = null)
        => new ContentCatalog(manifest ?? c.Manifest, resources ?? c.Resources, abilities ?? c.Abilities.Values, kits ?? c.Kits.Values,
            units ?? c.Units.Values, missions ?? c.Missions.Values, rewards ?? c.Rewards.Values, c.Campaigns, starters ?? c.StarterUnits);
    private static MissionContent ContentMission(ContentCatalog c, ObjectiveDefinition objective = null,
        IEnumerable<ActorSpawn> actors = null, IEnumerable<FieldPoint> deployment = null, BattlefieldMap map = null)
    {
        var m = c.Missions["fixture-mixed"];
        return new MissionContent(m.Id, null, false, 1, map ?? m.Map, deployment ?? m.Deployment,
            actors ?? m.Actors, objective ?? m.Objectives[0]);
    }
    private static ContentCatalog ReplaceMission(ContentCatalog c, MissionContent m)
        => Repackage(c, missions: c.Missions.Values.Where(x => x.Id != m.Id).Concat(new[] { m }));
    private static MissionFlow ContentFlow(ContentCatalog c)
    {
        var f = c.CreateFlow(new FakeSaveFiles(), new ProgressFiles(), "content-profile"); f.CreateProfile(c.StarterUnits); return f;
    }
    private static void ContentClear(MissionFlow f, string id, params string[] squad)
    { f.Start(id, squad); WinFlow(f); f.ClaimRewards(); f.ReturnToSelection(); }
    private static void ContentFullFlow()
    {
        var c = AbstractContentPackage.Create(); var f = ContentFlow(c);
        Equal(SquadFailure.MissionLocked, f.ValidateSquad("fixture-finale", c.StarterUnits));
        ContentClear(f, "fixture-opening", "fixture-a-1");
        f.AdvanceUnit("advance", "fixture-a-1"); f.CustomizeUnit("customize", "fixture-a-1", "fixture-health");
        f.UnlockUnit("unlock", "fixture-a-4");
        f.Start("fixture-finale", new[] { "fixture-a-1" });
        Equal(110, f.ReadBattle().Health.GetState("fixture-a-1").MaximumHealth);
        Equal(20, f.ReadBattle().Health.GetState("fixture-a-1").Armor);
        WinFlow(f); f.ClaimRewards(); f.ReturnToSelection(); f.ClaimCampaign("fixture-starter");
        Equal(true, f.Progress.Owns("fixture-a-5")); Equal(true, f.Progress.Owns("fixture-b-3"));
        Equal(false, f.Progress.Owns("fixture-b-5"));
        ContentClear(f, "fixture-next", "fixture-b-1"); f.ClaimCampaign("fixture-followup");
        Equal(false, f.Progress.Owns("fixture-b-5"));
        Equal(SquadFailure.None, f.ValidateSquad("fixture-mixed", new[] { "fixture-a-1", "fixture-b-1", "fixture-a-5" }));
        Equal(1, f.Progress.GetAdvancement("fixture-a-1").Rank);
    }
    private static void ContentResume()
    {
        var c = AbstractContentPackage.Create(); var b = new FakeSaveFiles(); var p = new ProgressFiles();
        var f = c.CreateFlow(b,p,"p"); f.CreateProfile(c.StarterUnits); f.Start("fixture-opening", new[] { "fixture-a-2" });
        var resumed = c.CreateFlow(b,p,"p"); resumed.Open(); Equal("fixture-a-2", resumed.ReadBattle().CurrentActivation.UnitId);
        WinFlow(resumed); var results = c.CreateFlow(b,p,"p"); results.Open(); Equal(MissionFlowPhase.Results, results.Phase);
        results.ClaimRewards(); var claimed = c.CreateFlow(b,p,"p"); claimed.Open(); Equal(MissionFlowPhase.Selection, claimed.Phase);
        Equal(10L, claimed.Progress.Balance("fixture-resource")); Equal(1, claimed.Progress.Claims.Count);
    }
    private static void ContentEnemy()
    {
        var c=AbstractContentPackage.Create(); var f=ContentFlow(c);
        Equal(SquadFailure.UnknownUnit,f.ValidateSquad("fixture-mixed",new[] { "fixture-npc" }));
        f.Start("fixture-mixed",new[] { "fixture-a-1" });
        f.Execute(b => {
            Equal(true,b.Battlefield.TryApplyEffect(b.CurrentActivation.ActivationId,AbilitySlot.NormalAttack,"fixture-enemy-1",out _,out _,out _));
            Equal(40,b.Health.GetState("fixture-enemy-1").CurrentHealth);
            b.EndActivation(b.CurrentActivation.ActivationId); b.BeginNextActivation();
            Equal(true,b.Battlefield.TryRunEnemyTurn(b.CurrentActivation.ActivationId,new[] { "fixture-a-1" },out _,out _));
        });
    }
    private static void ContentMixedHealing()
    {
        var c=AbstractContentPackage.Create(); var f=ContentFlow(c);
        ContentClear(f,"fixture-opening","fixture-a-1"); ContentClear(f,"fixture-finale","fixture-a-1"); f.ClaimCampaign("fixture-starter");
        f.Start("fixture-mixed",new[] { "fixture-a-1","fixture-b-1" });
        // An undamaged ally still passes compatibility; health resolution reports no missing health separately.
        f.Execute(b => { Equal(false,b.Battlefield.TryApplyEffect(b.CurrentActivation.ActivationId,AbilitySlot.Main,"fixture-b-1",out _,out _,out var failure)); Equal(HealthActionFailure.AlreadyFullHealth,failure); });
    }
    private static void ContentIdsInvalid()
    {
        var c=AbstractContentPackage.Create(); Throws<ArgumentException>(()=>Repackage(c,units:c.Units.Values.Concat(new[] { c.Units.Values.First() })));
        Throws<ArgumentException>(()=>AbilityContent.PassivePlaceholder("$squad"));
    }
    private static void ContentKitsInvalid()
    {
        var c=AbstractContentPackage.Create(); Throws<ArgumentException>(()=>Repackage(c,kits:Array.Empty<KitContent>()));
        var k=new KitContent("fixture-kit","fixture-normal","fixture-main","fixture-signature","fixture-passive",new UnitAbilityDefinition(2,SignatureReadiness.ReadyAtDeployment));
        Throws<ArgumentException>(()=>Repackage(c,kits:new[] { k }));
        Throws<ArgumentException>(()=>Repackage(c,abilities:c.Abilities.Values.Skip(1)));
    }
    private static void ContentResourcesInvalid()
    {
        var c=AbstractContentPackage.Create();
        foreach(var resource in new[] { "fixture-resource","fragment-fixture-b-5" })
            Throws<ArgumentException>(()=>Repackage(c,resources:c.Resources.Where(r=>r!=resource)));
    }
    private static void ContentRewardsInvalid()
    {
        var c=AbstractContentPackage.Create(); Throws<ArgumentException>(()=>Repackage(c,rewards:c.Rewards.Values.Skip(1)));
        Throws<ArgumentException>(()=>Repackage(c,rewards:c.Rewards.Values.Concat(new[] { new MissionRewardPolicy("missing","v",Array.Empty<ResourceGrant>(),Array.Empty<ResourceGrant>()) })));
    }
    private static void ContentActorsInvalid()
    {
        var c=AbstractContentPackage.Create();
        foreach(var a in new[] { new ActorSpawn("fixture-enemy-1","missing",P(6,0),false), new ActorSpawn("fixture-a-1","fixture-npc",P(6,0),false) })
            Throws<ArgumentException>(()=>ReplaceMission(c,ContentMission(c,actors:new[] { a })));
    }
    private static void ContentObjectivesInvalid()
    {
        var c=AbstractContentPackage.Create();
        foreach(string id in new[] { "unknown","$unknown","fixture-a-1" })
            Throws<ArgumentException>(()=>ReplaceMission(c,ContentMission(c,ObjectiveDefinition.Defeat("primary",new[] { id }))));
    }
    private static void ContentAliasesInvalid()
    {
        var c=AbstractContentPackage.Create();
        foreach(var o in new[] { ObjectiveDefinition.Protect("primary","$squad",2),
            ObjectiveDefinition.Defeat("primary",new[] { "$squad","$leader" }),
            ObjectiveDefinition.Secure("primary",Box(-2,-2,8,8),new[] { "$squad" },new[] { "$leader" },1,false) })
            Throws<ArgumentException>(()=>ReplaceMission(c,ContentMission(c,o)));
    }
    private static void ContentObjectiveKinds()
    {
        var c=AbstractContentPackage.Create(); var interaction=new InteractionDefinition(P(0,0),5,new[] { "$squad" });
        var objectives=new[] { ObjectiveDefinition.Defeat("primary",new[] { "fixture-enemy-1" }), ObjectiveDefinition.Protect("primary","$leader",1),
            ObjectiveDefinition.Rescue("primary","$leader",Box(-2,-2,8,8),interaction),
            ObjectiveDefinition.Secure("primary",Box(-2,-2,8,8),new[] { "$squad" },new[] { "fixture-enemy-1" },1,true),
            ObjectiveDefinition.Stabilize("primary",1,interaction), ObjectiveDefinition.Survive("primary",1) };
        foreach(var o in objectives)
        {
            var catalog=ReplaceMission(c,ContentMission(c,o)); var f=ContentFlow(catalog); f.Start("fixture-mixed",new[] { "fixture-a-2","fixture-a-3" });
            Equal(o.Kind,f.ReadBattle().Mission.GetObjective("primary").Kind);
        }
    }
    private static void ContentGeometryInvalid()
    {
        var c=AbstractContentPackage.Create();
        foreach(var points in new[] { new[] { P(0,0),P(0,0),P(4,0) }, new[] { P(20,20),P(2,0),P(4,0) } })
            Throws<ArgumentException>(()=>ReplaceMission(c,ContentMission(c,deployment:points)));
    }
    private static void ContentLargeBody()
    {
        var c=AbstractContentPackage.Create(); var u=c.Units["fixture-b-5"];
        var large=new UnitContent(u.Id,u.KitId,u.Health,u.Armor,u.Initiative,u.Movement,new FieldBody(2,2,2,P(0,0),P(0,0)),u.Roster);
        Throws<ArgumentException>(()=>Repackage(c,units:c.Units.Values.Where(x=>x.Id!=u.Id).Concat(new[] { large })));
    }
    private static void ContentBlocked()
    {
        var c=AbstractContentPackage.Create(); var map=new BattlefieldMap(c.Missions["fixture-mixed"].Map.Bounds,new[] { new FieldObstacle(Box(-1,-1,1,1),true,true) });
        Throws<ArgumentException>(()=>ReplaceMission(c,ContentMission(c,map:map)));
        var spawn=new ActorSpawn("fixture-enemy-1","fixture-npc",P(6,0),true,new EnemyBehavior(EnemyStyle.Defensive,2,Box(-2,-2,2,2)));
        Throws<ArgumentException>(()=>ReplaceMission(c,ContentMission(c,actors:new[] { spawn })));
    }
    private static void ContentObjectiveGeometry()
    {
        var c=AbstractContentPackage.Create();
        foreach(var o in new[] { ObjectiveDefinition.Stabilize("primary",1,new InteractionDefinition(P(90,90),2,new[] { "$squad" })),
            ObjectiveDefinition.Secure("primary",Box(80,80,90,90),new[] { "$squad" },Array.Empty<string>(),1,false) })
            Throws<ArgumentException>(()=>ReplaceMission(c,ContentMission(c,o)));
    }
    private static void ContentSquadsInvalid()
    {
        var c=AbstractContentPackage.Create();
        foreach(var starters in new[] { new[] { "fixture-a-1","fixture-a-2","fixture-a-5" }, new[] { "fixture-b-1","fixture-b-2","fixture-b-3" } })
            Throws<ArgumentException>(()=>Repackage(c,starters:starters));
        var m=c.Missions["fixture-opening"];
        var oversized=new MissionContent(m.Id,m.FactionId,true,1,m.Map,Enumerable.Range(0,6).Select(i=>P(i*2,0)),m.Actors,m.Objectives[0]);
        Throws<ArgumentException>(()=>ReplaceMission(c,oversized));
    }
    private static void ContentRevisions()
    {
        ContentManifest M(string p,string v,string balance="b")=>new ContentManifest(p,v,balance,ContentClassification.AbstractFixture);
        Equal(M("p","v").SaveRevision,M("p","v").SaveRevision); Equal(false,M("p","v").SaveRevision==M("p","v","b2").SaveRevision);
        Equal(false,M("a:b","c").SaveRevision==M("a","b:c").SaveRevision);
        var c=AbstractContentPackage.Create(); var b=new FakeSaveFiles(); var p=new ProgressFiles(); var f=c.CreateFlow(b,p,"p"); f.CreateProfile(c.StarterUnits); f.Start("fixture-opening",new[] { "fixture-a-1" });
        var revised=Repackage(c,manifest:M("abstract-flow","fixture-v2"));
        Throws<Exception>(()=>revised.CreateFlow(b,p,"p").Open());
    }
    private static void ContentCanon()
    {
        Throws<ArgumentException>(()=>new ContentManifest("p","v","b",ContentClassification.CanonCandidate));
        var c=AbstractContentPackage.Create(); Equal(ContentClassification.AbstractFixture,c.Manifest.Classification); Equal(true,c.ContainsPassivePlaceholders);
    }
    private static void ContentImmutable()
    {
        var c=AbstractContentPackage.Create(); var units=c.Units.Values.ToArray(); var copy=Repackage(c,units:units); units[0]=null;
        Equal(11,copy.Units.Count); Throws<NotSupportedException>(()=>((IDictionary<string,UnitContent>)copy.Units).Clear());
        Throws<NotSupportedException>(()=>((IList<string>)copy.Kits["fixture-kit"].AbilityIds).Clear());
    }
    private static void ContentExternalSignature()
        => Throws<ArgumentException>(()=>new KitContent("k","p","n","m","s",new UnitAbilityDefinition(1,SignatureReadiness.ExternalCondition)));
    private static void ContentDiagnostic()
    {
        var c=AbstractContentPackage.Create();
        try { ReplaceMission(c,ContentMission(c,ObjectiveDefinition.Defeat("primary",new[] { "unknown" }))); throw new Exception("Expected validation error."); }
        catch(ArgumentException e) { Equal(true,e.Message.Contains("mission fixture-mixed")); }
    }
}
