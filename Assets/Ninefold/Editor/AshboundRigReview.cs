using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ninefold.Editor
{
    /// <summary>
    /// Builds an isolated real SkinnedMeshRenderer Rift Forager review candidate.
    /// All meshes, clips, controllers, prefabs and scenes remain in disposable ArtReview.
    /// Does not auto-register a battle visual or touch gameplay, playable creatures or canon.
    /// </summary>
    public sealed class AshboundRigReview : EditorWindow
    {
        private static readonly string[] Models={ "RiftForager" };
        private static readonly string[] Labels={ "Rift Forager" };
        private const string Output="Assets/Ninefold/ArtReview";
        private string error="", current="";
        private GameObject instance;
        private AnimationClip[] clips;
        private int model, selectedClip;
        private float normalized;
        private bool playing,ownsAnimationMode;
        private double lastTick;

        [MenuItem("Ninefold/Art/Build Rift Forager Skinned Benchmark")]
        public static void Open()
        {
            var w=GetWindow<AshboundRigReview>("Rift Forager Rig");
            w.minSize=new Vector2(430,475);
        }
        private void OnEnable()
        {
            lastTick=EditorApplication.timeSinceStartup;
            EditorApplication.update+=Tick;
            AssemblyReloadEvents.beforeAssemblyReload+=ReleasePreview;
            EditorApplication.playModeStateChanged+=PlayModeChanged;
        }
        private void OnDisable()
        {
            EditorApplication.update-=Tick;
            AssemblyReloadEvents.beforeAssemblyReload-=ReleasePreview;
            EditorApplication.playModeStateChanged-=PlayModeChanged;
            ReleasePreview();
        }
        private void PlayModeChanged(PlayModeStateChange state)
        {
            ReleasePreview();Repaint();
        }
        private static bool Busy=>EditorApplication.isCompiling ||
            EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode;
        private void Tick()
        {
            double now=EditorApplication.timeSinceStartup;
            if(!playing || Busy || instance==null || clips==null)
            { playing=false;lastTick=now;return; }
            double diff=now-lastTick;
            if(diff<1.0/24) return;
            lastTick=now;
            normalized+=(float)(Math.Min(diff,.1)/clips[selectedClip].length);
            if(normalized>=1)
            {
                if(selectedClip<=1)normalized%=1;
                else {normalized=1;playing=false;}
            }
            Sample();Repaint();
        }
        private void OnGUI()
        {
            GUILayout.Label("RIFT FORAGER / SKINNED BENCHMARK 01",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Skinned 19-joint Rift Forager source mesh with PBR study textures, " +
                "with authored Unity animation curves for Idle, Advance, Attack, Hit, " +
                "Afterecho and Defeat. REVIEW ONLY, not production-certified or mapped to battles.",
                MessageType.Info);
            using(new EditorGUI.DisabledScope(Busy))
            {
                int next=0;
                if(next!=model)
                {
                    ReleasePreview();model=next;error="";
                }
                if(GUILayout.Button("Create / open independently staged rigged 3D scene",GUILayout.Height(36)))
                {
                    try { Build(Models[model]);error=""; }
                    catch(Exception e) { ReleasePreview();error=e.Message;Debug.LogException(e); }
                }
                if(instance!=null && clips!=null)
                {
                    EditorGUI.BeginChangeCheck();
                    selectedClip=EditorGUILayout.Popup("Authored skeletal clip",selectedClip,
                        new[]{"Idle","Advance","Attack","Hit","Afterecho","Defeat"});
                    if(EditorGUI.EndChangeCheck())
                    {
                        normalized=0;playing=false;Sample();
                    }
                    EditorGUI.BeginChangeCheck();
                    normalized=EditorGUILayout.Slider("Time",normalized,0,1);
                    if(EditorGUI.EndChangeCheck()) { playing=false;Sample(); }
                    EditorGUILayout.BeginHorizontal();
                    if(GUILayout.Button(playing?"Pause":"Play"))
                    { if(normalized>=1)normalized=0;playing=!playing;lastTick=EditorApplication.timeSinceStartup;Sample(); }
                    if(GUILayout.Button("Rest / reset"))
                    { playing=false;normalized=0;Sample(); }
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Label("Saved assets: "+current,EditorStyles.wordWrappedMiniLabel);
                }
            }
            if(Busy)EditorGUILayout.HelpBox("Leave Play mode and allow imports to finish.",MessageType.None);
            if(error.Length>0)EditorGUILayout.HelpBox(error,MessageType.Error);
            EditorGUILayout.HelpBox(
                "Animation Preview drives the skinned scene instance, not combat. " +
                "To inspect joint weights and clipping, orbit the Unity Scene view. " +
                "This rigged review candidate is not a production sculpt. It still needs " +
                "contact IK, human art direction, texture paint, mobile LODs and reference " +
                "approval still require later work. Do not promote ArtReview into battle.",
                MessageType.Warning);
        }
        private void Sample()
        {
            if(instance==null||clips==null||Busy)return;
            if(!ownsAnimationMode)
            {
                if(AnimationMode.InAnimationMode())return;
                AnimationMode.StartAnimationMode();ownsAnimationMode=true;
            }
            AnimationMode.BeginSampling();
            try { AnimationMode.SampleAnimationClip(instance,clips[selectedClip],
                normalized*clips[selectedClip].length); }
            finally { AnimationMode.EndSampling(); }
            SceneView.RepaintAll();
        }
        private void ReleasePreview()
        {
            playing=false;instance=null;clips=null;current="";
            if(ownsAnimationMode)
            {
                if(AnimationMode.InAnimationMode())AnimationMode.StopAnimationMode();
                ownsAnimationMode=false;
            }
        }

        private void Build(string name)
        {
            ReleasePreview();
            if(Busy)throw new InvalidOperationException("Unity is importing, compiling or in Play mode.");
            if(Application.unityVersion!="6000.3.21f1")
                throw new InvalidOperationException("Pinned Unity 6000.3.21f1 is required.");
            if(GraphicsSettings.defaultRenderPipeline==null)
                throw new InvalidOperationException("Configure the existing URP foundation first.");
            if(name!="RiftForager")
                throw new ArgumentException("Unknown Ashbound first-contact model.");
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if(shader==null)throw new InvalidOperationException("URP Lit is missing.");
            string root="SourceArt/Ashbound/"+name+"/";
            string obj=root+name+".obj",mtl=root+name+".mtl",manifest=root+"manifest.json";
            foreach(string file in new[]{obj,mtl,manifest})
                if(!File.Exists(file))throw new FileNotFoundException("Missing Ashbound model source",file);
            var materials=GreatStriderSource.ReadMaterials(File.ReadLines(mtl),root);
            var source=GreatStriderSource.ReadMesh(File.ReadLines(obj),materials);
            if(source.SourceVertices<18000||source.Triangles<36000||materials.Count!=8)
                throw new InvalidDataException("Unexpected original Ashbound geometry revision.");

            string signature;
            using(var hash=SHA256.Create())
            {
                var bytes=File.ReadAllBytes(obj).Concat(File.ReadAllBytes(mtl))
                    .Concat(File.ReadAllBytes(manifest))
                    .Concat(File.ReadAllBytes("Assets/Ninefold/Editor/AshboundSkeleton.cs"))
                    .Concat(File.ReadAllBytes("Assets/Ninefold/Editor/AshboundRigReview.cs"))
                    .Concat(File.ReadAllBytes("Assets/Ninefold/Editor/AshboundReviewPbr.cs"))
                    .Concat(File.ReadAllBytes("Assets/Ninefold/Editor/RiftForagerAnatomyReconstruction.cs")).ToArray();
                signature=BitConverter.ToString(hash.ComputeHash(bytes))
                    .Replace("-","").Substring(0,12).ToLowerInvariant();
            }
            if(!AssetDatabase.IsValidFolder(Output))
                AssetDatabase.CreateFolder("Assets/Ninefold","ArtReview");
            string folder=Output+"/Ashbound"+name+"Rig01_"+signature;
            string path=folder+"/"+name+"RigReview01.unity";
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)!=null)
            {
                var old=EditorSceneManager.OpenScene(path);
                instance=old.GetRootGameObjects().FirstOrDefault(go=>go.name==name+" / skinned Ashbound study");
                if(instance==null)throw new InvalidDataException("Existing review scene missing model.");
                clips=LoadClips(folder);current=folder;
                Selection.activeGameObject=instance;
                Sample();return;
            }

            if(!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(Output,"Ashbound"+name+"Rig01_"+signature);
            bool completed=false;
            try
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var actor=new GameObject(name+" / skinned Ashbound study");
                var bones=CreateSkeleton(actor.transform);
                var materialMap=AshboundReviewPbr.Build(folder,shader,materials);
                int sectionId=0;
                foreach(var section in source.Sections)
                {
                    if(section.Vertices.Count!=section.SourceParts.Count)
                        throw new InvalidDataException("Missing anatomical part binding on source vertices.");
                    // Retain authentic source accessory/face/mantle geometry,
                    // but actually REMOVE original detached torso/limb/head
                    // tubes. The reconstructed body replaces their triangles.
                    var retainedTriangles=new List<int>();
                    for(int face=0;face<section.Indices.Count;face+=3)
                    {
                        int vertexIndex=section.Indices[face];
                        if(!RiftForagerAnatomyReconstruction.ReplacesOldBodyPart(
                            section.SourceParts[vertexIndex]))
                        {
                            retainedTriangles.Add(section.Indices[face]);
                            retainedTriangles.Add(section.Indices[face+1]);
                            retainedTriangles.Add(section.Indices[face+2]);
                        }
                    }
                    if(retainedTriangles.Count==0)continue;
                    // Compact the retained source sections. Invisible
                    // original anatomy vertices should not consume mobile
                    // skinning buffers, alter renderer bounds or linger as
                    // unreferenced geometry after the actual topology swap.
                    var compactMap=new Dictionary<int,int>();
                    var compactVertices=new List<Vector3>();
                    var compactNormals=new List<Vector3>();
                    var compactUvs=new List<Vector2>();
                    var compactParts=new List<string>();
                    var compactCoordinates=new List<float[]>();
                    var compactTriangles=new List<int>();
                    foreach(int oldIndex in retainedTriangles)
                    {
                        if(!compactMap.TryGetValue(oldIndex,out int newIndex))
                        {
                            newIndex=compactVertices.Count;
                            compactMap.Add(oldIndex,newIndex);
                            var xyz=section.Vertices[oldIndex];
                            var normal=section.Normals[oldIndex];
                            var uv=section.UVs[oldIndex];
                            compactVertices.Add(new Vector3(xyz[0],xyz[1],xyz[2]));
                            compactNormals.Add(new Vector3(normal[0],normal[1],normal[2]));
                            compactUvs.Add(uv[0]!=0 || uv[1]!=0 ?
                                new Vector2(uv[0],uv[1]) :
                                new Vector2(xyz[0]*2.2f+xyz[2]*.3f,
                                    xyz[1]*1.8f+xyz[2]*1.4f));
                            compactParts.Add(section.SourceParts[oldIndex]);
                            compactCoordinates.Add(xyz);
                        }
                        compactTriangles.Add(newIndex);
                    }
                    var node=new GameObject("Surface_"+(sectionId++).ToString("D2"));
                    node.transform.SetParent(actor.transform,false);
                    var mesh=new Mesh { name=section.Name,indexFormat=IndexFormat.UInt32 };
                    mesh.SetVertices(compactVertices);
                    mesh.SetNormals(compactNormals);
                    mesh.SetUVs(0,compactUvs);
                    mesh.SetTriangles(compactTriangles,0);
                    mesh.RecalculateTangents();
                    var weights=new BoneWeight[compactVertices.Count];
                    for(int i=0;i<weights.Length;i++)
                    {
                        var w=AshboundSkeleton.Skin(compactParts[i],
                            compactCoordinates[i]);
                        weights[i]=new BoneWeight {
                            boneIndex0=w.First,weight0=w.Primary,
                            boneIndex1=w.Second,weight1=w.Secondary
                        };
                    }
                    mesh.boneWeights=weights;
                    var bindposes=new Matrix4x4[bones.Length];
                    for(int k=0;k<bones.Length;k++)
                        bindposes[k]=bones[k].worldToLocalMatrix*node.transform.localToWorldMatrix;
                    mesh.bindposes=bindposes;
                    mesh.RecalculateBounds();
                    AssetDatabase.CreateAsset(mesh,folder+"/section_"+(sectionId-1).ToString("D2")+".asset");
                    var renderer=node.AddComponent<SkinnedMeshRenderer>();
                    renderer.sharedMesh=mesh;
                    renderer.bones=bones;renderer.rootBone=bones[0];
                    renderer.sharedMaterial=materialMap[section.Material];
                    renderer.updateWhenOffscreen=false;
                    var bound=mesh.bounds;bound.Expand(.5f);renderer.localBounds=bound;
                }

                // REAL topology-level reconstruction: one continuous,
                // soft-joined biped skin instead of overlapping limb tubes.
                // Mesh is generated only in disposable ArtReview and is never
                // assigned as an approved live battle catalog model.
                var rebuilt=RiftForagerAnatomyReconstruction.Generate();
                var anatomy=new GameObject("Reconstructed continuous Avarin anatomy");
                anatomy.transform.SetParent(actor.transform,false);
                var unified=new Mesh {
                    name="Rift Forager continuous anatomical rest skin 01",
                    indexFormat=IndexFormat.UInt32
                };
                unified.SetVertices(rebuilt.Vertices.Select(v=>
                    new Vector3(v[0],v[1],v[2])).ToList());
                unified.SetNormals(rebuilt.Normals.Select(v=>
                    new Vector3(v[0],v[1],v[2])).ToList());
                unified.SetUVs(0,rebuilt.UVs.Select(v=>
                    new Vector2(v[0],v[1])).ToList());
                unified.subMeshCount=2;
                unified.SetTriangles(rebuilt.GarmentIndices,0);
                unified.SetTriangles(rebuilt.SkinIndices,1);
                unified.RecalculateTangents();
                var organicWeights=new BoneWeight[rebuilt.Vertices.Count];
                for(int i=0;i<organicWeights.Length;i++)
                {
                    var w=AshboundSkeleton.SkinOrganic(rebuilt.Vertices[i]);
                    organicWeights[i]=new BoneWeight {
                        boneIndex0=w.A,weight0=w.WA,
                        boneIndex1=w.B,weight1=w.WB,
                        boneIndex2=w.C,weight2=w.WC,
                        boneIndex3=w.D,weight3=w.WD
                    };
                }
                unified.boneWeights=organicWeights;
                var organicBindposes=new Matrix4x4[bones.Length];
                for(int i=0;i<bones.Length;i++)
                    organicBindposes[i]=
                        bones[i].worldToLocalMatrix*anatomy.transform.localToWorldMatrix;
                unified.bindposes=organicBindposes;
                unified.RecalculateBounds();
                AssetDatabase.CreateAsset(unified,
                    folder+"/RiftForagerContinuousAnatomy01.asset");
                var organicRenderer=anatomy.AddComponent<SkinnedMeshRenderer>();
                organicRenderer.sharedMesh=unified;
                organicRenderer.bones=bones;
                organicRenderer.rootBone=bones[0];
                organicRenderer.sharedMaterials=new[]{
                    materialMap["garment"],materialMap["skin"]
                };
                organicRenderer.updateWhenOffscreen=false;
                var organicBounds=unified.bounds;organicBounds.Expand(.5f);
                organicRenderer.localBounds=organicBounds;

                var built=BuildClips(folder,name);
                var controller=AnimatorController.CreateAnimatorControllerAtPath(
                    folder+"/"+name+"Review01.controller");
                var machine=controller.layers[0].stateMachine;
                // The actual battlefield's visual driver uses Moving(bool)
                // rather than sending an Advance trigger. Without this,
                // the authored walking animation would never play in combat.
                controller.AddParameter("Moving",AnimatorControllerParameterType.Bool);
                AnimatorState idle=null,advance=null;
                for(int i=0;i<built.Length;i++)
                {
                    var type=(AshboundMotion.Clip)i;
                    string label=type.ToString();
                    controller.AddParameter(label,AnimatorControllerParameterType.Trigger);
                    var state=machine.AddState(label);
                    state.motion=built[i];state.writeDefaultValues=true;
                    if(i==0){idle=state;machine.defaultState=state;}
                    if(i==1)advance=state;
                    var transition=machine.AddAnyStateTransition(state);
                    transition.hasExitTime=false;
                    transition.duration=.09f;
                    transition.AddCondition(AnimatorConditionMode.If,0,label);
                    if(i>1 && type!=AshboundMotion.Clip.Defeat)
                    {
                        var back=state.AddTransition(idle);
                        back.hasExitTime=true;back.exitTime=1;
                        back.duration=.14f;
                    }
                }
                // Combat compatibility remains visual-only. Authoritative
                // movement positions come from Core; this blends locomotion
                // when Start/EndMovement are called by the battle renderer.
                if(idle==null || advance==null)
                    throw new InvalidDataException("Missing Idle/Advance rig states.");
                var startWalking=idle.AddTransition(advance);
                startWalking.hasExitTime=false;
                startWalking.duration=.12f;
                startWalking.AddCondition(AnimatorConditionMode.If,0,"Moving");
                var stopWalking=advance.AddTransition(idle);
                stopWalking.hasExitTime=false;
                stopWalking.duration=.12f;
                stopWalking.AddCondition(AnimatorConditionMode.IfNot,0,"Moving");

                var animator=actor.AddComponent<Animator>();
                animator.runtimeAnimatorController=controller;
                animator.applyRootMotion=false;
                if(PrefabUtility.SaveAsPrefabAsset(actor,folder+"/"+name+"RigStudy01.prefab")==null)
                    throw new IOException("Could not save the review-only rigged prefab.");

                var ground=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ground.name="Neutral review stage only";
                ground.transform.position=new Vector3(0,-.045f,0);
                ground.transform.localScale=new Vector3(.82f,.045f,.82f);
                UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
                ground.GetComponent<Renderer>().sharedMaterial=materialMap["dark"];
                RenderSettings.ambientMode=AmbientMode.Flat;
                RenderSettings.ambientLight=new Color(.36f,.38f,.42f);
                RenderSettings.skybox=null;
                var light=new GameObject("Neutral review key").AddComponent<Light>();
                light.type=LightType.Directional;light.intensity=1.3f;
                light.transform.rotation=Quaternion.Euler(40,-35,0);
                var camera=new GameObject("Review camera").AddComponent<Camera>();
                camera.transform.position=new Vector3(2.6f,2.1f,3.0f);
                camera.transform.LookAt(new Vector3(0,.92f,0));
                camera.fieldOfView=37;
                if(!EditorSceneManager.SaveScene(scene,path))
                    throw new IOException("Could not save the generated rig scene.");
                AssetDatabase.SaveAssets();
                instance=actor;clips=built;current=folder;normalized=0;selectedClip=0;
                Selection.activeGameObject=actor;
                completed=true;
                Debug.Log("Ashbound reconstruction review built: "+name+
                    " / one seamless organic body, "+rebuilt.Vertices.Count+
                    " reconstructed vertices, "+rebuilt.TriangleCount+
                    " body triangles, "+AshboundSkeleton.Count+
                    " bones and six existing animation clips. PROVISIONAL ART.");
                Sample();
            }
            finally
            {
                if(!completed)
                {
                    ReleasePreview();
                    AssetDatabase.DeleteAsset(folder);
                }
            }
        }

        private static Transform[] CreateSkeleton(Transform parent)
        {
            var built=new Transform[AshboundSkeleton.Count];
            for(int i=0;i<built.Length;i++)
            {
                var bone=AshboundSkeleton.Bones[i];
                var node=new GameObject(bone.Name).transform;
                var parentBone=bone.Parent<0?parent:built[bone.Parent];
                node.SetParent(parentBone,false);
                float x=bone.X,y=bone.Y,z=bone.Z;
                if(bone.Parent>=0)
                {
                    var previous=AshboundSkeleton.Bones[bone.Parent];
                    x-=previous.X;y-=previous.Y;z-=previous.Z;
                }
                node.localPosition=new Vector3(x,y,z);
                node.localRotation=Quaternion.identity;
                built[i]=node;
            }
            return built;
        }
        private static string BonePath(int index)
        {
            var names=new List<string>();
            for(int i=index;i>=0;i=AshboundSkeleton.Bones[i].Parent)
                names.Add(AshboundSkeleton.Bones[i].Name);
            names.Reverse();
            return string.Join("/",names);
        }
        private static AnimationClip[] BuildClips(string folder,string model)
        {
            var result=new AnimationClip[6];
            bool striker=model=="PartedStriker";
            for(int j=0;j<result.Length;j++)
            {
                var motion=(AshboundMotion.Clip)j;
                float duration=(float)AshboundMotion.Duration(motion,striker);
                var clip=new AnimationClip { name=model+motion+"RigStudy01",frameRate=30 };
                var settings=AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime=j==0||j==1;
                AnimationUtility.SetAnimationClipSettings(clip,settings);
                // Smooth piecewise-pose curves sample the exact deterministic
                // skeleton contract; explicit zero tracks reset unanimated bones.
                for(int b=0;b<AshboundSkeleton.Count;b++)
                {
                    string bonePath=BonePath(b);
                    for(int channel=0;channel<3;channel++)
                    {
                        var keys=new Keyframe[25];
                        for(int k=0;k<keys.Length;k++)
                        {
                            double t=k/(double)(keys.Length-1);
                            var pose=AshboundSkeleton.Sample(b,motion,t,striker);
                            float value=channel==0?pose.X:channel==1?pose.Y:pose.Z;
                            keys[k]=new Keyframe((float)t*duration,value,0,0);
                        }
                        var binding=EditorCurveBinding.FloatCurve(bonePath,typeof(Transform),
                            "localEulerAnglesRaw."+(channel==0?"x":channel==1?"y":"z"));
                        AnimationUtility.SetEditorCurve(clip,binding,new AnimationCurve(keys));
                    }
                }
                var translation=new Keyframe[25];
                for(int k=0;k<translation.Length;k++)
                {
                    double t=k/(double)(translation.Length-1);
                    float value=AshboundSkeleton.Bones[1].Y+
                        AshboundSkeleton.Sample(1,motion,t,striker).PositionY;
                    translation[k]=new Keyframe((float)t*duration,value,0,0);
                }
                AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(
                    BonePath(1),typeof(Transform),"m_LocalPosition.y"),
                    new AnimationCurve(translation));
                AssetDatabase.CreateAsset(clip,folder+"/"+model+motion+"RigStudy01.anim");
                result[j]=clip;
            }
            return result;
        }
        private static AnimationClip[] LoadClips(string folder)
        {
            var list=new AnimationClip[6];
            // Opening a previously generated scene loads exactly the clips
            // referenced by its own sibling prefab (name includes signature).
            string name="RiftForager";
            for(int i=0;i<6;i++)
            {
                string path=folder+"/"+name+(AshboundMotion.Clip)i+"RigStudy01.anim";
                list[i]=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(list[i]==null)throw new FileNotFoundException("Review rig clip missing",path);
            }
            return list;
        }
    }
}
