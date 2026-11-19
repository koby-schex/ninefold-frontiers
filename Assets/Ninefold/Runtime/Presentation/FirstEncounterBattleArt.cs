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
            return new WildlifeRig(kind,bones);
        }

        // Preserve the original, carefully placed external route gear and
        // living sensory crescent. Only old separate anatomical lofts are
        // removed before batching/parenting. Skin replaces their actual
        // visible geometry (not a second overlay on top of the old body).
        internal static void RemoveForagerBlockout(GameObject root,List<Mesh> owned)
        {
            var removed=new List<Transform>();
            foreach(Transform part in root.transform)
            {
                string n=part.name;
                if(n=="Avarin torso"||n=="Avarin neck"||n=="Avarin temple face"||
                    n.StartsWith("Avarin arm ",StringComparison.Ordinal)||
                    n.StartsWith("Avarin leg ",StringComparison.Ordinal)||
                    n.StartsWith("Avarin foot ",StringComparison.Ordinal)||
                    n.StartsWith("Avarin forearm relief ",StringComparison.Ordinal)||
                    n.StartsWith("Clavicle contour ",StringComparison.Ordinal)||
                    n.StartsWith("Heel geometry ",StringComparison.Ordinal)||
                    n.StartsWith("Temple cheek structure ",StringComparison.Ordinal))
                    removed.Add(part);
            }
            foreach(var part in removed)
            {
                var filter=part.GetComponent<MeshFilter>();
                if(filter!=null&&filter.sharedMesh!=null)
                {
                    owned.Remove(filter.sharedMesh);
                    UnityEngine.Object.Destroy(filter.sharedMesh);
                }
                part.gameObject.SetActive(false);
                part.SetParent(null,true);
                UnityEngine.Object.Destroy(part.gameObject);
            }
        }

        internal static void AttachForager(GameObject root,Material skin,
            Material fabric,Material dark,List<Mesh> owned)
        {
            var core=root.transform.Find("Ashbound motion core");
            if(core==null)throw new InvalidOperationException("Missing Avarin spine");
            var bones=new[]{core,core.Find("Ashbound motion head"),
                core.Find("Ashbound motion arm Left"),core.Find("Ashbound motion arm Right"),
                root.transform.Find("Ashbound motion leg Left"),
                root.transform.Find("Ashbound motion leg Right")};
            AttachSkin(root,Sculpt.Enemy.RiftForager,bones,
                new[]{skin,fabric,dark},owned);
        }

        private static void AttachSkin(GameObject root,Sculpt.Enemy kind,
            Transform[] bones,Material[] materials,List<Mesh> owned)
        {
            if(bones.Length!=Sculpt.BoneCount(kind))throw new InvalidOperationException("Rig size");
            foreach(var bone in bones)
                if(bone==null)throw new InvalidOperationException("Missing anatomical joint");
            var geometry=Sculpt.Tactical(kind);
            var vertices=new List<Vector3>(geometry.Vertices.Count);
            var normals=new List<Vector3>(geometry.Vertices.Count);
            var uv=new List<Vector2>(geometry.Vertices.Count);
            var weights=new BoneWeight[geometry.Vertices.Count];
            for(int i=0;i<geometry.Vertices.Count;i++)
            {
                var point=geometry.Vertices[i];
                vertices.Add(Vector(point)); normals.Add(Vector(geometry.Normals[i]));
                uv.Add(new Vector2(geometry.UVs[i][0],geometry.UVs[i][1]));
                var w=Sculpt.Weights(kind,point[0],point[1],point[2]);
                weights[i]=new BoneWeight{boneIndex0=w.A,boneIndex1=w.B,
                    weight0=w.WeightA,weight1=1f-w.WeightA};
            }
            var mesh=new Mesh{name=kind+" / connected battle skin",
                indexFormat=IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);
            mesh.subMeshCount=3;
            for(int i=0;i<3;i++)mesh.SetTriangles(geometry.Indices[i],i);
            mesh.RecalculateTangents();
            var node=Joint(root.transform,kind+" weighted rest surface",Vector3.zero);
            // Bind to local-space pivots that also carry face/mantle/gear.
            var binds=new Matrix4x4[bones.Length];
            for(int i=0;i<bones.Length;i++)
                binds[i]=bones[i].worldToLocalMatrix*node.localToWorldMatrix;
            mesh.bindposes=binds;mesh.boneWeights=weights;
            mesh.RecalculateBounds();
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
            owned.Add(mesh);
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
