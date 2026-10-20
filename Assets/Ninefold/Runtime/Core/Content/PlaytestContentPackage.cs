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
        // V2 is retained solely to recover checkpoints saved before the varied
        // battlefields shipped. Never mutate an in-progress fight's geometry.
        public static ContentCatalog CreateLayoutV2() => Build(false);
        public static ContentCatalog Create() => Build(true);

        private static ContentCatalog Build(bool expandedLayouts)
        {
            var source = AbstractContentPackage.Create();
            FieldBox Box(decimal x, decimal z, decimal right, decimal top, decimal height = 5)
                => new FieldBox(new FieldPoint(x,0,z),new FieldPoint(right,height,top));
            BattlefieldMap Map(string id)
            {
                if (!expandedLayouts)
                {
                    switch (id)
                    {
                        case "fixture-opening":
                            return new BattlefieldMap(Box(-3,-3,8,7),new[] {
                                new FieldObstacle(Box(-2,2,0,4,2),true,true), new FieldObstacle(Box(5,3,7,5,2),true,true) });
                        case "fixture-mixed":
                            return new BattlefieldMap(Box(-3,-4,10,6),new[] {
                                new FieldObstacle(Box(2,1,4,3,2),true,true) });
                        case "fixture-finale":
                            return new BattlefieldMap(Box(-4,-3,9,9),new[] {
                                new FieldObstacle(Box(1,4,5,5,2),true,true) },new[] { new DifficultGround(Box(1,1,5,3,.1m),2) });
                        default:
                            return new BattlefieldMap(Box(-5,-4,8,8),new[] {
                                new FieldObstacle(Box(-3,1,-1,5,2),true,true), new FieldObstacle(Box(5,-2,6,2,2),true,true) });
                    }
                }
                // Compact tutorial approach, broad side-route encounter and
                // extended finale. These footprints are INTERNAL level-authoring
                // decisions, never player-selectable sizes or UI classifications.
                switch (id)
                {
                    case "fixture-opening":
                        return new BattlefieldMap(Box(-4,-5,7,6),new[] {
                            new FieldObstacle(Box(-3,0,-1,2,2),true,true),
                            new FieldObstacle(Box(5,0,6,3,2),true,true) });
                    case "fixture-finale":
                        return new BattlefieldMap(Box(-6,-5,11,9),new[] {
                            new FieldObstacle(Box(-3,1,0,3,2),true,true),
                            new FieldObstacle(Box(3,4,6,5,2),true,true),
                            new FieldObstacle(Box(8,0,9,4,2),true,true)
                        },new[] { new DifficultGround(Box(1,-1,4,1,.1m),2) });
                    case "fixture-mixed":
                        return new BattlefieldMap(Box(-4,-4,10,7),new[] {
                            new FieldObstacle(Box(1,2,3,4,2),true,true),
                            new FieldObstacle(Box(6,-1,7,1,2),true,true) });
                    default:
                        return new BattlefieldMap(Box(-5,-5,9,8),new[] {
                            new FieldObstacle(Box(-3,1,-1,4,2),true,true),
                            new FieldObstacle(Box(5,0,7,2,2),true,true),
                            new FieldObstacle(Box(1,5,3,6,2),true,true) },
                            new[] { new DifficultGround(Box(-1,-1,1,1,.1m),2) });
                }
            }

            // V3 places each stabilization objective away from the entry.
            // This makes the objective marker and route legible, rather than
            // spawning units on top of the completion interaction.
            FieldPoint[] Deployment(string id, MissionContent original)
            {
                if (!expandedLayouts) return original.Deployment.ToArray();
                switch (id)
                {
                    case "fixture-opening": return new[] { new FieldPoint(-1,0,-3), new FieldPoint(0,0,-3), new FieldPoint(1,0,-3) };
                    case "fixture-finale": return new[] { new FieldPoint(-2,0,-3), new FieldPoint(-1,0,-3), new FieldPoint(0,0,-3) };
                    case "fixture-next": return new[] { new FieldPoint(-2,0,-3), new FieldPoint(-1,0,-3), new FieldPoint(0,0,-3) };
                    case "fixture-mixed": return new[] { new FieldPoint(-2,0,-2), new FieldPoint(-1,0,-2), new FieldPoint(0,0,-2) };
                    default: return original.Deployment.ToArray();
                }
            }
            ObjectiveDefinition Primary(string id, MissionContent original)
            {
                if (!expandedLayouts || original.Objectives[0].Kind != Ninefold.Core.Missions.ObjectiveKind.Stabilize)
                    return original.Objectives[0];
                var target = id == "fixture-opening" ? new FieldPoint(3,0,2)
                    : id == "fixture-finale" ? new FieldPoint(5,0,6) : new FieldPoint(4,0,4);
                return Ninefold.Core.Missions.ObjectiveDefinition.Stabilize("primary", 1,
                    new Ninefold.Core.Missions.InteractionDefinition(target, 1.6m, new[] { "$squad" }));
            }

            var missions = source.Missions.Values.Select(m => new MissionContent(m.Id,m.FactionId,m.IsCampaign,m.MinimumSquad,
                Map(m.Id),Deployment(m.Id,m),m.Actors,Primary(m.Id,m),m.Priority,m.Objectives.Skip(1))).ToArray();
            // A new battle layout needs a new save revision. Old V2 attempts
            // continue against the frozen V2 catalog below until claimed.
            return new ContentCatalog(new ContentManifest("abstract-flow",expandedLayouts ? "fixture-layouts-v3" : "fixture-layouts-v2",
                    "balance-v1",ContentClassification.AbstractFixture),
                source.Resources,source.Abilities.Values,source.Kits.Values,source.Units.Values,missions,
                source.Rewards.Values,source.Campaigns,source.StarterUnits,source.Statuses.Values);
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
