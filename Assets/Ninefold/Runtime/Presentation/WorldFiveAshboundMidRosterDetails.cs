using System.Collections.Generic;
using UnityEngine;
using static Ninefold.Presentation.WorldFiveBattleOrganics;

namespace Ninefold.Presentation
{
    /// <summary>
    /// Real runtime geometry for the two mid-campaign ordinary Ashbound Avarin.
    /// The shapes are explicit, distinguishable anatomical/engineering studies
    /// and are not locked official character models or gameplay abilities.
    /// All meshes use the battlefield's existing owned disposal collection.
    /// </summary>
    internal static class WorldFiveAshboundMidRosterDetails
    {
        internal static void Build(GameObject root,int kind,
            Material skin,Material mantle,Material fabric,Material gear,
            Material conduit,Material dark,List<Mesh> owned)
        {
            if(kind!=2 && kind!=3)
                throw new System.ArgumentOutOfRangeException(nameof(kind),
                    "Mid-roster models are Seam Castigator and Anchor Wrecker.");
            bool wrecker=kind==3;
            // Identical natural species features to first-contact studies;
            // only equipment, armor and movement silhouettes diverge.
            AvarinDetailedAnatomy(root,wrecker,skin,mantle,fabric,gear,
                conduit,dark,owned);
            if(wrecker) Wrecker(root,fabric,gear,conduit,owned);
            else Castigator(root,fabric,gear,conduit,owned);
        }

        private static void Castigator(GameObject root,
            Material cloth,Material salvaged,Material wake,List<Mesh> owned)
        {
            // Upright/slender silhouette; engineered external apparatus
            // beside and behind the shoulder leaves the organic crescent
            // mantle unobstructed. No gun, soul mechanism or bonded creature.
            Part(root,owned,"Castigator field-shawl",cloth,P(-.08f,.15f,-.13f),
                new[]{
                    P(-.12f,.17f,.012f),P(-.10f,.02f,-.026f),
                    P(-.14f,-.16f,-.05f),P(-.18f,-.34f,-.04f)
                },
                new[]{R(.085f,.026f),R(.15f,.041f),
                    R(.17f,.034f),R(.045f,.012f)},20);
            Part(root,owned,"Castigator high right shoulder brace",salvaged,
                P(.248f,.245f,-.088f),new[]{
                    P(-.079f,.084f,.019f),P(.018f,.111f,-.010f),
                    P(.075f,.017f,-.053f),P(.052f,-.084f,.035f)},
                new[]{R(.055f,.040f),R(.109f,.068f),
                    R(.081f,.060f),R(.023f,.018f)},22);
            // A framed lattice looks like an environmental disruption
            // instrument, not a supernatural organ. Long silhouette at
            // portrait camera scale is its primary readability cue.
            for(int i=0;i<4;i++)
            {
                float shift=.047f*i;
                Part(root,owned,"Castigator right reflector "+i,
                    i%2==0?salvaged:wake,
                    P(.325f+shift,.39f,-.17f-.04f*i),new[]{
                    P(-.055f,-.19f,.009f),P(-.024f,.02f,.0f),
                    P(.008f,.24f,-.027f),P(-.029f,.43f,-.06f)},
                    new[]{R(.020f,.018f),R(.049f,.028f),
                        R(.037f,.023f),R(.006f,.008f)},16);
            }
            Part(root,owned,"Castigator right forearm cage",salvaged,
                P(.309f,-.155f,.162f),new[]{
                    P(0,.158f,-.024f),P(.011f,.018f,.033f),
                    P(.020f,-.145f,.097f),P(.020f,-.230f,.138f)},
                new[]{R(.069f,.063f),R(.123f,.089f),
                    R(.101f,.075f),R(.043f,.035f)},24);
            // The cage is open, not a huge featureless blunt boulder.
            for(int rib=0;rib<6;rib++)
            {
                float y=-.050f-.053f*rib;
                Part(root,owned,"Castigator right cage-rib "+rib,wake,
                    P(.334f,y,.248f),new[]{
                        P(-.095f,.0f,-.015f),
                        P(-.025f,.028f,.028f),
                        P(.067f,.017f,.018f),
                        P(.104f,-.004f,-.013f)},
                    new[]{R(.011f,.012f),R(.021f,.016f),
                        R(.020f,.015f),R(.008f,.009f)},12);
            }
            Part(root,owned,"Castigator rear capture spool",salvaged,
                P(-.18f,.038f,-.228f),new[]{
                    P(.004f,.23f,.018f),P(-.011f,.102f,-.028f),
                    P(-.041f,-.077f,-.043f),P(-.020f,-.228f,.0f)},
                new[]{R(.039f,.030f),R(.106f,.078f),
                    R(.112f,.080f),R(.044f,.035f)},20);
            for(int coil=0;coil<5;coil++)
            {
                float y=.167f-coil*.074f;
                Part(root,owned,"Castigator spool external winding "+coil,wake,
                    P(-.21f,y,-.301f),new[]{
                        P(-.095f,-.010f,-.008f),
                        P(-.034f,.013f,-.033f),
                        P(.061f,.013f,-.024f),
                        P(.099f,-.012f,.0f)},
                    new[]{R(.009f,.009f),R(.015f,.012f),
                        R(.015f,.013f),R(.007f,.009f)},14);
            }
            Part(root,owned,"Castigator left balancing pack",cloth,
                P(-.309f,-.28f,-.08f),new[]{
                    P(0,.161f,-.032f),P(-.015f,.005f,-.01f),
                    P(-.031f,-.161f,.016f)},
                new[]{R(.069f,.046f),R(.144f,.082f),
                    R(.079f,.056f)},20);
            for(int lead=0;lead<3;lead++)
            {
                float x=-.15f+lead*.034f;
                Part(root,owned,"Castigator routed back lead "+lead,wake,
                    P(x,.014f,-.234f),new[]{
                        P(0,.264f,.055f),P(.023f,.136f,-.018f),
                        P(-.007f,-.047f,-.052f),
                        P(.047f,-.202f,.011f)},
                    new[]{R(.009f,.010f),R(.016f,.013f),
                        R(.015f,.013f),R(.006f,.008f)},12);
            }
        }

