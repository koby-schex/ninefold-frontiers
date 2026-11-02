using System;
using System.Collections.Generic;

namespace Ninefold.Editor
{
    /// <summary>
    /// Versioned, continuous organic rest-surface candidate for the ordinary
    /// Rift Forager. Replaces ONLY twelve separate anatomical primitives in
    /// the review asset, leaving canonical living crescent mantle, facial
    /// features and external gear attached to their source sections.
    ///
    /// This is a single seamless volume, not a larger pile of lofts or props.
    /// It is a deterministic offline topology study. It is NOT a locked
    /// franchise sculpt, a gameplay mesh, or an approved material palette.
    /// Pure C# so CI can test watertightness and skinnability without Unity.
    /// </summary>
    internal static class RiftForagerAnatomyReconstruction
    {
        internal const string Revision="rift-forager-continuous-anatomy-surface-02";
        internal const float DefaultStep=.029f;

        internal sealed class Surface
        {
            internal readonly List<float[]> Vertices=new List<float[]>();
            internal readonly List<float[]> Normals=new List<float[]>();
            internal readonly List<float[]> UVs=new List<float[]>();
            internal readonly List<int> SkinIndices=new List<int>();
            internal readonly List<int> GarmentIndices=new List<int>();
            internal int TriangleCount=>(SkinIndices.Count+GarmentIndices.Count)/3;
        }

        // Only anatomical stand-in parts. Do NOT remove the separately modeled
        // natural mantle, eye/temple relief, ritual clothing or technical gear.
        internal static bool ReplacesOldBodyPart(string part)
        {
            if(string.IsNullOrEmpty(part))return false;
            if(part=="Avarin_Torso" || part=="Avarin_Pelvis" ||
               part=="Avarin_Neck" || part=="Avarin_TempleFace")
                return true;
            foreach(string prefix in new[]{"Avarin_Leg_","Avarin_Foot_",
                     "Avarin_Arm_","Avarin_Hand_"})
                if(part.StartsWith(prefix,StringComparison.Ordinal))return true;
            return false;
        }

        private static float Limit(float t,float min,float max)=>
            Math.Max(min,Math.Min(max,t));
        private static float Mix(float a,float b,float t)=>a+(b-a)*t;
        private static float Length(float x,float y,float z)=>
            (float)Math.Sqrt(x*x+y*y+z*z);

        private static float Ellipsoid(float x,float y,float z,
            float cx,float cy,float cz,float rx,float ry,float rz)
        {
            float dx=(x-cx)/rx,dy=(y-cy)/ry,dz=(z-cz)/rz;
            // Conservative continuous ellipsoid distance estimate.
            return (Length(dx,dy,dz)-1f)*Math.Min(rx,Math.Min(ry,rz));
        }

        private static float Capsule(float x,float y,float z,
            float ax,float ay,float az,float bx,float by,float bz,
            float radiusA,float radiusB)
        {
            float vx=bx-ax,vy=by-ay,vz=bz-az;
            float len=vx*vx+vy*vy+vz*vz;
            float t=Limit(((x-ax)*vx+(y-ay)*vy+(z-az)*vz)/
                Math.Max(.00001f,len),0,1);
            return Length(x-(ax+t*vx),y-(ay+t*vy),z-(az+t*vz))-
                Mix(radiusA,radiusB,t);
        }

        private static float Union(float a,float b,float softness)
        {
            float h=Limit(.5f+.5f*(b-a)/softness,0,1);
            return Mix(b,a,h)-softness*h*(1-h);
        }

