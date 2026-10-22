using System;

namespace Ninefold.Editor
{
    // Editor study math only. No game state, Unity dependency, or animation timing rules.
    internal static class LivingVeilMotion
    {
        internal enum Clip { RestingIdle, Deploy, ProtectiveIdle, Retract }
        internal readonly struct Frame
        {
            public readonly double Blend, Lift, Shift, Wave;
            public Frame(double blend, double lift, double shift, double wave)
            { Blend = blend; Lift = lift; Shift = shift; Wave = wave; }
        }
        internal static double Duration(Clip clip) => clip == Clip.Retract ? 4.1 : clip == Clip.Deploy ? 3.6 : 4;
        private static double Ease(double t)
        { t = Math.Max(0, Math.Min(1, t)); return t*t*t*(t*(t*6-15)+10); }
        internal static Frame Sample(Clip clip, double time)
        {
            if (double.IsNaN(time) || double.IsInfinity(time)) throw new ArgumentOutOfRangeException(nameof(time));
            time = Math.Max(0, Math.Min(1, time));
            switch (clip)
            {
                case Clip.RestingIdle: return new Frame(0, 0, 0, Math.Sin(time*Math.PI*2));
                case Clip.ProtectiveIdle: return new Frame(1, 0, 1, Math.Sin(time*Math.PI*2));
                case Clip.Retract:
                    time = 1-time;
                    time += .12*Math.Pow(Math.Sin(Math.PI*time),2);
                    break;
                case Clip.Deploy: break;
                default: throw new ArgumentOutOfRangeException(nameof(clip));
            }
            if (time <= 0) return new Frame(0, 0, 0, 0);
            if (time >= 1) return new Frame(1, 0, 1, 0);
            double travel = Ease(time);
            double arch = 16*travel*travel*(1-travel)*(1-travel);
            // Lift, unfolding and drift overlap instead of stopping between phases.
            return new Frame(Ease((time-.22)/.70),
                Ease(time/.42)*(1-Ease((time-.32)/.68)), travel,
                .25*Math.Sin(time*Math.PI*2)*arch);
        }
        internal static void Vertex(bool organism, bool membrane, float[] rest, float[] shelter, Frame frame,
            out float x, out float y, out float z)
        {
            if (!organism) { x=rest[0]; y=rest[1]; z=rest[2]; return; }
            double px=rest[0]+(shelter[0]-rest[0])*frame.Blend;
            double py=rest[1]+(shelter[1]-rest[1])*frame.Blend;
            double pz=rest[2]+(shelter[2]-rest[2])*frame.Blend;
            double swell=membrane ? 1+.006*frame.Wave : 1;
            x=(float)(px*swell+1.70*frame.Shift);
            y=(float)(py+frame.Lift+.025*frame.Wave);
            z=(float)(pz*swell+.35*frame.Shift);
        }
        internal static bool IsMembrane(string part) => part.StartsWith("Olyra_Lobe_", StringComparison.Ordinal) ||
            part.StartsWith("Olyra_Rib_", StringComparison.Ordinal) || part.StartsWith("Olyra_Vein_", StringComparison.Ordinal);
    }
}
