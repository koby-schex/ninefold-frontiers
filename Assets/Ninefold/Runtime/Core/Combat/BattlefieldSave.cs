
using System;
using System.IO;
using System.Linq;
using Ninefold.Core.Persistence;

namespace Ninefold.Core.Combat
{
    public sealed partial class BattlefieldController
    {
        internal void WriteSave(BinaryWriter w)
        {
            SaveIO.Box(w,Map.Bounds); w.Write(Map.Obstacles.Count);
            foreach (var o in Map.Obstacles) { SaveIO.Box(w,o.Bounds); w.Write(o.BlocksMovement); w.Write(o.BlocksShots); w.Write(o.CoverReduction); }
            w.Write(Map.Ground.Count); foreach (var g in Map.Ground) { SaveIO.Box(w,g.Bounds); w.Write(g.CostPerMeter); }
            w.Write(units.Count);
            foreach (var e in units.OrderBy(x=>x.Key,StringComparer.Ordinal))
            {
                w.Write(e.Key); var u=e.Value; SaveIO.Point(w,u.Position);
                w.Write(u.Body.HalfWidth); w.Write(u.Body.HalfDepth); w.Write(u.Body.Height); SaveIO.Point(w,u.Body.AttackOffset); SaveIO.Point(w,u.Body.TargetOffset);
                w.Write(u.Abilities.Count);
                foreach (var a in u.Abilities.Values.OrderBy(x=>(int)x.Slot))
                { w.Write((int)a.Slot); w.Write((int)a.Kind); w.Write(a.Amount); w.Write(a.Range); w.Write(a.UsesCover); }
                SaveIO.Strings(w,u.HealingTargets.OrderBy(x=>x,StringComparer.Ordinal));
            }
            w.Write(enemies.Count);
            foreach (var e in enemies.OrderBy(x=>x.Key,StringComparer.Ordinal))
            { w.Write(e.Key); w.Write((int)e.Value.Style); w.Write(e.Value.PreferredDistance); SaveIO.Box(w,e.Value.GuardArea); w.Write(e.Value.PathNodeLimit); }
        }
        internal static BattlefieldController ReadSave(BattleTurnController turns, BinaryReader r)
        {
            var bounds=SaveIO.Box(r); var obstacles=new FieldObstacle[SaveIO.Count(r)];
            for(int i=0;i<obstacles.Length;i++) obstacles[i]=new FieldObstacle(SaveIO.Box(r),r.ReadBoolean(),r.ReadBoolean(),r.ReadDecimal());
            var ground=new DifficultGround[SaveIO.Count(r)];
            for(int i=0;i<ground.Length;i++) ground[i]=new DifficultGround(SaveIO.Box(r),r.ReadDecimal());
            var field=new BattlefieldController(turns,new BattlefieldMap(bounds,obstacles,ground));
            int n=SaveIO.Count(r);
            for(int i=0;i<n;i++)
            {
                string id=SaveIO.Text(r); turns.IsUnitEligible(id); var position=SaveIO.Point(r);
                var body=new FieldBody(r.ReadDecimal(),r.ReadDecimal(),r.ReadDecimal(),SaveIO.Point(r),SaveIO.Point(r));
                var profiles=new FieldAbility[SaveIO.Count(r)];
                for(int j=0;j<profiles.Length;j++) profiles[j]=new FieldAbility(SaveIO.EnumValue<AbilitySlot>(r),SaveIO.EnumValue<HealthEffectKind>(r),r.ReadInt32(),r.ReadDecimal(),r.ReadBoolean());
                field.units.Add(id,new Placement(position,body,profiles,SaveIO.Strings(r)));
            }
            foreach(var e in field.units)
            {
                SaveIO.Require(field.Map.Bounds.Contains(e.Value.Body.At(e.Value.Position)),"Body outside map.");
                if(turns.IsUnitEligible(e.Key)) SaveIO.Require(field.LegalPosition(e.Key,e.Value,e.Value.Position),"Illegal living placement.");
            }
            n=SaveIO.Count(r);
            for(int i=0;i<n;i++)
            {
                string id=SaveIO.Text(r);
                var behavior=new EnemyBehavior(SaveIO.EnumValue<EnemyStyle>(r),r.ReadDecimal(),SaveIO.Box(r),r.ReadInt32());
                SaveIO.Require(field.units.ContainsKey(id),"Enemy placement missing.");
                turns.Health.GetState(id); turns.Abilities.GetState(id);
                field.enemies.Add(id,behavior);
            }
            return field;
        }
    }
}
