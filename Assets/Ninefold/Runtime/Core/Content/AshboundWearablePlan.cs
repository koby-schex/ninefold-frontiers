using System;
using System.Collections.Generic;
using System.Numerics;

namespace Ninefold.Core.Content
{
    /// <summary>
    /// Exact wearable recipes shared by the Unity battlefield AND the offline
    /// complete-model exporter. Extracted from the previous runtime Part
    /// recipes, not approximated from reference renders. Ordinary Avarin only;
    /// external engineered apparatus is not additional biology or a bond.
    /// </summary>
    public static class AshboundWearablePlan
    {
        public enum SurfaceRole { Skin, Mantle, Fabric, Gear, Wake, Dark }
        public sealed class Piece
        {
            public readonly string Name;
            public readonly SurfaceRole Role;
            public readonly Vector3 Position;
            public readonly Vector3[] Centers;
            public readonly Vector2[] Widths;
            public readonly int Sides;
            internal Piece(string name,SurfaceRole role,Vector3 position,
                Vector3[] centers,Vector2[] widths,int sides)
            { Name=name;Role=role;Position=position;Centers=centers;Widths=widths;Sides=sides; }
        }
        private static Vector3 P(float x,float y,float z)=>new Vector3(x,y,z);
        private static Vector2 R(float x,float y)=>new Vector2(x,y);
        private static bool Starts(string s,string prefix)=>s.StartsWith(prefix,StringComparison.Ordinal);

        // These old anatomical lofts were replaced by a single closed,
        // connected weighted rest skin and real fitted cloth. Never allocate
        // them just to immediately destroy them in a Unity frame.
        public static bool ReplacedAnatomy(string n)=>
            n=="Avarin torso"||n=="Avarin neck"||n=="Avarin temple face"||
            n=="Field mantle drape"||
            Starts(n,"Avarin arm ")||Starts(n,"Avarin leg ")||
            Starts(n,"Avarin foot ")||Starts(n,"Avarin forearm relief ")||
            Starts(n,"Clavicle contour ")||Starts(n,"Heel geometry ")||
            Starts(n,"Temple cheek structure ");

        private static void Part(List<Piece> root,List<Piece> owned,string name,
            SurfaceRole role,Vector3 position,Vector3[] centers,Vector2[] radii,int sides=20)
        {
            if(!ReplacedAnatomy(name))owned.Add(new Piece(name,role,position,centers,radii,sides));
        }
        public static IReadOnlyList<Piece> Build(int kind)
        {
            if(kind<0||kind>5)throw new ArgumentOutOfRangeException(nameof(kind));
            var pieces=new List<Piece>();
            Ashbound(pieces,kind,SurfaceRole.Skin,SurfaceRole.Mantle,
                SurfaceRole.Fabric,SurfaceRole.Gear,SurfaceRole.Wake,SurfaceRole.Dark,pieces);
            return pieces.AsReadOnly();
        }

        // Attachment index corresponds to real weighted joints in the
        // live model. Root/head/upper arms/legs retain original order.
        // Distal joints allow armor/brace/foot gear to rotate with the
        // corresponding forearm or shin, not float at the shoulder.
        public static int JointFor(string n,int kind)
        {
            bool left=n.EndsWith("Left",StringComparison.OrdinalIgnoreCase) ||
                n.IndexOf(" left ",StringComparison.OrdinalIgnoreCase)>=0 ||
                n.StartsWith("Wrecker left ",StringComparison.Ordinal);
            if(Starts(n,"Living crescent ")||Starts(n,"Mantle filament ")||Starts(n,"Temple "))
                return 1;
            if(Starts(n,"Shin reinforcement ")||Starts(n,"Wrecker leg load-brace ")||
                Starts(n,"Wrecker leg brace-rivet "))
                return kind==0?(left?4:5):(left?8:9);
            bool distal=Starts(n,"Field wrap ")||Starts(n,"Striker right arm ")||
                Starts(n,"Striker blunt ")||Starts(n,"Reinforced forearm ")||
                Starts(n,"Forager hooked ")||Starts(n,"Forager reader ")||
                Starts(n,"Castigator right forearm ")||Starts(n,"Castigator right cage-rib ")||
                Starts(n,"Wrecker right contact ")||Starts(n,"Wrecker right anchor ")||
                Starts(n,"Wrecker right pad-")||Starts(n,"Seer left forearm ")||
                Starts(n,"Seer right forearm ")||Starts(n,"Seer left brace channel ")||
                Starts(n,"Seer right brace channel ")||Starts(n,"Witness left arm ")||
                Starts(n,"Witness left signal ");
            if(distal)return kind==0?(left?2:3):(left?6:7);
            if(Starts(n,"Field sensor frame")||Starts(n,"Castigator high right shoulder ")||
                Starts(n,"Castigator right reflector ")||Starts(n,"Wrecker left load shoulder")||
                Starts(n,"Wrecker right counter shoulder")||Starts(n,"Witness reinforced left shoulder ")||
                Starts(n,"Witness lighter right shoulder "))return left?2:3;
            return 0;
        }

