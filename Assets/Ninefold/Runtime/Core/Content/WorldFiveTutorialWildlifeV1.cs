using System;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;

namespace Ninefold.Core.Content
{
    /// <summary>Opt-in post-tutorial Act One catalog with limited native wildlife
    /// during training. Existing V4 and encounter study saves stay isolated.</summary>
    public static class WorldFiveTutorialWildlifeV1
    {
        public const string Revision="w5-native-ecology-01";
        public const string ProfileId="world-five-native-ecology-v1";
        public const string SillrunnerId="w5-wild-sillrunner";
        public const string SeamstalkerId="w5-wild-seamstalker";
        public static ContentCatalog Create()
        {
            var baseCatalog=WorldFiveEncounterVarietyV2.Create();
            var body=baseCatalog.Units["fixture-npc"].Body;
            var profiles=new[]{
                new {Id=SillrunnerId,Health=52,Armor=1,Initiative=19,Movement=6m,Damage=11,Range=2m},
                new {Id=SeamstalkerId,Health=80,Armor=4,Initiative=14,Movement=5m,Damage=17,Range=2m}
            };
            var abilities=profiles.SelectMany(p=>new[]{
                AbilityContent.PassivePlaceholder(p.Id+"-passive"),
                new AbilityContent(p.Id+"-normal",new FieldAbility(AbilitySlot.NormalAttack,HealthEffectKind.Damage,p.Damage,p.Range,true)),
                new AbilityContent(p.Id+"-main",new FieldAbility(AbilitySlot.Main,HealthEffectKind.Damage,p.Damage+3,p.Range,true)),
                new AbilityContent(p.Id+"-signature",new FieldAbility(AbilitySlot.Signature,HealthEffectKind.Damage,p.Damage+6,p.Range,true))
            });
            var kits=profiles.Select(p=>new KitContent(p.Id+"-kit",
                p.Id+"-passive",p.Id+"-normal",p.Id+"-main",p.Id+"-signature",
                new UnitAbilityDefinition(2,SignatureReadiness.AfterNormalAttack)));
            var units=profiles.Select(p=>new UnitContent(p.Id,p.Id+"-kit",
                p.Health,p.Armor,p.Initiative,p.Movement,body));
            var seven=WorldFiveOpeningTenPrototype.MissionIds[6];
            var eight=WorldFiveOpeningTenPrototype.MissionIds[7];
            // Limited, ecological opposition: reuse authored legal spawn placements
            // and objectives, replacing only hostile proxy templates.
            var revised=baseCatalog.Missions.Values.Select(m=>{
                if(m.Id!=seven && m.Id!=eight)return m;
                var actors=m.Actors.Select(a=>new ActorSpawn(a.Id,
                    m.Id==seven?SeamstalkerId:SillrunnerId,
                    a.Position,a.IsHostile,a.Behavior)).ToArray();
                return new MissionContent(m.Id,m.FactionId,m.IsCampaign,m.MinimumSquad,
                    m.Map,m.Deployment,actors,m.Objectives[0],m.Priority,
                    m.Objectives.Skip(1),m.Encounter,m.EnvironmentKey);
            }).ToArray();
            return new ContentCatalog(
                new ContentManifest("w5-native-ecology",Revision,"balance-v1",ContentClassification.AbstractFixture),
                baseCatalog.Resources,baseCatalog.Abilities.Values.Concat(abilities),
                baseCatalog.Kits.Values.Concat(kits),
                baseCatalog.Units.Values.Concat(units),revised,
                baseCatalog.Rewards.Values,baseCatalog.Campaigns,baseCatalog.StarterUnits,
                baseCatalog.Statuses.Values);
        }
    }
}
