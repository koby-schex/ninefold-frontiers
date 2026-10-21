using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Content;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;

// Authoring/CI diagnostics only. This is not a difficulty rating or AI simulator.
internal static partial class Program
{
    private sealed class EncounterPressureSnapshot
    {
        public string MissionId;
        public int HostileCount;
        public int FriendlyNpcCount;
        public int SquadLimit;
        public int OptionalGoals;
        public ObjectiveKind PrimaryKind;
        public decimal HostilesPerSquadSlot;
    }

    private static (string Name,Action Run)[] EnemyEncounterQualityTests() =>
        new (string,Action)[] {
            ("World Five encounter pressure snapshots cover all ten authored missions", EncounterPressureCoverage),
            ("World Five late missions expose distinguishable objective pressure", EncounterPressureLateMissions),
            ("Encounter-pressure audit is deterministic and read-only", EncounterPressureReadOnly),
            ("Enemy lethal turn resolves once without a duplicate activation", EncounterPressureLethalTurn)
        };

    private static IReadOnlyList<EncounterPressureSnapshot> CaptureEncounterPressure()
    {
        var catalog=WorldFiveOpeningTenPrototype.Create();
        return Array.AsReadOnly(WorldFiveOpeningTenPrototype.MissionIds.Select(id => {
            var mission=catalog.Missions[id];
            var hostile=mission.Actors.Where(a=>a.IsHostile).ToArray();
            return new EncounterPressureSnapshot {
                MissionId=id,
                HostileCount=hostile.Length,
                FriendlyNpcCount=mission.Actors.Count(a=>!a.IsHostile),
                SquadLimit=mission.MaximumSquad,
                OptionalGoals=mission.Objectives.Count(o=>o.Id!="primary"),
                PrimaryKind=mission.Objectives.Single(o=>o.Id=="primary").Kind,
                HostilesPerSquadSlot=mission.MaximumSquad==0 ? 0m : (decimal)hostile.Length/mission.MaximumSquad
            };
        }).ToArray());
    }

    private static void EncounterPressureCoverage()
    {
        var rows=CaptureEncounterPressure();
        Equal(10,rows.Count);
        Equal(true,rows.Select(r=>r.MissionId).SequenceEqual(WorldFiveOpeningTenPrototype.MissionIds));
        Equal(true,rows.All(r=>r.SquadLimit>0 && r.HostilesPerSquadSlot>=0m));
        foreach(var row in rows)
        {
            Console.WriteLine("PRESSURE "+row.MissionId+" hostile="+row.HostileCount+
                " squad="+row.SquadLimit+" ratio="+row.HostilesPerSquadSlot+
                " friendlyNpc="+row.FriendlyNpcCount+" primary="+row.PrimaryKind+
                " optional="+row.OptionalGoals);
        }
    }

    private static void EncounterPressureLateMissions()
    {
        var rows=CaptureEncounterPressure();
        var seven=rows[6]; var eight=rows[7]; var nine=rows[8]; var ten=rows[9];
        Equal(ObjectiveKind.Defeat,seven.PrimaryKind);
        Equal(2,seven.HostileCount);
        Equal(ObjectiveKind.Secure,eight.PrimaryKind);
        Equal(1,eight.HostileCount);
        Equal(ObjectiveKind.RescueAndExtract,nine.PrimaryKind);
        Equal(1,nine.FriendlyNpcCount);
        Equal(1,nine.HostileCount);
        Equal(ObjectiveKind.Stabilize,ten.PrimaryKind);
        Equal(2,ten.OptionalGoals);
        Equal(1,ten.HostileCount);
        // These numbers document authored pressure, not whether it feels fair.
    }

    private static void EncounterPressureReadOnly()
    {
        var first=CaptureEncounterPressure();
        var second=CaptureEncounterPressure();
        Equal(string.Join("|",first.Select(r=>r.MissionId+":"+r.HostileCount+":"+
            r.FriendlyNpcCount+":"+r.OptionalGoals+":"+r.PrimaryKind)),
            string.Join("|",second.Select(r=>r.MissionId+":"+r.HostileCount+":"+
                r.FriendlyNpcCount+":"+r.OptionalGoals+":"+r.PrimaryKind)));
        Equal("w5-opening-ten-profile-v4",WorldFiveOpeningTenPrototype.ProfileId);
        Equal("w5-opening-ten-layout-04",WorldFiveOpeningTenPrototype.Revision);
    }

    private static void EncounterPressureLethalTurn()
    {
        var battle=EnemyBattle(range:10m,targetHp:20,mission:EnemyMission());
        var result=RunEnemy(battle,"b");
        Equal(true,result.BattleEnded);
        Equal(MissionOutcome.Defeat,battle.Mission.Result.Outcome);
        Equal(1L,battle.GetActivationCount("a"));
        Equal(true,battle.Health.GetState("b").IsDefeated);
        Equal(false,battle.Battlefield.TryRunEnemyTurn(1,new[] {"c"},out _,out _));
        Equal(1L,battle.GetActivationCount("a"));
    }
}
