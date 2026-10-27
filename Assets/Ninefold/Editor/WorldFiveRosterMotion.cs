using System;

namespace Ninefold.Editor
{
    // Pure rest-space offsets shared by the review builder and executable tests.
    // Three linear blend shapes; no accumulation and no movement of contact pads.
    internal static class WorldFiveRosterMotion
    {
        internal static void Delta(string part, float[] v, int channel, out float x, out float y, out float z)
        {
            x=y=z=0;
            bool tervan=part.StartsWith("Tervan_",StringComparison.Ordinal);
            bool vaskel=part.StartsWith("Vaskel_",StringComparison.Ordinal);
            bool equipment=part.StartsWith("Civic",StringComparison.Ordinal)||part.StartsWith("Convergence_",StringComparison.Ordinal);
            if ((!tervan&&!vaskel&&!equipment)||part.Contains("Contact_")) return;
            double w=Math.Max(0,Math.Min(1,(v[1]-.28)/.72));w=w*w*(3-2*w);
            if (channel==0 && tervan && part=="Tervan_Torso")
            {
                // Lateral expansion tapers off under the rigid dorsal load platform.
                double flank=Math.Max(0,1-Math.Pow((v[1]-1.10)/.43,2));
                x=(float)(v[0]*.012*flank);
            }
            if (channel==1)
            {
                x=(float)(.012*w);
                y=(float)(.006*w);
                // The complete civic payload translates rigidly with its support bed.
                if(equipment) { x=.012f;y=.006f; }
            }
            if(channel==0 && vaskel) { z=(float)(.009*w); }
            if(channel==2 && part=="Vaskel_Counterweight")
            {
                double tip=Math.Max(0,Math.Min(1,(-v[0]-.30)/.60));tip=tip*tip*(3-2*tip);
                z=(float)(.035*tip);y=(float)(.012*tip);
            }
        }
    }
}
