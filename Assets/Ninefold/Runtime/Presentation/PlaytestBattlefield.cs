using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
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
        // Tactical silhouettes share a consistent visual height regardless of lore-scale.
        // Collision and pathfinding still use the authoritative Core body dimensions.
        private const float StandardTokenHeight = 1.45f;
        private sealed class Token
        {
            public GameObject Body, TargetRing;
            public VisualElement Bar, Fill;
            public Label Caption;
            public Vector3 Position;
            public Vector3 Scale;
            public float Height;
        }
        private sealed class FloatingFeedback
        {
            public Label Label;
            public Vector3 Position;
            public float Born;
            public bool Moves;
        }
        private readonly GameObject world;
        private readonly Camera camera;
        private readonly Dictionary<string, Token> tokens = new Dictionary<string, Token>();
        private readonly Dictionary<string, string> wildlifeActors = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<Material> materials = new List<Material>();
        private readonly Dictionary<string, Label> objectiveLabels = new Dictionary<string, Label>();
        private readonly Dictionary<string, Vector3> objectivePositions = new Dictionary<string, Vector3>();
        private readonly Dictionary<string, GameObject> objectiveVisuals = new Dictionary<string, GameObject>();
        private readonly List<FloatingFeedback> feedback = new List<FloatingFeedback>();
        private readonly Material friendly, enemy, active, floor, grid, gold, cover, trim, markings, sillrunner, seamstalker;
        private readonly LineRenderer path, range;
        private GameObject terrain;
        private readonly BattlefieldScenery scenery;
        private string drawnSceneStudy = "none";
        public string SceneryStatus => terrain == null ? "not built (open a battle)" :
            "study 03 | " + drawnSceneStudy + " | meshes: " + scenery.MeshCount +
            " | visible: " + world.activeInHierarchy;
        private string drawnAttempt;
        private readonly GameObject destination, selection, inspection, impact, statusPulse;
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
            sillrunner = Make(new Color(.56f,.74f,.56f)); seamstalker = Make(new Color(.63f,.40f,.77f));
            active = Make(new Color(.94f, .85f, .52f)); floor = Make(new Color(.075f, .12f, .17f));
            grid = Make(new Color(.16f, .23f, .29f)); gold = Make(new Color(.9f, .65f, .25f));
            cover = Make(new Color(.26f, .35f, .42f)); trim = Make(new Color(.42f, .52f, .57f));
            markings = Make(new Color(.28f, .24f, .28f));
            trim.color = new Color(.64f,.60f,.58f);
            grid.color = new Color(.23f,.21f,.27f);
            scenery = new BattlefieldScenery(template, floor, cover);
            destination = Primitive("Destination preview", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.7f, .025f, .7f), gold);
            selection = Primitive("Active unit marker", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.95f, .015f, .95f), active);
            var white = Make(new Color(.9f,.96f,1f));
            inspection = Primitive("Inspected unit marker",PrimitiveType.Cube,Vector3.zero,new Vector3(1.15f,.015f,1.15f),white);
            impact = Primitive("Action impact",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.2f,gold); impact.SetActive(false);
            statusPulse = Primitive("Status effect pulse", PrimitiveType.Cylinder, Vector3.zero,
                new Vector3(.5f,.015f,.5f), Make(new Color(.69f,.43f,.95f)));
            statusPulse.SetActive(false);
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
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .10f, .17f);
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

        public void Show(InteractionView interaction, BattlePlanningView planning, IReadOnlyList<ObjectiveDefinition> objectives, VisualElement target,
            string environmentKey = null)
        {
            var view = interaction.Battle; var intent = interaction.Pending;
            surface = target; world.SetActive(true); camera.enabled = true;
            if (drawnAttempt != view.AttemptId)
            {
                BuildMap(view.Map,objectives,view.MissionId,environmentKey);
                drawnAttempt = view.AttemptId;
            }
            Reconcile(view);
            foreach (var token in tokens.Values) surface.Add(token.Bar);
            foreach (var item in feedback) surface.Add(item.Label);
            var actor = view.Units.FirstOrDefault(u => u.Id == view.Activation?.UnitId && u.Position.HasValue);
            selection.SetActive(actor != null);
            if (actor != null) selection.transform.position = Vector(actor.Position.Value) + Vector3.up * .04f;
            var inspected = view.Units.FirstOrDefault(u=>u.Id==interaction.SelectedUnitId && u.Position.HasValue && u.IsEligible);
            inspection.SetActive(inspected != null && inspected.Id != actor?.Id);
            if (inspected != null) inspection.transform.position = Vector(inspected.Position.Value)+Vector3.up*.025f;
            foreach (var pair in objectiveLabels)
            {
                var state = view.Objectives.First(o=>o.Id==pair.Key);
                var available = planning.Objectives.FirstOrDefault(o=>o.Id==pair.Key);
                var definition = objectives.First(o=>o.Id==pair.Key);
                if (state.Kind == ObjectiveKind.RescueAndExtract &&
                    definition.Zone != null && definition.Interaction != null)
                {
                    var pickup = Vector(definition.Interaction.Point);
                    var exit = (Vector(definition.Zone.Min) + Vector(definition.Zone.Max)) / 2f;
                    exit.y = pickup.y;
                    bool extractionStage = state.Rescued && state.Status == ObjectiveStatus.Active;
                    objectivePositions[pair.Key] = extractionStage ? exit : pickup;
                    if (objectiveVisuals.TryGetValue(pair.Key,out var marker))
                        marker.transform.position = extractionStage ? exit - pickup : Vector3.zero;
                }
                pair.Value.text = state.Kind == ObjectiveKind.RescueAndExtract && state.Rescued &&
                    state.Status == ObjectiveStatus.Active ? "EXIT" :
                    state.Status == ObjectiveStatus.Active ? "◎" :
                    state.Status == ObjectiveStatus.Completed ? "✓" : "×";
                pair.Value.EnableInClassList("objective-ready",available?.CanInteract == true);
                surface.Add(pair.Value);
            }
            path.positionCount = 0; destination.SetActive(false);
            if (intent?.Kind == IntentKind.Movement && actor != null)
            {
                var points = new[] { actor.Position.Value }.Concat(intent.Movement.Path).Select(p => Vector(p) + Vector3.up * .08f).ToArray();
                path.positionCount = points.Length; path.SetPositions(points);
                destination.SetActive(true); destination.transform.position = points.Last();
            }
            if (intent?.Kind == IntentKind.Ability && actor != null && tokens.TryGetValue(intent.Target.TargetId,out var aim))
            {
                path.positionCount=2;
                path.SetPositions(new[] { Vector(actor.Position.Value)+Vector3.up,aim.Position });
            }
            // The whole authored arena stays framed; movement never expands or pans the camera.
            var bounds = view.Map.Bounds;
            framing = new Bounds((Vector(bounds.Min)+Vector(bounds.Max))/2,
                new Vector3((float)(bounds.Max.X-bounds.Min.X),2,(float)(bounds.Max.Z-bounds.Min.Z)));
            framing.center = new Vector3(framing.center.x,1,framing.center.z);
            range.positionCount = 0;
            if (actor != null && view.Phase == BattleSessionPhase.PlayerInput)
            {
                Vector3 origin;
                float radius;
                if (interaction.SelectedAbility.HasValue)
                {
                    var ability = actor.Abilities.FirstOrDefault(a => a.Slot == interaction.SelectedAbility.Value);
                    origin = Vector(actor.Position.Value) + Vector(actor.Body.AttackOffset);
                    radius = ability == null ? 0f : (float)ability.Range;
                }
                else
                {
                    // Geometric radius of remaining movement, clipped to the
                    // real arena. Walls and slow ground can further constrain a
                    // path: the authoritative core still validates every tap.
                    origin = Vector(actor.Position.Value) + Vector3.up * .04f;
                    radius = (float)(view.Activation?.MovementRemaining ?? 0m);
                }
                var points = ClippedRange(origin,radius,bounds);
                range.positionCount = points.Length;
                if (points.Length > 0) range.SetPositions(points);
            }
            foreach (var pair in tokens)
            {
                bool pending = intent?.Kind == IntentKind.Ability && intent.Target.TargetId==pair.Key;
                pair.Value.TargetRing.SetActive(
                    interaction.SelectedUnitId == pair.Key ||
                    interaction.Targets.Any(t => t.TargetId == pair.Key && t.IsValid));
                pair.Value.TargetRing.transform.localScale = pending ? new Vector3(1.35f,.025f,1.35f) : new Vector3(.95f,.025f,.95f);
                pair.Value.Bar.EnableInClassList("preview-health",pending);
                if (pending && intent.Target.Effect.Health != null)
                    pair.Value.Caption.text = intent.Target.Effect.Health.HealthBefore+" → "+intent.Target.Effect.Health.HealthAfter;
            }
            foreach (var item in feedback) item.Label.BringToFront();
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

        private void BuildMap(BattlefieldMap map, IReadOnlyList<ObjectiveDefinition> objectives, string missionId,
            string environmentKey)
        {
            var direction = BattlefieldSceneDirection.ForMission(missionId,environmentKey);
            drawnSceneStudy = direction.Study;
            camera.backgroundColor = direction.Sky;
            foreach (var label in objectiveLabels.Values) label.RemoveFromHierarchy();
            objectiveLabels.Clear(); objectivePositions.Clear(); objectiveVisuals.Clear();
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
            scenery.Build(terrain.transform, map, floor, direction);
            Shape("West boundary",new Vector3(min.x,.15f,center.z),new Vector3(.1f,.3f,depth),trim);
            Shape("East boundary",new Vector3(max.x,.15f,center.z),new Vector3(.1f,.3f,depth),trim);
            Shape("South boundary",new Vector3(center.x,.15f,min.z),new Vector3(width,.3f,.1f),trim);
            Shape("North boundary",new Vector3(center.x,.15f,max.z),new Vector3(width,.3f,.1f),trim);
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
                var group = new GameObject("Objective stage marker · "+o.Id);
                group.transform.SetParent(terrain.transform,false);
                Shape("Objective "+o.Id,p+Vector3.up*.04f,new Vector3(1.05f,.04f,1.05f),gold)
                    .transform.SetParent(group.transform,true);
                Shape("Objective inset",p+Vector3.up*.07f,new Vector3(.78f,.025f,.78f),floor)
                    .transform.SetParent(group.transform,true);
                Shape("Objective cross X",p+Vector3.up*.09f,new Vector3(.58f,.025f,.1f),active)
                    .transform.SetParent(group.transform,true);
                Shape("Objective cross Z",p+Vector3.up*.09f,new Vector3(.1f,.025f,.58f),active)
                    .transform.SetParent(group.transform,true);
                // Existing navigation cue moves with the objective stage.
                // It is decorative and has no collider or tap handler.
                Shape("Objective guide post",p+new Vector3(.65f,.48f,0),
                    new Vector3(.10f,.84f,.10f),gold)
                    .transform.SetParent(group.transform,true);
                Shape("Objective guide tip",p+new Vector3(.65f,.93f,0),
                    new Vector3(.29f,.08f,.29f),active)
                    .transform.SetParent(group.transform,true);
                var label = new Label { pickingMode = PickingMode.Ignore };
                label.AddToClassList("objective-marker");
                objectiveLabels.Add(o.Id,label); objectivePositions.Add(o.Id,p);
                objectiveVisuals.Add(o.Id,group);
            }
        }

        // Only the active mission catalog may assign a wildlife visual. Shared
        // spawn IDs from older studies never imply a particular creature type.
        public void ConfigureWildlifeActors(IEnumerable<ActorSpawn> actors)
        {
            wildlifeActors.Clear();
            foreach (var actor in actors)
                if (actor.UnitId == WorldFiveTutorialWildlifeV1.SillrunnerId ||
                    actor.UnitId == WorldFiveTutorialWildlifeV1.SeamstalkerId)
                    wildlifeActors[actor.Id] = actor.UnitId;
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
                    wildlifeActors.TryGetValue(h.UnitId, out var creature);
                    bool isSillrunner = creature == WorldFiveTutorialWildlifeV1.SillrunnerId;
                    bool isSeamstalker = creature == WorldFiveTutorialWildlifeV1.SeamstalkerId;
                    t.Body = Primitive(h.UnitId,
                        isSillrunner ? PrimitiveType.Sphere :
                        isSeamstalker ? PrimitiveType.Capsule :
                        h.IsEnemyControlled ? PrimitiveType.Cube : PrimitiveType.Capsule,
                        Vector3.zero,
                        isSillrunner ? new Vector3(.8f,.7f,.75f) :
                        isSeamstalker ? new Vector3(.64f,StandardTokenHeight/2,.85f) :
                        h.IsEnemyControlled ? new Vector3(.5f,StandardTokenHeight,.5f) :
                        new Vector3(.5f,StandardTokenHeight/2,.5f),
                        isSillrunner ? sillrunner : isSeamstalker ? seamstalker :
                        h.IsEnemyControlled ? enemy : friendly);
                    // Abstract tactical tokens: high contrast footing and a readable upper collar.
                    var footing = Primitive("Unit footing", h.IsEnemyControlled ? PrimitiveType.Cube : PrimitiveType.Cylinder,
                        Vector3.down * (StandardTokenHeight/2-.09f), new Vector3(.65f,.07f,.65f), grid);
                    footing.transform.SetParent(t.Body.transform,true);
                    var collar = Primitive("Unit ID collar",PrimitiveType.Cube,new Vector3(0,StandardTokenHeight*.22f,-.02f),new Vector3(.54f,.12f,.54f),active);
                    collar.transform.SetParent(t.Body.transform,true);
                    t.Scale=t.Body.transform.localScale;
                    t.TargetRing = Primitive("Valid target marker",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.95f,.025f,.95f),gold);
                    t.TargetRing.SetActive(false);
                    // Floating per-unit health bars obscure the field on
                    // portrait phones. All HP lives in the lower HUD when
                    // the player inspects a unit. Keep this non-rendered
                    // legacy container only to avoid touching battle logic.
                    t.Bar = new VisualElement { pickingMode = PickingMode.Ignore };
                    t.Bar.style.display = DisplayStyle.None;
                    t.Bar.EnableInClassList("enemy-health",h.IsEnemyControlled);
                    t.Caption = new Label { pickingMode = PickingMode.Ignore }; t.Bar.Add(t.Caption);
                    var track = new VisualElement { pickingMode = PickingMode.Ignore }; track.AddToClassList("health-track"); t.Bar.Add(track);
                    t.Fill = new VisualElement { pickingMode = PickingMode.Ignore }; t.Fill.AddToClassList("health-fill");
                    t.Fill.style.backgroundColor = h.IsEnemyControlled ? new Color(.96f, .43f, .32f) : new Color(.25f, .87f, .83f); track.Add(t.Fill);
                }
                t.Height = StandardTokenHeight;
                t.Bar.EnableInClassList("active-health",h.UnitId == view.Activation?.UnitId);
                t.Position = Vector(h.Position) + Vector3.up * t.Height / 2;
                t.TargetRing.transform.position = Vector(h.Position)+Vector3.up*.055f;
                t.Body.transform.position = t.Position; t.Body.transform.localRotation = Quaternion.identity; t.Body.transform.localScale=t.Scale;
                t.Caption.text = h.CurrentHealth + "/" + h.MaximumHealth;
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
            // Bounding box in camera space keeps bodies and objective markers in view.
            float needed = 4;
            for (int i = 0; i < 8; i++)
            {
                var p = framing.center + Vector3.Scale(framing.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var local = camera.transform.InverseTransformPoint(p);
                needed = Mathf.Max(needed, Mathf.Abs(local.y) + 1.5f, (Mathf.Abs(local.x) + 1) / camera.aspect);
            }
            camera.orthographicSize = needed;
            barRects.Clear();
            foreach (var pair in objectiveLabels)
            {
                var point = Project(objectivePositions[pair.Key],root)-rect.position;
                pair.Value.style.left=Mathf.Clamp(point.x-16,0,Mathf.Max(0,rect.width-32));
                pair.Value.style.top=Mathf.Clamp(point.y+12,0,Mathf.Max(0,rect.height-32));
                barRects.Add(new Rect(Mathf.Clamp(point.x-16,0,Mathf.Max(0,rect.width-32)),Mathf.Clamp(point.y+12,0,Mathf.Max(0,rect.height-32)),32,32));
            }
            foreach (var item in feedback.ToArray())
            {
                float age=Time.unscaledTime-item.Born;
                if (age>1.5f) { item.Label.RemoveFromHierarchy(); feedback.Remove(item); continue; }
                item.Label.style.opacity=Mathf.Clamp01((1.5f-age)/.4f);
                var point=Project(item.Position+(item.Moves ? Vector3.up*age*.35f : Vector3.zero),root)-rect.position;
                item.Label.style.left=Mathf.Clamp(point.x-65,0,Mathf.Max(0,rect.width-130));
                item.Label.style.top=Mathf.Clamp(point.y-24,0,Mathf.Max(0,rect.height-30));
            }
            // Deliberately no health-label layout. The world view displays
            // units, selection rings and brief combat-number feedback only;
            // vital statistics are housed in the compact lower combat HUD.
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
                if (body.Contains(panel) && distance < best) { best = distance; nearest = pair.Key; }
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
            inspection.SetActive(false); impact.SetActive(false); statusPulse.SetActive(false);
            foreach (var t in tokens.Values) t.TargetRing.SetActive(false);
            // Keep before-state bars visible during playback; reconcile authoritative after-state once finished.
            float elapsed = 0;
            var movement = update.Events.FirstOrDefault(e => e.Kind == BattleEventKind.Movement && e.Movement != null);
            var pulse = update.Events.FirstOrDefault(e => e.Kind == BattleEventKind.AbilityUsed);
            bool shown = false;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; float t = Mathf.Clamp01(elapsed / duration);
                if (!shown && t>=.55f) { AddFeedback(update,true); shown=true; }
                float moveTime = pulse == null ? t : Mathf.Clamp01(t/.55f);
                float strike = Mathf.Clamp01((t-.55f)/.45f);
                foreach (var before in update.Before.Units.Where(u => u.Position.HasValue))
                {
                    if (!tokens.TryGetValue(before.Id, out var token)) continue;
                    var after = update.After.Units.FirstOrDefault(u=>u.Id==before.Id);
                    Vector3 position = Vector3.Lerp(Vector(before.Position.Value),Vector(after?.Position ?? before.Position.Value),moveTime);
                    if (movement?.UnitId == before.Id)
                    {
                        var route = new[] { before.Position.Value }.Concat(movement.Movement.Path).ToArray();
                        position = AlongRoute(route,moveTime);
                    }
                    token.Body.transform.position = position + Vector3.up * token.Height / 2;
                    if (before.Id==update.Before.Activation?.UnitId) selection.transform.position=position+Vector3.up*.04f;
                    if (pulse?.UnitId == before.Id && tokens.TryGetValue(pulse.TargetId,out var strikeTarget))
                    {
                        // Anticipation, strike and recovery: move toward the actual target,
                        // then return. Never leave residual transform offsets after playback.
                        var facing = strikeTarget.Body.transform.position - token.Body.transform.position;
                        facing.y = 0;
                        if (facing.sqrMagnitude > .0001f)
                        {
                            var direction = facing.normalized;
                            float windup = Mathf.Sin(Mathf.Clamp01(t/.55f)*Mathf.PI);
                            float thrust = Mathf.Sin(Mathf.Clamp01((t-.55f)/.45f)*Mathf.PI);
                            token.Body.transform.position += direction * (-.11f*windup+.24f*thrust);
                            token.Body.transform.localRotation = Quaternion.Euler(0,
                                Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,
                                Mathf.Sin(strike*Mathf.PI)*8f);
                        }
                    }
                    if (pulse?.TargetId == before.Id)
                    {
                        float reaction = Mathf.Sin(strike*Mathf.PI);
                        token.Body.transform.position += Vector3.up * reaction * .11f;
                        token.Body.transform.localScale = token.Scale*(1f+.07f*reaction);
                    }
                    if (after?.Health?.IsDefeated == true)
                    {
                        token.Body.transform.localScale=token.Scale*(1-strike*.85f);
                        token.Body.transform.localRotation=Quaternion.Euler(0,0,strike*70);
                    }
                }
                if (pulse != null && strike>0 && tokens.TryGetValue(pulse.UnitId,out var source) && tokens.TryGetValue(pulse.TargetId,out var target))
                {
                    path.positionCount=2; path.SetPositions(new[] { source.Body.transform.position,target.Body.transform.position });
                    impact.SetActive(true); impact.transform.position=target.Body.transform.position;
                    impact.transform.localScale=Vector3.one*(.15f+Mathf.Sin(strike*Mathf.PI)*.6f);
                    bool healing = pulse.AbilityEffect?.Health != null &&
                        pulse.AbilityEffect.Health.HealthAfter > pulse.AbilityEffect.Health.HealthBefore;
                    bool hasStatus = pulse.AbilityEffect?.Status?.Applies == true;
                    // Read the authoritative effect, never guess based on a unit name
                    // or author new damage/status outcomes in the renderer.
                    impact.GetComponent<Renderer>().sharedMaterial = healing ? friendly :
                        hasStatus && pulse.AbilityEffect.Health == null ? markings : gold;
                    if (hasStatus)
                    {
                        statusPulse.SetActive(true);
                        statusPulse.transform.position = target.Body.transform.position -
                            Vector3.up * (target.Height*.5f-.04f);
                        float radius = .5f + strike*.9f;
                        statusPulse.transform.localScale = new Vector3(radius,.018f,radius);
                    }
                    else statusPulse.SetActive(false);
                }
                yield return null;
            }
            if (!shown) AddFeedback(update,false);
            path.positionCount=0; impact.SetActive(false); statusPulse.SetActive(false);
            Reconcile(update.After);
        }
        private static Vector3 AlongRoute(FieldPoint[] route, float fraction)
        {
            float total=0; for(int i=1;i<route.Length;i++) total+=Vector3.Distance(Vector(route[i-1]),Vector(route[i]));
            float remaining=total*fraction;
            for(int i=1;i<route.Length;i++)
            {
                float distance=Vector3.Distance(Vector(route[i-1]),Vector(route[i]));
                if (remaining<=distance) return Vector3.Lerp(Vector(route[i-1]),Vector(route[i]),distance==0?1:remaining/distance);
                remaining-=distance;
            }
            return Vector(route[route.Length-1]);
        }
        private void AddFeedback(BattleUpdate update, bool moves)
        {
            void Add(string text, Vector3 position, bool positive)
            {
                var label=new Label(text) { pickingMode=PickingMode.Ignore }; label.AddToClassList("combat-float");
                label.EnableInClassList("healing-float",positive); surface.Add(label);
                feedback.Add(new FloatingFeedback { Label=label,Position=position,Born=Time.unscaledTime,Moves=moves });
            }
            foreach (var after in update.After.Units)
            {
                var before=update.Before.Units.FirstOrDefault(u=>u.Id==after.Id);
                if (before?.Health == null || after.Health == null || !before.Position.HasValue) continue;
                int delta=after.Health.CurrentHealth-before.Health.CurrentHealth;
                if (delta==0) continue;
                Add((delta>0 ? "+"+delta : delta.ToString())+(after.Health.IsDefeated ? " · Defeated" : ""),Vector(after.Position ?? before.Position.Value)+Vector3.up*(float)before.Body.Height,delta>0);
            }
            foreach (var change in update.Events.Where(e=>e.Kind==BattleEventKind.ObjectiveChanged))
                if (objectivePositions.TryGetValue(change.ObjectiveId,out var position)) Add("Objective updated",position+Vector3.up*.8f,true);
        }
        public void Hide()
        {
            foreach (var item in feedback) item.Label.RemoveFromHierarchy(); feedback.Clear();
            world.SetActive(false); camera.enabled = true; camera.rect = new Rect(0, 0, 1, 1);
        }
        public static Vector3 Vector(FieldPoint p) => new Vector3((float)p.X, (float)p.Y, (float)p.Z);
        public void Dispose() { scenery.Dispose(); Object.Destroy(world); foreach (var material in materials) Object.Destroy(material); }
    }
}
