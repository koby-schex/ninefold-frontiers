using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Ninefold.Core.Content;

namespace Ninefold.Presentation
{
    /// <summary>
    /// Compact, actual in-battle organic MESH fallback for World Five wildlife and
    /// specifically identified Ashbound NPCs. No authored/approved prefab is replaced.
    /// These are noncanonical provisional silhouettes, not final skinned assets.
    /// This class creates render geometry only: no colliders, scripts, combat data,
    /// pathing, sensors, object interactions or implicit creature bonds.
    /// </summary>
    internal static class WorldFiveBattleOrganics
    {
        /// <summary>
        /// Low-memory repeatable surface grain. Tinted by the material itself;
        /// purposefully subtle so skin and salvaged hardware don't look like
        /// flat Microsoft Paint fills or high-gloss plastic. No source textures.
        /// </summary>
        internal static Texture2D CreateSurfaceTexture(int seed)
        {
            const int size=128;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,true)
            {
                name="World Five organic study surface "+seed,
                filterMode=FilterMode.Trilinear,
                wrapMode=TextureWrapMode.Repeat
            };
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)
            for(int x=0;x<size;x++)
            {
                unchecked
                {
                    uint n=(uint)(x*374761393+y*668265263+seed*1013904223);
                    n=(n^(n>>13))*1274126177;
                    float fine=((n&65535)/65535f-.5f);
                    float band=Mathf.Sin(x*.23f+y*.035f+seed*.71f);
                    float broad=Mathf.Sin(x*.064f-y*.089f+seed*1.7f);
                    float tone=Mathf.Clamp(.84f+fine*.095f+band*.045f+broad*.045f,.67f,.99f);
                    pixels[y*size+x]=new Color(tone,tone*.99f,tone*.96f,1);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(true,true);
            return texture;
        }

        private static Vector3 P(float x,float y,float z)=>new Vector3(x,y,z);
        private static Transform Pivot(Transform parent,string label,Vector3 center)
        {
            var t=new GameObject(label).transform;
            t.SetParent(parent,false);t.localPosition=center;return t;
        }
        internal static FirstEncounterBattleArt.WildlifeRig Wildlife(GameObject root,bool stalker,
            Material body,Material accent,Material dark,List<Mesh> owned)
            =>FirstEncounterBattleArt.BuildWildlife(root,stalker,body,accent,dark,owned);

        // Mesh source is the same engine-independent numerical geometry
        // exported to the complete-model CI renderer. No substitute visual.
        internal static GameObject Part(GameObject root,List<Mesh> owned,
            AshboundWearablePlan.Piece piece,Material material,Transform joint)
        {
            var node=new GameObject(piece.Name);
            node.transform.SetParent(root.transform,false);
            var data=AshboundWearablePlan.Geometry(piece);
            var vertices=new List<Vector3>(data.Vertices.Count);
            var uv=new List<Vector2>(data.Vertices.Count);
            var normals=new List<Vector3>(data.Vertices.Count);
            for(int i=0;i<data.Vertices.Count;i++)
            {
                var p=data.Vertices[i];vertices.Add(P(p[0],p[1],p[2]));
                var n=data.Normals[i];normals.Add(P(n[0],n[1],n[2]));
                var t=data.UVs[i];uv.Add(new Vector2(t[0],t[1]));
            }
            var mesh=new Mesh{name=piece.Name+" / shaped wearable"};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetNormals(normals);
            mesh.SetTriangles(data.Indices[0],0);
            mesh.RecalculateTangents();mesh.RecalculateBounds();
            owned.Add(mesh);
            node.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=node.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=false;
            // Rest geometry is in actor-local coordinates. Preserve its
            // placement when parenting to the articulated bone.
            node.transform.SetParent(joint,true);
            return node;
        }

        internal static void Ashbound(GameObject root,int kind,
            Material skin,Material mantle,Material fabric,Material gear,
            Material conduit,Material dark,List<Mesh> owned)
        {
            var bones=GroupAshboundMotion(root,kind);
            var materials=new[]{skin,mantle,fabric,gear,conduit,dark};
            foreach(var piece in AshboundWearablePlan.Build(kind))
                Part(root,owned,piece,materials[(int)piece.Role],
                    bones[AshboundWearablePlan.JointFor(piece.Name,kind)]);
            // Co-material gear surfaces are batched per moving bone.
            // Keep the head separate: living mantle movement is independent
            // of the original facial relief and external equipment.
            for(int i=0;i<bones.Length;i++)
                if(i!=1)BatchPivotSurfaces(bones[i],owned);
            FirstEncounterBattleArt.AttachAshbound(root,kind,bones,skin,fabric,dark,owned);
        }

        private static Transform[] GroupAshboundMotion(GameObject root,int kind)
        {
            int count=FirstEncounterSculpts.BoneCount(FirstEncounterSculpts.Ashbound(kind));
            var bones=new Transform[count];
            bones[0]=Pivot(root.transform,"Ashbound motion core",P(0,-.13f,-.01f));
            bones[1]=Pivot(bones[0],"Ashbound motion head",P(0,.64f,0));
            bones[2]=Pivot(bones[0],"Ashbound motion arm Left",P(-.245f,.28f,-.02f));
            bones[3]=Pivot(bones[0],"Ashbound motion arm Right",P(.245f,.28f,-.02f));
            bones[4]=Pivot(root.transform,"Ashbound motion leg Left",P(-.15f,-.32f,-.02f));
            bones[5]=Pivot(root.transform,"Ashbound motion leg Right",P(.15f,-.32f,-.02f));
            if(count==10)
            {
                bones[6]=Pivot(bones[2],"Ashbound elbow Left",P(-.037f,-.105f,.047f));
                bones[7]=Pivot(bones[3],"Ashbound elbow Right",P(.037f,-.105f,.047f));
                bones[8]=Pivot(bones[4],"Ashbound knee Left",P(-.004f,-.19f,.035f));
                bones[9]=Pivot(bones[5],"Ashbound knee Right",P(.004f,-.19f,.035f));
            }
            return bones;
        }

        private static void BatchPivotSurfaces(Transform pivot,List<Mesh> owned)
        {
            var grouping=new Dictionary<Material,List<MeshFilter>>();
            foreach(Transform child in pivot)
            {
                var filter=child.GetComponent<MeshFilter>();
                var renderer=child.GetComponent<MeshRenderer>();
                if(filter==null || renderer==null || filter.sharedMesh==null)
                    continue;
                var material=renderer.sharedMaterial;
                if(!grouping.TryGetValue(material,out var filters))
                {
                    filters=new List<MeshFilter>();
                    grouping.Add(material,filters);
                }
                filters.Add(filter);
            }
            foreach(var group in grouping)
            {
                if(group.Value.Count<2) continue;
                var combines=new CombineInstance[group.Value.Count];
                for(int i=0;i<group.Value.Count;i++)
                {
                    var filter=group.Value[i];
                    combines[i]=new CombineInstance {
                        mesh=filter.sharedMesh,
                        transform=pivot.worldToLocalMatrix*filter.transform.localToWorldMatrix
                    };
                }
                var mesh=new Mesh {
                    name=pivot.name+" / consolidated "+group.Key.name,
                    indexFormat=IndexFormat.UInt32
                };
                mesh.CombineMeshes(combines,true,true,false);
                mesh.RecalculateBounds();
                owned.Add(mesh);
                var node=new GameObject("Surface batch / "+group.Key.name);
                node.transform.SetParent(pivot,false);
                node.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=node.AddComponent<MeshRenderer>();
                renderer.sharedMaterial=group.Key;
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                renderer.receiveShadows=false;
                foreach(var original in group.Value)
                {
                    // Original meshes are now owned only by this batch.
                    // Unity schedules the source object teardown safely
                    // outside the traversal; scene models are not persisted.
                    owned.Remove(original.sharedMesh);
                    original.gameObject.SetActive(false);
                    original.transform.SetParent(null,true);
                    if(Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(original.sharedMesh);
                        UnityEngine.Object.Destroy(original.gameObject);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(original.sharedMesh);
                        UnityEngine.Object.DestroyImmediate(original.gameObject);
                    }
                }
            }
        }
    }
}
