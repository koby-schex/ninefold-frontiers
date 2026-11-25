using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;
using Ninefold.Core.Progression;

namespace Ninefold.Core.Content
{
    // Modes are standalone battle entries against the SAME core scheduler.
    // No campaign stage or existing mission is reauthored by this add-on.
    // The special Ninefold path mechanics here are candidate playtest rules,
    // NOT an assertion that the Master Lore Archive contains a new cosmic law.
    public enum FrontierMode { Horde, Pathweave, WorldCollision }

    public sealed class FrontierModeStage
    {
        public string Id { get; }
        public FrontierMode Mode { get; }
        public string Name { get; }
        public string Briefing { get; }
        public string Success { get; }
        public string Instruction { get; }
        internal FrontierModeStage(string id,FrontierMode mode,string name,string briefing,
            string success,string instruction)
        {
            Id=id;Mode=mode;Name=name;Briefing=briefing;Success=success;Instruction=instruction;
        }
    }

    public sealed class WorldCampaignSlot
    {
        public int World { get; }
        public string LockedName { get; }
        public bool Authored { get; }
        internal WorldCampaignSlot(int world,string name,bool authored)
        {World=world;LockedName=name;Authored=authored;}
    }

    /// <summary>Playable mode pilots with profile-backed gates and consequential
    /// route forks; Worlds 1–9 availability is explicit, never implied.</summary>
    public static class WorldFiveModes
    {
        public const string Revision="nf-mode-pilots-01";
        public const string HordePrefix="nf-horde-";
        public const string PathEntry="nf-path-entry";
        public const string PathShelter="nf-path-shelter";
        public const string PathSeal="nf-path-seal";
        public const string PathShelterEnd="nf-path-shelter-end";
        public const string PathSealEnd="nf-path-seal-end";
        public const string CollisionEntry="nf-collision-entry";
        public const string CollisionEnd="nf-collision-next";

        public static IReadOnlyList<WorldCampaignSlot> WorldCampaigns { get; } =
            Array.AsReadOnly(Enumerable.Range(1,9).Select(w=>new WorldCampaignSlot(w,
                w==1?"The Brightfall Reach":w==2?"The Shattered Heights":
                w==5?"The Fractured Meridian":null,w==5)).ToArray());
        public static string HordeId(int wave)
        {
            if(wave<1||wave>6)throw new ArgumentOutOfRangeException(nameof(wave));
            return HordePrefix+wave.ToString("00",System.Globalization.CultureInfo.InvariantCulture);
        }

