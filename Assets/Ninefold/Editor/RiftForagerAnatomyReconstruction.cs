using System;
using System.Collections.Generic;
using System.Linq;

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
        internal const string Revision="rift-forager-continuous-anatomy-01";
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
            // Controlled very-low-amplitude sculpt-like volume refinement:
            // anatomical broad plane transitions, not stochastic micro-noise
            // or unnatural protruding scales.
            float shoulder=(float)Math.Exp(
                -(y-1.31f)*(y-1.31f)/.095f)*
                Math.Min(1f,Math.Abs(x)*5f);
            float structure=.0025f*(float)Math.Sin(13f*y+4f*z)*
                shoulder+.0017f*(float)Math.Sin(9f*z+7f*y)*
                (1f-shoulder);
            return d+structure;
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
            int strideY=nx,strideZ=nx*ny;
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
            float[][] pointCache=new float[8][];
            int[] cubeIds=new int[8];
            float[] cubeValues=new float[8];
            float[] candidateNormal=new float[3];

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
                if(cx*cx+cy*cy+cz*cz<1e-13f)return;
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
                    int[] inner=new int[4],outer=new int[4];
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
