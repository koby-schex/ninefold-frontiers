using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Campaigns;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;
using Ninefold.Core.Progression;
using A=Ninefold.Core.Content.BattlefieldArtPlan.Mood;
using K=Ninefold.Core.Missions.ObjectiveKind;
using C=Ninefold.Core.Content.WorldFiveRouteRecovery.Chapter;

namespace Ninefold.Core.Content
{
    /// <summary>Append-only Act Four candidate, testing and defending the restored
    /// corridors before the final campaign chapter. None of the proposed story text
    /// establishes a new canonical region, named boss or protected reveal.</summary>
    public static class WorldFiveConvergence
    {
        public const int First=61,Last=80;
        public const string Revision="w5-convergence-01";
        private static FieldPoint P(decimal x,decimal z)=>new FieldPoint(x,0,z);
        private static FieldBox B(decimal x,decimal z,decimal r,decimal t,decimal h=5)
            =>new FieldBox(P(x,z),new FieldPoint(r,h,t));
        private static FieldObstacle R(decimal x,decimal z,decimal r,decimal t,decimal h=2)
            =>new FieldObstacle(B(x,z,r,t,h),true,true);
        private static DifficultGround G(decimal x,decimal z,decimal r,decimal t)
            =>new DifficultGround(B(x,z,r,t,.1m),1.5m);
        private static FieldObstacle[] Rocks(params FieldObstacle[] r)=>r;
        private static DifficultGround[] Ground(params DifficultGround[] g)=>g;
        private static (int,int,int)[] E(params (int,int,int)[] e)=>e;
        private static readonly string[] Types={WorldFiveActOnePrototype.ScoutId,
            WorldFiveActOnePrototype.StrikerId,WorldFiveActOnePrototype.CastigatorId,
            WorldFiveActOnePrototype.WreckerId,WorldFiveActOnePrototype.SeerId,
            WorldFiveActOnePrototype.WitnessId,WorldFiveTutorialWildlifeV1.SeamstalkerId,
            WorldFiveTutorialWildlifeV1.SillrunnerId};
        private static ObjectiveDefinition Stage(string id,K kind,int count,FieldPoint goal,FieldBox zone,
            string[] enemies,string traveler)
        {
            switch(kind)
            {
                case K.Stabilize:return ObjectiveDefinition.Stabilize(id,count,new InteractionDefinition(goal,1.5m,new[]{"$squad"}));
                case K.Secure:return ObjectiveDefinition.Secure(id,zone,new[]{"$squad"},enemies,count,true);
                case K.Protect:return ObjectiveDefinition.Protect(id,traveler,count);
                case K.RescueAndExtract:return ObjectiveDefinition.RescueCarried(id,traveler,zone,
                    new InteractionDefinition(goal,1.5m,new[]{"$leader"}),"$leader");
                case K.Survive:return ObjectiveDefinition.Survive(id,count);
                default:return ObjectiveDefinition.Defeat(id,enemies);
            }
        }
        private static string Instruction(K kind,int count)
        {
            switch(kind)
            {
                case K.Stabilize:return "Reach the gold marker and stabilize it "+count+" time(s). Each interaction uses one action.";
                case K.Secure:return "Hold the gold zone for "+count+" consecutive rounds with enemies outside it.";
                case K.Protect:return "Keep the marked traveler alive for "+count+" rounds; intercept threats and heal when needed.";
                case K.RescueAndExtract:return "Recover the traveler with your first deployed unit, then carry them into the gold exit.";
                case K.Survive:return "Keep at least one squad unit alive for "+count+" rounds.";
                default:return "Defeat the marked hostiles.";
            }
        }
        private static C Make(int n,string title,string briefing,string success,A mood,K kind,int count,
            FieldBox bounds,FieldObstacle[] cover,DifficultGround[] ground,FieldPoint deploy,
            (int type,int x,int z)[] threats,FieldPoint goal,FieldBox zone=null,
            bool procedural=false,K? follow=null,int followCount=1,FieldPoint followGoal=default,
            FieldBox followZone=null,bool bonus=false,K? third=null,int thirdCount=1,
            FieldPoint thirdGoal=default,FieldBox thirdZone=null)
        {
            string id=WorldFiveRouteRecovery.Id(n),traveler=id+"-traveler";
            var actors=new List<ActorSpawn>();
            for(int i=0;i<threats.Length;i++)
            {
                var t=threats[i];
                bool defensive=t.type==2||t.type==3||t.type==4||t.type==5||t.type==7;
                // Local guard envelopes establish a rear line, not a blanket
                // omniscient pursuit. Faster scouts and strikers have separate routes.
                var guard=defensive?B(Math.Max(bounds.Min.X,t.x-5),Math.Max(bounds.Min.Z,t.z-5),
                    Math.Min(bounds.Max.X,t.x+5),Math.Min(bounds.Max.Z,t.z+5)):null;
                decimal preferred=t.type==4?5:t.type==2||t.type==3||t.type==5?4:1.75m;
                var priorities=(kind==K.RescueAndExtract && t.type==0 && i==0)?new[]{"$carrier"}:
                    (kind==K.Protect && t.type==0 && i==0)?new[]{traveler}:null;
                actors.Add(new ActorSpawn(id+"-threat-"+(i+1),Types[t.type],P(t.x,t.z),true,
                    new EnemyBehavior(defensive?EnemyStyle.Defensive:EnemyStyle.Aggressive,preferred,guard,
                        pathNodeLimit:128,objectivePriorityTargets:priorities)));
            }
            var hostile=actors.Select(a=>a.Id).ToArray();
            if(kind==K.Protect||kind==K.RescueAndExtract)actors.Add(new ActorSpawn(traveler,"fixture-npc",goal,false));
            var first=Stage("primary",kind,count,goal,zone,hostile,traveler);
            var additional=new List<ObjectiveDefinition>();
            if(follow.HasValue)additional.Add(Stage("stage-2",follow.Value,followCount,followGoal,followZone,hostile,traveler));
            if(third.HasValue)
            {
                if(!follow.HasValue)throw new ArgumentException("Third stage requires second.");
                additional.Add(Stage("stage-3",third.Value,thirdCount,thirdGoal,thirdZone,hostile,traveler));
            }
            if(bonus && !procedural && !third.HasValue && kind!=K.Defeat && follow!=K.Defeat)
                additional.Add(ObjectiveDefinition.Defeat("optional-threat",hostile));
            string instructions=Instruction(kind,count);
            if(follow.HasValue)instructions="Stage 1: "+instructions+" Stage 2: "+Instruction(follow.Value,followCount);
            if(third.HasValue)instructions+=" Stage 3: "+Instruction(third.Value,thirdCount);
            if(procedural)instructions+=" Secondary cover varies on fresh attempts.";
            var mission=new MissionContent(id,"fixture-a",true,2,new BattlefieldMap(bounds,cover,ground),
                Enumerable.Range(0,4).Select(i=>P(deploy.X+i,deploy.Z)),actors,first,optional:additional,
                environmentKey:mood==A.PaleSurvey||mood==A.MineralBasin?"study-w5-survey":"study-w5-fracture",
                proceduralCover:procedural);
            // First-clear grants increase modestly across the arc; replay stays
            // capped. Earn ordinary fragments, never Apex fragments here.
            string[] fragments={"fragment-fixture-a-4","fragment-fixture-a-2","fragment-fixture-a-3","fragment-fixture-a-1"};
            var rewards=new MissionRewardPolicy(id,Revision,new[]{
                new ResourceGrant("fixture-resource",350+(n-First)*4),
                new ResourceGrant(fragments[(n-First)%4],5+(follow.HasValue?2:0)+(third.HasValue?2:0))
            },new[]{new ResourceGrant("fixture-resource",3)});
            return new C(n,title,briefing,success,instructions,n<=66?"Act Four · Reconnecting the network":
                n<=73?"Act Four · Pressure and accountability":"Act Four · The last approach",
                mood,mission,rewards);
        }

