using System;
using System.Collections.Generic;

namespace Ninefold.Editor
{
    /// <summary>
    /// First-contact review skeleton: explicit Avarin biped hierarchy, deterministic
    /// two-joint continuous skin weights and six bone-pose studies.
    /// Has no Unity dependency; separate from previous CPU vertex deformation.
    /// This is a rigging *study*, not locked production anatomy/animation approval.
    /// </summary>
    internal static class AshboundSkeleton
    {
        internal readonly struct Joint
        {
            internal readonly string Name;
            internal readonly int Parent;
            internal readonly float X,Y,Z;
            internal Joint(string name, int parent, float x,float y,float z)
            { Name=name;Parent=parent;X=x;Y=y;Z=z; }
        }
        internal readonly struct Weight
        {
            internal readonly int First,Second;
            internal readonly float Primary,Secondary;
            internal Weight(int first,int second,float secondary)
            {
                First=first;Second=second;Secondary=second==first?0:secondary;
                Primary=1-Secondary;
            }
        }
        internal readonly struct Pose
        {
            internal readonly float X,Y,Z,PositionY;
            internal Pose(float x,float y,float z,float py=0)
            { X=x;Y=y;Z=z;PositionY=py; }
        }

        // World-space bind coordinates; each Unity Transform gets (joint-parent)
        // local translation. A single skeleton spans all material submeshes.
        internal static readonly Joint[] Bones = {
            new Joint("Root",-1,0,0,0),                         // 0
            new Joint("Pelvis",0,0,.87f,-.025f),                 // 1
            new Joint("Spine",1,0,1.10f,-.02f),                  // 2
            new Joint("Chest",2,0,1.34f,-.025f),                 // 3
            new Joint("Neck",3,0,1.52f,-.015f),                  // 4
            new Joint("Face",4,0,1.665f,.013f),                  // 5
            new Joint("LivingCrescentMantle",4,0,1.52f,-.13f),  // 6
            new Joint("LeftShoulder",3,-.182f,1.398f,-.018f),  // 7
            new Joint("LeftElbow",7,-.256f,1.125f,.045f),      // 8
            new Joint("LeftHand",8,-.252f,.876f,.144f),        // 9
            new Joint("RightShoulder",3,.182f,1.398f,-.018f), // 10
            new Joint("RightElbow",10,.256f,1.125f,.045f),     // 11
            new Joint("RightHand",11,.252f,.876f,.144f),       // 12
            new Joint("LeftHip",1,-.11f,.86f,-.02f),            // 13
            new Joint("LeftKnee",13,-.135f,.49f,.025f),        // 14
            new Joint("LeftAnkle",14,-.154f,.125f,.078f),      // 15
            new Joint("RightHip",1,.11f,.86f,-.02f),            // 16
            new Joint("RightKnee",16,.135f,.49f,.025f),        // 17
            new Joint("RightAnkle",17,.154f,.125f,.078f)       // 18
        };
        internal static int Count => Bones.Length;
        private static bool Starts(string input,string s) =>
            input.StartsWith(s,StringComparison.Ordinal);
        private static double Clamp(double t) => Math.Max(0,Math.Min(1,t));
        private static float Ease(double t)
        {
            t=Clamp(t);
            return (float)(t*t*(3-2*t));
        }
        private static float Wave(double t) => (float)Math.Sin(2*Math.PI*t);
        private static float Pulse(double t)
        {
            double s=Math.Sin(Math.PI*Clamp(t));
            return (float)(s*s*s*s);
        }
        private static Weight Blend(int a,int b,float fraction) =>
            new Weight(a,b,Ease(fraction));

