using System;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Persistence;
using Ninefold.Core.Progression;
using Ninefold.Core.Views;

namespace Ninefold.Core.Content
{
    /// <summary>
    /// Opt-in noncanonical support-role study. Never replace existing V4
    /// content or load V4 checkpoint data with this revised ability catalog.
    /// </summary>
    public static class UnitCombatIdentityPrototype
    {
        public const string ProfileId = "unit-identity-support-v1";
        public const string Revision = "unit-identity-support-v1";
        public const string SupportUnitId = "fixture-a-2";

        public static ContentCatalog Create()
        {
            var source = WorldFiveOpeningTenPrototype.Create();
            var abilities = new[] {
                AbilityContent.PassivePlaceholder("identity-support-passive"),
                new AbilityContent("identity-support-normal",
                    new FieldAbility(AbilitySlot.NormalAttack,HealthEffectKind.Damage,25,9,true)),
                new AbilityContent("identity-support-main",
                    new FieldAbility(AbilitySlot.Main,HealthEffectKind.Healing,45,7,false)),
                new AbilityContent("identity-support-signature",
                    new FieldAbility(AbilitySlot.Signature,HealthEffectKind.Damage,35,9,true))
            };
            var kit = new KitContent("identity-support-kit",
                abilities[0].Id,abilities[1].Id,abilities[2].Id,abilities[3].Id,
                new UnitAbilityDefinition(2,SignatureReadiness.AfterNormalAttack));
            var original = source.Units[SupportUnitId];
            var replacement = new UnitContent(original.Id,kit.Id,
                original.Health,original.Armor,original.Initiative,
                original.Movement,original.Body,original.Roster);
            return new ContentCatalog(
                new ContentManifest("w5-identity-study",Revision,"support-balance-v1",
                    ContentClassification.AbstractFixture),
                source.Resources,source.Abilities.Values.Concat(abilities),
                source.Kits.Values.Concat(new[]{kit}),
                source.Units.Values.Where(u=>u.Id!=SupportUnitId).Concat(new[]{replacement}),
                source.Missions.Values,source.Rewards.Values,source.Campaigns,
                source.StarterUnits,source.Statuses.Values);
        }

        public static MissionPresentation Open(IBattleSaveFiles battles,IProgressSaveFiles progress)
        {
            if(battles==null || progress==null)
                throw new ArgumentNullException("Identity study requires isolated save stores.");
            var menu=new MissionPresentation(Create(),battles,progress,ProfileId);
            if(progress.Read(0)!=null || progress.Read(1)!=null) menu.Open();
            else menu.CreateProfile();
            return menu;
        }
    }
}
