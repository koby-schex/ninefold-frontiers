using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ninefold.Core.Content;
using UnityEngine;
using UnityEngine.Rendering;
using S=Ninefold.Core.Content.PlayableBondSource;
using R=Ninefold.Core.Content.PlayableBondRig;
using N=System.Numerics.Vector3;
using Object=UnityEngine.Object;

namespace Ninefold.Presentation
{
    /// <summary>
    /// Live, render-only candidates for the four existing World Five standard
    /// slots, using the same source geometry as the separate art reviews.
    /// One immutable mesh/material cache per battlefield; every token has
    /// independent joints. No components that own simulation or root motion.
    /// </summary>
    internal sealed class PlayableBondBattleArt : IDisposable
    {
        private const string ResourceRoot="WorldFiveBondModels/";
        private readonly Material template;
        private readonly Dictionary<R.Bond,Asset> assets=new Dictionary<R.Bond,Asset>();
        private readonly HashSet<R.Bond> unavailable=new HashSet<R.Bond>();
        private readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>(StringComparer.Ordinal);
        private readonly List<Mesh> meshes=new List<Mesh>();
        private readonly List<Material> materials=new List<Material>();
        private sealed class Asset
        {
            internal R.Plan Plan;
            internal N Min,Max;
            internal readonly List<Mesh> Meshes=new List<Mesh>();
            internal readonly List<Material> Materials=new List<Material>();
        }
        internal sealed class Instance
        {
            private readonly R.Plan plan;
            private readonly Transform[] bones;
            private readonly SkinnedMeshRenderer[] morphs;
            internal Instance(R.Plan plan,Transform[] bones,SkinnedMeshRenderer[] morphs)
            {this.plan=plan;this.bones=bones;this.morphs=morphs;}
            internal void Pose(float cycle,float motion,float attack,float hit,float defeat,float idle,
                float protective=0)
            {
                for(int i=0;i<bones.Length;i++)
                {
                    if(plan.Coverage[i]==0)continue;
                    var p=R.Pose(plan,i,cycle,motion,attack,hit,defeat,idle);
                    bones[i].localPosition=Vec(plan.Pivots[i]+p.Translation);
                    bones[i].localRotation=Quaternion.Euler(Vec(p.Euler));
                }
                foreach(var morph in morphs)morph.SetBlendShapeWeight(0,Mathf.Clamp01(protective)*100f);
            }
            internal void Reset()=>Pose(0,0,0,0,0,0);
        }

        internal PlayableBondBattleArt(Material materialTemplate){template=materialTemplate;}
        private static Vector3 Vec(N p)=>new Vector3(p.X,p.Y,p.Z);
        private static Vector3 Vec(float[] p)=>new Vector3(p[0],p[1],p[2]);
        private static TextAsset Load(string path)=>Resources.Load<TextAsset>(ResourceRoot+path);
        private static string[] Lines(string path)
        {
            var asset=Load(path);
            if(asset==null)throw new FileNotFoundException("Missing bundled bond source "+path);
            try{return asset.text.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries);}
            finally{Resources.UnloadAsset(asset);}
        }
        private static bool Exists(string path)
        {
            var asset=Load(path);
            if(asset==null)return false;
            Resources.UnloadAsset(asset);return true;
        }

