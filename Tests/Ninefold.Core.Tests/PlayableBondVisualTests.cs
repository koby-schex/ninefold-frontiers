using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Ninefold.Core.Content;
using R=Ninefold.Core.Content.PlayableBondRig;
using S=Ninefold.Core.Content.PlayableBondSource;

internal static partial class Program
{
    private static IEnumerable<(string Name,Action Run)> PlayableBondVisualTests()
    {
        yield return ("The three starter unit IDs and fourth standard select only the World Five squad visuals",
            PlayableStarterMapping);
        yield return ("All packed source assets match original bytes and preserve all four complete models",
            PlayablePackedModels);
        yield return ("Actual source geometry has four/eight contact rigs, separate partners, and truly legless Vaskel",
            PlayableRigCoverage);
        yield return ("Olyra's exact protective pose correspondence preserves five membrane lobes and the independent partner",
            PlayableShelterMorph);
        yield return ("All four source-weighted rigs have bounded rest, movement, impact and defeat poses and tactical bounds",
            PlayableRigPoses);
    }
    private static string BondRoot(string model)
        =>Path.Combine(Directory.GetParent(Directory.GetParent(StriderRoot()).FullName).FullName,
            "Assets","Ninefold","Resources","WorldFiveBondModels",model);
    private static S.Model BondModel(R.Bond kind,bool shelter=false)
    {
        string root=BondRoot(kind.ToString());
        var surfaces=S.ReadMaterials(File.ReadLines(Path.Combine(root,kind+".mtl.bytes")),
            p=>File.Exists(Path.Combine(root,p+".bytes")));
        return S.ReadMesh(File.ReadLines(Path.Combine(root,
            (shelter?"LivingVeilProtective":kind.ToString())+".obj.bytes")),surfaces);
    }
    private static readonly Lazy<S.Model>[] BondModels=Enumerable.Range(0,4)
        .Select(i=>new Lazy<S.Model>(()=>BondModel((R.Bond)i))).ToArray();

    private static void PlayableStarterMapping()
    {
        var catalog=WorldFiveTutorialWildlifeV1.Create();
        Equal(3,catalog.StarterUnits.Count);
        string[] names={"fixture-a-1","fixture-a-2","fixture-a-3","fixture-a-4"};
        foreach(string id in catalog.StarterUnits)Equal(true,names.Take(3).Contains(id));
        for(int i=0;i<names.Length;i++)
        {
            Equal(true,R.TrySelect(WorldFiveOpeningTenPrototype.MissionIds[0],names[i],true,out var b));
            Equal((R.Bond)i,b);
            Equal(true,R.TrySelect(WorldFiveStorySequence.FirstCampaignMissionId,names[i],true,out _));
            Equal(false,R.TrySelect("fixture-opening",names[i],true,out _));
            Equal(false,R.TrySelect(WorldFiveOpeningTenPrototype.MissionIds[0],names[i],false,out _));
        }
        Equal(false,R.TrySelect(WorldFiveOpeningTenPrototype.MissionIds[0],"fixture-a-5",true,out _));
        Equal(false,R.TrySelect(WorldFiveOpeningTenPrototype.MissionIds[0],"fixture-npc",false,out _));
        Equal(false,R.TrySelect("w5-story-999",names[0],true,out _));
    }

