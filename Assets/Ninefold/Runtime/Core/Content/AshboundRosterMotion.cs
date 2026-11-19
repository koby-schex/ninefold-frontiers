using System;

namespace Ninefold.Core.Content
{
    /// <summary>Visual-only distal joint articulation. Exact same role order as
    /// the authored Ashbound encounter catalog. The caller supplies actual battle
    /// movement/attack/impact/defeat envelopes, never infers a gameplay effect.
    /// Neutral returns all four joints to their rest rotations.</summary>
    public static class AshboundRosterMotion
    {
        public readonly struct Pose
        {
            public readonly float LeftElbow,RightElbow,LeftKnee,RightKnee,LeftTwist,RightTwist;
            internal Pose(float a,float b,float c,float d,float e,float f)
            { LeftElbow=a;RightElbow=b;LeftKnee=c;RightKnee=d;LeftTwist=e;RightTwist=f; }
        }
        private static float Unit(float n)=>Math.Max(0f,Math.Min(1f,n));
        public static Pose Sample(int role,float gait,float attack,float hit,float defeat)
        {
            if(role<0||role>5)throw new ArgumentOutOfRangeException(nameof(role));
            gait=Math.Max(-1f,Math.Min(1f,gait));
            attack=Unit(attack);hit=Unit(hit);defeat=Unit(defeat);
            float left=Math.Max(0f,gait),right=Math.Max(0f,-gait);
            float leftStrike,rightStrike,stride,twist;
            // The visual language follows the existing exterior equipment:
            // Striker commits the right contact housing; Castigator operates
            // the right cage; Wrecker braces; Seer checks both instruments;
            // Witness uses the left relay. These are not new abilities.
            switch(role)
            {
                case 1:leftStrike=12;rightStrike=34;stride=23;twist=6;break;
                case 2:leftStrike=9;rightStrike=25;stride=20;twist=11;break;
                case 3:leftStrike=20;rightStrike=17;stride=15;twist=3;break;
                case 4:leftStrike=21;rightStrike=18;stride=18;twist=8;break;
                case 5:leftStrike=31;rightStrike=7;stride=16;twist=10;break;
                default:leftStrike=13;rightStrike=27;stride=22;twist=5;break;
            }
            // Collapse bends accompany the *authoritative* defeat event.
            // Never move the actual token anchor or simulation position.
            return new Pose(left*10f+attack*leftStrike+hit*12f+defeat*28f,
                right*10f+attack*rightStrike+hit*9f+defeat*34f,
                -left*stride-attack*(role==3?12f:5f)-defeat*42f,
                -right*stride-attack*(role==3?8f:4f)-defeat*49f,
                -attack*twist+hit*3f,attack*twist-hit*3f);
        }
    }
}
