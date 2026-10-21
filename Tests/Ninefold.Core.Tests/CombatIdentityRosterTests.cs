using System;
using System.Linq;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;

internal static partial class Program
{
    private static (string Name,Action Run)[] CombatIdentityRosterTests() => new (string,Action)[] {
        ("Combat identity V3 gives every template unique HP and normal damage", IdentityRosterUniqueness),
        ("Combat identity V3 preserves base prototype without altering V2", IdentityRosterSaveIsolation),
        ("Combat identity V3 support kit keeps role-specific healing", IdentityRosterSupport)
    };
    private static void IdentityRosterUniqueness()
    {
        var c=UnitCombatIdentityRosterV3.Create();
        Equal(11,c.Units.Count);
        Equal(11,c.Units.Values.Select(u=>u.Health).Distinct().Count());
        Equal(11,c.Units.Values.Select(u=>
            c.Abilities[c.Kits[u.KitId].AbilityIds[1]].Effect.Amount).Distinct().Count());
        Equal(true,c.Units.Values.All(u=>u.Health>=1000));
    }
    private static void IdentityRosterSaveIsolation()
    {
        var a=UnitCombatIdentityPrototype.Create();
        var b=UnitCombatIdentityRosterV3.Create();
        Equal(false,a.Manifest.SaveRevision==b.Manifest.SaveRevision);
        Equal(true,a.Missions.Keys.OrderBy(x=>x).SequenceEqual(b.Missions.Keys.OrderBy(x=>x)));
        Equal(1320,a.Units["fixture-a-2"].Health);
        Equal(480,a.Abilities[a.Kits[a.Units["fixture-a-1"].KitId].AbilityIds[1]].Effect.Amount);
    }
    private static void IdentityRosterSupport()
    {
        var c=UnitCombatIdentityRosterV3.Create();
        var kit=c.Kits[c.Units["fixture-a-2"].KitId];
        Equal(540,c.Abilities[kit.AbilityIds[2]].Effect.Amount);
        var menu=UnitCombatIdentityRosterV3.Open(new FakeSaveFiles(),new ProgressFiles());
        Equal(MissionFlowPhase.Selection,menu.Read().Phase);
    }
}
