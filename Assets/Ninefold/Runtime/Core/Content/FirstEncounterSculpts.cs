using System;
using System.Collections.Generic;

namespace Ninefold.Core.Content
{
    /// <summary>
    /// Actual connected rest surfaces for the three earliest named enemy models.
    /// Core/Unity-free so the exact meshes used in battle can be exported and
    /// tested without approximating their anatomy in another tool. Visual-only;
    /// never used by combat movement, collision, AI or saved progression.
    /// Wildlife forms follow the existing *proposed* native ecology design.
    /// </summary>
    public static class FirstEncounterSculpts
    {
        public enum Enemy { Seamstalker, Sillrunner, RiftForager }
        public const string Revision="first-encounter-connected-sculpt-01";
        public sealed class Surface
        {
            public readonly List<float[]> Vertices=new List<float[]>();
            public readonly List<float[]> Normals=new List<float[]>();
            public readonly List<float[]> UVs=new List<float[]>();
            // Body/skin, dorsal/fabric, dark eye/foot.
            public readonly List<int>[] Indices={
                new List<int>(),new List<int>(),new List<int>()};
            public int TriangleCount=>(Indices[0].Count+Indices[1].Count+Indices[2].Count)/3;
        }
        private static float Limit(float t,float lo,float hi)=>Math.Max(lo,Math.Min(hi,t));
        private static float Mix(float a,float b,float t)=>a+(b-a)*t;
        private static float Length(float x,float y,float z)=>(float)Math.Sqrt(x*x+y*y+z*z);
        private static float Ellipsoid(float x,float y,float z,
            float cx,float cy,float cz,float rx,float ry,float rz)
        {
            float dx=(x-cx)/rx,dy=(y-cy)/ry,dz=(z-cz)/rz;
            return (Length(dx,dy,dz)-1f)*Math.Min(rx,Math.Min(ry,rz));
        }
        private static float Capsule(float x,float y,float z,
            float ax,float ay,float az,float bx,float by,float bz,float r0,float r1)
        {
            float vx=bx-ax,vy=by-ay,vz=bz-az;
            float t=Limit(((x-ax)*vx+(y-ay)*vy+(z-az)*vz)/
                Math.Max(.00001f,vx*vx+vy*vy+vz*vz),0,1);
            return Length(x-ax-t*vx,y-ay-t*vy,z-az-t*vz)-Mix(r0,r1,t);
        }
        private static float Union(float a,float b,float k)
        {
            float t=Limit(.5f+.5f*(b-a)/k,0,1);
            return Mix(b,a,t)-k*t*(1-t);
        }

        // Body landmarks and rig joint coordinates come from the same source.
        // Each native has three articulated limbs on each side, consistent
        // with the existing native ecology proposal (not locked public lore).
        public static float[] WildlifeJoint(Enemy kind,int limb,bool knee)
        {
            if(kind==Enemy.RiftForager || limb<0||limb>=6)
                throw new ArgumentOutOfRangeException(nameof(limb));
            bool stalker=kind==Enemy.Seamstalker;
            float s=limb<3?-1:1;
            int pair=limb%3;
            float z=pair==0?.36f:pair==1?-.04f:-.47f;
            if(knee)return new[]{s*(stalker?.55f:.48f),-.33f,
                z+(pair==0?.10f:pair==2?-.10f:.02f)};
            return new[]{s*(stalker?.30f:.32f),-.035f,z};
        }

