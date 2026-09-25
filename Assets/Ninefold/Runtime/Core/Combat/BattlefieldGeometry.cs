using System;
using System.Collections.Generic;
using System.Linq;

namespace Ninefold.Core.Combat
{
    /// <summary>World meters, bounded to keep decimal geometry arithmetic safe.</summary>
    public readonly struct FieldPoint : IEquatable<FieldPoint>
    {
        public decimal X { get; }
        public decimal Y { get; }
        public decimal Z { get; }
        public FieldPoint(decimal x, decimal y, decimal z)
        {
            if (x < -10000m || x > 10000m || y < -10000m || y > 10000m || z < -10000m || z > 10000m
                || decimal.Round(x,6) != x || decimal.Round(y,6) != y || decimal.Round(z,6) != z)
                throw new ArgumentOutOfRangeException(nameof(x));
            X = x; Y = y; Z = z;
        }
        public bool Equals(FieldPoint other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is FieldPoint point && Equals(point);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        internal static FieldPoint Add(FieldPoint a, FieldPoint b) => new FieldPoint(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        internal static decimal Distance(FieldPoint a, FieldPoint b)
        {
            decimal x = a.X-b.X, y = a.Y-b.Y, z = a.Z-b.Z;
            decimal n = x*x+y*y+z*z;
            if (n == 0m) return 0m;
            decimal root = n >= 1m ? n : 1m;
            decimal previous = 0m;
            for (int i = 0; i < 100; i++)
            {
                decimal next = (root + n/root)/2m;
                if (next == root || next == previous) return Math.Min(next,root);
                previous = root; root = next;
            }
            return root;
        }
    }

    /// <summary>Closed axis-aligned volume; touching an obstacle counts as blocked.</summary>
    public sealed class FieldBox
    {
        public FieldPoint Min { get; }
        public FieldPoint Max { get; }
        public FieldBox(FieldPoint min, FieldPoint max)
        {
            if (min.X >= max.X || min.Y >= max.Y || min.Z >= max.Z)
                throw new ArgumentException("Box must have positive extent on every axis.");
            Min = min; Max = max;
        }
        internal bool Contains(FieldPoint p) => p.X >= Min.X && p.X <= Max.X
            && p.Y >= Min.Y && p.Y <= Max.Y && p.Z >= Min.Z && p.Z <= Max.Z;
        internal bool Contains(FieldBox b) => Contains(b.Min) && Contains(b.Max);
        internal bool Overlaps(FieldBox b) => Min.X <= b.Max.X && Max.X >= b.Min.X
            && Min.Y <= b.Max.Y && Max.Y >= b.Min.Y && Min.Z <= b.Max.Z && Max.Z >= b.Min.Z;
        internal bool Intersects(FieldPoint from, FieldPoint to, out decimal enter, out decimal exit,
            decimal halfX = 0m, decimal height = 0m, decimal halfZ = 0m)
        {
            enter = 0m; exit = 1m;
            return Clip(from.X, to.X-from.X, Min.X-halfX, Max.X+halfX, ref enter, ref exit)
                && Clip(from.Y, to.Y-from.Y, Min.Y-height, Max.Y, ref enter, ref exit)
                && Clip(from.Z, to.Z-from.Z, Min.Z-halfZ, Max.Z+halfZ, ref enter, ref exit);
        }
        private static bool Clip(decimal origin, decimal delta, decimal min, decimal max, ref decimal lo, ref decimal hi)
        {
            if (delta == 0m) return origin >= min && origin <= max;
            decimal a = (min-origin)/delta, b = (max-origin)/delta;
            lo = Math.Max(lo, Math.Min(a,b)); hi = Math.Min(hi, Math.Max(a,b));
            return lo <= hi;
        }
    }

    public sealed class FieldObstacle
    {
        public FieldBox Bounds { get; }
        public bool BlocksMovement { get; }
        public bool BlocksShots { get; }
        public decimal CoverReduction { get; }
        public FieldObstacle(FieldBox bounds, bool blocksMovement, bool blocksShots, decimal coverReduction = 0m)
        {
            Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
            if (coverReduction < 0m || coverReduction > 1m) throw new ArgumentOutOfRangeException(nameof(coverReduction));
            BlocksMovement = blocksMovement; BlocksShots = blocksShots; CoverReduction = coverReduction;
        }
    }

    public sealed class DifficultGround
    {
        public FieldBox Bounds { get; }
        public decimal CostPerMeter { get; }
        public DifficultGround(FieldBox bounds, decimal costPerMeter)
        {
            Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
            if (costPerMeter < 1m || costPerMeter > 100m) throw new ArgumentOutOfRangeException(nameof(costPerMeter));
            CostPerMeter = costPerMeter;
        }
    }

    /// <summary>Immutable authored map. No terrain mutation, navigation generation or slopes yet.</summary>
    public sealed class BattlefieldMap
    {
        public FieldBox Bounds { get; }
        public IReadOnlyList<FieldObstacle> Obstacles { get; }
        public IReadOnlyList<DifficultGround> Ground { get; }
        public BattlefieldMap(FieldBox bounds, IEnumerable<FieldObstacle> obstacles, IEnumerable<DifficultGround> ground = null)
        {
            Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
            var walls = (obstacles ?? throw new ArgumentNullException(nameof(obstacles))).ToArray();
            var terrain = (ground ?? Array.Empty<DifficultGround>()).ToArray();
            if (walls.Any(w => w == null) || terrain.Any(g => g == null)) throw new ArgumentException("Null map entry.");
            Obstacles = Array.AsReadOnly(walls); Ground = Array.AsReadOnly(terrain);
        }
    }

    /// <summary>Conservative fixed-orientation collision proxy, not a canonical body model.</summary>
    public sealed class FieldBody
    {
        public decimal HalfWidth { get; }
        public decimal HalfDepth { get; }
        public decimal Height { get; }
        public FieldPoint AttackOffset { get; }
        public FieldPoint TargetOffset { get; }
        public FieldBody(decimal halfWidth, decimal halfDepth, decimal height, FieldPoint attackOffset, FieldPoint targetOffset)
        {
            if (halfWidth <= 0m || halfWidth > 100m || halfDepth <= 0m || halfDepth > 100m || height <= 0m || height > 100m
                || decimal.Round(halfWidth,6) != halfWidth || decimal.Round(halfDepth,6) != halfDepth || decimal.Round(height,6) != height)
                throw new ArgumentOutOfRangeException(nameof(halfWidth));
            foreach (var offset in new[] { attackOffset, targetOffset })
                if (Math.Abs(offset.X) > halfWidth || Math.Abs(offset.Z) > halfDepth || offset.Y < 0m || offset.Y > height)
                    throw new ArgumentException("Reference points must lie within the body proxy.");
            HalfWidth = halfWidth; HalfDepth = halfDepth; Height = height;
            AttackOffset = attackOffset; TargetOffset = targetOffset;
        }
        internal FieldBox At(FieldPoint p) => new FieldBox(new FieldPoint(p.X-HalfWidth,p.Y,p.Z-HalfDepth),
            new FieldPoint(p.X+HalfWidth,p.Y+Height,p.Z+HalfDepth));
    }
}
