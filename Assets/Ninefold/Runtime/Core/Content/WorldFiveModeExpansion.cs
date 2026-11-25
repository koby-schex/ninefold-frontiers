using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;
using Ninefold.Core.Progression;

namespace Ninefold.Core.Content
{
    /// <summary>Additional offline encounters built on the same catalog, core
    /// battle flow, save revision and paid-free reward ledger. No new canon.</summary>
    public static class WorldFiveModeExpansion
    {
        public const string Revision="nf-modes-expansion-197";
        public const string HordeRunId="nf-horde-continuous";
        public const string ShelterWitness="nf-path-shelter-witness";
        public const string ShelterPassage="nf-path-shelter-passage";
        public const string SealRecord="nf-path-seal-record";
        public const string SealGuard="nf-path-seal-guard";
        public const string CollisionFlanks="nf-collision-flanks";
        public const string CollisionRelay="nf-collision-relay";
        public const string CollisionFinal="nf-collision-final";

        private static readonly IReadOnlyList<FrontierModeStage> NewStages=Array.AsReadOnly(new[]{
            new FrontierModeStage(HordeRunId,FrontierMode.Horde,"The Unbroken Line",
                "Six assaults in ONE uninterrupted battle. Defeated hostiles yield an open preparation interval: heal or reposition before activating the next wave at the rally marker. Squad injuries, spent signatures, and positions persist until the run ends.",
                "Six assaults repelled. The same surviving formation held the protected crossing for the entire run.",
                "Clear each wave, then interact with the rally marker to begin the next one. Inter-wave recovery uses your existing support abilities. All six waves share one saved battle."),
            new FrontierModeStage(ShelterWitness,FrontierMode.Pathweave,"The testimony route",
                "A shelter decision now forks again. Preserve a witness's account and stabilize the safe passage; completing this permanently closes the alternate passage choice.",
                "The account survives and the passage remains verifiable. The shelter's consequences are now part of the recorded route.",
                "Protect the witness for three rounds, then stabilize the record."),
            new FrontierModeStage(ShelterPassage,FrontierMode.Pathweave,"The open passage",
                "Commit the shelter's last resources to keeping a larger crossing open. This choice foregoes the witness-record operation in this journey.",
                "The wider passage stands. The team recorded what it could and chose to keep the route itself open.",
                "Hold the evacuation zone for three rounds, then stabilize the crossing."),
            new FrontierModeStage(SealRecord,FrontierMode.Pathweave,"The containment record",
                "Your sealed route leaves behind a disputed account. Recover an independent comparison and stabilize its reference rather than reopening the passage.",
                "The isolated route now has a verified record. No explanation of the wider crisis is asserted.",
                "Defeat the armed interceptors, then confirm the surviving record twice."),
            new FrontierModeStage(SealGuard,FrontierMode.Pathweave,"The guarded boundary",
                "Maintain the seal through an extended security watch, choosing immediate protection over a second archival survey.",
                "The unstable lane remains closed, and the defended boundary still protects nearby travelers.",
                "Hold the boundary for three rounds, then clear the remaining armed pressure."),
            new FrontierModeStage(CollisionFlanks,FrontierMode.WorldCollision,"Three exposed approaches",
                "A seven-unit mixed-formation field exercise. Hold the near approach, then the far flank, then reconnect the distant marker.",
                "The unit formation covered both fronts and restored the training reference.",
                "Secure the near lane, hold the far lane, then stabilize the isolated marker."),
            new FrontierModeStage(CollisionRelay,FrontierMode.WorldCollision,"Sundered relay",
                "A two-front encounter across a broad field. Clear the mobile screen, restore the relay and hold the departure sector.",
                "The mixed unit preserved a route through the contested field.",
                "Defeat the mobile interception, stabilize the relay, then hold the departure sector."),
            new FrontierModeStage(CollisionFinal,FrontierMode.WorldCollision,"A formation across the fracture",
                "Large-scale mixed-roster trial: defend a broad approach, defeat the remaining armed opposition and stabilize the far marker. This is not a lore-approved crossover battle.",
                "The field is secured. Cross-world formations can now be exercised by the common core without inventing an unapproved story.",
                "Hold the approach, defeat the remaining hostiles, then stabilize the far reference.")
        });
        public static IReadOnlyList<FrontierModeStage> Stages=>NewStages;
        public static bool TryGet(string id,out FrontierModeStage stage)
        {stage=Stages.FirstOrDefault(s=>s.Id==id);return stage!=null;}
        public static bool IsModeMission(string id)=>TryGet(id,out _)||WorldFiveModes.IsModeMission(id);
        public static bool TryGetAny(string id,out FrontierModeStage stage)
            =>TryGet(id,out stage)||WorldFiveModes.TryGet(id,out stage);