        private static float Wildlife(Enemy kind,float x,float y,float z)
        {
            bool s=kind==Enemy.Seamstalker;
            // Distinct silhouettes, not a palette swap: lower slender stalker
            // with raised forequarters/crown, compact broad runner with fan.
            float d=Ellipsoid(x,y,z,0,.115f,-.12f,
                s?.31f:.395f,s?.25f:.29f,s?.66f:.55f);
            d=Union(d,Ellipsoid(x,y,z,0,.17f,.34f,
                s?.33f:.35f,s?.28f:.25f,.32f),.11f);
            d=Union(d,Ellipsoid(x,y,z,0,s?.22f:.12f,.64f,
                s?.205f:.25f,s?.19f:.19f,s?.35f:.30f),.09f);
            d=Union(d,Ellipsoid(x,y,z,0,s?.12f:.07f,.91f,
                s?.14f:.19f,.115f,.19f),.065f);
            // Lower jaw and cheek contour remain part of the same surface.
            d=Union(d,Ellipsoid(x,y,z,0,.012f,.76f,
                s?.17f:.21f,.10f,.25f),.052f);
            for(int limb=0;limb<6;limb++)
            {
                // Scalar joint coordinates avoid millions of transient arrays
                // during signed-distance sampling. Matched to WildlifeJoint.
                float side=limb<3?-1f:1f;
                int pair=limb%3;
                float hipX=side*(s?.30f:.32f);
                float hipZ=pair==0?.36f:pair==1?-.04f:-.47f;
                float kneeX=side*(s?.55f:.48f);
                float kneeZ=hipZ+(pair==0?.10f:pair==2?-.10f:.02f);
                float footZ=kneeZ+(pair==0?.18f:pair==2?-.05f:.07f);
                // Splayed upper limb, clearly articulated lower limb,
                // rounded weight-bearing foot and short anchor-claw (stalker).
                d=Union(d,Capsule(x,y,z,hipX,-.035f,hipZ,
                    kneeX,-.33f,kneeZ,s?.12f:.135f,.079f),.045f);
                d=Union(d,Capsule(x,y,z,kneeX,-.33f,kneeZ,
                    side*(s?.58f:.50f),-.64f,footZ,.078f,.053f),.032f);
                d=Union(d,Ellipsoid(x,y,z,side*(s?.58f:.50f),-.695f,
                    footZ+.055f,s?.086f:.098f,.063f,s?.155f:.12f),.025f);
                if(s && pair==0)
                    d=Union(d,Ellipsoid(x,y,z,side*.58f,-.71f,
                        footZ+.175f,.041f,.036f,.105f),.024f);
            }
            for(int sign=-1;sign<=1;sign+=2)
            {
                float a=sign;
                // Integrated lateral sensory ridge and recessed eye region
                // are not human-like head additions.
                d=Union(d,Ellipsoid(x,y,z,a*(s?.174f:.205f),.24f,.74f,
                    .073f,.064f,.085f),.036f);
                if(s)
                {
                    // Split crown merging into forequarters, not wings.
                    d=Union(d,Capsule(x,y,z,a*.15f,.29f,.55f,
                        a*.25f,.49f,.33f,.09f,.045f),.037f);
                    d=Union(d,Ellipsoid(x,y,z,a*.23f,.40f,.24f,
                        .11f,.14f,.22f),.054f);
                }
                else
                {
                    // Long lateral sensory folds terminate gradually.
                    for(int ridge=0;ridge<3;ridge++)
                    {
                        float rz=.23f-ridge*.25f;
                        d=Union(d,Ellipsoid(x,y,z,a*.33f,.29f,rz,
                            .091f,.072f,.19f),.041f);
                    }
                }
            }
            if(s)
            {
                d=Union(d,Capsule(x,y,z,0,.10f,-.64f,0,.075f,-1.04f,
                    .18f,.10f),.065f);
                d=Union(d,Capsule(x,y,z,0,.075f,-1.04f,0,.13f,-1.37f,
                    .10f,.027f),.035f);
                for(int ridge=0;ridge<3;ridge++)
                    d=Union(d,Ellipsoid(x,y,z,0,.35f,.17f-ridge*.25f,
                        .095f,.105f,.17f),.043f);
            }
            else
            {
                d=Union(d,Ellipsoid(x,y,z,0,.06f,-.71f,.24f,.115f,.30f),.065f);
                d=Union(d,Ellipsoid(x,y,z,0,.075f,-.99f,.37f,.079f,.25f),.050f);
                // Dorsal folds model real geometry; color alone cannot
                // make these profiles look like different animals.
                for(int ridge=0;ridge<4;ridge++)
                    d=Union(d,Ellipsoid(x,y,z,0,.34f,.23f-ridge*.22f,
                        .29f,.085f,.105f),.038f);
            }
            return d;
        }

