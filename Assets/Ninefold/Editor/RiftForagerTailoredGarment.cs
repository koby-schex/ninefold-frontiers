using System;
using System.Collections.Generic;

namespace Ninefold.Editor
{
    /// <summary>
    /// Two deliberately asymmetrical rear field-cloth panels for the provisional
    /// Rift Forager ArtReview rig. Actual triangulated, double-sided *thick*
    /// fabric, not an overlayed texture, particle, primitive cylinder or mantle.
    /// The original natural Avarin sensory crescent and salvaged gear are
    /// preserved separately. No new biology, Arcana or gameplay equipment.
    /// </summary>
    internal static class RiftForagerTailoredGarment
    {
        internal const string Revision="rift-forager-tailored-fieldcloth-03";
        internal const int Columns=15;
        internal const int Rows=23;

        internal sealed class Surface
        {
            internal readonly List<float[]> Vertices=new List<float[]>();
            internal readonly List<float[]> Normals=new List<float[]>();
            internal readonly List<float[]> UVs=new List<float[]>();
            internal readonly List<int> ClothIndices=new List<int>();
            internal readonly List<int> EdgeIndices=new List<int>();
            internal int TriangleCount=>(ClothIndices.Count+EdgeIndices.Count)/3;
        }

        // Shell is behind the torso, under and CLEAR of the living sensory
        // crescent, not a biological mantle or a third companion.
        internal static float[] PanelPoint(int panel,int column,int row,bool inside)
        {
            if(panel<0 || panel>1 || column<0 || column>=Columns ||
               row<0 || row>=Rows)
                throw new ArgumentOutOfRangeException(nameof(panel));
            float u=(float)column/(Columns-1),v=(float)row/(Rows-1);
            bool left=panel==0;
            float leftEdge=left?-.172f:.020f;
            float rightEdge=left?-.013f:.162f;
            float spread=(float)Math.Sin(Math.PI*v)*.025f;
            float x=leftEdge+(rightEdge-leftEdge)*u+
                (left?-1f:1f)*spread*(.25f+.75f*u);
            // Asymmetric hanging hems are visibly different from one another.
            // Avoid perfectly straight sheet edges and disconnected ribbons.
            float length=left?.350f:.445f;
            float y=1.083f-length*v+
                (.017f*(float)Math.Sin(2.2f*Math.PI*u+.7f*panel))*v*v;
            float fold=(float)Math.Sin(4f*Math.PI*u+1.4f*panel+.7f*v);
            float bias=(float)Math.Sin(2f*Math.PI*u+1.3f*v+.8f*panel);
            float z=-.142f-.034f*v-.011f*v*v-
                .016f*(float)Math.Sin(Math.PI*v)*fold-
                .006f*v*v*bias;
            // A smooth, continuous rest-fit solve along the rear normal.
            // Former fixed 8-mm steps made jagged diagonal surface stripes
            // when neighboring fabric columns crossed an anatomy threshold.
            // Bisection avoids quantized stepping and keeps the cloth outside.
            if(RiftForagerAnatomyReconstruction.Field(x,y,z)<.011f)
            {
                float near=z;
                float far=z-.35f;
                if(RiftForagerAnatomyReconstruction.Field(x,y,far)<.011f)
                    throw new InvalidOperationException(
                        "Rear garment could not clear the original anatomy.");
                for(int i=0;i<21;i++)
                {
                    float mid=(near+far)*.5f;
                    if(RiftForagerAnatomyReconstruction.Field(x,y,mid)<.011f)
                        near=mid;
                    else
                        far=mid;
                }
                z=far;
            }
            if(RiftForagerAnatomyReconstruction.Field(x,y,z)<.009f ||
               z<-.49f)
                throw new InvalidOperationException(
                    "Fieldcloth intersects anatomy or floats off the back.");
            // Two physical sheets with stitched perimeter, 7 mm apart.
            if(inside) z+=.003f;
            else z-=.004f;
            return new[]{x,y,z};
        }

