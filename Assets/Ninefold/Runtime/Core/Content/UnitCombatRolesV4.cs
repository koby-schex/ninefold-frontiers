using System;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Persistence;
using Ninefold.Core.Progression;
using Ninefold.Core.Views;

namespace Ninefold.Core.Content
{
    /// <summary>Opt-in frontline and striker role study. Noncanonical fixture
    /// values; does not change V3 profiles, World Five V4, or active saves.</summary>
    public static class UnitCombatRolesV4
    {
        public const string ProfileId="unit-combat-roles-v4";
        public const string Revision="unit-combat-roles-v4";
        public const string FrontlineId="fixture-a-1";
        public const string StrikerId="fixture-a-3";
        public const string SupportId="fixture-a-2";

        public static ContentCatalog Create()
        {
            var source=UnitCombatIdentityRosterV3.Create();
            var front=source.Units[FrontlineId];
            var strike=source.Units[StrikerId];
            var abilities=source.Abilities.Values.ToList();
            var kits=source.Kits.Values.ToList();
            var units=source.Units.Values.Select(u=>
                u.Id==FrontlineId ? new UnitContent(u.Id,"identity-v4-frontline-kit",
                    2450,420,u.Initiative,u.Movement,u.Body,u.Roster)
                : u.Id==StrikerId ? new UnitContent(u.Id,"identity-v4-striker-kit",
                    1565,125,u.Initiative,u.Movement,u.Body,u.Roster) : u).ToArray();

            abilities.Add(new AbilityContent("identity-v4-frontline-main",
                new FieldAbility(AbilitySlot.Main,HealthEffectKind.Healing,490,4,false)));
            abilities.Add(new AbilityContent("identity-v4-striker-main",
                new FieldAbility(AbilitySlot.Main,HealthEffectKind.Damage,620,8,true)));
            var frontKit=source.Kits[front.KitId];
            var strikeKit=source.Kits[strike.KitId];
            kits.Add(new KitContent("identity-v4-frontline-kit",frontKit.AbilityIds[0],
                frontKit.AbilityIds[1],"identity-v4-frontline-main",frontKit.AbilityIds[3],
                frontKit.Availability));
            kits.Add(new KitContent("identity-v4-striker-kit",strikeKit.AbilityIds[0],
                strikeKit.AbilityIds[1],"identity-v4-striker-main",strikeKit.AbilityIds[3],
                strikeKit.Availability));

            if(units.Select(u=>u.Health).Distinct().Count()!=units.Length ||
                units.Select(u=>abilities.Single(a=>a.Id==
                    kits.Single(k=>k.Id==u.KitId).AbilityIds[1]).Effect.Amount)
                    .Distinct().Count()!=units.Length ||
                units.Select(u=>abilities.Single(a=>a.Id==
                    kits.Single(k=>k.Id==u.KitId).AbilityIds[3]).Effect.Amount)
                    .Distinct().Count()!=units.Length)
                throw new InvalidOperationException("Each unit needs distinct HP and attack values.");
            return new ContentCatalog(new ContentManifest("w5-combat-roles",Revision,
                "combat-roles-v4",ContentClassification.AbstractFixture),
                source.Resources,abilities,kits,units,source.Missions.Values,
                source.Rewards.Values,source.Campaigns,source.StarterUnits,source.Statuses.Values);
        }
        public static MissionPresentation Open(IBattleSaveFiles battles,IProgressSaveFiles progress)
        {
            if(battles==null||progress==null)
                throw new ArgumentNullException("Combat roles require independent save stores.");
            var menu=new MissionPresentation(Create(),battles,progress,ProfileId);
            if(progress.Read(0)!=null||progress.Read(1)!=null) menu.Open();
            else menu.CreateProfile();
            return menu;
        }
    }
}