        // Forager: rest shape matched to the authored IN-BATTLE gear
        // and pivots, not mechanically transplanted from incompatible
        // ArtReview proportions. All face/gear/mantle source meshes stay
        // attached separately; old disconnected limb/torso lofts are removed.
        private static float Forager(float x,float y,float z)
        {
            float d=Ellipsoid(x,y,z,0,-.22f,-.02f,.19f,.22f,.15f);
            d=Union(d,Ellipsoid(x,y,z,0,.04f,-.035f,.205f,.29f,.155f),.085f);
            d=Union(d,Ellipsoid(x,y,z,0,.285f,-.035f,.223f,.17f,.135f),.065f);
            d=Union(d,Ellipsoid(x,y,z,0,.465f,.008f,.086f,.14f,.084f),.056f);
            // Elongated, tapered temple-face; avoid a generic spherical head.
            d=Union(d,Ellipsoid(x,y,z,0,.66f,.042f,.123f,.172f,.120f),.047f);
            d=Union(d,Ellipsoid(x,y,z,0,.505f,.083f,.075f,.077f,.080f),.035f);
            d=Union(d,Ellipsoid(x,y,z,0,.785f,.008f,.101f,.073f,.088f),.035f);
            for(int sign=-1;sign<=1;sign+=2)
            {
                float a=sign;
                d=Union(d,Ellipsoid(x,y,z,a*.098f,.645f,.101f,.052f,.057f,.052f),.028f);
                d=Union(d,Ellipsoid(x,y,z,a*.193f,.302f,-.033f,.098f,.09f,.093f),.055f);
                d=Union(d,Capsule(x,y,z,a*.22f,.29f,-.035f,
                    a*.282f,.045f,.017f,.096f,.078f),.051f);
                d=Union(d,Capsule(x,y,z,a*.282f,.045f,.017f,
                    a*.311f,-.215f,.105f,.077f,.058f),.043f);
                d=Union(d,Ellipsoid(x,y,z,a*.309f,-.274f,.148f,.048f,.079f,.058f),.032f);
                d=Union(d,Ellipsoid(x,y,z,a*.115f,-.30f,-.025f,.114f,.14f,.106f),.064f);
                d=Union(d,Capsule(x,y,z,a*.125f,-.29f,-.022f,
                    a*.154f,-.51f,.015f,.103f,.083f),.052f);
                d=Union(d,Capsule(x,y,z,a*.154f,-.51f,.015f,
                    a*.16f,-.69f,.065f,.078f,.055f),.042f);
                d=Union(d,Ellipsoid(x,y,z,a*.163f,-.72f,.137f,
                    .085f,.058f,.133f),.032f);
            }
            // Subtle bilateral anatomical carving instead of primitive bulges.
            float ax=Math.Abs(x);
            float temple=.004f*(float)Math.Exp(
                -(ax-.116f)*(ax-.116f)/.0012f-(y-.74f)*(y-.74f)/.009f);
            float waist=.005f*(float)Math.Exp(-(y+.10f)*(y+.10f)/.025f);
            return d+temple+waist;
        }
        public static float Field(Enemy kind,float x,float y,float z)=>
            kind==Enemy.RiftForager?Forager(x,y,z):Wildlife(kind,x,y,z);

        public static int MaterialSlot(Enemy kind,float x,float y,float z)
        {
            float ax=Math.Abs(x);
            if(kind==Enemy.RiftForager)
            {
                if(y<-.67f)return 2;
                return y>.455f || (ax>.255f&&y<-.20f&&y>-.37f)?0:1;
            }
            bool s=kind==Enemy.Seamstalker;
            float eyeX=s?.205f:.229f;
            if(Math.Abs(ax-eyeX)<.066f && Math.Abs(y-.245f)<.065f &&
               Math.Abs(z-.74f)<.092f)return 2;
            if(y<-.65f || y>.31f && z<.55f ||
               !s && z<-.80f || s&&z>.75f&&y<-.62f)return 1;
            return 0;
        }

