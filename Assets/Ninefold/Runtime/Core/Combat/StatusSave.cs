using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ninefold.Core.Persistence;

namespace Ninefold.Core.Combat
{
    public sealed partial class BattleStatusController
    {
        internal void WriteSave(BinaryWriter w)
        {
            w.Write(definitions.Count);
            foreach (var d in definitions.Values.OrderBy(d => d.Id, System.StringComparer.Ordinal))
            { w.Write(d.Id); w.Write(d.ArmorBasisPoints); w.Write(d.PowerBasisPoints); w.Write(d.Duration); w.Write(d.MaximumStacks); w.Write((int)d.Stacking); }
            w.Write(passives.Count);
            foreach (var p in passives.OrderBy(p => p.Key, System.StringComparer.Ordinal))
            { w.Write(p.Key); w.Write((int)p.Value.Trigger); w.Write(p.Value.StatusId); }
            w.Write(active.Count);
            foreach (var owner in active.OrderBy(p => p.Key, System.StringComparer.Ordinal))
            {
                w.Write(owner.Key); w.Write(owner.Value.Count);
                foreach (var effect in owner.Value.OrderBy(p => p.Key, System.StringComparer.Ordinal))
                { w.Write(effect.Key); w.Write(effect.Value.Stacks); w.Write(effect.Value.Remaining); }
            }
        }
        internal void ReadSave(BinaryReader r)
        {
            int count = SaveIO.Count(r);
            for (int i = 0; i < count; i++)
            {
                var d = new StatusDefinition(SaveIO.Text(r), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), SaveIO.EnumValue<StatusStacking>(r));
                definitions.Add(d.Id, d);
            }
            count = SaveIO.Count(r);
            for (int i = 0; i < count; i++)
            {
                string owner = SaveIO.Text(r); turns.IsUnitEligible(owner);
                var p = new PassiveDefinition(SaveIO.EnumValue<PassiveTrigger>(r), SaveIO.Text(r));
                SaveIO.Require(definitions.ContainsKey(p.StatusId) && turns.Health.TryGetState(owner, out _), "Invalid passive binding.");
                passives.Add(owner, p);
            }
            count = SaveIO.Count(r);
            for (int i = 0; i < count; i++)
            {
                string owner = SaveIO.Text(r);
                SaveIO.Require(turns.IsUnitEligible(owner) && turns.Health.TryGetState(owner, out var hp) && !hp.IsDefeated, "Inactive status owner.");
                int n = SaveIO.Count(r); SaveIO.Require(n > 0, "Empty status owner.");
                var list = new Dictionary<string, State>(System.StringComparer.Ordinal);
                for (int j = 0; j < n; j++)
                {
                    string id = SaveIO.Text(r); int stacks = r.ReadInt32(), remaining = r.ReadInt32();
                    SaveIO.Require(definitions.TryGetValue(id, out var d) && stacks >= 1 && stacks <= d.MaximumStacks && remaining >= 1 && remaining <= d.Duration, "Invalid status state.");
                    list.Add(id, new State { Stacks = stacks, Remaining = remaining });
                }
                active.Add(owner, list);
            }
        }
    }
}
