using System;
using System.Linq;
using Ninefold.Core.Campaigns;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;
using Ninefold.Core.Persistence;
using Ninefold.Core.Progression;
using Ninefold.Core.Views;

namespace Ninefold.Core.Content
{
    /// <summary>
    /// Opt-in, independently saved prototype for the first three World Five
    /// candidate campaign beats. Uses ABSTRACT fixture units and abilities
    /// only; this package is neither public canon nor a release catalog.
    /// </summary>
    public static class WorldFivePlayableSlice
    {
        public const string CampaignId = "w5-slice";
        public const string ProfileId = "w5-slice-profile-v1";
        public const string Revision = "w5-slice-layout-01";
        public static readonly string[] MissionIds = {
            "w5-draft-001", "w5-draft-002", "w5-draft-003"
        };

        private static FieldPoint P(decimal x, decimal z) => new FieldPoint(x,0,z);
        private static FieldBox B(decimal x0,decimal z0,decimal x1,decimal z1,decimal height=5)
            => new FieldBox(P(x0,z0),new FieldPoint(x1,height,z1));
        private static FieldObstacle Cover(decimal x0,decimal z0,decimal x1,decimal z1)
            => new FieldObstacle(B(x0,z0,x1,z1,2),true,true);

        public static ContentCatalog Create()
        {
            WorldFiveOpeningCampaignDraft.Validate();
            // Keep the trusted fixture roster and four ability slots until
            // canonical unit identities and rendered assets are approved.
            var fixture=PlaytestContentPackage.Create();
            var one=new MissionContent(MissionIds[0],"fixture-a",true,1,
                new BattlefieldMap(B(-4,-5,7,6),new[] {
                    Cover(-3,0,-1,2),Cover(5,0,6,3)
                }),
                new[] { P(-1,-3),P(0,-3),P(1,-3) },
                Array.Empty<ActorSpawn>(),
                ObjectiveDefinition.Stabilize("primary",1,
                    new InteractionDefinition(P(3,3),1.4m,new[] { "$squad" })),
                environmentKey:"study-w5-survey");

            var two=new MissionContent(MissionIds[1],"fixture-a",true,1,
                new BattlefieldMap(B(-6,-5,9,8),new[] {
                    Cover(-3,1,-1,4),Cover(3,2,5,4)
                },new[] { new DifficultGround(B(0,1,2,4,.1m),2m) }),
                new[] { P(-2,-3),P(-1,-3),P(0,-3) },
                Array.Empty<ActorSpawn>(),
                ObjectiveDefinition.Stabilize("primary",1,
                    new InteractionDefinition(P(6,5),1.4m,new[] { "$squad" })),
                environmentKey:"study-w5-fracture");

            // The current core has no escorted movement commands for a
            // friendly NPC. Put this temporary rescue subject *inside* its
            // marked extraction zone, so reaching and activating the rescue
            // succeeds without requiring an uncommandable actor to walk.
            // A two-stage escort mission is a future explicit gameplay task.
            var rescue=new MissionContent(MissionIds[2],"fixture-a",true,1,
                new BattlefieldMap(B(-6,-5,10,8),new[] {
                    Cover(-4,-1,-2,2),Cover(1,0,3,2),Cover(7,5,8,6)
                },new[] { new DifficultGround(B(3,-2,5,0,.1m),2m) }),
                new[] { P(-2,-3),P(-1,-3),P(0,-3) },
                new[] { new ActorSpawn("w5-rescue-proxy","fixture-npc",P(6,3),false) },
                ObjectiveDefinition.Rescue("primary","w5-rescue-proxy",
                    B(5,2,8,5,3),
                    new InteractionDefinition(P(6,3),1.4m,new[] { "$squad" })),
                environmentKey:"study-w5-survey");

            var missions=new[] { one,two,rescue,fixture.Missions["fixture-mixed"] };
            var rewardPolicies=MissionIds.Select((id,index)=>new MissionRewardPolicy(id,"w5-slice-rewards-01",
                new[] { new ResourceGrant("fixture-resource",(index+1)*10),
                    new ResourceGrant("fragment-fixture-a-1",5) },
                new[] { new ResourceGrant("fixture-resource",2) }))
                .Concat(new[] { fixture.Rewards["fixture-mixed"] });
            var campaign=new CampaignDefinition(CampaignId,"fixture-a","w5-slice-01",
                new[] { new CampaignMission(MissionIds[0]),
                    new CampaignMission(MissionIds[1],new[] { MissionIds[0] }),
                    new CampaignMission(MissionIds[2],new[] { MissionIds[1] }) },
                new[] { new ResourceGrant("fixture-resource",15) });
            return new ContentCatalog(new ContentManifest("w5-prototype",Revision,"balance-v1",
                    ContentClassification.AbstractFixture),
                fixture.Resources,fixture.Abilities.Values,fixture.Kits.Values,
                fixture.Units.Values,missions,rewardPolicies,new[] { campaign },
                fixture.StarterUnits,fixture.Statuses.Values);
        }

        /// <summary>
        /// This MUST receive storage independent of AbstractPlaytest-v1.
        /// Never silently import the user's previous checkpoint or wallet.
        /// </summary>
        public static MissionPresentation Open(IBattleSaveFiles battles,IProgressSaveFiles progress)
        {
            if (battles == null || progress == null)
                throw new ArgumentNullException("Independent battle/profile files are required.");
            var menu=new MissionPresentation(Create(),battles,progress,ProfileId);
            if (progress.Read(0) != null || progress.Read(1) != null) menu.Open();
            else menu.CreateProfile();
            return menu;
        }
    }
}
