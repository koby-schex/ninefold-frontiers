using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ninefold.Editor
{
    // Independent art scenes only. No runtime unit registration or UI changes.
    public sealed class WorldFiveRosterReview : EditorWindow
    {
        private string unit = "Burdenbeast", status = "";
        [MenuItem("Ninefold/Art/Burdenbeast Review")]
        public static void Burdenbeast() { var w=GetWindow<WorldFiveRosterReview>("World Five roster"); w.unit="Burdenbeast"; }
        [MenuItem("Ninefold/Art/Riftwalker Review")]
        public static void Riftwalker() { var w=GetWindow<WorldFiveRosterReview>("World Five roster"); w.unit="Riftwalker"; }
        private void OnGUI()
        {
            EditorGUILayout.LabelField(unit + " · Proportion study 01", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Early source-model study. Flat material colors, proposed contours and equipment layout. No rig, animation, LODs, production textures or gameplay registration. Locked lore and visual masters control further development.", MessageType.Info);
            if (GUILayout.Button("Create a fresh review scene"))
            {
                try { status=Build(unit)?"New review scene created; previous copies preserved. Use Scene view to orbit and inspect.":"Scene creation cancelled."; }
                catch (Exception e) { status=e.Message; Debug.LogException(e); }
            }
            EditorGUILayout.HelpBox(status,MessageType.None);
        }
        private static bool Build(string unit)
        {
            if (Application.unityVersion != "6000.3.21f1") throw new InvalidOperationException("Use Unity 6000.3.21f1.");
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Stop Play mode and finish compilation first.");
            if (GraphicsSettings.defaultRenderPipeline == null) throw new InvalidOperationException("Configure the project's URP foundation first.");
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit is unavailable.");
            string source="SourceArt/"+unit+"/";
            var materials=GreatStriderSource.ReadMaterials(File.ReadLines(source+unit+".mtl"),source);
            var data=GreatStriderSource.ReadMesh(File.ReadLines(source+unit+".obj"),materials);
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            string hash;
            using (var sha=SHA256.Create())
                hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(File.ReadAllText(source+unit+".obj")+File.ReadAllText(source+unit+".mtl")+File.ReadAllText("Assets/Ninefold/Editor/WorldFiveRosterReview.cs")))).Replace("-","").Substring(0,12);
            string folder=AssetDatabase.GenerateUniqueAssetPath("Assets/Ninefold/ArtReview/"+unit+"01_"+hash);
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject(unit+" — Proportion study 01");
            var mapped=materials.ToDictionary(s=>s.Name,s=>
            {
                var m=new Material(shader) { name=s.Name };
                var c=new Color(s.Color[0],s.Color[1],s.Color[2]).gamma;c.a=s.Opacity;
                m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",1-Mathf.Pow(2/(s.Exponent+2),.25f));
                bool transparent=s.Opacity<1;
                m.SetFloat("_Surface",transparent?1:0);m.SetFloat("_Blend",0);m.SetFloat("_AlphaClip",0);
                m.SetFloat("_SrcBlend",(float)(transparent?BlendMode.SrcAlpha:BlendMode.One));
                m.SetFloat("_DstBlend",(float)(transparent?BlendMode.OneMinusSrcAlpha:BlendMode.Zero));
                m.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);m.SetFloat("_DstBlendAlpha",(float)(transparent?BlendMode.OneMinusSrcAlpha:BlendMode.Zero));
                m.SetFloat("_ZWrite",transparent?0:1);m.SetFloat("_Cull",(float)CullMode.Back);
                m.SetOverrideTag("RenderType",transparent?"Transparent":"Opaque");
                m.renderQueue=(int)(transparent?RenderQueue.Transparent:RenderQueue.Geometry);
                if (transparent) m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetShaderPassEnabled("ShadowCaster",!transparent);m.SetShaderPassEnabled("DepthOnly",!transparent);
                AssetDatabase.CreateAsset(m,folder+"/"+s.Name+".mat");return m;
            });
            int section=0;
            foreach (var part in data.Sections)
            {
                var mesh=new Mesh { name=part.Name,indexFormat=IndexFormat.UInt32 };
                mesh.SetVertices(part.Vertices.Select(v=>new Vector3(v[0],v[1],v[2])).ToList());
                mesh.SetNormals(part.Normals.Select(v=>new Vector3(v[0],v[1],v[2])).ToList());
                mesh.SetTriangles(part.Indices,0);mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh,folder+"/section-"+(section++)+".asset");
                var go=new GameObject(part.Name);go.transform.SetParent(root.transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mapped[part.Material];
            }
            if (PrefabUtility.SaveAsPrefabAsset(root,folder+"/"+unit+"Study01.prefab") == null) throw new IOException("Prefab save failed.");
            var light=new GameObject("Review key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;
            light.transform.rotation=Quaternion.Euler(45,-30,0);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.45f,.45f,.45f);
            var camera=new GameObject("Review camera").AddComponent<Camera>();camera.tag="MainCamera";
            var target=new Vector3(unit=="Burdenbeast"?.4f:.3f,1.1f,0);
            camera.transform.position=target+new Vector3(6,3,9).normalized*(unit=="Burdenbeast"?11:4.5f);
            camera.transform.LookAt(target);camera.nearClipPlane=.05f;camera.farClipPlane=100;
            camera.backgroundColor=new Color(.16f,.16f,.16f);camera.clearFlags=CameraClearFlags.SolidColor;
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene,folder+"/"+unit+"Review01.unity")) throw new IOException("Scene save failed.");
            Selection.activeGameObject=root;
            SceneView.lastActiveSceneView?.FrameSelected();
            return true;
        }
    }
}
