using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Content;
using S=Ninefold.Core.Content.FirstEncounterSculpts;

internal static partial class Program
{
    private static IEnumerable<(string Name,Action Run)> FirstEncounterSculptTests()
    {
        yield return ("First three distinct named hostile encounters remain ordered and scoped",
            EncounterSculptOrder);
        yield return ("All three actual runtime enemy sculpts are closed connected finite surfaces",
            EncounterSculptTopology);
        yield return ("Wildlife species have distinctive six-limb profiles and all joints influence skin",
            EncounterWildlifeIdentity);
        yield return ("Forager connected body retains bipedal anatomical silhouette and visible facial relief",
            EncounterForagerIdentity);
        yield return ("Forager actually wears two closed fitted fieldcloth panels without rest penetration",
            EncounterForagerCloth);
        yield return ("Wildlife eyes are fitted closed oval geometry with a real outlined rim",
            EncounterEyeShells);
    }
    private static void EncounterSculptOrder()
    {
        var c=WorldFiveTutorialWildlifeV1.Create();
        var ids=WorldFiveOpeningTenPrototype.MissionIds;
        Equal(true,c.Missions[ids[6]].Actors.All(x=>x.UnitId==
            WorldFiveTutorialWildlifeV1.SeamstalkerId));
        Equal(true,c.Missions[ids[7]].Actors.All(x=>x.UnitId==
            WorldFiveTutorialWildlifeV1.SillrunnerId));
        Equal(WorldFiveActOnePrototype.ScoutId,
            c.Missions[WorldFiveActOnePrototype.FirstMission].Actors[0].UnitId);
        Equal(WorldFiveActOnePrototype.StrikerId,
            c.Missions[WorldFiveActOnePrototype.FirstMission].Actors[1].UnitId);
    }
    private static void EncounterSculptTopology()
    {
        foreach(S.Enemy kind in Enum.GetValues(typeof(S.Enemy)))
        {
            var m=S.Tactical(kind);
            if(!ReferenceEquals(m,S.Tactical(kind)) || m.TriangleCount<3000 ||
                m.TriangleCount>75000 || m.Vertices.Count>50000)
                throw new Exception(kind+" unexpected cached mesh budget.");
            Equal(m.Vertices.Count,m.Normals.Count);
            Equal(m.Vertices.Count,m.UVs.Count);
            var uses=new Dictionary<ulong,int>();
            int[] parent=Enumerable.Range(0,m.Vertices.Count).ToArray();
            int Root(int i)
            {
                while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}
                return i;
            }
            void Edge(int a,int b)
            {
                ulong key=((ulong)(uint)Math.Min(a,b)<<32)|(uint)Math.Max(a,b);
                uses[key]=uses.TryGetValue(key,out int n)?n+1:1;
                parent[Root(a)]=Root(b);
            }
            var jointCoverage=new HashSet<int>();
            foreach(var tris in m.Indices)
            {
                if(tris.Count<3 || tris.Count%3!=0)
                    throw new Exception(kind+" missing surface material region.");
                for(int i=0;i<tris.Count;i+=3)
                {
                    int a=tris[i],b=tris[i+1],c=tris[i+2];
                    if(a<0||b<0||c<0||a>=m.Vertices.Count||b>=m.Vertices.Count||
                        c>=m.Vertices.Count||a==b||a==c||b==c)
                        throw new Exception(kind+" bad indexed face.");
                    Edge(a,b);Edge(b,c);Edge(c,a);
                }
            }
            int open=uses.Count(e=>e.Value!=2);
            int components=Enumerable.Range(0,parent.Length).Select(Root).Distinct().Count();
            if(open!=0||components!=1)
                throw new Exception(kind+" must be one closed connected skin; open="+open+
                    " components="+components);
            for(int i=0;i<m.Vertices.Count;i++)
            {
                var p=m.Vertices[i];var n=m.Normals[i];var uv=m.UVs[i];
                if(p.Concat(n).Concat(uv).Any(v=>float.IsNaN(v)||float.IsInfinity(v)))
                    throw new Exception(kind+" nonfinite coordinates.");
                if(Math.Abs(n[0]*n[0]+n[1]*n[1]+n[2]*n[2]-1)>2e-3)
                    throw new Exception(kind+" non-unit gradient normal.");
                var w=S.Weights(kind,p[0],p[1],p[2]);
                if(w.A<0||w.B<0||w.A>=S.BoneCount(kind)||w.B>=S.BoneCount(kind)||
                    float.IsNaN(w.WeightA)||w.WeightA<0||w.WeightA>1)
                    throw new Exception(kind+" invalid anatomical skin binding.");
                if(w.WeightA>.03f)jointCoverage.Add(w.A);
                if(w.WeightA<.97f)jointCoverage.Add(w.B);
            }
            Equal(S.BoneCount(kind),jointCoverage.Count);
            Console.WriteLine("INFO "+kind+" runtime "+m.Vertices.Count+" vertices, "+
                m.TriangleCount+" triangles, "+jointCoverage.Count+" weighted joints.");
        }
    }
    private static void EncounterWildlifeIdentity()
    {
        var stalk=S.Tactical(S.Enemy.Seamstalker);
        var run=S.Tactical(S.Enemy.Sillrunner);
        float Rear(S.Surface m)=>m.Vertices.Min(p=>p[2]);
        float Crown(S.Surface m)=>m.Vertices.Max(p=>p[1]);
        if(Rear(stalk)>Rear(run)-.08f || Crown(stalk)<Crown(run)+.035f)
            throw new Exception("Stalker's tapered tail/split crown not distinct from runner.");
        foreach(var kind in new[]{S.Enemy.Seamstalker,S.Enemy.Sillrunner})
        for(int limb=0;limb<6;limb++)
        {
            var h=S.WildlifeJoint(kind,limb,false);
            var k=S.WildlifeJoint(kind,limb,true);
            if(S.Field(kind,h[0],h[1],h[2])>=0 ||
               S.Field(kind,k[0],k[1],k[2])>=0)
                throw new Exception(kind+" limb disconnected from physical skin.");
            var heel=S.Weights(kind,k[0],-.58f,k[2]);
            Equal(4+limb*2,heel.A);
        }
    }
    private static void EncounterEyeShells()
    {
        foreach(var kind in new[]{S.Enemy.Seamstalker,S.Enemy.Sillrunner})
        {
            var eyes=S.EyeShells(kind);
            if(eyes.TriangleCount<1100 || eyes.Indices[0].Count!=0 ||
                eyes.Indices[1].Count==0 || eyes.Indices[2].Count==0)
                throw new Exception("Wildlife eye uses material patches instead of physical shell.");
            var uses=new Dictionary<ulong,int>();
            void Edge(int a,int b)
            {
                ulong key=((ulong)(uint)Math.Min(a,b)<<32)|(uint)Math.Max(a,b);
                uses[key]=uses.TryGetValue(key,out int n)?n+1:1;
            }
            foreach(var group in eyes.Indices)
            for(int i=0;i<group.Count;i+=3)
            {
                Edge(group[i],group[i+1]);Edge(group[i+1],group[i+2]);Edge(group[i+2],group[i]);
            }
            if(uses.Any(e=>e.Value!=2))
                throw new Exception("Eye lenses have an open rim.");
            for(int i=0;i<eyes.Vertices.Count;i++)
            {
                var p=eyes.Vertices[i];var n=eyes.Normals[i];
                if(Math.Abs(S.Field(kind,p[0],p[1],p[2]))>.026f||
                    Math.Abs(n[0]*n[0]+n[1]*n[1]+n[2]*n[2]-1f)>.002f)
                    throw new Exception("Eye shell detached from body or lacks a unit normal.");
            }
        }
    }
    private static void EncounterForagerCloth()
    {
        var cloth=FirstEncounterForagerCloth.Tactical();
        if(cloth.TriangleCount<1900||cloth.TriangleCount>4000||
            cloth.Indices[0].Count!=0||cloth.Indices[1].Count==0||
            cloth.Indices[2].Count==0)
            throw new Exception("Live fieldcloth lost actual fabric/trim geometry.");
        var uses=new Dictionary<ulong,int>();
        void Edge(int a,int b)
        {
            ulong key=((ulong)(uint)Math.Min(a,b)<<32)|(uint)Math.Max(a,b);
            uses[key]=uses.TryGetValue(key,out int n)?n+1:1;
        }
        foreach(var group in cloth.Indices)
        for(int k=0;k<group.Count;k+=3)
        {
            int a=group[k],b=group[k+1],c=group[k+2];
            Edge(a,b);Edge(b,c);Edge(c,a);
        }
        if(uses.Any(x=>x.Value!=2))
            throw new Exception("The runtime garment must have a closed sewn perimeter.");
        foreach(var point in cloth.Vertices)
            if(S.Field(S.Enemy.RiftForager,point[0],point[1],point[2])<.002f ||
                point[1]>-.12f||point[1]<-.57f)
                throw new Exception("The live fieldcloth penetrates the body or upper gear.");
    }

    private static void EncounterForagerIdentity()
    {
        var kind=S.Enemy.RiftForager;
        var m=S.Tactical(kind);
        if(m.Vertices.Min(p=>p[1])>-.70f || m.Vertices.Max(p=>p[1])<.80f ||
            m.Vertices.Max(p=>Math.Abs(p[0]))<.30f)
            throw new Exception("Forager changed battle scale or lost arms/head.");
        if(S.Field(kind,0,.65f,.05f)>=0 || S.Field(kind,0,.10f,0)>=0)
            throw new Exception("Forager skull/trunk is hollow.");
        foreach(float side in new[]{-1f,1f})
        {
            if(S.Field(kind,side*.16f,-.71f,.12f)>=0 ||
               S.Field(kind,side*.31f,-.24f,.13f)>=0)
                throw new Exception("Forager lost connected foot/hand.");
            float eye=S.Field(kind,side*.103f,.692f,.166f);
            if(eye<-.035f || eye>.06f)
                throw new Exception("Forager authored eye would bury or float away.");
        }
    }
}
