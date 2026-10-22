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
    /// Opt-in, independently saved prototype for six World Five opening
    /// candidate campaign beats. Uses ABSTRACT fixture units and abilities
    /// only; this package is neither public canon nor a release catalog.
    /// </summary>
    public static class WorldFivePlayableSlice
    {
        public const string CampaignId = "w5-slice";
        // V3 is separate from the V1 and V2 World Five prototype profiles.
        // Both earlier checkpoints remain on disk unchanged for recovery.
        public const string ProfileId = "w5-slice-profile-v3";
        public const string Revision = "w5-slice-layout-03";
        public static readonly string[] MissionIds = {
            "w5-draft-001", "w5-draft-002", "w5-draft-003",
            "w5-draft-004", "w5-draft-005", "w5-draft-006"
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

            // Two-stage recovery: the first deployed squad member performs
            // the pickup, then returns to a separate extraction zone. This
            // uses actor-carried recovery rather than independently commanded
            // NPC escort AI; it remains an abstract World Five prototype.
            var rescue=new MissionContent(MissionIds[2],"fixture-a",true,1,
                new BattlefieldMap(B(-6,-5,10,8),new[] {
                    Cover(-4,-1,-2,2),Cover(1,0,3,2),Cover(7,5,8,6)
                },new[] { new DifficultGround(B(3,-2,5,0,.1m),2m) }),
                new[] { P(-2,-3),P(-1,-3),P(0,-3) },
                new[] { new ActorSpawn("w5-rescue-proxy","fixture-npc",P(6,3),false) },
                ObjectiveDefinition.RescueCarried("primary","w5-rescue-proxy",
                    B(-5,-4,-1,-2,3),
                    new InteractionDefinition(P(6,3),1.65m,new[] { "$leader" }),
                    "$leader"),
                environmentKey:"study-w5-survey");

            // Second playable arc: real different battle objectives instead of
            // repeating a route interaction on an altered backdrop.
            // Each opponent is an abstract fixture NPC, not a new canonical
            // World Five enemy species or faction.
            var secure=new MissionContent(MissionIds[3],"fixture-a",true,2,
                new BattlefieldMap(B(-7,-5,9,8),new[] {
                    Cover(-5,0,-3,2),Cover(-1,1,1,3),Cover(6,-1,7,2)
                },new[] { new DifficultGround(B(1,-2,3,0,.1m),2m) }),
                new[] { P(-3,-3),P(-2,-3),P(-1,-3) },
                new[] {
                    new ActorSpawn("w5-zone-test-threat","fixture-npc",P(7,5),true,
                        new EnemyBehavior(EnemyStyle.Aggressive,3))
                },
                ObjectiveDefinition.Secure("primary",B(2,2,5,6,3),
                    new[] { "$squad" },new[] { "w5-zone-test-threat" },2,true),
                environmentKey:"study-w5-survey");

            var protect=new MissionContent(MissionIds[4],"fixture-a",true,2,
                new BattlefieldMap(B(-7,-5,8,8),new[] {
                    Cover(-4,0,-2,2),Cover(2,0,4,2),Cover(4,5,5,6)
                }),
                new[] { P(-2,-3),P(-1,-3),P(0,-3) },
                new[] {
                    new ActorSpawn("w5-shelter-test-party","fixture-npc",P(0,3),false),
                    new ActorSpawn("w5-shelter-test-threat","fixture-npc",P(6,4),true,
                        new EnemyBehavior(EnemyStyle.Aggressive,3))
                },
                ObjectiveDefinition.Protect("primary","w5-shelter-test-party",2),
                environmentKey:"study-w5-fracture");

            var survive=new MissionContent(MissionIds[5],"fixture-a",true,2,
                new BattlefieldMap(B(-6,-5,10,8),new[] {
                    Cover(-4,1,-2,3),Cover(1,-1,2,2),Cover(5,3,7,4)
                },new[] { new DifficultGround(B(2,4,5,6,.1m),2m) }),
                new[] { P(-2,-3),P(-1,-3),P(0,-3) },
                new[] {
                    new ActorSpawn("w5-window-test-threat-a","fixture-npc",P(7,-2),true,
                        new EnemyBehavior(EnemyStyle.Aggressive,3)),
                    new ActorSpawn("w5-window-test-threat-b","fixture-npc",P(7,6),true,
                        new EnemyBehavior(EnemyStyle.Aggressive,3))
                },
                ObjectiveDefinition.Survive("primary",3),
                environmentKey:"study-w5-survey");

            var missions=new[] { one,two,rescue,secure,protect,survive,fixture.Missions["fixture-mixed"] };
            var rewardPolicies=MissionIds.Select((id,index)=>new MissionRewardPolicy(id,"w5-slice-rewards-01",
                new[] { new ResourceGrant("fixture-resource",(index+1)*10),
                    new ResourceGrant("fragment-fixture-a-1",5) },
                new[] { new ResourceGrant("fixture-resource",2) }))
                .Concat(new[] { fixture.Rewards["fixture-mixed"] });
            var campaign=new CampaignDefinition(CampaignId,"fixture-a","w5-slice-02",
                new[] { new CampaignMission(MissionIds[0]),
                    new CampaignMission(MissionIds[1],new[] { MissionIds[0] }),
                    new CampaignMission(MissionIds[2],new[] { MissionIds[1] }),
                    new CampaignMission(MissionIds[3],new[] { MissionIds[2] }),
                    new CampaignMission(MissionIds[4],new[] { MissionIds[3] }),
                    new CampaignMission(MissionIds[5],new[] { MissionIds[4] }) },
                new[] { new ResourceGrant("fixture-resource",25) });
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
            => Open(Create(),battles,progress);

        public static MissionPresentation Open(ContentCatalog catalog,IBattleSaveFiles battles,IProgressSaveFiles progress)
        {
            if (catalog == null || catalog.Manifest.PackageId != "w5-prototype" ||
                catalog.Manifest.ContentRevision != Revision)
                throw new ArgumentException("Expected the World Five slice catalog.",nameof(catalog));
            if (battles == null || progress == null)
                throw new ArgumentNullException("Independent battle/profile files are required.");
            var menu=new MissionPresentation(catalog,battles,progress,ProfileId);
            if (progress.Read(0) != null || progress.Read(1) != null) menu.Open();
            else menu.CreateProfile();
            return menu;
        }
    }
}
