using System;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
using Ninefold.Core.Views;

internal static partial class Program
{
    private static (string Name,Action Run)[] CombatIdentityTests() => new (string,Action)[] {
        ("Identity support kit has differentiated actionable values", CombatIdentityKit),
        ("Identity study preserves base World Five roster and mission layout", CombatIdentityIsolation),
        ("Identity study saves and resumes a distinct deployed support unit", CombatIdentityResume)
    };

    private static void CombatIdentityKit()
    {
        var c=UnitCombatIdentityPrototype.Create();
        var support=c.Units[UnitCombatIdentityPrototype.SupportUnitId];
        var kit=c.Kits[support.KitId];
        Equal("identity-support-kit",kit.Id);
        Equal(300,c.Abilities[kit.AbilityIds[1]].Effect.Amount);
        Equal(540,c.Abilities[kit.AbilityIds[2]].Effect.Amount);
        Equal(420,c.Abilities[kit.AbilityIds[3]].Effect.Amount);
        Equal(true,c.Abilities[kit.AbilityIds[0]].IsPassivePlaceholder);
        Equal(false,c.Units["fixture-a-1"].KitId==support.KitId);
        Equal(480,c.Abilities[c.Kits[c.Units["fixture-a-1"].KitId].AbilityIds[1]].Effect.Amount);
        Equal(1220,c.Units["fixture-a-1"].Health);
        Equal(1320,support.Health);
        Equal(1080,c.Units["fixture-npc"].Health);
    }
    private static void CombatIdentityIsolation()
    {
        var original=WorldFiveOpeningTenPrototype.Create();
        var study=UnitCombatIdentityPrototype.Create();
        Equal(false,original.Manifest.SaveRevision==study.Manifest.SaveRevision);
        Equal(original.Missions.Count,study.Missions.Count);
        Equal(true,original.Campaigns.Single().Missions.Select(x=>x.MissionId)
            .SequenceEqual(study.Campaigns.Single().Missions.Select(x=>x.MissionId)));
        Equal("fixture-kit",original.Units[UnitCombatIdentityPrototype.SupportUnitId].KitId);
        Equal(true,study.StarterUnits.SequenceEqual(original.StarterUnits));
    }
    private static void CombatIdentityResume()
    {
        var b=new FakeSaveFiles();var p=new ProgressFiles();
        var m=UnitCombatIdentityPrototype.Open(b,p);
        var id=WorldFiveOpeningTenPrototype.MissionIds[0];
        m.SelectMission(id);
        m.SetSquad(new[]{"fixture-a-2"});
        Equal(true,m.Read().CanStart);
        m.Start(m.Read().PlanId);
        m=UnitCombatIdentityPrototype.Open(b,p);
        Equal(MissionFlowPhase.Battle,m.Read().Phase);
        Equal(id,m.Read().Resume.MissionId);
        Equal("fixture-a-2",m.ResumeBattle().Read().Battle.Activation.UnitId);
    }
}
