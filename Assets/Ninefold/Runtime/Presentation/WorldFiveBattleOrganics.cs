using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ninefold.Presentation
{
    /// <summary>
    /// Compact, actual in-battle organic MESH fallback for World Five wildlife and
    /// specifically identified Ashbound NPCs. No authored/approved prefab is replaced.
    /// These are noncanonical provisional silhouettes, not final skinned assets.
    /// This class creates render geometry only: no colliders, scripts, combat data,
    /// pathing, sensors, object interactions or implicit creature bonds.
    /// </summary>
    internal static class WorldFiveBattleOrganics
    {
        /// <summary>
        /// Low-memory repeatable surface grain. Tinted by the material itself;
        /// purposefully subtle so skin and salvaged hardware don't look like
        /// flat Microsoft Paint fills or high-gloss plastic. No source textures.
        /// </summary>
        internal static Texture2D CreateSurfaceTexture(int seed)
        {
            const int size=128;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,true)
            {
                name="World Five organic study surface "+seed,
                filterMode=FilterMode.Trilinear,
                wrapMode=TextureWrapMode.Repeat
            };
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)
            for(int x=0;x<size;x++)
            {
                unchecked
                {
                    uint n=(uint)(x*374761393+y*668265263+seed*1013904223);
                    n=(n^(n>>13))*1274126177;
                    float fine=((n&65535)/65535f-.5f);
                    float band=Mathf.Sin(x*.23f+y*.035f+seed*.71f);
                    float broad=Mathf.Sin(x*.064f-y*.089f+seed*1.7f);
                    float tone=Mathf.Clamp(.84f+fine*.095f+band*.045f+broad*.045f,.67f,.99f);
                    pixels[y*size+x]=new Color(tone,tone*.99f,tone*.96f,1);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(true,true);
            return texture;
        }

        // A curved loft makes connected shoulders/bodies/limbs, not Unity cubes
        // and spheres assembled as a pretend character. All values are local.
        private static Mesh Loft(string name, Vector3[] centers, Vector2[] widths, int sides=20)
        {
            if(centers.Length!=widths.Length || centers.Length<3)
                throw new ArgumentException("Every loft ring needs a matching radius.");
            var vertices=new List<Vector3>();
            var uv=new List<Vector2>();
            var triangles=new List<int>();
            for(int i=0;i<centers.Length;i++)
            {
                Vector3 tangent=centers[Mathf.Min(i+1,centers.Length-1)]-
                    centers[Mathf.Max(i-1,0)];
                tangent.Normalize();
                if(tangent.sqrMagnitude<.5f) tangent=Vector3.up;
                Vector3 axis=Vector3.Cross(tangent,Vector3.up);
                if(axis.sqrMagnitude<.1f) axis=Vector3.Cross(tangent,Vector3.forward);
                axis.Normalize();
                Vector3 other=Vector3.Cross(tangent,axis).normalized;
                for(int j=0;j<sides;j++)
                {
                    float angle=j*Mathf.PI*2f/sides;
                    float rippling=1f + .025f*Mathf.Sin(j*2.7f+i*.93f);
                    var p=centers[i]+
                        (axis*Mathf.Cos(angle)*widths[i].x +
                         other*Mathf.Sin(angle)*widths[i].y)*rippling;
                    vertices.Add(p);
                    uv.Add(new Vector2(j/(float)sides,i/(float)(centers.Length-1)));
                }
            }
            for(int i=0;i<centers.Length-1;i++)
            {
                for(int j=0;j<sides;j++)
                {
                    int a=i*sides+j,b=i*sides+(j+1)%sides,c=(i+1)*sides+j,
                        d=(i+1)*sides+(j+1)%sides;
                    triangles.Add(a);triangles.Add(b);triangles.Add(c);
                    triangles.Add(b);triangles.Add(d);triangles.Add(c);
                }
            }
            // Close the ends; no open hollow primitive tubes at the ankles.
            int baseA=vertices.Count;vertices.Add(centers[0]);uv.Add(new Vector2(.5f,0));
            int baseB=vertices.Count;vertices.Add(centers[centers.Length-1]);uv.Add(new Vector2(.5f,1));
            for(int j=0;j<sides;j++)
            {
                triangles.Add(baseA);triangles.Add(j);triangles.Add((j+1)%sides);
                int last=(centers.Length-1)*sides;
                triangles.Add(baseB);triangles.Add(last+(j+1)%sides);triangles.Add(last+j);
            }
            var mesh=new Mesh {name=name};
            mesh.SetVertices(vertices);
            mesh.SetUVs(0,uv);
            mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject Part(GameObject root,List<Mesh> owned,string name,
            Material material,Vector3 position,Vector3[] centers,Vector2[] radii,int sides=20)
        {
            var node=new GameObject(name);
            node.transform.SetParent(root.transform,false);
            node.transform.localPosition=position;
            var mesh=Loft(name+" · connected surface",centers,radii,sides);
            owned.Add(mesh);
            node.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=node.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=false;
            return node;
        }
        private static Vector2 R(float x,float y) => new Vector2(x,y);
        private static Vector3 P(float x,float y,float z) => new Vector3(x,y,z);

        internal static void Wildlife(GameObject root,bool stalker,
            Material body,Material accent,Material dark,List<Mesh> owned)
        {
            // Sculpted flowing trunk rather than an exposed capsule + sphere.
            Part(root,owned,"Dorsal shell",body,Vector3.zero,new[]{
                P(0,.12f,-.65f),P(0,.18f,-.51f),P(0,.23f,-.30f),
                P(0,.25f,-.07f),P(0,.23f,.19f),P(0,.17f,.43f),
                P(0,.13f,.59f)
            },new[]{R(.065f,.10f),R(.30f,.20f),R(.39f,.28f),
                R(.43f,.28f),R(.40f,.26f),R(.29f,.20f),R(.07f,.09f)},24);
            var head = Part(root,owned,"Head",body,P(0,.12f,.54f),new[]{
                P(0,0,-.13f),P(0,.02f,.02f),P(0,.03f,.20f),
                P(0,.0f,.38f),P(0,-.045f,.52f)
            },new[]{R(.16f,.16f),R(.26f,.23f),R(.25f,.20f),
                R(.19f,.15f),R(.035f,.045f)},24);
            // Underside is a continuous relief, not a floating second body.
            Part(root,owned,"Chest folds",accent,Vector3.zero,new[]{
                P(0,-.04f,-.42f),P(0,-.07f,-.23f),
                P(0,-.065f,.08f),P(0,-.06f,.35f)
            },new[]{R(.08f,.05f),R(.23f,.11f),R(.22f,.11f),R(.055f,.04f)},20);
            foreach(float side in new[]{-1f,1f})
            {
                for(int pair=0;pair<2;pair++)
                {
                    float z=pair==0?.34f:-.38f;
                    var walkingLeg = Part(root,owned,"Walking limb",body,
                        P(side*.43f,-.38f,z),new[]{
                        P(-side*.055f,.29f,0),
                        P(0,.11f,side*.008f),
                        P(side*.025f,-.11f,.036f),
                        P(side*.04f,-.30f,.10f)
                    },new[]{R(.14f,.16f),R(.122f,.123f),R(.090f,.092f),
                        R(.074f,.103f)},16);
                    var foot = Part(root,owned,"Foot keratin",accent,
                        P(side*.43f+side*.04f,-.68f,z+.1f),new[]{
                            P(0,0,-.07f),P(0,-.013f,.03f),P(0,-.025f,.18f)
                        },new[]{R(.081f,.046f),R(.095f,.05f),R(.042f,.026f)},12);
                    foot.transform.SetParent(walkingLeg.transform,true);
                }
                Part(root,owned,"Sensory ridge",accent,P(side*.24f,.36f,.43f),new[]{
                    P(0,-.10f,-.27f),P(side*.024f,.02f,-.09f),
                    P(side*.035f,.10f,.10f),P(side*.016f,.04f,.31f)
                },new[]{R(.035f,.038f),R(.076f,.061f),R(.085f,.051f),
                    R(.023f,.019f)},14);
                var eye = Part(root,owned,"Lateral eye",dark,
                    P(side*.215f,.18f,.83f),new[]{
                    P(0,0,-.04f),P(side*.016f,.005f,0),P(side*.016f,0,.052f)
                },new[]{R(.016f,.020f),R(.046f,.053f),R(.008f,.013f)},12);
                eye.transform.SetParent(head.transform,true);
            }
            if(stalker)
            {
                Part(root,owned,"Tapered tail",body,P(0,-.02f,-.64f),new[]{
                    P(0,0,.06f),P(0,.01f,-.18f),P(0,.015f,-.40f),
                    P(0,.04f,-.64f),P(0,.07f,-.82f)
                },new[]{R(.18f,.15f),R(.15f,.13f),R(.11f,.09f),
                    R(.066f,.05f),R(.01f,.015f)},18);
                Part(root,owned,"Dorsal segmented relief",accent,Vector3.zero,new[]{
                    P(0,.43f,-.46f),P(0,.49f,-.25f),P(0,.52f,-.08f),
                    P(0,.45f,.13f)
                },new[]{R(.055f,.038f),R(.11f,.063f),
                    R(.095f,.06f),R(.04f,.03f)},14);
            }
            else
            {
                Part(root,owned,"Balance fan",accent,P(0,.08f,-.68f),new[]{
                    P(0,0,.10f),P(0,.055f,-.06f),P(0,.043f,-.21f),
                    P(0,.018f,-.36f)
                },new[]{R(.13f,.07f),R(.31f,.10f),R(.32f,.078f),
                    R(.06f,.033f)},20);
            }
        }

        internal static void Ashbound(GameObject root,int kind,
            Material skin,Material mantle,Material fabric,Material gear,
            Material conduit,Material dark,List<Mesh> owned)
        {
            // Technical NPC type selects shoulder/gear proportions but NEVER
            // creates companion creatures or turns the mantle into a magic organ.
            float bulk=kind==1?1.19f:kind==3?1.32f:kind==4?.84f:1f;
            Part(root,owned,"Avarin torso",skin,Vector3.zero,new[]{
                P(0,-.35f,0),P(0,-.21f,0),P(0,-.04f,-.015f),
                P(0,.16f,-.035f),P(0,.31f,-.03f),P(0,.44f,-.02f)
            },new[]{R(.11f,.09f),R(.22f*bulk,.14f),
                R(.23f*bulk,.16f),R(.255f*bulk,.17f),R(.21f*bulk,.14f),
                R(.065f,.07f)},24);
            Part(root,owned,"Avarin neck",skin,Vector3.zero,new[]{
                P(0,.33f,-.01f),P(0,.45f,.012f),P(0,.57f,.027f)
            },new[]{R(.065f,.067f),R(.10f,.09f),R(.071f,.08f)},18);
            Part(root,owned,"Avarin temple face",skin,Vector3.zero,new[]{
                P(0,.42f,.014f),P(0,.53f,.063f),P(0,.68f,.07f),
                P(0,.80f,.025f),P(0,.85f,-.02f)
            },new[]{R(.087f,.085f),R(.135f,.118f),R(.14f,.13f),
                R(.105f,.105f),R(.023f,.035f)},24);
            // Curved bilateral living *sensory* crescent with material continuity.
            foreach(float side in new[]{-1f,1f})
            {
                string sideName=side<0?"Left":"Right";
                Part(root,owned,"Living crescent "+sideName,mantle,Vector3.zero,new[]{
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
            Part(root,owned,"Field mantle drape",fabric,Vector3.zero,new[]{
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
            if(kind==0 || kind==1)
            {
                // First contact: visually refines the actual Mission 11 enemy
                // meshes, rather than only replacing their labels or making
                // further disconnected ArtReview studies.
                AshboundFirstContactDetails(root,kind,skin,mantle,
                    fabric,gear,conduit,dark,owned);
                GroupAshboundMotion(root);
            }
        }

        /// <summary>
        /// Added anatomical relief and wearable gear on the two first-contact
        /// anonymous Avarin. Every feature is provisional artwork; no new
        /// biology, magical organs, bonded creature or weapon lore is asserted.
        /// </summary>
        private static void AshboundFirstContactDetails(GameObject root,int kind,
            Material skin,Material mantle,Material fabric,Material gear,
            Material conduit,Material dark,List<Mesh> owned)
        {
            bool striker=kind==1;
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
                        rib==1?skin:mantle,Vector3.zero,
                        new[]{P(side*(.105f+reach),.63f,-.108f),
                              P(side*(.23f+reach),.69f,-.090f),
                              P(side*(.37f+reach),.74f,-.067f),
                              P(side*(.39f+reach),.83f,-.045f)},
                        new[]{R(.010f,.011f),R(.014f,.012f),
                              R(.012f,.010f),R(.003f,.004f)},10);
                }

                // Continuous layered muscle and soft field wrapping;
                // narrower Forager and heavier reinforced Striker shapes.
                Part(root,owned,"Clavicle contour "+half,skin,Vector3.zero,
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
            Part(root,owned,"Upper field wrap",fabric,Vector3.zero,
                new[]{P(0,.258f,.122f),P(0,.145f,.160f),
                      P(0,.016f,.163f),P(0,-.100f,.147f)},
                new[]{R(.133f,.039f),R(striker ? .225f : .172f,.046f),
                      R(striker ? .225f : .170f,.052f),R(.113f,.038f)},24);
            // Overlapping chest-wear strips read clearly at portrait zoom.
            for(int strip=0;strip<4;strip++)
            {
                float side=strip<2?-1f:1f;
                float y=.195f-(strip%2)*.089f;
                Part(root,owned,"Cross body harness "+strip,gear,Vector3.zero,
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

        /// <summary>
        /// Pivots keep eyes attached to the face, living mantle attached to
        /// the head and equipment attached to its actual limb. This corrects
        /// floating accessories in the prior independent-part motion.
        /// Per-limb contact remains a review study, not skeletal IK.
        /// </summary>
        private static void GroupAshboundMotion(GameObject root)
        {
            Transform Pivot(string label,Vector3 center,Transform parent)
            {
                var t=new GameObject(label).transform;
                t.SetParent(parent,false);t.localPosition=center;
                return t;
            }
            var core=Pivot("Ashbound motion core",P(0,-.13f,-.01f),root.transform);
            var head=Pivot("Ashbound motion head",P(0,.64f,.0f),core);
            var leftArm=Pivot("Ashbound motion arm Left",P(-.245f,.28f,-.02f),core);
            var rightArm=Pivot("Ashbound motion arm Right",P(.245f,.28f,-.02f),core);
            var leftLeg=Pivot("Ashbound motion leg Left",P(-.15f,-.32f,-.02f),root.transform);
            var rightLeg=Pivot("Ashbound motion leg Right",P(.15f,-.32f,-.02f),root.transform);
            var parts=new List<Transform>();
            foreach(Transform child in root.transform)
                if(child!=core && child!=leftLeg && child!=rightLeg)
                    parts.Add(child);
            foreach(var part in parts)
            {
                string n=part.name;
                Transform target=core;
                bool left=n.EndsWith("Left",StringComparison.Ordinal) ||
                    n.Contains(" Left ",StringComparison.Ordinal);
                bool right=n.EndsWith("Right",StringComparison.Ordinal) ||
                    n.Contains(" Right ",StringComparison.Ordinal);
                if(n.StartsWith("Avarin leg ",StringComparison.Ordinal) ||
                   n.StartsWith("Avarin foot ",StringComparison.Ordinal) ||
                   n.StartsWith("Shin reinforcement ",StringComparison.Ordinal) ||
                   n.StartsWith("Heel geometry ",StringComparison.Ordinal))
                    target=left?leftLeg:rightLeg;
                else if(n.StartsWith("Avarin arm ",StringComparison.Ordinal) ||
                        n.StartsWith("Avarin forearm relief ",StringComparison.Ordinal) ||
                        n.StartsWith("Field wrap ",StringComparison.Ordinal) ||
                        n.StartsWith("Striker right arm ",StringComparison.Ordinal) ||
                        n.StartsWith("Striker blunt ",StringComparison.Ordinal) ||
                        n.StartsWith("Reinforced forearm ",StringComparison.Ordinal) ||
                        n.StartsWith("Forager hooked ",StringComparison.Ordinal) ||
                        n.StartsWith("Forager reader ",StringComparison.Ordinal) ||
                        n.StartsWith("Field sensor frame",StringComparison.Ordinal))
                    target=left?leftArm:rightArm;
                else if(n.StartsWith("Avarin temple face",StringComparison.Ordinal) ||
                        n.StartsWith("Living crescent ",StringComparison.Ordinal) ||
                        n.StartsWith("Mantle filament ",StringComparison.Ordinal) ||
                        n.StartsWith("Temple ",StringComparison.Ordinal))
                    target=head;
                part.SetParent(target,true);
            }
        }
    }
}
