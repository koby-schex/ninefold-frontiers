using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Views;
using Ninefold.Core.Missions;
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
            public GameObject Body, TargetRing;
            public VisualElement Bar, Fill;
            public Label Caption;
            public Vector3 Position;
            public float Height;
        }
        private readonly GameObject world;
        private readonly Camera camera;
        private readonly Dictionary<string, Token> tokens = new Dictionary<string, Token>();
        private readonly List<Material> materials = new List<Material>();
        private readonly Material friendly, enemy, active, floor, grid, gold, cover, trim, markings;
        private readonly LineRenderer path, range;
        private GameObject terrain;
        private string drawnAttempt;
        private readonly GameObject destination, selection;
        private VisualElement surface;
        private Bounds framing;
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
            cover = Make(new Color(.26f, .35f, .42f)); trim = Make(new Color(.42f, .52f, .57f));
            markings = Make(new Color(.22f, .31f, .37f));
            destination = Primitive("Destination preview", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.7f, .025f, .7f), gold);
            selection = Primitive("Active unit marker", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.95f, .015f, .95f), active);
            var lineObject = new GameObject("Movement preview"); lineObject.transform.SetParent(world.transform, false);
            path = lineObject.AddComponent<LineRenderer>(); path.sharedMaterial = gold;
            path.startWidth = path.endWidth = .045f; path.useWorldSpace = true;
            path.positionCount = 0;
            var rangeObject = new GameObject("Selected ability range"); rangeObject.transform.SetParent(world.transform,false);
            range = rangeObject.AddComponent<LineRenderer>(); range.sharedMaterial = friendly;
            range.startWidth = range.endWidth = .06f; range.useWorldSpace = true; range.positionCount = 0;
            // Unlit tactical lines stay readable independently of the scene's lighting.
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit != null)
            {
                var routeInk = new Material(unlit) { color = new Color(1f, .79f, .34f) }; materials.Add(routeInk);
                var rangeInk = new Material(unlit) { color = new Color(.33f, .94f, .91f) }; materials.Add(rangeInk);
                path.sharedMaterial = routeInk; range.sharedMaterial = rangeInk;
            }
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

        public void Show(InteractionView interaction, IReadOnlyList<ObjectiveDefinition> objectives, VisualElement target)
        {
            var view = interaction.Battle; var intent = interaction.Pending;
            surface = target; world.SetActive(true); camera.enabled = true;
            if (drawnAttempt != view.AttemptId) { BuildMap(view.Map,objectives); drawnAttempt = view.AttemptId; }
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
            // The whole authored arena stays framed; movement never expands or pans the camera.
            var bounds = view.Map.Bounds;
            framing = new Bounds((Vector(bounds.Min)+Vector(bounds.Max))/2,
                new Vector3((float)(bounds.Max.X-bounds.Min.X),2,(float)(bounds.Max.Z-bounds.Min.Z)));
            framing.center = new Vector3(framing.center.x,1,framing.center.z);
            range.positionCount = 0;
            if (actor != null && interaction.SelectedAbility.HasValue)
            {
                var ability = actor.Abilities.FirstOrDefault(a => a.Slot == interaction.SelectedAbility.Value);
                if (ability != null)
                {
                    var origin = Vector(actor.Position.Value) + Vector(actor.Body.AttackOffset);
                    var points = ClippedRange(origin, (float)ability.Range, bounds);
                    range.positionCount = points.Length; range.SetPositions(points);
                }
            }
            foreach (var pair in tokens) pair.Value.TargetRing.SetActive(interaction.Targets.Any(t => t.TargetId == pair.Key && t.IsValid));
        }

        private static Vector3[] ClippedRange(Vector3 origin, float radius, FieldBox bounds)
        {
            if (radius <= 0) return Array.Empty<Vector3>();
            var polygon = new List<Vector3>();
            for (int i=0;i<128;i++)
            {
                float angle=i*Mathf.PI*2/128;
                polygon.Add(new Vector3(origin.x+Mathf.Cos(angle)*radius,.34f,origin.z+Mathf.Sin(angle)*radius));
            }
            // Clip the sampled disk to the arena, rather than projecting circle points onto walls.
            // This is geometric range only. The core determines target legality and line of sight.
            void Clip(Func<Vector3,float> distance)
            {
                if (polygon.Count == 0) return;
                var result = new List<Vector3>(); var previous = polygon[polygon.Count-1]; float pd = distance(previous);
                foreach (var point in polygon)
                {
                    float d = distance(point);
                    if ((d >= 0) != (pd >= 0)) result.Add(Vector3.Lerp(previous,point,pd/(pd-d)));
                    if (d >= 0) result.Add(point);
                    previous=point; pd=d;
                }
                polygon=result;
            }
            Clip(p=>p.x-(float)bounds.Min.X); Clip(p=>(float)bounds.Max.X-p.x);
            Clip(p=>p.z-(float)bounds.Min.Z); Clip(p=>(float)bounds.Max.Z-p.z);
            if (polygon.Count > 0) polygon.Add(polygon[0]);
            return polygon.ToArray();
        }

        private void BuildMap(BattlefieldMap map, IReadOnlyList<ObjectiveDefinition> objectives)
        {
            if (terrain != null) { terrain.SetActive(false); Object.Destroy(terrain); }
            terrain = new GameObject("Authored arena"); terrain.transform.SetParent(world.transform,false);
            GameObject Shape(string name, Vector3 position, Vector3 scale, Material material)
            {
                var item = Primitive(name,PrimitiveType.Cube,position,scale,material);
                item.transform.SetParent(terrain.transform,true); return item;
            }
            var min = Vector(map.Bounds.Min); var max = Vector(map.Bounds.Max); var center = (min+max)/2;
            float width=max.x-min.x, depth=max.z-min.z;
            Shape("Bounded floor",new Vector3(center.x,-.16f,center.z),new Vector3(width,.3f,depth),floor);
            Shape("Arena plinth",new Vector3(center.x,-.42f,center.z),new Vector3(width+.4f,.25f,depth+.4f),grid);
            for (int x=(int)Math.Ceiling(min.x);x<=max.x;x++) Shape("Grid X",new Vector3(x,.002f,center.z),new Vector3(.018f,.01f,depth),grid);
            for (int z=(int)Math.Ceiling(min.z);z<=max.z;z++) Shape("Grid Z",new Vector3(center.x,.002f,z),new Vector3(width,.01f,.018f),grid);
            Shape("West boundary",new Vector3(min.x,.15f,center.z),new Vector3(.1f,.3f,depth),gold);
            Shape("East boundary",new Vector3(max.x,.15f,center.z),new Vector3(.1f,.3f,depth),gold);
            Shape("South boundary",new Vector3(center.x,.15f,min.z),new Vector3(width,.3f,.1f),gold);
            Shape("North boundary",new Vector3(center.x,.15f,max.z),new Vector3(width,.3f,.1f),gold);
            foreach (var o in map.Obstacles)
            {
                var a = Vector(o.Bounds.Min); var b = Vector(o.Bounds.Max); var size = b-a; var c = (a+b)/2;
                Shape("Cover / obstacle",c,size,cover);
                Shape("Cover top",new Vector3(c.x,b.y+.012f,c.z),new Vector3(size.x,.025f,size.z),trim);
                // Decorative seams remain inside the authored collision footprint.
                for (float x=a.x+.3f;x<b.x;x+=.7f)
                    Shape("Cover seam",new Vector3(x,b.y+.03f,c.z),new Vector3(.035f,.015f,size.z),grid);
            }
            foreach (var g in map.Ground)
            {
                var c=(Vector(g.Bounds.Min)+Vector(g.Bounds.Max))/2; var size=Vector(g.Bounds.Max)-Vector(g.Bounds.Min);
                Shape("Slow ground",new Vector3(c.x,.02f,c.z),new Vector3(size.x,.025f,size.z),markings);
                for (float z=(float)g.Bounds.Min.Z+.12f;z<(float)g.Bounds.Max.Z;z+=.35f)
                    Shape("Slow ground stripe",new Vector3(c.x,.04f,z),new Vector3(size.x,.015f,.06f),gold);
            }
            foreach (var o in objectives.Where(o=>o.Interaction!=null))
            {
                var p = Vector(o.Interaction.Point);
                Shape("Objective "+o.Id,p+Vector3.up*.04f,new Vector3(1.05f,.04f,1.05f),gold);
                Shape("Objective inset",p+Vector3.up*.07f,new Vector3(.78f,.025f,.78f),floor);
                Shape("Objective cross X",p+Vector3.up*.09f,new Vector3(.58f,.025f,.1f),active);
                Shape("Objective cross Z",p+Vector3.up*.09f,new Vector3(.1f,.025f,.58f),active);
            }
        }

        private void Reconcile(BattleView view)
        {
            var living = new HashSet<string>(view.HealthOverlays.Select(h => h.UnitId), StringComparer.Ordinal);
            foreach (string id in tokens.Keys.Where(id => !living.Contains(id)).ToArray())
            { Object.Destroy(tokens[id].Body); Object.Destroy(tokens[id].TargetRing); tokens[id].Bar.RemoveFromHierarchy(); tokens.Remove(id); }
            foreach (var h in view.HealthOverlays)
            {
                if (!tokens.TryGetValue(h.UnitId, out var t))
                {
                    t = new Token(); tokens.Add(h.UnitId, t);
                    t.Body = Primitive(h.UnitId, h.IsEnemyControlled ? PrimitiveType.Cube : PrimitiveType.Capsule, Vector3.zero,
                        h.IsEnemyControlled ? new Vector3(.5f, (float)h.Height, .5f) : new Vector3(.5f, (float)h.Height / 2, .5f), h.IsEnemyControlled ? enemy : friendly);
                    // Abstract tactical tokens: high contrast footing and a readable upper collar.
                    var footing = Primitive("Unit footing", h.IsEnemyControlled ? PrimitiveType.Cube : PrimitiveType.Cylinder,
                        Vector3.down * ((float)h.Height/2-.09f), new Vector3(.65f,.07f,.65f), grid);
                    footing.transform.SetParent(t.Body.transform,true);
                    var collar = Primitive("Unit ID collar",PrimitiveType.Cube,new Vector3(0,(float)h.Height*.22f,-.02f),new Vector3(.54f,.12f,.54f),active);
                    collar.transform.SetParent(t.Body.transform,true);
                    t.TargetRing = Primitive("Valid target marker",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.95f,.025f,.95f),gold);
                    t.TargetRing.SetActive(false);
                    t.Bar = new VisualElement { pickingMode = PickingMode.Ignore }; t.Bar.AddToClassList("health-overlay");
                    t.Bar.EnableInClassList("enemy-health",h.IsEnemyControlled);
                    t.Caption = new Label { pickingMode = PickingMode.Ignore }; t.Bar.Add(t.Caption);
                    var track = new VisualElement { pickingMode = PickingMode.Ignore }; track.AddToClassList("health-track"); t.Bar.Add(track);
                    t.Fill = new VisualElement { pickingMode = PickingMode.Ignore }; t.Fill.AddToClassList("health-fill");
                    t.Fill.style.backgroundColor = h.IsEnemyControlled ? new Color(.96f, .43f, .32f) : new Color(.25f, .87f, .83f); track.Add(t.Fill);
                }
                t.Height = (float)h.Height;
                t.Bar.EnableInClassList("active-health",h.UnitId == view.Activation?.UnitId);
                t.Position = Vector(h.Position) + Vector3.up * t.Height / 2;
                t.TargetRing.transform.position = Vector(h.Position)+Vector3.up*.055f;
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
            path.positionCount = 0; range.positionCount = 0; destination.SetActive(false);
            foreach (var t in tokens.Values) t.TargetRing.SetActive(false);
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
