using System;
using System.Linq;
using Ninefold.Core.Content;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;

internal static partial class Program
{
    private static (string Name,Action Run)[] OpeningRouteReadinessTests() =>
        new (string,Action)[] {
            ("All ten World Five prototype objectives have a static body-valid approach", OpeningRoutesTen),
            ("Static route review rejects a blocked mandatory interaction", OpeningRoutesBlocked),
            ("Static route review rejects an inaccessible carrier-extraction zone", OpeningRoutesNoExit),
            ("Static route audit rejects duplicate and unknown mission IDs", OpeningRoutesValidation),
            ("Static route audit never mutates the catalog or V4 save identifiers", OpeningRoutesReadOnly)
        };

    private static FieldPoint AuditP(decimal x,decimal z) => new FieldPoint(x,0,z);
    private static FieldBox AuditB(decimal x0,decimal z0,decimal x1,decimal z1,decimal height=5)
        => new FieldBox(AuditP(x0,z0),new FieldPoint(x1,height,z1));
    private static BattlefieldMap AuditSplitField()
    {
        // The wall touches every possible route clearance along both north
        // and south edges (field bodies are wider than the remaining slit).
        return new BattlefieldMap(AuditB(-5,-5,5,5),new [] {
            new FieldObstacle(AuditB(-.25m,-4.75m,.25m,4.75m,2),true,true)
        });
    }

    private static void OpeningRoutesTen()
    {
        var catalog=WorldFiveOpeningTenPrototype.Create();
        var report=OpeningRouteReadiness.Review(catalog,WorldFiveOpeningTenPrototype.MissionIds);
        Equal(10,report.MissionsChecked);
        Equal(true,report.Passes);
        Equal(0,report.Findings.Count);
        var second=OpeningRouteReadiness.Review(catalog,WorldFiveOpeningTenPrototype.MissionIds);
        Equal(true,second.Passes);
        Equal(report.Findings.Count,second.Findings.Count);
    }

    private static void OpeningRoutesBlocked()
    {
        var catalog=WorldFiveOpeningTenPrototype.Create();
        var m=new MissionContent("route-test-blocked","fixture-a",true,1,
            AuditSplitField(),new[] { AuditP(-3,0) },
            Array.Empty<ActorSpawn>(),
            ObjectiveDefinition.Stabilize("primary",1,
                new InteractionDefinition(AuditP(3,0),1.4m,new[]{"$squad"})));
        var first=catalog.Units[catalog.StarterUnits[0]];
        var problems=OpeningRouteReadiness.ReviewMission(m,first.Body,12m,catalog.Units);
        Equal(true,problems.Any(p=>p.Code=="INTERACTION_UNREACHABLE"));
        Equal(true,problems.All(p=>p.MissionId=="route-test-blocked"));
    }

    private static void OpeningRoutesNoExit()
    {
        var catalog=WorldFiveOpeningTenPrototype.Create();
        var m=new MissionContent("route-test-no-exit","fixture-a",true,1,
            AuditSplitField(),new[] { AuditP(-3,0) },
            new[] { new ActorSpawn("route-rescue-actor","fixture-npc",AuditP(-2,0),false) },
            ObjectiveDefinition.RescueCarried("primary","route-rescue-actor",
                AuditB(2,-1,4,1,3),
                new InteractionDefinition(AuditP(-2,0),1.4m,new[]{"$leader"}),
                "$leader"));
        var first=catalog.Units[catalog.StarterUnits[0]];
        var problems=OpeningRouteReadiness.ReviewMission(m,first.Body,12m,catalog.Units);
        Equal(false,problems.Any(p=>p.Code=="INTERACTION_UNREACHABLE"));
        Equal(true,problems.Any(p=>p.Code=="EXTRACTION_UNREACHABLE"));
    }

    private static void OpeningRoutesValidation()
    {
        var c=WorldFiveOpeningTenPrototype.Create();
        Throws<ArgumentException>(()=>OpeningRouteReadiness.Review(c,
            new[]{WorldFiveOpeningTenPrototype.MissionIds[0],WorldFiveOpeningTenPrototype.MissionIds[0]}));
        Throws<ArgumentException>(()=>OpeningRouteReadiness.Review(c,
            new[]{"mission-does-not-exist"}));
        Throws<ArgumentException>(()=>OpeningRouteReadiness.Review(c,
            Array.Empty<string>()));
        Throws<ArgumentNullException>(()=>OpeningRouteReadiness.Review(null,
            WorldFiveOpeningTenPrototype.MissionIds));
    }

    private static void OpeningRoutesReadOnly()
    {
        var c=WorldFiveOpeningTenPrototype.Create();
        var revision=c.Manifest.SaveRevision;
        var missions=c.Campaigns.Single().Missions.Select(m=>m.MissionId).ToArray();
        var positions=c.Missions.Values.SelectMany(m=>m.Deployment).ToArray();
        var report=OpeningRouteReadiness.Review(c,WorldFiveOpeningTenPrototype.MissionIds);
        Equal(true,report.Passes);
        Equal(revision,c.Manifest.SaveRevision);
        Equal(true,missions.SequenceEqual(c.Campaigns.Single().Missions.Select(m=>m.MissionId)));
        Equal(true,positions.SequenceEqual(c.Missions.Values.SelectMany(m=>m.Deployment)));
        Equal("w5-opening-ten-profile-v4",WorldFiveOpeningTenPrototype.ProfileId);
        Equal("w5-opening-ten-layout-04",WorldFiveOpeningTenPrototype.Revision);
    }
}
