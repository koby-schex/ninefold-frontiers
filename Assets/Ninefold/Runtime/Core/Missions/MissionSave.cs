
using System;
using System.IO;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Persistence;

namespace Ninefold.Core.Missions
{
    public sealed partial class MissionController
    {
        internal void WriteSave(BinaryWriter w)
        {
            w.Write(Definition.MissionId); w.Write(Definition.AttemptId); SaveIO.Strings(w,Definition.Squad); w.Write((int)Definition.Priority);
            w.Write(states.Length);
            foreach (var s in states)
            {
                var d=s.Definition; w.Write(d.Id); w.Write((int)d.Kind); w.Write(d.Required); SaveIO.Strings(w,d.Subjects);
                SaveIO.Strings(w,d.Contestants); SaveIO.Box(w,d.Zone); w.Write(d.Consecutive); w.Write(d.Interaction != null);
                if (d.Interaction != null) { SaveIO.Point(w,d.Interaction.Point); w.Write(d.Interaction.Reach); SaveIO.Strings(w,d.Interaction.AllowedActors); }
                w.Write((int)s.Status); w.Write(s.Progress); w.Write(s.Rescued);
            }
            w.Write(LastResolvedRound); w.Write(Result != null);
            if (Result != null) { w.Write((int)Result.Outcome); w.Write(Result.Round); w.Write(Result.Reason); }
        }
        internal static MissionController ReadSave(BattleTurnController turns, BinaryReader r)
        {
            string missionId=SaveIO.Text(r), attempt=SaveIO.Text(r); var squad=SaveIO.Strings(r); var priority=SaveIO.EnumValue<OutcomePriority>(r);
            int n=SaveIO.Count(r); SaveIO.Require(n >= 1 && n <= 3,"Invalid objective count.");
            var restored=new State[n];
            for (int i=0;i<n;i++)
            {
                string id=SaveIO.Text(r); var kind=SaveIO.EnumValue<ObjectiveKind>(r); int required=r.ReadInt32();
                var subjects=SaveIO.Strings(r); var contestants=SaveIO.Strings(r); var zone=SaveIO.Box(r); bool consecutive=r.ReadBoolean();
                InteractionDefinition interaction=null;
                if(r.ReadBoolean()) interaction=new InteractionDefinition(SaveIO.Point(r),r.ReadDecimal(),SaveIO.Strings(r));
                ObjectiveDefinition d;
                switch(kind)
                {
                    case ObjectiveKind.Defeat: d=ObjectiveDefinition.Defeat(id,subjects); break;
                    case ObjectiveKind.Protect: SaveIO.Require(subjects.Length==1,"Protect subject missing."); d=ObjectiveDefinition.Protect(id,subjects[0],required); break;
                    case ObjectiveKind.RescueAndExtract: SaveIO.Require(subjects.Length==1,"Rescue subject missing."); d=ObjectiveDefinition.Rescue(id,subjects[0],zone,interaction); break;
                    case ObjectiveKind.Secure: d=ObjectiveDefinition.Secure(id,zone,subjects,contestants,required,consecutive); break;
                    case ObjectiveKind.Stabilize: d=ObjectiveDefinition.Stabilize(id,required,interaction); break;
                    default: d=ObjectiveDefinition.Survive(id,required); break;
                }
                SaveIO.Require(d.Required==required,"Objective threshold mismatch.");
                var state=new State(d) { Status=SaveIO.EnumValue<ObjectiveStatus>(r), Progress=r.ReadInt32(), Rescued=r.ReadBoolean() };
                SaveIO.Require(state.Progress>=0 && state.Progress<=required && (!state.Rescued || kind==ObjectiveKind.RescueAndExtract),"Invalid objective progress.");
                SaveIO.Require(state.Status!=ObjectiveStatus.Completed || state.Progress==required,"Incomplete completed objective.");
                SaveIO.Require(state.Status!=ObjectiveStatus.Active || state.Progress<required,"Completed active objective.");
                restored[i]=state;
            }
            var definition=new MissionDefinition(missionId,attempt,squad,restored[0].Definition,priority,restored.Skip(1).Select(s=>s.Definition));
            var controller=new MissionController(turns,definition,true);
            for(int i=0;i<n;i++) controller.states[i]=restored[i];
            controller.LastResolvedRound=r.ReadInt32();
            SaveIO.Require(controller.LastResolvedRound>=0 && controller.LastResolvedRound<=turns.RoundNumber,"Invalid round checkpoint.");
            if(!turns.IsBattleEnded && turns.RoundNumber>0)
                SaveIO.Require(controller.LastResolvedRound>=turns.RoundNumber-1,"Skipped mission checkpoint.");
            if(controller.LastResolvedRound==turns.RoundNumber && turns.RoundNumber>0)
                SaveIO.Require(turns.IsRoundComplete || turns.IsBattleEnded,"Resolved unfinished round.");
            if(r.ReadBoolean())
            {
                var outcome=SaveIO.EnumValue<MissionOutcome>(r); int round=r.ReadInt32(); string reason=SaveIO.Text(r);
                SaveIO.Require(turns.IsBattleEnded && round==turns.RoundNumber && round>0,"Result on open battle.");
                bool won=restored[0].Status==ObjectiveStatus.Completed;
                bool squadLost=!squad.Any(controller.Alive);
                bool failed=squadLost || restored[0].Status==ObjectiveStatus.Failed;
                bool victory=won && (!failed || priority==OutcomePriority.SuccessFirst);
                SaveIO.Require((won || failed) && (outcome==MissionOutcome.Victory)==victory,"Inconsistent outcome.");
                string expected=victory ? "PrimaryCompleted" : squadLost ? "SquadUnavailable" : "PrimaryFailed";
                SaveIO.Require(reason==expected,"Invalid result reason.");
                controller.Result=new BattleResult(definition,outcome,round,reason,restored.Select(s=>s.View()).ToArray());
            }
            return controller;
        }
    }
}