        internal static Surface Generate()
        {
            var mesh=new Surface();
            const int n=Columns*Rows;
            for(int panel=0;panel<2;panel++)
            {
                int start=mesh.Vertices.Count;
                // Back-facing and body-facing sheets share a sealed perimeter.
                for(int layer=0;layer<2;layer++)
                for(int r=0;r<Rows;r++)
                for(int c=0;c<Columns;c++)
                {
                    bool inside=layer==1;
                    mesh.Vertices.Add(PanelPoint(panel,c,r,inside));
                    mesh.UVs.Add(new[]{(float)c/(Columns-1),
                        (float)r/(Rows-1)});
                    mesh.Normals.Add(new[]{0f,0f,inside?1f:-1f});
                }

                int At(int layer,int row,int col)=>
                    start+layer*n+row*Columns+col;
                void Face(List<int> indices,int a,int b,int c)
                {
                    if(a==b || b==c || a==c)
                        throw new InvalidOperationException("Collapsed cloth face.");
                    indices.Add(a);indices.Add(b);indices.Add(c);
                }
                for(int r=0;r<Rows-1;r++)
                for(int c=0;c<Columns-1;c++)
                {
                    int a=At(0,r,c),b=At(0,r,c+1);
                    int d=At(0,r+1,c),e=At(0,r+1,c+1);
                    Face(mesh.ClothIndices,a,b,d);
                    Face(mesh.ClothIndices,b,e,d);
                    // Opposing face winding is essential for lighting.
                    a=At(1,r,c);b=At(1,r,c+1);
                    d=At(1,r+1,c);e=At(1,r+1,c+1);
                    Face(mesh.ClothIndices,a,d,b);
                    Face(mesh.ClothIndices,b,d,e);
                }
                // Join BOTH physical fabric surfaces along every perimeter edge.
                // The edging is separate dark cloth material, not extra props.
                void Stitch(int rowA,int colA,int rowB,int colB)
                {
                    int a=At(0,rowA,colA),b=At(0,rowB,colB);
                    int ia=At(1,rowA,colA),ib=At(1,rowB,colB);
                    Face(mesh.EdgeIndices,b,a,ia);
                    Face(mesh.EdgeIndices,b,ia,ib);
                }
                for(int c=0;c<Columns-1;c++)
                {
                    Stitch(0,c,0,c+1);
                    Stitch(Rows-1,c+1,Rows-1,c);
                }
                for(int r=0;r<Rows-1;r++)
                {
                    Stitch(r,Columns-1,r+1,Columns-1);
                    Stitch(r+1,0,r,0);
                }
            }
            if(mesh.Vertices.Count!=4*Columns*Rows ||
               mesh.UVs.Count!=mesh.Vertices.Count ||
               mesh.Normals.Count!=mesh.Vertices.Count)
                throw new InvalidOperationException("Unexpected fieldcloth buffers.");

            // Actual area-weighted mesh normals, not the old uniform +/-Z
            // normals that made curved folds shade like a flat cutout. Both
            // cloth skins and the separately material-assigned sewn border
            // contribute geometric face normals at their shared positions.
            var accumulated=new float[mesh.Vertices.Count][];
            for(int i=0;i<accumulated.Length;i++)
                accumulated[i]=new float[3];
            void Accumulate(List<int> faces)
            {
                for(int i=0;i<faces.Count;i+=3)
                {
                    int a=faces[i],b=faces[i+1],c=faces[i+2];
                    var p=mesh.Vertices[a];
                    var q=mesh.Vertices[b];
                    var t=mesh.Vertices[c];
                    float ux=q[0]-p[0],uy=q[1]-p[1],uz=q[2]-p[2];
                    float vx=t[0]-p[0],vy=t[1]-p[1],vz=t[2]-p[2];
                    float nx=uy*vz-uz*vy;
                    float ny=uz*vx-ux*vz;
                    float nz=ux*vy-uy*vx;
                    for(int k=0;k<3;k++)
                    {
                        int idx=faces[i+k];
                        accumulated[idx][0]+=nx;
                        accumulated[idx][1]+=ny;
                        accumulated[idx][2]+=nz;
                    }
                }
            }
            Accumulate(mesh.ClothIndices);
            Accumulate(mesh.EdgeIndices);
            for(int i=0;i<mesh.Vertices.Count;i++)
            {
                var gradient=accumulated[i];
                float len=(float)Math.Sqrt(
                    gradient[0]*gradient[0]+gradient[1]*gradient[1]+
                    gradient[2]*gradient[2]);
                if(len<1e-9f)
                    throw new InvalidOperationException("Invalid stitched cloth normal.");
                mesh.Normals[i]=new[]{
                    gradient[0]/len,gradient[1]/len,gradient[2]/len};
            }
            return mesh;
        }
    }
}