        internal static Weight Skin(string part,float[] coordinate)
        {
            if(string.IsNullOrEmpty(part)) throw new ArgumentException("Named source part is required.",nameof(part));
            if(coordinate==null || coordinate.Length!=3 ||
                float.IsNaN(coordinate[0])||float.IsNaN(coordinate[1])||float.IsNaN(coordinate[2])||
                float.IsInfinity(coordinate[0])||float.IsInfinity(coordinate[1])||float.IsInfinity(coordinate[2]))
                throw new ArgumentException("Finite rest-space vertex expected.",nameof(coordinate));
            float y=coordinate[1];

            if(Starts(part,"Avarin_LivingCrescentMantle") ||
               Starts(part,"Avarin_CrescentRim") || Starts(part,"Avarin_MantleSensingRibbon_"))
                return Blend(4,6,.85f+.15f*Ease((y-1.50f)/.24f));
            if(Starts(part,"Avarin_TempleFace") || Starts(part,"Avarin_Eye_") ||
               Starts(part,"Avarin_TempleRelief_") || Starts(part,"Avarin_Forehead") ||
               Starts(part,"Avarin_Mouth"))
                return Blend(4,5,.92f);

            bool left=part.EndsWith("_L",StringComparison.Ordinal);
            bool right=part.EndsWith("_R",StringComparison.Ordinal);
            if(Starts(part,"Avarin_Leg_") || Starts(part,"Salvage_ShinShell_") ||
               Starts(part,"Avarin_Foot_") || Starts(part,"Salvage_BootEdge_"))
            {
                int hip=left?13:16,knee=left?14:17,ankle=left?15:18;
                if(Starts(part,"Avarin_Foot_")||Starts(part,"Salvage_BootEdge_"))
                    return new Weight(ankle,ankle,0);
                return y>=.49f ? Blend(knee,hip,(y-.45f)/.41f)
                               : Blend(ankle,knee,(y-.14f)/.36f);
            }
            if(Starts(part,"Avarin_Arm_") || Starts(part,"Avarin_Hand_") ||
               Starts(part,"Salvage_BootEdge_ForearmNever"))
            {
                int shoulder=left?7:10,elbow=left?8:11,hand=left?9:12;
                if(Starts(part,"Avarin_Hand_"))return new Weight(hand,hand,0);
                return y>=1.125f ? Blend(elbow,shoulder,(y-1.105f)/.30f)
                                 : Blend(hand,elbow,(y-.865f)/.26f);
            }
            if(Starts(part,"Forager_Probe")||Starts(part,"Forager_SensorEnd")||
               Starts(part,"Striker_RightForearmPlate")||
               Starts(part,"Striker_ImpactRidge")||Starts(part,"Striker_ImpactNode"))
                return new Weight(12,12,0);
            if(Starts(part,"Striker_OverbuiltRightShoulder"))
                return new Weight(10,10,0);
            if(Starts(part,"Striker_BrokenLeftShoulder"))
                return new Weight(7,7,0);
            if(Starts(part,"Forager_OffsetShoulder"))
                return new Weight(10,10,0);
            if(Starts(part,"Forager_LowRouteSatchel")||Starts(part,"Forager_TrailTags")||
               Starts(part,"Forager_SatchelTie")||Starts(part,"Salvage_SplitFieldApron"))
                return new Weight(1,1,0);
            if(Starts(part,"Avarin_Neck"))return Blend(3,4,(y-1.42f)/.16f);
            if(Starts(part,"Avarin_Torso")||Starts(part,"Avarin_ChestProtection")||
               Starts(part,"Salvage_ChestBraid")||Starts(part,"Salvage_WaistHousing")||
               Starts(part,"Wake_")||Starts(part,"Striker_AfterechoStabilizer")||
               Starts(part,"Striker_StabilizerVane"))
                return y<=1.10f ? Blend(1,2,(y-.82f)/.28f)
                     : Blend(2,3,(y-1.08f)/.26f);

            // Every accessory is explicitly bound to this creature's
            // physical torso or pelvis, never to a phantom companion.
            return y<1.04f?Blend(1,2,(y-.80f)/.23f)
                  :Blend(2,3,(y-1.04f)/.31f);
        }

