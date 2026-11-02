using System;
using System.Collections.Generic;
using UnityEngine;
using static Ninefold.Presentation.WorldFiveBattleOrganics;

namespace Ninefold.Presentation
{
    /// <summary>
    /// Last two ordinary Ashbound archetypes, shown as *actual in-battle*
    /// provisional lofted meshes. Distinct exterior gear must not be mistaken
    /// for an anatomical part of an Avarin or new approved lore.
    /// All meshes are owned by the same battlefield disposal system as prior
    /// Forager/Striker/Castigator/Wrecker source studies.
    /// </summary>
    internal static class WorldFiveAshboundFinalPairDetails
    {
        internal static void Build(GameObject root,int kind,
            Material skin,Material mantle,Material cloth,Material salvage,
            Material wake,Material dark,List<Mesh> owned)
        {
            if(kind!=4 && kind!=5)
                throw new ArgumentOutOfRangeException(nameof(kind));
            // Species consistency is deliberate. Neither design creates an
            // animal companion, hybrid body, new organ or new magic source.
            AvarinDetailedAnatomy(root,false,skin,mantle,cloth,
                salvage,wake,dark,owned);
            if(kind==4) Seer(root,mantle,cloth,salvage,wake,owned);
            else Witness(root,cloth,salvage,wake,owned);
        }

        private static void Seer(GameObject root,
            Material livingMantle,Material cloth,Material salvage,
            Material wake,List<Mesh> owned)
        {
            // The hazardous Afterecho is sensory disorientation, not power
            // without consequence. Exposed gauges are ENGINEERED readings.
            // Asymmetric slender external baffles leave the Avarin's
            // biological crescent mantle clearly visible from the front.
            Part(root,owned,"Seer damaged left shoulder shroud",cloth,
                P(-.217f,.225f,-.115f),new[]{
                    P(.108f,.136f,.018f),P(.026f,.033f,-.014f),
                    P(-.078f,-.123f,-.051f),
                    P(-.086f,-.315f,-.026f)},
                new[]{R(.041f,.030f),R(.125f,.062f),
                    R(.132f,.042f),R(.023f,.013f)},20);
            Part(root,owned,"Seer right external capture hinge",salvage,
                P(.251f,.311f,-.130f),new[]{
                    P(-.040f,-.085f,.021f),P(.012f,.046f,-.014f),
                    P(.057f,.199f,-.032f),P(.092f,.340f,-.064f)},
                new[]{R(.027f,.026f),R(.065f,.048f),
                    R(.046f,.031f),R(.015f,.014f)},18);
            // Irregular, mismatched petals: perceptual equipment rather than
            // a second set of antennae or sensory organs.
            for(int petal=0;petal<4;petal++)
            {
                float offset=petal*.073f;
                bool right=petal<3;
                float side=right?1f:-1f;
                float x=right?.316f+offset:-.271f;
                Part(root,owned,"Seer external signal petal "+petal,
                    petal%3==0?wake:salvage,
                    P(x,.48f-offset*.23f,-.21f-offset*.18f),new[]{
                        P(-side*.015f,-.190f,.023f),
                        P(side*.038f,-.011f,-.013f),
                        P(side*.054f,.164f,-.038f),
                        P(-side*.009f,.289f,-.072f)},
                    new[]{R(.012f,.010f),R(.054f,.025f),
                        R(.068f,.024f),R(.009f,.010f)},16);
                Part(root,owned,"Seer petal exposed conduit "+petal,
                    wake,P(x+side*.026f,.50f-offset*.23f,-.17f-offset*.18f),
                    new[]{P(0,-.117f,.015f),P(side*.016f,.010f,-.023f),
                        P(side*.026f,.169f,-.027f)},
                    new[]{R(.007f,.008f),R(.014f,.012f),
                        R(.006f,.007f)},12);
            }

            // A small, off-axis cassette mounted to chest harness, not
            // an overloaded orb or impossible floating halo.
            Part(root,owned,"Seer offset Wake capture cassette",salvage,
                P(.122f,-.073f,.187f),new[]{
                    P(0,.171f,-.006f),P(.019f,.020f,.013f),
                    P(.026f,-.174f,-.006f)},
                new[]{R(.042f,.030f),R(.103f,.075f),
                    R(.044f,.035f)},22);
            for(int dial=0;dial<7;dial++)
            {
                float h=.048f-dial*.037f;
                Part(root,owned,"Seer cassette instability readout "+dial,
                    wake,P(.120f,h,.256f),new[]{
                        P(-.058f,.0f,-.014f),
                        P(-.016f,(dial%2==0?.010f:-.007f),.014f),
                        P(.068f,-.006f,-.003f)},
                    new[]{R(.006f,.007f),R(.011f,.010f),
                        R(.006f,.007f)},12);
            }
            // Separate forearm stabilizers visibly attached to moving arms.
            for(float side=-1f;side<=1f;side+=2f)
            {
                string half=side<0?"left":"right";
                Part(root,owned,"Seer "+half+" forearm brace",salvage,
                    P(side*.300f,-.182f,.111f),new[]{
                        P(0,.152f,-.023f),
                        P(side*.016f,.007f,.015f),
                        P(side*.032f,-.145f,.071f)},
                    new[]{R(.057f,.050f),R(.083f,.067f),
                        R(.050f,.043f)},18);
                for(int band=0;band<4;band++)
                {
                    float h=-.085f-band*.075f;
                    Part(root,owned,"Seer "+half+" brace channel "+band,
                        wake,P(side*.301f,h,.179f),new[]{
                            P(-side*.065f,.010f,-.029f),
                            P(0,.026f,.004f),
                            P(side*.066f,.009f,-.014f)},
                        new[]{R(.008f,.009f),R(.017f,.014f),
                            R(.007f,.009f)},12);
                }
            }
            Part(root,owned,"Seer trailing damaged garment",cloth,
                P(-.117f,-.251f,-.157f),new[]{
                    P(.031f,.206f,-.010f),P(-.024f,.046f,-.022f),
                    P(-.078f,-.142f,-.044f),
                    P(-.141f,-.377f,.011f)},
                new[]{R(.074f,.021f),R(.133f,.040f),
                    R(.126f,.031f),R(.025f,.008f)},18);
            for(int seam=0;seam<3;seam++)
            {
                float x=-.263f+seam*.055f;
                Part(root,owned,"Seer stitched garment seam "+seam,
                    seam==1?livingMantle:salvage,
                    P(x,-.377f,-.145f),new[]{
                        P(.041f,.283f,.015f),
                        P(.008f,.060f,-.008f),
                        P(-.034f,-.144f,.021f)},
                    new[]{R(.008f,.007f),R(.013f,.009f),
                        R(.005f,.006f)},10);
            }
            Part(root,owned,"Seer rear Wake stabilizer reel",salvage,
                P(-.152f,.115f,-.261f),new[]{
                    P(0,.142f,.021f),P(-.018f,.023f,-.033f),
                    P(-.012f,-.154f,.012f)},
                new[]{R(.049f,.045f),R(.109f,.083f),
                    R(.049f,.043f)},18);
            for(int lead=0;lead<3;lead++)
            {
                float x=-.21f+lead*.046f;
                Part(root,owned,"Seer rear cassette wiring "+lead,
                    wake,P(x,.045f,-.319f),new[]{
                        P(0,.190f,.026f),P(.021f,.047f,-.018f),
                        P(.024f,-.114f,-.014f)},
                    new[]{R(.010f,.009f),R(.015f,.013f),
                        R(.006f,.008f)},12);
            }
        }

