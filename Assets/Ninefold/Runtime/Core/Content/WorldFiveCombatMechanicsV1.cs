using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;

namespace Ninefold.Core.Content
{
    /// <summary>
    /// Opt-in status-mechanics prototype layered over the four playable
    /// traditions and six anonymous Ashbound NPC templates.
    /// Existing shipped profiles and V4 mission data remain untouched.
    /// </summary>
    public static class WorldFiveCombatMechanicsV1
    {
        public const string Revision="w5-combat-mechanics-01";
        public const string Shelter="w5-olyra-shelter";
        public const string Exposed="w5-fracture-exposed";
        public const string Disrupted="w5-afterecho-disrupted";
        public const string Afterecho="w5-afterecho-overload";
        public static ContentCatalog Create()
        {
            var source=WorldFivePlayableAbilityStudy.Create();
            var statuses=source.Statuses.Values.Concat(new[]{
                new StatusDefinition(Shelter,2500,0,2),
                new StatusDefinition(Exposed,-1800,0,2),
                new StatusDefinition(Disrupted,0,-1500,2),
                new StatusDefinition(Afterecho,-1800,-1000,2)
            }).ToArray();
            var abilities=source.Abilities.Values.ToList();
            var kits=source.Kits.Values.ToList();
            var substitutions=new Dictionary<string,string>(StringComparer.Ordinal);
            void Main(string unitId,string label,string statusId,AbilityTarget? target=null)
            {
                var old=source.Kits[source.Units[unitId].KitId];
                var main=source.Abilities[old.AbilityIds[2]].Effect;
                var key="w5-mechanics-"+label+"-main";
                abilities.Add(new AbilityContent(key,new FieldAbility(
                    AbilitySlot.Main,main.Kind,main.Amount,main.Range,
                    main.UsesCover,statusId,target)));
                string kitId="w5-mechanics-"+label+"-kit";
                kits.Add(new KitContent(kitId,old.AbilityIds[0],old.AbilityIds[1],
                    key,old.AbilityIds[3],old.Availability));
                substitutions.Add(unitId,kitId);
            }
            // Heal + shelter or damage + a telegraphed defensive/offensive penalty.
            Main("fixture-a-2","olyra",Shelter);
            Main("fixture-a-3","vaskel",Exposed);
            Main("fixture-a-4","tervan",Exposed);
            Main(WorldFiveActOnePrototype.CastigatorId,"castigator",Disrupted);
            Main(WorldFiveActOnePrototype.WreckerId,"wrecker",Exposed);
            // Afterecho is a real, repeatable self-imposed drawback, not
            // an unfair enemy-only immunity or a random miss chance.
            foreach(var pair in new[]{
                new {Id=WorldFiveActOnePrototype.StrikerId,Label="striker"},
                new {Id=WorldFiveActOnePrototype.SeerId,Label="seer"}
            })
            {
                var unit=source.Units[pair.Id];
                var original=source.Kits[unit.KitId];
                string passive="w5-mechanics-"+pair.Label+"-afterecho";
                string kitId="w5-mechanics-"+pair.Label+"-kit";
                abilities.Add(new AbilityContent(passive,
                    new PassiveDefinition(PassiveTrigger.OwnerActivationStarted,Afterecho)));
                kits.Add(new KitContent(kitId,passive,original.AbilityIds[1],
                    original.AbilityIds[2],original.AbilityIds[3],original.Availability));
                substitutions.Add(pair.Id,kitId);
            }
            var units=source.Units.Values.Select(u=>substitutions.TryGetValue(u.Id,out var kitId)
                ? new UnitContent(u.Id,kitId,u.Health,u.Armor,u.Initiative,
                    u.Movement,u.Body,u.Roster) : u).ToArray();
            return new ContentCatalog(new ContentManifest("w5-combat-study",Revision,
                "prototype-01",ContentClassification.AbstractFixture),
                source.Resources,abilities,kits,units,source.Missions.Values,
                source.Rewards.Values,source.Campaigns,source.StarterUnits,statuses);
        }
    }
}