        private static readonly Lazy<IReadOnlyList<C>> Chapters=new Lazy<IReadOnlyList<C>>(()=>Array.AsReadOnly(new[]{
            Make(61,"Beyond containment",
                "The contained interference has quieted, but a nearby route point still rejects the recovered reference. Establish a reading before the next team advances.",
                "The recovered reference is valid at this site. The disturbance is not explained by the one local device.",
                A.PaleSurvey,K.Stabilize,2,B(-9,-7,11,10),Rocks(R(-5,1,-3,4),R(0,-1,2,1),R(6,3,7,5)),
                Ground(G(2,4,4,6)),P(-5,-5),E((2,9,7),(0,8,-3),(4,8,8)),P(5,6),bonus:true),
            Make(62,"Three reference points",
                "Compare three independent markers in order. Each reference must be complete before the next can yield a reliable reading.",
                "All three agree. Their agreement limits the possible explanations for the contradiction without revealing its origin.",
                A.VioletShelves,K.Stabilize,1,B(-9,-6,11,9),Rocks(R(-4,1,-2,3),R(0,2,2,4),R(7,-1,8,1)),
                Ground(G(3,-3,5,-1)),P(-5,-4),E((5,9,7),(2,9,3)),P(3,-2),
                follow:K.Stabilize,followCount:1,followGoal:P(5,2),third:K.Stabilize,thirdCount:1,thirdGoal:P(4,6)),
            Make(63,"A diverted interception",
                "An armed patrol has left the damaged route and is crossing the recovery team's approach. Stop the interception.",
                "The interception has ended. The local opposition no longer blocks the recovery team's path.",
                A.BrokenTerrace,K.Defeat,1,B(-10,-7,12,9),Rocks(R(-5,0,-3,3),R(1,-2,3,0),R(7,3,9,5)),
                Ground(),P(-6,-5),E((0,8,-3),(1,10,2),(2,10,7)),P(0,0),procedural:true),
            Make(64,"Recover the field observer",
                "An observer studying the false impressions has become separated from the party. Recover the observer and return to the marked exit.",
                "The observer returns. Their account describes Afterecho symptoms without identifying the ultimate route-crisis cause.",
                A.MineralBasin,K.RescueAndExtract,1,B(-10,-8,13,10),Rocks(R(-5,0,-3,3),R(0,-2,2,1),R(8,3,10,6)),
                Ground(G(3,3,5,5)),P(-6,-6),E((0,10,-4),(1,11,5),(5,9,8)),P(5,7),B(-8,-7,-3,-4,3)),
            Make(65,"A contested departure",
                "Open the departure lane and restore its reference marker while the defenders are held away from it.",
                "The lane is secured and its reference reports a usable route for the recovery parties.",
                A.BrokenTerrace,K.Secure,2,B(-11,-8,13,11),Rocks(R(-6,1,-4,3),R(0,3,2,5),R(8,-1,10,2)),
                Ground(G(3,-4,6,-2)),P(-6,-6),E((3,11,6),(2,11,-3),(1,8,-4)),P(0,0),B(3,0,6,3,3),
                follow:K.Stabilize,followCount:2,followGoal:P(6,7)),
            Make(66,"Establish a return corridor",
                "Secure the crossing, bring its relay back online, then hold the distant exit while the recovery parties pass.",
                "The two positions and the relay are operational. The party can continue without the unstable hardware.",
                A.DeepFracture,K.Secure,2,B(-11,-8,14,11),Rocks(R(-6,0,-4,3),R(0,-2,2,1),R(8,2,10,5)),
                Ground(G(3,-5,6,-3)),P(-7,-6),E((3,12,8),(4,12,1),(0,9,-4)),P(0,0),B(3,1,6,4,3),
                follow:K.Stabilize,followCount:1,followGoal:P(5,6),third:K.Secure,thirdCount:2,thirdZone:B(7,7,10,10,3)),
            Make(67,"Echo pressure",
                "The team must wait for a complete observation window while intermittent interference distorts the readings.",
                "The observation window concludes. The records show how unreliable the unstable capture devices become under pressure.",
                A.DeepFracture,K.Survive,4,B(-10,-8,12,11),Rocks(R(-5,2,-3,5),R(0,0,2,2),R(7,5,9,7)),
                Ground(G(3,-4,6,-2),G(3,7,5,9)),P(-6,-6),E((4,10,8),(1,10,-4),(2,8,2),(0,7,-3)),P(0,0)),
            Make(68,"Guard the testimony",
                "A witness has agreed to share an account of the Parting. Keep the witness safe, then establish a secure reference for the record.",
                "The testimony is preserved. The Continuance rejects the betrayal of genuine creature bonds as a path to power.",
                A.PaleSurvey,K.Protect,3,B(-10,-7,13,10),Rocks(R(-5,1,-3,4),R(0,-2,2,1),R(7,4,9,6)),
                Ground(G(3,3,5,5)),P(-6,-5),E((0,8,-3),(1,10,6),(2,11,8)),P(-3,-1),
                follow:K.Stabilize,followCount:1,followGoal:P(5,6)),
            Make(69,"Moving screen",
                "The remaining mobile screen is attempting to cut off the observation team. Disperse the armed patrol, not its camp.",
                "The observation route is clear. The team can deliver its findings to the recovery party.",
                A.BrokenTerrace,K.Defeat,1,B(-11,-9,14,10),Rocks(R(-5,-1,-3,3),R(1,-3,3,0),R(6,3,8,6)),
                Ground(),P(-6,-7),E((0,8,-5),(1,11,4),(3,12,7),(2,10,-3)),P(0,0),procedural:true),
            Make(70,"Verify the boundary",
                "Restore the forward marker, hold the contested boundary, then confirm the far reference. Measurements without a safe corridor cannot be trusted.",
                "Both references are stable and the boundary is secured. The team has a defensible basis for continuing its investigation.",
                A.VioletShelves,K.Stabilize,2,B(-12,-8,14,11),Rocks(R(-6,1,-4,4),R(0,-2,2,1),R(8,1,10,4)),
                Ground(G(3,-4,6,-2)),P(-7,-6),E((2,12,6),(3,11,-3),(4,12,9),(1,8,-4)),P(3,2),
                follow:K.Secure,followCount:2,followZone:B(4,4,7,7,3),third:K.Stabilize,thirdGoal:P(8,8)),
            Make(71,"Unquiet basin",
                "A conduit closure has displaced native wildlife into the alternative passage. Hold the field until the party can move safely.",
                "The passage is secure. The wildlife remains a natural part of the fractured landscape, separate from the cult.",
                A.MineralBasin,K.Secure,2,B(-10,-7,12,11),Rocks(R(-5,0,-3,2),R(1,3,3,5),R(8,-1,9,2)),
                Ground(G(3,-4,5,-2)),P(-6,-5),E((6,9,-3),(7,10,7),(6,6,8)),P(0,0),B(4,1,7,4,3)),
            Make(72,"The guarded assembly",
                "An armed group prevents access to a damaged capture assembly. Defeat its defenders before stabilizing the adjacent route reference.",
                "The assembly can no longer prevent the team from reaching the reference. There is no transfer of a creature's bond.",
                A.DeepFracture,K.Defeat,1,B(-12,-8,14,12),Rocks(R(-6,0,-4,3),R(0,2,2,4),R(8,-2,10,1)),
                Ground(G(4,4,6,6)),P(-7,-6),E((3,12,8),(4,11,10),(2,12,-4),(1,8,-3)),P(0,0),
                follow:K.Stabilize,followCount:2,followGoal:P(7,7)),
            Make(73,"Protect the record bearer",
                "The latest comparisons have to reach the departure party. Protect the bearer during the closing observation window.",
                "The record bearer is safe. The investigation distinguishes deliberate Ashbound interference from the wider unexplained instability.",
                A.PaleSurvey,K.Protect,4,B(-11,-8,13,11),Rocks(R(-6,2,-4,4),R(0,-1,2,2),R(7,4,10,6)),
                Ground(G(3,-4,6,-2)),P(-6,-6),E((0,10,-3),(1,10,7),(2,12,2),(5,8,8)),P(-3,-2)),
            Make(74,"Hold the two ends",
                "Secure the near end of the corridor, stabilize the central relay, and secure the far end before the team proceeds.",
                "Both ends of the corridor are defended, and its relay has a trusted reference. The withdrawal path is ready.",
                A.BrokenTerrace,K.Secure,2,B(-12,-9,14,12),Rocks(R(-6,0,-4,3),R(0,-2,2,1),R(9,0,11,3),R(8,6,10,8)),
                Ground(G(3,-5,6,-3)),P(-7,-7),E((3,12,6),(2,12,-4),(5,11,10),(1,8,-4)),P(0,0),B(3,1,6,4,3),
                follow:K.Stabilize,followCount:1,followGoal:P(6,5),third:K.Secure,thirdCount:2,thirdZone:B(4,7,7,10,3)),
            Make(75,"A last mobile threat",
                "A roaming group has diverted toward the newly verified corridor. Stop its advance while the civilian route remains clear.",
                "The mobile group is no longer preventing the passage. No permanent camp has been assaulted.",
                A.DeepFracture,K.Defeat,1,B(-13,-8,14,11),Rocks(R(-7,0,-5,3),R(1,-2,3,1),R(7,4,9,6)),
                Ground(),P(-8,-6),E((0,9,-4),(1,11,-3),(3,12,8),(4,10,9)),P(0,0),procedural:true),
            Make(76,"Retrieve the last surveyor",
                "One surveyor remains beyond the defended route. Recover the traveler, keeping the carrier within support range on the return.",
                "The surveyor is safe. The party has recovered its latest set of measurements without sacrificing the bonded companions.",
                A.VioletShelves,K.RescueAndExtract,1,B(-12,-9,14,11),Rocks(R(-6,1,-4,3),R(0,-2,2,1),R(8,4,10,7)),
                Ground(G(3,2,6,4)),P(-7,-7),E((0,11,-5),(2,12,8),(1,10,1)),P(5,7),B(-9,-8,-4,-5,3)),
            Make(77,"Audit the restored network",
                "Establish fresh measurements at two separated references, then hold the departure zone while the team verifies their agreement.",
                "The two readings agree. The team can leave this portion of the network operating under a verified reference.",
                A.PaleSurvey,K.Stabilize,2,B(-13,-9,15,11),Rocks(R(-7,1,-5,4),R(-1,-2,1,1),R(8,0,10,3)),
                Ground(G(3,-5,6,-3)),P(-8,-7),E((2,13,7),(4,12,9),(1,10,-4)),P(3,1),
                follow:K.Stabilize,followCount:1,followGoal:P(6,6),third:K.Secure,thirdCount:2,thirdZone:B(8,6,11,9,3)),
            Make(78,"A sustained window",
                "Hold the safe lane long enough to complete the next observation window, then restore the reference on its far side.",
                "The observation and reference are complete. The evidence points toward the final investigation rather than confirming an origin.",
                A.MineralBasin,K.Secure,3,B(-11,-9,13,12),Rocks(R(-6,1,-4,4),R(0,3,2,5),R(8,-2,10,1)),
                Ground(G(3,-5,6,-3)),P(-7,-7),E((3,11,8),(2,11,-4),(5,8,7),(1,9,-3)),P(0,0),B(4,1,7,4,3),
                follow:K.Stabilize,followCount:2,followGoal:P(6,8)),
            Make(79,"Secure the final approach",
                "An armed patrol is blocking the last approach to the investigation site. Clear the route and let the recovery team advance.",
                "The approach is clear. The next operation can begin without a pursuit into the dispersed camps.",
                A.BrokenTerrace,K.Defeat,1,B(-13,-9,15,12),Rocks(R(-7,1,-5,3),R(1,-3,3,0),R(8,3,10,6)),
                Ground(),P(-8,-7),E((1,11,-5),(3,13,8),(4,12,10),(5,10,2)),P(0,0),procedural:true),
            Make(80,"Prepare the final investigation",
                "Bring the approach reference online, hold the contested route, then confirm the distant measurement before the next operation begins.",
                "The approach is secure and the measurements are ready. The final investigation remains ahead; neither the wider crisis nor its protected mysteries are resolved.",
                A.DeepFracture,K.Stabilize,2,B(-13,-9,15,12),Rocks(R(-7,0,-5,4),R(-1,2,1,5),R(9,-1,11,2),R(10,8,12,10)),
                Ground(G(3,-5,6,-3),G(3,8,6,10)),P(-8,-7),E((3,13,6),(4,13,10),(2,12,-4),(1,8,-4)),P(3,1),
                follow:K.Secure,followCount:3,followZone:B(4,3,7,6,3),third:K.Stabilize,thirdCount:2,thirdGoal:P(8,8))
        }));
        public static IReadOnlyList<C> All=>Chapters.Value;
        public static bool TryGet(string id,out C chapter)
        {
            chapter=null;
            if(!WorldFiveRouteRecovery.TryNumber(id,out int n)||n<First||n>Last)return false;
            chapter=All[n-First];return true;
        }
        public static ContentCatalog Create()
        {
            var prior=WorldFiveEscalation.Create();
            var existing=prior.Campaigns.Single();
            var chain=existing.Missions.Concat(All.Select(c=>new CampaignMission(c.Mission.Id,
                new[]{WorldFiveRouteRecovery.Id(c.Number-1)})));
            var campaign=new CampaignDefinition(existing.Id,existing.FactionId,Revision,chain,
                existing.CompletionRewards,existing.StarterBonus);
            return new ContentCatalog(prior.Manifest,prior.Resources,prior.Abilities.Values,prior.Kits.Values,
                prior.Units.Values,prior.Missions.Values.Concat(All.Select(c=>c.Mission)),
                prior.Rewards.Values.Concat(All.Select(c=>c.Rewards)),new[]{campaign},prior.StarterUnits,prior.Statuses.Values);
        }
    }
}