        private static void Witness(GameObject root,
            Material cloth,Material salvage,Material wake,List<Mesh> owned)
        {
            // Camp Witness reads as logistical support, never a captain or
            // Apex. Field registry equipment, offset protective cloth and
            // a hand-held collapsible signaling board define silhouette.
            Part(root,owned,"Witness layered field overcoat",cloth,
                P(-.021f,.060f,-.129f),new[]{
                    P(-.132f,.305f,.012f),
                    P(-.152f,.119f,-.016f),
                    P(-.181f,-.090f,-.028f),
                    P(-.191f,-.322f,.020f),
                    P(-.200f,-.492f,.036f)},
                new[]{R(.098f,.022f),R(.196f,.055f),
                    R(.235f,.062f),R(.230f,.046f),
                    R(.043f,.017f)},24);
            Part(root,owned,"Witness reinforced left shoulder wrap",salvage,
                P(-.236f,.266f,-.081f),new[]{
                    P(.103f,.113f,.026f),P(-.008f,.126f,-.004f),
                    P(-.120f,.014f,-.045f),P(-.056f,-.099f,.009f)},
                new[]{R(.045f,.034f),R(.116f,.067f),
                    R(.085f,.049f),R(.020f,.019f)},20);
            Part(root,owned,"Witness lighter right shoulder wrap",cloth,
                P(.230f,.277f,-.075f),new[]{
                    P(-.065f,.096f,.013f),P(.009f,.093f,-.018f),
                    P(.070f,-.008f,-.031f),
                    P(.020f,-.117f,.021f)},
                new[]{R(.037f,.025f),R(.087f,.051f),
                    R(.071f,.038f),R(.020f,.016f)},18);

            // A multi-strap camp inventory system instead of creature
            // harnessing gear; no bonded companion remains/parts.
            for(int strap=0;strap<4;strap++)
            {
                float shift=strap*.062f;
                Part(root,owned,"Witness repair strap "+strap,salvage,
                    P(-.165f+shift,-.025f,.161f),new[]{
                        P(-.031f,.263f,-.037f),
                        P(-.008f,.124f,.003f),
                        P(.021f,-.064f,.013f),
                        P(.035f,-.272f,-.036f)},
                    new[]{R(.018f,.013f),R(.026f,.017f),
                        R(.022f,.015f),R(.008f,.009f)},14);
            }
            Part(root,owned,"Witness right camp registry satchel",salvage,
                P(.293f,-.330f,-.041f),new[]{
                    P(.007f,.163f,-.018f),
                    P(.017f,.019f,-.018f),
                    P(.007f,-.131f,.025f)},
                new[]{R(.075f,.049f),R(.153f,.091f),
                    R(.084f,.058f)},20);
            Part(root,owned,"Witness registry reader face",wake,
                P(.307f,-.324f,.050f),new[]{
                    P(-.106f,.093f,-.009f),
                    P(-.086f,.028f,.022f),
                    P(.061f,-.055f,.014f),
                    P(.090f,-.106f,-.016f)},
                new[]{R(.015f,.010f),R(.019f,.010f),
                    R(.016f,.010f),R(.008f,.007f)},14);
            for(int line=0;line<6;line++)
            {
                float y=-.257f-line*.041f;
                Part(root,owned,"Witness registry marking "+line,
                    line%2==0?wake:cloth,
                    P(.324f,y,.138f),new[]{
                        P(-.076f,.008f,-.012f),
                        P(.017f,.013f,.018f),
                        P(.089f,.003f,-.010f)},
                    new[]{R(.006f,.006f),R(.011f,.009f),
                        R(.006f,.005f)},12);
            }

            // The left arm holds an ordinary foldable camp signal plate.
            // Large flat silhouette remains mounted on physical equipment,
            // not standing in air as an implied magical barrier.
            Part(root,owned,"Witness left arm signal support",salvage,
                P(-.319f,-.157f,.174f),new[]{
                    P(.047f,.184f,-.035f),
                    P(-.015f,.046f,.008f),
                    P(-.038f,-.130f,.064f)},
                new[]{R(.061f,.049f),R(.091f,.070f),
                    R(.036f,.041f)},18);
            for(int slat=0;slat<5;slat++)
            {
                float shift=slat*.052f;
                Part(root,owned,"Witness left signal slat "+slat,
                    slat==0||slat==4?salvage:cloth,
                    P(-.390f+shift,-.125f,.251f),new[]{
                        P(0,-.085f,-.016f),
                        P(-.004f,.077f,.005f),
                        P(-.025f,.250f,-.019f)},
                    new[]{R(.033f,.018f),R(.050f,.029f),
                        R(.025f,.015f)},14);
            }
            for(int runner=0;runner<3;runner++)
            {
                float h=-.151f+runner*.115f;
                Part(root,owned,"Witness left signal hinge "+runner,
                    wake,P(-.320f,h,.289f),new[]{
                        P(-.175f,.009f,-.021f),
                        P(-.029f,.014f,.007f),
                        P(.129f,.010f,-.012f)},
                    new[]{R(.010f,.009f),R(.018f,.016f),
                        R(.008f,.009f)},12);
            }

            Part(root,owned,"Witness rear camp relay cassette",salvage,
                P(-.179f,.096f,-.273f),new[]{
                    P(.004f,.169f,.017f),
                    P(-.018f,.014f,-.045f),
                    P(-.018f,-.154f,-.001f)},
                new[]{R(.050f,.043f),R(.113f,.096f),
                    R(.045f,.039f)},20);
            for(int lobe=0;lobe<6;lobe++)
            {
                float a=lobe*Mathf.PI/3f;
                Part(root,owned,"Witness field relay channel "+lobe,wake,
                    P(-.172f+Mathf.Cos(a)*.082f,
                        .096f+Mathf.Sin(a)*.087f,-.370f),
                    new[]{P(0,.015f,.014f),P(.013f,.0f,-.014f),
                        P(0,-.017f,-.022f)},
                    new[]{R(.009f,.012f),R(.019f,.019f),
                        R(.007f,.009f)},12);
            }
            // Compact field-kit pouches reinforce support, not warrior or
            // creature-trainer tropes.
            for(int pouch=0;pouch<3;pouch++)
            {
                float x=-.228f+pouch*.144f;
                Part(root,owned,"Witness field utility pouch "+pouch,
                    pouch==1?salvage:cloth,
                    P(x,-.422f,.020f),new[]{
                        P(0,.093f,-.017f),
                        P(.012f,.013f,.024f),
                        P(.019f,-.120f,.025f)},
                    new[]{R(.048f,.033f),R(.095f,.056f),
                        R(.048f,.033f)},16);
            }
        }
    }
}