        // The sculpt field is anatomical: a unified biped torso, shoulder
        // transitions, tapered arms, planted legs, organic face and natural
        // temple. It deliberately adds NO bonded companion, organ or gear.
        internal static float Field(float x,float y,float z)
        {
            float d=Ellipsoid(x,y,z,0,.947f,-.016f,.168f,.206f,.132f);
            d=Union(d,Ellipsoid(x,y,z,0,1.154f,-.024f,
                .180f,.250f,.141f),.10f);
            d=Union(d,Ellipsoid(x,y,z,0,1.343f,-.036f,
                .203f,.180f,.132f),.11f);
            d=Union(d,Ellipsoid(x,y,z,0,1.487f,-.012f,
                .074f,.128f,.076f),.085f);
            // Sculpted temple and naturally tapered chin; separate detailed
            // eyes, ridges and the living mantle remain real source geometry.
            d=Union(d,Ellipsoid(x,y,z,0,1.669f,.020f,
                .108f,.139f,.105f),.060f);
            d=Union(d,Ellipsoid(x,y,z,0,1.596f,.059f,
                .068f,.054f,.072f),.036f);
            d=Union(d,Ellipsoid(x,y,z,0,1.750f,-.012f,
                .090f,.054f,.079f),.035f);

            // Broad organic planes rather than disconnected facial detail
            // shapes. Integrate temple and cheekbone structure into the
            // actual ONE-piece head; the separately authored eye details
            // remain anchored to the source face.
            for(int sign=-1;sign<=1;sign+=2)
            {
                float s=sign;
                d=Union(d,Ellipsoid(x,y,z,s*.063f,1.636f,.058f,
                    .049f,.052f,.054f),.030f);
                d=Union(d,Ellipsoid(x,y,z,s*.081f,1.703f,.018f,
                    .033f,.070f,.057f),.033f);
                // Shoulder and chest transition planes stay inside the
                // anatomical silhouette rather than resembling a glued
                // shoulder-pad prop.
                d=Union(d,Ellipsoid(x,y,z,s*.116f,1.365f,.063f,
                    .082f,.039f,.051f),.036f);
                d=Union(d,Ellipsoid(x,y,z,s*.131f,1.294f,-.106f,
                    .078f,.112f,.051f),.050f);
                // Natural tissue volume follows the elbow and knee joints.
                d=Union(d,Ellipsoid(x,y,z,s*.255f,1.045f,.075f,
                    .048f,.078f,.050f),.035f);
                d=Union(d,Ellipsoid(x,y,z,s*.135f,.485f,.079f,
                    .067f,.059f,.053f),.036f);
                // Continuous tapered anterior foot rather than a rounded
                // anonymous ball; no extra toes or invented limbs.
                d=Union(d,Ellipsoid(x,y,z,s*.159f,.061f,.212f,
                    .052f,.036f,.069f),.025f);
            }

            for(int sign=-1;sign<=1;sign+=2)
            {
                float s=sign;
                // Natural shoulder-to-elbow transition. Subtle taper is
                // anatomical, not overlapping cylinders for each segment.
                d=Union(d,Ellipsoid(x,y,z,s*.166f,1.366f,-.021f,
                    .105f,.107f,.105f),.070f);
                d=Union(d,Capsule(x,y,z,s*.181f,1.387f,-.022f,
                    s*.250f,1.132f,.049f,.091f,.073f),.062f);
                d=Union(d,Capsule(x,y,z,s*.250f,1.135f,.049f,
                    s*.254f,.891f,.125f,.071f,.048f),.048f);
                d=Union(d,Ellipsoid(x,y,z,s*.253f,.845f,.158f,
                    .034f,.070f,.044f),.036f);

                // Unified hip/leg anatomy; knee and ankle articulations
                // are distinct but never physically disconnected.
                d=Union(d,Ellipsoid(x,y,z,s*.107f,.862f,-.027f,
                    .114f,.133f,.105f),.075f);
                d=Union(d,Capsule(x,y,z,s*.105f,.868f,-.021f,
                    s*.130f,.496f,.022f,.101f,.080f),.065f);
                d=Union(d,Ellipsoid(x,y,z,s*.134f,.482f,.025f,
                    .078f,.084f,.079f),.045f);
                d=Union(d,Capsule(x,y,z,s*.134f,.488f,.028f,
                    s*.155f,.140f,.074f,.074f,.049f),.047f);
                d=Union(d,Ellipsoid(x,y,z,s*.158f,.075f,.121f,
                    .065f,.057f,.144f),.043f);
            }
            // Shape tension follows anatomical landmark regions, not
            // random high-frequency noise or equipment-like bumps. No
            // additional triangles/components are layered on the surface.
            float upperBack=(float)Math.Exp(
                -(y-1.319f)*(y-1.319f)/.065f)*
                (float)Math.Exp(-(z+.099f)*(z+.099f)/.045f);
            float waist=(float)Math.Exp(
                -(y-1.037f)*(y-1.037f)/.042f)*
                (float)Math.Exp(-z*z/.055f);
            float neck=(float)Math.Exp(
                -(y-1.512f)*(y-1.512f)/.016f);
            float shoulder=(float)Math.Exp(
                -(y-1.303f)*(y-1.303f)/.083f)*
                Math.Min(1f,Math.Abs(x)*5f);
            float structure=
                .004f*waist*(float)Math.Cos(11f*x+2f*z)+
                .003f*upperBack*(float)Math.Sin(12f*x)+
                .0019f*shoulder*(float)Math.Cos(9f*y+3f*z)+
                .0011f*neck*(float)Math.Cos(17f*x);
            return d+structure;
        }

