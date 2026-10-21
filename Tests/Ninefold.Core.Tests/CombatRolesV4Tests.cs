using System;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;

internal static partial class Program
{
    private static (string Name,Action Run)[] CombatRolesV4Tests() => new (string,Action)[] {
        ("Combat roles frontline sustains and striker deals damage", RolesV4Distinct),
        ("Combat roles preserve unique health and attacks", RolesV4Unique),
        ("Combat roles preserve V3 and World Five save revisions", RolesV4Isolation),
        ("Combat roles open independent selectable campaign", RolesV4Open),
        ("Unit preview tags allow multiple tags or none, without faction restrictions", RolesV4OptionalTags)
    };
    private static void RolesV4Distinct()
    {
        var c=UnitCombatRolesV4.Create();
        var front=c.Units[UnitCombatRolesV4.FrontlineId];
        var striker=c.Units[UnitCombatRolesV4.StrikerId];
        var support=c.Units[UnitCombatRolesV4.SupportId];
        Equal(2450,front.Health);
        Equal(1565,striker.Health);
        Equal(true,front.Armor>striker.Armor);
        Equal(true,front.Health>support.Health);
        var frontlineMain=c.Abilities[c.Kits[front.KitId].AbilityIds[2]].Effect;
        var strikerMain=c.Abilities[c.Kits[striker.KitId].AbilityIds[2]].Effect;
        var supportMain=c.Abilities[c.Kits[support.KitId].AbilityIds[2]].Effect;
        Equal(HealthEffectKind.Healing,frontlineMain.Kind);
        Equal(490,frontlineMain.Amount);
        Equal(HealthEffectKind.Damage,strikerMain.Kind);
        Equal(620,strikerMain.Amount);
        Equal(HealthEffectKind.Healing,supportMain.Kind);
        Equal(540,supportMain.Amount);
    }
    private static void RolesV4Unique()
    {
        var c=UnitCombatRolesV4.Create();
        Equal(c.Units.Count,c.Units.Values.Select(u=>u.Health).Distinct().Count());
        Equal(c.Units.Count,c.Units.Values.Select(u=>
            c.Abilities[c.Kits[u.KitId].AbilityIds[1]].Effect.Amount).Distinct().Count());
        Equal(c.Units.Count,c.Units.Values.Select(u=>
            c.Abilities[c.Kits[u.KitId].AbilityIds[3]].Effect.Amount).Distinct().Count());
    }
    private static void RolesV4Isolation()
    {
        var current=UnitCombatRolesV4.Create();
        var previous=UnitCombatIdentityRosterV3.Create();
        Equal(false,current.Manifest.SaveRevision==previous.Manifest.SaveRevision);
        Equal(false,current.Manifest.SaveRevision==WorldFiveOpeningTenPrototype.Create().Manifest.SaveRevision);
        Equal(true,current.Missions.Keys.OrderBy(x=>x).SequenceEqual(previous.Missions.Keys.OrderBy(x=>x)));
        Equal(true,previous.Units[UnitCombatRolesV4.FrontlineId].Health!=2450);
    }
    private static void RolesV4Open()
    {
        var menu=UnitCombatRolesV4.Open(new FakeSaveFiles(),new ProgressFiles());
        Equal(MissionFlowPhase.Selection,menu.Read().Phase);
        Equal(true,menu.Read().Missions.Any(m=>m.Available));
    }
    private static void RolesV4OptionalTags()
    {
        Equal(0,UnitPreviewTags.For(UnitPreviewTag.None).Count);
        Equal(2,UnitPreviewTags.For(UnitPreviewTag.Frontline|UnitPreviewTag.Support).Count);
        Equal(3,UnitPreviewTags.For(UnitPreviewTag.Frontline|UnitPreviewTag.Damage|UnitPreviewTag.Support).Count);
        Throws<ArgumentOutOfRangeException>(()=>UnitPreviewTags.For((UnitPreviewTag)8));
        // Tags are UI hints only: no faction or three-role lineup constraint.
        var catalog=UnitCombatRolesV4.Create();
        Equal(true,catalog.Campaigns.Count>0);
    }
}
