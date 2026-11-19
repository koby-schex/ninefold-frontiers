using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace Ninefold.Core.Content
{
    // Visual mapping of the four existing standard slots from
    // WorldFivePlayableAbilityStudy. Never alters ownership, kits or IDs.
    public static class PlayableBondRig
    {
        public enum Bond { GreatStrider, LivingVeil, Riftwalker, Burdenbeast }
        public static bool TrySelect(string mission,string unit,bool squad,out Bond bond)
        {
            bond=default;
            if(!squad||mission==null||unit==null)return false;
            bool world5=WorldFiveStorySequence.IsTutorial(mission);
            if(!world5 && mission.StartsWith("w5-story-",StringComparison.Ordinal) &&
                mission.Length==12 && int.TryParse(mission.Substring(9),NumberStyles.None,
                CultureInfo.InvariantCulture,out int number))
                world5=number>=11&&number<=100;
            if(!world5)return false;
            int index=Array.IndexOf(WorldFivePlayableAbilityStudy.StandardIds,unit);
            if(index<0||index>3)return false;
            bond=(Bond)index;return true;
        }
        public const int Body=0,Head=1,Partner=2,PartnerHead=3,LeftArm=4,RightArm=5,
            LeftLeg=6,RightLeg=7,Mantle=8,Tail=9,FirstLimb=10;
        private static bool Starts(string s,string p)=>s.StartsWith(p,StringComparison.Ordinal);
        private static bool Has(string s,string p)=>s.IndexOf(p,StringComparison.Ordinal)>=0;
        private static bool IsPartner(string p)=>Starts(p,"Avarin_")||Starts(p,"VeilHarness_");
        private static bool IsMounted(string p)=>IsPartner(p)||Starts(p,"Cradle_")||Starts(p,"Waycaster_");
        private static readonly string[] StriderLimbs={"L_Fore","R_Rear","R_Fore","L_Rear"};
        private static readonly string[] BurdenLimbs={"L0","R0","L1","R1","L2","R2","L3","R3"};
        public static int Limbs(Bond bond)=>bond==Bond.GreatStrider?4:bond==Bond.Burdenbeast?8:0;
        public static int BoneCount(Bond bond)=>FirstLimb+3*Limbs(bond);
        public static bool IsOlyra(string p)=>Starts(p,"Olyra_");
        public static bool IsPartnerPart(string p)=>IsPartner(p);
        public static string LimbLabel(Bond bond,int limb)=>
            bond==Bond.GreatStrider?StriderLimbs[limb]:BurdenLimbs[limb];
        public static int LimbIndex(Bond bond,string name)
        {
            if(bond==Bond.GreatStrider&&Starts(name,"Ilyth_"))
            {
                for(int i=0;i<StriderLimbs.Length;i++)if(Has(name,StriderLimbs[i]))return i;
            }
            if(bond==Bond.Burdenbeast&&Starts(name,"Tervan_"))
            {
                for(int i=0;i<BurdenLimbs.Length;i++)if(name.EndsWith(BurdenLimbs[i],StringComparison.Ordinal))return i;
            }
            return -1;
        }
        public static bool IsContact(string part)=>Has(part,"Contact_")||Has(part,"ContactHeel_");
        public static Vector3 V(float[] v)=>new Vector3(v[0],v[1],v[2]);
        public readonly struct Weight
        {
            public readonly int A,B;public readonly float W;
            public Weight(int a,int b,float w){A=a;B=b;W=w;}
        }
        public sealed class Plan
        {
            public readonly Bond Kind;
            public readonly Vector3[] Pivots;
            public readonly Vector3 Min,Max;
            public readonly Vector3[] LimbMin,LimbMax;
            public readonly int[] Coverage;
            internal Plan(Bond kind,Vector3[] pivots,Vector3 min,Vector3 max,Vector3[] lo,Vector3[] hi,int[] coverage)
            {Kind=kind;Pivots=pivots;Min=min;Max=max;LimbMin=lo;LimbMax=hi;Coverage=coverage;}
        }
        private static int Region(Bond bond,string part)
        {
            if(bond==Bond.GreatStrider&&IsMounted(part))
                return Starts(part,"Cradle_Attachment")?Body:Partner;
            if(IsPartner(part))
            {
                if(Starts(part,"Avarin_Arm_")||Starts(part,"Avarin_Hand_")||
                    Starts(part,"Avarin_ForearmGuard_")||Starts(part,"Avarin_Cuff_"))
                    return Has(part,"_L")?LeftArm:RightArm;
                if(Starts(part,"Avarin_Leg_")||Starts(part,"Avarin_Foot_"))
                    return Has(part,"_L")?LeftLeg:RightLeg;
                if(Starts(part,"Avarin_Head")||Starts(part,"Avarin_Temple")||
                    Starts(part,"Avarin_Eye")||Starts(part,"Avarin_Mouth")||
                    Starts(part,"Avarin_Forehead")||Starts(part,"Avarin_UpperLid")||
                    Starts(part,"Avarin_LowerLid")||Starts(part,"Avarin_Crescent")||
                    Starts(part,"Avarin_Mantle")||Starts(part,"Avarin_SensoryRibbon"))
                    return PartnerHead;
                return Partner;
            }
            if(bond==Bond.GreatStrider)
            {
                if(Has(part,"SensorySail")||Has(part,"SailVein")||Has(part,"SailChannel")||Has(part,"SailRoot"))return Mantle;
                if(Has(part,"CompactHead")||Has(part,"CranialMineral")||Has(part,"FeedingSeam")||
                    Has(part,"Eye_")||Has(part,"Brow_")||Has(part,"ShortNeck"))return Head;
            }
            if(bond==Bond.Riftwalker&&Starts(part,"Vaskel_Counterweight"))return Tail;
            return Body;
        }
        private static Vector3 Inf(float sign)=>new Vector3(sign*float.PositiveInfinity);
        private static float Smooth(float a)
        {a=Math.Max(0,Math.Min(1,a));return a*a*(3-2*a);}
        public static Plan Build(Bond kind,PlayableBondSource.Model model)
        {
            int count=BoneCount(kind),limbs=Limbs(kind);
            var lower=Enumerable.Repeat(Inf(1),count).ToArray();
            var upper=Enumerable.Repeat(Inf(-1),count).ToArray();
            var lo=Enumerable.Repeat(Inf(1),limbs).ToArray();
            var hi=Enumerable.Repeat(Inf(-1),limbs).ToArray();
            var contactLo=Enumerable.Repeat(Inf(1),limbs).ToArray();
            var contactHi=Enumerable.Repeat(Inf(-1),limbs).ToArray();
            Vector3 min=Inf(1),max=Inf(-1);
            foreach(var s in model.Sections)
            for(int i=0;i<s.Vertices.Count;i++)
            {
                string name=s.SourceParts[i];Vector3 p=V(s.Vertices[i]);
                min=Vector3.Min(min,p);max=Vector3.Max(max,p);
                int limb=LimbIndex(kind,name);
                if(limb>=0)
                {
                    lo[limb]=Vector3.Min(lo[limb],p);hi[limb]=Vector3.Max(hi[limb],p);
                    if(IsContact(name))
                    {
                        contactLo[limb]=Vector3.Min(contactLo[limb],p);
                        contactHi[limb]=Vector3.Max(contactHi[limb],p);
                    }
                }
                else
                {
                    int region=Region(kind,name);
                    lower[region]=Vector3.Min(lower[region],p);upper[region]=Vector3.Max(upper[region],p);
                }
            }
            var pivots=new Vector3[count];var coverage=new int[count];
            for(int b=0;b<FirstLimb;b++)
            {
                if(float.IsInfinity(lower[b].X))continue;
                Vector3 p=(lower[b]+upper[b])*.5f;
                if(b>=LeftArm&&b<=RightLeg)p.Y=upper[b].Y;
                if(b==PartnerHead)p.Y=lower[b].Y;
                pivots[b]=p;coverage[b]=1;
            }
            // Actual contact pads define their own rigid stance transforms.
            // Upper/lower pivot heights follow the real source limb extents.
            for(int i=0;i<limbs;i++)
            {
                if(float.IsInfinity(lo[i].X)||float.IsInfinity(contactLo[i].X))
                    throw new InvalidOperationException("Missing authored limb/contact: "+LimbLabel(kind,i));
                int j=FirstLimb+i*3;
                Vector3 c=(contactLo[i]+contactHi[i])*.5f;
                var mid=(lo[i]+hi[i])*.5f;
                float side=c.X<0?-1:1;
                float inner=side<0?hi[i].X:lo[i].X;
                pivots[j]=new Vector3(inner,hi[i].Y,mid.Z);
                pivots[j+1]=new Vector3(mid.X,mid.Y,mid.Z);
                pivots[j+2]=c;coverage[j]=coverage[j+1]=coverage[j+2]=1;
            }
            return new Plan(kind,pivots,min,max,lo,hi,coverage);
        }
        public static Weight Skin(Plan plan,string part,Vector3 point)
        {
            int limb=LimbIndex(plan.Kind,part);
            if(limb>=0)
            {
                int j=FirstLimb+limb*3;
                if(IsContact(part)||Has(part,"Ankle_"))return new Weight(j+2,j+2,1);
                float t=(point.Y-plan.LimbMin[limb].Y)/
                    Math.Max(.001f,plan.LimbMax[limb].Y-plan.LimbMin[limb].Y);
                if(t>.75f)return new Weight(Body,j,Smooth((t-.75f)/.25f));
                if(t>.45f)return new Weight(j,j+1,Smooth((t-.45f)/.30f));
                return new Weight(j+1,j+2,Smooth(t/.45f));
            }
            int b=Region(plan.Kind,part);
            // The inner ends of the independent standing Avarin's skin and
            // fieldwear follow the torso at their anatomical attachments.
            // Keep hand/foot accessories entirely on their proper limb.
            if((b==LeftArm||b==RightArm)&&Starts(part,"Avarin_Arm_"))
                return new Weight(b,Partner,Smooth((plan.Pivots[b].Y-point.Y)/.19f));
            if((b==LeftLeg||b==RightLeg)&&Starts(part,"Avarin_Leg_"))
                return new Weight(b,Partner,Smooth((plan.Pivots[b].Y-point.Y)/.22f));
            if(b==Tail)
                return new Weight(b,Body,Smooth(Math.Abs(point.X-plan.Pivots[Body].X)/.55f));
            return new Weight(b,b,1);
        }
        public static Vector3 ProtectiveVertex(string part,Vector3 rest,Vector3 protective)
            =>IsOlyra(part)?protective+new Vector3(1.70f,0,.35f):rest;
        public static float Shelter(float action,bool protect)
        {
            if(!protect)return 0;
            float t=Math.Max(0,Math.Min(1,action));
            return Smooth(Math.Min(t/.28f,(1-t)/.32f));
        }
        public static float Fit(Vector3 min,Vector3 max,float height,float width)
        {
            var size=max-min;
            if(size.Y<=.001f||Math.Max(size.X,size.Z)<=.001f)
                throw new InvalidOperationException("Empty model bounds");
            return Math.Min(height/size.Y,width/Math.Max(size.X,size.Z));
        }
        public readonly struct BonePose
        {
            public readonly Vector3 Translation,Euler;
            public BonePose(Vector3 t,Vector3 e){Translation=t;Euler=e;}
        }
        // Pure bounded visual pose; no per-frame vertex generation or combat
        // movement/hit logic. Stance contact height remains at the rest plane.
        public static BonePose Pose(Plan plan,int b,float cycle,float motion,float attack,float hit,
            float defeat,float idle)
        {
            motion=Math.Max(0,Math.Min(1,motion));
            float phase=cycle*2f*MathF.PI;
            float wave=MathF.Sin(phase),height=plan.Max.Y-plan.Min.Y;
            float bob=(.005f*idle+.009f*motion*MathF.Sin(phase*2))*height;
            float effort=attack-hit*.6f;
            if(b>=FirstLimb)
            {
                int leg=(b-FirstLimb)/3,section=(b-FirstLimb)%3;
                float fraction=cycle+(float)leg/Limbs(plan.Kind);
                fraction-=MathF.Floor(fraction);
                float stance=plan.Kind==Bond.GreatStrider?.8f:.62f;
                float swing=fraction>stance?(fraction-stance)/(1-stance):0;
                float lift=motion*MathF.Pow(MathF.Sin(swing*MathF.PI),2);
                float stride=motion*MathF.Sin(fraction*2f*MathF.PI)*height*.022f;
                if(section==2)return new BonePose(new Vector3(0,lift*height*.04f,stride),Vector3.Zero);
                if(section==1)return new BonePose(new Vector3(0,lift*height*.018f,stride*.5f),
                    new Vector3(lift*8f+effort*3f,0,0));
                return new BonePose(new Vector3(0,bob,0),new Vector3(motion*wave*2f,0,0));
            }
            if(b==Partner||b==PartnerHead)
            {
                float rider=plan.Kind==Bond.GreatStrider?bob:0;
                return new BonePose(new Vector3(0,rider,0),
                    new Vector3(b==PartnerHead?effort*6f+idle*1.5f:effort*2f,0,0));
            }
            if(b==LeftArm||b==RightArm)
            {
                float side=b==LeftArm?-1:1;
                return new BonePose(Vector3.Zero,new Vector3(side*wave*motion*14f-effort*16f,0,side*attack*4));
            }
            if(b==LeftLeg||b==RightLeg)
            {
                float side=b==LeftLeg?-1:1;
                return new BonePose(Vector3.Zero,new Vector3(side*wave*motion*16f,0,0));
            }
            if(b==Tail)
            {
                // Vaskel remains suspended/legless; no foot or locomotor
                // appendage can ever be added by this rig.
                return new BonePose(new Vector3(0,bob,0),new Vector3(0,4f*wave*motion+idle*1.6f,effort*3f));
            }
            if(b==Mantle||b==Head)
                return new BonePose(new Vector3(0,bob,0),new Vector3(idle*.9f-effort*(b==Head?6f:3f),0,0));
            return new BonePose(new Vector3(0,bob,0),new Vector3(-effort*2f,0,defeat*8f));
        }
    }
}
