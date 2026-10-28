using System;

namespace Ninefold.Editor
{
    /// <summary>
    /// Pure, deterministic rest-space deformation for the first two Ashbound review meshes.
    /// Does not change their source vertices, author a humanoid rig, or control combat timing.
    /// Kept Unity-free so the real math runs in the net8 repository regression suite.
    /// </summary>
    internal static class AshboundMotion
    {
        internal enum Clip { Idle, Advance, Attack, Hit, Afterecho, Defeat }

        internal readonly struct Frame
        {
            internal readonly Clip Motion;
            internal readonly double Time, Breath, Sway, Walk, Attack, Recoil, Strain, Twitch, Collapse;
            internal Frame(Clip motion, double time, double breath, double sway,
                double walk, double attack, double recoil, double strain, double twitch, double collapse)
            {
                Motion = motion; Time = time; Breath = breath; Sway = sway;
                Walk = walk; Attack = attack; Recoil = recoil; Strain = strain;
                Twitch = twitch; Collapse = collapse;
            }
        }

        internal static double Duration(Clip clip, bool striker)
        {
            switch (clip)
            {
                case Clip.Idle: return striker ? 3.0 : 2.7;
                case Clip.Advance: return striker ? 1.40 : 1.08;
                case Clip.Attack: return striker ? 1.05 : .76;
                case Clip.Hit: return .70;
                case Clip.Afterecho: return 1.65;
                case Clip.Defeat: return 1.35;
                default: throw new ArgumentOutOfRangeException(nameof(clip));
            }
        }

        private static double Clamp(double t) => Math.Max(0, Math.Min(1, t));
        private static double Ease(double t) { t = Clamp(t); return t*t*(3-2*t); }
        private static double Pulse(double t) { double s=Math.Sin(Math.PI*Clamp(t)); return s*s*s*s; }
        private static double Swing(double t) => Math.Sin(2*Math.PI*t);

        internal static Frame Sample(Clip clip, double time)
        {
            if (double.IsNaN(time) || double.IsInfinity(time))
                throw new ArgumentOutOfRangeException(nameof(time));
            time = Clamp(time);
            double breathing = 0, sway = 0, walk = 0, attack = 0, recoil = 0;
            double strain = 0, twitch = 0, collapse = 0;
            switch (clip)
            {
                case Clip.Idle:
                    breathing = Swing(time);
                    sway = Math.Sin(2*Math.PI*time + .35)-Math.Sin(.35);
                    // Window endpoints are rest-pose identical despite the offset phase.
                    sway *= Math.Sin(Math.PI*time)*Math.Sin(Math.PI*time);
                    break;
                case Clip.Advance:
                    walk = Swing(time);
                    breathing = .4*Swing(time*2);
                    sway = .5*Swing(time);
                    break;
                case Clip.Attack:
                    attack = Pulse(time);
                    breathing = .55*attack;
                    break;
                case Clip.Hit:
                    recoil = Pulse(time);
                    break;
                case Clip.Afterecho:
                    strain = Pulse(time);
                    twitch = strain*(.65*Math.Sin(18*Math.PI*time)+.35*Math.Sin(26*Math.PI*time));
                    break;
                case Clip.Defeat:
                    collapse = Ease(time);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(clip));
            }
            return new Frame(clip, time, breathing, sway, walk,
                attack, recoil, strain, twitch, collapse);
        }

        private static bool Has(string value, string part) =>
            value.StartsWith(part, StringComparison.Ordinal);
        private static bool RightPart(string name) =>
            name.EndsWith("_R", StringComparison.Ordinal) || name.Contains("Striker_Right") ||
            Has(name, "Forager_Probe") || Has(name, "Forager_SensorEnd");
        private static bool LeftPart(string name) =>
            name.EndsWith("_L", StringComparison.Ordinal);
        private static bool LegPart(string name) =>
            Has(name,"Avarin_Leg_") || Has(name,"Avarin_Foot_") ||
            Has(name,"Salvage_ShinShell_") || Has(name,"Salvage_BootEdge_") ||
            Has(name,"Wrecker_LegBrace_");
        private static bool ArmPart(string name) =>
            Has(name,"Avarin_Arm_") || Has(name,"Avarin_Hand_") ||
            Has(name,"Striker_RightForearmPlate") || Has(name,"Striker_ImpactRidge") ||
            Has(name,"Striker_ImpactNode") || Has(name,"Forager_Probe") ||
            Has(name,"Forager_SensorEnd");
        private static bool MantlePart(string name) =>
            Has(name,"Avarin_LivingCrescentMantle") || Has(name,"Avarin_CrescentRim") ||
            Has(name,"Avarin_MantleSensingRibbon_");

        /// <summary>
        /// Evaluates from immutable rest coordinates, never the previous animated frame.
        /// Keeps both soles on the same baseline when planted; derives attachment
        /// motion from individual named source parts rather than a rigid whole-body bob.
        /// </summary>
        internal static void Vertex(string part, float[] rest, Frame frame, bool striker,
            out float x, out float y, out float z)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            if (rest == null || rest.Length != 3 ||
                double.IsNaN(rest[0]) || double.IsNaN(rest[1]) || double.IsNaN(rest[2]) ||
                double.IsInfinity(rest[0]) || double.IsInfinity(rest[1]) || double.IsInfinity(rest[2]))
                throw new ArgumentException("Expected a finite three-coordinate source vertex.", nameof(rest));

