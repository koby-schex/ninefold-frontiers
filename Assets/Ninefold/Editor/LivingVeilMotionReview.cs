using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ninefold.Editor
{
    /// <summary>Transient, editor-only motion viewport. Never edits asset meshes or the open scene.</summary>
    public sealed class LivingVeilMotionReview : EditorWindow
    {
        private const string Source = "SourceArt/LivingVeil/";
        private PreviewRenderUtility preview;
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private readonly List<Section> sections = new List<Section>();
        private LivingVeilMotion.Clip clip = LivingVeilMotion.Clip.RestingIdle;
        private float time, yaw = 145, pitch = 15, zoom = 1;
        private bool playing, loop, dirty = true;
        private double lastTick;
        private string error = "";
        private sealed class Section
        {
            public GreatStriderSource.Section Rest, Shelter;
            public Mesh Mesh;
            public Vector3[] Positions;
            public bool[] Organism, Membrane;
        }
        private static bool Busy => EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("Ninefold/Art/Living Veil Motion Review")]
        public static void Open()
        {
            var window = GetWindow<LivingVeilMotionReview>("Olyra Motion 03");
            window.minSize = new Vector2(420, 680);
        }
        private void OnEnable()
        {
            lastTick = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            AssemblyReloadEvents.beforeAssemblyReload += Release;
            EditorApplication.playModeStateChanged += PlayModeChanged;
        }
        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            AssemblyReloadEvents.beforeAssemblyReload -= Release;
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            Release();
        }
        private void PlayModeChanged(PlayModeStateChange state) { Release(); Repaint(); }
        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (!playing || preview == null || Busy) { playing = false; lastTick = now; return; }
            double elapsed = now-lastTick;
            if (elapsed < 1.0/24) return;
            lastTick = now;
            time += (float)(Math.Min(elapsed, .10)/LivingVeilMotion.Duration(clip));
            if (time >= 1)
            {
                bool idle = clip == LivingVeilMotion.Clip.RestingIdle || clip == LivingVeilMotion.Clip.ProtectiveIdle;
                if (loop && idle) time %= 1;
                else { time = 1; playing = false; }
            }
            dirty = true; Repaint();
        }
        private void OnGUI()
        {
            GUILayout.Label("Living Veil / Olyra · Motion study 03", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Editor motion study: fixed Avarin, gentle idle, rise / unfold / shelter / return. Provisional motion timing; no gameplay or production rig.", MessageType.Info);
            using (new EditorGUI.DisabledScope(Busy))
            {
                if (GUILayout.Button(preview == null ? "Load motion study" : "Reload source / reset preview"))
                {
                    try { Load(); error = ""; }
                    catch (Exception e) { Release(); error = e.Message; Debug.LogException(e); }
                }
                using (new EditorGUI.DisabledScope(preview == null))
                {
                    EditorGUI.BeginChangeCheck();
                    var next = (LivingVeilMotion.Clip)EditorGUILayout.EnumPopup("Motion", clip);
                    if (EditorGUI.EndChangeCheck()) { clip = next; time = 0; playing = false; dirty = true; }
                    EditorGUI.BeginChangeCheck();
                    time = EditorGUILayout.Slider("Scrub", time, 0, 1);
                    if (EditorGUI.EndChangeCheck()) { playing = false; dirty = true; }
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(playing ? "Pause" : "Play"))
                    { if (time >= 1) time = 0; playing = !playing; lastTick = EditorApplication.timeSinceStartup; dirty = true; }
                    if (GUILayout.Button("Reset")) { time = 0; playing = false; dirty = true; }
                    EditorGUILayout.EndHorizontal();
                    loop = EditorGUILayout.Toggle("Loop idle clips", loop);
                    yaw = EditorGUILayout.Slider("Orbit", yaw, -180, 180);
                    pitch = EditorGUILayout.Slider("Elevation", pitch, -10, 70);
                    zoom = EditorGUILayout.Slider("Zoom", zoom, .7f, 1.4f);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Front")) { yaw = 180; pitch = 0; zoom = 1; }
                    if (GUILayout.Button("Side")) { yaw = 90; pitch = 0; zoom = 1; }
                    if (GUILayout.Button("Tactical")) { yaw = 140; pitch = 45; zoom = 1; }
                    EditorGUILayout.EndHorizontal();
                }
            }
            if (Busy) EditorGUILayout.HelpBox("Stop Play mode and wait for import/compilation to finish.", MessageType.None);
            if (error.Length > 0) EditorGUILayout.HelpBox(error, MessageType.Error);
            var rect = GUILayoutUtility.GetRect(100, 10000, 180, 10000, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (preview == null || Busy || Event.current.type != EventType.Repaint) return;
            try
            {
                if (dirty) { Apply(); dirty = false; }
                preview.BeginPreview(rect, GUIStyle.none);
                Texture result;
                try
                {
                    var rotation = Quaternion.Euler(pitch, yaw, 0);
                    var camera = preview.camera;
                    camera.orthographic = true;
                    camera.orthographicSize = Mathf.Max(3.1f, 4.2f/Mathf.Max(.2f, rect.width/rect.height))/zoom;
                    camera.transform.SetPositionAndRotation(new Vector3(.9f, 2.4f, .15f)-rotation*Vector3.forward*18, rotation);
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = new Color(.14f,.14f,.14f);
                    preview.Render(true, false);
                }
                finally { result = preview.EndPreview(); }
                GUI.DrawTexture(rect, result, ScaleMode.StretchToFill, false);
            }
            catch (Exception e) { Release(); error = e.Message; Debug.LogException(e); }
        }
        private T Own<T>(T value) where T : UnityEngine.Object
        { value.hideFlags = HideFlags.HideAndDontSave; owned.Add(value); return value; }
        private void Load()
        {
            Release();
            if (Application.unityVersion != "6000.3.21f1") throw new InvalidOperationException("Use Unity 6000.3.21f1.");
            if (GraphicsSettings.defaultRenderPipeline == null) throw new InvalidOperationException("Configure the existing URP foundation first.");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader unavailable.");
            var surfaces = GreatStriderSource.ReadMaterials(File.ReadLines(Source+"LivingVeil.mtl"), Source);
            var rest = GreatStriderSource.ReadMesh(File.ReadLines(Source+"LivingVeil.obj"), surfaces);
            var shelter = GreatStriderSource.ReadMesh(File.ReadLines(Source+"LivingVeilProtective.obj"), surfaces);
            if (rest.SourceVertices != 47136 || rest.Triangles != 93768 || shelter.Sections.Count != rest.Sections.Count)
                throw new InvalidDataException("Unexpected source revision; review motion compatibility first.");
            var materials = surfaces.ToDictionary(s => s.Name, s => MakeMaterial(shader, s));
            preview = new PreviewRenderUtility();
            preview.ambientColor = new Color(.30f,.30f,.30f);
            preview.camera.nearClipPlane = .1f; preview.camera.farClipPlane = 80;
            preview.lights[0].intensity = 1.2f; preview.lights[0].color = Color.white;
            preview.lights[0].transform.rotation = Quaternion.Euler(45,-35,0);
            preview.lights[1].intensity = .6f; preview.lights[1].color = Color.white;
            preview.lights[1].transform.rotation = Quaternion.Euler(25,140,0);
            var root = Own(new GameObject("Olyra transient motion study")); preview.AddSingleGO(root);
            for (int i=0; i<rest.Sections.Count; i++)
            {
                var a=rest.Sections[i]; var b=shelter.Sections[i];
                if (a.Name != b.Name || a.Vertices.Count != b.Vertices.Count || !a.Indices.SequenceEqual(b.Indices) ||
                    !a.SourceParts.SequenceEqual(b.SourceParts) || a.SourceParts.Count != a.Vertices.Count)
                    throw new InvalidDataException("Pose topology mismatch: " + a.Name);
                var mesh=Own(new Mesh { name=a.Name, indexFormat=IndexFormat.UInt32 }); mesh.MarkDynamic();
                var points=a.Vertices.Select(v=>new Vector3(v[0],v[1],v[2])).ToArray();
                mesh.vertices=points; mesh.SetUVs(0,a.UVs.Select(v=>new Vector2(v[0],v[1])).ToList());
                mesh.SetTriangles(a.Indices,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                var go=new GameObject(a.Name) { hideFlags=HideFlags.HideAndDontSave }; go.transform.SetParent(root.transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=materials[a.Material];
                renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
                sections.Add(new Section { Rest=a,Shelter=b,Mesh=mesh,Positions=points,
                    Organism=a.SourceParts.Select(n=>n.StartsWith("Olyra_",StringComparison.Ordinal)).ToArray(),
                    Membrane=a.SourceParts.Select(LivingVeilMotion.IsMembrane).ToArray() });
            }
            time=0; dirty=true;
        }
        private Material MakeMaterial(Shader shader, GreatStriderSource.Surface surface)
        {
            var m=Own(new Material(shader) { name=surface.Name });
            var color=new Color(surface.Color[0],surface.Color[1],surface.Color[2]).gamma;
            color.a=surface.Texture == null ? surface.Opacity : 1;
            m.SetColor("_BaseColor",color); m.SetFloat("_Metallic",0);
            m.SetFloat("_Smoothness",1-Mathf.Pow(2/(surface.Exponent+2),.25f));
            if (surface.Texture != null)
            {
                var texture=Own(new Texture2D(2,2,TextureFormat.RGBA32,true,false));
                if (!texture.LoadImage(File.ReadAllBytes(GreatStriderSource.TexturePath(Source,surface.Texture))))
                    throw new InvalidDataException("Membrane texture could not be loaded.");
                texture.wrapMode=TextureWrapMode.Clamp; texture.filterMode=FilterMode.Trilinear;
                m.SetTexture("_BaseMap",texture);
            }
            bool transparent=surface.Opacity<1;
            m.SetFloat("_Surface",transparent?1:0); m.SetFloat("_Blend",0); m.SetFloat("_AlphaClip",0);
            m.SetFloat("_BlendModePreserveSpecular",0);
            m.SetFloat("_SrcBlend",(float)(transparent?BlendMode.SrcAlpha:BlendMode.One));
            m.SetFloat("_DstBlend",(float)(transparent?BlendMode.OneMinusSrcAlpha:BlendMode.Zero));
            m.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha",(float)(transparent?BlendMode.OneMinusSrcAlpha:BlendMode.Zero));
            m.SetFloat("_ZWrite",transparent?0:1); m.SetFloat("_Cull",(float)CullMode.Back);
            m.SetOverrideTag("RenderType",transparent?"Transparent":"Opaque");
            m.renderQueue=(int)(transparent?RenderQueue.Transparent:RenderQueue.Geometry);
            if (transparent) m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetShaderPassEnabled("ShadowCaster",false); m.SetShaderPassEnabled("DepthOnly",!transparent);
            return m;
        }
        private void Apply()
        {
            var frame=LivingVeilMotion.Sample(clip,time);
            foreach (var s in sections)
            {
                for (int i=0;i<s.Positions.Length;i++)
                {
                    LivingVeilMotion.Vertex(s.Organism[i],s.Membrane[i],s.Rest.Vertices[i],s.Shelter.Vertices[i],frame,out float x,out float y,out float z);
                    s.Positions[i]=new Vector3(x,y,z);
                }
                s.Mesh.vertices=s.Positions; s.Mesh.RecalculateNormals(); s.Mesh.RecalculateBounds();
            }
        }
        private void Release()
        {
            playing=false;
            if (preview != null) { preview.Cleanup(); preview=null; }
            for (int i=owned.Count-1;i>=0;i--) if (owned[i] != null) DestroyImmediate(owned[i]);
            owned.Clear(); sections.Clear(); dirty=true;
        }
    }
}
