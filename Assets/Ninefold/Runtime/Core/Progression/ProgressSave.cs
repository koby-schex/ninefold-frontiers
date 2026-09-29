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
        private const int Version = 2;
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
                if (schema != 1 && schema != Version) throw new IncompatibleSaveException("Unsupported progress schema; preserve files.");
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
                SaveIO.Require(stream.Position == stream.Length, "Trailing progress data.");
                return new PlayerProgress(storedId, claims, initial, unlocks);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException || ex is InvalidOperationException)
            { throw new InvalidDataException("Invalid progress snapshot.", ex); }
        }
    }
}