        private static bool Clear(PlayerProgress p,string id)=>p.Missions.ContainsKey(id);
        public static bool Available(string id,PlayerProgress p)
        {
            if(p==null)throw new ArgumentNullException(nameof(p));
            switch(id)
            {
                case HordeRunId: return Clear(p,WorldFiveModes.HordeId(1));
                case ShelterWitness:return Clear(p,WorldFiveModes.PathShelterEnd)&&!Clear(p,ShelterPassage);
                case ShelterPassage:return Clear(p,WorldFiveModes.PathShelterEnd)&&!Clear(p,ShelterWitness);
                case SealRecord:return Clear(p,WorldFiveModes.PathSealEnd)&&!Clear(p,SealGuard);
                case SealGuard:return Clear(p,WorldFiveModes.PathSealEnd)&&!Clear(p,SealRecord);
                case CollisionFlanks:return Clear(p,WorldFiveModes.CollisionEnd);
                case CollisionRelay:return Clear(p,CollisionFlanks);
                case CollisionFinal:return Clear(p,CollisionRelay);
                default:return false;
            }
        }
        public static string Consequence(PlayerProgress p)
        {
            if(p==null)throw new ArgumentNullException(nameof(p));
            string route=WorldFiveModes.ChosenPath(p);
            if(route==null)return "Junction unresolved";
            if(route=="SHELTER")
                return Clear(p,ShelterWitness)?"Shelter · testimony preserved":
                    Clear(p,ShelterPassage)?"Shelter · passage maintained":
                    "Shelter · second decision pending";
            return Clear(p,SealRecord)?"Containment · record verified":
                Clear(p,SealGuard)?"Containment · boundary defended":
                "Containment · second decision pending";
        }
        public static int HordeWave(BattleResult result)
        {
            if(result==null||result.MissionId!=HordeRunId)throw new ArgumentException("Horde result required.");
            return result.Objectives.Count(o=>o.Kind==ObjectiveKind.Defeat&&o.Status==ObjectiveStatus.Completed);
        }
        public static long HordeScore(BattleResult result)
        {
            int waves=HordeWave(result);
            return waves==0?0L:checked(waves*1000L+Math.Max(0,100-result.Round)*10L);
        }

        private static FieldPoint P(decimal x,decimal z)=>new FieldPoint(x,0,z);
        private static FieldBox B(decimal x,decimal z,decimal r,decimal t,decimal h=5)
            =>new FieldBox(P(x,z),new FieldPoint(r,h,t));
        private static FieldObstacle Rock(int x,int z,int r,int t)
            =>new FieldObstacle(B(x,z,r,t,2),true,true);
        private static DifficultGround Slow(int x,int z,int r,int t)
            =>new DifficultGround(B(x,z,r,t,.1m),1.5m);
        private static readonly string[] EnemyTypes={
            WorldFiveActOnePrototype.ScoutId,WorldFiveActOnePrototype.StrikerId,
            WorldFiveActOnePrototype.CastigatorId,WorldFiveActOnePrototype.WreckerId,
            WorldFiveActOnePrototype.SeerId,WorldFiveActOnePrototype.WitnessId };
        private static ActorSpawn[] Threats(string id,FieldBox bounds,params (int kind,int x,int z)[] specs)
            =>specs.Select((t,i)=>{
                bool defensive=t.kind==2||t.kind==3||t.kind==4||t.kind==5;
                var guard=defensive?B(Math.Max(bounds.Min.X,t.x-5),Math.Max(bounds.Min.Z,t.z-5),
                    Math.Min(bounds.Max.X,t.x+5),Math.Min(bounds.Max.Z,t.z+5)):null;
                return new ActorSpawn(id+"-hostile-"+(i+1),EnemyTypes[t.kind],P(t.x,t.z),true,
                    new EnemyBehavior(defensive?EnemyStyle.Defensive:EnemyStyle.Aggressive,
                        defensive?5:2,guard,128));
            }).ToArray();
        private static ObjectiveDefinition Defeat(string id,ActorSpawn[] group)
            =>ObjectiveDefinition.Defeat(id,group.Select(a=>a.Id));
        private static ObjectiveDefinition Hold(string id,FieldBox zone,ActorSpawn[] group,int rounds)
            =>ObjectiveDefinition.Secure(id,zone,new[]{"$squad"},group.Select(a=>a.Id),rounds,true);
        private static ObjectiveDefinition Mark(string id,int count,FieldPoint point)
            =>ObjectiveDefinition.Stabilize(id,count,new InteractionDefinition(point,1.5m,new[]{"$squad"}));
        private static FieldPoint[] Deploy(int count,int start=-10,int z=-11)
            =>Enumerable.Range(0,count).Select(i=>P(start+i,z)).ToArray();