        /// <summary>
        /// Smooth closed, genuinely triangulated wearable loft. Hermite
        /// centerline refinement preserves original ring landmarks; radius
        /// interpolation stays positive. Closed cap normals face outward.
        /// The identical mesh builder drives both Unity and OBJ review.
        /// </summary>
        public static FirstEncounterSculpts.Surface Geometry(Piece part)
        {
            var mesh=new FirstEncounterSculpts.Surface();
            int rings=(part.Centers.Length-1)*2+1,sides=part.Sides;
            var points=new Vector3[rings];
            var widths=new Vector2[rings];
            for(int i=0;i<rings;i++)
            {
                int a=i/2,b=Math.Min(a+1,part.Centers.Length-1);
                if(i%2==0){points[i]=part.Centers[a];widths[i]=part.Widths[a];}
                else
                {
                    Vector3 p0=part.Centers[Math.Max(0,a-1)],p1=part.Centers[a],
                        p2=part.Centers[b],p3=part.Centers[Math.Min(b+1,part.Centers.Length-1)];
                    points[i]=.5f*(p1+p2)+.0625f*(p1+p2-p0-p3);
                    widths[i]=.5f*(part.Widths[a]+part.Widths[b]);
                }
            }
            void Add(Vector3 p,float u,float v)
            {
                p+=part.Position;
                mesh.Vertices.Add(new[]{p.X,p.Y,p.Z});
                mesh.Normals.Add(new float[3]);
                mesh.UVs.Add(new[]{u,v});
            }
            void Face(int a,int b,int c)
            {mesh.Indices[0].Add(a);mesh.Indices[0].Add(b);mesh.Indices[0].Add(c);}
            Vector3 previousAxis=Vector3.Zero;
            for(int i=0;i<rings;i++)
            {
                Vector3 tangent=points[Math.Min(i+1,rings-1)]-points[Math.Max(i-1,0)];
                if(tangent.LengthSquared()<1e-10f)tangent=Vector3.UnitY;
                tangent=Vector3.Normalize(tangent);
                Vector3 axis=i==0?Vector3.Cross(tangent,Vector3.UnitY):
                    previousAxis-Vector3.Dot(previousAxis,tangent)*tangent;
                if(axis.LengthSquared()<.01f)axis=Vector3.Cross(tangent,Vector3.UnitZ);
                if(axis.LengthSquared()<.01f)axis=Vector3.Cross(tangent,Vector3.UnitX);
                axis=Vector3.Normalize(axis);
                if(i>0 && Vector3.Dot(axis,previousAxis)<0f)axis=-axis;
                previousAxis=axis;
                Vector3 other=Vector3.Normalize(Vector3.Cross(tangent,axis));
                // Continuous oriented section; avoid arbitrary section-axis
                // flips as the loft curves around a nearly vertical tangent.
                for(int j=0;j<sides;j++)
                {
                    float a=j*2f*MathF.PI/sides;
                    float ripple=1f+.012f*MathF.Sin(j*2.7f+i*.465f);
                    Add(points[i]+(axis*MathF.Cos(a)*widths[i].X+
                        other*MathF.Sin(a)*widths[i].Y)*ripple,
                        (float)j/sides,(float)i/(rings-1));
                }
            }
            for(int i=0;i<rings-1;i++)for(int j=0;j<sides;j++)
            {
                int a=i*sides+j,b=i*sides+(j+1)%sides,
                    c=(i+1)*sides+j,d=(i+1)*sides+(j+1)%sides;
                Face(a,b,c);Face(b,d,c);
            }
            int first=mesh.Vertices.Count;Add(points[0],.5f,0);
            int last=mesh.Vertices.Count;Add(points[rings-1],.5f,1);
            for(int j=0;j<sides;j++)
            {
                Face(first,(j+1)%sides,j);
                Face(last,(rings-1)*sides+j,(rings-1)*sides+(j+1)%sides);
            }
            var faces=mesh.Indices[0];
            for(int i=0;i<faces.Count;i+=3)
            {
                var a=mesh.Vertices[faces[i]];var b=mesh.Vertices[faces[i+1]];var c=mesh.Vertices[faces[i+2]];
                var n=Vector3.Cross(new Vector3(b[0]-a[0],b[1]-a[1],b[2]-a[2]),
                    new Vector3(c[0]-a[0],c[1]-a[1],c[2]-a[2]));
                for(int k=0;k<3;k++)
                {
                    var sum=mesh.Normals[faces[i+k]];
                    sum[0]+=n.X;sum[1]+=n.Y;sum[2]+=n.Z;
                }
            }
            foreach(var n in mesh.Normals)
            {
                var v=new Vector3(n[0],n[1],n[2]);
                if(v.LengthSquared()<1e-16f)throw new InvalidOperationException("Invalid wearable normal: "+part.Name);
                v=Vector3.Normalize(v);n[0]=v.X;n[1]=v.Y;n[2]=v.Z;
            }
            return mesh;
        }

