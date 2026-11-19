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
    /// <summary>Append-only Act Three candidate, continuing the evacuation
    /// into investigation and containment. None of the proposed story text
    /// establishes a new canonical region, named boss or protected reveal.</summary>
    public static class WorldFiveEscalation
    {
        public const int First=35,Last=60;
        public const string Revision="w5-escalation-01";
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
            FieldBox followZone=null,bool bonus=false)
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
                var priorities=kind==K.RescueAndExtract && t.type==0 && i==0?new[]{"$carrier"}:null;
                actors.Add(new ActorSpawn(id+"-threat-"+(i+1),Types[t.type],P(t.x,t.z),true,
                    new EnemyBehavior(defensive?EnemyStyle.Defensive:EnemyStyle.Aggressive,preferred,guard,
                        pathNodeLimit:128,objectivePriorityTargets:priorities)));
            }
            var hostile=actors.Select(a=>a.Id).ToArray();
            if(kind==K.Protect||kind==K.RescueAndExtract)actors.Add(new ActorSpawn(traveler,"fixture-npc",goal,false));
            var first=Stage("primary",kind,count,goal,zone,hostile,traveler);
            var additional=new List<ObjectiveDefinition>();
            if(follow.HasValue)additional.Add(Stage("stage-2",follow.Value,followCount,followGoal,followZone,hostile,traveler));
            if(bonus && !procedural && kind!=K.Defeat && follow!=K.Defeat)
                additional.Add(ObjectiveDefinition.Defeat("optional-threat",hostile));
            string instructions=Instruction(kind,count);
            if(follow.HasValue)instructions="Stage 1: "+instructions+" Stage 2: "+Instruction(follow.Value,followCount);
            if(procedural)instructions+=" Secondary cover varies on fresh attempts.";
            var mission=new MissionContent(id,"fixture-a",true,2,new BattlefieldMap(bounds,cover,ground),
                Enumerable.Range(0,4).Select(i=>P(deploy.X+i,deploy.Z)),actors,first,optional:additional,
                environmentKey:mood==A.PaleSurvey||mood==A.MineralBasin?"study-w5-survey":"study-w5-fracture",
                proceduralCover:procedural);
            // First-clear grants increase modestly across the arc; replay stays
            // capped. Earn ordinary fragments, never Apex fragments here.
            string[] fragments={"fragment-fixture-a-4","fragment-fixture-a-2","fragment-fixture-a-3","fragment-fixture-a-1"};
            var rewards=new MissionRewardPolicy(id,Revision,new[]{
                new ResourceGrant("fixture-resource",245+(n-First)*4),
                new ResourceGrant(fragments[(n-First)%4],5+(follow.HasValue?2:0))
            },new[]{new ResourceGrant("fixture-resource",3)});
            return new C(n,title,briefing,success,instructions,n<=42?"Act Three · Contradictory records":
                n<=51?"Act Three · Pressure at the crossings":"Act Three · Containment",
                mood,mission,rewards);
        }

        private static readonly Lazy<IReadOnlyList<C>> Chapters=new Lazy<IReadOnlyList<C>>(()=>Array.AsReadOnly(new[]{
            Make(35,"A second reading",
                "The evacuation is complete, but the restored route is reporting two incompatible positions. Take a reading from the far marker.",
                "The readings disagree even in the absence of hostile equipment. The investigation cannot attribute every failure to the Ashbound.",
                A.PaleSurvey,K.Stabilize,2,B(-9,-7,12,9),Rocks(R(-4,1,-2,3),R(1,-1,3,1),R(7,4,8,6)),
                Ground(G(3,3,5,5)),P(-5,-5),E((0,8,-2),(2,10,5),(4,7,7)),P(6,6),bonus:true),
            Make(36,"Close the blind lane",
                "A hostile observation group has found a route toward the returning survey teams. Hold the lane, then reestablish its marker.",
                "The lane is secure and its alternate marker is working. The false readings can now be compared across both sites.",
                A.VioletShelves,K.Secure,2,B(-10,-7,12,10),Rocks(R(-5,-1,-3,2),R(1,3,3,5),R(8,1,10,3)),
                Ground(G(2,-3,5,-1)),P(-6,-5),E((1,9,-3),(2,10,7),(5,7,6)),P(0,0),B(4,0,7,3,3),
                follow:K.Stabilize,followCount:1,followGoal:P(6,6)),
            Make(37,"Patrol across the shelf",
                "A roaming interception patrol threatens the survey detour. Keep its attention away from the recovering travelers.",
                "The detour is clear. A mobile patrol need not be pursued into its home camp.",
                A.BrokenTerrace,K.Defeat,1,B(-10,-8,13,9),Rocks(R(-4,0,-2,3),R(2,-2,4,0),R(6,3,8,5)),
                Ground(),P(-6,-6),E((0,8,7),(1,10,-2),(2,11,5),(0,7,-3)),P(0,0),procedural:true),
            Make(38,"Unanswered signal",
                "A route reader remained behind after the recovery operation. Retrieve them before an approaching patrol reaches the site.",
                "The reader returns with a report of distorted sensory impressions, corroborating the survivors' earlier account.",
                A.MineralBasin,K.RescueAndExtract,1,B(-9,-8,12,10),Rocks(R(-5,0,-3,3),R(0,2,2,4),R(5,-2,7,1)),
                Ground(G(3,4,5,6)),P(-5,-6),E((0,9,-4),(1,10,5),(5,8,7)),P(5,7),B(-7,-7,-2,-4,3)),
            Make(39,"Hold through the noise",
                "The signal interference is spreading across the local crossing. Remain operational while another team confirms the records.",
                "The team has endured the false readings. Their timing appears connected to the unstable capture devices.",
                A.DeepFracture,K.Survive,4,B(-10,-7,11,11),Rocks(R(-6,1,-4,4),R(0,-1,2,1),R(6,4,8,7)),
                Ground(G(3,-4,6,-2)),P(-6,-5),E((4,9,8),(1,8,-3),(2,6,6)),P(0,0)),
            Make(40,"Witness at the edge",
                "A traveler has asked to share what they witnessed near an empty harness. Protect them while they finish their account.",
                "The testimony confirms the Parting has a human cost and is not a necessary act of survival. The team preserves the report.",
                A.VioletShelves,K.Protect,3,B(-9,-7,13,9),Rocks(R(-4,2,-2,4),R(1,-2,3,1),R(7,3,9,5)),
                Ground(G(3,4,5,6)),P(-5,-5),E((0,8,-2),(2,11,6),(1,9,7)),P(-2,-1),bonus:true),
            Make(41,"Separate the readings",
                "Two distant markers retain contradictory records. Establish one safe reference before bringing the second online.",
                "Both reference points now agree with each other. The earlier discrepancy was not merely a damaged route instrument.",
                A.PaleSurvey,K.Stabilize,2,B(-11,-7,13,10),Rocks(R(-5,-1,-3,2),R(1,1,3,3),R(7,3,9,6)),
                Ground(G(3,-4,5,-2)),P(-6,-5),E((2,11,7),(5,9,-3)),P(4,-1),
                follow:K.Stabilize,followCount:2,followGoal:P(5,7)),
            Make(42,"Break the observation line",
                "A distant Ashbound line is preventing the team from comparing the restored references. Disperse the armed opposition.",
                "The observation line is broken. The Continuance recovers space to investigate without attacking a civilian camp.",
                A.DeepFracture,K.Defeat,1,B(-11,-8,13,11),Rocks(R(-6,0,-4,3),R(0,-2,2,1),R(5,2,7,5),R(9,6,11,8)),
                Ground(G(2,5,4,7)),P(-6,-6),E((3,11,-3),(4,10,9),(2,9,1),(1,8,-4)),P(0,0)),
            Make(43,"Wild crossing",
                "The survey detour passes through territorial native creatures. Hold the marked passage without treating the ecology as a cult.",
                "The passage is open. Native wildlife continues to respond to its own environment, not to Ashbound orders.",
                A.MineralBasin,K.Secure,2,B(-10,-8,12,10),Rocks(R(-5,0,-3,3),R(1,-2,3,0),R(7,3,9,6)),
                Ground(G(2,4,4,7)),P(-6,-6),E((7,10,7),(6,9,-4),(7,6,8)),P(0,0),B(4,1,6,4,3)),
            Make(44,"Intercept the advance",
                "A patrol has shifted toward the sheltered crossing. Clear the approach before the support team moves through.",
                "The path is safe enough for the next group to cross. The conflict remains local.",
                A.BrokenTerrace,K.Defeat,1,B(-12,-8,14,10),Rocks(R(-6,1,-4,4),R(0,-3,2,0),R(7,1,9,4)),
                Ground(),P(-7,-6),E((1,11,-3),(0,8,-4),(3,12,6),(5,9,8)),P(0,0),procedural:true),
            Make(45,"A threatened relay",
                "A route technician must complete a reading before the relay can be restarted. Keep them safe from the moving patrol.",
                "The technician survives and the relay is operational again. The route team's records are intact.",
                A.PaleSurvey,K.Protect,4,B(-10,-7,12,11),Rocks(R(-5,2,-3,5),R(0,-1,2,2),R(7,4,9,6)),
                Ground(G(3,-4,6,-2)),P(-6,-5),E((0,8,-4),(1,10,5),(2,9,8)),P(-3,-1),
                follow:K.Stabilize,followCount:1,followGoal:P(5,5)),
            Make(46,"Crossing under watch",
                "Restore a compromised route point from beyond the obstruction. The defenders are positioned to deny both approaches.",
                "The point is restored. Its readings reveal another nearby interference source without exposing the wider crisis.",
                A.VioletShelves,K.Stabilize,3,B(-11,-8,13,10),Rocks(R(-6,-1,-4,2),R(0,1,3,3),R(7,-2,9,1)),
                Ground(G(3,4,6,6)),P(-6,-6),E((2,11,6),(4,8,7),(1,10,-4),(5,12,1)),P(6,3),bonus:true),
            Make(47,"Return before collapse",
                "A late survey party is stranded near the edge of a fracture shelf. Recover their remaining traveler and return to the secure lane.",
                "The traveler is safe. The retrieved instruments record the same short window of contradictory route signals.",
                A.DeepFracture,K.RescueAndExtract,1,B(-12,-8,13,11),Rocks(R(-6,1,-4,3),R(0,-2,2,1),R(7,4,9,7)),
                Ground(G(3,2,6,4)),P(-7,-6),E((0,10,-4),(2,11,7),(1,8,-2)),P(5,7),B(-9,-7,-4,-4,3)),
            Make(48,"Hold the junction",
                "The evacuation lane and investigation route now meet at a contested junction. Hold it before redirecting the signal.",
                "The junction is held and redirected. The two routes can operate without relying on the compromised reference.",
                A.BrokenTerrace,K.Secure,3,B(-12,-8,14,11),Rocks(R(-6,0,-4,4),R(0,3,2,5),R(9,0,11,3)),
                Ground(G(3,-4,6,-2)),P(-7,-6),E((3,12,7),(2,11,-4),(5,9,6),(1,8,-3)),P(0,0),B(4,1,7,4,3),
                follow:K.Stabilize,followCount:1,followGoal:P(6,7)),
            Make(49,"Moving interference",
                "A roaming patrol is carrying unstable hardware along the detour. Stop the attackers before they reach the restored corridor.",
                "The mobile hardware is no longer interfering with the corridor. Its instability remains dangerous to its operators.",
                A.DeepFracture,K.Defeat,1,B(-11,-9,14,10),Rocks(R(-5,-1,-3,2),R(2,-3,4,0),R(7,3,9,6)),
                Ground(),P(-6,-7),E((4,11,7),(1,10,-4),(0,8,-5),(2,12,2)),P(0,0),procedural:true),
            Make(50,"Endure the feedback",
                "The recovery equipment is producing interference bursts. Hold the team together until the reading window ends.",
                "The team survives the feedback. The records show the Afterecho is a persistent vulnerability, not a source of unlimited strength.",
                A.MineralBasin,K.Survive,4,B(-10,-8,13,11),Rocks(R(-5,2,-3,4),R(1,-1,3,2),R(8,4,10,6)),
                Ground(G(3,4,6,6),G(4,-5,7,-3)),P(-6,-6),E((4,11,9),(1,9,-4),(2,10,1),(0,7,-5)),P(0,0)),
            Make(51,"A narrow departure",
                "A traveler carrying the latest records has to wait near the return route. Protect the traveler until their passage becomes safe.",
                "The records reach the recovery team. The comparisons point to a localized interference network rather than a confirmed origin of the crisis.",
                A.VioletShelves,K.Protect,3,B(-11,-9,13,10),Rocks(R(-6,0,-4,3),R(1,2,3,4),R(7,-2,9,1)),
                Ground(G(3,-5,6,-3)),P(-6,-7),E((0,9,-4),(1,11,6),(2,10,8),(5,8,5)),P(-3,-2)),
            Make(52,"Anchor the reference",
                "Containment requires a dependable reference at the edge of the affected route. Restore two points in sequence.",
                "The two reference points are active. They establish a reliable local baseline without explaining the larger disruption.",
                A.PaleSurvey,K.Stabilize,2,B(-12,-8,14,11),Rocks(R(-6,1,-4,3),R(-1,-2,1,1),R(5,3,7,5),R(10,-2,11,1)),
                Ground(G(3,-4,5,-2)),P(-7,-6),E((2,12,7),(4,10,9),(1,9,-4)),P(3,2),
                follow:K.Stabilize,followCount:2,followGoal:P(8,7)),
            Make(53,"Defend the outer lane",
                "Hostile defenders are contesting the approach to the interference source. Hold the lane while the recovery team moves past.",
                "The outer lane is clear. The team can proceed toward the equipment without entering the camp.",
                A.BrokenTerrace,K.Secure,3,B(-12,-9,14,11),Rocks(R(-6,-1,-4,3),R(0,4,2,6),R(8,0,10,3)),
                Ground(G(2,-5,5,-3)),P(-7,-7),E((3,12,7),(2,11,-4),(5,9,6),(1,7,-4)),P(0,0),B(3,1,7,4,3),bonus:true),
            Make(54,"Sweep the detour",
                "The remaining mobile defenders have moved toward the alternate crossing. Clear the detour and leave the camp itself untouched.",
                "The detour is open. The local opposition has lost its ability to delay the evacuation path.",
                A.DeepFracture,K.Defeat,1,B(-13,-8,15,11),Rocks(R(-7,0,-5,3),R(1,-2,3,1),R(8,4,10,6)),
                Ground(),P(-8,-6),E((0,9,-4),(1,12,-3),(3,13,7),(2,11,9)),P(0,0),procedural:true),
            Make(55,"Recover the instrument keeper",
                "A stranded keeper has the last usable equipment notes. Recover the traveler and return to the marked departure zone.",
                "The keeper returns safely. Their notes allow the team to distinguish unstable capture hardware from a genuine creature bond.",
                A.MineralBasin,K.RescueAndExtract,1,B(-12,-9,14,12),Rocks(R(-6,1,-4,4),R(0,-2,2,1),R(8,4,10,7)),
                Ground(G(3,2,6,4)),P(-7,-7),E((0,11,-5),(2,12,8),(1,10,1)),P(5,8),B(-9,-8,-4,-5,3)),
            Make(56,"Disable the instrument",
                "Clear an armed group guarding a damaged capture assembly. Once the immediate danger is gone, restore the nearby route reference.",
                "The assembly no longer obstructs the route. The Continuance has not adopted the Ashbound's method.",
                A.DeepFracture,K.Defeat,1,B(-11,-8,14,12),Rocks(R(-5,0,-3,3),R(1,2,3,4),R(8,-2,10,1)),
                Ground(G(4,5,6,7)),P(-6,-6),E((3,12,8),(4,11,10),(1,10,-4),(0,7,-3)),P(0,0),
                follow:K.Stabilize,followCount:1,followGoal:P(7,7)),
            Make(57,"Wait for the return signal",
                "The restored reference needs a complete reading cycle before another crossing can open. Survive the remaining pressure.",
                "The return signal is stable across the full reading cycle. The team can begin closing the affected route safely.",
                A.VioletShelves,K.Survive,5,B(-12,-8,13,12),Rocks(R(-6,2,-4,5),R(0,-1,2,2),R(8,4,10,7)),
                Ground(G(3,-4,6,-2)),P(-7,-6),E((4,11,10),(2,10,-3),(1,9,-4),(0,7,7)),P(0,0)),
            Make(58,"The last relay",
                "The closing procedure requires one trusted relay to link the recovered corridor to the safe departure route. Stabilize it, then hold the exit.",
                "The last relay has responded and the departure lane is secure. The team is ready for containment.",
                A.PaleSurvey,K.Stabilize,2,B(-13,-9,15,12),Rocks(R(-7,0,-5,3),R(-1,3,1,6),R(8,-1,10,2),R(10,7,12,9)),
                Ground(G(3,-5,6,-3)),P(-8,-7),E((2,13,8),(3,12,-5),(0,8,-4)),P(4,1),
                follow:K.Secure,followCount:2,followZone:B(4,5,7,8,3)),
            Make(59,"The remaining patrol",
                "A final mobile patrol is threatening the departure corridor. Defeat the attackers before the containment team arrives.",
                "The departure corridor is clear of the patrol. No pursuit into the dispersed camp is necessary.",
                A.BrokenTerrace,K.Defeat,1,B(-13,-9,14,12),Rocks(R(-7,1,-5,4),R(1,-3,3,0),R(7,4,9,7)),
                Ground(),P(-8,-7),E((1,11,-5),(3,12,8),(4,12,10),(5,9,2)),P(0,0),procedural:true),
            Make(60,"Containment",
                "Hold the last crossing until the team can isolate the local interference, then confirm its route marker. Do not pursue fleeing opposition.",
                "The local interference is contained and the corridor is stable. The records raise unresolved questions for the next campaign chapter.",
                A.DeepFracture,K.Secure,3,B(-13,-9,15,12),Rocks(R(-7,0,-5,4),R(-1,3,1,6),R(9,0,11,3),R(10,8,12,10)),
                Ground(G(3,-5,6,-3),G(3,8,6,10)),P(-8,-7),E((3,13,6),(4,13,10),(2,12,-4),(1,8,-4)),P(0,0),B(4,1,7,4,3),
                follow:K.Stabilize,followCount:2,followGoal:P(7,7))
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
            var prior=WorldFiveRouteRecovery.Create();
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
