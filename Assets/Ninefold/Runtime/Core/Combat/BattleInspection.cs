using System;
using System.Collections.Generic;
using System.Linq;

namespace Ninefold.Core.Combat
{
    public sealed partial class BattleTurnController
    {
        internal string[] InspectUnitIds() => units.Keys.OrderBy(id => id,StringComparer.Ordinal).ToArray();
        internal string[] InspectUpcoming() => IsBattleEnded ? Array.Empty<string>() :
            (active == null ? Array.Empty<string>() : new[] { active.UnitId }).Concat(roundOrder.Skip(nextIndex).Where(id => units[id].Eligible)).ToArray();
    }
    public sealed partial class BattlefieldController
    {
        internal bool InspectPlacement(string id, out FieldPoint position, out FieldBody body, out IReadOnlyList<FieldAbility> profiles)
        {
            position = default; body = null; profiles = Array.Empty<FieldAbility>();
            if (!units.TryGetValue(id,out var p)) return false;
            position = p.Position; body = p.Body; profiles = Array.AsReadOnly(p.Abilities.Values.OrderBy(a => a.Slot).ToArray()); return true;
        }
    }
    public sealed partial class BattleAbilityController
    {
        internal AbilityStateView InspectKit(string id) => kits.ContainsKey(id) ? GetState(id) : null;
    }
}