        internal static void Ashbound(List<Piece> root,int kind,
            SurfaceRole skin,SurfaceRole mantle,SurfaceRole fabric,SurfaceRole gear,
            SurfaceRole conduit,SurfaceRole dark,List<Piece> owned)
        {
            // Technical NPC type selects shoulder/gear proportions but NEVER
            // creates companion creatures or turns the mantle into a magic organ.
            float bulk=kind==1?1.19f:kind==3?1.32f:kind==4?.84f:1f;
            Part(root,owned,"Avarin torso",skin,Vector3.Zero,new[]{
                P(0,-.35f,0),P(0,-.21f,0),P(0,-.04f,-.015f),
                P(0,.16f,-.035f),P(0,.31f,-.03f),P(0,.44f,-.02f)
            },new[]{R(.11f,.09f),R(.22f*bulk,.14f),
                R(.23f*bulk,.16f),R(.255f*bulk,.17f),R(.21f*bulk,.14f),
                R(.065f,.07f)},24);
            Part(root,owned,"Avarin neck",skin,Vector3.Zero,new[]{
                P(0,.33f,-.01f),P(0,.45f,.012f),P(0,.57f,.027f)
            },new[]{R(.065f,.067f),R(.10f,.09f),R(.071f,.08f)},18);
            Part(root,owned,"Avarin temple face",skin,Vector3.Zero,new[]{
                P(0,.42f,.014f),P(0,.53f,.063f),P(0,.68f,.07f),
                P(0,.80f,.025f),P(0,.85f,-.02f)
            },new[]{R(.087f,.085f),R(.135f,.118f),R(.14f,.13f),
                R(.105f,.105f),R(.023f,.035f)},24);
            // Curved bilateral living *sensory* crescent with material continuity.
            foreach(float side in new[]{-1f,1f})
            {
                string sideName=side<0?"Left":"Right";
                Part(root,owned,"Living crescent "+sideName,mantle,Vector3.Zero,new[]{
                    P(side*.072f,.60f,-.16f),P(side*.18f,.66f,-.15f),
                    P(side*.33f,.69f,-.14f),P(side*.46f,.72f,-.12f),
                    P(side*.49f,.81f,-.09f),P(side*.40f,.91f,-.07f)
                },new[]{R(.055f,.057f),R(.097f,.061f),R(.110f,.052f),
                    R(.086f,.043f),R(.057f,.039f),R(.012f,.017f)},18);
                Part(root,owned,"Avarin arm "+sideName,skin,
                    P(side*.25f,.18f,-.035f),new[]{
                    P(0,.14f,0),P(side*.035f,.02f,.02f),
                    P(side*.061f,-.19f,.052f),P(side*.055f,-.44f,.13f)
                },new[]{R(.13f,.10f),R(.122f,.092f),
                    R(.079f,.079f),R(.051f,.065f)},16);
                Part(root,owned,"Avarin leg "+sideName,skin,
                    P(side*.145f,-.32f,-.02f),new[]{
                    P(0,.04f,0),P(side*.015f,-.11f,.01f),
                    P(side*.016f,-.28f,.045f),
                    P(side*.016f,-.44f,.066f)
                },new[]{R(.135f,.121f),R(.122f,.112f),
                    R(.088f,.086f),R(.064f,.085f)},18);
                Part(root,owned,"Avarin foot "+sideName,dark,
                    P(side*.16f,-.70f,.035f),new[]{
                    P(0,0,-.02f),P(0,-.012f,.09f),P(0,-.02f,.22f)
                },new[]{R(.089f,.046f),R(.110f,.05f),
                    R(.032f,.023f)},14);
                Part(root,owned,"Salvage shoulder "+sideName,gear,
                    P(side*.20f,.29f,-.067f),new[]{
                    P(0,.078f,0),P(side*.045f,.012f,.028f),
                    P(side*.066f,-.105f,.045f)
                },new[]{R(.055f,.042f),R(.130f*bulk,.075f),
                    R(.047f,.041f)},18);
            }
            // Damage-worn, non-magical clothing + deliberately external Wake.
            Part(root,owned,"Field mantle drape",fabric,Vector3.Zero,new[]{
                P(0,.19f,-.148f),P(0,.07f,-.165f),
                P(0,-.18f,-.17f),P(0,-.42f,-.14f)
            },new[]{R(.20f,.048f),R(.225f,.056f),
                R(.235f,.052f),R(.14f,.045f)},20);
            Part(root,owned,"Fracture Wake equipment",conduit,
                P(kind%2==0?-.185f:.19f,-.01f,-.20f),new[]{
                P(0,.21f,0),P(0,.11f,-.045f),P(0,-.055f,-.061f),
                P(0,-.20f,-.03f)
            },new[]{R(.042f,.036f),R(.096f,.065f),
                R(.083f,.072f),R(.041f,.038f)},16);
            // The six approved archetypes are distinguished by external
            // equipment density, not by altering species or adding a bond.
            if(kind==0 || kind==2 || kind==4)
                Part(root,owned,"Field sensor frame",gear,P(.32f,.07f,.065f),new[]{
                    P(0,-.30f,.12f),P(.08f,-.05f,.14f),
                    P(.09f,.24f,.12f),P(.028f,.41f,.05f)
                },new[]{R(.026f,.028f),R(.051f,.048f),
                    R(.064f,.043f),R(.017f,.022f)},16);
            if(kind==1 || kind==3)
                Part(root,owned,"Reinforced forearm brace",gear,
                    P(.325f,-.11f,.14f),new[]{
                    P(0,.11f,0),P(.005f,-.04f,.02f),
                    P(.023f,-.22f,.062f)
                },new[]{R(.089f,.083f),R(.117f,.092f),
                    R(.063f,.071f)},16);
            if(kind==5 || kind==3)
                Part(root,owned,"Field registry satchel",gear,
                    P(-.31f,-.32f,-.042f),new[]{
                    P(0,.17f,0),P(-.019f,.015f,-.01f),
                    P(-.028f,-.16f,.02f)
                },new[]{R(.061f,.043f),R(.125f,.09f),
                    R(.083f,.071f)},16);
            if(kind==4 || kind==5)
            {
                AvarinDetailedAnatomy(root,false,skin,mantle,fabric,gear,conduit,dark,owned);
                if(kind==4)Seer(root,mantle,fabric,gear,conduit,owned);
                else Witness(root,fabric,gear,conduit,owned);
            }
            if(kind==2 || kind==3)
            {
                AvarinDetailedAnatomy(root,kind==3,skin,mantle,fabric,gear,conduit,dark,owned);
                if(kind==2)Castigator(root,fabric,gear,conduit,owned);
                else Wrecker(root,fabric,gear,conduit,owned);
            }
            if(kind==0 || kind==1)
                AshboundFirstContactDetails(root,kind,skin,mantle,fabric,gear,conduit,dark,owned);
        }

