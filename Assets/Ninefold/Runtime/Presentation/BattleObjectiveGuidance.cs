using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Missions;
using Ninefold.Core.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninefold.Presentation
{
    /// <summary>
    /// Compact, read-only objective wayfinding. This is a tactical schematic,
    /// NOT a route/path solver or a new playable radar mode. It never invokes
    /// movement, targeting, combat, save, prefab or 3D-renderer APIs.
    /// </summary>
    internal static class BattleObjectiveGuidance
    {
        private static Label Label(VisualElement parent, string value, string className)
        {
            var label = new Label(value) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            parent.Add(label);
            return label;
        }

        private static string KindName(ObjectiveKind kind)
        {
            switch (kind)
            {
                case ObjectiveKind.Defeat: return "ELIMINATE";
                case ObjectiveKind.Protect: return "PROTECT";
                case ObjectiveKind.RescueAndExtract: return "RESCUE / EXTRACT";
                case ObjectiveKind.Secure: return "SECURE";
                case ObjectiveKind.Stabilize: return "STABILIZE";
                case ObjectiveKind.Survive: return "SURVIVE";
                default: return "OBJECTIVE";
            }
        }

        public static void Header(VisualElement parent, IReadOnlyList<ObjectiveView> objectives)
        {
            var panel = new VisualElement { pickingMode = PickingMode.Ignore };
            panel.AddToClassList("btg-objective-header");
            parent.Add(panel);
            var primary = objectives.FirstOrDefault();
            if (primary == null)
            {
                Label(panel, "OBJECTIVE", "btg-goal");
                return;
            }

            var headline = new VisualElement { pickingMode = PickingMode.Ignore };
            headline.AddToClassList("btg-goal-line");
            panel.Add(headline);
            Label(headline,
                primary.Kind == ObjectiveKind.RescueAndExtract && primary.Rescued
                    ? "EXTRACT" : KindName(primary.Kind), "btg-goal");
            Label(headline, primary.Status == ObjectiveStatus.Completed ? "DONE" :
                primary.Status == ObjectiveStatus.Failed ? "FAILED" :
                primary.Progress + "/" + primary.Required, "btg-progress");

            var extras = objectives.Skip(1).ToArray();
            if (extras.Length == 0) return;
            int complete = extras.Count(o => o.Status == ObjectiveStatus.Completed);
            Label(panel, "BONUS " + complete + "/" + extras.Length, "btg-optional");
        }

        /// <summary>
        /// Returns a noninteractive inset map. Its HitTest area must be excluded
        /// by the battle surface's pointer handlers so a tap on the inset never
        /// accidentally becomes a movement order in the 3D camera below.
        /// </summary>
        public static VisualElement Radar(VisualElement parent, MissionContent mission, BattleView battle)
        {
            var radar = new VisualElement { pickingMode = PickingMode.Ignore };
            radar.AddToClassList("btg-radar");
            parent.Add(radar);
            Label(radar, "FIELD", "btg-radar-caption");

            var map = new VisualElement { pickingMode = PickingMode.Ignore };
            map.AddToClassList("btg-radar-map");
            radar.Add(map);
            var bounds = battle.Map.Bounds;
            float width = (float)(bounds.Max.X - bounds.Min.X);
            float depth = (float)(bounds.Max.Z - bounds.Min.Z);
            if (width <= 0 || depth <= 0) return radar;

            float Px(decimal x) => Mathf.Clamp((float)(x - bounds.Min.X) / width * 100f, 0, 100);
            float Pz(decimal z) => Mathf.Clamp((float)(bounds.Max.Z - z) / depth * 100f, 0, 100);

            void Area(FieldBox area, string cls)
            {
                var box = new VisualElement { pickingMode = PickingMode.Ignore };
                box.AddToClassList(cls);
                box.style.position = Position.Absolute;
                box.style.left = Length.Percent(Px(area.Min.X));
                box.style.top = Length.Percent(Pz(area.Max.Z));
                box.style.width = Length.Percent(Mathf.Max(0, Px(area.Max.X) - Px(area.Min.X)));
                box.style.height = Length.Percent(Mathf.Max(0, Pz(area.Min.Z) - Pz(area.Max.Z)));
                map.Add(box);
            }
            void Dot(FieldPoint point, string cls)
            {
                var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                dot.AddToClassList("btg-point");
                dot.AddToClassList(cls);
                dot.style.position = Position.Absolute;
                dot.style.left = Length.Percent(Px(point.X));
                dot.style.top = Length.Percent(Pz(point.Z));
                map.Add(dot);
            }
            foreach (var ground in battle.Map.Ground) Area(ground.Bounds, "btg-slow");
            foreach (var obstacle in battle.Map.Obstacles) Area(obstacle.Bounds, "btg-cover");

            var current = battle.Objectives.FirstOrDefault(o => o.Status == ObjectiveStatus.Active);
            var authored = current == null ? null : mission.Objectives.FirstOrDefault(o => o.Id == current.Id);
            bool hasDestination = false;
            if (authored != null)
            {
                // These are *locations*, not a proposed straight route. Never
                // draw a path through cover or suggest illegal movement.
                if (authored.Kind == ObjectiveKind.RescueAndExtract && current.Rescued && authored.Zone != null)
                {
                    Dot(Center(authored.Zone), "btg-waypoint"); hasDestination = true;
                }
                else if (authored.Interaction != null)
                {
                    Dot(authored.Interaction.Point, "btg-waypoint"); hasDestination = true;
                }
                else if (authored.Zone != null)
                {
                    Dot(Center(authored.Zone), "btg-waypoint"); hasDestination = true;
                }
                else if (authored.Kind == ObjectiveKind.Defeat ||
                    authored.Kind == ObjectiveKind.Protect ||
                    authored.Kind == ObjectiveKind.RescueAndExtract)
                {
                    foreach (var id in authored.Subjects)
                    {
                        var unit = battle.Units.FirstOrDefault(u => u.Id == id &&
                            u.IsEligible && u.Position.HasValue && u.Health?.IsDefeated != true);
                        if (unit?.Position == null) continue;
                        Dot(unit.Position.Value, "btg-waypoint"); hasDestination = true;
                    }
                }
            }
            foreach (var unit in battle.Units.Where(u => u.IsEligible && u.Position.HasValue
                && u.Health?.IsDefeated != true))
            {
                Dot(unit.Position.Value, unit.IsEnemyControlled ? "btg-enemy" :
                    unit.IsPlayerSquad ? "btg-ally" : "btg-neutral");
                if (unit.Id == battle.Activation?.UnitId)
                    Dot(unit.Position.Value, "btg-active");
            }

            string footer = current == null
                ? battle.Objectives.Any(o => o.Status == ObjectiveStatus.Failed) ? "FAILED" : "COMPLETE"
                : hasDestination ? "GOLD = GOAL"
                : current.Kind == ObjectiveKind.Survive ? "HOLD POSITION"
                : current.Kind == ObjectiveKind.Protect ? "GUARD TARGET"
                : "FOLLOW GOAL";
            Label(radar, footer, "btg-radar-foot");
            return radar;
        }

        private static FieldPoint Center(FieldBox box)
            => new FieldPoint((box.Min.X + box.Max.X) / 2m,
                (box.Min.Y + box.Max.Y) / 2m, (box.Min.Z + box.Max.Z) / 2m);
    }
}
