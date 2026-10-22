using System;
using System.Linq;
using Ninefold.Core.Campaigns;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;
using Ninefold.Core.Progression;

namespace Ninefold.Core.Content
{
    /// <summary>Separate candidate for diverse, deliberately authored Act One encounters.
    /// Does not replace an in-progress saved V1 study.</summary>
    public static class WorldFiveEncounterVarietyV2
    {
        public const string Revision="w5-encounter-variety-02";
        public const string ProfileId="world-five-encounter-variety-v2";
        public static readonly string[] NewMissionIds={"w5-story-012","w5-story-013","w5-story-014"};
        public static ContentCatalog Create()
        {
            var source=WorldFiveCombatMechanicsV1.Create();
            FieldPoint P(int x,int z)=>new FieldPoint(x,0,z);
            FieldBox B(int x,int z,int r,int t)=>new FieldBox(P(x,z),new FieldPoint(r,5,t));
            BattlefieldMap Map(int kind)=>new BattlefieldMap(B(-9,-7,12,10),
                kind==0 ? new[]{
                    new FieldObstacle(B(-2,1,0,3),true,true),
                    new FieldObstacle(B(4,4,6,6),true,true)
                } : kind==1 ? new[]{
                    new FieldObstacle(B(-5,1,-3,4),true,true),
                    new FieldObstacle(B(2,-2,4,0),true,true),
                    new FieldObstacle(B(7,5,9,7),true,true)
                } : new[]{
                    new FieldObstacle(B(-4,3,-2,5),true,true),
                    new FieldObstacle(B(1,1,3,4),true,true)
                });
            ActorSpawn E(int mission,int index,string type,int x,int z,decimal preferred)=>
                new ActorSpawn("ashbound-"+mission+"-"+index,type,P(x,z),true,
                    new EnemyBehavior(EnemyStyle.Aggressive,preferred));
            MissionContent Mission(string id,int index,ActorSpawn[] enemies)=>
                new MissionContent(id,"fixture-a",true,2,Map(index),
                    new[]{P(-5,-5),P(-4,-5),P(-3,-5)},enemies,
                    ObjectiveDefinition.Defeat("primary",enemies.Select(a=>a.Id).ToArray()),
                    environmentKey:"study-w5-fracture");
            var missions=new[]{
                Mission(NewMissionIds[0],0,new[]{
                    E(12,1,WorldFiveActOnePrototype.ScoutId,7,6,3),
                    E(12,2,WorldFiveActOnePrototype.ScoutId,8,2,3),
                    E(12,3,WorldFiveActOnePrototype.CastigatorId,9,8,5)
                }),
                Mission(NewMissionIds[1],1,new[]{
                    E(13,1,WorldFiveActOnePrototype.StrikerId,7,3,2),
                    E(13,2,WorldFiveActOnePrototype.WreckerId,8,7,4),
                    E(13,3,WorldFiveActOnePrototype.WitnessId,5,8,4),
                    E(13,4,WorldFiveActOnePrototype.ScoutId,10,-2,3)
                }),
                Mission(NewMissionIds[2],2,new[]{
                    E(14,1,WorldFiveActOnePrototype.SeerId,9,7,6),
                    E(14,2,WorldFiveActOnePrototype.CastigatorId,8,1,5)
                })
            };
            var prior=source.Campaigns.Single();
            var chain=prior.Missions.Concat(NewMissionIds.Select((id,i)=>
                new CampaignMission(id,new[]{i==0 ? WorldFiveActOnePrototype.FirstMission : NewMissionIds[i-1]}))).ToArray();
            var campaign=new CampaignDefinition(prior.Id,"fixture-a",Revision,chain,
                new[]{new ResourceGrant("fixture-resource",40)});
            var rewards=missions.Select((m,i)=>new MissionRewardPolicy(m.Id,"w5-variety-reward-02",
                new[]{new ResourceGrant("fixture-resource",105+i*15)},
                new[]{new ResourceGrant("fixture-resource",2)}));
            return new ContentCatalog(
                new ContentManifest("w5-variety-prototype",Revision,"balance-v1",ContentClassification.AbstractFixture),
                source.Resources,source.Abilities.Values,source.Kits.Values,source.Units.Values,
                source.Missions.Values.Concat(missions),source.Rewards.Values.Concat(rewards),
                new[]{campaign},source.StarterUnits,source.Statuses.Values);
        }
    }
}
