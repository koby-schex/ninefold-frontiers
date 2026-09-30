using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Ninefold.Core.Missions;
using Ninefold.Core.Persistence;

namespace Ninefold.Core.Progression
{
    internal static class ProgressSave
    {
        private const int Version = 5;
        internal static string Fingerprint(BattleResult result)
        {
            using var stream = new MemoryStream();
            using (var w = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                w.Write(result.MissionId); w.Write(result.AttemptId); w.Write((int)result.Outcome);
                w.Write(result.Round); w.Write(result.Reason); w.Write(result.Objectives.Count);
                foreach (var o in result.Objectives)
                {
                    w.Write(o.Id); w.Write((int)o.Kind); w.Write((int)o.Status);
                    w.Write(o.Progress); w.Write(o.Required); w.Write(o.Rescued);
                }
            }
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "");
        }
        internal static byte[] Encode(PlayerProgress progress)
        {
            using var stream = new MemoryStream();
            using (var w = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                w.Write("ninefold-progress"); w.Write(Version); w.Write(progress.ProfileId);
                w.Write(progress.Claims.Count);
                foreach (var c in progress.Claims)
                {
                    w.Write(c.AttemptId); w.Write(c.MissionId); w.Write(c.ResultFingerprint);
                    w.Write((int)c.Outcome); w.Write(c.IsFirstClear); w.Write(c.PolicyRevision);
                    w.Write(c.Grants.Count);
                    foreach (var g in c.Grants) { w.Write(g.ResourceId); w.Write(g.Amount); }
                }
                SaveIO.Strings(w, progress.InitialUnits);
                w.Write(progress.Unlocks.Count);
                foreach (var u in progress.Unlocks)
                {
                    w.Write(u.OperationId); w.Write(u.UnitId); w.Write(u.FragmentResourceId);
                    w.Write(u.Cost); w.Write(u.DefinitionRevision);
                }
                w.Write(progress.CampaignClaims.Count);
                foreach (var c in progress.CampaignClaims)
                {
                    w.Write(c.CampaignId); w.Write(c.DefinitionRevision); SaveIO.Strings(w, c.RequiredMissions);
                    w.Write(c.Grants.Count);
                    foreach (var grant in c.Grants) { w.Write(grant.ResourceId); w.Write(grant.Amount); }
                    SaveIO.Strings(w, c.GrantedUnits); w.Write(c.IsStarterBonus);
                    if (c.IsStarterBonus) w.Write(c.NextCampaignId);
                }
                w.Write(progress.AdvancementReceipts.Count);
                foreach (var a in progress.AdvancementReceipts)
                {
                    w.Write(a.OperationId); w.Write(a.UnitId); w.Write(a.Rank); w.Write(a.FragmentResourceId);
                    w.Write(a.Cost); w.Write(a.DefinitionRevision);
                    w.Write(a.TotalBonus.Health); w.Write(a.TotalBonus.Armor); w.Write(a.TotalBonus.Power);
                }
                w.Write(progress.CustomizationReceipts.Count);
                foreach (var c in progress.CustomizationReceipts)
                {
                    w.Write(c.OperationId); w.Write(c.UnitId); w.Write(c.OptionId != null);
                    if (c.OptionId != null) w.Write(c.OptionId);
                    w.Write(c.DefinitionRevision); w.Write(c.Bonus.Health); w.Write(c.Bonus.Armor); w.Write(c.Bonus.Power);
                }
            }
            var bytes = BattleSave.Pack(stream.ToArray());
            Decode(bytes, progress.ProfileId); // Validate limits and schema before writing any slot.
            return bytes;
        }
        internal static PlayerProgress Decode(byte[] bytes, string profileId)
        {
            try
            {
                using var stream = new MemoryStream(BattleSave.Unpack(bytes), false);
                using var r = new BinaryReader(stream);
                if (SaveIO.Text(r) != "ninefold-progress")
                    throw new IncompatibleSaveException("Unsupported progress schema; preserve files.");
                int schema = r.ReadInt32();
                if (schema < 1 || schema > Version) throw new IncompatibleSaveException("Unsupported progress schema; preserve files.");
                string storedId = SaveIO.Text(r);
                if (storedId != profileId) throw new IncompatibleSaveException("Wrong profile; preserve files.");
                int count = r.ReadInt32();
                SaveIO.Require(count >= 0 && count <= RewardRules.MaximumClaims, "Invalid receipt count.");
                var claims = new MissionClaim[count];
                for (int i = 0; i < count; i++)
                {
                    string attempt = SaveIO.Text(r), mission = SaveIO.Text(r), fingerprint = SaveIO.Text(r);
                    var outcome = SaveIO.EnumValue<MissionOutcome>(r);
                    byte flag = r.ReadByte(); SaveIO.Require(flag <= 1, "Invalid first-clear flag.");
                    string revision = SaveIO.Text(r);
                    var grants = new ResourceGrant[SaveIO.Count(r)];
                    for (int j = 0; j < grants.Length; j++) grants[j] = new ResourceGrant(SaveIO.Text(r), r.ReadInt64());
                    claims[i] = new MissionClaim(attempt, mission, fingerprint, outcome, flag == 1, revision, grants);
                }
                var initial = schema == 1 ? Array.Empty<string>() : SaveIO.Strings(r);
                int unlockCount = schema == 1 ? 0 : r.ReadInt32();
                SaveIO.Require(unlockCount >= 0 && unlockCount <= RewardRules.MaximumClaims - count, "Invalid unlock count.");
                var unlocks = new UnitUnlockReceipt[unlockCount];
                for (int i = 0; i < unlockCount; i++)
                {
                    string operation = SaveIO.Text(r), unit = SaveIO.Text(r), resource = SaveIO.Text(r);
                    long cost = r.ReadInt64(); string revision = SaveIO.Text(r);
                    unlocks[i] = new UnitUnlockReceipt(operation, new UnitUnlockDefinition(unit, resource, cost, revision));
                }
                int completionCount = schema < 3 ? 0 : r.ReadInt32();
                SaveIO.Require(completionCount >= 0 && completionCount <= RewardRules.MaximumClaims - count - unlockCount, "Invalid campaign receipt count.");
                var completions = new CampaignClaim[completionCount];
                for (int i = 0; i < completionCount; i++)
                {
                    string id = SaveIO.Text(r), revision = SaveIO.Text(r); var required = SaveIO.Strings(r);
                    var grants = new ResourceGrant[SaveIO.Count(r)];
                    for (int j = 0; j < grants.Length; j++) grants[j] = new ResourceGrant(SaveIO.Text(r), r.ReadInt64());
                    var units = SaveIO.Strings(r); byte starter = r.ReadByte(); SaveIO.Require(starter <= 1, "Invalid starter flag.");
                    string next = starter == 1 ? SaveIO.Text(r) : null;
                    completions[i] = new CampaignClaim(id, revision, required, grants, units, starter == 1, next);
                }
                int advancementCount = schema < 4 ? 0 : r.ReadInt32();
                SaveIO.Require(advancementCount >= 0 && advancementCount <= RewardRules.MaximumClaims - count - unlockCount - completionCount, "Invalid advancement count.");
                var advances = new AdvancementReceipt[advancementCount];
                for (int i = 0; i < advancementCount; i++)
                {
                    string operation = SaveIO.Text(r), unit = SaveIO.Text(r); int rank = r.ReadInt32(); string resource = SaveIO.Text(r);
                    long cost = r.ReadInt64(); string revision = SaveIO.Text(r);
                    var bonus = new AdvancementBonus(r.ReadInt32(), r.ReadInt32(), r.ReadInt32());
                    advances[i] = new AdvancementReceipt(operation, unit, rank, resource, cost, revision, bonus);
                }
                int choiceCount = schema < 5 ? 0 : r.ReadInt32();
                SaveIO.Require(choiceCount >= 0 && choiceCount <= RewardRules.MaximumClaims - count - unlockCount - completionCount - advancementCount, "Invalid customization count.");
                var choices = new CustomizationReceipt[choiceCount];
                for (int i = 0; i < choiceCount; i++)
                {
                    string operation = SaveIO.Text(r), unit = SaveIO.Text(r); byte hasOption = r.ReadByte();
                    SaveIO.Require(hasOption <= 1, "Invalid customization flag."); string option = hasOption == 1 ? SaveIO.Text(r) : null;
                    string revision = SaveIO.Text(r); var bonus = new CustomizationBonus(r.ReadInt32(), r.ReadInt32(), r.ReadInt32());
                    choices[i] = new CustomizationReceipt(operation, unit, option, revision, bonus);
                }
                SaveIO.Require(stream.Position == stream.Length, "Trailing progress data.");
                return new PlayerProgress(storedId, claims, initial, unlocks, completions, advances, choices);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException || ex is InvalidOperationException)
            { throw new InvalidDataException("Invalid progress snapshot.", ex); }
        }
    }
}