        private static void Wrecker(GameObject root,
            Material cloth,Material salvaged,Material wake,List<Mesh> owned)
        {
            // Squared, exceptionally weighted defensive disruption
            // silhouette. Its contact pad is compact/industrial, not a gun,
            // claw, magical emitter or additional Avarin limb.
            Part(root,owned,"Wrecker left load shoulder",salvaged,
                P(-.23f,.260f,-.070f),new[]{
                    P(.106f,.124f,.023f),
                    P(-.037f,.133f,-.016f),
                    P(-.179f,.045f,-.052f),
                    P(-.105f,-.140f,.027f)},
                new[]{R(.082f,.069f),R(.162f,.104f),
                    R(.133f,.094f),R(.054f,.046f)},24);
            Part(root,owned,"Wrecker right counter shoulder",salvaged,
                P(.224f,.270f,-.080f),new[]{
                    P(-.055f,.112f,.019f),P(.020f,.142f,-.019f),
                    P(.126f,.039f,-.044f),P(.069f,-.106f,.039f)},
                new[]{R(.052f,.047f),R(.124f,.081f),
                    R(.096f,.072f),R(.029f,.025f)},22);
            for(int layer=0;layer<5;layer++)
            {
                float y=.253f-layer*.099f;
                Part(root,owned,"Wrecker overlapping body lamina "+layer,
                    layer%2==0?salvaged:cloth,
                    P(0,y,.118f),new[]{
                        P(-.253f,.057f,-.035f),
                        P(-.111f,.041f,.030f),
                        P(.090f,.015f,.037f),
                        P(.273f,-.047f,-.034f)},
                    new[]{R(.025f,.023f),R(.050f,.032f),
                        R(.053f,.035f),R(.018f,.019f)},20);
            }
            for(float side=-1f;side<=1f;side+=2f)
            {
                string half=side<0?"Left":"Right";
                Part(root,owned,"Wrecker leg load-brace "+half,salvaged,
                    P(side*.154f,-.480f,.036f),new[]{
                    P(0,.140f,-.04f),P(side*.017f,-.017f,.004f),
                    P(side*.023f,-.166f,.048f),
                    P(side*.022f,-.249f,.082f)},
                    new[]{R(.115f,.076f),R(.138f,.098f),
                        R(.111f,.068f),R(.039f,.041f)},18);
                for(int rivet=0;rivet<3;rivet++)
                {
                    float y=-.425f-rivet*.097f;
                    Part(root,owned,"Wrecker leg brace-rivet "+half+" "+rivet,
                        wake,P(side*.180f,y,.129f),new[]{
                            P(-side*.060f,.009f,-.018f),
                            P(0,.020f,.014f),
                            P(side*.055f,.009f,-.006f)},
                        new[]{R(.007f,.008f),R(.014f,.012f),
                            R(.007f,.008f)},12);
                }
            }
            Part(root,owned,"Wrecker right contact brace",salvaged,
                P(.334f,-.153f,.164f),new[]{
                    P(-.011f,.143f,-.037f),
                    P(.018f,.019f,.035f),
                    P(.031f,-.176f,.098f),
                    P(.041f,-.302f,.144f)},
                new[]{R(.105f,.085f),R(.165f,.115f),
                    R(.166f,.119f),R(.078f,.073f)},24);
            Part(root,owned,"Wrecker right anchor contact-pad",salvaged,
                P(.364f,-.39f,.30f),new[]{
                    P(-.192f,.060f,-.042f),
                    P(-.082f,.068f,.015f),
                    P(.077f,.049f,.024f),
                    P(.209f,.020f,-.009f)},
                new[]{R(.039f,.034f),R(.093f,.064f),
                    R(.100f,.068f),R(.032f,.032f)},24);
            for(int rail=0;rail<5;rail++)
            {
                float x=.206f+rail*.078f;
                Part(root,owned,"Wrecker right pad-reinforcement "+rail,
                    wake,P(x,-.393f,.363f),new[]{
                    P(0,.089f,-.033f),P(.009f,-.008f,.012f),
                    P(.009f,-.103f,-.024f)},
                    new[]{R(.010f,.013f),R(.023f,.017f),
                        R(.009f,.011f)},12);
            }
            Part(root,owned,"Wrecker left ballast cassette",salvaged,
                P(-.314f,-.150f,-.159f),new[]{
                    P(0,.201f,0),P(-.030f,.016f,-.034f),
                    P(-.035f,-.190f,.017f)},
                new[]{R(.090f,.073f),R(.163f,.095f),
                    R(.085f,.061f)},22);
            Part(root,owned,"Wrecker rear containment cassette",salvaged,
                P(.137f,.029f,-.251f),new[]{
                    P(0,.160f,.012f),P(.018f,.017f,-.045f),
                    P(.028f,-.183f,-.003f)},
                new[]{R(.054f,.044f),R(.133f,.109f),
                    R(.057f,.041f)},22);
            for(int vent=0;vent<6;vent++)
            {
                float y=.106f-vent*.067f;
                Part(root,owned,"Wrecker containment vent "+vent,wake,
                    P(.153f,y,-.335f),new[]{
                        P(-.096f,.005f,-.018f),
                        P(-.034f,.018f,-.025f),
                        P(.080f,.007f,-.012f)},
                    new[]{R(.009f,.010f),R(.016f,.013f),
                        R(.008f,.009f)},12);
            }
        }
    }
}