        // Two strongest weights, normalized. Each limb blends across its
        // actual hip and knee region; head and tail remain attached to skin.
        public readonly struct Skin
        {
            public readonly int A,B;
            public readonly float WeightA;
            public Skin(int a,int b,float w){A=a;B=b;WeightA=Limit(w,0,1);}
        }
        public static Skin Weights(Enemy kind,float x,float y,float z)
        {
            float ax=Math.Abs(x);
            if(kind==Enemy.RiftForager)
            {
                if(y>.37f)return new Skin(1,0,Limit((y-.37f)/.20f,0,1));
                if(ax>.18f && y>-.36f)
                    return new Skin(x<0?2:3,0,Limit((ax-.18f)/.135f,0,1));
                if(y<-.23f)
                    return new Skin(x<0?4:5,0,Limit((-y-.23f)/.24f,0,1));
                return new Skin(0,0,1);
            }
            if(y<-.075f && ax>.23f)
            {
                int pair=z>.16f?0:z<-.26f?2:1;
                int limb=(x<0?0:3)+pair;
                int hip=3+limb*2,knee=hip+1;
                if(y<-.22f)return new Skin(knee,hip,Limit((-y-.22f)/.22f,0,1));
                return new Skin(hip,0,Limit((-y-.075f)/.145f,0,1));
            }
            if(z>.42f)return new Skin(1,0,Limit((z-.42f)/.28f,0,1));
            if(z<-.70f)return new Skin(2,0,Limit((-z-.70f)/.25f,0,1));
            return new Skin(0,0,1);
        }
        public static int BoneCount(Enemy kind)=>kind==Enemy.RiftForager?6:15;
        // Runtime instances reuse immutable generated source data, not Unity
        // meshes (scene ownership still destroys each actual uploaded mesh).
        private static readonly Lazy<Surface>[] TacticalCache={
            new Lazy<Surface>(()=>Generate(Enemy.Seamstalker,.045f)),
            new Lazy<Surface>(()=>Generate(Enemy.Sillrunner,.045f)),
            new Lazy<Surface>(()=>Generate(Enemy.RiftForager,.038f))};
        public static Surface Tactical(Enemy kind)=>TacticalCache[(int)kind].Value;

        private static readonly int[,] Tetrahedra = {
            {0,5,1,6},{0,1,2,6},{0,2,3,6},
            {0,3,7,6},{0,7,4,6},{0,4,5,6}
        };
        private static readonly int[,] CornerOffsets = {
            {0,0,0},{1,0,0},{1,1,0},{0,1,0},
            {0,0,1},{1,0,1},{1,1,1},{0,1,1}
        };

