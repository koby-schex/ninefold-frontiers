using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ninefold.Editor
{
    /// <summary>
    /// Transient viewport of actual Rift Forager/Parted Striker vertex motion.
    /// Loads real source geometry, deforms cloned meshes only in memory, and never
    /// edits the source OBJ, Unity scene, prefab, combat animation or profile.
    /// </summary>
    public sealed class AshboundMotionReview : EditorWindow
    {
        private const string SourceRoot="SourceArt/Ashbound/";
        private readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        private readonly List<Section> sections=new List<Section>();
        private PreviewRenderUtility preview;
        private string selected="RiftForager", loaded="", error="";
        private AshboundMotion.Clip clip=AshboundMotion.Clip.Idle;
        private double normalizedTime, lastTick;
        private bool playing, loop=true, dirty=true;
        private float yaw=145, elevation=15, zoom=1;

        private sealed class Section
        {
            internal GreatStriderSource.Section Source;
            internal Mesh Mesh;
            internal Vector3[] Positions;
        }

        private static bool Busy =>
            EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating;

        [MenuItem("Ninefold/Art/Ashbound First Contact Motion Review")]
        public static void Open()
        {
            var w=GetWindow<AshboundMotionReview>("Ashbound Motion");
            w.minSize=new Vector2(460,640);
        }

        private void OnEnable()
        {
            lastTick=EditorApplication.timeSinceStartup;
            EditorApplication.update+=Tick;
            AssemblyReloadEvents.beforeAssemblyReload+=Release;
            EditorApplication.playModeStateChanged+=PlayModeChanged;
        }
        private void OnDisable()
        {
            EditorApplication.update-=Tick;
            AssemblyReloadEvents.beforeAssemblyReload-=Release;
            EditorApplication.playModeStateChanged-=PlayModeChanged;
            Release();
        }
        private void PlayModeChanged(PlayModeStateChange state)
        {
            Release(); Repaint();
        }
        private void Tick()
        {
            double now=EditorApplication.timeSinceStartup;
            if(!playing || preview==null || Busy)
            {
                playing=false; lastTick=now; return;
            }
            double elapsed=now-lastTick;
            if(elapsed<1.0/20) return;
            lastTick=now;
            double duration=AshboundMotion.Duration(clip,loaded=="PartedStriker");
            normalizedTime+=Math.Min(.10,elapsed)/duration;
            if(normalizedTime>=1)
            {
                if(loop && (clip==AshboundMotion.Clip.Idle || clip==AshboundMotion.Clip.Advance))
                    normalizedTime%=1;
                else { normalizedTime=1; playing=false; }
            }
            dirty=true; Repaint();
        }

        private void OnGUI()
        {
            GUILayout.Label("ASHBOUND / FIRST-CONTACT MOTION STUDY 01",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Genuine 3D mesh deformation: independent leg strides, arms, balance, " +
                "sensory-mantle reaction and strain. Six clips are procedural REVIEW STUDIES, " +
                "not a skinned production rig or gameplay-ready Animator.",
                MessageType.Info);
            using(new EditorGUI.DisabledScope(Busy))
            {
                EditorGUI.BeginChangeCheck();
                selected=EditorGUILayout.Popup("Enemy",selected=="PartedStriker"?1:0,
                    new[]{"Rift Forager","Parted Striker"})==1?"PartedStriker":"RiftForager";
                if(EditorGUI.EndChangeCheck() && loaded!=selected)
                {
                    Release(); normalizedTime=0; error="";
                }
                if(GUILayout.Button(preview==null?"Load genuine source mesh":"Reload source (discard transient motion preview)"))
                {
                    try { Load(); error=""; }
                    catch(Exception e) { Release(); error=e.Message; Debug.LogException(e); }
                }
                using(new EditorGUI.DisabledScope(preview==null))
                {
                    EditorGUI.BeginChangeCheck();
                    var next=(AshboundMotion.Clip)EditorGUILayout.EnumPopup("Clip",clip);
                    if(EditorGUI.EndChangeCheck())
                    {
                        clip=next; normalizedTime=0; playing=false; dirty=true;
                    }
                    EditorGUI.BeginChangeCheck();
                    normalizedTime=EditorGUILayout.Slider("Scrub",(float)normalizedTime,0,1);
                    if(EditorGUI.EndChangeCheck()) { playing=false; dirty=true; }
                    EditorGUILayout.BeginHorizontal();
                    if(GUILayout.Button(playing?"Pause":"Play"))
                    {
                        if(normalizedTime>=1) normalizedTime=0;
                        playing=!playing; lastTick=EditorApplication.timeSinceStartup; dirty=true;
                    }
                    if(GUILayout.Button("Reset pose"))
                    {
                        normalizedTime=0;playing=false;dirty=true;
                    }
                    EditorGUILayout.EndHorizontal();
                    loop=EditorGUILayout.Toggle("Loop idle and advance",loop);
                    yaw=EditorGUILayout.Slider("Orbit",yaw,-180,180);
                    elevation=EditorGUILayout.Slider("Camera elevation",elevation,-10,60);
                    zoom=EditorGUILayout.Slider("Zoom",zoom,.6f,1.8f);
                    EditorGUILayout.BeginHorizontal();
                    if(GUILayout.Button("Front")) { yaw=180;elevation=0;zoom=1; }
                    if(GUILayout.Button("Side")) { yaw=90;elevation=0;zoom=1; }
                    if(GUILayout.Button("Tactical")) { yaw=145;elevation=32;zoom=1; }
                    EditorGUILayout.EndHorizontal();
                }
            }
            if(Busy) EditorGUILayout.HelpBox("Stop Play mode and allow all compilation/import work to finish.",MessageType.None);
            if(error.Length>0) EditorGUILayout.HelpBox(error,MessageType.Error);
            EditorGUILayout.HelpBox(
                "These test poses do not create an Animator Controller or move battle units. " +
                "Nothing is written to ArtReview or runtime models. After art approval, " +
                "a proper deformation rig, authored clips, LODs and iPhone profiling are still required.",
                MessageType.Warning);

            var rect=GUILayoutUtility.GetRect(100,10000,175,10000,GUILayout.ExpandWidth(true),GUILayout.ExpandHeight(true));
            if(preview==null || Busy || Event.current.type!=EventType.Repaint) return;
            try
            {
                if(dirty) { ApplyPose(); dirty=false; }
                preview.BeginPreview(rect,GUIStyle.none);
                Texture result;
                try
                {
                    var rot=Quaternion.Euler(elevation,yaw,0);
                    var cam=preview.camera;
                    float aspect=Mathf.Max(.25f,rect.width/Mathf.Max(1,rect.height));
                    cam.orthographic=true;
                    cam.orthographicSize=Mathf.Max(1.13f, .78f/aspect)/zoom;
                    var focus=new Vector3(0,.92f,.045f);
                    cam.transform.SetPositionAndRotation(focus-rot*Vector3.forward*6,rot);
                    cam.clearFlags=CameraClearFlags.SolidColor;
                    cam.backgroundColor=new Color(.12f,.14f,.17f);
                    preview.Render(true,false);
                }
                finally { result=preview.EndPreview(); }
                GUI.DrawTexture(rect,result,ScaleMode.StretchToFill,false);
            }
            catch(Exception e) { Release(); error=e.Message; Debug.LogException(e); }
        }

        private T Own<T>(T asset) where T: UnityEngine.Object
        {
            asset.hideFlags=HideFlags.HideAndDontSave; owned.Add(asset);return asset;
        }

        private void Load()
        {
            Release();
            if(Busy) throw new InvalidOperationException("Unity must be idle before loading review.");
            if(Application.unityVersion!="6000.3.21f1")
                throw new InvalidOperationException("Review requires the pinned Unity 6000.3.21f1.");
            if(GraphicsSettings.defaultRenderPipeline==null)
                throw new InvalidOperationException("Configure URP in the existing Playtest Launcher first.");
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if(shader==null) throw new InvalidOperationException("URP Lit is not loaded.");
            if(selected!="RiftForager" && selected!="PartedStriker")
                throw new InvalidOperationException("Only Mission 11 first-contact enemies supported.");
            string source=SourceRoot+selected+"/";
            var surfaces=GreatStriderSource.ReadMaterials(File.ReadLines(source+selected+".mtl"),source);
            var geometry=GreatStriderSource.ReadMesh(File.ReadLines(source+selected+".obj"),surfaces);
            if(geometry.SourceVertices<18000 || geometry.Triangles<36000 || surfaces.Count!=8)
                throw new InvalidDataException("Unknown enemy mesh revision; validate source before motion review.");
            if(!geometry.Sections.SelectMany(s=>s.SourceParts).Any(p=>p=="Avarin_LivingCrescentMantle") ||
                !geometry.Sections.SelectMany(s=>s.SourceParts).Any(p=>p=="Avarin_TempleFace"))
                throw new InvalidDataException("Avarin sensory mantle or face was lost.");

            var materials=surfaces.ToDictionary(s=>s.Name,s=>CreateMaterial(shader,s),StringComparer.Ordinal);
            preview=new PreviewRenderUtility();
            preview.ambientColor=new Color(.40f,.42f,.45f);
            preview.camera.nearClipPlane=.05f;
            preview.camera.farClipPlane=40;
            preview.lights[0].intensity=1.3f;
            preview.lights[0].transform.rotation=Quaternion.Euler(40,-40,0);
            preview.lights[1].intensity=.48f;
            preview.lights[1].transform.rotation=Quaternion.Euler(20,140,0);
            var root=Own(new GameObject("Transient Ashbound motion viewport / not runtime art"));
            preview.AddSingleGO(root);
            foreach(var section in geometry.Sections)
            {
                if(section.SourceParts.Count!=section.Vertices.Count)
                    throw new InvalidDataException("Source part names missing at remapped vertices.");
                var mesh=Own(new Mesh { name=section.Name,indexFormat=IndexFormat.UInt32 });
                mesh.MarkDynamic();
                var points=section.Vertices.Select(v=>new Vector3(v[0],v[1],v[2])).ToArray();
                mesh.vertices=points;
                mesh.SetNormals(section.Normals.Select(v=>new Vector3(v[0],v[1],v[2])).ToList());
                mesh.SetUVs(0,section.UVs.Select(v=>new Vector2(v[0],v[1])).ToList());
                mesh.SetTriangles(section.Indices,0);
                mesh.RecalculateBounds();
                var go=new GameObject(section.Name) { hideFlags=HideFlags.HideAndDontSave };
                go.transform.SetParent(root.transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial=materials[section.Material];
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                renderer.receiveShadows=false;
                sections.Add(new Section { Source=section,Mesh=mesh,Positions=points });
            }
            loaded=selected;normalizedTime=0;playing=false;dirty=true;
        }

        private Material CreateMaterial(Shader shader,GreatStriderSource.Surface surface)
        {
            var material=Own(new Material(shader) { name=surface.Name });
            var color=new Color(surface.Color[0],surface.Color[1],surface.Color[2]).gamma;
            color.a=surface.Opacity;
            material.SetColor("_BaseColor",color);
            material.SetFloat("_Metallic",0);
            material.SetFloat("_Smoothness",1-Mathf.Pow(2/(surface.Exponent+2),.25f));
            return material;
        }

        private void ApplyPose()
        {
            var frame=AshboundMotion.Sample(clip,normalizedTime);
            bool striker=loaded=="PartedStriker";
            foreach(var section in sections)
            {
                for(int i=0;i<section.Positions.Length;i++)
                {
                    AshboundMotion.Vertex(section.Source.SourceParts[i],
                        section.Source.Vertices[i],frame,striker,
                        out float x,out float y,out float z);
                    section.Positions[i]=new Vector3(x,y,z);
                }
                section.Mesh.vertices=section.Positions;
                section.Mesh.RecalculateNormals();
                section.Mesh.RecalculateBounds();
            }
        }

        private void Release()
        {
            playing=false;loaded="";
            if(preview!=null) { preview.Cleanup();preview=null; }
            for(int i=owned.Count-1;i>=0;i--)
                if(owned[i]!=null) DestroyImmediate(owned[i]);
            owned.Clear();sections.Clear();dirty=true;
        }
    }
}
