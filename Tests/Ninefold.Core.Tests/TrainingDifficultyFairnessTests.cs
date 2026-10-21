using System;
using System.Linq;
using Ninefold.Core.Content;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;

// CI authoring guardrails, not simulated victory rates or a player skill model.
internal static partial class Program
{
    private static (string Name,Action Run)[] TrainingDifficultyFairnessTests() =>
        new (string,Action)[] {
            ("Opening three lessons have no hostile damage pressure", FairnessSafeIntroduction),
            ("Training encounters ramp enemy counts gradually", FairnessEncounterRamp),
            ("All ten missions retain a bounded three-unit squad", FairnessSquadBudget),
            ("Objective-specific AI hint stays in late rescue lesson only", FairnessHintRamp),
            ("Training pressure rules are observational and save-safe", FairnessNoSaveChanges)
        };

    private static void FairnessSafeIntroduction()
    {
        var c=WorldFiveOpeningTenPrototype.Create();
        var ids=WorldFiveOpeningTenPrototype.MissionIds;
        for (int i=0;i<3;i++)
        {
            var m=c.Missions[ids[i]];
            Equal(0,m.Actors.Count(a=>a.IsHostile));
            Equal(true,m.Objectives[0].Kind==ObjectiveKind.Stabilize ||
                m.Objectives[0].Kind==ObjectiveKind.RescueAndExtract);
            Equal(true,m.Actors.All(a=>!a.IsHostile || a.Behavior==null));
        }
    }
    private static void FairnessEncounterRamp()
    {
        var c=WorldFiveOpeningTenPrototype.Create();
        var counts=WorldFiveOpeningTenPrototype.MissionIds
            .Select(id=>c.Missions[id].Actors.Count(a=>a.IsHostile)).ToArray();
        Equal(true,counts.Skip(3).Take(3).All(n=>n>=1 && n<=2));
        Equal(true,counts.Skip(6).All(n=>n>=1 && n<=2));
        // Escalation is intentionally non-monotonic: recovery missions need breathing room.
        Equal(true,counts[5]>counts[4]);
        Equal(true,counts[8]<counts[6]);
        foreach(var pair in counts.Select((n,i)=>new { n,i }))
            Console.WriteLine("TRAINING_PRESSURE "+(pair.i+1).ToString("D3")+
                " hostile="+pair.n+" squad=3");
    }
    private static void FairnessSquadBudget()
    {
        var c=WorldFiveOpeningTenPrototype.Create();
        foreach(var id in WorldFiveOpeningTenPrototype.MissionIds)
        {
            var m=c.Missions[id];
            Equal(3,m.MaximumSquad);
            Equal(true,m.MinimumSquad>=1 && m.MinimumSquad<=3);
        }
    }
    private static void FairnessHintRamp()
    {
        var c=WorldFiveOpeningTenPrototype.Create();
        var ids=WorldFiveOpeningTenPrototype.MissionIds;
        for(int i=0;i<ids.Count;i++)
        {
            var hints=c.Missions[ids[i]].Actors.Where(a=>a.IsHostile && a.Behavior!=null)
                .SelectMany(a=>a.Behavior.ObjectivePriorityTargets).ToArray();
            Equal(i==8?1:0,hints.Length);
            if(i==8) Equal("$carrier",hints.Single());
        }
    }
    private static void FairnessNoSaveChanges()
    {
        var c=WorldFiveOpeningTenPrototype.Create();
        var revision=c.Manifest.SaveRevision;
        FairnessSafeIntroduction();
        FairnessEncounterRamp();
        FairnessSquadBudget();
        FairnessHintRamp();
        Equal(revision,c.Manifest.SaveRevision);
        Equal("w5-opening-ten-profile-v4",WorldFiveOpeningTenPrototype.ProfileId);
        Equal("w5-opening-ten-layout-04",WorldFiveOpeningTenPrototype.Revision);
    }
}
