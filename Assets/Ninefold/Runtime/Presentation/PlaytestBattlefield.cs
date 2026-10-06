using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Views;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Ninefold.Presentation
{
    /// <summary>Disposable fixture renderer. It never owns simulation state or changes health.</summary>
    internal sealed class PlaytestBattlefield : IDisposable
    {
        private sealed class Token
        {
            public GameObject Body;
            public VisualElement Bar, Fill;
            public Label Caption;
            public Vector3 Position;
            public float Height;
        }
        private readonly GameObject world;
        private readonly Camera camera;
        private readonly Dictionary<string, Token> tokens = new Dictionary<string, Token>();
        private readonly List<Material> materials = new List<Material>();
        private readonly Material friendly, enemy, active, floor, grid, gold;
        private readonly LineRenderer path;
        private readonly GameObject destination, selection;
        private VisualElement surface;
        private Bounds framing;
        private string framedAttempt;
        private readonly List<Rect> barRects = new List<Rect>();

        public PlaytestBattlefield(Transform parent, Camera camera, Material template)
        {
            this.camera = camera;
            world = new GameObject("Abstract battlefield — not canon"); world.transform.SetParent(parent, false);
            Material Make(Color color)
            {
                var m = new Material(template) { color = color }; materials.Add(m); return m;
            }
            friendly = Make(new Color(.14f, .72f, .8f)); enemy = Make(new Color(.94f, .34f, .27f));
            active = Make(new Color(.94f, .85f, .52f)); floor = Make(new Color(.075f, .12f, .17f));
            grid = Make(new Color(.16f, .23f, .29f)); gold = Make(new Color(.9f, .65f, .25f));
            Primitive("Test floor", PrimitiveType.Cube, new Vector3(5, -.16f, 5), new Vector3(30, .3f, 30), floor);
            for (int i = -10; i <= 20; i++)
            {
                Primitive("Grid X", PrimitiveType.Cube, new Vector3(i, .002f, 5), new Vector3(.018f, .01f, 30), grid);
                Primitive("Grid Z", PrimitiveType.Cube, new Vector3(5, .002f, i), new Vector3(30, .01f, .018f), grid);
            }
            destination = Primitive("Destination preview", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.7f, .025f, .7f), gold);
            selection = Primitive("Active unit marker", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.95f, .015f, .95f), active);
            var lineObject = new GameObject("Movement preview"); lineObject.transform.SetParent(world.transform, false);
            path = lineObject.AddComponent<LineRenderer>(); path.sharedMaterial = gold;
            path.startWidth = path.endWidth = .045f; path.useWorldSpace = true;
            path.positionCount = 0;
            camera.orthographic = true; camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.04f, .07f, .1f);
            Hide();
        }

        private GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name; g.transform.SetParent(world.transform, false);
            g.transform.position = position; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = material;
            // Picking is projected into panel coordinates; no physics world is needed for fixture controls.
            Object.Destroy(g.GetComponent<Collider>()); return g;
        }

        public void Show(BattleView view, BattleIntent intent, VisualElement target)
        {
            surface = target; world.SetActive(true); camera.enabled = true;
            Reconcile(view);
            foreach (var token in tokens.Values) surface.Add(token.Bar);
            var actor = view.Units.FirstOrDefault(u => u.Id == view.Activation?.UnitId && u.Position.HasValue);
            selection.SetActive(actor != null);
            if (actor != null) selection.transform.position = Vector(actor.Position.Value) + Vector3.up * .04f;
            path.positionCount = 0; destination.SetActive(false);
            if (intent?.Kind == IntentKind.Movement && actor != null)
            {
                var points = new[] { actor.Position.Value }.Concat(intent.Movement.Path).Select(p => Vector(p) + Vector3.up * .08f).ToArray();
                path.positionCount = points.Length; path.SetPositions(points);
                destination.SetActive(true); destination.transform.position = points.Last();
            }
            // Fit living units and a movement preview with a margin, rather than shrinking tokens to fit the entire empty fixture map.
            // Freeze framing while a preview is armed, so a second tap lands on the same world point.
            if (intent == null || framedAttempt != view.AttemptId)
            {
                framing = new Bounds(new Vector3(3, 0, 0), new Vector3(8, 2, 5));
                foreach (var u in view.Units.Where(u => u.IsEligible && u.Position.HasValue)) framing.Encapsulate(Vector(u.Position.Value) + Vector3.up);
                framing.Expand(3); framedAttempt = view.AttemptId;
            }
        }

        private void Reconcile(BattleView view)
        {
            var living = new HashSet<string>(view.HealthOverlays.Select(h => h.UnitId), StringComparer.Ordinal);
            foreach (string id in tokens.Keys.Where(id => !living.Contains(id)).ToArray())
            { Object.Destroy(tokens[id].Body); tokens[id].Bar.RemoveFromHierarchy(); tokens.Remove(id); }
            foreach (var h in view.HealthOverlays)
            {
                if (!tokens.TryGetValue(h.UnitId, out var t))
                {
                    t = new Token(); tokens.Add(h.UnitId, t);
                    t.Body = Primitive(h.UnitId, h.IsEnemyControlled ? PrimitiveType.Cube : PrimitiveType.Capsule, Vector3.zero,
                        h.IsEnemyControlled ? new Vector3(.5f, (float)h.Height, .5f) : new Vector3(.5f, (float)h.Height / 2, .5f), h.IsEnemyControlled ? enemy : friendly);
                    t.Bar = new VisualElement { pickingMode = PickingMode.Ignore }; t.Bar.AddToClassList("health-overlay");
                    t.Caption = new Label { pickingMode = PickingMode.Ignore }; t.Bar.Add(t.Caption);
                    var track = new VisualElement { pickingMode = PickingMode.Ignore }; track.AddToClassList("health-track"); t.Bar.Add(track);
                    t.Fill = new VisualElement { pickingMode = PickingMode.Ignore }; t.Fill.AddToClassList("health-fill");
                    t.Fill.style.backgroundColor = h.IsEnemyControlled ? new Color(.96f, .43f, .32f) : new Color(.25f, .87f, .83f); track.Add(t.Fill);
                }
                t.Height = (float)h.Height;
                t.Position = Vector(h.Position) + Vector3.up * t.Height / 2;
                t.Body.transform.position = t.Position; t.Body.transform.localRotation = Quaternion.identity;
                t.Caption.text = FrontiersPlaytest.UnitName(h.UnitId) + "  " + h.CurrentHealth + "/" + h.MaximumHealth;
                t.Fill.style.width = Length.Percent((float)h.Fraction * 100);
            }
        }

        public void Layout(VisualElement target, VisualElement root)
        {
            if (target.worldBound.width <= 1 || target.worldBound.height <= 1 || root.resolvedStyle.width <= 1) return;
            float sx = Screen.width / root.resolvedStyle.width, sy = Screen.height / root.resolvedStyle.height;
            var rect = target.worldBound;
            camera.pixelRect = new Rect(rect.x * sx, Screen.height - rect.yMax * sy, rect.width * sx, rect.height * sy);
            camera.transform.rotation = Quaternion.Euler(58, 0, 0);
            camera.transform.position = framing.center - camera.transform.forward * 30;
            // Bounding box in camera space, with room for bars above bodies.
            float needed = 4;
            for (int i = 0; i < 8; i++)
            {
                var p = framing.center + Vector3.Scale(framing.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var local = camera.transform.InverseTransformPoint(p);
                needed = Mathf.Max(needed, Mathf.Abs(local.y) + 1.5f, (Mathf.Abs(local.x) + 1) / camera.aspect);
            }
            camera.orthographicSize = needed;
            // Stable ID order + collision avoidance keeps adjacent units' numeric health readable.
            barRects.Clear();
            foreach (var pair in tokens.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                var t = pair.Value;
                var point = Project(t.Body.transform.position + Vector3.up * (t.Height / 2 + .25f), root) - rect.position;
                var bar = new Rect(Mathf.Clamp(point.x - 48, 0, Mathf.Max(0, rect.width - 96)), Mathf.Clamp(point.y - 34, 0, Mathf.Max(0, rect.height - 34)), 96, 32);
                float preferred = bar.y;
                for (int n = 1; n <= tokens.Count * 2 && barRects.Any(other => other.Overlaps(bar)); n++)
                    bar.y = Mathf.Clamp(preferred + (n % 2 == 1 ? -1 : 1) * ((n + 1) / 2) * 34, 0, Mathf.Max(0, rect.height - 34));
                barRects.Add(bar); t.Bar.style.left = bar.x; t.Bar.style.top = bar.y;
            }
        }

        public Vector2 Project(Vector3 position, VisualElement root)
        {
            var p = camera.WorldToScreenPoint(position);
            return RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(p.x, Screen.height - p.y));
        }
        public string PickUnit(Vector2 panel, VisualElement root)
        {
            // Bounds include the rendered body and its health badge. Closest projected body wins ties.
            string nearest = null; float best = float.MaxValue;
            foreach (var pair in tokens)
            {
                var t = pair.Value; var center = Project(t.Body.transform.position, root);
                var head = Project(t.Body.transform.position + Vector3.up * t.Height / 2, root);
                var feet = Project(t.Body.transform.position - Vector3.up * t.Height / 2, root);
                var body = new Rect(center.x - 18, Mathf.Min(head.y, feet.y) - 10, 36, Mathf.Abs(head.y - feet.y) + 20);
                float distance = Vector2.SqrMagnitude(center - panel);
                if ((body.Contains(panel) || t.Bar.worldBound.Contains(panel)) && distance < best) { best = distance; nearest = pair.Key; }
            }
            return nearest;
        }
        public bool PickGround(Vector2 panel, VisualElement root, out FieldPoint point)
        {
            var pixel = new Vector2(panel.x * Screen.width / root.resolvedStyle.width, Screen.height - panel.y * Screen.height / root.resolvedStyle.height);
            var ray = camera.ScreenPointToRay(pixel); point = default;
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance)) return false;
            var hit = ray.GetPoint(distance);
            // Half-unit destination assistance is a fixture UI choice; core still supports free-position paths.
            point = new FieldPoint((decimal)(Mathf.Round(hit.x * 2) / 2), 0, (decimal)(Mathf.Round(hit.z * 2) / 2)); return true;
        }

        public IEnumerator Play(BattleUpdate update, float duration)
        {
            path.positionCount = 0; destination.SetActive(false);
            // Keep before-state bars visible during playback; reconcile authoritative after-state once finished.
            float elapsed = 0;
            var movement = update.Events.FirstOrDefault(e => e.Kind == BattleEventKind.Movement && e.Movement != null);
            var pulse = update.Events.FirstOrDefault(e => e.Kind == BattleEventKind.AbilityUsed);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; float t = Mathf.Clamp01(elapsed / duration);
                foreach (var u in update.After.Units.Where(u => u.Position.HasValue))
                {
                    if (!tokens.TryGetValue(u.Id, out var token)) continue;
                    var before = update.Before.Units.FirstOrDefault(x => x.Id == u.Id);
                    if (before?.Position == null) continue;
                    Vector3 position = Vector3.Lerp(Vector(before.Position.Value), Vector(u.Position.Value), t);
                    if (movement?.UnitId == u.Id)
                    {
                        var route = new[] { before.Position.Value }.Concat(movement.Movement.Path).ToArray();
                        float segment = t * (route.Length - 1); int index = Mathf.Min((int)segment, route.Length - 2);
                        position = Vector3.Lerp(Vector(route[index]), Vector(route[index + 1]), segment - index);
                    }
                    token.Body.transform.position = position + Vector3.up * token.Height / 2;
                    if (pulse?.UnitId == u.Id) token.Body.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI) * 12);
                    if (pulse?.TargetId == u.Id) token.Body.transform.position += Vector3.up * Mathf.Sin(t * Mathf.PI) * .18f;
                }
                yield return null;
            }
            Reconcile(update.After);
        }
        public void Hide() { world.SetActive(false); camera.enabled = true; camera.rect = new Rect(0, 0, 1, 1); }
        public static Vector3 Vector(FieldPoint p) => new Vector3((float)p.X, (float)p.Y, (float)p.Z);
        public void Dispose() { Object.Destroy(world); foreach (var material in materials) Object.Destroy(material); }
    }
}
