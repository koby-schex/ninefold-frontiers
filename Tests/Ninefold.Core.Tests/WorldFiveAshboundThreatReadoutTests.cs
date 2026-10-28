using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Content;

internal static partial class Program
{
    private static IEnumerable<(string Name, Action Run)> AshboundThreatReadoutTests()
    {
        yield return ("Ashbound threat readout recognizes exactly the six ordinary NPC archetypes", ThreatReadoutIds);
        yield return ("Ashbound threat readout never treats wildlife or player units as Ashbound", ThreatReadoutNoIdentityLeak);
        yield return ("Mission 11 and 12-14 reveal actual authored Ashbound enemy compositions", ThreatReadoutMissions);
        yield return ("Threat descriptions do not promise unavailable skills or new world lore", ThreatReadoutAccuracy);
    }
    private static void ThreatReadoutIds()
    {
        var expected=WorldFiveActOnePrototype.EnemyTemplateIds;
        Equal(6,expected.Length);
        Equal(6,WorldFiveAshboundThreatReadout.All.Count);
        foreach(string id in expected)
        {
            if(!WorldFiveAshboundThreatReadout.TryGet(id,out var entry))
                throw new Exception("Missing ordinary threat: "+id);
            if(entry.TemplateId!=id || string.IsNullOrWhiteSpace(entry.Name) ||
                string.IsNullOrWhiteSpace(entry.Role) ||
                string.IsNullOrWhiteSpace(entry.Readout))
                throw new Exception("Unreadable threat descriptor: "+id);
            if(entry.Name.Contains("ashbound-",StringComparison.Ordinal) ||
                entry.Readout.Contains("guaranteed",StringComparison.OrdinalIgnoreCase))
                throw new Exception("Unfriendly UI or false combat promise.");
        }
        Equal(expected.Length,WorldFiveAshboundThreatReadout.All
            .Select(t=>t.Name).Distinct(StringComparer.Ordinal).Count());
    }
    private static void ThreatReadoutNoIdentityLeak()
    {
        foreach(string other in new[]{
            null,"","ASHBOUND-RIFT-FORAGER","ashbound-encounter-forager-1",
            "ashbound-rift-forager-copy",WorldFiveTutorialWildlifeV1.SillrunnerId,
            WorldFiveTutorialWildlifeV1.SeamstalkerId,
            "fixture-a-1","fixture-npc","w5-story-011","ashbound-leader"
        })
            if(WorldFiveAshboundThreatReadout.TryGet(other,out var threat))
                throw new Exception("Invented Ashbound identity: "+other);
    }
    private static void ThreatReadoutMissions()
    {
        var m=WorldFiveEncounterVarietyV2.Create();
        var ids=new[]{WorldFiveStorySequence.FirstCampaignMissionId}
            .Concat(WorldFiveEncounterVarietyV2.NewMissionIds).ToArray();
        var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(string id in ids)
        {
            var mission=m.Missions[id];
            var threats=mission.Actors.Select(a=>a.UnitId)
                .Where(unit=>WorldFiveAshboundThreatReadout.TryGet(unit,out _))
                .ToArray();
            if(threats.Length!=mission.Actors.Count ||
                threats.Length<2)
                throw new Exception("Enemy roster unreadable in actual mission: "+id);
            foreach(var threat in threats)seen.Add(threat);
        }
        Equal(6,seen.Count);
        var tutorial=WorldFiveOpeningTenPrototype.Create();
        foreach(var mission in tutorial.Missions.Values)
        foreach(var actor in mission.Actors)
            if(WorldFiveAshboundThreatReadout.TryGet(actor.UnitId,out _))
                throw new Exception("Ashbound were introduced inside the only tutorial.");
    }
    private static void ThreatReadoutAccuracy()
    {
        var entries=WorldFiveAshboundThreatReadout.All;
        foreach(var e in entries)
        {
            if(e.Readout.Contains("summon",StringComparison.OrdinalIgnoreCase) ||
               e.Readout.Contains("soul",StringComparison.OrdinalIgnoreCase) ||
               e.Readout.Contains("bonded",StringComparison.OrdinalIgnoreCase) ||
               e.Readout.Contains("apex",StringComparison.OrdinalIgnoreCase) ||
               e.Readout.Contains("miss chance",StringComparison.OrdinalIgnoreCase))
                throw new Exception("Unapproved mechanic or lore claim: "+e.Name);
        }
        Equal("Rift Forager",entries[0].Name);
        Equal("Parted Striker",entries[1].Name);
        Equal("Seam Castigator",entries[2].Name);
        Equal("Anchor Wrecker",entries[3].Name);
        Equal("Afterecho Seer",entries[4].Name);
        Equal("Camp Witness",entries[5].Name);
    }
}
