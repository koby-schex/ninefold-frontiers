using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Campaigns;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Progression;

namespace Ninefold.Core.Content
{
    /// <summary>Engineering fixture only. Names, bodies, numbers and short campaigns are NOT production content.</summary>
    public static class AbstractContentPackage
    {
        public static ContentCatalog Create()
        {
            var abilities = new[] {
                AbilityContent.PassivePlaceholder("fixture-passive"),
                new AbilityContent("fixture-normal", new FieldAbility(AbilitySlot.NormalAttack, HealthEffectKind.Damage, 40, 12, true)),
                new AbilityContent("fixture-main", new FieldAbility(AbilitySlot.Main, HealthEffectKind.Healing, 30, 12, false)),
                new AbilityContent("fixture-signature", new FieldAbility(AbilitySlot.Signature, HealthEffectKind.Damage, 60, 12, true)) };
            var kit = new KitContent("fixture-kit", abilities[0].Id, abilities[1].Id, abilities[2].Id, abilities[3].Id,
                new UnitAbilityDefinition(2, SignatureReadiness.AfterNormalAttack));
            var body = new FieldBody(.25m, .25m, 2, new FieldPoint(0, 1, 0), new FieldPoint(0, 1, 0));
            var units = new List<UnitContent>();
            foreach (string faction in new[] { "fixture-a", "fixture-b" })
                for (int i = 1; i <= 5; i++)
                {
                    string id = faction + "-" + i, fragment = "fragment-" + id;
                    var roster = new RosterUnit(id, faction, i == 5, new UnitUnlockDefinition(id, fragment, i == 5 ? 50 : 5, "fixture-v1"),
                        new UnitAdvancementDefinition(id, fragment, "fixture-v1", new AdvancementBonus(1000, 1000, 1000),
                            new[] { new AdvancementStep(5, new AdvancementBonus(500, 500, 500)) }),
                        new UnitCustomizationDefinition(id, "fixture-v1", new[] { new CustomizationOption("fixture-health", new CustomizationBonus(500, -500, 0)) }));
                    units.Add(new UnitContent(id, kit.Id, i == 5 ? 110 : 100, 20, 30 - i, 5, body, roster));
                }
            units.Add(new UnitContent("fixture-npc", kit.Id, 80, 0, 10, 4, body));
            var map = new BattlefieldMap(new FieldBox(new FieldPoint(-10, 0, -10), new FieldPoint(20, 5, 20)), Array.Empty<FieldObstacle>());
            var deployment = new[] { new FieldPoint(0, 0, 0), new FieldPoint(2, 0, 0), new FieldPoint(4, 0, 0) };
            MissionContent Stabilize(string id, string faction) => new MissionContent(id, faction, true, 1, map, deployment,
                Array.Empty<ActorSpawn>(), ObjectiveDefinition.Stabilize("primary", 1, new InteractionDefinition(new FieldPoint(0, 0, 0), 6, new[] { "$squad" })));
            var missions = new[] {
                Stabilize("fixture-opening", "fixture-a"), Stabilize("fixture-finale", "fixture-a"), Stabilize("fixture-next", "fixture-b"),
                new MissionContent("fixture-mixed", null, false, 1, map, deployment,
                    new[] { new ActorSpawn("fixture-enemy-1", "fixture-npc", new FieldPoint(6, 0, 0), true, new EnemyBehavior(EnemyStyle.Aggressive, 3)) },
                    ObjectiveDefinition.Defeat("primary", new[] { "fixture-enemy-1" })) };
            var rewards = missions.Select(m => new MissionRewardPolicy(m.Id, "fixture-v1",
                new[] { new ResourceGrant("fixture-resource", 10), new ResourceGrant("fragment-fixture-a-1", 5), new ResourceGrant("fragment-fixture-a-4", 5) },
                new[] { new ResourceGrant("fixture-resource", 2) })).ToArray();
            var campaigns = new[] {
                new CampaignDefinition("fixture-starter", "fixture-a", "fixture-v1", new[] { new CampaignMission("fixture-opening"), new CampaignMission("fixture-finale", new[] { "fixture-opening" }) },
                    new[] { new ResourceGrant("fixture-resource", 5) }, new StarterCampaignBonus("fixture-a-5", new[] { new NextFactionBundle("fixture-followup", new[] { "fixture-b-1", "fixture-b-2", "fixture-b-3" }) })),
                new CampaignDefinition("fixture-followup", "fixture-b", "fixture-v1", new[] { new CampaignMission("fixture-next") }, Array.Empty<ResourceGrant>()) };
            return new ContentCatalog(new ContentManifest("abstract-flow", "fixture-v1", "balance-v1", ContentClassification.AbstractFixture),
                units.Where(u => u.Roster != null).Select(u => u.Roster.Unlock.FragmentResourceId).Concat(new[] { "fixture-resource" }),
                abilities, new[] { kit }, units, missions, rewards, campaigns, new[] { "fixture-a-1", "fixture-a-2", "fixture-a-3" });
        }
    }
}
