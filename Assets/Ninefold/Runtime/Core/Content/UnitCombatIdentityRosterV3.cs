using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Persistence;
using Ninefold.Core.Progression;
using Ninefold.Core.Views;

namespace Ninefold.Core.Content
{
    /// <summary>
    /// Opt-in fixture roster identity experiment. Every unit template gets
    /// a unique health pool and normal-attack amount; V2 remains recoverable.
    /// </summary>
    public static class UnitCombatIdentityRosterV3
    {
        public const string ProfileId="unit-identity-roster-v3";
        public const string Revision="unit-identity-roster-v3";

        public static ContentCatalog Create()
        {
            var source=UnitCombatIdentityPrototype.Create();
            var ordered=source.Units.Values.OrderBy(u=>u.Id,StringComparer.Ordinal).ToArray();
            var abilities=new List<AbilityContent>(source.Abilities.Values);
            var kits=new List<KitContent>(source.Kits.Values);
            var units=new List<UnitContent>();
            for(int index=0;index<ordered.Length;index++)
            {
                var original=ordered[index];
                var baseKit=source.Kits[original.KitId];
                int health=1180+index*95;
                int attack=315+index*37;
                string normalId="identity-v3-normal-"+original.Id;
                string kitId="identity-v3-kit-"+original.Id;
                abilities.Add(new AbilityContent(normalId,
                    new FieldAbility(AbilitySlot.NormalAttack,HealthEffectKind.Damage,
                        attack,source.Abilities[baseKit.AbilityIds[1]].Effect.Range,true)));
                kits.Add(new KitContent(kitId,baseKit.AbilityIds[0],normalId,
                    baseKit.AbilityIds[2],baseKit.AbilityIds[3],baseKit.Availability));
                units.Add(new UnitContent(original.Id,kitId,health,original.Armor,
                    original.Initiative,original.Movement,original.Body,original.Roster));
            }
            if(units.Select(u=>u.Health).Distinct().Count()!=units.Count ||
                units.Select(u=>abilities.Single(a=>a.Id==
                    kits.Single(k=>k.Id==u.KitId).AbilityIds[1]).Effect.Amount)
                    .Distinct().Count()!=units.Count)
                throw new InvalidOperationException("Combat identity values must be unique.");
            return new ContentCatalog(new ContentManifest("w5-identity-roster",
                Revision,"roster-balance-v3",ContentClassification.AbstractFixture),
                source.Resources,abilities,kits,units,source.Missions.Values,
                source.Rewards.Values,source.Campaigns,source.StarterUnits,source.Statuses.Values);
        }

        public static MissionPresentation Open(IBattleSaveFiles battles,IProgressSaveFiles progress)
        {
            if(battles==null || progress==null)
                throw new ArgumentNullException("Identity roster study needs separate stores.");
            var menu=new MissionPresentation(Create(),battles,progress,ProfileId);
            if(progress.Read(0)!=null || progress.Read(1)!=null) menu.Open();
            else menu.CreateProfile();
            return menu;
        }
    }
}