        /// <summary>
        /// Geometry-preserving UV seam split. The connected raw anatomical
        /// surface remains topologically watertight for deformation and
        /// testing; duplicated render vertices at the cylindrical U=0/1
        /// transition prevent interpolation across an entire atlas tile.
        /// </summary>
        internal static Surface MakeTextureReady(Surface raw)
        {
            if(raw==null)throw new ArgumentNullException(nameof(raw));
            if(raw.Vertices.Count!=raw.Normals.Count ||
                raw.Vertices.Count!=raw.UVs.Count)
                throw new ArgumentException(
                    "Anatomical positions/normals/UV lengths disagree.",
                    nameof(raw));

            var result=new Surface();
            for(int i=0;i<raw.Vertices.Count;i++)
            {
                result.Vertices.Add(raw.Vertices[i]);
                result.Normals.Add(raw.Normals[i]);
                result.UVs.Add(raw.UVs[i]);
            }
            var shifted=new Dictionary<int,int>();
            int Seam(int vertex)
            {
                if(shifted.TryGetValue(vertex,out int duplicated))
                    return duplicated;
                var uv=raw.UVs[vertex];
                int index=result.Vertices.Count;
                result.Vertices.Add(raw.Vertices[vertex]);
                result.Normals.Add(raw.Normals[vertex]);
                result.UVs.Add(new[]{uv[0]+1f,uv[1]});
                shifted.Add(vertex,index);
                return index;
            }

            // Cylindrical coordinates are undefined near their axis. A
            // triangle straddling that pole can cover >180 degrees even
            // after a 0/1 seam wrap: planar UVs are safer there, avoiding
            // huge normal-map streaks while leaving anatomical positions
            // and gradients unchanged. Cached vertices keep shared seams
            // stable for adjacent pole triangles.
            var planar=new Dictionary<int,int>();
            int Planar(int vertex)
            {
                if(planar.TryGetValue(vertex,out int existing))
                    return existing;
                var point=raw.Vertices[vertex];
                int index=result.Vertices.Count;
                result.Vertices.Add(point);
                result.Normals.Add(raw.Normals[vertex]);
                result.UVs.Add(new[]{
                    .5f+point[0]*2.0f+point[2]*.35f,
                    raw.UVs[vertex][1]
                });
                planar.Add(vertex,index);
                return index;
            }

            void Rewrite(List<int> original,List<int> destination)
            {
                for(int i=0;i<original.Count;i+=3)
                {
                    int a=original[i],b=original[i+1],c=original[i+2];
                    float u0=raw.UVs[a][0],u1=raw.UVs[b][0],
                        u2=raw.UVs[c][0];
                    float min=Math.Min(u0,Math.Min(u1,u2));
                    float max=Math.Max(u0,Math.Max(u1,u2));
                    bool crosses=max-min>.5f;
                    int aa=crosses && u0<.5f?Seam(a):a;
                    int bb=crosses && u1<.5f?Seam(b):b;
                    int cc=crosses && u2<.5f?Seam(c):c;
                    float minU=Math.Min(result.UVs[aa][0],
                        Math.Min(result.UVs[bb][0],result.UVs[cc][0]));
                    float maxU=Math.Max(result.UVs[aa][0],
                        Math.Max(result.UVs[bb][0],result.UVs[cc][0]));
                    if(maxU-minU>.5f)
                    {
                        // True polar singularity: shifting by one map tile
                        // still cannot produce locally continuous UVs.
                        aa=Planar(a);bb=Planar(b);cc=Planar(c);
                    }
                    destination.Add(aa);
                    destination.Add(bb);
                    destination.Add(cc);
                }
            }

            Rewrite(raw.GarmentIndices,result.GarmentIndices);
            Rewrite(raw.SkinIndices,result.SkinIndices);
            if(result.TriangleCount!=raw.TriangleCount ||
                result.Vertices.Count>raw.Vertices.Count*3)
                throw new InvalidOperationException(
                    "UV seam correction damaged anatomical triangle counts.");
            return result;
        }

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
        internal static Surface Generate(float step=DefaultStep)
        {
            if(step<.021f || step>.07f || float.IsNaN(step) ||
                float.IsInfinity(step))
                throw new ArgumentOutOfRangeException(nameof(step));
            const float minX=-.50f,minY=-.055f,minZ=-.33f;
            const float maxX=.50f,maxY=1.895f,maxZ=.555f;
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
                values[Id(x,y,z)]=Field(
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
                float gx=Field(px+epsilon,py,pz)-Field(px-epsilon,py,pz);
                float gy=Field(px,py+epsilon,pz)-Field(px,py-epsilon,pz);
                float gz=Field(px,py,pz+epsilon)-Field(px,py,pz-epsilon);
                float norm=Math.Max(1e-8f,Length(gx,gy,gz));
                int index=mesh.Vertices.Count;
                mesh.Vertices.Add(new[]{px,py,pz});
                mesh.Normals.Add(new[]{gx/norm,gy/norm,gz/norm});
                // Cylindrical anatomical UV parametrization suitable for
                // nonfinal fabric and skin inspection; seams await hand UVs.
                float u=(float)(Math.Atan2(px,pz)/(2*Math.PI)+.5);
                mesh.UVs.Add(new[]{u,py/1.85f});
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
                float y=(p[1]+q[1]+t[1])/3f;
                float x=Math.Abs((p[0]+q[0]+t[0])/3f);
                bool bareTemple=y>1.485f;
                bool bareHand=x>.195f && y>.78f && y<1.005f;
                var target=bareTemple||bareHand?
                    mesh.SkinIndices:mesh.GarmentIndices;
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
            if(mesh.TriangleCount<3000 || mesh.SkinIndices.Count==0 ||
                mesh.GarmentIndices.Count==0 || mesh.Vertices.Count>250000)
                throw new InvalidOperationException(
                    "Anatomical surface failed minimum coherent resolution.");
            return mesh;
        }
    }
}