        private static readonly IReadOnlyList<FrontierModeStage> Definitions=Array.AsReadOnly(
            Enumerable.Range(1,6).Select(w=>new FrontierModeStage(HordeId(w),FrontierMode.Horde,
                "Wave "+w,w==1?"The first Ashbound assault reaches the protected route. Keep the crossing defended.":
                "A fresh group approaches the protected lane from a different direction. Defeat the assault before the next wave.",
                "The crossing holds. Recover and prepare for the next attack.",
                "Defeat all active attackers. Health and turns reset between this pilot's separate waves."))
            .Concat(new[]{
                new FrontierModeStage(PathEntry,FrontierMode.Pathweave,"The forked crossing",
                    "A World Five path has split around the same damaged route. Stabilize its reference, then secure the junction. What follows depends on your choice.",
                    "Two valid routes remain. You must decide what this crossing is used to protect.",
                    "Stabilize the junction twice, then hold its capture zone for two rounds."),
                new FrontierModeStage(PathShelter,FrontierMode.Pathweave,"Shelter the stranded",
                    "Choose the evacuation path. Protect a stranded traveler and stabilize the shelter marker. This permanently chooses the shelter branch for this test journey.",
                    "The traveler is secure. The next operation will preserve the protected passage.",
                    "Defend the traveler, then stabilize the far shelter marker."),
                new FrontierModeStage(PathSeal,FrontierMode.Pathweave,"Seal the unstable route",
                    "Choose containment. Drive off an armed blocking group and hold a safe barrier. This permanently chooses containment for this test journey.",
                    "The dangerous lane is blocked without harming a bonded creature. Your next mission is the containment outcome.",
                    "Defeat the armed defenders, then hold the isolating zone."),
                new FrontierModeStage(PathShelterEnd,FrontierMode.Pathweave,"Path of refuge",
                    "Your prior choice saved the traveler. Protect the departure party, restore a reliable reference and secure the final corridor.",
                    "The shelter route remains open. The choice you made changed what had to be defended.",
                    "Protect the traveler, then restore the reference, then hold the departure lane."),
                new FrontierModeStage(PathSealEnd,FrontierMode.Pathweave,"Path of containment",
                    "Your prior choice sealed the unstable lane. Clear a remaining armed screen, hold the boundary and confirm its reference.",
                    "The unstable route is isolated. The crossing's outcome reflects the earlier containment choice.",
                    "Defeat the armed screen, then secure the boundary, then stabilize the reference."),
                new FrontierModeStage(CollisionEntry,FrontierMode.WorldCollision,"Linked formations",
                    "A large battlefield exercise for seven available deployment positions and cross-faction squads. Not an approved encounter between named worlds.",
                    "The mixed formation secured its crossing and synchronized a second reference.",
                    "With at least six units from the unlocked roster, hold the wide crossing and restore the reference."),
                new FrontierModeStage(CollisionEnd,FrontierMode.WorldCollision,"The open flank",
                    "The second large battlefield exercise challenges a mixed roster to defeat a scattered opposition before securing a distant lane.",
                    "The large mixed formation held the position; the mechanics are ready for future canon-approved World Collision scenes.",
                    "Defeat all armed opponents then hold the far lane for two rounds.")
            }).ToArray());
        public static IReadOnlyList<FrontierModeStage> Stages=>Definitions;
        public static bool TryGet(string id,out FrontierModeStage stage)
        {stage=Definitions.FirstOrDefault(d=>d.Id==id);return stage!=null;}
        public static bool IsModeMission(string id)=>TryGet(id,out _);
        public static bool Cleared(PlayerProgress progress,string id)
            =>progress.Missions.ContainsKey(id);
        public static string ChosenPath(PlayerProgress p)
        {
            if(p==null)throw new ArgumentNullException(nameof(p));
            if(Cleared(p,PathShelter))return "SHELTER";
            if(Cleared(p,PathSeal))return "CONTAINMENT";
            return null;
        }
        // Enforced in BOTH MissionCard and the flow's squad/start gate,
        // never just on the mode-selection button.
        public static bool Available(string id,PlayerProgress p)
        {
            if(p==null)throw new ArgumentNullException(nameof(p));
            if(!TryGet(id,out var stage))return false;
            if(stage.Mode==FrontierMode.Horde)
            {
                int wave=int.Parse(id.Substring(HordePrefix.Length),
                    System.Globalization.CultureInfo.InvariantCulture);
                return wave==1||Cleared(p,HordeId(wave-1));
            }
            switch(id)
            {
                case PathEntry:return true;
                case PathShelter:return Cleared(p,PathEntry)&&!Cleared(p,PathSeal);
                case PathSeal:return Cleared(p,PathEntry)&&!Cleared(p,PathShelter);
                case PathShelterEnd:return Cleared(p,PathShelter)&&!Cleared(p,PathSeal);
                case PathSealEnd:return Cleared(p,PathSeal)&&!Cleared(p,PathShelter);
                default:
                    return p.CampaignClaims.Any(c=>c.CampaignId==WorldFiveFinale.CompletionReceiptId)&&
                        p.OwnedUnits.Count>=6 &&
                        (id==CollisionEntry||id==CollisionEnd&&Cleared(p,CollisionEntry));
            }
        }

