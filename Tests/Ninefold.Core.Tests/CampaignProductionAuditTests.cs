using System;
using System.Linq;
using Ninefold.Core.Content;

internal static partial class Program
{
    private static (string Name, Action Run)[] CampaignProductionAuditTests() =>
        new (string, Action)[] {
            ("Campaign draft audit reports prototype repeats without changing the catalog", AuditPrototype),
            ("Campaign V3 audit recognizes distinct arenas and World Five environment identity", AuditVariedMaps),
            ("Campaign release gate rejects unfinished fixtures and inadequate launch scope", AuditReleaseGate),
            ("Campaign audit rejects invalid requests and requires a known campaign", AuditInvalidRequests)
        };

    private static void AuditPrototype()
    {
        var c=AbstractContentPackage.Create();
        string revision=c.Manifest.SaveRevision;
        var report=CampaignProductionAudit.Review(c,"fixture-starter");
        Equal(CampaignReviewStage.Draft,report.Stage);
        Equal(2,report.MissionCount);
        Equal(1,report.DistinctLayoutCount);
        Equal(0,report.DistinctEnvironmentCount);
        Equal(1,report.DistinctPrimaryObjectiveCount);
        Equal(true,report.PassesGate);
        Equal(true,report.Findings.Any(f => f.Code == "CONSECUTIVE_MAP_REPEAT" &&
            f.MissionId == "fixture-finale" && f.Severity == CampaignReviewSeverity.Advisory));
        Equal(true,report.Findings.Any(f => f.Code == "OBJECTIVE_AT_DEPLOYMENT"));
        Equal(true,report.Findings.Any(f => f.Code == "MISSING_ENVIRONMENT"));
        Equal(revision,c.Manifest.SaveRevision);
        Equal(2,c.Campaigns[0].Missions.Count);
    }

    private static void AuditVariedMaps()
    {
        var c=PlaytestContentPackage.Create();
        var starter=CampaignProductionAudit.Review(c,"fixture-starter",
            environmentPrefix:"study-w5-");
        Equal(true,starter.PassesGate);
        Equal(2,starter.MissionCount);
        Equal(2,starter.DistinctLayoutCount);
        Equal(2,starter.DistinctEnvironmentCount);
        Equal(false,starter.Findings.Any(f => f.Code == "CONSECUTIVE_MAP_REPEAT" ||
            f.Code == "CROSS_WORLD_ENVIRONMENT" || f.Code == "OBJECTIVE_AT_DEPLOYMENT"));

        var wrong=CampaignProductionAudit.Review(c,"fixture-followup",environmentPrefix:"study-w5-");
        Equal(true,wrong.Findings.Any(f => f.Code == "CROSS_WORLD_ENVIRONMENT" &&
            f.Severity == CampaignReviewSeverity.Advisory));
    }

    private static void AuditReleaseGate()
    {
        var c=PlaytestContentPackage.Create();
        var result=CampaignProductionAudit.Review(c,"fixture-starter",
            CampaignReviewStage.Release, "study-w5-");
        Equal(false,result.PassesGate);
        foreach(var required in new[] { "ABSTRACT_FIXTURE", "CAMPAIGN_LENGTH",
            "MAP_VARIETY", "ENVIRONMENT_VARIETY", "OBJECTIVE_VARIETY" })
            Equal(true,result.Findings.Any(f=>f.Code==required &&
                f.Severity == CampaignReviewSeverity.Blocking));
        Equal(100,CampaignProductionAudit.LaunchMissionMinimum);

        // Launch target is authoring policy, not an artificial limit on
        // development reviews or the current two-mission prototype.
        var reduced=CampaignProductionAudit.Review(c,"fixture-starter",
            CampaignReviewStage.Release,"other-world-",minimumReleaseMissions:2);
        Equal(false,reduced.Findings.Any(f=>f.Code=="CAMPAIGN_LENGTH"));
        Equal(true,reduced.Findings.Count(f=>f.Code=="CROSS_WORLD_ENVIRONMENT")==2);
    }

    private static void AuditInvalidRequests()
    {
        var c=PlaytestContentPackage.Create();
        Throws<ArgumentNullException>(()=>CampaignProductionAudit.Review(null,"fixture-starter"));
        Throws<ArgumentException>(()=>CampaignProductionAudit.Review(c,"missing"));
        Throws<ArgumentOutOfRangeException>(()=>CampaignProductionAudit.Review(c,"fixture-starter",
            minimumReleaseMissions:0));
        Throws<ArgumentException>(()=>CampaignProductionAudit.Review(c,"fixture-starter",
            environmentPrefix:" "));
        Throws<ArgumentOutOfRangeException>(()=>CampaignProductionAudit.Review(c,"fixture-starter",
            stage:(CampaignReviewStage)999));
    }
}
