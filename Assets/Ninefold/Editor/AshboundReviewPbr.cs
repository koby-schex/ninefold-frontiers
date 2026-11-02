using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ninefold.Editor
{
    /// <summary>
    /// Actual, imported albedo and tangent-space normal-map PNGs for a
    /// repeatable Rift Forager material/lookdev baseline. Subtle detail
    /// follows garment weave, salvage abrasion, living mantle ridges and
    /// organic skin variation. All maps are PROVISIONAL study textures;
    /// they are not painted final franchise art or approved canonical hues.
    /// </summary>
    internal static class AshboundReviewPbr
    {
        private const int Size=256;
        private static readonly float Circle=2f*Mathf.PI;

        internal static Dictionary<string,Material> Build(
            string folder,Shader shader,IReadOnlyList<GreatStriderSource.Surface> surfaces)
        {
            if(!AssetDatabase.IsValidFolder(folder))
                throw new DirectoryNotFoundException(folder);
            if(shader==null) throw new ArgumentNullException(nameof(shader));
            var output=new Dictionary<string,Material>(StringComparer.Ordinal);
            foreach(var surface in surfaces)
            {
                if(output.ContainsKey(surface.Name))
                    throw new InvalidDataException("Duplicate material "+surface.Name);
                string prefix=folder+"/RiftForager_"+surface.Name;
                var albedo=WriteMap(prefix+"_albedo.png",surface.Name,false);
                var normal=WriteMap(prefix+"_normal.png",surface.Name,true);
                var material=new Material(shader) {name="Rift Forager / "+surface.Name};
                material.SetColor("_BaseColor",new Color(
                    surface.Color[0],surface.Color[1],surface.Color[2],surface.Opacity));
                material.SetTexture("_BaseMap",albedo);
                material.SetTexture("_BumpMap",normal);
                material.SetFloat("_BumpScale",NormalStrength(surface.Name));
                material.EnableKeyword("_NORMALMAP");
                material.SetFloat("_Metallic",surface.Name=="hardware"?.48f:
                    surface.Name=="salvage"?.29f:
                    surface.Name=="seam"?.11f:.01f);
                material.SetFloat("_Smoothness",surface.Name=="eye"?.51f:
                    surface.Name=="hardware"?.39f:
                    surface.Name=="skin"?.31f:
                    surface.Name=="mantle"?.24f:.20f);
                material.enableInstancing=true;
                AssetDatabase.CreateAsset(material,prefix+".mat");
                output.Add(surface.Name,material);
            }
            return output;
        }

        private static float NormalStrength(string material)
        {
            switch(material)
            {
                case "skin":return .30f;
                case "mantle":return .36f;
                case "garment":return .53f;
                case "salvage":return .77f;
                case "hardware":return .61f;
                case "seam":return .45f;
                case "dark":return .34f;
                case "eye":return .11f;
                default:return .30f;
            }
        }

        private static int Seed(string material)
        {
            // Stable across machines, process seeds and Editor restarts.
            unchecked
            {
                uint hash=2166136261;
                foreach(char symbol in material)
                    hash=(hash^symbol)*16777619;
                return (int)(hash&0x7fffffff);
            }
        }

        private static float Height(string material,int x,int y,int seed)
        {
            float theta=Circle*x/Size,phi=Circle*y/Size;
            float grain=Mathf.Sin(theta*13+seed*.00019f)*
                        Mathf.Cos(phi*11-seed*.00013f);
            float fine=Mathf.Sin(theta*47+phi*43+seed*.00034f)*
                       Mathf.Sin(phi*37-theta*41);
            switch(material)
            {
                case "skin":
                    // Shallow irregular pores, never painted scales or an
                    // invented biological membrane.
                    return .23f*grain+.15f*fine+
                        .09f*Mathf.Sin(theta*61)*Mathf.Sin(phi*59);
                case "mantle":
                    // Long, flowing organic relief on the *living* sensory
                    // crescent, not an artificial shielding device.
                    return .34f*Mathf.Sin(theta*5+.75f*Mathf.Sin(phi*3))+
                        .13f*grain+.09f*fine;
                case "garment":
                    // Quiet directional woven fabric and repaired seams.
                    return .26f*Mathf.Sin(theta*30)+
                        .18f*Mathf.Sin(phi*29)+.09f*grain;
                case "salvage":
                    // Thin worn mechanical grain, not conspicuous random
                    // impact craters or invented relic glyphs.
                    return .22f*Mathf.Sin(theta*15+.3f*Mathf.Sin(phi*3))+
                        .17f*grain+.12f*fine;
                case "hardware":
                    return .27f*Mathf.Sin(theta*19)+
                        .13f*Mathf.Sin(phi*23)+.12f*fine;
                case "seam":
                    return .28f*Mathf.Sin(theta*17)+.19f*grain;
                case "dark":
                    return .13f*grain+.09f*fine;
                case "eye":
                    return .035f*grain;
                default:
                    return .14f*grain;
            }
        }

        private static Texture2D WriteMap(string path,string material,bool isNormal)
        {
            int seed=Seed(material);
            var pixels=new Color32[Size*Size];
            for(int y=0;y<Size;y++)
            for(int x=0;x<Size;x++)
            {
                float height=Height(material,x,y,seed);
                if(isNormal)
                {
                    float dx=Height(material,(x+1)%Size,y,seed)-
                             Height(material,(x+Size-1)%Size,y,seed);
                    float dy=Height(material,x,(y+1)%Size,seed)-
                             Height(material,x,(y+Size-1)%Size,seed);
                    float scale=material=="eye"?.7f:2.1f;
                    var normal=new Vector3(-dx*scale,-dy*scale,1).normalized;
                    pixels[y*Size+x]=new Color32(
                        ToByte(.5f+.5f*normal.x),
                        ToByte(.5f+.5f*normal.y),
                        ToByte(.5f+.5f*normal.z),255);
                }
                else
                {
                    float tone=Mathf.Clamp(.91f+height*.10f,.74f,1f);
                    pixels[y*Size+x]=new Color32(
                        ToByte(tone),ToByte(tone*.993f),
                        ToByte(tone*.984f),255);
                }
            }

            var staging=new Texture2D(Size,Size,TextureFormat.RGBA32,false,
                isNormal) {name=Path.GetFileNameWithoutExtension(path)};
            try
            {
                staging.SetPixels32(pixels);
                staging.Apply(false,false);
                File.WriteAllBytes(path,staging.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(staging); }
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)
                throw new IOException("Failed to import candidate texture "+path);
            importer.textureType=isNormal ? TextureImporterType.NormalMap :
                TextureImporterType.Default;
            importer.sRGBTexture=!isNormal;
            importer.mipmapEnabled=true;
            importer.wrapMode=TextureWrapMode.Repeat;
            importer.filterMode=FilterMode.Trilinear;
            importer.maxTextureSize=256;
            importer.SaveAndReimport();
            var loaded=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(loaded==null)
                throw new IOException("Review texture did not import: "+path);
            return loaded;
        }
        private static byte ToByte(float value) =>
            (byte)Mathf.RoundToInt(Mathf.Clamp01(value)*255);
    }
}
