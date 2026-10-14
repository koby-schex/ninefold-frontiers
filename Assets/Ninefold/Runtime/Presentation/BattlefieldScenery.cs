using System;
using System.Collections.Generic;
using Ninefold.Core.Combat;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ninefold.Presentation
{
    /// <summary>Presentation-only environment study. Never changes authored navigation or save data.</summary>
    internal sealed class BattlefieldScenery : IDisposable
    {
        public int MeshCount => meshes.Count;
        private readonly Material stone, pale, distant, vegetation;
        private readonly Texture2D mineral;
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();

        public BattlefieldScenery(Material template, Material floor, Material cover)
        {
            Material Make(string name, Color color)
            {
                var m = new Material(template) { name = name, color = color };
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .16f);
                materials.Add(m); return m;
            }
            stone = Make("Layered mauve stone", new Color(.29f,.25f,.30f));
            pale = Make("Weathered mineral", new Color(.53f,.48f,.47f));
            distant = Make("Dusk escarpment", new Color(.25f,.22f,.32f));
            vegetation = Make("Dusty sage", new Color(.32f,.37f,.31f));
            mineral = new Texture2D(128,128,TextureFormat.RGBA32,true) { name = "Procedural mineral grain", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
            var pixels = new Color[128*128];
            for (int y=0;y<128;y++) for (int x=0;x<128;x++)
            {
                // Periodic strata and deterministic fine grain; no global random state.
                float grain = Noise(x,y)*.13f;
                float strata = Mathf.Sin(x*Mathf.PI/16 + Mathf.Sin(y*Mathf.PI/32)*1.4f)*.045f;
                float v=.76f+grain+strata;
                pixels[y*128+x]=new Color(v,v*.97f,v*.99f,1);
            }
            mineral.SetPixels(pixels); mineral.Apply(true,true);
            floor.mainTexture = mineral; cover.mainTexture = mineral;
            floor.color = new Color(.34f,.30f,.35f); cover.color = new Color(.47f,.44f,.46f);
            if (floor.HasProperty("_Smoothness")) floor.SetFloat("_Smoothness",.12f);
            if (cover.HasProperty("_Smoothness")) cover.SetFloat("_Smoothness",.22f);
        }

        private static float Noise(int x, int y)
        {
            unchecked { uint n=(uint)(x*374761393+y*668265263); n=(n^(n>>13))*1274126177; return (n&65535)/65535f; }
        }

        public void Build(Transform parent, BattlefieldMap map, Material floor)
        {
            ClearMeshes();
            float x0=(float)map.Bounds.Min.X, x1=(float)map.Bounds.Max.X;
            float z0=(float)map.Bounds.Min.Z, z1=(float)map.Bounds.Max.Z;
            floor.mainTextureScale = new Vector2((x1-x0)/3,(z1-z0)/3);
            var layers=new Batch(); var highlights=new Batch(); var horizon=new Batch(); var plants=new Batch();
            // Edge layers remain below walking height and outside the legal rectangle.
            int edge=0;
            for(float x=x0; x<x1; x+=1.4f)
            {
                float w=Mathf.Min(1.4f,x1-x);
                layers.Rock(new Vector3(x+w/2,-.75f,z0-.45f),new Vector3(w,.9f,.85f),edge++);
                layers.Rock(new Vector3(x+w/2,-.85f,z1+.5f),new Vector3(w,1.1f,.95f),edge++);
            }
            for(float z=z0; z<z1; z+=1.4f)
            {
                float d=Mathf.Min(1.4f,z1-z);
                layers.Rock(new Vector3(x0-.5f,-.8f,z+d/2),new Vector3(.95f,1f,d),edge++);
                layers.Rock(new Vector3(x1+.5f,-.8f,z+d/2),new Vector3(.95f,1f,d),edge++);
            }
            // Distant backdrop stays behind the far boundary; no props in playable lanes.
            for(int i=0;i<9;i++)
            {
                float x=Mathf.Lerp(x0-4,x1+4,i/8f), h=1.4f+Noise(i,9)*2.7f;
                horizon.Rock(new Vector3(x,-.7f,z1+4+Noise(i,7)*2),new Vector3(2.3f,h,2.8f),i+50);
                highlights.Rock(new Vector3(x,-.9f,z1+2),new Vector3(1.5f,.8f,1.3f),i+90);
            }
            // Restrained low silhouette vegetation, exclusively beyond the far edge.
            for(int i=0;i<18;i++)
            {
                float x=Mathf.Lerp(x0-1,x1+1,i/17f), z=z1+1.3f+Noise(i,3)*.4f;
                for(int j=0;j<3;j++) plants.Rock(new Vector3(x+j*.10f,-.25f,z),new Vector3(.12f,.3f+Noise(i,j)*.35f,.12f),i*3+j);
            }
            Emit(parent,"Layered boundary geology",layers,stone);
            Emit(parent,"Weathered outer shelves",highlights,pale);
            Emit(parent,"Distant fractured escarpment",horizon,distant);
            Emit(parent,"Dusty vegetation silhouettes",plants,vegetation);
            var lightObject=new GameObject("Dusk fill — environment study"); lightObject.transform.SetParent(parent,false);
            lightObject.transform.localPosition=new Vector3((x0+x1)/2,7,z1+1);
            var fill=lightObject.AddComponent<Light>(); fill.type=LightType.Point;
            fill.color=new Color(.73f,.64f,.88f); fill.intensity=1.4f; fill.range=Mathf.Max(x1-x0,z1-z0)*2;
            fill.shadows=LightShadows.None;
        }

        private void Emit(Transform parent,string name,Batch batch,Material material)
        {
            var mesh=new Mesh { name=name }; mesh.SetVertices(batch.Vertices); mesh.SetTriangles(batch.Triangles,0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); meshes.Add(mesh);
            var go=new GameObject(name); go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterial=material;
            // No colliders, click targets, animations, or gameplay components.
        }
        private void ClearMeshes() { foreach(var mesh in meshes) Object.Destroy(mesh); meshes.Clear(); }
        public void Dispose() { ClearMeshes(); foreach(var material in materials) Object.Destroy(material); Object.Destroy(mineral); }

        private sealed class Batch
        {
            public readonly List<Vector3> Vertices=new List<Vector3>();
            public readonly List<int> Triangles=new List<int>();
            private void Face(Vector3 a,Vector3 b,Vector3 c)
            { int n=Vertices.Count; Vertices.Add(a); Vertices.Add(b); Vertices.Add(c); Triangles.Add(n); Triangles.Add(n+1); Triangles.Add(n+2); }
            public void Rock(Vector3 center,Vector3 size,int seed)
            {
                const int sides=7;
                var lower=new Vector3[sides]; var upper=new Vector3[sides];
                for(int i=0;i<sides;i++)
                {
                    float angle=i*Mathf.PI*2/sides, radius=.82f+Noise(seed,i)*.18f;
                    var radial=new Vector3(Mathf.Cos(angle)*size.x*.5f*radius,0,Mathf.Sin(angle)*size.z*.5f*radius);
                    lower[i]=center+radial-Vector3.up*size.y*.5f;
                    upper[i]=center+radial*.78f+Vector3.up*size.y*(.28f+Noise(seed+7,i)*.22f);
                }
                for(int i=0;i<sides;i++)
                {
                    int j=(i+1)%sides;
                    Face(upper[i],center+Vector3.up*size.y*.38f,upper[j]);
                    Face(lower[i],upper[i],upper[j]); Face(lower[i],upper[j],lower[j]);
                    Face(lower[j],center-Vector3.up*size.y*.5f,lower[i]);
                }
            }
        }
    }
}
