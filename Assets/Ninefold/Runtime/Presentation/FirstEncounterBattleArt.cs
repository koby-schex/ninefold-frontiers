using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Ninefold.Core.Content;
using Sculpt=Ninefold.Core.Content.FirstEncounterSculpts;

namespace Ninefold.Presentation
{
    // The actual battle fallback meshes for the first three named encounters.
    // No gameplay/physics component is attached. Approved prefab precedence
    // remains with PlaytestBattlefield. All Unity meshes are battle owned.
    internal static class FirstEncounterBattleArt
    {
        private static Vector3 Vector(float[] p)=>new Vector3(p[0],p[1],p[2]);
        private static Transform Joint(Transform parent,string name,Vector3 local)
        {
            var child=new GameObject(name).transform;
            child.SetParent(parent,false);
            child.localPosition=local;
            return child;
        }

        internal sealed class WildlifeRig
        {
            internal readonly Sculpt.Enemy Kind;
            internal readonly Transform[] Bones;
            internal WildlifeRig(Sculpt.Enemy kind,Transform[] bones)
            {Kind=kind;Bones=bones;}

            internal void Pose(float phase,bool moving,bool attacking,float strike,
                bool idle=false)
            {
                bool stalker=Kind==Sculpt.Enemy.Seamstalker;
                float elapsed=moving?phase*(stalker?16f:21f):0f;
                float thrust=attacking?Mathf.Sin(Mathf.Clamp01(strike)*Mathf.PI):0f;
                float breath=idle?Mathf.Sin(phase)*.65f:0f;
                Bones[0].localRotation=Quaternion.Euler(
                    (moving?1.5f*Mathf.Sin(elapsed*2):0f)-thrust*(stalker?7f:4f),
                    0,(moving?Mathf.Sin(elapsed)*1.2f:0f));
                Bones[0].localScale=new Vector3(1f+breath*.012f,1f+breath*.018f,1f);
                Bones[1].localRotation=Quaternion.Euler(
                    -thrust*(stalker?24f:10f)+breath,
                    idle?Mathf.Sin(phase*.6f)*(stalker?4f:2f):0f,0);
                Bones[2].localRotation=Quaternion.Euler(
                    thrust*4f,moving?Mathf.Sin(elapsed)*12f:breath*4f,0);
                // Alternating tripods: left front/rear and right middle,
                // then right front/rear and left middle. Two-joint legs
                // deform the continuous actual mesh, rather than moving
                // rigid disconnected cylinders.
                for(int limb=0;limb<6;limb++)
                {
                    int side=limb<3?-1:1;
                    int pair=limb%3;
                    float offset=((pair%2==0)==(side<0))?0f:Mathf.PI;
                    float wave=moving?Mathf.Sin(elapsed+offset):0f;
                    float lift=Mathf.Max(0,wave);
                    float brace=attacking&&pair==0?thrust*(stalker?11f:5f):0f;
                    Bones[3+limb*2].localRotation=Quaternion.Euler(
                        wave*(stalker?19f:14f)-brace,0,side*lift*3f);
                    Bones[4+limb*2].localRotation=Quaternion.Euler(
                        -lift*(stalker?28f:22f)+brace*.5f,0,0);
                }
            }
        }

        internal static WildlifeRig BuildWildlife(GameObject root,bool stalker,
            Material body,Material accent,Material dark,List<Mesh> owned)
        {
            var kind=stalker?Sculpt.Enemy.Seamstalker:Sculpt.Enemy.Sillrunner;
            var bones=new Transform[Sculpt.BoneCount(kind)];
            bones[0]=Joint(root.transform,"Wildlife continuous trunk",Vector3.zero);
            bones[1]=Joint(bones[0],"Wildlife head joint",new Vector3(0,.14f,.53f));
            bones[2]=Joint(bones[0],"Wildlife tail or balance-fan joint",
                new Vector3(0,.10f,-.64f));
            for(int limb=0;limb<6;limb++)
            {
                string label=(limb<3?"Left ":"Right ")+(limb%3+1);
                Vector3 hip=Vector(Sculpt.WildlifeJoint(kind,limb,false));
                Vector3 knee=Vector(Sculpt.WildlifeJoint(kind,limb,true));
                bones[3+limb*2]=Joint(bones[0],"Wildlife hip "+label,hip);
                bones[4+limb*2]=Joint(bones[3+limb*2],"Wildlife lower leg "+label,knee-hip);
            }
            AttachSkin(root,kind,bones,new[]{body,accent,dark},owned);
            AttachSkin(root,kind,bones,new[]{accent,dark},owned,detail:"fitted eyes");
            return new WildlifeRig(kind,bones);
        }

        internal static void AttachAshbound(GameObject root,int kind,Transform[] bones,
            Material skin,Material fabric,Material dark,List<Mesh> owned)
        {
            var enemy=Sculpt.Ashbound(kind);
            AttachSkin(root,enemy,bones,new[]{skin,fabric,dark},owned);
            AttachSkin(root,enemy,bones,new[]{fabric,dark},owned,detail:"stitched fieldcloth");
        }