        private static FieldPoint P(decimal x,decimal z)=>new FieldPoint(x,0,z);
        private static FieldBox B(decimal x,decimal z,decimal r,decimal t,decimal h=5)
            =>new FieldBox(P(x,z),new FieldPoint(r,h,t));
        private static FieldObstacle Rock(int x,int z,int r,int t)
            =>new FieldObstacle(B(x,z,r,t,2),true,true);
        private static DifficultGround Slow(int x,int z,int r,int t)
            =>new DifficultGround(B(x,z,r,t,.1m),1.5m);
        private static readonly string[] Enemies={
            WorldFiveActOnePrototype.ScoutId,WorldFiveActOnePrototype.StrikerId,
            WorldFiveActOnePrototype.CastigatorId,WorldFiveActOnePrototype.WreckerId,
            WorldFiveActOnePrototype.SeerId,WorldFiveActOnePrototype.WitnessId };
        private static readonly (int type,int x,int z)[] WavePositions={
            (0,10,-7),(1,12,-2),(2,11,7),(0,8,-6),(3,13,5),(4,8,8),(5,14,0)
        };
        private static ActorSpawn[] Threats(string id,FieldBox bounds,params (int type,int x,int z)[] specs)
        {
            return specs.Select((t,i)=>{
                bool defensive=t.type==2||t.type==3||t.type==4||t.type==5;
                var guard=defensive?B(Math.Max(bounds.Min.X,t.x-4),Math.Max(bounds.Min.Z,t.z-4),
                    Math.Min(bounds.Max.X,t.x+4),Math.Min(bounds.Max.Z,t.z+4)):null;
                return new ActorSpawn(id+"-enemy-"+(i+1),Enemies[t.type],P(t.x,t.z),true,
                    new EnemyBehavior(defensive?EnemyStyle.Defensive:EnemyStyle.Aggressive,
                        t.type==4?5:t.type==2?4:2,guard,128));
            }).ToArray();
        }
        private static ObjectiveDefinition Interact(string id,FieldPoint p,int count)
            =>ObjectiveDefinition.Stabilize(id,count,new InteractionDefinition(p,1.5m,new[]{"$squad"}));
        private static ObjectiveDefinition Hold(string id,FieldBox zone,ActorSpawn[] threats,int rounds)
            =>ObjectiveDefinition.Secure(id,zone,new[]{"$squad"},threats.Where(t=>t.IsHostile).Select(t=>t.Id),
                rounds,true);
        private static ObjectiveDefinition Clear(string id,ActorSpawn[] threats)
            =>ObjectiveDefinition.Defeat(id,threats.Where(a=>a.IsHostile).Select(a=>a.Id));
        private static FieldPoint[] Deploy(int count,int start=-7,int z=-7)
            =>Enumerable.Range(0,count).Select(i=>P(start+i,z)).ToArray();
        private static MissionContent Horde(int wave)
        {
            string id=HordeId(wave);
            var bounds=B(-13-wave,-10,16+wave,13);
            var cover=new[]{Rock(-7,1,-5,4),Rock(1,-4,3,-2),Rock(7,2,9,4)};
            var ground=new[]{Slow(-1,6,2,8)};
            var threats=Threats(id,bounds,WavePositions.Take(wave+1).ToArray());
            return new MissionContent(id,null,false,2,new BattlefieldMap(bounds,cover,ground),
                Deploy(4),threats,Clear("primary",threats),environmentKey:"study-w5-fracture");
        }
        private static MissionContent Paths(string id)
        {
            var bounds=B(-12,-9,15,12);
            var map=new BattlefieldMap(bounds,new[]{Rock(-6,1,-4,4),Rock(0,-2,2,1),Rock(8,0,10,3)},
                new[]{Slow(3,-5,5,-3)});
            var threats=Threats(id,bounds,(0,10,-5),(2,11,5),(1,12,9));
            var actors=new List<ActorSpawn>(threats);
            ObjectiveDefinition primary,second=null,third=null;
            if(id==PathEntry)
            {
                primary=Interact("primary",P(4,7),2);
                second=Hold("stage-2",B(4,1,7,4,3),threats,2);
            }
            else if(id==PathShelter||id==PathShelterEnd)
            {
                string traveler=id+"-traveler";
                actors.Add(new ActorSpawn(traveler,"fixture-npc",P(-1,5),false));
                primary=ObjectiveDefinition.Protect("primary",traveler,id==PathShelter?3:4);
                second=Interact("stage-2",P(5,7),2);
                if(id==PathShelterEnd)third=Hold("stage-3",B(4,3,7,6,3),threats,2);
            }
            else if(id==PathSeal)
            {
                primary=Clear("primary",threats);
                second=Hold("stage-2",B(4,1,7,4,3),threats,2);
            }
            else
            {
                primary=Clear("primary",threats);
                second=Hold("stage-2",B(4,2,7,5,3),threats,2);
                third=Interact("stage-3",P(7,8),2);
            }
            var optional=new[]{second,third}.Where(o=>o!=null).ToArray();
            return new MissionContent(id,null,false,3,map,Deploy(4),actors,primary,
                optional:optional,environmentKey:"study-w5-survey");
        }
        private static MissionContent Collision(string id)
        {
            bool second=id==CollisionEnd;
            var bounds=B(-19,-13,22,17);
            var map=new BattlefieldMap(bounds,second?new[]{
                Rock(-10,3,-8,6),Rock(-1,-4,2,-1),Rock(6,4,8,7),Rock(14,-2,16,1)
            }:new[]{Rock(-10,1,-8,4),Rock(-1,0,2,3),Rock(6,-5,8,-2),Rock(13,5,15,8)},
                new[]{Slow(2,-8,5,-6),Slow(3,10,6,12)});
            var threats=Threats(id,bounds,(0,13,-8),(1,15,-4),(2,16,3),(3,17,12),(4,11,10),(5,12,-1));
            var first=second?Clear("primary",threats):Hold("primary",B(4,1,8,5,3),threats,2);
            var follow=second?Hold("stage-2",B(8,7,11,10,3),threats,2):Interact("stage-2",P(10,12),2);
            return new MissionContent(id,null,false,6,map,Deploy(7,-14,-10),threats,first,
                optional:new[]{follow},encounter:EncounterKind.WorldCollision,environmentKey:"study-w5-fracture");
        }

