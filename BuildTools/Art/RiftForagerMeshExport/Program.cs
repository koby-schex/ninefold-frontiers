using System;
using System.Globalization;
using System.IO;
using System.Text;
using Ninefold.Editor;

internal static class Program
{
    private static readonly CultureInfo Invariant=CultureInfo.InvariantCulture;

    private static string Number(float value)
    {
        if(float.IsNaN(value)||float.IsInfinity(value))
            throw new InvalidDataException("Invalid anatomy vertex/UV/normal.");
        return value.ToString("F8",Invariant);
    }

    private static void WriteTriplet(StreamWriter output,string prefix,float[] v)
    {
        output.Write(prefix);
        for(int i=0;i<v.Length;i++)
        {
            output.Write(' ');
            output.Write(Number(v[i]));
        }
        output.WriteLine();
    }

    private static int WriteFaces(StreamWriter output,string name,
        System.Collections.Generic.List<int> indices,int vertices)
    {
        if(indices.Count%3!=0)throw new InvalidDataException("Incomplete triangles.");
        output.WriteLine("usemtl "+name);
        int triangleCount=0;
        for(int i=0;i<indices.Count;i+=3)
        {
            output.Write("f");
            for(int j=0;j<3;j++)
            {
                int index=indices[i+j];
                if(index<0||index>=vertices)
                    throw new InvalidDataException("Out of range triangle index");
                int obj=index+1;
                output.Write(" "+obj+"/"+obj+"/"+obj);
            }
            output.WriteLine();
            triangleCount++;
        }
        return triangleCount;
    }

    private static int Main(string[] args)
    {
        try
        {
            if(args.Length!=1)
            {
                Console.Error.WriteLine(
                    "Usage: RiftForagerMeshExport <output-anatomy.obj>");
                return 2;
            }

            // The EXACT pure C# geometry generator used in the Unity
            // AshboundRigReview. No port or loosely synchronized remake.
            var raw=RiftForagerAnatomyReconstruction.Generate();
            var mesh=RiftForagerAnatomyReconstruction.MakeTextureReady(raw);
            if(mesh.Vertices.Count!=mesh.Normals.Count||
                mesh.Vertices.Count!=mesh.UVs.Count)
                throw new InvalidDataException("Geometry/UV buffers disagree.");
            if(mesh.Vertices.Count<4000||mesh.TriangleCount<8000)
                throw new InvalidDataException("Source anatomy is unexpectedly small.");
            if(mesh.TriangleCount!=raw.TriangleCount)
                throw new InvalidDataException("UV seam correction dropped faces.");
            string target=Path.GetFullPath(args[0]);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using(var writer=new StreamWriter(target,false,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier:false)))
            {
                writer.WriteLine("# Ninefold - unapproved Rift Forager ArtReview model");
                writer.WriteLine("# Reconstructed body from EXACT Unity C# surface source");
                writer.WriteLine("# NO animations, skin weights or Unity shader are baked.");
                writer.WriteLine("# Source revision: "+RiftForagerAnatomyReconstruction.Revision);
                writer.WriteLine("mtllib RiftForager.mtl");
                writer.WriteLine("o RiftForager_Connected_ArtReviewBody");
                foreach(var point in mesh.Vertices)WriteTriplet(writer,"v",point);
                foreach(var uv in mesh.UVs)WriteTriplet(writer,"vt",uv);
                foreach(var normal in mesh.Normals)WriteTriplet(writer,"vn",normal);
                writer.WriteLine("s 1");
                int cloth=WriteFaces(writer,"garment",mesh.GarmentIndices,
                    mesh.Vertices.Count);
                int skin=WriteFaces(writer,"skin",mesh.SkinIndices,
                    mesh.Vertices.Count);
                if(cloth+skin!=mesh.TriangleCount)
                    throw new InvalidDataException("Exported triangle mismatch.");
            }
            Console.WriteLine("EXPORT "+target);
            Console.WriteLine("SOURCE_REVISION "+RiftForagerAnatomyReconstruction.Revision);
            Console.WriteLine("CONNECTED_RAW_VERTICES "+raw.Vertices.Count);
            Console.WriteLine("RENDER_VERTICES "+mesh.Vertices.Count);
            Console.WriteLine("CLOTH_TRIANGLES "+mesh.GarmentIndices.Count/3);
            Console.WriteLine("SKIN_TRIANGLES "+mesh.SkinIndices.Count/3);
            Console.WriteLine("TOTAL_TRIANGLES "+mesh.TriangleCount);
            return 0;
        }
        catch(Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
