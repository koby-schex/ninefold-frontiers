using System;
using System.Collections.Generic;
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
    /// Opt-in, noncanonical 10-mission CONTENT CANDIDATE. The Unity Home
    /// prototype entry selects this catalog under an independent V4 profile.
    /// Older V1/V2/V3 progress remains intact and is not migrated.
    /// </summary>
    public static class WorldFiveOpeningTenPrototype
    {
        public const string ProfileId = "w5-opening-ten-profile-v4";
        public const string Revision = "w5-opening-ten-layout-04";
        public const string CampaignId = WorldFivePlayableSlice.CampaignId;
        public const string Status = "PROTOTYPE CONTENT - NOT CANON-LOCKED OR UI-RELEASED";

        public static IReadOnlyList<string> MissionIds { get; } = Array.AsReadOnly(
            Enumerable.Range(1,10).Select(n => "w5-draft-" +
                n.ToString("D3",System.Globalization.CultureInfo.InvariantCulture)).ToArray());

        private static FieldPoint P(decimal x,decimal z) => new FieldPoint(x,0,z);
        private static FieldBox B(decimal x0,decimal z0,decimal x1,decimal z1,decimal h=5)
            => new FieldBox(P(x0,z0),new FieldPoint(x1,h,z1));
        private static FieldObstacle Cover(decimal x0,decimal z0,decimal x1,decimal z1)
            => new FieldObstacle(B(x0,z0,x1,z1,2),true,true);

        public static ContentCatalog Create()
        {
            WorldFiveOpeningCampaignDraft.Validate();
            // Preserve the V3 first six authored encounters byte-for-byte as
            // definitions, including existing rescue-carrier behavior.
            var baseCatalog=WorldFivePlayableSlice.Create();

            var seven=new MissionContent(MissionIds[6],"fixture-a",true,2,
                new BattlefieldMap(B(-7,-5,9,8),new[] {
                    Cover(-5,0,-3,3),Cover(-1,1,1,3),Cover(4,-1,6,1)
                },new[] {
                    new DifficultGround(B(2,3,4,6,.1m),2m)
                }),
                new[] { P(-2,-3),P(-1,-3),P(0,-3) },
                new[] {
                    new ActorSpawn("w5-clear-proxy-a","fixture-npc",P(6,5),true,
                        new EnemyBehavior(EnemyStyle.Aggressive,3)),
                    new ActorSpawn("w5-clear-proxy-b","fixture-npc",P(7,-2),true,
                        new EnemyBehavior(EnemyStyle.Aggressive,3))
                },
                ObjectiveDefinition.Defeat("primary",new[] {
                    "w5-clear-proxy-a","w5-clear-proxy-b"
                }),
                environmentKey:"study-w5-fracture");

            var eight=new MissionContent(MissionIds[7],"fixture-a",true,2,
                new BattlefieldMap(B(-8,-6,11,9),new[] {
                    Cover(-4,-1,-2,4),Cover(0,-3,2,1),
                    Cover(4,3,6,5),Cover(8,-1,9,2)
                },new[] {
                    new DifficultGround(B(3,-4,5,-1,.1m),2m),
                    new DifficultGround(B(-1,5,2,7,.1m),2m)
                }),
                new[] { P(-4,-4),P(-3,-4),P(-2,-4) },
                new[] {
                    new ActorSpawn("w5-divide-proxy","fixture-npc",P(8,6),true,
                        new EnemyBehavior(EnemyStyle.Aggressive,3))
                },
                ObjectiveDefinition.Secure("primary",B(4,1,7,4,3),
                    new[] { "$squad" },new[] { "w5-divide-proxy" },2,false),
                environmentKey:"study-w5-survey");

            var nine=new MissionContent(MissionIds[8],"fixture-a",true,2,
                new BattlefieldMap(B(-7,-5,10,9),new[] {
                    Cover(-5,0,-3,2),Cover(1,0,3,2),Cover(5,-1,6,1)
                },new[] {
                    new DifficultGround(B(2,3,4,5,.1m),2m)
                }),
                new[] { P(-2,-3),P(-1,-3),P(0,-3) },
                new[] {
                    new ActorSpawn("w5-return-proxy","fixture-npc",P(6,5),false),
                    new ActorSpawn("w5-return-threat","fixture-npc",P(8,-2),true,
                        new EnemyBehavior(EnemyStyle.Aggressive,3))
                },
                ObjectiveDefinition.RescueCarried("primary","w5-return-proxy",
                    B(-5,-4,-1,-2,3),
                    new InteractionDefinition(P(6,5),1.4m,new[] { "$leader" }),
                    "$leader"),
                environmentKey:"study-w5-fracture");

            // Tutorial act finale combines a clear, visible primary marker
            // with up to two bonus tasks; its mystery remains unresolved.
            var ten=new MissionContent(MissionIds[9],"fixture-a",true,2,
                new BattlefieldMap(B(-8,-6,11,9),new[] {
                    Cover(-5,1,-3,3),Cover(-1,-1,1,2),
                    Cover(3,3,5,5),Cover(7,-2,8,0)
                },new[] {
                    new DifficultGround(B(2,-3,4,-1,.1m),2m),
                    new DifficultGround(B(6,5,8,7,.1m),2m)
                }),
                new[] { P(-3,-4),P(-2,-4),P(-1,-4) },
                new[] {
                    new ActorSpawn("w5-line-threat","fixture-npc",P(8,5),true,
                        new EnemyBehavior(EnemyStyle.Aggressive,3))
                },
                ObjectiveDefinition.Stabilize("primary",2,
                    new InteractionDefinition(P(6,3),1.5m,new[] { "$squad" })),
                optional:new[] {
                    ObjectiveDefinition.Defeat("optional-threat",
                        new[] { "w5-line-threat" }),
                    ObjectiveDefinition.Survive("optional-endurance",2)
                },
                environmentKey:"study-w5-survey");

            var additions=new[] { seven,eight,nine,ten };
            var original=baseCatalog.Campaigns.Single();
            var chain=MissionIds.Select((id,index) => new CampaignMission(id,
                index==0 ? Array.Empty<string>() : new[] { MissionIds[index-1] })).ToArray();
            var campaign=new CampaignDefinition(CampaignId,original.FactionId,
                "w5-opening-ten-04",chain,
                new[] { new ResourceGrant("fixture-resource",40) });
            var rewards=additions.Select((m,index) => new MissionRewardPolicy(m.Id,
                "w5-slice-rewards-01",
                new[] {
                    new ResourceGrant("fixture-resource",(index+7)*10),
                    new ResourceGrant("fragment-fixture-a-1",5)
                },
                new[] { new ResourceGrant("fixture-resource",2) }));
            return new ContentCatalog(
                new ContentManifest("w5-prototype",Revision,"balance-v1",
                    ContentClassification.AbstractFixture),
                baseCatalog.Resources,baseCatalog.Abilities.Values,
                baseCatalog.Kits.Values,baseCatalog.Units.Values,
                baseCatalog.Missions.Values.Concat(additions),
                baseCatalog.Rewards.Values.Concat(rewards),
                new[] { campaign },baseCatalog.StarterUnits,
                baseCatalog.Statuses.Values);
        }

        // Caller must provide a fresh V4-specific save location; never reuse
        // original AbstractPlaytest-v1 or WorldFiveSlice-v1/v2/v3 folders.
        public static MissionPresentation Open(IBattleSaveFiles battles,IProgressSaveFiles progress)
            => Open(Create(),battles,progress);

        public static MissionPresentation Open(ContentCatalog catalog,
            IBattleSaveFiles battles,IProgressSaveFiles progress)
        {
            if (catalog == null || catalog.Manifest.PackageId != "w5-prototype" ||
                catalog.Manifest.ContentRevision != Revision)
                throw new ArgumentException("Expected the isolated World Five 10-mission V4 catalog.",
                    nameof(catalog));
            if (battles == null || progress == null)
                throw new ArgumentNullException("Independent V4 battle/profile files are required.");
            var menu=new MissionPresentation(catalog,battles,progress,ProfileId);
            if (progress.Read(0) != null || progress.Read(1) != null) menu.Open();
            else menu.CreateProfile();
            return menu;
        }
    }
}
