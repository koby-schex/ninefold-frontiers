using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Ninefold.Core.Combat;

namespace Ninefold.Core.Content
{
    /// <summary>Shared exact terrain surface recipes; independent of Unity and game collision.
    /// All elevations are decorative outside the walkable rectangle or inside real cover.</summary>
    public static class BattlefieldArtPlan
    {
        public enum Mood { PaleSurvey, VioletShelves, MineralBasin, BrokenTerrace, DeepFracture, Neutral }
        public enum Surface { Floor, FloorLight, FloorShade, Threads, Foundation, Stratum,
            Cover, CoverBand, CoverCrown, Rim, Slow, SlowEdge, OuterStone, OuterRidge, FarShelf, FarHaze }
        public sealed class Mesh
        {
            public readonly Surface Material;
            public readonly List<Vector3> Vertices=new List<Vector3>();
            public Mesh(Surface material){Material=material;}
            public void Face(Vector3 a,Vector3 b,Vector3 c)
            {
                if(Vector3.Cross(b-a,c-a).LengthSquared()<1e-12f)return;
                Vertices.Add(a);Vertices.Add(b);Vertices.Add(c);
            }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){Face(a,b,c);Face(a,c,d);}
        }
        public sealed class Scene
        {
            public readonly Mood Mood;
            public readonly uint Seed;
            public readonly Mesh[] Meshes;
            public readonly Vector3 Sky,Fill;
            public readonly Vector3[] Palette;
            internal Scene(Mood mood,uint seed,Vector3[] palette,Vector3 sky,Vector3 fill)
            {
                Mood=mood;Seed=seed;Palette=palette;Sky=sky;Fill=fill;
                Meshes=Enum.GetValues(typeof(Surface)).Cast<Surface>().Select(s=>new Mesh(s)).ToArray();
            }
            public int Triangles=>Meshes.Sum(m=>m.Vertices.Count/3);
            internal Mesh Get(Surface s)=>Meshes[(int)s];
        }
        private static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        private static float L(float a,float b,float t)=>a+(b-a)*t;
        private static Vector3 C(uint hex)=>V(((hex>>16)&255)/255f,((hex>>8)&255)/255f,(hex&255)/255f);
        private static Vector3 Tint(Vector3 a,Vector3 b,float t)=>Vector3.Lerp(a,b,t);
        private static float U(uint seed,int i)=> (BattlefieldVariation.Mix(seed,(uint)i)&0xffffffu)/16777215f;
        public static bool IsWorldFive(string mission,string environment)=>
            environment=="study-w5-survey"||environment=="study-w5-fracture"||
            WorldFiveStorySequence.IsTutorial(mission)||
            mission!=null&&mission.StartsWith("w5-story-",StringComparison.Ordinal);
        public static Mood Choose(string mission,string environment)
        {
            if(!IsWorldFive(mission,environment))return Mood.Neutral;
            int n=0;
            if(mission!=null&&mission.Length>=3)
                int.TryParse(mission.Substring(mission.Length-3),NumberStyles.None,CultureInfo.InvariantCulture,out n);
            // Deliberate story visual progression, not arbitrary per-attempt recolors.
            if(n==1||n==2||n==3)return Mood.PaleSurvey;
            if(n==4||n==5)return Mood.VioletShelves;
            if(n==6||n==7||n==8)return Mood.MineralBasin;
            if(n==9||n==10)return Mood.BrokenTerrace;
            if(n==11||n==14)return Mood.DeepFracture;
            if(n==12)return Mood.VioletShelves;
            if(n==13)return Mood.BrokenTerrace;
            return environment=="study-w5-survey"?Mood.PaleSurvey:
                n>14?(Mood)(1+BattlefieldVariation.Seed(mission)%4):Mood.DeepFracture;
        }
        public static uint SceneSeed(string mission,string attempt,bool varying)=>
            BattlefieldVariation.Seed("terrain-art-02/"+mission+(varying?"/"+attempt:""));
        private static Scene Make(Mood mood,uint seed)
        {
            Vector3 floor,stone,crown,sky,fill,vein,slow;
            switch(mood)
            {
                case Mood.PaleSurvey: floor=C(0x766d79);stone=C(0x61576b);crown=C(0xa79aa5);sky=C(0x3b354e);fill=C(0xe2d5e7);vein=C(0xa19aad);slow=C(0x77705c);break;
                case Mood.VioletShelves: floor=C(0x615771);stone=C(0x55485e);crown=C(0x92819e);sky=C(0x30283f);fill=C(0xc9b1e6);vein=C(0x9c81af);slow=C(0x777063);break;
                case Mood.MineralBasin: floor=C(0x807681);stone=C(0x665b72);crown=C(0xa89aab);sky=C(0x51465c);fill=C(0xe7cbdc);vein=C(0xb09ca9);slow=C(0x716758);break;
                case Mood.BrokenTerrace: floor=C(0x696172);stone=C(0x51495f);crown=C(0x97859d);sky=C(0x29283e);fill=C(0xc8d1e8);vein=C(0x918ca9);slow=C(0x7e715e);break;
                case Mood.DeepFracture: floor=C(0x514a63);stone=C(0x41364f);crown=C(0x837295);sky=C(0x201b33);fill=C(0xb7a3e0);vein=C(0x8671a9);slow=C(0x78665c);break;
                default: floor=C(0x69676b);stone=C(0x504f57);crown=C(0x99969c);sky=C(0x303641);fill=C(0xd6dce3);vein=C(0x85858f);slow=C(0x756b58);break;
            }
            var dark=C(0x252433);
            var colors=new[]{floor,Tint(floor,crown,.18f),Tint(floor,dark,.14f),Tint(floor,vein,.34f),
                Tint(stone,dark,.35f),Tint(stone,crown,.18f),stone,Tint(stone,crown,.32f),crown,
                Tint(crown,vein,.25f),slow,Tint(slow,crown,.36f),stone,Tint(stone,crown,.40f),
                Tint(stone,sky,.40f),Tint(stone,sky,.72f)};
            return new Scene(mood,seed,colors,sky,fill);
        }
        private static void Rect(Mesh m,float x0,float z0,float x1,float z1,float y)
        {if(x1>x0&&z1>z0)m.Quad(V(x0,y,z0),V(x0,y,z1),V(x1,y,z1),V(x1,y,z0));}
        private static Vector3[] Ring(float x0,float z0,float x1,float z1,float y,float cut)
        {
            return new[]{V(x0+cut,y,z0),V(x0,y,z0+cut),V(x0,y,z1-cut),
                V(x0+cut,y,z1),V(x1-cut,y,z1),V(x1,y,z1-cut),
                V(x1,y,z0+cut),V(x1-cut,y,z0)};
        }
        private static void Join(Mesh m,Vector3[] lower,Vector3[] upper)
        {
            for(int i=0;i<lower.Length;i++){int j=(i+1)%lower.Length;
                m.Quad(lower[i],lower[j],upper[j],upper[i]);}
        }
        private static void Cap(Mesh m,Vector3[] ring)
        {
            var c=ring.Aggregate(Vector3.Zero,(sum,p)=>sum+p)/ring.Length;
            for(int i=0;i<ring.Length;i++)m.Face(c,ring[i],ring[(i+1)%ring.Length]);
        }
        private static void Stroke(Mesh m,Vector3 a,Vector3 b,float width)
        {
            var d=b-a;float len=MathF.Sqrt(d.X*d.X+d.Z*d.Z);
            if(len<.001f)return;
            var n=V(-d.Z/len*width,0,d.X/len*width);
            m.Quad(a-n,a+n,b+n,b-n);
        }
        private static void Floor(Scene s,float x0,float x1,float z0,float z1)
        {
            // Fully covered level floor, no raised subdivisions or fake holes.
            int nx=Math.Max(2,Math.Min(30,(int)Math.Ceiling((x1-x0)/1.15f)));
            int nz=Math.Max(2,Math.Min(30,(int)Math.Ceiling((z1-z0)/1.15f)));
            var p=new Vector3[nx+1,nz+1];
            for(int x=0;x<=nx;x++)for(int z=0;z<=nz;z++)
            {
                int i=x*(nz+1)+z;float dx=(x1-x0)/nx,dz=(z1-z0)/nz;
                p[x,z]=V(L(x0,x1,x/(float)nx)+(x>0&&x<nx?(U(s.Seed,i*3)-.5f)*dx*.46f:0),
                    -.014f,L(z0,z1,z/(float)nz)+(z>0&&z<nz?(U(s.Seed,i*3+1)-.5f)*dz*.46f:0));
            }
            for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)
            {
                int i=5000+x*nz+z;
                var a=p[x,z];var b=p[x,z+1];var c=p[x+1,z+1];var d=p[x+1,z];
                var slot=(Surface)((int)(U(s.Seed,i)*3)%3);
                if(U(s.Seed,i+1300)<.5f)s.Get(slot).Quad(a,b,c,d);
                else s.Get(slot).Quad(b,c,d,a); // stable alternate triangulation
            }
            // Restrained interrupted sediment grain, below all objective shapes.
            int count=s.Mood==Mood.DeepFracture?58:36;
            for(int i=0;i<count;i++)
            {
                float x=L(x0+.28f,x1-.28f,U(s.Seed,8000+i*7));
                float z=L(z0+.28f,z1-.28f,U(s.Seed,8001+i*7));
                float angle=U(s.Seed,8002+i*7)*MathF.PI*2;
                float length=.16f+U(s.Seed,8003+i*7)*.66f;
                var a=V(x,-.009f,z);
                var b=V(Math.Max(x0+.27f,Math.Min(x1-.27f,x+MathF.Cos(angle)*length)),
                    -.009f,Math.Max(z0+.27f,Math.Min(z1-.27f,z+MathF.Sin(angle)*length)));
                Stroke(s.Get(Surface.Threads),a,b,.009f+U(s.Seed,8004+i*7)*.012f);
            }
            // Thin inset markings follow exact legal bounds, not a physical wall.
            float w=Math.Min(.075f,Math.Min(x1-x0,z1-z0)*.05f);
            Rect(s.Get(Surface.Rim),x0,z0,x1,z0+w,.003f);
            Rect(s.Get(Surface.Rim),x0,z1-w,x1,z1,.003f);
            Rect(s.Get(Surface.Rim),x0,z0+w,x0+w,z1-w,.003f);
            Rect(s.Get(Surface.Rim),x1-w,z0+w,x1,z1-w,.003f);
        }
        private static void Foundation(Scene s,float x0,float x1,float z0,float z1)
        {
            // Stepped irregular substrate, not the former rectangular game-table block.
            // Upper rectangular edge aligns exactly with the visible playable floor.
            var top=Ring(x0,z0,x1,z1,-.014f,0);
            var shoulder=Ring(x0-.38f,z0-.38f,x1+.38f,z1+.38f,-.36f,.32f);
            var split=Ring(x0-.54f,z0-.54f,x1+.54f,z1+.54f,-.66f,.42f);
            var low=Ring(x0-1.15f,z0-.76f,x1+1.02f,z1+.88f,-1.22f,.67f);
            Join(s.Get(Surface.Foundation),shoulder,top);
            Join(s.Get(Surface.Stratum),split,shoulder);
            Join(s.Get(Surface.Foundation),low,split);
        }
        private static void Cover(Scene s,FieldObstacle o)
        {
            var a=o.Bounds.Min;var b=o.Bounds.Max;
            float x0=(float)a.X,z0=(float)a.Z,x1=(float)b.X,z1=(float)b.Z,y0=(float)a.Y,y1=(float)b.Y;
            float h=y1-y0,cut=Math.Min(.13f,Math.Min(x1-x0,z1-z0)*.09f);
            var baseRing=Ring(x0,z0,x1,z1,y0,cut*.45f);
            var bandA=Ring(x0,z0,x1,z1,y0+h*.38f,cut);
            float bandInset=Math.Min(.012f,Math.Min(x1-x0,z1-z0)*.025f);
            var bandB=Ring(x0+bandInset,z0+bandInset,x1-bandInset,z1-bandInset,y0+h*.48f,cut);
            var shoulder=Ring(x0,z0,x1,z1,y1-h*.12f,cut);
            float inset=Math.Min(.06f,cut*.4f);
            var crown=Ring(x0+inset,z0+inset,x1-inset,z1-inset,y1,cut*.80f);
            Join(s.Get(Surface.Cover),baseRing,bandA);
            Join(s.Get(Surface.CoverBand),bandA,bandB);
            Join(s.Get(Surface.Cover),bandB,shoulder);
            Join(s.Get(Surface.CoverCrown),shoulder,crown);
            Cap(s.Get(Surface.CoverCrown),crown);
        }
        private static void SlowGround(Scene s,DifficultGround ground)
        {
            float x0=(float)ground.Bounds.Min.X,z0=(float)ground.Bounds.Min.Z,
                x1=(float)ground.Bounds.Max.X,z1=(float)ground.Bounds.Max.Z;
            Rect(s.Get(Surface.Slow),x0,z0,x1,z1,.010f);
            // Actual slow surface only: muted grit with short parallel chevrons
            // rather than full gold objective stripes.
            int n=Math.Min(36,Math.Max(1,(int)((z1-z0)/.42f)));
            float w=Math.Min(.06f,(x1-x0)*.13f);
            for(int i=0;i<n;i++)
            {
                float z=L(z0,z1,(i+.5f)/n);
                float tick=Math.Min(.023f,(z1-z0)/(n*3f));
                Rect(s.Get(Surface.SlowEdge),x0,z-tick,x0+w,z+tick,.014f);
                Rect(s.Get(Surface.SlowEdge),x1-w,z-tick,x1,z+tick,.014f);
                if(x1-x0>.55f&&z>z0+.09f&&z<z1-.09f)Stroke(s.Get(Surface.SlowEdge),
                    V((x0+x1)*.5f-.12f,.014f,z-.06f),
                    V((x0+x1)*.5f+.12f,.014f,z+.06f),.024f);
            }
        }
        private static void Rock(Scene s,float x,float y,float z,float w,float h,float d,int id,Surface slot,bool tall)
        {
            const int n=7;
            var low=new Vector3[n];var middle=new Vector3[n];var upper=new Vector3[n];
            for(int i=0;i<n;i++)
            {
                // Descending azimuth gives outward side faces, upward caps.
                float angle=-i*2*MathF.PI/n+.37f*U(s.Seed,id);
                float radius=.75f+.25f*U(s.Seed,id+i+1);
                float vx=MathF.Cos(angle)*w*.5f*radius,vz=MathF.Sin(angle)*d*.5f*radius;
                low[i]=V(x+vx,y,z+vz);
                middle[i]=V(x+vx*.91f,y+h*(.33f+.13f*U(s.Seed,id+21+i)),z+vz*.93f);
                upper[i]=V(x+vx*(tall?.46f:.75f)+w*.12f,y+h*(.75f+.25f*U(s.Seed,id+41+i)),z+vz*.67f);
            }
            Join(s.Get(slot),low,middle);
            Join(s.Get(slot==Surface.OuterStone?Surface.OuterRidge:slot),middle,upper);
            Cap(s.Get(slot),upper);
        }
        private static void Surround(Scene s,float x0,float x1,float z0,float z1)
        {
            float width=x1-x0,depth=z1-z0;
            float spacing=1.10f,ridgeHeight=1.45f,ridgeVariance=.85f,ridgeWidth=1.4f,ridgeDepth=1.30f;
            float shelfDepth=8.5f;
            int bands=2;
            bool fins=false;
            // Five different silhouettes, rather than one layout with a tint.
            // All geology is interpretive unnamed scenery, not new canon.
            switch(s.Mood)
            {
                case Mood.PaleSurvey:spacing=1.8f;ridgeHeight=.94f;ridgeVariance=.55f;
                    ridgeWidth=2.05f;ridgeDepth=1.70f;shelfDepth=9.4f;break;
                case Mood.VioletShelves:spacing=1.3f;ridgeHeight=1.65f;ridgeVariance=.55f;
                    ridgeWidth=1.65f;ridgeDepth=2.0f;bands=3;break;
                case Mood.MineralBasin:spacing=2.1f;ridgeHeight=.66f;ridgeVariance=.46f;
                    ridgeWidth=2.7f;ridgeDepth=2.25f;shelfDepth=11.2f;break;
                case Mood.BrokenTerrace:spacing=1.45f;ridgeHeight=1.9f;ridgeVariance=1.1f;
                    ridgeWidth=1.3f;ridgeDepth=1.8f;bands=3;break;
                case Mood.DeepFracture:spacing=.98f;ridgeHeight=2.7f;ridgeVariance=1.5f;
                    ridgeWidth=.60f;ridgeDepth=1.3f;fins=true;break;
            }
            int n=Math.Min(22,Math.Max(6,(int)(width/spacing)));
            // Sculpted layers and staggered erosion outside the legal map only.
            // Foreground and side relief stays below walking height; distant
            // mountains are well beyond the far side and cannot be path targets.
            for(int i=0;i<n;i++)
            {
                float x=L(x0,x1,(i+.5f)/n),w=width/n*1.6f;
                Rock(s,x,-1.7f,z0-1.13f,w,.92f+U(s.Seed,9000+i)*.57f,1.0f,9100+i*67,Surface.Foundation,false);
                Rock(s,x,-1.35f,z1+1.14f,w,1.05f+U(s.Seed,9200+i)*.64f,1.15f,9300+i*67,Surface.OuterStone,false);
                float dist=3.3f+U(s.Seed,9600+i)*1.4f;
                float height=ridgeHeight+U(s.Seed,9700+i)*ridgeVariance;
                Rock(s,x,-.92f,z1+dist,w*ridgeWidth,height,ridgeDepth,9800+i*61,Surface.OuterStone,fins);
                if(bands==3 && i%2==0)
                    Rock(s,x+.18f*w,-1.14f,z1+dist+2.05f,w*2.2f,
                        .9f+height*.56f,1.9f,10400+i*71,Surface.OuterRidge,false);
                if(s.Mood==Mood.MineralBasin && i%2==1)
                    Rock(s,x,-1.8f,z1+2.8f,w*1.9f,.78f,2.2f,10500+i*91,Surface.Foundation,false);
                Rock(s,x,-2.2f,z1+shelfDepth,Math.Max(3.2f,w*2.8f),3.9f+U(s.Seed,11000+i)*2.9f,3.6f,
                    11100+i*67,Surface.FarShelf,false);
                if(i%2==0)Rock(s,x,-2.6f,z1+13f,Math.Max(6f,w*4),5.3f+U(s.Seed,12200+i)*3.3f,5f,
                    12300+i*67,Surface.FarHaze,false);
            }
            int sideCount=Math.Min(18,Math.Max(5,(int)(depth/1.3f)));
            for(int i=0;i<sideCount;i++)
            {
                float z=L(z0,z1,(i+.5f)/sideCount),d=depth/sideCount*1.35f;
                Rock(s,x0-1.15f,-1.64f,z,1.14f,1.1f+U(s.Seed,13310+i)*.43f,d,13400+i*61,Surface.OuterStone,false);
                Rock(s,x1+1.15f,-1.67f,z,1.14f,1.1f+U(s.Seed,13510+i)*.43f,d,13600+i*61,Surface.OuterStone,false);
            }
        }
        public static Scene Build(BattlefieldMap map,string mission,string environment,string attempt,bool varying)
        {
            if(map==null)throw new ArgumentNullException(nameof(map));
            var scene=Make(Choose(mission,environment),SceneSeed(mission,attempt,varying));
            float x0=(float)map.Bounds.Min.X,x1=(float)map.Bounds.Max.X,z0=(float)map.Bounds.Min.Z,z1=(float)map.Bounds.Max.Z;
            Floor(scene,x0,x1,z0,z1);Foundation(scene,x0,x1,z0,z1);
            foreach(var o in map.Obstacles)Cover(scene,o);
            foreach(var g in map.Ground)SlowGround(scene,g);
            Surround(scene,x0,x1,z0,z1);
            return scene;
        }
    }
}
