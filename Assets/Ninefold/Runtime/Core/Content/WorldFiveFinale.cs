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
    /// <summary>Append-only Act Five candidate. Resolves the immediate Ashbound
    /// interference campaign, not the protected wider route mystery. None of the proposed story text
    /// establishes a new canonical region, named boss or protected reveal.</summary>
    public static class WorldFiveFinale
    {
        public const int First=81,Last=100;
        public const string Revision="w5-finale-01";
        public const string CompletionReceiptId="w5-final-100-completion";
        public const string TransferCampaignId="w5-next-faction-study";
        public const string TransferMissionId="w5-next-faction-entry";
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
                new ResourceGrant("fixture-resource",430+(n-First)*5),
                new ResourceGrant(fragments[(n-First)%4],5+(follow.HasValue?2:0)+(third.HasValue?2:0))
            },new[]{new ResourceGrant("fixture-resource",3)});
            return new C(n,title,briefing,success,instructions,n<=87?"Act Five · Confirming the source":
                n<=94?"Act Five · Breaking the obstruction":"Act Five · A route restored",
                mood,mission,rewards);
        }

        private static readonly Lazy<IReadOnlyList<C>> Chapters=new Lazy<IReadOnlyList<C>>(()=>Array.AsReadOnly(new[]{
            Make(81,"The last investigation",
                "The verified approach leads to a route reference near the remaining interference equipment. Establish a fresh reading before examining the damaged assembly.",
                "The reading confirms deliberate local interference. Natural route failures elsewhere are still unexplained.",
                A.PaleSurvey,K.Stabilize,2,B(-10,-7,12,10),Rocks(R(-5,1,-3,4),R(0,-1,2,1),R(7,3,9,5)),
                Ground(G(2,4,4,6)),P(-6,-5),E((2,10,7),(0,8,-3),(4,8,8)),P(5,6),bonus:true),
            Make(82,"A record worth defending",
                "Guard a returning investigator while their account of the damaged reference is recorded. Then secure the nearby departure crossing.",
                "The investigator and the account are safe. The evidence identifies the immediate obstruction rather than the origin of the wider crisis.",
                A.MineralBasin,K.Protect,3,B(-11,-8,13,10),Rocks(R(-6,1,-4,4),R(0,-2,2,1),R(8,3,10,6)),
                Ground(G(3,-4,6,-2)),P(-6,-6),E((0,9,-4),(1,11,5),(5,10,8)),P(-3,-1),
                follow:K.Secure,followCount:2,followZone:B(4,3,7,6,3)),
            Make(83,"The southern diversion",
                "A mobile Ashbound patrol is cutting across the verified approach. Break the interception before it reaches the investigators.",
                "The diversion fails. The investigators can continue without pursuing the patrol into its camp.",
                A.BrokenTerrace,K.Defeat,1,B(-11,-8,13,10),Rocks(R(-6,0,-4,3),R(1,-2,3,0),R(8,3,10,5)),
                Ground(),P(-7,-6),E((0,9,-4),(1,11,2),(2,11,8)),P(0,0),procedural:true),
            Make(84,"The displaced traveler",
                "A traveler cannot cross the disrupted passage alone. Recover them and return through the marked safe lane.",
                "The traveler reaches safety. The recovery party records another consequence of the deliberate obstruction.",
                A.VioletShelves,K.RescueAndExtract,1,B(-11,-9,13,11),Rocks(R(-6,0,-4,3),R(0,-2,2,1),R(8,4,10,7)),
                Ground(G(3,3,5,5)),P(-7,-7),E((0,10,-5),(1,11,5),(2,10,9)),P(5,7),B(-9,-8,-4,-5,3)),
            Make(85,"Measure, hold, compare",
                "Read the forward reference, secure the disputed lane, then confirm the reading against the distant marker.",
                "The discrepancy is localized to the equipment along the blocked route. The team has enough evidence to plan its removal.",
                A.DeepFracture,K.Stabilize,2,B(-12,-8,14,11),Rocks(R(-6,2,-4,5),R(0,-2,2,1),R(8,0,10,3)),
                Ground(G(3,-4,6,-2)),P(-7,-6),E((3,12,8),(2,12,-3),(4,11,9),(1,8,-4)),P(3,2),
                follow:K.Secure,followCount:2,followZone:B(4,4,7,7,3),third:K.Stabilize,thirdGoal:P(8,8)),
            Make(86,"Weather the readings",
                "Interference intensifies while an independent observation is underway. Keep the squad operational through the full reading window.",
                "The observation completes. Afterecho-related errors are recorded without claiming the instability is a reliable source of power.",
                A.DeepFracture,K.Survive,5,B(-11,-8,13,11),Rocks(R(-6,2,-4,4),R(0,-1,2,2),R(7,5,9,7)),
                Ground(G(3,-4,6,-2),G(3,7,5,9)),P(-6,-6),E((4,11,9),(1,11,-4),(2,9,3),(0,8,-3)),P(0,0)),
            Make(87,"A passage for the living",
                "The redirected traffic has brought territorial wildlife into the remaining safe passage. Hold the crossing until the route is clear.",
                "The crossing is clear. The native creatures are not blamed for the Ashbound's actions.",
                A.MineralBasin,K.Secure,2,B(-10,-8,12,11),Rocks(R(-5,0,-3,3),R(1,3,3,5),R(8,-1,9,2)),
                Ground(G(3,-4,5,-2)),P(-6,-6),E((6,9,-3),(7,10,7),(6,6,8)),P(0,0),B(4,1,7,4,3)),
            Make(88,"The defended obstruction",
                "An armed rear guard remains at the damaged equipment. Defeat the defenders and establish a usable reference beside it.",
                "The obstruction is accessible. The Continuance can dismantle the interference without adopting the Ashbound's methods.",
                A.DeepFracture,K.Defeat,1,B(-13,-9,15,11),Rocks(R(-7,1,-5,4),R(0,2,2,4),R(9,-2,11,1)),
                Ground(G(4,4,6,6)),P(-8,-7),E((3,13,8),(4,12,9),(2,13,-4),(1,8,-4)),P(0,0),
                follow:K.Stabilize,followCount:2,followGoal:P(7,7)),
            Make(89,"Evacuation under pressure",
                "Protect a route worker while the departure group assembles. Restore the departure marker only after the worker is safe.",
                "The departure team leaves with the record and its surviving members. The remaining operation can proceed.",
                A.PaleSurvey,K.Protect,4,B(-12,-9,14,11),Rocks(R(-6,2,-4,4),R(0,-1,2,2),R(8,4,10,6)),
                Ground(G(3,-5,6,-3)),P(-7,-7),E((0,9,-4),(1,11,7),(2,12,2)),P(-3,-2),
                follow:K.Stabilize,followGoal:P(5,7)),
            Make(90,"Divide the pressure",
                "An interception group is following the withdrawal route. Clear the moving opposition without entering its occupied camp.",
                "The withdrawal route is protected. The opposition can no longer split the recovering parties here.",
                A.BrokenTerrace,K.Defeat,1,B(-12,-8,14,10),Rocks(R(-6,-1,-4,3),R(1,-3,3,0),R(7,3,9,6)),
                Ground(),P(-7,-6),E((0,9,-4),(1,11,2),(3,12,7),(2,11,-3)),P(0,0),procedural:true),
            Make(91,"Secure both exits",
                "Hold the near exit, restore the relay, then secure the far exit. Both ends must be available before the equipment operation starts.",
                "The two exits and their relay are operational. The party will not be trapped by a single reference failure.",
                A.BrokenTerrace,K.Secure,2,B(-13,-9,15,12),Rocks(R(-7,0,-5,3),R(0,-2,2,1),R(9,0,11,3),R(9,6,11,8)),
                Ground(G(3,-5,6,-3)),P(-8,-7),E((3,13,6),(2,13,-4),(5,12,10),(1,8,-4)),P(0,0),B(3,1,6,4,3),
                follow:K.Stabilize,followGoal:P(6,5),third:K.Secure,thirdCount:2,thirdZone:B(4,7,7,10,3)),
            Make(92,"The last stranded worker",
                "Recover the last isolated worker near the obstruction. Return to safety with the evidence they carried.",
                "The worker is recovered. The group can proceed without abandoning anyone at the damaged crossing.",
                A.VioletShelves,K.RescueAndExtract,1,B(-13,-9,14,12),Rocks(R(-7,1,-5,3),R(0,-2,2,1),R(8,4,10,7)),
                Ground(G(3,2,6,4)),P(-8,-7),E((0,11,-5),(2,12,9),(1,10,1)),P(5,8),B(-10,-8,-5,-5,3)),
            Make(93,"Break the remaining screen",
                "Defeat the armed screen defending access to the equipment. Hold the approach, then confirm the exposed reference.",
                "The screen is broken and the approach is stable. No civilian camp has been made a target.",
                A.DeepFracture,K.Defeat,1,B(-12,-9,15,12),Rocks(R(-6,1,-4,4),R(0,3,2,5),R(9,-2,11,1)),
                Ground(G(3,-5,6,-3)),P(-7,-7),E((3,13,8),(4,12,10),(2,13,-4),(1,8,-4)),P(0,0),
                follow:K.Secure,followCount:2,followZone:B(4,1,7,4,3),third:K.Stabilize,thirdGoal:P(6,7)),
            Make(94,"The verified sequence",
                "Take three sequential readings from separate reference points. Confirm the isolated obstruction before committing to the closing operation.",
                "All three readings align. The immediate obstruction can be isolated without claiming a solution to every fracture.",
                A.PaleSurvey,K.Stabilize,1,B(-12,-8,14,11),Rocks(R(-6,1,-4,3),R(0,2,2,4),R(8,-2,10,1)),
                Ground(G(3,-4,6,-2)),P(-7,-6),E((5,12,7),(2,12,3)),P(3,-1),
                follow:K.Stabilize,followGoal:P(5,3),third:K.Stabilize,thirdGoal:P(7,8)),
            Make(95,"A final observation",
                "The closing operation must wait for a verified observation window. Maintain a living squad while the remaining opposition disrupts the area.",
                "The window is complete. The recovered measurements allow the final operation to proceed.",
                A.DeepFracture,K.Survive,5,B(-12,-9,14,12),Rocks(R(-6,2,-4,5),R(0,-1,2,2),R(9,4,11,7)),
                Ground(G(3,-5,6,-3),G(3,8,6,10)),P(-7,-7),E((4,12,10),(2,12,-4),(1,9,-4),(0,8,7)),P(0,0)),
            Make(96,"The last roving patrol",
                "The last mobile interference group threatens the closing operation. Stop the armed patrol before it reaches the route.",
                "The patrol is defeated. The final obstruction no longer has mobile support.",
                A.BrokenTerrace,K.Defeat,1,B(-13,-9,15,11),Rocks(R(-7,1,-5,3),R(1,-3,3,0),R(8,3,10,6)),
                Ground(),P(-8,-7),E((1,11,-5),(3,13,8),(4,12,9),(5,10,2)),P(0,0),procedural:true),
            Make(97,"Protect the departure",
                "Keep the final route worker alive while the team records the closing sequence, then secure the departure zone.",
                "The worker survives and the departure zone is secure. No further civilian lives need be risked.",
                A.MineralBasin,K.Protect,3,B(-12,-8,14,12),Rocks(R(-6,1,-4,4),R(0,-2,2,1),R(8,4,10,7)),
                Ground(G(3,3,5,5)),P(-7,-6),E((0,9,-4),(1,11,6),(2,12,9)),P(-3,-1),
                follow:K.Secure,followCount:2,followZone:B(4,6,7,9,3)),
            Make(98,"Bring the references into agreement",
                "Restore the near reference, hold the central route and confirm the far marker. These readings will support the final isolation.",
                "The route is secured and the references agree. The team can now remove the deliberate local obstruction.",
                A.VioletShelves,K.Stabilize,2,B(-13,-9,15,12),Rocks(R(-7,0,-5,4),R(-1,2,1,5),R(9,-1,11,2)),
                Ground(G(3,-5,6,-3)),P(-8,-7),E((3,13,7),(4,13,10),(2,12,-4),(1,8,-4)),P(3,1),
                follow:K.Secure,followCount:3,followZone:B(4,3,7,6,3),third:K.Stabilize,thirdCount:2,thirdGoal:P(8,8)),
            Make(99,"The last defensive line",
                "The remaining armed defenders block the final crossing. Defeat them, then secure the field for the closing party.",
                "The defense has collapsed. The closing party has a safe approach to the remaining equipment.",
                A.DeepFracture,K.Defeat,1,B(-14,-9,16,12),Rocks(R(-8,1,-6,4),R(0,-2,2,1),R(9,0,11,3),R(10,8,12,10)),
                Ground(G(3,-5,6,-3)),P(-9,-7),E((3,14,6),(4,14,10),(2,13,-4),(1,9,-4)),P(0,0),
                follow:K.Secure,followCount:2,followZone:B(4,4,8,7,3)),
            Make(100,"The route endures",
                "The last armed Ashbound group is defending the local interference assembly. Break its resistance, hold the exposed crossing and restore its reference. Protect the departure; do not pursue beyond the field.",
                "The remaining deliberate obstruction is isolated. The stranded have returned, the crossing is open, and the Continuance preserves its genuine bonds. The immediate Ashbound campaign is won; the greater route crisis remains unexplained.",
                A.DeepFracture,K.Defeat,1,B(-14,-10,16,13),Rocks(R(-8,1,-6,4),R(-1,-2,1,2),R(8,-1,10,3),R(10,7,12,9),R(2,9,4,11)),
                Ground(G(3,-6,6,-4),G(5,8,8,10)),P(-9,-8),E((3,14,5),(4,14,11),(2,13,-5),(1,8,-5),(5,9,11)),P(0,0),
                follow:K.Secure,followCount:3,followZone:B(3,3,7,6,3),third:K.Stabilize,thirdCount:3,thirdGoal:P(8,11))
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
            var prior=WorldFiveConvergence.Create();
            var existing=prior.Campaigns.Single();
            var chain=existing.Missions.Concat(All.Select(c=>new CampaignMission(c.Mission.Id,
                new[]{WorldFiveRouteRecovery.Id(c.Number-1)})));
            // The original small prototype receipt remains valid and earned.
            // A new stable milestone ID is granted only after all 100 required
            // missions; it does not reuse or overwrite that early receipt.
            var campaign=new CampaignDefinition(existing.Id,existing.FactionId,Revision,chain,
                new[]{new ResourceGrant("fixture-resource",600)},
                new StarterCampaignBonus("fixture-a-5",new[]{
                    new NextFactionBundle(TransferCampaignId,new[]{"fixture-b-1","fixture-b-2","fixture-b-3"})}),
                completionClaimId:CompletionReceiptId);
            // Explicit provisional transfer exercise, NOT an invented second
            // world's canonical campaign. It tests the next roster's handoff
            // against the real mission flow until its art and lore are approved.
            var transfer=new MissionContent(TransferMissionId,"fixture-b",true,3,
                new BattlefieldMap(B(-5,-5,9,8),Rocks(R(-3,1,-1,3),R(4,-1,6,1))),
                new[]{P(-2,-3),P(-1,-3),P(0,-3)},Array.Empty<ActorSpawn>(),
                ObjectiveDefinition.Stabilize("primary",1,new InteractionDefinition(P(3,5),1.5m,new[]{"$squad"})),
                environmentKey:"study-neutral-other");
            var transferReward=new MissionRewardPolicy(TransferMissionId,Revision,
                new[]{new ResourceGrant("fixture-resource",30)},Array.Empty<ResourceGrant>());
            var next=new CampaignDefinition(TransferCampaignId,"fixture-b",Revision,
                new[]{new CampaignMission(TransferMissionId)},Array.Empty<ResourceGrant>());
            return new ContentCatalog(prior.Manifest,prior.Resources,prior.Abilities.Values,prior.Kits.Values,
                prior.Units.Values,prior.Missions.Values.Concat(All.Select(c=>c.Mission)).Concat(new[]{transfer}),
                prior.Rewards.Values.Concat(All.Select(c=>c.Rewards)).Concat(new[]{transferReward}),
                new[]{campaign,next},prior.StarterUnits,prior.Statuses.Values);
        }
    }
}
