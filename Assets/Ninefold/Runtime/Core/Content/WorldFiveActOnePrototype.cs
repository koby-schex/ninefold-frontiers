using System;
using System.Linq;
using Ninefold.Core.Campaigns;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;
using Ninefold.Core.Progression;

namespace Ninefold.Core.Content
{
    /// <summary>
    /// Isolated Act One content candidate. V4 tutorial and its saved profiles
    /// remain unchanged; explicit migration is required before Unity selection.
    /// </summary>
    public static class WorldFiveActOnePrototype
    {
        public const string Revision = "w5-act-one-candidate-02";
        public const string FirstMission = WorldFiveStorySequence.FirstCampaignMissionId;
        public const string ScoutId = "ashbound-rift-forager";
        public const string StrikerId = "ashbound-parted-striker";

        public const string CastigatorId = "ashbound-seam-castigator";
        public const string WreckerId = "ashbound-anchor-wrecker";
        public const string SeerId = "ashbound-afterecho-seer";
        public const string WitnessId = "ashbound-camp-witness";
        public static readonly string[] EnemyTemplateIds = {
            ScoutId, StrikerId, CastigatorId, WreckerId, SeerId, WitnessId
        };
        public static ContentCatalog Create()
        {
            WorldFiveStorySequence.Validate();
            var tutorial = WorldFiveOpeningTenPrototype.Create();
            var body = tutorial.Units["fixture-npc"].Body;
            // Anonymous repeatable enemies, not collectible, special or Apex.
            var profiles = new[] {
                new { Id=ScoutId, HP=65, Armor=0, Init=18, Move=6m, Damage=12, Range=2m },
                new { Id=StrikerId, HP=85, Armor=5, Init=14, Move=5m, Damage=22, Range=2m },
                new { Id=CastigatorId, HP=70, Armor=2, Init=12, Move=4m, Damage=16, Range=5m },
                new { Id=WreckerId, HP=90, Armor=8, Init=9, Move=3m, Damage=18, Range=4m },
                new { Id=SeerId, HP=60, Armor=0, Init=16, Move=4m, Damage=17, Range=6m },
                new { Id=WitnessId, HP=75, Armor=3, Init=11, Move=4m, Damage=10, Range=4m }
            };
            // These are repeatable hostile NPCs; they have no Roster entry.
            var enemyAbilities = profiles.SelectMany(p => new[] {
                AbilityContent.PassivePlaceholder(p.Id+"-passive"),
                new AbilityContent(p.Id+"-normal",new FieldAbility(
                    AbilitySlot.NormalAttack,HealthEffectKind.Damage,p.Damage,p.Range,true)),
                new AbilityContent(p.Id+"-main",new FieldAbility(
                    AbilitySlot.Main,HealthEffectKind.Damage,p.Damage+5,p.Range,true)),
                new AbilityContent(p.Id+"-signature",new FieldAbility(
                    AbilitySlot.Signature,HealthEffectKind.Damage,p.Damage+10,p.Range,true))
            }).ToArray();
            var enemyKits = profiles.Select(p => new KitContent(p.Id+"-kit",
                p.Id+"-passive",p.Id+"-normal",p.Id+"-main",p.Id+"-signature",
                new UnitAbilityDefinition(2,SignatureReadiness.AfterNormalAttack))).ToArray();
            var enemyUnits = profiles.Select(p => new UnitContent(
                p.Id,p.Id+"-kit",p.HP,p.Armor,p.Init,p.Move,body)).ToArray();
            var bounds=new FieldBox(new FieldPoint(-7,0,-5),new FieldPoint(10,5,9));
            var map = new BattlefieldMap(bounds,new[] {
                new FieldObstacle(new FieldBox(new FieldPoint(-2,0,1),new FieldPoint(0,2,3)),true,true),
                new FieldObstacle(new FieldBox(new FieldPoint(3,0,-1),new FieldPoint(5,2,1)),true,true)
            });
            var first=new MissionContent(FirstMission,"fixture-a",true,2,map,
                new[] {new FieldPoint(-3,0,-3),new FieldPoint(-2,0,-3),new FieldPoint(-1,0,-3)},
                new[] {
                    new ActorSpawn("ashbound-encounter-forager-1",ScoutId,new FieldPoint(6,0,5),true,
                        new EnemyBehavior(EnemyStyle.Aggressive,3)),
                    new ActorSpawn("ashbound-encounter-striker-1",StrikerId,new FieldPoint(7,0,3),true,
                        new EnemyBehavior(EnemyStyle.Aggressive,2))
                },
                ObjectiveDefinition.Defeat("primary",new[]{
                    "ashbound-encounter-forager-1","ashbound-encounter-striker-1"
                }),environmentKey:"study-w5-fracture");
            var chain=tutorial.Campaigns.Single().Missions
                .Concat(new[]{new CampaignMission(FirstMission,new[]{WorldFiveOpeningTenPrototype.MissionIds[9]})});
            var campaign=new CampaignDefinition(WorldFiveOpeningTenPrototype.CampaignId,"fixture-a",
                "w5-act-one-candidate-01",chain,new[]{new ResourceGrant("fixture-resource",40)});
            var reward=new MissionRewardPolicy(FirstMission,"w5-slice-rewards-01",
                new[]{new ResourceGrant("fixture-resource",100),new ResourceGrant("fragment-fixture-a-1",5)},
                new[]{new ResourceGrant("fixture-resource",2)});
            return new ContentCatalog(new ContentManifest("w5-act-one-prototype",Revision,"balance-v1",
                ContentClassification.AbstractFixture),
                tutorial.Resources,tutorial.Abilities.Values.Concat(enemyAbilities),
                tutorial.Kits.Values.Concat(enemyKits),
                tutorial.Units.Values.Concat(enemyUnits),
                tutorial.Missions.Values.Concat(new[]{first}),
                tutorial.Rewards.Values.Concat(new[]{reward}),new[]{campaign},
                tutorial.StarterUnits,tutorial.Statuses.Values);
        }
    }
}