        /// <summary>
        /// Euler offsets (degrees) to the anatomical local bind orientation,
        /// plus optional pelvis vertical shift in meters. No root movement.
        /// All nonterminal clips start and end at bind pose.
        /// </summary>
        internal static Pose Sample(int bone,AshboundMotion.Clip clip,double time,bool striker)
        {
            if(bone<0||bone>=Count)throw new ArgumentOutOfRangeException(nameof(bone));
            if(double.IsNaN(time)||double.IsInfinity(time))throw new ArgumentOutOfRangeException(nameof(time));
            time=Clamp(time);
            float beat=Wave(time), doubleBeat=Wave(time*2),pulse=Pulse(time);
            float heavy=striker?1.13f:1f,settle=Ease(time);
            switch(clip)
            {
                case AshboundMotion.Clip.Idle:
                    if(bone==1)return new Pose(.7f*beat,0,.6f*beat);
                    if(bone==2)return new Pose(.8f*doubleBeat,0,.7f*beat);
                    if(bone==3)return new Pose(-1.4f*beat,0,-.55f*beat);
                    if(bone==5)return new Pose(.65f*beat,.8f*beat,0);
                    if(bone==6)return new Pose(1.6f*beat,.7f*doubleBeat,1.1f*beat);
                    if(bone==7||bone==10)return new Pose((bone==7?-1:1)*.8f*beat,0,0);
                    return new Pose(0,0,0);
                case AshboundMotion.Clip.Advance:
                    if(bone==1)return new Pose(2f*doubleBeat,1.1f*beat,2.6f*heavy*beat);
                    if(bone==2)return new Pose(3.2f*doubleBeat,0,2f*beat);
                    if(bone==3)return new Pose(-2f*doubleBeat,0,-1.4f*beat);
                    if(bone==5)return new Pose(.75f*doubleBeat,-.9f*beat,0);
                    if(bone==6)return new Pose(1.8f*doubleBeat,0,1.8f*beat);
                    if(bone==13)return new Pose(16f*beat/heavy,0,0);
                    if(bone==16)return new Pose(-16f*beat/heavy,0,0);
                    if(bone==14)return new Pose(Math.Max(0,-beat)*25f,0,0);
                    if(bone==17)return new Pose(Math.Max(0,beat)*25f,0,0);
                    if(bone==15)return new Pose(-8f*beat,0,0);
                    if(bone==18)return new Pose(8f*beat,0,0);
                    if(bone==7)return new Pose(-10f*beat,0,-.9f*beat);
                    if(bone==10)return new Pose(10f*beat,0,.9f*beat);
                    if(bone==8)return new Pose(-2f*beat,0,0);
                    if(bone==11)return new Pose(2f*beat,0,0);
                    return new Pose(0,0,0);
                case AshboundMotion.Clip.Attack:
                    if(bone==1)return new Pose(4f*pulse,0,-2f*pulse);
                    if(bone==2)return new Pose(-11f*pulse,0,-3f*pulse);
                    if(bone==3)return new Pose(-8f*pulse,2f*pulse,3f*pulse);
                    if(bone==10)return new Pose(-37f*heavy*pulse,-4f*pulse,7f*pulse);
                    if(bone==11)return new Pose(-38f*heavy*pulse,0,0);
                    if(bone==12)return new Pose(-9f*pulse,0,0);
                    if(bone==7)return new Pose(14f*pulse,0,-8f*pulse);
                    if(bone==5)return new Pose(7f*pulse,0,0);
                    if(bone==6)return new Pose(6f*pulse,2f*pulse,0);
                    return new Pose(0,0,0);
                case AshboundMotion.Clip.Hit:
                    if(bone==1)return new Pose(-6f*pulse,0,3f*pulse);
                    if(bone==2)return new Pose(15f*pulse,0,4f*pulse);
                    if(bone==3)return new Pose(5f*pulse,0,3f*pulse);
                    if(bone==5)return new Pose(8f*pulse,-5f*pulse,0);
                    if(bone==6)return new Pose(-12f*pulse,0,7f*pulse);
                    if(bone==7)return new Pose(9f*pulse,0,0);
                    if(bone==10)return new Pose(13f*pulse,0,0);
                    return new Pose(0,0,0);
                case AshboundMotion.Clip.Afterecho:
                    float tremor=(float)(Math.Sin(18*Math.PI*time)*.65+
                        Math.Sin(26*Math.PI*time)*.35)*pulse;
                    if(bone==2)return new Pose(11f*pulse,0,4f*tremor);
                    if(bone==3)return new Pose(-7f*pulse,2f*tremor,5f*tremor);
                    if(bone==5)return new Pose(7f*pulse,7f*tremor,0);
                    if(bone==6)return new Pose(-14f*pulse,9f*tremor,11f*tremor);
                    if(bone==7||bone==10)return new Pose(-6f*pulse,0,4f*tremor);
                    if(bone==11||bone==8)return new Pose(5f*pulse,0,0);
                    return new Pose(0,0,0);
                case AshboundMotion.Clip.Defeat:
                    if(bone==1)return new Pose(18f*settle,0,8f*settle,-.17f*settle);
                    if(bone==2)return new Pose(46f*settle,0,5f*settle);
                    if(bone==3)return new Pose(22f*settle,0,0);
                    if(bone==5)return new Pose(15f*settle,0,0);
                    if(bone==6)return new Pose(-22f*settle,0,0);
                    if(bone==7||bone==10)return new Pose(24f*settle,0,(bone==7?-12:12)*settle);
                    if(bone==13||bone==16)return new Pose(-10f*settle,0,0);
                    if(bone==14||bone==17)return new Pose(24f*settle,0,0);
                    return new Pose(0,0,0);
                default:throw new ArgumentOutOfRangeException(nameof(clip));
            }
        }
    }
}
