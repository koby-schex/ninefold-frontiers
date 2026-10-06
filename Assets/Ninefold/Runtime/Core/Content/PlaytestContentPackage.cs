using System;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Persistence;
using Ninefold.Core.Progression;
using Ninefold.Core.Views;

namespace Ninefold.Core.Content
{
    /// <summary>Distinct, bounded engineering arenas. No canonical geography or production missions.</summary>
    public static class PlaytestContentPackage
    {
        public static ContentCatalog Create()
        {
            var source = AbstractContentPackage.Create();
            FieldBox Box(decimal x, decimal z, decimal right, decimal top, decimal height = 5)
                => new FieldBox(new FieldPoint(x,0,z),new FieldPoint(right,height,top));
            BattlefieldMap Map(string id)
            {
                switch (id)
                {
                    case "fixture-opening": // Open center, two offset side blocks.
                        return new BattlefieldMap(Box(-3,-3,8,7),new[] {
                            new FieldObstacle(Box(-2,2,0,4,2),true,true), new FieldObstacle(Box(5,3,7,5,2),true,true) });
                    case "fixture-mixed": // Long combat lane with a central cover island and two approaches.
                        return new BattlefieldMap(Box(-3,-4,10,6),new[] {
                            new FieldObstacle(Box(2,1,4,3,2),true,true) });
                    case "fixture-finale": // Northern barrier leaves west/east routes around a slow center.
                        return new BattlefieldMap(Box(-4,-3,9,9),new[] {
                            new FieldObstacle(Box(1,4,5,5,2),true,true) },new[] { new DifficultGround(Box(1,1,5,3,.1m),2) });
                    default: // Offset side walls create a distinct asymmetric approach.
                        return new BattlefieldMap(Box(-5,-4,8,8),new[] {
                            new FieldObstacle(Box(-3,1,-1,5,2),true,true), new FieldObstacle(Box(5,-2,6,2,2),true,true) });
                }
            }
            var missions = source.Missions.Values.Select(m => new MissionContent(m.Id,m.FactionId,m.IsCampaign,m.MinimumSquad,
                Map(m.Id),m.Deployment,m.Actors,m.Objectives[0],m.Priority,m.Objectives.Skip(1))).ToArray();
            return new ContentCatalog(new ContentManifest("abstract-flow","fixture-layouts-v2","balance-v1",ContentClassification.AbstractFixture),
                source.Resources,source.Abilities.Values,source.Kits.Values,source.Units.Values,missions,source.Rewards.Values,source.Campaigns,source.StarterUnits,source.Statuses.Values);
        }
    }

    /// <summary>Shares existing progression; old unclaimed battles finish against their original catalog.</summary>
    public sealed class PlaytestProfile
    {
        public const string ProfileId = "unity-abstract-v1";
        public ContentCatalog Catalog { get; }
        public MissionPresentation Menu { get; }
        public bool IsLegacy { get; }
        private PlaytestProfile(ContentCatalog catalog, MissionPresentation menu, bool legacy)
        { Catalog = catalog; Menu = menu; IsLegacy = legacy; }
        public static PlaytestProfile Open(IBattleSaveFiles legacy, IBattleSaveFiles current, IProgressSaveFiles progress)
        {
            bool hasCurrent = current.Read(0) != null || current.Read(1) != null;
            bool hasLegacy = legacy.Read(0) != null || legacy.Read(1) != null;
            bool hasProfile = progress.Read(0) != null || progress.Read(1) != null;
            if (!hasProfile && (hasLegacy || hasCurrent)) throw new InvalidOperationException("Battle exists without its profile; recover the profile before playing.");
            if (!hasCurrent && hasLegacy)
            {
                var oldCatalog = AbstractContentPackage.Create();
                var oldMenu = new MissionPresentation(oldCatalog,legacy,progress,ProfileId); oldMenu.Open();
                if (oldMenu.Read().Phase != MissionFlowPhase.Selection) return new PlaytestProfile(oldCatalog,oldMenu,true);
            }
            var catalog = PlaytestContentPackage.Create(); var menu = new MissionPresentation(catalog,current,progress,ProfileId);
            if (hasProfile) menu.Open(); else menu.CreateProfile();
            return new PlaytestProfile(catalog,menu,false);
        }
    }
}
