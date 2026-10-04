using System;
using System.Linq;

namespace Ninefold.Core.Combat
{
    public sealed partial class BattleStatusController
    {
        /// <summary>Provisional local heuristic, not a lookahead simulation. Positive means useful to the caster's team.</summary>
        internal decimal ApplicationUtility(string owner, StatusApplicationPreview preview, bool allied, int armor, int power, bool endsNow)
        {
            if (preview == null || !preview.Applies) return 0;
            // Self effects with duration one disappear as the AI ends this activation.
            int afterDuration = preview.RemainingOwnerActivations - (endsNow ? 1 : 0);
            if (afterDuration <= 0) return 0;
            active.TryGetValue(owner, out var list);
            State old = null; if (list != null) list.TryGetValue(preview.StatusId, out old);
            int oldStacks = old?.Stacks ?? 0, oldDuration = Math.Max(0, (old?.Remaining ?? 0) - (endsNow ? 1 : 0));
            var definition = definitions[preview.StatusId];
            decimal value = Stat(true, armor) + Stat(false, power);
            return allied ? value : -value;

            decimal Stat(bool isArmor, int baseline)
            {
                if (baseline == 0) return 0; // Zero armor and status-only kits gain no value from scaling that stat.
                long without = list == null ? 0 : list.Where(e => e.Key != preview.StatusId)
                    .Sum(e => (long)(isArmor ? definitions[e.Key].ArmorBasisPoints : definitions[e.Key].PowerBasisPoints) * e.Value.Stacks);
                int perStack = isArmor ? definition.ArmorBasisPoints : definition.PowerBasisPoints;
                int Value(int stacks) => Scale(baseline, (int)Math.Max(-5000L, Math.Min(5000L, without + (long)perStack * stacks)), !isArmor);
                int oldValue = Value(oldDuration > 0 ? oldStacks : 0), newValue = Value(preview.StacksAfter);
                decimal gain = (newValue - (decimal)oldValue) * 100m / baseline;
                // A refresh of useful existing strength has discounted value, but a full-duration refresh has none.
                if (oldDuration > 0 && afterDuration > oldDuration)
                    gain += (Value(Math.Min(oldStacks, preview.StacksAfter)) - (decimal)Value(0)) * 100m / baseline
                        * (afterDuration - oldDuration) / afterDuration / 4m;
                return gain;
            }
        }
    }
}
