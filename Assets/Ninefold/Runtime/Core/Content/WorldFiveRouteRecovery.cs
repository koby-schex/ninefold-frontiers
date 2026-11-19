using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ninefold.Core.Campaigns;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;
using Ninefold.Core.Progression;

namespace Ninefold.Core.Content
{
    /// <summary>Append-only, save-compatible continuation of the current live
    /// World Five prototype. Unnamed sites and dialogue are story candidates;
    /// no new locked canon, character, species or fundamental reveal.</summary>
    public static class WorldFiveRouteRecovery
    {
        public const string Revision="w5-route-recovery-01";
        public const int First=15,Last=34;
        public static string Id(int n)=>"w5-story-"+n.ToString("D3",CultureInfo.InvariantCulture);
        public static bool TryNumber(string id,out int n)
        {
            n=0;
            return id!=null&&id.Length==12&&id.StartsWith("w5-story-",StringComparison.Ordinal)&&
                int.TryParse(id.Substring(9),NumberStyles.None,CultureInfo.InvariantCulture,out n)&&n>=First&&n<=WorldFiveConvergence.Last;
        }
        public sealed class Chapter
        {
            public readonly int Number;
            public readonly string Title,Briefing,Success,Instructions,Act;
            public readonly BattlefieldArtPlan.Mood Mood;
            public readonly MissionContent Mission;
            public readonly MissionRewardPolicy Rewards;
            internal Chapter(int number,string title,string briefing,string success,string instructions,
                string act,BattlefieldArtPlan.Mood mood,MissionContent mission,MissionRewardPolicy rewards)
            {Number=number;Title=title;Briefing=briefing;Success=success;Instructions=instructions;
                Act=act;Mood=mood;Mission=mission;Rewards=rewards;}
        }
        private static FieldPoint P(decimal x,decimal z)=>new FieldPoint(x,0,z);
        private static FieldBox B(decimal x,decimal z,decimal r,decimal t,decimal h=5)
            =>new FieldBox(P(x,z),new FieldPoint(r,h,t));
        private static FieldObstacle C(decimal x,decimal z,decimal r,decimal t,decimal h=2)
            =>new FieldObstacle(B(x,z,r,t,h),true,true);
        private static DifficultGround G(decimal x,decimal z,decimal r,decimal t)=>
            new DifficultGround(B(x,z,r,t,.1m),1.5m);
        private static readonly string[] Types={
            WorldFiveActOnePrototype.ScoutId,WorldFiveActOnePrototype.StrikerId,
            WorldFiveActOnePrototype.CastigatorId,WorldFiveActOnePrototype.WreckerId,
            WorldFiveActOnePrototype.SeerId,WorldFiveActOnePrototype.WitnessId,
            WorldFiveTutorialWildlifeV1.SeamstalkerId,WorldFiveTutorialWildlifeV1.SillrunnerId};
        private static Chapter Make(int n,string title,string briefing,string success,
            BattlefieldArtPlan.Mood mood,ObjectiveKind kind,int required,
            FieldBox bounds,FieldObstacle[] cover,DifficultGround[] ground,FieldPoint deploy,
            (int type,int x,int z)[] threats,FieldPoint goal,FieldBox zone=null,
            bool procedural=false,bool four=false,bool bonus=false)
        {
            string id=Id(n),friendly=id+"-traveler";
            var spawns=new List<ActorSpawn>();
            for(int i=0;i<threats.Length;i++)
            {
                var e=threats[i];
                bool defensive=e.type==2||e.type==3||e.type==4||e.type==5||e.type==7;
                decimal distance=e.type==4?5:e.type==2||e.type==3||e.type==5?4:e.type==0?2:1.5m;
                spawns.Add(new ActorSpawn(id+"-threat-"+(i+1),Types[e.type],P(e.x,e.z),true,
                    new EnemyBehavior(defensive?EnemyStyle.Defensive:EnemyStyle.Aggressive,distance,
                        defensive?bounds:null,pathNodeLimit:128)));
            }
            string[] hostile=spawns.Select(a=>a.Id).ToArray();
            if(kind==ObjectiveKind.Protect||kind==ObjectiveKind.RescueAndExtract)
                spawns.Add(new ActorSpawn(friendly,"fixture-npc",goal,false));
            ObjectiveDefinition objective;
            string instructions;
            switch(kind)
            {
                case ObjectiveKind.Secure:
                    objective=ObjectiveDefinition.Secure("primary",zone,new[]{"$squad"},hostile,required,true);
                    instructions="Hold the gold zone for "+required+" consecutive rounds. Keep hostile units out.";
                    break;
                case ObjectiveKind.Protect:
                    objective=ObjectiveDefinition.Protect("primary",friendly,required);
                    instructions="Keep the traveler alive for "+required+" rounds. Intercept threats and use healing.";
                    break;
                case ObjectiveKind.RescueAndExtract:
                    objective=ObjectiveDefinition.RescueCarried("primary",friendly,zone,
                        new InteractionDefinition(goal,1.5m,new[]{"$leader"}),"$leader");
                    instructions="Use your first deployed unit to recover the traveler, then carry them into the gold extraction zone.";
                    break;
                case ObjectiveKind.Stabilize:
                    objective=ObjectiveDefinition.Stabilize("primary",required,
                        new InteractionDefinition(goal,1.5m,new[]{"$squad"}));
                    instructions="Reach the marked route point and stabilize it "+required+" times. Each interaction uses an action.";
                    break;
                case ObjectiveKind.Survive:
                    objective=ObjectiveDefinition.Survive("primary",required);
                    instructions="Keep at least one squad unit alive for "+required+" rounds. You do not need to defeat every enemy.";
                    break;
                default:
                    objective=ObjectiveDefinition.Defeat("primary",hostile);
                    instructions=procedural?"Defeat the opposing patrol. Secondary cover varies on fresh attempts.":
                        "Defeat the marked opposition. Use the terrain and distinct enemy ranges to your advantage.";
                    break;
            }
            var deployment=Enumerable.Range(0,four?4:3).Select(i=>P(deploy.X+i,deploy.Z)).ToArray();
            var extras=bonus&&!procedural&&kind!=ObjectiveKind.Defeat?
                new[]{ObjectiveDefinition.Defeat("optional-threat",hostile)}:Array.Empty<ObjectiveDefinition>();
            // The first four standards remain optional unlocks; never require
            // a paid, four-unit or Apex squad to make campaign progress.
            var mission=new MissionContent(id,"fixture-a",true,2,
                new BattlefieldMap(bounds,cover,ground),deployment,spawns,objective,
                optional:extras,environmentKey:mood==BattlefieldArtPlan.Mood.PaleSurvey||
                mood==BattlefieldArtPlan.Mood.MineralBasin?"study-w5-survey":"study-w5-fracture",
                proceduralCover:procedural);
            int supplies=145+5*(n-First);
            var fragments=new[]{"fragment-fixture-a-4","fragment-fixture-a-2","fragment-fixture-a-3","fragment-fixture-a-1"};
            var reward=new MissionRewardPolicy(id,Revision,new[]{
                new ResourceGrant("fixture-resource",supplies),
                new ResourceGrant(fragments[(n-First)%4],5)
            },new[]{new ResourceGrant("fixture-resource",3)});
            string act=n<=20?"Act One · Broken connections":n<=27?"Act Two · Camps and crossings":
                "Act Two · Restoring the routes";
            return new Chapter(n,title,briefing,success,instructions,act,mood,mission,reward);
        }