        public static ContentCatalog Create()
        {
            var old=WorldFiveFinale.Create();
            var extra=Stages.Select(d=>d.Mode==FrontierMode.Horde?
                Horde(int.Parse(d.Id.Substring(HordePrefix.Length),System.Globalization.CultureInfo.InvariantCulture)):
                d.Mode==FrontierMode.Pathweave?Paths(d.Id):Collision(d.Id)).ToArray();
            // Distinct mission IDs and policies isolate claims in the EXISTING
            // profile; there is no second wallet or reset of campaign progress.
            var rewards=Stages.Select(d=>{
                int index=d.Mode==FrontierMode.Horde?
                    int.Parse(d.Id.Substring(HordePrefix.Length),System.Globalization.CultureInfo.InvariantCulture):
                    d.Mode==FrontierMode.WorldCollision?7:4;
                long baseAmount=20+index*8;
                return new MissionRewardPolicy(d.Id,Revision,
                    new[]{new ResourceGrant("fixture-resource",baseAmount)},
                    new[]{new ResourceGrant("fixture-resource",2)});
            }).ToArray();
            var gates=Stages.ToDictionary(d=>d.Id,d=>(Func<PlayerProgress,bool>)(p=>Available(d.Id,p)),
                StringComparer.Ordinal);
            return new ContentCatalog(old.Manifest,old.Resources,old.Abilities.Values,old.Kits.Values,
                old.Units.Values,old.Missions.Values.Concat(extra),old.Rewards.Values.Concat(rewards),
                old.Campaigns,old.StarterUnits,old.Statuses.Values,gates);
        }
    }
}
