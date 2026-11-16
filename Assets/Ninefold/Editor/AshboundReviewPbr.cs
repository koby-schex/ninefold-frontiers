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
        private const int Size=512;
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
                material.SetFloat("_Metallic",surface.Name=="hardware" ? .48f :
                    surface.Name=="salvage" ? .29f :
                    surface.Name=="seam" ? .11f : .01f);
                material.SetFloat("_Smoothness",surface.Name=="eye" ? .51f :
                    surface.Name=="hardware" ? .39f :
                    surface.Name=="skin" ? .31f :
                    surface.Name=="mantle" ? .24f : .20f);
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

        // Tileable smooth noise avoids the mechanical repeating sine-wave
        // appearance of the former 256-pixel prototype. Each octave wraps
        // exactly across the material tile, including image edges.
        private static float HashNoise(int x,int y,int seed)
        {
            unchecked
            {
                uint h=(uint)seed;
                h^=(uint)x*0x9E3779B9u;
                h^=(uint)y*0x85EBCA6Bu;
                h^=h>>16;
                h*=0x7FEB352Du;
                h^=h>>15;
                h*=0x846CA68Bu;
                h^=h>>16;
                return (h&0x00FFFFFFu)/8388607.5f-1f;
            }
        }

        private static float TiledNoise(int x,int y,int cells,int seed)
        {
            float px=(float)x*cells/Size,py=(float)y*cells/Size;
            int ix=Mathf.FloorToInt(px),iy=Mathf.FloorToInt(py);
            float tx=px-ix,ty=py-iy;
            tx=tx*tx*(3f-2f*tx);
            ty=ty*ty*(3f-2f*ty);
            int bx=ix%cells,by=iy%cells;
            int nx=(bx+1)%cells,ny=(by+1)%cells;
            float a=Mathf.Lerp(HashNoise(bx,by,seed),HashNoise(nx,by,seed),tx);
            float b=Mathf.Lerp(HashNoise(bx,ny,seed),HashNoise(nx,ny,seed),tx);
            return Mathf.Lerp(a,b,ty);
        }

        private static float Height(string material,int x,int y,int seed)
        {
            float theta=Circle*x/Size,phi=Circle*y/Size;
            float grain=.55f*TiledNoise(x,y,7,seed)+
                        .28f*TiledNoise(x,y,23,seed+71)+
                        .17f*TiledNoise(x,y,61,seed+137);
            float fine=TiledNoise(x,y,89,seed+211);
            switch(material)
            {
                case "skin":
                    // Shallow irregular pores, never painted scales or an
                    // invented biological membrane.
                    return .30f*grain+.09f*fine+
                        .035f*Mathf.Sin(theta*61)*Mathf.Sin(phi*59);
                case "mantle":
                    // Long, flowing organic relief on the *living* sensory
                    // crescent, not an artificial shielding device.
                    return .22f*Mathf.Sin(theta*5+.45f*Mathf.Sin(phi*3))+
                        .22f*grain+.06f*fine;
                case "garment":
                    // Quiet directional woven fabric and repaired seams.
                    return .09f*Mathf.Sin(theta*58+.15f*grain)+
                        .09f*Mathf.Sin(phi*63-.15f*grain)+
                        .28f*grain+.06f*fine;
                case "salvage":
                    // Thin worn mechanical grain, not conspicuous random
                    // impact craters or invented relic glyphs.
                    return .18f*Mathf.Sin(theta*15+.3f*Mathf.Sin(phi*3))+
                        .26f*grain+.11f*fine;
                case "hardware":
                    return .12f*Mathf.Sin(theta*19)+
                        .12f*Mathf.Sin(phi*23)+.22f*grain+.09f*fine;
                case "seam":
                    return .11f*Mathf.Sin(theta*17)+.29f*grain;
                case "dark":
                    return .28f*grain+.05f*fine;
                case "eye":
                    return .025f*grain;
                default:
                    return .19f*grain;
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
                    float scale=material=="eye" ? .7f : 2.1f;
                    var normal=new Vector3(-dx*scale,-dy*scale,1).normalized;
                    pixels[y*Size+x]=new Color32(
                        ToByte(.5f+.5f*normal.x),
                        ToByte(.5f+.5f*normal.y),
                        ToByte(.5f+.5f*normal.z),255);
                }
                else
                {
                    // Modulate the EXISTING eight source swatches rather than
                    // inventing approved skin, mantle, Arcana or faction hues.
                    float contrast=material=="skin" ? .085f :
                        material=="garment" ? .12f :
                        material=="salvage" ? .14f : .075f;
                    float tone=Mathf.Clamp(.94f+height*contrast,.73f,1f);
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
            importer.maxTextureSize=512;
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