    private static void PlayablePackedModels()
    {
        string root=Directory.GetParent(StriderRoot()).FullName;
        int[] vertices={40646,47136,30030,35854};
        int[] faces={80556,93768,59720,70968};
        int[] parts={184,126,85,185};
        for(int i=0;i<4;i++)
        {
            string key=((R.Bond)i).ToString(),packed=BondRoot(key);
            foreach(string file in Directory.EnumerateFiles(Path.Combine(root,key),"*",SearchOption.AllDirectories))
            {
                string ext=Path.GetExtension(file);
                if(ext!=".obj"&&ext!=".mtl"&&ext!=".png")continue;
                string relative=Path.GetRelativePath(Path.Combine(root,key),file);
                if(ext==".png"&&!relative.StartsWith("Textures"+Path.DirectorySeparatorChar,StringComparison.Ordinal))continue;
                string copy=Path.Combine(packed,relative+".bytes");
                if(!File.Exists(copy)||!SHA256.HashData(File.ReadAllBytes(copy)).SequenceEqual(SHA256.HashData(File.ReadAllBytes(file))))
                    throw new Exception("Stale runtime source asset: "+key+"/"+relative);
            }
            var m=BondModels[i].Value;
            Equal(vertices[i],m.SourceVertices);Equal(faces[i],m.Triangles);Equal(parts[i],m.Parts);
            Equal(m.Triangles*3,m.Sections.Sum(s=>s.Indices.Count));
            if(m.Sections.Count<8||m.Sections.Count>38)throw new Exception("Draw packet batching invalid.");
            Console.WriteLine("INFO "+key+" "+m.Parts+" source parts, "+m.Triangles+
                " triangles, "+m.Sections.Count+" batched sections.");
        }
    }
    private static void PlayableRigCoverage()
    {
        foreach(R.Bond kind in Enum.GetValues(typeof(R.Bond)))
        {
            var model=BondModels[(int)kind].Value;
            var plan=R.Build(kind,model);
            var names=model.Sections.SelectMany(s=>s.SourceParts).Distinct().ToArray();
            var coverage=new HashSet<int>();
            int limbs=R.Limbs(kind);
            Equal(kind==R.Bond.GreatStrider?4:kind==R.Bond.Burdenbeast?8:0,limbs);
            if(!names.Contains("Avarin_Head")||!names.Any(n=>n.StartsWith("Avarin_Crescent",StringComparison.Ordinal)))
                throw new Exception(kind+" lost original Avarin partner.");
            if(kind==R.Bond.Riftwalker)
            {
                if(names.Any(n=>n.StartsWith("Vaskel_",StringComparison.Ordinal)&&
                    new[]{"Limb","Leg","Foot","Contact","Ankle","WaistWeb"}.Any(n.Contains)))
                    throw new Exception("Legless Vaskel regressed.");
                float minimum=model.Sections.SelectMany(s=>s.Vertices.Select((p,i)=>
                    new{p,name=s.SourceParts[i]})).Where(x=>x.name.StartsWith("Vaskel_",StringComparison.Ordinal))
                    .Min(x=>x.p[1]);
                if(minimum<=.4f)throw new Exception("Vaskel ground clearance lost.");
            }
            foreach(var s in model.Sections)
            for(int i=0;i<s.Vertices.Count;i++)
            {
                var p=R.V(s.Vertices[i]);var w=R.Skin(plan,s.SourceParts[i],p);
                if(w.A<0||w.B<0||w.A>=plan.Pivots.Length||w.B>=plan.Pivots.Length||
                    w.W<0||w.W>1||float.IsNaN(w.W))
                    throw new Exception("Invalid actual source influence");
                if(w.W>.001f)coverage.Add(w.A);
                if(w.W<.999f)coverage.Add(w.B);
                int l=R.LimbIndex(kind,s.SourceParts[i]);
                if(l>=0&&R.IsContact(s.SourceParts[i]))
                {Equal(R.FirstLimb+3*l+2,w.A);Equal(1f,w.W);}
                if(kind==R.Bond.GreatStrider&&R.IsPartnerPart(s.SourceParts[i]))
                    Equal(R.Partner,w.A);
            }
            Equal(true,coverage.Contains(R.Body));
            Equal(true,coverage.Contains(R.Partner));
            for(int l=0;l<limbs;l++)
                for(int j=0;j<3;j++)Equal(true,coverage.Contains(R.FirstLimb+l*3+j));
            if(kind!=R.Bond.GreatStrider)
            {
                Equal(true,coverage.Contains(R.PartnerHead));
                Equal(true,coverage.Contains(R.LeftLeg));Equal(true,coverage.Contains(R.RightLeg));
                Equal(true,coverage.Contains(R.LeftArm));Equal(true,coverage.Contains(R.RightArm));
            }
            if(kind==R.Bond.Riftwalker)Equal(true,coverage.Contains(R.Tail));
        }
    }

    private static void PlayableShelterMorph()
    {
        var rest=BondModels[(int)R.Bond.LivingVeil].Value;
        var shelter=BondModel(R.Bond.LivingVeil,true);
        S.ValidateCorrespondence(rest,shelter);
        Equal(5,rest.Sections.SelectMany(s=>s.SourceParts).Distinct()
            .Count(s=>s.StartsWith("Olyra_Lobe_",StringComparison.Ordinal)));
        int moved=0;
        foreach(var s in rest.Sections)
        for(int i=0;i<s.Vertices.Count;i++)
        {
            var p=R.V(s.Vertices[i]);
            var q=R.ProtectiveVertex(s.SourceParts[i],p,R.V(shelter.RawVertices[s.SourceIndices[i]]));
            if(!R.IsOlyra(s.SourceParts[i]))Equal(p,q);
            else if(Vector3.Distance(p,q)>.10f)moved++;
        }
        if(moved<1000)throw new Exception("Protective shape is not real source deformation.");
        Equal(0f,R.Shelter(0,true));Equal(1f,R.Shelter(.5f,true));
        Equal(0f,R.Shelter(1,true));Equal(0f,R.Shelter(.5f,false));
        bool failed=false;
        try{S.ValidateCorrespondence(rest,BondModels[0].Value);}
        catch(InvalidDataException){failed=true;}
        Equal(true,failed);
    }

    private static void PlayableRigPoses()
    {
        foreach(R.Bond kind in Enum.GetValues(typeof(R.Bond)))
        {
            var plan=R.Build(kind,BondModels[(int)kind].Value);
            var s=plan.Max-plan.Min;
            float scale=R.Fit(plan.Min,plan.Max,1.45f,1.42f);
            if(scale<=0||s.Y*scale>1.451f||Math.Max(s.X,s.Z)*scale>1.421f)
                throw new Exception("Model exceeding tactical visual budget.");
            for(int b=0;b<plan.Pivots.Length;b++)
            {
                var rest=R.Pose(plan,b,0,0,0,0,0,0);
                Equal(Vector3.Zero,rest.Translation);Equal(Vector3.Zero,rest.Euler);
                for(int i=0;i<=20;i++)
                {
                    var pose=R.Pose(plan,b,i/20f,1,.5f,.4f,.3f,.2f);
                    if(!float.IsFinite(pose.Translation.LengthSquared())||
                        !float.IsFinite(pose.Euler.LengthSquared())||
                        pose.Translation.Length()*scale>.15f||pose.Euler.Length()>45)
                        throw new Exception(kind+" unstable source-weighted pose.");
                }
                if(b>=R.FirstLimb&&(b-R.FirstLimb)%3==2)
                {
                    var idle=R.Pose(plan,b,.15f,0,0,0,0,1f);
                    Equal(Vector3.Zero,idle.Translation);Equal(Vector3.Zero,idle.Euler);
                }
            }
        }
    }
}