        private static void AshboundFirstContactDetails(List<Piece> root,int kind,
            SurfaceRole skin,SurfaceRole mantle,SurfaceRole fabric,SurfaceRole gear,
            SurfaceRole conduit,SurfaceRole dark,List<Piece> owned)
        {
            bool striker=kind==1;
            AvarinDetailedAnatomy(root,striker,skin,mantle,fabric,
                gear,conduit,dark,owned);
            if(!striker)
            {
                // Forager reads light and alert: asymmetrical terrain probe,
                // flexible tethers, narrow repair apron and tool pack.
                Part(root,owned,"Forager shoulder capture relay",gear,
                    P(.305f,.305f,-.070f),
                    new[]{P(-.07f,.038f,-.01f),
                          P(0,.059f,-.027f),P(.109f,.009f,-.026f),
                          P(.128f,-.081f,.010f)},
                    new[]{R(.040f,.039f),R(.069f,.049f),
                          R(.043f,.037f),R(.018f,.017f)},16);
                Part(root,owned,"Forager hooked route reader",conduit,
                    P(.335f,-.165f,.146f),
                    new[]{P(0,.110f,-.012f),P(.018f,-.011f,.042f),
                          P(.011f,-.172f,.160f),
                          P(-.024f,-.230f,.242f)},
                    new[]{R(.035f,.034f),R(.052f,.040f),
                          R(.035f,.025f),R(.010f,.011f)},16);
                Part(root,owned,"Forager reader tip",gear,
                    P(.324f,-.396f,.389f),
                    new[]{P(0,.028f,-.050f),P(.009f,0,0),
                          P(-.021f,-.033f,.071f)},
                    new[]{R(.015f,.014f),R(.059f,.027f),
                          R(.010f,.008f)},12);
                Part(root,owned,"Forager repair satchel",gear,
                    P(-.245f,-.304f,-.16f),
                    new[]{P(0,.106f,-.013f),P(-.021f,-.015f,-.014f),
                          P(-.024f,-.185f,.017f)},
                    new[]{R(.077f,.056f),R(.125f,.088f),
                          R(.070f,.056f)},18);
                Part(root,owned,"Forager trailing tag",fabric,
                    P(-.24f,-.465f,-.108f),
                    new[]{P(0,.07f,0),P(-.035f,-.024f,-.017f),
                          P(-.070f,-.143f,-.015f)},
                    new[]{R(.044f,.018f),R(.050f,.017f),
                          R(.014f,.007f)},12);
            }
            else
            {
                // Striker's silhouette is unequivocally different at battle
                // scale: overbuilt RIGHT shoulder and broad blunt forearm
                // contact implement; the left remains visibly lighter.
                Part(root,owned,"Striker layered right shoulder",gear,
                    P(.217f,.274f,-.09f),
                    new[]{P(-.09f,.094f,.02f),
                          P(.026f,.122f,-.025f),
                          P(.165f,.025f,-.045f),
                          P(.112f,-.121f,.037f)},
                    new[]{R(.071f,.063f),R(.147f,.098f),
                          R(.128f,.090f),R(.025f,.027f)},24);
                Part(root,owned,"Striker right arm impact housing",gear,
                    P(.320f,-.180f,.175f),
                    new[]{P(0,.136f,-.024f),P(.022f,.014f,.023f),
                          P(.019f,-.151f,.105f)},
                    new[]{R(.093f,.082f),R(.147f,.110f),
                          R(.101f,.077f)},20);
                Part(root,owned,"Striker blunt contact ridge",conduit,
                    P(.361f,-.336f,.282f),
                    new[]{P(-.095f,.021f,-.020f),P(0,.032f,.024f),
                          P(.100f,.017f,.039f)},
                    new[]{R(.048f,.036f),R(.071f,.056f),
                          R(.047f,.034f)},18);
                Part(root,owned,"Striker side ballast",gear,
                    P(-.232f,-.075f,-.212f),
                    new[]{P(0,.165f,-.016f),P(-.022f,.009f,-.044f),
                          P(-.021f,-.178f,.012f)},
                    new[]{R(.055f,.048f),R(.127f,.073f),
                          R(.056f,.041f)},18);
                for(int plate=0;plate<3;plate++)
                {
                    float h=-.04f-plate*.082f;
                    Part(root,owned,"Striker layered chest plate "+plate,gear,
                        P(0,h,.157f),
                        new[]{P(-.224f,.043f,-.060f),
                              P(-.055f,.020f,.027f),
                              P(.135f,-.008f,.014f),
                              P(.234f,-.036f,-.056f)},
                        new[]{R(.016f,.013f),R(.032f,.025f),
                              R(.032f,.023f),R(.010f,.010f)},18);
                }
            }
        }