        // Individually authored tactical footprints and story beats. Distinct
        // maps are not generated solely by incrementing a mission counter.
        private static readonly Lazy<IReadOnlyList<Chapter>> Chapters=new Lazy<IReadOnlyList<Chapter>>(()=>Array.AsReadOnly(new[]{
            Make(15,"Interrupted relay",
                "The captured equipment points to a relay that stopped reporting. Its route marker is still intact.",
                "The relay is responding again. Several missing reports came from the same stretch of crossings.",
                BattlefieldArtPlan.Mood.PaleSurvey,ObjectiveKind.Stabilize,2,
                B(-7,-6,9,8),new[]{C(-3,0,-1,2),C(2,3,4,5)},new[]{G(1,-2,3,0)},P(-4,-4),
                new[]{(0,5,5),(2,7,0)},P(5,2),bonus:true),
            Make(16,"Missing survey party",
                "A survey traveler has been cut off from the return route. Recover them before the patrol can close in.",
                "The traveler is safe. Their report describes abandoned equipment and an unsafe detour.",
                BattlefieldArtPlan.Mood.VioletShelves,ObjectiveKind.RescueAndExtract,1,
                B(-9,-6,11,9),new[]{C(-5,1,-3,4),C(0,-1,2,2),C(5,3,7,5)},new[]{G(3,-3,5,-1)},P(-5,-4),
                new[]{(0,7,7),(1,9,-1)},P(3,5),B(-7,-5,-2,-2,3)),
            Make(17,"Open the approach",
                "An anonymous Ashbound patrol is restricting access to a crossing. Clear it so the recovery team can pass.",
                "The approach is clear. No permanent settlement was attacked.",
                BattlefieldArtPlan.Mood.BrokenTerrace,ObjectiveKind.Defeat,1,
                B(-8,-7,10,8),new[]{C(-3,0,-1,2),C(3,2,5,4)},Array.Empty<DifficultGround>(),P(-4,-5),
                new[]{(0,6,-1),(1,7,5),(0,8,2)},P(0,0),procedural:true),
            Make(18,"Temporary shelter",
                "A stranded traveler must wait for a safe route signal. Keep the shelter party alive until it arrives.",
                "The traveler can leave. The team has learned which readings remain dependable under pressure.",
                BattlefieldArtPlan.Mood.MineralBasin,ObjectiveKind.Protect,3,
                B(-7,-5,10,9),new[]{C(-4,2,-2,4),C(1,1,3,3),C(5,-2,6,1)},new[]{G(3,5,5,7)},P(-4,-3),
                new[]{(1,7,4),(0,6,-3)},P(-2,0)),
            Make(19,"A lane through the shelves",
                "Hold a narrow crossing for the returning survey team. Do not pursue enemies beyond the lane.",
                "The return lane remained open long enough for the group to cross.",
                BattlefieldArtPlan.Mood.VioletShelves,ObjectiveKind.Secure,2,
                B(-9,-7,10,10),new[]{C(-5,-1,-3,3),C(0,3,2,5),C(7,-1,8,3)},new[]{G(2,-3,4,-1)},P(-5,-5),
                new[]{(3,8,6),(5,5,7)},P(0,0),B(3,1,6,4,3),bonus:true),
            Make(20,"Faulted instruments",
                "A capture assembly is throwing conflicting signals. Outlast the hostile pressure while the team compares readings.",
                "The repeated false readings suggest a dangerous drawback to the Ashbound's methods, not immunity to harm.",
                BattlefieldArtPlan.Mood.DeepFracture,ObjectiveKind.Survive,3,
                B(-8,-6,12,9),new[]{C(-4,0,-2,2),C(0,2,2,4),C(5,-1,7,1)},new[]{G(3,5,5,7)},P(-4,-4),
                new[]{(4,9,6),(1,8,-2),(2,6,4)},P(0,0)),
            Make(21,"Camp perimeter",
                "A small patrol is defending recovered supplies on the approach to a dispersed camp. Clear the route, not the camp.",
                "The path is accessible. The Continuance can continue without threatening noncombatants.",
                BattlefieldArtPlan.Mood.BrokenTerrace,ObjectiveKind.Defeat,1,
                B(-10,-7,12,10),new[]{C(-4,1,-2,3),C(1,-2,3,0),C(6,3,8,5)},Array.Empty<DifficultGround>(),P(-5,-5),
                new[]{(5,9,7),(2,9,-1),(0,5,7)},P(0,0),procedural:true,four:true),
            Make(22,"Recover the route record",
                "A damaged route point contains the last useful record of a crossing. Reestablish it before leaving.",
                "The recovered record confirms that some equipment had been redirected. The wider crisis remains unexplained.",
                BattlefieldArtPlan.Mood.PaleSurvey,ObjectiveKind.Stabilize,2,
                B(-9,-6,11,8),new[]{C(-4,0,-2,2),C(2,1,4,3),C(7,-3,8,-1)},new[]{G(0,4,2,6)},P(-5,-4),
                new[]{(2,8,5),(4,9,1)},P(5,5),four:true,bonus:true),
            Make(23,"A survivor's warning",
                "A traveler near discarded companion equipment has asked for a safe exit. Keep them out of the confrontation.",
                "The survivor describes an initiation that destroys a genuine bond. The account is testimony, not a completed investigation.",
                BattlefieldArtPlan.Mood.DeepFracture,ObjectiveKind.RescueAndExtract,1,
                B(-8,-7,12,10),new[]{C(-3,1,-1,4),C(2,-2,4,0),C(7,4,9,6)},new[]{G(4,1,6,3)},P(-4,-5),
                new[]{(1,10,2),(0,8,-3),(5,10,8)},P(5,6),B(-6,-6,-1,-3,3),four:true),
            Make(24,"Natural crossing",
                "A route diversion approaches territorial wildlife. Hold the far-side passage without treating every animal as an Ashbound threat.",
                "The team has a safe crossing. The encounter was ecological, not evidence of an alliance.",
                BattlefieldArtPlan.Mood.MineralBasin,ObjectiveKind.Secure,2,
                B(-8,-6,10,9),new[]{C(-4,-1,-2,2),C(0,3,2,5),C(6,1,7,3)},new[]{G(2,-3,5,-1)},P(-4,-4),
                new[]{(7,7,6),(6,8,-2)},P(0,0),B(3,3,5,6,3),four:true),
            Make(25,"Escort under fracture",
                "A route reader needs time to inspect damaged hardware. Protect the reader while the patrol approaches.",
                "The inspection is complete. The capture mechanism is engineered and unstable; it does not create a living bond.",
                BattlefieldArtPlan.Mood.VioletShelves,ObjectiveKind.Protect,3,
                B(-10,-6,11,9),new[]{C(-5,2,-3,4),C(1,-1,3,2),C(6,4,8,6)},new[]{G(3,5,5,7)},P(-5,-4),
                new[]{(0,8,-3),(2,9,2),(1,7,7)},P(-2,-1),four:true),
            Make(26,"Patrol diversion",
                "A mobile patrol has broken away from the camp. Clear the diversion before reopening the main route.",
                "The patrol no longer blocks the return path.",
                BattlefieldArtPlan.Mood.BrokenTerrace,ObjectiveKind.Defeat,1,
                B(-9,-8,12,9),new[]{C(-4,-1,-2,2),C(4,1,6,4)},Array.Empty<DifficultGround>(),P(-5,-6),
                new[]{(1,8,5),(3,9,-1),(0,7,7)},P(0,0),procedural:true,four:true),
            Make(27,"Signal window",
                "The next safe departure window is brief. Keep the team operating until a reliable signal returns.",
                "The signal is stable long enough to move. The team's evidence still leaves the central route crisis unresolved.",
                BattlefieldArtPlan.Mood.DeepFracture,ObjectiveKind.Survive,4,
                B(-9,-7,11,10),new[]{C(-4,1,-2,3),C(0,-2,2,1),C(5,2,7,5)},new[]{G(-1,5,2,7)},P(-5,-5),
                new[]{(4,9,7),(2,8,-3),(0,7,6)},P(0,0),four:true),
            Make(28,"Return conduit",
                "The team is attempting to restore a damaged return route, using the records already recovered.",
                "A dependable return signal is restored. The party can start bringing stranded travelers home.",
                BattlefieldArtPlan.Mood.PaleSurvey,ObjectiveKind.Stabilize,3,
                B(-9,-7,12,8),new[]{C(-5,0,-3,2),C(1,2,3,4),C(7,-2,8,1)},new[]{G(3,-3,5,-1)},P(-5,-5),
                new[]{(2,9,5),(5,10,2)},P(6,5),four:true,bonus:true),
            Make(29,"Far-side pickup",
                "A final stranded traveler is waiting beyond a damaged shelf. An Ashbound interception team is nearby.",
                "The traveler has returned safely. The rescue establishes another reliable route, not a final answer to the crisis.",
                BattlefieldArtPlan.Mood.VioletShelves,ObjectiveKind.RescueAndExtract,1,
                B(-10,-7,12,10),new[]{C(-5,0,-3,3),C(0,-2,2,1),C(6,3,8,5)},new[]{G(3,2,5,4)},P(-6,-5),
                new[]{(1,10,1),(2,9,7),(0,8,-3)},P(4,6),B(-8,-6,-3,-3,3),four:true),
            Make(30,"Keep the exit open",
                "The recovering teams need a secure exit while the broader party withdraws from the contested route.",
                "The exit is open. The withdrawing team is clear of the immediate danger.",
                BattlefieldArtPlan.Mood.BrokenTerrace,ObjectiveKind.Secure,3,
                B(-10,-8,13,10),new[]{C(-5,-1,-3,2),C(0,2,2,4),C(8,1,10,3)},new[]{G(2,-4,5,-2)},P(-6,-6),
                new[]{(3,10,7),(2,11,-3),(5,8,6)},P(0,0),B(4,2,7,5,3),four:true,bonus:true),
            Make(31,"Last interception",
                "A final roaming patrol is attempting to cut off the recovering route. Clear the tactical approach.",
                "The immediate interception is over. The team can focus on completing the restoration.",
                BattlefieldArtPlan.Mood.DeepFracture,ObjectiveKind.Defeat,1,
                B(-11,-7,13,10),new[]{C(-5,0,-3,3),C(2,-2,4,1),C(7,4,9,6)},Array.Empty<DifficultGround>(),P(-6,-5),
                new[]{(1,10,-2),(3,11,7),(4,10,2)},P(0,0),procedural:true,four:true),
            Make(32,"Holding the record",
                "Protect the traveler carrying the investigation's recovered reports as the last readings synchronize.",
                "The records are secured and their carrier is safe. Nothing yet identifies the full cause of the route crisis.",
                BattlefieldArtPlan.Mood.MineralBasin,ObjectiveKind.Protect,3,
                B(-10,-8,12,10),new[]{C(-5,2,-3,5),C(0,-1,2,2),C(6,3,8,5)},new[]{G(3,-4,5,-2)},P(-6,-6),
                new[]{(0,8,-4),(2,10,3),(1,9,7)},P(-3,-2),four:true),
            Make(33,"Restore the crossing",
                "With the records secured, stabilize the remaining route point before the safe window closes.",
                "The crossing responds. The Continuance has a reliable path for the immediate evacuation.",
                BattlefieldArtPlan.Mood.VioletShelves,ObjectiveKind.Stabilize,3,
                B(-11,-7,13,11),new[]{C(-6,1,-4,3),C(-1,-2,1,0),C(4,4,6,6),C(9,-1,10,2)},new[]{G(2,1,4,3)},P(-6,-5),
                new[]{(4,11,8),(3,11,-3),(2,8,3)},P(7,7),four:true,bonus:true),
            Make(34,"Safe passage",
                "Hold the reopened route while the last party crosses. End the local confrontation without pursuing the Ashbound into their camps.",
                "The stranded parties have a safe return route. The investigation can continue in the next chapter; the wider crisis remains unresolved.",
                BattlefieldArtPlan.Mood.DeepFracture,ObjectiveKind.Secure,3,
                B(-11,-8,13,11),new[]{C(-6,0,-4,3),C(-1,3,1,5),C(8,0,10,3)},new[]{G(2,-4,5,-2),G(4,7,7,9)},P(-6,-6),
                new[]{(3,11,6),(1,9,-3),(4,11,9),(5,7,8)},P(0,0),B(4,1,7,4,3),four:true,bonus:true)
        }));
        public static IReadOnlyList<Chapter> All=>Chapters.Value;
        public static bool TryGet(string id,out Chapter chapter)
        {
            chapter=null;
            if(!TryNumber(id,out int n))return false;
            if(n>WorldFiveEscalation.Last)return WorldFiveConvergence.TryGet(id,out chapter);
            if(n>Last)return WorldFiveEscalation.TryGet(id,out chapter);
            chapter=All[n-First];return true;
        }

        public static ContentCatalog Create()
        {
            var source=WorldFiveTutorialWildlifeV1.Create();
            var prior=source.Campaigns.Single();
            var chain=prior.Missions.Concat(All.Select(c=>new CampaignMission(c.Mission.Id,
                new[]{Id(c.Number-1)})));
            var campaign=new CampaignDefinition(prior.Id,prior.FactionId,Revision,chain,
                prior.CompletionRewards,prior.StarterBonus);
            // Append-only compatibility contract: preserve exact manifest,
            // profile ID, existing definitions, rewards, kits and battle bytes.
            // Old earned campaign receipts stay earned, never grant twice.
            return new ContentCatalog(source.Manifest,source.Resources,source.Abilities.Values,
                source.Kits.Values,source.Units.Values,source.Missions.Values.Concat(All.Select(c=>c.Mission)),
                source.Rewards.Values.Concat(All.Select(c=>c.Rewards)),new[]{campaign},source.StarterUnits,source.Statuses.Values);
        }
    }
}