        private Texture2D Texture(string path,bool linear,bool normal=false)
        {
            string key=path+"/"+(normal?"normal":linear?"linear":"color");
            if(textures.TryGetValue(key,out var found))return found;
            var source=Load(path);
            if(source==null)return null;
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,true,linear)
            {name=path,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};
            try
            {
                if(!texture.LoadImage(source.bytes,false))
                    throw new InvalidDataException("Invalid bundled PNG "+path);
                if(normal)
                {
                    // URP accepts normal maps in RG/AG form. Preserve the
                    // authored tangent X in A and Y in G, independently of
                    // platform texture importer or compression settings.
                    var pixels=texture.GetPixels32();
                    for(int i=0;i<pixels.Length;i++)
                        pixels[i]=new Color32(255,pixels[i].g,255,pixels[i].r);
                    texture.SetPixels32(pixels);
                }
                texture.Apply(true,true);
                textures.Add(key,texture);return texture;
            }
            catch{Object.Destroy(texture);throw;}
            finally{Resources.UnloadAsset(source);}
        }
        private static void Float(Material m,string name,float value)
        {if(m.HasProperty(name))m.SetFloat(name,value);}
        private Material CreateMaterial(string folder,S.Surface surface)
        {
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            var material=shader!=null?new Material(shader):new Material(template);
            materials.Add(material);material.name="Bond / "+surface.Name;
            bool transparent=surface.Opacity<1;
            // Olyra alpha is already in the authored RGBA source tile.
            // Applying its MTL classification again would double-fade it.
            float opacity=surface.Name=="olyra_membrane"?1:surface.Opacity;
            material.color=new Color(surface.Color[0],surface.Color[1],surface.Color[2],opacity);
            Float(material,"_Metallic",0);
            Float(material,"_Smoothness",Mathf.Clamp(Mathf.Sqrt(surface.Exponent/100f)*.58f,.09f,.7f));
            if(surface.Texture!=null)
            {
                var diffuse=Texture(folder+surface.Texture,false);
                if(diffuse==null)throw new InvalidDataException("Missing albedo "+surface.Texture);
                material.mainTexture=diffuse;
            }
            var bump=Texture(folder+"Textures/"+surface.Name+"_Normal.png",true,true);
            if(bump!=null&&material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap",bump);
                Float(material,"_BumpScale",.82f);material.EnableKeyword("_NORMALMAP");
            }
            var finish=Texture(folder+"Textures/"+surface.Name+"_Finish.png",true);
            if(finish!=null&&material.HasProperty("_MetallicGlossMap"))
            {
                material.SetTexture("_MetallicGlossMap",finish);
                Float(material,"_Smoothness",1);Float(material,"_Metallic",1);
                Float(material,"_SmoothnessTextureChannel",0);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            // Each transparent closed section remains separate for sorting.
            // Suppress opaque casting and alpha clipping; preserve source color.
            Float(material,"_Surface",transparent?1:0);
            Float(material,"_Blend",0);Float(material,"_AlphaClip",0);
            Float(material,"_BlendModePreserveSpecular",0);
            Float(material,"_SrcBlend",(float)(transparent?BlendMode.SrcAlpha:BlendMode.One));
            Float(material,"_DstBlend",(float)(transparent?BlendMode.OneMinusSrcAlpha:BlendMode.Zero));
            Float(material,"_SrcBlendAlpha",(float)BlendMode.One);
            Float(material,"_DstBlendAlpha",(float)(transparent?BlendMode.OneMinusSrcAlpha:BlendMode.Zero));
            Float(material,"_ZWrite",transparent?0:1);
            Float(material,"_Cull",(float)CullMode.Back);
            material.SetOverrideTag("RenderType",transparent?"Transparent":"Opaque");
            if(transparent)material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            else material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue=(int)(transparent?RenderQueue.Transparent:RenderQueue.Geometry);
            material.SetShaderPassEnabled("ShadowCaster",false);
            return material;
        }

        private Asset Get(R.Bond kind)
        {
            if(assets.TryGetValue(kind,out var cached))return cached;
            string folder=kind+"/";
            var surfaces=S.ReadMaterials(Lines(folder+kind+".mtl"),p=>Exists(folder+p));
            var model=S.ReadMesh(Lines(folder+kind+".obj"),surfaces);
            var plan=R.Build(kind,model);
            var asset=new Asset{Plan=plan,Min=plan.Min,Max=plan.Max};
            S.Model protective=null;
            if(kind==R.Bond.LivingVeil)
            {
                protective=S.ReadMesh(Lines(folder+"LivingVeilProtective.obj"),surfaces);
                S.ValidateCorrespondence(model,protective);
            }
            var palette=surfaces.ToDictionary(p=>p.Name,p=>CreateMaterial(folder,p),StringComparer.Ordinal);
            var bindposes=plan.Pivots.Select(p=>Matrix4x4.Translate(-Vec(p))).ToArray();
            foreach(var section in model.Sections)
            {
                int count=section.Vertices.Count;
                var vertices=new List<Vector3>(count);var normals=new List<Vector3>(count);
                var uv=new List<Vector2>(count);var weights=new BoneWeight[count];
                bool morph=protective!=null&&section.SourceParts.Any(R.IsOlyra);
                var delta=morph?new Vector3[count]:null;
                var normalDelta=morph?new Vector3[count]:null;
                for(int i=0;i<count;i++)
                {
                    var p=R.V(section.Vertices[i]);vertices.Add(Vec(p));
                    var normal=Vec(section.Normals[i]);normals.Add(normal);
                    uv.Add(new Vector2(section.UVs[i][0],section.UVs[i][1]));
                    var w=R.Skin(plan,section.SourceParts[i],p);
                    weights[i]=new BoneWeight{boneIndex0=w.A,boneIndex1=w.B,
                        weight0=w.W,weight1=1f-w.W};
                    if(morph&&R.IsOlyra(section.SourceParts[i]))
                    {
                        int source=section.SourceIndices[i];
                        var end=R.ProtectiveVertex(section.SourceParts[i],p,R.V(protective.RawVertices[source]));
                        delta[i]=Vec(end-p);normalDelta[i]=Vec(protective.RawNormals[source])-normal;
                        asset.Min=N.Min(asset.Min,end);asset.Max=N.Max(asset.Max,end);
                    }
                }
                var mesh=new Mesh{name=kind+" / "+section.Name,
                    indexFormat=count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
                meshes.Add(mesh);
                mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);
                mesh.SetTriangles(section.Indices,0);
                mesh.bindposes=bindposes;mesh.boneWeights=weights;
                mesh.RecalculateTangents();mesh.RecalculateBounds();
                if(morph)mesh.AddBlendShapeFrame("Olyra authored protective form",100f,delta,normalDelta,null);
                // Upload once. Instance-specific skinning changes only bones
                // and blend weights; no per-frame mesh cloning or CPU morph.
                mesh.UploadMeshData(true);
                asset.Meshes.Add(mesh);asset.Materials.Add(palette[section.Material]);
            }
            assets.Add(kind,asset);
            return asset;
        }

        internal bool TryBuild(R.Bond kind,Transform parent,float tokenHeight,
            out GameObject root,out Instance instance)
        {
            root=null;instance=null;
            if(unavailable.Contains(kind))return false;
            try
            {
                var asset=Get(kind);
                root=new GameObject(kind+" · playable bond visual candidate");
                root.transform.SetParent(parent,false);
                var fit=new GameObject("Normalized complete companion + Avarin").transform;
                fit.SetParent(root.transform,false);
                float factor=R.Fit(asset.Min,asset.Max,tokenHeight,1.42f);
                N center=(asset.Min+asset.Max)*.5f;
                fit.localScale=Vector3.one*factor;
                fit.localPosition=new Vector3(-center.X*factor,-tokenHeight*.5f-asset.Min.Y*factor,-center.Z*factor);
                var bones=new Transform[asset.Plan.Pivots.Length];
                for(int i=0;i<bones.Length;i++)
                {
                    bones[i]=new GameObject("Bond joint "+i).transform;
                    bones[i].SetParent(fit,false);
                    bones[i].localPosition=Vec(asset.Plan.Pivots[i]);
                }
                var morphs=new List<SkinnedMeshRenderer>();
                var bounds=new Bounds(Vec(center),Vec(asset.Max-asset.Min));
                bounds.Expand(Vec(asset.Max-asset.Min)*.22f);
                for(int i=0;i<asset.Meshes.Count;i++)
                {
                    var child=new GameObject(asset.Meshes[i].name);child.transform.SetParent(fit,false);
                    var skin=child.AddComponent<SkinnedMeshRenderer>();
                    skin.sharedMesh=asset.Meshes[i];skin.sharedMaterial=asset.Materials[i];
                    skin.bones=bones;skin.rootBone=fit;skin.quality=SkinQuality.Bone2;
                    skin.localBounds=bounds;skin.updateWhenOffscreen=false;
                    skin.shadowCastingMode=ShadowCastingMode.Off;skin.receiveShadows=false;
                    if(skin.sharedMesh.blendShapeCount>0)morphs.Add(skin);
                }
                instance=new Instance(asset.Plan,bones,morphs.ToArray());instance.Reset();
                return true;
            }
            catch(Exception error)
            {
                if(root!=null){root.SetActive(false);Object.Destroy(root);}
                unavailable.Add(kind);root=null;instance=null;
                Debug.LogWarning("Playable bond visual unavailable for "+kind+
                    "; preserving the playable fixture fallback. "+error.Message);
                return false;
            }
        }

        public void Dispose()
        {
            foreach(var mesh in meshes)Object.Destroy(mesh);
            foreach(var material in materials)Object.Destroy(material);
            foreach(var texture in textures.Values)Object.Destroy(texture);
            meshes.Clear();materials.Clear();textures.Clear();assets.Clear();unavailable.Clear();
        }
    }
}
