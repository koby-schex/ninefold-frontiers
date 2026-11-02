using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Ninefold.Editor
{
    /// <summary>
    /// Renders the real skinned Rift Forager prefab at tactical mobile scale.
    /// Never changes the user's open scene or playtest, never registers the
    /// unapproved model to gameplay, and never exports generated art as canon.
    /// </summary>
    public sealed class RiftForagerBattleCameraReview : EditorWindow
    {
        private const string ReviewRoot="Assets/Ninefold/ArtReview/";
        private const string Stem="RiftForagerRigStudy01.prefab";
        private const string ExpectedUnity="6000.3.21f1";
        private GameObject candidate;
        private string status="",lastFolder="";
        private Vector2 scroll;

        [MenuItem("Ninefold/Art/Review Rift Forager at Battle Camera Scale")]
        public static void Open()
        {
            var w=GetWindow<RiftForagerBattleCameraReview>("Forager Battle Camera");
            w.minSize=new Vector2(470,440);
        }

        private void OnEnable()
        {
            if(candidate==null)candidate=FindLatest();
        }
        private static GameObject FindLatest()
        {
            var paths=AssetDatabase.FindAssets("RiftForagerRigStudy01 t:Prefab",
                new[]{"Assets/Ninefold/ArtReview"})
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p=>p.StartsWith(ReviewRoot+"AshboundRiftForagerRig01_",
                    StringComparison.Ordinal) &&
                    p.EndsWith("/"+Stem,StringComparison.Ordinal))
                .OrderByDescending(p=>File.GetLastWriteTimeUtc(p))
                .ToArray();
            return paths.Length==0?null:
                AssetDatabase.LoadAssetAtPath<GameObject>(paths[0]);
        }

        private void OnGUI()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Label("RIFT FORAGER / BATTLE-CAMERA ART QUALITY GATE",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Compare the REAL skinned Rift Forager prefab at the game's 58° "+
                "orthographic battle angle, 390×844 and 320×568 portrait views, "+
                "plus close-up animation frames. PNGs and a measurement report "+
                "are exported locally to Library/NinefoldReviews. Review only; "+
                "nothing is added to Mission 11 or the approved model catalog.",
                MessageType.Info);

            using(new EditorGUILayout.HorizontalScope())
            {
                candidate=EditorGUILayout.ObjectField("Rig review prefab",candidate,
                    typeof(GameObject),false) as GameObject;
                if(GUILayout.Button("Find latest",GUILayout.Width(91)))
                    candidate=FindLatest();
            }

            if(candidate==null)
            {
                EditorGUILayout.HelpBox(
                    "First run Ninefold > Art > Build Rift Forager Skinned Benchmark "+
                    "and create its skinned ArtReview prefab. Then select that "+
                    "generated RiftForagerRigStudy01 prefab here.",
                    MessageType.Warning);
                if(GUILayout.Button("Open rig builder",GUILayout.Height(32)))
                    AshboundRigReview.Open();
            }
            EditorGUILayout.HelpBox(
                "Tactical frames reproduce the current battlefield camera's 58° "+
                "tilt and minimum orthographic half-height of 4 world units. "+
                "A wide mission map may frame the unit even SMALLER. "+
                "Close-up frames are for checking anatomy, weights and texture "+
                "artifacts, not a substitute for actual mobile battle readability.",
                MessageType.None);
            bool busy=EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating;
            using(new EditorGUI.DisabledScope(candidate==null || busy))
            {
                if(GUILayout.Button("CAPTURE PORTRAIT ART REVIEW",
                    GUILayout.Height(42)))
                {
                    try
                    {
                        lastFolder=Export(candidate);
                        status="Review images and exact mesh report exported to "+lastFolder;
                        Debug.Log("Rift Forager battle-camera capture: "+lastFolder);
                    }
                    catch(Exception ex)
                    {
                        status=ex.GetType().Name+": "+ex.Message;
                        Debug.LogException(ex);
                    }
                }
            }
            if(!string.IsNullOrEmpty(lastFolder) && Directory.Exists(lastFolder))
            {
                if(GUILayout.Button("Reveal exported PNGs and measurements",
                    GUILayout.Height(30)))
                    EditorUtility.RevealInFinder(Path.GetFullPath(lastFolder));
            }
            if(!string.IsNullOrEmpty(status))
                EditorGUILayout.HelpBox(status,MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "REJECTION CRITERIA: Incorrect Avarin face or living sensory "+
                "mantle, floating equipment, poor skin weighting, arm clipping, "+
                "plastic or flat materials, unreadable portrait silhouette, "+
                "unacceptable mobile draw calls, or deviation from the locked "+
                "World Five visual master. A successful capture is NOT "+
                "production approval or proof of native phone performance.",
                MessageType.Warning);
            EditorGUILayout.EndScrollView();
        }

        private static string Validate(GameObject prefab,
            out int rendererCount,out long triangles,out int vertices,
            out int distinctMaterials)
        {
            if(Application.unityVersion!=ExpectedUnity)
                throw new InvalidOperationException("Use pinned Unity "+ExpectedUnity);
            if(GraphicsSettings.defaultRenderPipeline==null)
                throw new InvalidOperationException("URP project foundation is missing.");
            if(prefab==null)throw new ArgumentNullException(nameof(prefab));
            string path=AssetDatabase.GetAssetPath(prefab);
            if(!path.StartsWith(ReviewRoot+"AshboundRiftForagerRig01_",
                StringComparison.Ordinal) ||
                !path.EndsWith("/"+Stem,StringComparison.Ordinal))
                throw new InvalidDataException("Only the actual fingerprinted Rift "+
                    "Forager review rig can be captured. No gameplay prefab accepted.");
            if(prefab.GetComponentsInChildren<MonoBehaviour>(true).Length>0 ||
                prefab.GetComponentsInChildren<Rigidbody>(true).Length>0 ||
                prefab.GetComponentsInChildren<Collider>(true).Length>0)
                throw new InvalidDataException("Visual candidate has behavior or "+
                    "physical components. Remove them before an art review.");
            var renderers=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if(renderers.Length==0 ||
                prefab.GetComponentsInChildren<Renderer>(true).Length!=renderers.Length)
                throw new InvalidDataException("Expected only real skinned "+
                    "source mesh renderers, not generic decorative primitives.");
            var connected=renderers.Where(renderer=>
                renderer.sharedMesh!=null &&
                renderer.sharedMesh.name==
                "Rift Forager continuous anatomical rest skin 01").ToArray();
            if(connected.Length!=1 || connected[0].sharedMesh.subMeshCount!=2)
                throw new InvalidDataException(
                    "Build the updated connected-anatomy Rift Forager first. "+
                    "The older disconnected body is not an acceptable "+
                    "reconstruction review candidate.");
            var mats=new HashSet<Material>();
            rendererCount=renderers.Length;triangles=0;vertices=0;
            foreach(var renderer in renderers)
            {
                var mesh=renderer.sharedMesh;
                if(mesh==null || renderer.bones.Length!=AshboundSkeleton.Count)
                    throw new InvalidDataException("Review candidate has invalid "+
                        "mesh or 19-bone binding.");
                vertices+=mesh.vertexCount;
                for(int i=0;i<mesh.subMeshCount;i++)
                    triangles+=(long)mesh.GetIndexCount(i)/3L;
                foreach(var mat in renderer.sharedMaterials)
                {
                    if(mat==null || mat.GetTexture("_BaseMap")==null ||
                        mat.GetTexture("_BumpMap")==null)
                        throw new InvalidDataException(
                            "Missing UV-linked albedo/normal-map material.");
                    mats.Add(mat);
                }
            }
            distinctMaterials=mats.Count;
            if(triangles<36000 || vertices<18000 || distinctMaterials!=8)
                throw new InvalidDataException("Source geometry or material "+
                    "count differs unexpectedly; do not silently approve.");
            return Path.GetDirectoryName(path).Replace('\\','/');
        }

        private static string Export(GameObject prefab)
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Stop Play mode and finish script imports before capture.");

            int renderers,vertices,materialCount;
            long triangles;
            string assetFolder=Validate(prefab,out renderers,out triangles,
                out vertices,out materialCount);
            var labels=new[]{"Idle","Advance","Attack","Hit","Afterecho","Defeat"};
            var clips=new Dictionary<string,AnimationClip>(StringComparer.Ordinal);
            foreach(string label in labels)
            {
                string path=assetFolder+"/RiftForager"+label+"RigStudy01.anim";
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(clip==null || clip.length<.1f)
                    throw new InvalidDataException("Rig clip missing: "+path);
                clips.Add(label,clip);
            }

            string export=Path.Combine("Library","NinefoldReviews","RiftForager",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff",
                    CultureInfo.InvariantCulture));
            Directory.CreateDirectory(export);
            Scene scene=default;
            Material terrain=null;
            var completed=new List<string>();
            try
            {
                scene=EditorSceneManager.NewPreviewScene();
                var actor=PrefabUtility.InstantiatePrefab(prefab,scene) as GameObject;
                if(actor==null)
                    throw new InvalidOperationException(
                        "Unable to instantiate the original skinned asset.");
                actor.name="Rift Forager · temporary visual review only";
                actor.transform.position=Vector3.zero;
                actor.transform.rotation=Quaternion.identity;
                foreach(var animator in actor.GetComponentsInChildren<Animator>(true))
                {
                    animator.applyRootMotion=false;
                    animator.enabled=false; // Clip.SampleAnimation owns the pose.
                }

                var shader=Shader.Find("Universal Render Pipeline/Lit");
                if(shader==null)throw new InvalidOperationException("URP Lit unavailable.");
                terrain=new Material(shader){name="Temporary neutral battlefield floor"};
                terrain.SetColor("_BaseColor",new Color(.20f,.21f,.23f,1));
                terrain.SetFloat("_Smoothness",.14f);
                var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);
                SceneManager.MoveGameObjectToScene(floor,scene);
                floor.name="Neutral scale floor (not World Five canon)";
                floor.transform.position=new Vector3(0,-.01f,0);
                UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
                floor.GetComponent<Renderer>().sharedMaterial=terrain;

                var lightGo=new GameObject("Neutral art-review key");
                SceneManager.MoveGameObjectToScene(lightGo,scene);
                var light=lightGo.AddComponent<Light>();
                light.type=LightType.Directional;
                light.intensity=1.35f;
                light.transform.rotation=Quaternion.Euler(49,-37,0);
                light.shadows=LightShadows.None;

                var cameraGo=new GameObject("Temporary portrait capture camera");
                SceneManager.MoveGameObjectToScene(cameraGo,scene);
                var camera=cameraGo.AddComponent<Camera>();
                camera.orthographic=true;camera.nearClipPlane=.1f;
                camera.farClipPlane=50;
                camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color(.105f,.115f,.14f);
                camera.allowHDR=false;
                camera.allowMSAA=false;

                // Exact game-side tactical tilt and minimum orthographic size
                // (PlaytestBattlefield.Layout). Both outputs are truthful
                // fixed-camera estimates, NOT a fabricated device screenshot.
                var views=new[]{
                    new View("front",180f),new View("three-quarter",140f),
                    new View("side",90f),new View("rear",0f)
                };
                foreach(string pose in new[]{"Idle","Attack","Afterecho"})
                {
                    clips[pose].SampleAnimation(actor,clips[pose].length*.5f);
                    foreach(var view in views)
                    {
                        string name="tactical-390x844-"+pose.ToLowerInvariant()+
                            "-"+view.Name+".png";
                        Capture(camera,390,844,58f,view.Yaw,4f,
                            new Vector3(0,.93f,0),Path.Combine(export,name));
                        completed.Add(name);
                    }
                }

                foreach(string pose in new[]{"Idle","Attack"})
                {
                    clips[pose].SampleAnimation(actor,clips[pose].length*.5f);
                    foreach(var view in new[]{views[0],views[1]})
                    {
                        string narrow="tactical-320x568-"+pose.ToLowerInvariant()+
                            "-"+view.Name+".png";
                        Capture(camera,320,568,58f,view.Yaw,4f,
                            new Vector3(0,.93f,0),Path.Combine(export,narrow));
                        completed.Add(narrow);
                        string detail="detail-390x844-"+pose.ToLowerInvariant()+
                            "-"+view.Name+".png";
                        Capture(camera,390,844,18f,view.Yaw,1.26f,
                            new Vector3(0,.95f,0),Path.Combine(export,detail));
                        completed.Add(detail);
                    }
                }

                // Neutral albedo/normal-map A/B of the exact same pose.
                clips["Idle"].SampleAnimation(actor,clips["Idle"].length*.5f);
                var overrides=new Dictionary<Renderer,MaterialPropertyBlock>();
                foreach(var renderer in actor.GetComponentsInChildren<Renderer>(true))
                {
                    if(!(renderer is SkinnedMeshRenderer))continue;
                    var block=new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    overrides.Add(renderer,block);
                    var flat=new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(flat);
                    flat.SetFloat("_BumpScale",0);
                    renderer.SetPropertyBlock(flat);
                }
                const string control="control-390x844-idle-no-normal-detail.png";
                Capture(camera,390,844,58f,180f,4f,
                    new Vector3(0,.93f,0),Path.Combine(export,control));
                completed.Add(control);
                foreach(var entry in overrides)entry.Key.SetPropertyBlock(entry.Value);

                // Contact sheets make comparison actionable without asking
                // reviewers to manually assemble twelve separate PNG files.
                var tactical=completed.Where(n=>
                    n.StartsWith("tactical-390x844-",StringComparison.Ordinal)).ToArray();
                if(tactical.Length!=12)
                    throw new InvalidDataException("Tactical review must have 12 poses/views.");
                const string tacticalSheet="contact-sheet-tactical-390x844.png";
                ContactSheet(export,tactical,4,3,390,844,tacticalSheet);
                completed.Add(tacticalSheet);

                var narrowFrames=completed.Where(n=>
                    n.StartsWith("tactical-320x568-",StringComparison.Ordinal)).ToArray();
                if(narrowFrames.Length!=4)
                    throw new InvalidDataException("Narrow review must have four poses/views.");
                const string narrowSheet="contact-sheet-narrow-320x568.png";
                ContactSheet(export,narrowFrames,2,2,320,568,narrowSheet);
                completed.Add(narrowSheet);

                var report=new StringBuilder();
                report.AppendLine("NINEFOLD: FRONTIERS — RIFT FORAGER REAL SKINNED MODEL REVIEW");
                report.AppendLine("Status: PROVISIONAL / NOT CANON APPROVED / NOT DEVICE CERTIFIED");
                report.AppendLine("Date UTC: "+DateTime.UtcNow.ToString("O"));
                report.AppendLine("Unity: "+Application.unityVersion);
                report.AppendLine("Source prefab: "+AssetDatabase.GetAssetPath(prefab));
                report.AppendLine("SkinnedMeshRenderers: "+renderers);
                report.AppendLine("Mesh vertices (all sections): "+vertices);
                report.AppendLine("Total mesh triangles: "+triangles);
                report.AppendLine("Distinct material assets: "+materialCount);
                report.AppendLine("Rig bone count: "+AshboundSkeleton.Count);
                report.AppendLine("Clips: "+string.Join(", ",labels));
                report.AppendLine("Tactical: 58° downward, orthographic half-height=4 world units");
                report.AppendLine("Camera estimate: actual missions may pull farther back.");
                report.AppendLine("Details: 18° downward, orthographic half-height=1.26.");
                report.AppendLine("No performance / GPU / FPS claims; review in iPhone 14.");
                report.AppendLine("No gameplay catalog, saved progression or scene modified.");
                report.AppendLine("Artifact count: "+completed.Count);
                report.AppendLine("");
                foreach(var name in completed)report.AppendLine(name);
                File.WriteAllText(Path.Combine(export,"review-report.txt"),
                    report.ToString());
                return export;
            }
            finally
            {
                if(scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
                if(terrain!=null) UnityEngine.Object.DestroyImmediate(terrain);
            }
        }

        private static void ContactSheet(string folder,string[] names,
            int columns,int rows,int tileWidth,int tileHeight,string output)
        {
            int w=columns*tileWidth,h=rows*tileHeight;
            var sheet=new Texture2D(w,h,TextureFormat.RGB24,false);
            try
            {
                var surface=new Color32[w*h];
                for(int i=0;i<names.Length;i++)
                {
                    var tile=new Texture2D(tileWidth,tileHeight,
                        TextureFormat.RGB24,false);
                    try
                    {
                        if(!tile.LoadImage(File.ReadAllBytes(
                            Path.Combine(folder,names[i])),false))
                            throw new InvalidDataException(
                                "Cannot decode actual render "+names[i]);
                        if(tile.width!=tileWidth || tile.height!=tileHeight)
                            throw new InvalidDataException(
                                "Unexpected render size for "+names[i]);
                        var pixels=tile.GetPixels32();
                        int column=i%columns;
                        int row=rows-1-i/columns; // top row = first pose
                        for(int y=0;y<tileHeight;y++)
                            Array.Copy(pixels,y*tileWidth,surface,
                                (row*tileHeight+y)*w+column*tileWidth,
                                tileWidth);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(tile); }
                }
                sheet.SetPixels32(surface);
                sheet.Apply(false,false);
                File.WriteAllBytes(Path.Combine(folder,output),sheet.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(sheet); }
        }

        private struct View
        {
            public readonly string Name;public readonly float Yaw;
            public View(string name,float yaw){Name=name;Yaw=yaw;}
        }

        private static void Capture(Camera camera,int width,int height,
            float tilt,float yaw,float orthoSize,Vector3 focus,string path)
        {
            var previous=RenderTexture.active;
            var previousTarget=camera.targetTexture;
            var rt=RenderTexture.GetTemporary(width,height,24,
                RenderTextureFormat.ARGB32);
            Texture2D image=null;
            try
            {
                rt.filterMode=FilterMode.Bilinear;
                camera.targetTexture=rt;
                camera.aspect=width/(float)height;
                camera.orthographicSize=orthoSize;
                camera.transform.rotation=Quaternion.Euler(tilt,yaw,0);
                camera.transform.position=
                    focus-camera.transform.forward*15f;
                camera.Render();
                RenderTexture.active=rt;
                image=new Texture2D(width,height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,width,height),0,0);
                image.Apply(false,false);
                File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=previousTarget;
                RenderTexture.active=previous;
                RenderTexture.ReleaseTemporary(rt);
                if(image!=null) UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