        internal static void AvarinDetailedAnatomy(List<Piece> root,bool armored,
            SurfaceRole skin,SurfaceRole mantle,SurfaceRole fabric,SurfaceRole gear,
            SurfaceRole conduit,SurfaceRole dark,List<Piece> owned)
        {
            bool striker=armored;
            // Explicit face architecture. No sphere-blob face or floating eyes.
            Part(root,owned,"Temple brow bridge",skin,P(0,.72f,.127f),
                new[]{P(-.155f,0,0),P(-.085f,.018f,.02f),
                      P(0,.022f,.027f),P(.085f,.018f,.02f),
                      P(.155f,0,0)},
                new[]{R(.014f,.018f),R(.037f,.026f),R(.035f,.028f),
                      R(.037f,.026f),R(.014f,.018f)},14);
            Part(root,owned,"Temple mouth relief",dark,P(0,.495f,.157f),
                new[]{P(-.052f,.005f,0),P(0,0,.018f),P(.052f,.005f,0)},
                new[]{R(.008f,.010f),R(.014f,.012f),R(.008f,.010f)},12);
            for(float side=-1f;side<=1f;side+=2f)
            {
                string half=side<0?"Left":"Right";
                Part(root,owned,"Temple eye "+half,dark,
                    P(side*.103f,.692f,.166f),
                    new[]{P(0,0,-.03f),P(side*.01f,.004f,0),
                          P(side*.012f,.008f,.026f)},
                    new[]{R(.011f,.010f),R(.029f,.022f),R(.007f,.009f)},12);
                Part(root,owned,"Temple cheek structure "+half,skin,
                    P(side*.099f,.625f,.146f),
                    new[]{P(0,.042f,-.010f),P(side*.012f,0,.025f),
                          P(-side*.007f,-.067f,.0f)},
                    new[]{R(.019f,.021f),R(.037f,.034f),R(.012f,.015f)},14);
                // Fine organic creases follow the living mantle, never
                // replacing it with rigid ears, spikes or magical wings.
                for(int rib=0;rib<3;rib++)
                {
                    float reach=rib*.036f;
                    Part(root,owned,"Mantle filament "+half+" "+rib,
                        rib==1?skin:mantle,Vector3.Zero,
                        new[]{P(side*(.105f+reach),.63f,-.108f),
                              P(side*(.23f+reach),.69f,-.090f),
                              P(side*(.37f+reach),.74f,-.067f),
                              P(side*(.39f+reach),.83f,-.045f)},
                        new[]{R(.010f,.011f),R(.014f,.012f),
                              R(.012f,.010f),R(.003f,.004f)},10);
                }

                // Continuous layered muscle and soft field wrapping;
                // narrower Forager and heavier reinforced Striker shapes.
                Part(root,owned,"Clavicle contour "+half,skin,Vector3.Zero,
                    new[]{P(side*.058f,.335f,.100f),
                          P(side*.14f,.301f,.137f),
                          P(side*.230f,.270f,.092f)},
                    new[]{R(.022f,.027f),R(.034f,.030f),
                          R(.009f,.013f)},14);
                Part(root,owned,"Avarin forearm relief "+half,
                    skin,P(side*.285f,-.045f,.03f),
                    new[]{P(0,.094f,-.012f),
                          P(side*.026f,-.040f,.031f),
                          P(side*.033f,-.195f,.088f)},
                    new[]{R(.036f,.041f),R(.050f,.053f),
                          R(.015f,.025f)},14);
                Part(root,owned,"Field wrap "+half,fabric,
                    P(side*.304f,-.210f,.101f),
                    new[]{P(0,.048f,-.012f),
                          P(side*.007f,0,.016f),
                          P(side*.007f,-.062f,.042f)},
                    new[]{R(.066f,.065f),R(.077f,.066f),
                          R(.061f,.059f)},14);
                Part(root,owned,"Shin reinforcement "+half,
                    striker?gear:fabric,
                    P(side*.161f,-.55f,.055f),
                    new[]{P(0,.105f,-.018f),
                          P(side*.008f,-.015f,.010f),
                          P(side*.008f,-.15f,.057f)},
                    new[]{R(.082f,.060f),R(.072f,.062f),
                          R(.035f,.033f)},14);
                Part(root,owned,"Heel geometry "+half,dark,
                    P(side*.163f,-.739f,.016f),
                    new[]{P(0,.025f,-.06f),P(0,-.012f,.035f),
                          P(0,-.017f,.140f)},
                    new[]{R(.059f,.038f),R(.097f,.046f),
                          R(.024f,.018f)},12);
            }
            Part(root,owned,"Upper field wrap",fabric,Vector3.Zero,
                new[]{P(0,.258f,.122f),P(0,.145f,.160f),
                      P(0,.016f,.163f),P(0,-.100f,.147f)},
                new[]{R(.133f,.039f),R(striker ? .225f : .172f,.046f),
                      R(striker ? .225f : .170f,.052f),R(.113f,.038f)},24);
            // Overlapping chest-wear strips read clearly at portrait zoom.
            for(int strip=0;strip<4;strip++)
            {
                float side=strip<2?-1f:1f;
                float y=.195f-(strip%2)*.089f;
                Part(root,owned,"Cross body harness "+strip,gear,Vector3.Zero,
                    new[]{P(side*.052f,y+.113f,.155f),
                          P(side*.116f,y+.058f,.168f),
                          P(side*.191f,y-.05f,.084f)},
                    new[]{R(.021f,.015f),R(.025f,.018f),
                          R(.013f,.010f)},14);
            }
            Part(root,owned,"External Wake support loom",gear,
                P(-.16f,-.04f,-.187f),
                new[]{P(0,.235f,0),P(-.014f,.070f,-.030f),
                      P(-.008f,-.145f,-.043f),P(.012f,-.270f,.006f)},
                new[]{R(.030f,.028f),R(.050f,.045f),
                      R(.039f,.036f),R(.016f,.021f)},14);
            for(int conduitIndex=0;conduitIndex<3;conduitIndex++)
            {
                float offset=conduitIndex*.027f;
                Part(root,owned,"External Wake conductor "+conduitIndex,
                    conduit,P(-.17f+offset,.012f,-.235f),
                    new[]{P(0,.177f,.020f),P(.008f,.086f,-.020f),
                          P(-.014f,-.051f,-.038f),
                          P(-.017f,-.194f,-.006f)},
                    new[]{R(.009f,.010f),R(.013f,.012f),
                          R(.012f,.011f),R(.006f,.008f)},10);
            }

        }

        private static void Castigator(List<Piece> root,
            SurfaceRole cloth,SurfaceRole salvaged,SurfaceRole wake,List<Piece> owned)
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

        private static void Wrecker(List<Piece> root,
            SurfaceRole cloth,SurfaceRole salvaged,SurfaceRole wake,List<Piece> owned)
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

        private static void Seer(List<Piece> root,
            SurfaceRole livingMantle,SurfaceRole cloth,SurfaceRole salvage,
            SurfaceRole wake,List<Piece> owned)
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
                float x=right ? .316f+offset : -.271f;
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
                        P(-.016f,(dial%2==0 ? .010f : -.007f),.014f),
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

        private static void Witness(List<Piece> root,
            SurfaceRole cloth,SurfaceRole salvage,SurfaceRole wake,List<Piece> owned)
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
                float a=lobe*MathF.PI/3f;
                Part(root,owned,"Witness field relay channel "+lobe,wake,
                    P(-.172f+MathF.Cos(a)*.082f,
                        .096f+MathF.Sin(a)*.087f,-.370f),
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