        private static MissionContent HordeRun()
        {
            var id=HordeRunId;
            var bounds=B(-20,-15,23,20);
            var map=new BattlefieldMap(bounds,new[]{
                Rock(-5,0,-3,2),Rock(3,-4,5,-2),Rock(7,5,9,7),Rock(-7,8,-5,10)
            },new[]{Slow(1,8,4,10),Slow(-9,-6,-7,-4)});
            var waves=new[]{
                Threats(id+"-w1",bounds,(0,9,-3),(1,10,4)),
                Threats(id+"-w2",bounds,(0,14,-6),(1,15,0),(2,14,6)),
                Threats(id+"-w3",bounds,(0,7,12),(2,12,14),(3,17,12)),
                Threats(id+"-w4",bounds,(1,-4,10),(0,-8,7),(2,-9,13),(4,0,14)),
                Threats(id+"-w5",bounds,(0,13,-10),(3,18,-6),(2,18,0),(1,18,6)),
                Threats(id+"-w6",bounds,(1,2,15),(3,6,16),(4,12,16),(2,18,13),(5,-10,15))
            };
            var objectives=new List<ObjectiveDefinition>();
            objectives.Add(Defeat("primary",waves[0]));
            for(int n=2;n<=6;n++)
            {
                objectives.Add(Mark("stage-"+(n*2-2),1,P(-1,-7)));
                objectives.Add(Defeat("stage-"+(n*2-1),waves[n-1]));
            }
            return new MissionContent(id,null,false,3,map,Deploy(4,-11,-11),
                waves.SelectMany(w=>w),objectives[0],optional:objectives.Skip(1),
                environmentKey:"study-w5-fracture");
        }
        private static MissionContent Path(string id)
        {
            bool shelter=id==ShelterWitness||id==ShelterPassage;
            var bounds=B(-15,-11,18,15);
            var cover=id==ShelterWitness?new[]{Rock(-7,1,-5,3),Rock(1,-3,3,0),Rock(9,2,11,5)}:
                id==ShelterPassage?new[]{Rock(-8,2,-6,5),Rock(0,3,2,5),Rock(10,-2,12,1)}:
                id==SealRecord?new[]{Rock(-7,-2,-5,1),Rock(1,2,3,5),Rock(9,3,11,6)}:
                new[]{Rock(-8,-1,-6,2),Rock(2,-3,4,0),Rock(10,4,12,6)};
            var threats=Threats(id,bounds,(0,10,-5),(1,12,3),(2,13,10));
            var actors=new List<ActorSpawn>(threats);
            ObjectiveDefinition primary,follow;
            if(id==ShelterWitness)
            {
                var witness=id+"-witness";
                actors.Add(new ActorSpawn(witness,"fixture-npc",P(-2,5),false));
                primary=ObjectiveDefinition.Protect("primary",witness,3);
                follow=Mark("stage-2",2,P(5,11));
            }
            else if(id==ShelterPassage)
            {primary=Hold("primary",B(4,1,7,4,3),threats,3);follow=Mark("stage-2",1,P(9,11));}
            else if(id==SealRecord)
            {primary=Defeat("primary",threats);follow=Mark("stage-2",2,P(7,12));}
            else
            {primary=Hold("primary",B(5,2,8,5,3),threats,3);follow=Defeat("stage-2",threats);}
            return new MissionContent(id,null,false,3,
                new BattlefieldMap(bounds,cover,new[]{Slow(3,-6,6,-4)}),
                Deploy(4,-10,-8),actors,primary,optional:new[]{follow},
                environmentKey:shelter?"study-w5-survey":"study-w5-fracture");
        }
        private static MissionContent Collision(string id)
        {
            int n=id==CollisionFlanks?0:id==CollisionRelay?1:2;
            var bounds=B(-23-n,-16,27+n,23);
            var cover=n==0?new[]{
                Rock(-11,2,-9,5),Rock(-1,-5,2,-2),Rock(8,3,10,6),Rock(17,-3,19,0)}:
                n==1?new[]{Rock(-12,3,-10,6),Rock(-2,-2,1,1),Rock(8,5,10,8),Rock(18,-4,20,-1)}:
                new[]{Rock(-13,2,-11,5),Rock(-2,-6,1,-3),Rock(9,4,11,7),Rock(19,-2,21,1)};
            var threats=Threats(id,bounds,(0,15,-9),(1,17,-5),(2,18,3),(3,20,13),
                (4,12,15),(5,15,0),(0,21,9),(1,5,18));
            ObjectiveDefinition primary,follow,third;
            if(n==0)
            {
                primary=Hold("primary",B(2,-1,6,3,3),threats,2);
                follow=Hold("stage-2",B(9,8,12,11,3),threats,2);
                third=Mark("stage-3",2,P(16,17));
            }
            else if(n==1)
            {
                primary=Defeat("primary",threats);
                follow=Mark("stage-2",2,P(12,12));
                third=Hold("stage-3",B(8,5,12,9,3),threats,2);
            }
            else
            {
                primary=Hold("primary",B(3,3,7,7,3),threats,3);
                follow=Defeat("stage-2",threats);
                third=Mark("stage-3",3,P(17,16));
            }
            return new MissionContent(id,null,false,6,
                new BattlefieldMap(bounds,cover,new[]{Slow(3,-9,6,-7),Slow(-3,11,0,13)}),
                Deploy(7,-17,-12),threats,primary,optional:new[]{follow,third},
                encounter:EncounterKind.WorldCollision,environmentKey:"study-w5-fracture");
        }