            double px=rest[0], py=rest[1], pz=rest[2];
            bool leg=LegPart(part), arm=ArmPart(part), mantle=MantlePart(part);
            int side=RightPart(part) ? 1 : LeftPart(part) ? -1 : 0;
            double chest=Ease((py-.76)/.71), neck=Ease((py-1.40)/.28);
            double torsoBend=-.070*frame.Attack+.075*frame.Recoil+.15*frame.Strain+.42*frame.Collapse;
            double breathing=.013*frame.Breath*Ease((py-.9)/.42);
            double sway=(striker ? .008 : .013)*frame.Sway*chest;

            if (leg && side != 0)
            {
                // In-place walk with opposing hips, a knee-oriented lower-leg arc
                // and a corrected ankle baseline. Swinging sole lifts; planted sole does not.
                double phase=frame.Time*2*Math.PI+(side<0 ? 0 : Math.PI);
                double cycle=Math.Sin(phase);
                double angle=(striker ? .21 : .29)*frame.Walk* (side<0 ? 1 : -1);
                // Walk is active throughout the cycle, not only near its peaks.
                if(frame.Motion==Clip.Advance) angle=(striker ? .21 : .29)*cycle;
                angle+=(side<0 ? -.10:.11)*frame.Attack;
                angle+=(side<0 ? -.18:.18)*frame.Collapse;
                double pivotY=.90, pivotZ=-.024;
                double dy=py-pivotY,dz=pz-pivotZ;
                double rotatedY=pivotY+dy*Math.Cos(angle)-dz*Math.Sin(angle);
                double rotatedZ=pivotZ+dy*Math.Sin(angle)+dz*Math.Cos(angle);
                // Transform the corresponding boot's nominal ankle point using
                // the exact same rotation, then cancel its vertical drop.
                double ankleZ=.091+(striker ? .17 : .07)*(side<0 ? -.26:1);
                double ankleDy=.063-pivotY,ankleDz=ankleZ-pivotZ;
                double ankleY=pivotY+ankleDy*Math.Cos(angle)-ankleDz*Math.Sin(angle);
                double footWeight=1-Ease((py-.20)/.66);
                double lift=frame.Motion==Clip.Advance ? .070*Math.Pow(Math.Max(0,cycle),2) : 0;
                py=rotatedY+footWeight*(.063-ankleY+lift)
                    -.22*frame.Collapse*Ease((rest[1]-.17)/.70);
                pz=rotatedZ+(striker ? .035 : .055)*frame.Walk*footWeight;
                px+=.020*frame.Recoil*side+.12*frame.Collapse*Ease((rest[1]-.16)/.72);
            }
            else if (arm && side != 0)
            {
                double shoulderY=1.34, shoulderZ=.0;
                double dy=py-shoulderY,dz=pz-shoulderZ;
                double angle=(striker ? .19 : .27)*frame.Walk*(side<0 ? -1 : 1);
                // Larger attack reach on the right; left stays as a counterbalance.
                angle+=(side>0 ? -.48:.18)*frame.Attack;
                angle+=(side>0 ? .31:-.19)*frame.Recoil;
                angle+=(side>0 ? .12:-.15)*frame.Strain;
                angle+=(side>0 ? -.28:.33)*frame.Collapse;
                py=shoulderY+dy*Math.Cos(angle)-dz*Math.Sin(angle)
                    -.06*frame.Collapse;
                pz=shoulderZ+dy*Math.Sin(angle)+dz*Math.Cos(angle)
                    +.10*frame.Attack*(side>0 ? 1:.36)
                    -.06*frame.Recoil;
                px+=(striker ? .014 : .023)*frame.Walk*side;
            }
            else
            {
                // Shared organic balance. Armour/harness stays attached to its owner.
                double hinge=.84;
                double bend=torsoBend*chest;
                double dy=py-hinge,dz=pz;
                py=hinge+dy*Math.Cos(bend)-dz*Math.Sin(bend)
                    +breathing-.11*frame.Collapse*chest;
                pz=dy*Math.Sin(bend)+dz*Math.Cos(bend)
                    +.12*frame.Attack*chest-.070*frame.Recoil*chest;
                px+=sway+.060*frame.Collapse*chest;
            }

            // Rest-space head/mantle motion remains restrained. The mantle is
            // sensory anatomy (not a bond receptor), so it reacts to strain
            // without growing new lobes or becoming a magic shield.
            if(mantle)
            {
                double rim=Ease((rest[1]-1.46)/.28);
                px+=.014*rim*frame.Breath+.020*rim*frame.Twitch;
                py+=.005*rim*frame.Breath-.009*rim*frame.Strain;
                pz+=.012*rim*frame.Twitch;
            }
            else if(Has(part,"Avarin_TempleFace") || Has(part,"Avarin_Eye_") ||
                Has(part,"Avarin_TempleRelief_") || Has(part,"Avarin_Forehead") ||
                Has(part,"Avarin_Mouth"))
            {
                px+=.008*neck*frame.Twitch;
                pz+=.010*neck*frame.Recoil;
            }
            // Physically small, deterministic postures. Never write or move source.
            x=(float)px; y=(float)py; z=(float)pz;
        }
    }
}
