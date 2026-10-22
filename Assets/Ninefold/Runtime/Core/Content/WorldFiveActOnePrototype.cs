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
        public const string Revision = "w5-act-one-candidate-01";
        public const string FirstMission = WorldFiveStorySequence.FirstCampaignMissionId;
        public const string ScoutId = "ashbound-rift-forager";
        public const string StrikerId = "ashbound-parted-striker";

        public static ContentCatalog Create()
        {
            WorldFiveStorySequence.Validate();
            var tutorial = WorldFiveOpeningTenPrototype.Create();
            var body = tutorial.Units["fixture-npc"].Body;
            // Anonymous repeatable enemies, not collectible, special or Apex.
            var scout = new UnitContent(ScoutId,"fixture-kit",65,0,18,6,body);
            var striker = new UnitContent(StrikerId,"fixture-kit",85,5,14,5,body);
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
                tutorial.Resources,tutorial.Abilities.Values,tutorial.Kits.Values,
                tutorial.Units.Values.Concat(new[]{scout,striker}),
                tutorial.Missions.Values.Concat(new[]{first}),
                tutorial.Rewards.Values.Concat(new[]{reward}),new[]{campaign},
                tutorial.StarterUnits,tutorial.Statuses.Values);
        }
    }
}
