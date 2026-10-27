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
            public GameObject Body, TargetRing, CarryIcon, ContactShadow;
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
        private readonly FrontiersBattleVisualCatalog approvedModels;
        private string drawnSceneStudy = "none";
        public string SceneryStatus => terrain == null ? "not built (open a battle)" :
            "study 03 | " + drawnSceneStudy + " | meshes: " + scenery.MeshCount +
            " | visible: " + world.activeInHierarchy;
        private string drawnAttempt;
        private string rescueSubjectId;
        private readonly GameObject destination, selection, inspection, impact, statusPulse;
        private readonly GameObject impactBurst;
        private readonly Transform[] impactShards = new Transform[8];
        private readonly Mesh markerRingMesh;
        private readonly Texture2D contactShadowTexture;
        private readonly Material contactShadowMaterial;
        private readonly bool translucentContactShadows;
        private readonly Light battlefieldKey, impactLight;
        private Vector2 cameraImpulse;
        private VisualElement surface;
        private Bounds framing;
        private readonly List<Rect> barRects = new List<Rect>();

        public PlaytestBattlefield(Transform parent, Camera camera, Material template,
            FrontiersBattleVisualCatalog approvedModels = null)
        {
            this.camera = camera;
            this.approvedModels = approvedModels;
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
            markerRingMesh = BuildMarkerRingMesh();
            contactShadowTexture = BuildContactShadowTexture();
            var shadowShader = Shader.Find("Sprites/Default");
            if (shadowShader == null) shadowShader = Shader.Find("Unlit/Transparent");
            // A stripped shader must never leave opaque black quads on the map.
            translucentContactShadows = shadowShader != null;
            contactShadowMaterial = shadowShader != null
                ? new Material(shadowShader) : new Material(template);
            contactShadowMaterial.name = "Mobile grounded unit contact shadows";
            contactShadowMaterial.color = Color.white;
            contactShadowMaterial.mainTexture = contactShadowTexture;
            materials.Add(contactShadowMaterial);
            var keyObject = new GameObject("Battlefield art key · presentation only");
            keyObject.transform.SetParent(world.transform,false);
            battlefieldKey = keyObject.AddComponent<Light>();
            battlefieldKey.type = LightType.Directional;
            battlefieldKey.shadows = LightShadows.None;
            battlefieldKey.intensity = 1.05f;
            battlefieldKey.transform.localRotation = Quaternion.Euler(48f,-38f,0f);
            var hitLightObject = new GameObject("Pooled ability impact light");
            hitLightObject.transform.SetParent(world.transform,false);
            impactLight = hitLightObject.AddComponent<Light>();
            impactLight.type = LightType.Point;
            impactLight.shadows = LightShadows.None;
            impactLight.range = 3.8f;
            impactLight.intensity = 0f;
            impactLight.enabled = false;
            destination = MarkerRing("Destination preview · open center",gold,.70f);
            selection = MarkerRing("Active unit halo · open center",active,.95f);
            var white = Make(new Color(.9f,.96f,1f));
            inspection = MarkerRing("Inspected unit halo · open center",white,1.15f);
            impact = Primitive("Action impact",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.2f,gold); impact.SetActive(false);
            // Pooled, reusable radial strike shards: zero per-frame allocations,
            // no colliders, no scene-object creation when an ability fires.
            impactBurst = new GameObject("Battle impact · eight radial mineral shards");
            impactBurst.transform.SetParent(world.transform,false);
            for(int i=0;i<impactShards.Length;i++)
            {
                var shard=Primitive("Arcana / impact shard "+i,PrimitiveType.Cube,
                    Vector3.zero,new Vector3(.06f,.06f,.22f),gold);
                shard.transform.SetParent(impactBurst.transform,false);
                impactShards[i]=shard.transform;
            }
            impactBurst.SetActive(false);
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

        // One shared open-center mesh replaces opaque cylinders for tactical
        // selection, destination, inspection and target indications.
        private static Mesh BuildMarkerRingMesh()
        {
            const int count=48;
            var vertices=new Vector3[count*2];
            var normals=new Vector3[count*2];
            var uv=new Vector2[count*2];
            var triangles=new int[count*6];
            for(int i=0;i<count;i++)
            {
                float angle=i*Mathf.PI*2f/count;
                var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                vertices[i*2]=direction*.5f;
                vertices[i*2+1]=direction*.425f;
                normals[i*2]=normals[i*2+1]=Vector3.up;
                uv[i*2]=uv[i*2+1]=new Vector2((direction.x+1f)*.5f,(direction.z+1f)*.5f);
                int outside=i*2, inside=outside+1;
                int next=((i+1)%count)*2;
                int t=i*6;
                triangles[t]=outside; triangles[t+1]=next+1; triangles[t+2]=next;
                triangles[t+3]=outside; triangles[t+4]=inside; triangles[t+5]=next+1;
            }
            var mesh=new Mesh {
                name="Ninefold shared open-center tactical ring",
                vertices=vertices,normals=normals,uv=uv,triangles=triangles
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        private GameObject MarkerRing(string name,Material material,float diameter)
        {
            var ring=new GameObject(name);
            ring.transform.SetParent(world.transform,false);
            ring.transform.localScale=new Vector3(diameter,1f,diameter);
            ring.AddComponent<MeshFilter>().sharedMesh=markerRingMesh;
            ring.AddComponent<MeshRenderer>().sharedMaterial=material;
            return ring;
        }

        private static Texture2D BuildContactShadowTexture()
        {
            const int dimension=64;
            var texture=new Texture2D(dimension,dimension,TextureFormat.RGBA32,false)
            {
                name="Mobile soft contact shadow · shared",
                filterMode=FilterMode.Bilinear,
                wrapMode=TextureWrapMode.Clamp
            };
            var pixels=new Color[dimension*dimension];
            for(int y=0;y<dimension;y++) for(int x=0;x<dimension;x++)
            {
                float px=(x+.5f-dimension*.5f)/(dimension*.5f);
                float py=(y+.5f-dimension*.5f)/(dimension*.5f);
                float radius=Mathf.Sqrt(px*px+py*py);
                float falloff=Mathf.Pow(Mathf.Clamp01(1f-radius),2f);
                pixels[y*dimension+x]=new Color(0f,0f,0f,.31f*falloff);
            }
            texture.SetPixels(pixels);
            texture.Apply(false,true);
            return texture;
        }

        private GameObject ContactShadow(string id)
        {
            var shadow=Primitive(id+" · grounded contact shadow",PrimitiveType.Quad,
                Vector3.zero,new Vector3(1.65f,1.65f,1f),contactShadowMaterial);
            // Unity Quad faces +Z; -90 X points it upward toward the camera.
            shadow.transform.rotation=Quaternion.Euler(-90f,0f,0f);
            var renderer=shadow.GetComponent<Renderer>();
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows=false;
            return shadow;
        }

        public void Show(InteractionView interaction, BattlePlanningView planning, IReadOnlyList<ObjectiveDefinition> objectives, VisualElement target,
            string environmentKey = null)
        {
            var view = interaction.Battle; var intent = interaction.Pending;
            surface = target; world.SetActive(true); camera.enabled = true;
            rescueSubjectId = objectives.FirstOrDefault(o=>o.Kind == ObjectiveKind.RescueAndExtract)?.Subjects.FirstOrDefault();
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
            // Presentation-only environment lighting. Does not alter encounter
            // data, navigation, model prefabs or shared RenderSettings.
            battlefieldKey.color = Color.Lerp(direction.Fill,Color.white,.25f);
            battlefieldKey.intensity = direction.Study.StartsWith("W5",StringComparison.Ordinal) ? 1.15f : .95f;
            battlefieldKey.transform.localRotation = Quaternion.Euler(52f,
                direction.Study.Contains("fracture") ? -25f : -43f,0f);
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
            var next = actors.Where(actor =>
                actor.UnitId == WorldFiveTutorialWildlifeV1.SillrunnerId ||
                actor.UnitId == WorldFiveTutorialWildlifeV1.SeamstalkerId)
                .ToDictionary(actor => actor.Id, actor => actor.UnitId, StringComparer.Ordinal);
            if (next.Count == wildlifeActors.Count && next.All(pair =>
                wildlifeActors.TryGetValue(pair.Key,out var prior) && prior == pair.Value))
                return;
            // Recreate visuals when switching mission studies that reuse actor
            // spawn IDs; never let a prior creature skin leak into a new battle.
            foreach (var token in tokens.Values)
            {
                Object.Destroy(token.Body);
                Object.Destroy(token.TargetRing);
                Object.Destroy(token.ContactShadow);
                token.Bar.RemoveFromHierarchy();
            }
            tokens.Clear();
            wildlifeActors.Clear();
            foreach (var pair in next) wildlifeActors.Add(pair.Key,pair.Value);
        }

        // Lightweight primitive assembly, not an approved anatomical sculpt.
        // Each child is owned by its token root and is destroyed with it.
        private void AddWildlifeForm(GameObject root, bool stalker)
        {
            GameObject Part(string name, PrimitiveType shape, Vector3 offset, Vector3 scale, Material material)
            {
                var part = Primitive(name, shape, Vector3.zero, Vector3.one, material);
                part.transform.SetParent(root.transform, false);
                part.transform.localPosition = offset;
                part.transform.localScale = scale;
                return part;
            }
            var hide = stalker ? seamstalker : sillrunner;
            // Forward is positive Z. The low body and separate limbs make
            // wildlife read as animals, not standing humanoid tokens.
            Part("Dorsal shell", PrimitiveType.Sphere, new Vector3(0,.20f,-.05f),
                new Vector3(.86f,.34f,.9f), hide);
            Part("Head", PrimitiveType.Sphere, new Vector3(0,.12f,.60f),
                new Vector3(.55f,.42f,.56f), hide);
            foreach (float side in new[] { -1f,1f })
            {
                for (int i=0;i<2;i++)
                {
                    Part("Walking limb", PrimitiveType.Capsule,
                        new Vector3(side*.47f,-.38f,i==0?.34f:-.38f),
                        new Vector3(.18f,.58f,.18f), hide);
                }
                Part("Sensory ridge", PrimitiveType.Cube,
                    new Vector3(side*.24f,.39f,.60f),
                    new Vector3(.11f,.21f,.43f), trim);
            }
            if (stalker)
            {
                Part("Tapered tail", PrimitiveType.Capsule,
                    new Vector3(0,-.08f,-.75f),new Vector3(.22f,.22f,.83f),hide);
                Part("Shoulder spine",PrimitiveType.Cube,
                    new Vector3(0,.48f,-.14f),new Vector3(.14f,.28f,.43f),markings);
            }
            else
            {
                Part("Balance fan",PrimitiveType.Sphere,
                    new Vector3(0,.08f,-.67f),new Vector3(.6f,.24f,.35f),trim);
            }
        }

        // Procedural motion on the child pieces only. The authoritative root,
        // placement, footprint, targeting and combat outcome never change.
        private void PoseWildlife(Token token, string species, float phase,
            bool moving, bool attacking, float strike)
        {
            if (species != WorldFiveTutorialWildlifeV1.SillrunnerId &&
                species != WorldFiveTutorialWildlifeV1.SeamstalkerId) return;
            bool stalker = species == WorldFiveTutorialWildlifeV1.SeamstalkerId;
            int legIndex = 0;
            foreach (Transform part in token.Body.transform)
            {
                if (part.name == "Walking limb")
                {
                    int pair = legIndex % 2; // front or rear on the same side
                    int side = legIndex < 2 ? -1 : 1;
                    float amplitude = stalker ? 24f : 13f;
                    float stride = moving ? Mathf.Sin(phase * (stalker ? 21f : 17f)
                        + pair * Mathf.PI + (side > 0 ? Mathf.PI : 0)) : 0f;
                    part.localRotation = Quaternion.Euler(amplitude * stride,0,0);
                    // Lift the advancing foot; child offsets return to their authored
                    // location when the creature stops or reduced motion is selected.
                    float lift = moving ? Mathf.Max(0f,stride) * (stalker ? .12f : .075f) : 0f;
                    part.localPosition = new Vector3(side*.47f,-.38f+lift,pair==0?.34f:-.38f);
                    legIndex++;
                }
                else if (part.name == "Head")
                {
                    float lean = attacking ? Mathf.Sin(strike * Mathf.PI) * (stalker ? -27f : -12f) : 0f;
                    part.localRotation = Quaternion.Euler(lean,0,0);
                }
                else if (part.name == "Tapered tail")
                    part.localRotation = Quaternion.Euler(0,
                        (moving || attacking) ? Mathf.Sin(phase * 12f)*18f : 0f,0);
                else if (part.name == "Balance fan")
                    part.localRotation = Quaternion.Euler(0,
                        moving ? Mathf.Sin(phase * 10f)*11f : 0f,0);
                else if (part.name == "Dorsal shell")
                    part.localRotation = Quaternion.Euler(
                        moving ? Mathf.Sin(phase * (stalker ? 21f : 17f)) * 4f : 0f,0,0);
            }
        }

        private static Bounds MeasureLocalVisualBounds(Transform origin,Renderer[] renderers)
        {
            bool started=false;
            Bounds result=default;
            foreach(var renderer in renderers)
            {
                var r=renderer.bounds;
                for(int i=0;i<8;i++)
                {
                    var corner=new Vector3(
                        (i&1)==0?r.min.x:r.max.x,
                        (i&2)==0?r.min.y:r.max.y,
                        (i&4)==0?r.min.z:r.max.z);
                    var point=origin.InverseTransformPoint(corner);
                    if(!started) { result=new Bounds(point,Vector3.zero); started=true; }
                    else result.Encapsulate(point);
                }
            }
            return result;
        }

        // Battlefield visual handoff: game state and target picking continue to
        // use the actual Core UnitId and the normal token anchor. Only its
        // rendered body is replaced; no prefab component owns gameplay rules.
        private bool TryCreateApprovedVisual(string unitId,out GameObject body)
        {
            body=null;
            if(approvedModels==null ||
                !approvedModels.TryGetApproved(unitId,out var selected))
                return false;
            // An approved battle prefab is art only. Reject live gameplay
            // scripts before Instantiate so OnEnable cannot execute on the
            // battlefield. Animators and renderers remain supported.
            if(selected.Prefab.GetComponentsInChildren<MonoBehaviour>(true).Length>0)
            {
                Debug.LogWarning("Battle model "+unitId+
                    " contains MonoBehaviour scripts; remove them from the visual-only prefab.");
                return false;
            }

            var root=new GameObject(unitId+" · approved combat model");
            root.transform.SetParent(world.transform,false);
            var fitting=new GameObject("Tactical model normalization");
            fitting.transform.SetParent(root.transform,false);
            GameObject instance=null;
            try
            {
                instance=Object.Instantiate(selected.Prefab,fitting.transform,false);
                // Model prefabs may retain authoring components from art review.
                // Disable any physical interaction/embedded scene lighting:
                // the renderer remains cosmetic and never controls battle.
                foreach(var collider in instance.GetComponentsInChildren<Collider>(true))
                    collider.enabled=false;
                foreach(var rigidbody in instance.GetComponentsInChildren<Rigidbody>(true))
                {
                    rigidbody.isKinematic=true;
                    rigidbody.detectCollisions=false;
                }
                foreach(var embeddedCamera in instance.GetComponentsInChildren<Camera>(true))
                    embeddedCamera.enabled=false;
                foreach(var embeddedLight in instance.GetComponentsInChildren<Light>(true))
                    embeddedLight.enabled=false;
                foreach(var animator in instance.GetComponentsInChildren<Animator>(true))
                    animator.applyRootMotion=false;

                var renderers=instance.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer=>renderer.enabled && renderer.gameObject.activeInHierarchy)
                    .ToArray();
                if(renderers.Length==0)
                    throw new InvalidOperationException("Prefab has no active visible renderer.");
                // Renderer bounds are world-space, but fitting takes place in
                // the token's local space. Convert every AABB corner first so
                // review-prefab roots and non-identity scene transforms work.
                var bounds=MeasureLocalVisualBounds(fitting.transform,renderers);
                if(bounds.size.y<=.001f ||
                    Mathf.Max(bounds.size.x,bounds.size.z)<=.001f ||
                    float.IsNaN(bounds.size.y) || float.IsInfinity(bounds.size.y))
                    throw new InvalidOperationException("Prefab has invalid visual bounds.");

                // Preserve the proportions and authored material stack. Both
                // the visual height and footprint are capped independently;
                // exceptional units can get explicit limits in the catalog.
                float maxHeight=Mathf.Clamp(selected.MaximumHeight,.6f,2.5f);
                float maxWidth=Mathf.Clamp(selected.MaximumWidth,.5f,3.5f);
                float factor=Mathf.Min(maxHeight/bounds.size.y,
                    maxWidth/Mathf.Max(bounds.size.x,bounds.size.z));
                if(float.IsNaN(factor) || float.IsInfinity(factor) || factor<=.00001f)
                    throw new InvalidOperationException("Model bounds cannot be fitted.");
                fitting.transform.localScale=Vector3.one*factor;
                fitting.transform.localPosition=new Vector3(
                    -bounds.center.x*factor,
                    -StandardTokenHeight*.5f - bounds.min.y*factor,
                    -bounds.center.z*factor);
                body=root;
                return true;
            }
            catch(Exception error)
            {
                Debug.LogWarning("Battle prefab unavailable for "+unitId+
                    "; keeping abstract token. "+error.Message);
                root.SetActive(false);
                Object.Destroy(root);
                return false;
            }
        }

        private void Reconcile(BattleView view)
        {
            var living = new HashSet<string>(view.HealthOverlays.Select(h => h.UnitId), StringComparer.Ordinal);
            foreach (string id in tokens.Keys.Where(id => !living.Contains(id)).ToArray())
            { Object.Destroy(tokens[id].Body); Object.Destroy(tokens[id].TargetRing);
                Object.Destroy(tokens[id].ContactShadow);
                tokens[id].Bar.RemoveFromHierarchy(); tokens.Remove(id); }
            foreach (var h in view.HealthOverlays)
            {
                if (!tokens.TryGetValue(h.UnitId, out var t))
                {
                    t = new Token(); tokens.Add(h.UnitId, t);
                    wildlifeActors.TryGetValue(h.UnitId, out var creature);
                    bool isSillrunner = creature == WorldFiveTutorialWildlifeV1.SillrunnerId;
                    bool isSeamstalker = creature == WorldFiveTutorialWildlifeV1.SeamstalkerId;
                    bool hasApprovedPrefab = TryCreateApprovedVisual(h.UnitId,out var modelBody);
                    if(hasApprovedPrefab)
                        t.Body=modelBody;
                    else
                    {
                        t.Body = Primitive(h.UnitId,
                            isSillrunner ? PrimitiveType.Sphere :
                            isSeamstalker ? PrimitiveType.Capsule :
                            h.IsEnemyControlled ? PrimitiveType.Cube : PrimitiveType.Capsule,
                            Vector3.zero,
                            isSillrunner ? new Vector3(.8f,StandardTokenHeight,.75f) :
                            isSeamstalker ? new Vector3(.64f,StandardTokenHeight/2,.85f) :
                            h.IsEnemyControlled ? new Vector3(.5f,StandardTokenHeight,.5f) :
                            new Vector3(.5f,StandardTokenHeight/2,.5f),
                            isSillrunner ? sillrunner : isSeamstalker ? seamstalker :
                            h.IsEnemyControlled ? enemy : friendly);
                        if (isSillrunner || isSeamstalker)
                            AddWildlifeForm(t.Body,isSeamstalker);
                        // The primitive-only locator collar and footing are not
                        // layered on top of approved organic production art.
                        var footing = Primitive("Unit footing",
                            h.IsEnemyControlled ? PrimitiveType.Cube : PrimitiveType.Cylinder,
                            Vector3.down*(StandardTokenHeight/2-.09f),
                            new Vector3(.65f,.07f,.65f),grid);
                        footing.transform.SetParent(t.Body.transform,true);
                        var collar = Primitive("Unit ID collar",PrimitiveType.Cube,
                            new Vector3(0,StandardTokenHeight*.22f,-.02f),
                            new Vector3(.54f,.12f,.54f),active);
                        collar.transform.SetParent(t.Body.transform,true);
                    }
                    t.Scale=t.Body.transform.localScale;
                    t.TargetRing = MarkerRing("Valid target halo · open center",gold,.95f);
                    t.TargetRing.SetActive(false);
                    t.ContactShadow = ContactShadow(h.UnitId);
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
                // Restore neutral poses after animation and on reduced-motion frames.
                if (wildlifeActors.TryGetValue(h.UnitId,out var species))
                    PoseWildlife(t,species,0f,false,false,0f);
                bool carried = view.Objectives.Any(o=>o.Kind == ObjectiveKind.RescueAndExtract && o.Rescued &&
                    o.Status == ObjectiveStatus.Active);
                bool pickedUp = carried && h.UnitId == rescueSubjectId;
                // The carried traveler no longer appears to be an independently
                // selectable/stranded NPC at the old pickup site.
                t.Body.SetActive(!pickedUp);
                t.ContactShadow.SetActive(!pickedUp && translucentContactShadows);
                if (carried && h.UnitId == view.RescueCarrierId && t.CarryIcon == null)
                {
                    t.CarryIcon = Primitive("Rescued traveler carried", PrimitiveType.Sphere,
                        Vector3.zero,new Vector3(.56f,.56f,.56f),gold);
                    t.CarryIcon.transform.SetParent(t.Body.transform,false);
                    t.CarryIcon.transform.localPosition = new Vector3(.67f,-.18f,0);
                }
                if ((!carried || h.UnitId != view.RescueCarrierId) && t.CarryIcon != null)
                {
                    Object.Destroy(t.CarryIcon);
                    t.CarryIcon = null;
                }
                t.Height = StandardTokenHeight;
                t.Bar.EnableInClassList("active-health",h.UnitId == view.Activation?.UnitId);
                t.Position = Vector(h.Position) + Vector3.up * t.Height / 2;
                t.TargetRing.transform.position = Vector(h.Position)+Vector3.up*.055f;
                t.ContactShadow.transform.position = Vector(h.Position)+Vector3.up*.019f;
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
            // Tiny bounded art-only recoil, automatically zero outside hits.
            camera.transform.position += camera.transform.right * cameraImpulse.x +
                camera.transform.up * cameraImpulse.y;
            // Bounding box in camera space keeps bodies and objective markers in view.
            float needed = 4;
            for (int i = 0; i < 8; i++)
            {
                var p = framing.center + Vector3.Scale(framing.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var local = camera.transform.InverseTransformPoint(p);
                needed = Mathf.Max(needed, Mathf.Abs(local.y) + 1.5f, (Mathf.Abs(local.x) + 1) / camera.aspect);
            }
            camera.orthographicSize = needed;
            // Subtle active-unit heartbeat improves instant initiative scanning
            // without adding overhead text or changing hit-testing.
            if(selection.activeSelf)
            {
                float pulse=1f+Mathf.Sin(Time.unscaledTime*2.5f)*.035f;
                selection.transform.localScale=new Vector3(.95f*pulse,1f,.95f*pulse);
            }
            if(destination.activeSelf)
            {
                float pulse=1f+Mathf.Sin(Time.unscaledTime*3.4f)*.045f;
                destination.transform.localScale=new Vector3(.70f*pulse,1f,.70f*pulse);
            }
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
                var t = pair.Value;
                if (!t.Body.activeSelf) continue; var center = Project(t.Body.transform.position, root);
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
            inspection.SetActive(false); impact.SetActive(false); statusPulse.SetActive(false); impactBurst.SetActive(false);
            impactLight.enabled=false; cameraImpulse=Vector2.zero;
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
                float impactEnvelope = Mathf.Sin(strike*Mathf.PI);
                // Small impact-only recoil. Reduced-motion playback uses a zero
                // duration and skips the loop entirely.
                cameraImpulse = pulse == null ? Vector2.zero :
                    new Vector2(Mathf.Sin(strike*31f),Mathf.Cos(strike*24f)) * (.035f*impactEnvelope);
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
                    token.ContactShadow.transform.position = position + Vector3.up*.019f;
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
                    if (wildlifeActors.TryGetValue(before.Id,out var wildlifeSpecies))
                    {
                        bool travelling = movement?.UnitId == before.Id;
                        if (travelling)
                        {
                            // Orient the visual animal along its actual traversed path.
                            // Never rotate the authoritative collision body in Core.
                            var heading = AlongRoute(
                                new[]{before.Position.Value}.Concat(movement.Movement.Path).ToArray(),
                                Mathf.Min(1f,moveTime+.025f)) - position;
                            heading.y = 0;
                            if (heading.sqrMagnitude > .0001f)
                                token.Body.transform.localRotation = Quaternion.LookRotation(heading);
                        }
                        PoseWildlife(token,wildlifeSpecies,t,travelling,
                            pulse?.UnitId == before.Id,strike);
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
                    // The shock burst reads the same authoritative hit/heal/status
                    // event as floating damage feedback, without affecting rules.
                    var burstMaterial = healing ? friendly :
                        hasStatus && pulse.AbilityEffect.Health == null ? markings : gold;
                    // One reused, shadowless light brightens the existing models
                    // according to a real action event, with no scene allocation.
                    impactLight.enabled = true;
                    impactLight.color = burstMaterial.color;
                    impactLight.transform.position = target.Body.transform.position + Vector3.up*.35f;
                    impactLight.intensity = 1.7f*impactEnvelope;
                    impactBurst.SetActive(true);
                    impactBurst.transform.position = target.Body.transform.position;
                    float bloom = Mathf.Sin(strike*Mathf.PI);
                    for(int i=0;i<impactShards.Length;i++)
                    {
                        float angle = i*Mathf.PI*2f/impactShards.Length;
                        var direction = new Vector3(Mathf.Cos(angle),.12f,
                            Mathf.Sin(angle)).normalized;
                        var shard=impactShards[i];
                        shard.localPosition = direction*(.18f+.6f*strike);
                        shard.localRotation = Quaternion.LookRotation(direction);
                        shard.localScale = new Vector3(.045f,.05f,.14f+.38f*bloom);
                        shard.GetComponent<Renderer>().sharedMaterial = burstMaterial;
                    }
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
            path.positionCount=0; impact.SetActive(false); statusPulse.SetActive(false); impactBurst.SetActive(false);
            impactLight.enabled=false; cameraImpulse=Vector2.zero;
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
            impactBurst.SetActive(false); impactLight.enabled=false; cameraImpulse=Vector2.zero;
            world.SetActive(false); camera.enabled = true; camera.rect = new Rect(0, 0, 1, 1);
        }
        public static Vector3 Vector(FieldPoint p) => new Vector3((float)p.X, (float)p.Y, (float)p.Z);
        public void Dispose() { scenery.Dispose(); Object.Destroy(world);
            Object.Destroy(markerRingMesh); Object.Destroy(contactShadowTexture);
            foreach (var material in materials) Object.Destroy(material); }
    }
}
