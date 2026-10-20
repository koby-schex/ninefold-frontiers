using System;
using UnityEngine;

namespace Ninefold.Presentation
{
    /// <summary>
    /// Lightweight World Five-inspired environment STUDY resources. Does not
    /// assert a named canon location or own any unit-model assets. The model
    /// production chat remains the sole owner of unit geometry and prefabs.
    /// </summary>
    internal static class WorldFiveSceneryKit
    {
        internal readonly struct Style
        {
            internal readonly int FacetCount;
            internal readonly Color RiftStone;
            internal readonly Color MineralHighlights;
            internal readonly Color SeamAccent;

            internal Style(int count, Color stone, Color highlights, Color seam)
            {
                FacetCount=count;
                RiftStone=stone;
                MineralHighlights=highlights;
                SeamAccent=seam;
            }
        }

        internal static bool TryGet(BattlefieldSceneDirection direction, out Style style)
        {
            if (direction.Study == "W5 study · pale survey corridor")
            {
                style=new Style(8,
                    new Color(.38f,.33f,.45f),
                    new Color(.66f,.57f,.71f),
                    new Color(.63f,.54f,.83f));
                return true;
            }
            if (direction.Study == "W5 study · deep mineral fracture")
            {
                style=new Style(15,
                    new Color(.27f,.22f,.39f),
                    new Color(.49f,.39f,.68f),
                    new Color(.55f,.38f,.83f));
                return true;
            }
            style=default;
            return false;
        }

        /// <summary>
        /// Deterministic, tiled mineral grain/strata at modest phone resolution.
        /// This is a rendering texture, not a world/terrain or collision map.
        /// Caller owns its Unity lifetime and may share it between study maps.
        /// </summary>
        internal static Texture2D CreateMineralSurface()
        {
            const int size=256;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,true)
            {
                name="World Five · fracture mineral study",
                wrapMode=TextureWrapMode.Repeat,
                filterMode=FilterMode.Trilinear,
                anisoLevel=2
            };
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)
                for(int x=0;x<size;x++)
                {
                    float low=Noise(x/9,y/9), high=Noise(x,y);
                    float offset=Mathf.Sin(y*.057f+Mathf.Sin(x*.026f)*2.4f)*.065f;
                    float bands=Mathf.Sin(x*.12f+y*.017f+offset*17f)*.045f;
                    float hairline=Mathf.Abs(Mathf.Sin(y*.14f+
                        Mathf.Sin(x*.031f+low)*3f));
                    float seam=Mathf.Pow(Mathf.Clamp01(1f-hairline),17f)*.10f;
                    float value=Mathf.Clamp01(.70f+(low-.5f)*.14f+
                        (high-.5f)*.075f+bands+seam);
                    // Violet-gray variation; material palette remains fully
                    // controlled by the authored BattlefieldSceneDirection.
                    pixels[y*size+x]=new Color(value*.96f,value*.94f,value,1f);
                }
            texture.SetPixels(pixels);
            texture.Apply(true,true);
            return texture;
        }

        private static float Noise(int x,int y)
        {
            unchecked
            {
                uint n=(uint)(x*374761393+y*668265263);
                n=(n^(n>>13))*1274126177;
                return (n&65535)/65535f;
            }
        }
    }
}