        private static void AttachSkin(GameObject root,Sculpt.Enemy kind,
            Transform[] bones,Material[] materials,List<Mesh> owned,string detail=null)
        {
            if(bones.Length!=Sculpt.BoneCount(kind))throw new InvalidOperationException("Rig size");
            foreach(var bone in bones)
                if(bone==null)throw new InvalidOperationException("Missing anatomical joint");
            bool extra=detail!=null;
            string meshName=kind+" / "+(detail??"connected battle skin");
            var node=Joint(root.transform,meshName,Vector3.zero);
            // Same kind/rig rest dimensions are immutable. Reuse one GPU
            // mesh across all like enemies, never reuse a transform/bone.
            // The scene's owned collection releases it exactly once.
            Mesh mesh=owned.Find(m=>m!=null && m.name==meshName);
            if(mesh==null)
            {
                var geometry=detail=="stitched fieldcloth"?FirstEncounterForagerCloth.Tactical((int)kind-2):
                    detail=="fitted eyes"?Sculpt.EyeShells(kind):Sculpt.Tactical(kind);
                var vertices=new List<Vector3>(geometry.Vertices.Count);
                var normals=new List<Vector3>(geometry.Vertices.Count);
                var uv=new List<Vector2>(geometry.Vertices.Count);
                var weights=new BoneWeight[geometry.Vertices.Count];
                for(int i=0;i<geometry.Vertices.Count;i++)
                {
                    var point=geometry.Vertices[i];
                    vertices.Add(Vector(point)); normals.Add(Vector(geometry.Normals[i]));
                    uv.Add(new Vector2(geometry.UVs[i][0],geometry.UVs[i][1]));
                    var w=detail=="stitched fieldcloth"?
                        FirstEncounterForagerCloth.ClothWeights((int)kind-2,point[0],point[1],point[2]):
                        Sculpt.Weights(kind,point[0],point[1],point[2]);
                    if(detail=="fitted eyes")w=new Sculpt.Skin(1,0,1f);
                    weights[i]=new BoneWeight{boneIndex0=w.A,boneIndex1=w.B,
                        weight0=w.WeightA,weight1=1f-w.WeightA};
                }
                mesh=new Mesh{name=meshName,indexFormat=geometry.Vertices.Count>65535?
                    IndexFormat.UInt32:IndexFormat.UInt16};
                mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);
                mesh.subMeshCount=extra?2:3;
                for(int i=0;i<mesh.subMeshCount;i++)
                    mesh.SetTriangles(geometry.Indices[i+(extra?1:0)],i);
                mesh.RecalculateTangents();
                // Bind to local-space pivots that also carry face/mantle/gear.
                var binds=new Matrix4x4[bones.Length];
                for(int i=0;i<bones.Length;i++)
                    binds[i]=bones[i].worldToLocalMatrix*node.localToWorldMatrix;
                mesh.bindposes=binds;mesh.boneWeights=weights;
                mesh.RecalculateBounds();
                owned.Add(mesh);
            }
            var renderer=node.gameObject.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh=mesh;renderer.bones=bones;renderer.rootBone=bones[0];
            renderer.quality=SkinQuality.Bone2;renderer.sharedMaterials=materials;
            renderer.updateWhenOffscreen=false;
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=false;
            var bounds=mesh.bounds;
            // Conservative swing envelope measured in root-bone space.
            var origin=root.transform.InverseTransformPoint(bones[0].position);
            bounds.center-=origin;bounds.Expand(.65f);renderer.localBounds=bounds;
        }

        // Reused linear packed R-metallic/A-smoothness maps for external
        // salvaged hardware and worn fabric. Distinct surfaces, not new hues.
        internal static Texture2D SurfaceResponse(bool metal)
        {
            const int size=128;
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=2f*Mathf.PI*x/size,v=2f*Mathf.PI*y/size;
                float wear=Mathf.Sin(u*7+Mathf.Sin(v*5))*.6f+
                    Mathf.Sin(v*19-u*13)*.4f;
                byte Byte(float value)=>(byte)Mathf.RoundToInt(Mathf.Clamp01(value)*255);
                pixels[y*size+x]=new Color32(Byte(metal?.40f+.11f*wear:.01f),0,0,
                    Byte(metal?.33f+.09f*wear:.16f+.035f*wear));
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,true,true)
            {
                name=metal?"Ashbound worn salvage response":"Ashbound matte textile response",
                wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear
            };
            texture.SetPixels32(pixels);texture.Apply(true,true);
            return texture;
        }

        // Separate non-flat material response for each wildlife surface
        // and the Forager garment. Repeatable tiled normal detail, sampled
        // in the real shader, no invented colors or magic markings.
        internal static Texture2D OrganicNormal(bool stalker)
        {
            const int size=128;
            var pixels=new Color32[size*size];
            float Height(int x,int y)
            {
                float u=2f*Mathf.PI*x/size,v=2f*Mathf.PI*y/size;
                return (stalker?.12f:.09f)*Mathf.Sin(u*9+Mathf.Sin(v*3))+
                    .06f*Mathf.Sin(v*17+Mathf.Cos(u*5))+
                    .025f*Mathf.Sin(u*29-v*23);
            }
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=Height(x+1,y)-Height(x-1,y);
                float dy=Height(x,y+1)-Height(x,y-1);
                var n=new Vector3(-dx*1.4f,-dy*1.4f,1).normalized;
                byte Byte(float value)=>(byte)Mathf.RoundToInt(Mathf.Clamp01(value)*255);
                pixels[y*size+x]=new Color32(Byte(n.x*.5f+.5f),
                    Byte(n.y*.5f+.5f),Byte(n.z*.5f+.5f),255);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,true,true)
            {
                name="Early enemy tiled normal study",
                wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear
            };
            texture.SetPixels32(pixels);texture.Apply(true,true);
            return texture;
        }
    }
}
