using System;
using System.Collections.Generic;

namespace Ninefold.Core.Content
{
    /// <summary>
    /// Read-only explanation of the already approved six ordinary nonplayable
    /// Ashbound NPC templates. This is tactical UI copy, not a new lore system,
    /// behavior implementation, global character class, or unit unlock roster.
    /// Only exact technical UnitIds resolve: never infer identity from a
    /// spawned actor's name, faction, artwork, or a substring match.
    /// </summary>
    public static class WorldFiveAshboundThreatReadout
    {
        public sealed class Threat
        {
            public string TemplateId { get; }
            public string Name { get; }
            public string Role { get; }
            public string Readout { get; }

            internal Threat(string id, string name, string role, string readout)
            {
                TemplateId=id;Name=name;Role=role;Readout=readout;
            }
        }

        private static readonly IReadOnlyDictionary<string,Threat> Known =
            new Dictionary<string,Threat>(StringComparer.Ordinal)
            {
                { WorldFiveActOnePrototype.ScoutId,
                    new Threat(WorldFiveActOnePrototype.ScoutId,
                        "Rift Forager", "FAST PURSUER",
                        "Mobile, close-range pressure. Watch its approach and protect exposed allies.") },
                { WorldFiveActOnePrototype.StrikerId,
                    new Threat(WorldFiveActOnePrototype.StrikerId,
                        "Parted Striker", "CLOSE PRESSURE",
                        "Stronger close-range attacks. Inspect its current range before moving in.") },
                { WorldFiveActOnePrototype.CastigatorId,
                    new Threat(WorldFiveActOnePrototype.CastigatorId,
                        "Seam Castigator", "RANGED DISRUPTION",
                        "Longer-range threat. Inspect its active attack and any listed status effect.") },
                { WorldFiveActOnePrototype.WreckerId,
                    new Threat(WorldFiveActOnePrototype.WreckerId,
                        "Anchor Wrecker", "ARMORED DEFENDER",
                        "Slower, more protected opponent. Plan your movement around its position.") },
                { WorldFiveActOnePrototype.SeerId,
                    new Threat(WorldFiveActOnePrototype.SeerId,
                        "Afterecho Seer", "LONG-RANGE PRESSURE",
                        "Reach-oriented threat. Inspect its actual attack range and status effects.") },
                { WorldFiveActOnePrototype.WitnessId,
                    new Threat(WorldFiveActOnePrototype.WitnessId,
                        "Camp Witness", "CAMP SUPPORT",
                        "A defensive support-oriented presence. Watch its position and legal attacks.") }
            };

        public static bool TryGet(string templateId, out Threat threat)
        {
            if(templateId == null)
            {
                threat=null;
                return false;
            }
            return Known.TryGetValue(templateId,out threat);
        }

        public static IReadOnlyList<Threat> All
        {
            get
            {
                var output=new List<Threat>();
                foreach(string key in WorldFiveActOnePrototype.EnemyTemplateIds)
                    output.Add(Known[key]);
                return output.AsReadOnly();
            }
        }
    }
}