        /// <summary>
        /// Shared-edge marching tetrahedra, gradient normals, indexed 3D
        /// triangles, coherent UVs and source body/skin material regions.
        /// Topology has zero disjoint arm/leg primitive seams by construction.
        /// </summary>
        public static Surface Generate(Enemy kind,float step=.045f)
        {
            if(step<.030f || step>.075f || float.IsNaN(step) ||
                float.IsInfinity(step))
                throw new ArgumentOutOfRangeException(nameof(step));
            if(!Enum.IsDefined(typeof(Enemy),kind))throw new ArgumentOutOfRangeException(nameof(kind));
            bool forager=kind==Enemy.RiftForager;
            float minX=forager?-.55f:-.87f,minY=-.84f,minZ=forager?-.48f:-1.55f;
            float maxX=forager?.55f:.87f,maxY=forager?.94f:.73f,maxZ=forager?.53f:1.30f;
            int nx=(int)Math.Ceiling((maxX-minX)/step)+1;
            int ny=(int)Math.Ceiling((maxY-minY)/step)+1;
            int nz=(int)Math.Ceiling((maxZ-minZ)/step)+1;
            int strideZ=nx*ny;
            int total=nx*ny*nz;
            var values=new float[total];
            int Id(int x,int y,int z)=>x+nx*(y+ny*z);
            for(int z=0;z<nz;z++)
            for(int y=0;y<ny;y++)
            for(int x=0;x<nx;x++)
                values[Id(x,y,z)]=Field(kind,
                    minX+x*step,minY+y*step,minZ+z*step);

            var mesh=new Surface();
            var edges=new Dictionary<ulong,int>();
            int[] cubeIds=new int[8];
            float[] cubeValues=new float[8];

            int Edge(int idA,int idB,float valueA,float valueB)
            {
                int a=Math.Min(idA,idB),b=Math.Max(idA,idB);
                ulong key=((ulong)(uint)a<<32)|(uint)b;
                if(edges.TryGetValue(key,out int existing))return existing;
                float fraction=Limit(valueA/(valueA-valueB),.0001f,.9999f);
                int ax=idA%nx, ay=(idA/nx)%ny, az=idA/strideZ;
                int bx=idB%nx, by=(idB/nx)%ny, bz=idB/strideZ;
                float px=minX+Mix(ax,bx,fraction)*step;
                float py=minY+Mix(ay,by,fraction)*step;
                float pz=minZ+Mix(az,bz,fraction)*step;
                const float epsilon=.005f;
                float gx=Field(kind,px+epsilon,py,pz)-Field(kind,px-epsilon,py,pz);
                float gy=Field(kind,px,py+epsilon,pz)-Field(kind,px,py-epsilon,pz);
                float gz=Field(kind,px,py,pz+epsilon)-Field(kind,px,py,pz-epsilon);
                float norm=Math.Max(1e-8f,Length(gx,gy,gz));
                int index=mesh.Vertices.Count;
                mesh.Vertices.Add(new[]{px,py,pz});
                mesh.Normals.Add(new[]{gx/norm,gy/norm,gz/norm});
                // Cylindrical anatomical UV parametrization suitable for
                // nonfinal fabric and skin inspection; seams await hand UVs.
                // Locally continuous planar UVs: no pole or cylindrical seam across a leg.
                mesh.UVs.Add(new[]{px*.7f+pz*.38f,py*.9f+pz*.52f});
                edges.Add(key,index);
                return index;
            }

            void Triangle(int a,int b,int c)
            {
                if(a==b||b==c||a==c)return;
                float[] p=mesh.Vertices[a],q=mesh.Vertices[b],t=mesh.Vertices[c];
                float ux=q[0]-p[0],uy=q[1]-p[1],uz=q[2]-p[2];
                float vx=t[0]-p[0],vy=t[1]-p[1],vz=t[2]-p[2];
                float cx=uy*vz-uz*vy,cy=uz*vx-ux*vz,
                    cz=ux*vy-uy*vx;
                // Surface extraction must preserve even very small lawful
                // triangles around a cell/tetra junction. The previous
                // aggressive area cull opened hundreds of boundary edges.
                // Clamped interpolation already prevents collapsed corners.
                if(cx==0f && cy==0f && cz==0f)
                    throw new InvalidOperationException(
                        "Collapsed iso-surface triangle: input field degeneracy.");
                var n=mesh.Normals[a];
                if(cx*n[0]+cy*n[1]+cz*n[2]<0)
                {
                    int swap=b;b=c;c=swap;
                }
                int slot=MaterialSlot(kind,(p[0]+q[0]+t[0])/3f,
                    (p[1]+q[1]+t[1])/3f,(p[2]+q[2]+t[2])/3f);
                var target=mesh.Indices[slot];
                target.Add(a);target.Add(b);target.Add(c);
            }

            int[] inner=new int[4],outer=new int[4];
            for(int z=0;z<nz-1;z++)
            for(int y=0;y<ny-1;y++)
            for(int x=0;x<nx-1;x++)
            {
                bool inside=false,outside=false;
                for(int corner=0;corner<8;corner++)
                {
                    int id=Id(x+CornerOffsets[corner,0],
                        y+CornerOffsets[corner,1],z+CornerOffsets[corner,2]);
                    cubeIds[corner]=id;
                    cubeValues[corner]=values[id];
                    if(values[id]<0)inside=true;else outside=true;
                }
                if(!inside||!outside)continue;
                for(int tetra=0;tetra<6;tetra++)
                {
                    int nInner=0,nOuter=0;
                    for(int v=0;v<4;v++)
                    {
                        int corner=Tetrahedra[tetra,v];
                        if(cubeValues[corner]<0)inner[nInner++]=corner;
                        else outer[nOuter++]=corner;
                    }
                    if(nInner==0||nInner==4)continue;
                    int Cut(int p,int q)=>Edge(cubeIds[p],cubeIds[q],
                        cubeValues[p],cubeValues[q]);
                    if(nInner==1)
                    {
                        Triangle(Cut(inner[0],outer[0]),
                            Cut(inner[0],outer[1]),
                            Cut(inner[0],outer[2]));
                    }
                    else if(nInner==3)
                    {
                        Triangle(Cut(outer[0],inner[0]),
                            Cut(outer[0],inner[1]),
                            Cut(outer[0],inner[2]));
                    }
                    else
                    {
                        int p=Cut(inner[0],outer[0]);
                        int q=Cut(inner[0],outer[1]);
                        int a=Cut(inner[1],outer[0]);
                        int d=Cut(inner[1],outer[1]);
                        Triangle(p,q,a);
                        Triangle(q,d,a);
                    }
                }
            }
            if(mesh.TriangleCount<2500 || mesh.Indices[0].Count==0 ||
                mesh.Indices[1].Count==0 || mesh.Indices[2].Count==0 ||
                mesh.Vertices.Count>80000)
                throw new InvalidOperationException(
                    "Anatomical surface failed minimum coherent resolution.");
            return mesh;
        }
    }
}