        public static ContentCatalog Create()
        {
            var prior=WorldFiveModes.Create();
            var extras=Stages.Select(stage=>stage.Mode==FrontierMode.Horde?HordeRun():
                stage.Mode==FrontierMode.Pathweave?Path(stage.Id):Collision(stage.Id)).ToArray();
            var rewards=Stages.Select(stage=>{
                int scale=stage.Mode==FrontierMode.Horde?8:
                    stage.Mode==FrontierMode.WorldCollision?6:3;
                return new MissionRewardPolicy(stage.Id,Revision,
                    new[]{new ResourceGrant("fixture-resource",20L*scale)},
                    new[]{new ResourceGrant("fixture-resource",2L*scale)});
            }).ToArray();
            // Re-register old core gates! Rebuilding the catalog without them
            // would silently unlock skipped Horde waves and rejected branches.
            var gates=WorldFiveModes.Stages.Concat(Stages).ToDictionary(s=>s.Id,
                s=>(Func<PlayerProgress,bool>)(p=>WorldFiveModes.IsModeMission(s.Id)?
                    WorldFiveModes.Available(s.Id,p):Available(s.Id,p)),StringComparer.Ordinal);
            return new ContentCatalog(prior.Manifest,prior.Resources,prior.Abilities.Values,prior.Kits.Values,
                prior.Units.Values,prior.Missions.Values.Concat(extras),
                prior.Rewards.Values.Concat(rewards),prior.Campaigns,prior.StarterUnits,prior.Statuses.Values,gates);
        }
    }
}
