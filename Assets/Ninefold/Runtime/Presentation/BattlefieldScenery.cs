using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Plan=Ninefold.Core.Content.BattlefieldArtPlan;
using Object=UnityEngine.Object;
using N=System.Numerics.Vector3;

namespace Ninefold.Presentation
{
    /// <summary>Presentation-only actual shared terrain mesh adapter.
    /// One material and at most one mesh per terrain surface. No colliders,
    /// navigation generation, per-prop GameObjects or global scene changes.</summary>
    internal sealed class BattlefieldScenery : IDisposable
    {
        public int MeshCount=>meshes.Count;
        private readonly List<Mesh> meshes=new List<Mesh>();
        private readonly Material[] palette=new Material[Enum.GetValues(typeof(Plan.Surface)).Length];
        private readonly Texture2D mineral,normal;
        private readonly Material floorSurface,coverSurface;
        private Color restingThreads;
        private bool hasScene;
        private static Color ColorOf(N p)=>new Color(p.X,p.Y,p.Z,1);
        private static Vector3 Vec(N p)=>new Vector3(p.X,p.Y,p.Z);
        public BattlefieldScenery(Material template,Material floor,Material cover)
        {
            floorSurface=floor;coverSurface=cover;
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            for(int i=0;i<palette.Length;i++)
            {
                var slot=(Plan.Surface)i;
                var m=shader!=null?new Material(shader):new Material(template);
                m.name="Fractured Meridian terrain / "+slot;
                if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",
                    slot==Plan.Surface.CoverCrown?.36f:slot==Plan.Surface.Threads?.21f:.13f);
                if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0);
                palette[i]=m;
            }
            mineral=CreateMineralMap(false);normal=CreateMineralMap(true);
            foreach(var m in palette)
            {
                var name=m.name;
                if(name.EndsWith("Rim",StringComparison.Ordinal)||name.EndsWith("SlowEdge",StringComparison.Ordinal)||
                    name.EndsWith("Threads",StringComparison.Ordinal)||name.EndsWith("FarHaze",StringComparison.Ordinal))continue;
                m.mainTexture=mineral;
                if(m.HasProperty("_BumpMap"))
                {
                    m.SetTexture("_BumpMap",normal);
                    if(m.HasProperty("_BumpScale"))m.SetFloat("_BumpScale",.40f);
                    m.EnableKeyword("_NORMALMAP");
                }
            }
        }
        private static float Height(int x,int y,int size)
        {
            // Periodic integer tile sampling. Paired scalar and tangent-space
            // maps share the same physical grain/vein pattern across UV seams.
            float u=2f*Mathf.PI*x/size,v=2f*Mathf.PI*y/size;
            float warp=Mathf.Sin(v*3+Mathf.Sin(u*2)*1.2f);
            float bands=Mathf.Pow(.5f+.5f*Mathf.Sin(u*6+warp*.9f+v*2),9);
            float fine=.5f*Mathf.Sin(u*37+v*29)+.28f*Mathf.Sin(v*67-u*41)+.22f*Mathf.Sin(u*83+v*71);
            return .5f+.09f*Mathf.Sin(u*3+v*2)+.055f*Mathf.Sin(v*7-u*5)+.085f*bands+.035f*fine;
        }
        private static Texture2D CreateMineralMap(bool bump)
        {
            const int size=256;
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float h=Height(x,y,size);
                if(bump)
                {
                    float dx=(Height(x+1,y,size)-Height(x-1,y,size))*3.0f;
                    float dy=(Height(x,y+1,size)-Height(x,y-1,size))*3.0f;
                    var n=new Vector3(-dx,-dy,1).normalized;
                    byte Channel(float a)=>(byte)Mathf.RoundToInt((a*.5f+.5f)*255f);
                    // URP tangent normal convention: encoded X in A, Y in G.
                    pixels[y*size+x]=new Color32(255,Channel(n.y),255,Channel(n.x));
                }
                else
                {
                    float shade=Mathf.Clamp01(.58f+h*.38f);
                    byte Channel(float a)=>(byte)Mathf.RoundToInt(Mathf.Clamp01(a)*255f);
                    pixels[y*size+x]=new Color32(Channel(shade),Channel(shade*.985f),Channel(shade),255);
                }
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,true,bump)
            {
                name=bump?"Mineral strata tangent surface":"Mineral strata albedo",
                wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=2
            };
            texture.SetPixels32(pixels);texture.Apply(true,true);return texture;
        }
        public void Build(Transform parent,Plan.Scene scene)
        {
            ClearMeshes();
            for(int i=0;i<palette.Length;i++)palette[i].color=ColorOf(scene.Palette[i]);
            floorSurface.color=ColorOf(scene.Palette[(int)Plan.Surface.Floor]);
            coverSurface.color=ColorOf(scene.Palette[(int)Plan.Surface.Cover]);
            floorSurface.mainTexture=mineral;coverSurface.mainTexture=mineral;
            floorSurface.mainTextureScale=Vector2.one;coverSurface.mainTextureScale=Vector2.one;
            restingThreads=palette[(int)Plan.Surface.Threads].color;hasScene=true;
            foreach(var source in scene.Meshes)
            {
                if(source.Vertices.Count==0)continue;
                var vertices=new List<Vector3>(source.Vertices.Count);
                var uv=new List<Vector2>(source.Vertices.Count);
                var indices=new int[source.Vertices.Count];
                for(int i=0;i<source.Vertices.Count;i+=3)
                {
                    var a=Vec(source.Vertices[i]);var b=Vec(source.Vertices[i+1]);var c=Vec(source.Vertices[i+2]);
                    var n=Vector3.Cross(b-a,c-a);var abs=new Vector3(Mathf.Abs(n.x),Mathf.Abs(n.y),Mathf.Abs(n.z));
                    for(int j=0;j<3;j++)
                    {
                        var p=Vec(source.Vertices[i+j]);vertices.Add(p);indices[i+j]=i+j;
                        bool horizontal=abs.y>=abs.x&&abs.y>=abs.z;
                        uv.Add(horizontal?new Vector2(p.x/2.7f,p.z/2.7f):
                            abs.x>abs.z?new Vector2(p.z/2.7f,p.y/2.7f):new Vector2(p.x/2.7f,p.y/2.7f));
                    }
                }
                var mesh=new Mesh{name="Terrain · "+source.Material,
                    indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
                mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);
                mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
                mesh.UploadMeshData(true);meshes.Add(mesh);
                var go=new GameObject(mesh.name);go.transform.SetParent(parent,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=palette[(int)source.Material];
                // Terrain is not a shadow-casting object wall; only the exact
                // tactical obstacle shapes use vertical cover. No extra lights.
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
            }
        }
        public void Animate(float time)
        {
            if(hasScene)palette[(int)Plan.Surface.Threads].color=Color.Lerp(restingThreads,
                Color.white,.022f+.012f*Mathf.Sin(time*.70f));
        }
        private void ClearMeshes(){foreach(var mesh in meshes)Object.Destroy(mesh);meshes.Clear();hasScene=false;}
        public void Dispose()
        {
            ClearMeshes();
            foreach(var m in palette)Object.Destroy(m);
            Object.Destroy(mineral);Object.Destroy(normal);
        }
    }
}
