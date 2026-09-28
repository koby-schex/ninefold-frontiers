
using System;
using System.IO;
using System.Linq;
using Ninefold.Core.Persistence;
using Ninefold.Core.Missions;

namespace Ninefold.Core.Combat
{
    public sealed partial class BattleTurnController
    {
        internal void WriteSave(BinaryWriter w)
        {
            w.Write(Health.Rules.ArmorScale); w.Write(Health.Rules.MaximumOrdinaryReduction); w.Write(Health.Rules.MinimumOneDamage);
            w.Write(units.Count);
            foreach (var entry in units.OrderBy(x => x.Key,StringComparer.Ordinal))
            {
                var u = entry.Value; w.Write(entry.Key); w.Write(u.Definition.Initiative); w.Write(u.Definition.MovementAllowance);
                w.Write(u.Initiative); w.Write(u.ActivationCount); w.Write(u.Eligible);
            }
            w.Write(RoundNumber); w.Write(IsBattleEnded); w.Write(nextIndex); w.Write(activationSequence);
            SaveIO.Strings(w,roundOrder); w.Write(active != null);
            if (active != null) { w.Write(active.ActivationId); w.Write(active.UnitId); w.Write(active.RoundNumber); w.Write(active.MovementRemaining); w.Write(active.PrimaryActionAvailable); }
            Abilities.WriteSave(w); Health.WriteSave(w);
            w.Write(Battlefield != null); Battlefield?.WriteSave(w);
            w.Write(Mission != null); Mission?.WriteSave(w);
        }
        internal static BattleTurnController ReadSave(BinaryReader r)
        {
            var b = new BattleTurnController(Array.Empty<UnitTurnDefinition>(),new DamageRules(r.ReadInt32(),r.ReadDecimal(),r.ReadBoolean()));
            int count = SaveIO.Count(r);
            for (int i=0;i<count;i++)
            {
                string id = SaveIO.Text(r); var definition = new UnitTurnDefinition(id,r.ReadInt32(),r.ReadDecimal());
                var unit = new UnitState(definition) { Initiative = r.ReadInt32(), ActivationCount = r.ReadInt64(), Eligible = r.ReadBoolean() };
                SaveIO.Require(unit.Initiative >= 0 && unit.ActivationCount >= 0,"Invalid unit turn state.");
                b.units.Add(id,unit);
            }
            b.RoundNumber = r.ReadInt32(); b.IsBattleEnded = r.ReadBoolean(); b.nextIndex = r.ReadInt32(); b.activationSequence = r.ReadInt64();
            b.roundOrder = SaveIO.Strings(r);
            SaveIO.Require(b.RoundNumber >= 0 && b.activationSequence >= 0 && b.nextIndex >= 0 && b.nextIndex <= b.roundOrder.Length,"Invalid scheduler.");
            SaveIO.Require(b.roundOrder.All(b.units.ContainsKey),"Unknown queued unit.");
            SaveIO.Require(b.RoundNumber != 0 || (b.roundOrder.Length == 0 && b.nextIndex == 0 && b.activationSequence == 0),"Invalid unstarted battle.");
            SaveIO.Require(b.units.Values.Sum(x => x.ActivationCount) == b.activationSequence,"Activation counters disagree.");
            SaveIO.Require(b.units.Values.All(x => x.ActivationCount <= b.RoundNumber),"Too many owner activations.");
            if (r.ReadBoolean())
            {
                b.active = new ActivationView(r.ReadInt64(),SaveIO.Text(r),r.ReadInt32(),r.ReadDecimal(),r.ReadBoolean());
                var a = b.active; var owner = b.FindUnit(a.UnitId);
                SaveIO.Require(!b.IsBattleEnded && b.RoundNumber > 0 && a.RoundNumber == b.RoundNumber && a.ActivationId == b.activationSequence
                    && a.ActivationId > 0 && b.nextIndex > 0 && b.roundOrder[b.nextIndex-1] == a.UnitId && owner.Eligible
                    && owner.ActivationCount > 0 && a.MovementRemaining >= 0m && a.MovementRemaining <= owner.Definition.MovementAllowance,"Invalid active turn.");
            }
            b.Abilities.ReadSave(r); b.Health.ReadSave(r);
            if (r.ReadBoolean()) b.Battlefield = BattlefieldController.ReadSave(b,r);
            if (r.ReadBoolean()) b.Mission = MissionController.ReadSave(b,r);
            return b;
        }
    }
    public sealed partial class BattleAbilityController
    {
        internal void WriteSave(BinaryWriter w)
        {
            w.Write(kits.Count);
            foreach (var e in kits.OrderBy(x=>x.Key,StringComparer.Ordinal))
            {
                w.Write(e.Key); w.Write(e.Value.Definition.MainCooldownTurns); w.Write((int)e.Value.Definition.SignatureRequirement);
                w.Write(e.Value.MainReadyAtActivation); w.Write(e.Value.SignatureRequirementMet); w.Write(e.Value.SignatureUsed);
            }
        }
        internal void ReadSave(BinaryReader r)
        {
            int n = SaveIO.Count(r);
            for (int i=0;i<n;i++)
            {
                string id = SaveIO.Text(r); long owner = turns.GetActivationCount(id);
                var state = new AbilityState(new UnitAbilityDefinition(r.ReadInt32(),SaveIO.EnumValue<SignatureReadiness>(r)))
                { MainReadyAtActivation = r.ReadInt64(), SignatureRequirementMet = r.ReadBoolean(), SignatureUsed = r.ReadBoolean() };
                SaveIO.Require(state.MainReadyAtActivation >= 0 && state.MainReadyAtActivation <= checked(owner+state.Definition.MainCooldownTurns)
                    && (!state.SignatureUsed || state.SignatureRequirementMet),"Invalid ability resources.");
                SaveIO.Require(state.Definition.SignatureRequirement != SignatureReadiness.ReadyAtDeployment || state.SignatureRequirementMet,"Deployment readiness missing.");
                SaveIO.Require(owner > 0 || (!state.SignatureUsed && state.MainReadyAtActivation == 0),"Unactivated unit spent ability.");
                kits.Add(id,state);
            }
        }
    }
    public sealed partial class BattleHealthController
    {
        internal void WriteSave(BinaryWriter w)
        {
            w.Write(units.Count);
            foreach (var e in units.OrderBy(x=>x.Key,StringComparer.Ordinal))
            { w.Write(e.Key); w.Write(e.Value.Definition.TeamId); w.Write(e.Value.Definition.MaximumHealth); w.Write(e.Value.Definition.Armor); w.Write(e.Value.Current); }
        }
        internal void ReadSave(BinaryReader r)
        {
            int n=SaveIO.Count(r);
            for (int i=0;i<n;i++)
            {
                string id=SaveIO.Text(r); bool eligible=turns.IsUnitEligible(id);
                var definition=new UnitHealthDefinition(SaveIO.Text(r),r.ReadInt32(),r.ReadInt32()); int hp=r.ReadInt32();
                SaveIO.Require(hp >= 0 && hp <= definition.MaximumHealth && (hp != 0 || !eligible),"Invalid health.");
                units.Add(id,new HealthState(definition,hp));
            }
        }
    }
}
