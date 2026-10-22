using System;
using System.Linq;
using Ninefold.Core.Combat;

namespace Ninefold.Core.Content
{
    /// <summary>
    /// Isolated ability study for four World Five bonded traditions.
    /// Numeric effects are prototypes, not canon and not shipped Unity kits.
    /// Does not change the established Act One or V4 save revisions.
    /// </summary>
    public static class WorldFivePlayableAbilityStudy
    {
        public const string Revision="w5-playable-abilities-01";
        public static readonly string[] StandardIds = {
            "fixture-a-1","fixture-a-2","fixture-a-3","fixture-a-4"
        };
        public static ContentCatalog Create()
        {
            var source=WorldFiveActOnePrototype.Create();
            var specs=new[] {
                new { Unit=StandardIds[0], Name="great-strider", Kind=HealthEffectKind.Damage,
                    Amount=24, Range=4m, Cover=true },
                new { Unit=StandardIds[1], Name="living-veil", Kind=HealthEffectKind.Healing,
                    Amount=35, Range=5m, Cover=false },
                new { Unit=StandardIds[2], Name="riftwalker", Kind=HealthEffectKind.Damage,
                    Amount=32, Range=3m, Cover=true },
                new { Unit=StandardIds[3], Name="burdenbeast", Kind=HealthEffectKind.Damage,
                    Amount=28, Range=5m, Cover=true }
            };
            var abilities=source.Abilities.Values.ToList();
            var kits=source.Kits.Values.ToList();
            foreach(var spec in specs)
            {
                var original=source.Units[spec.Unit];
                var kit=source.Kits[original.KitId];
                var mainId="w5-"+spec.Name+"-main";
                abilities.Add(new AbilityContent(mainId,new FieldAbility(
                    AbilitySlot.Main,spec.Kind,spec.Amount,spec.Range,spec.Cover)));
                kits.Add(new KitContent("w5-"+spec.Name+"-kit",
                    kit.AbilityIds[0],kit.AbilityIds[1],mainId,
                    kit.AbilityIds[3],kit.Availability));
            }
            var units=source.Units.Values.Select(u => {
                var match=specs.FirstOrDefault(p=>p.Unit==u.Id);
                return match==null ? u : new UnitContent(
                    u.Id,"w5-"+match.Name+"-kit",u.Health,u.Armor,
                    u.Initiative,u.Movement,u.Body,u.Roster);
            });
            return new ContentCatalog(new ContentManifest("w5-ability-study",Revision,
                "prototype-01",ContentClassification.AbstractFixture),
                source.Resources,abilities,kits,units,
                source.Missions.Values,source.Rewards.Values,source.Campaigns,
                source.StarterUnits,source.Statuses.Values);
        }
    }
}
