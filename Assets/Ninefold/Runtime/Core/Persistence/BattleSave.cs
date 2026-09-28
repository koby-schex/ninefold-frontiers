
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Ninefold.Core.Combat;

namespace Ninefold.Core.Persistence
{
    public sealed class IncompatibleSaveException : Exception
    {
        public IncompatibleSaveException(string message) : base(message) { }
    }

    /// <summary>Explicit binary schema. Call only on the simulation thread at completed-command boundaries.</summary>
    public static class BattleSave
    {
        public const int Version = 1;
        public const int MaximumBytes = 8 * 1024 * 1024;
        public static byte[] Capture(BattleTurnController battle, string contentRevision)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            CheckRevision(contentRevision);
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream,System.Text.Encoding.UTF8,true))
            {
                writer.Write(contentRevision);
                battle.WriteSave(writer);
            }
            var bytes = Pack(stream.ToArray());
            // Validate before publishing any file, including structural consistency and limits.
            Restore(bytes,contentRevision);
            return bytes;
        }
        public static BattleTurnController Restore(byte[] bytes, string expectedContentRevision)
        {
            CheckRevision(expectedContentRevision);
            try
            {
                using var stream = new MemoryStream(Unpack(bytes),false);
                using var reader = new BinaryReader(stream);
                if (SaveIO.Text(reader) != expectedContentRevision) throw new IncompatibleSaveException("Content revision mismatch; migration required.");
                var battle = BattleTurnController.ReadSave(reader);
                SaveIO.Require(stream.Position == stream.Length,"Trailing snapshot data.");
                return battle;
            }
            catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is InvalidOperationException || ex is OverflowException || ex is System.Collections.Generic.KeyNotFoundException)
            { throw new InvalidDataException("Invalid battle snapshot.",ex); }
        }
        private static void CheckRevision(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 1024) throw new ArgumentException("Content revision required.");
        }
        internal static byte[] Pack(byte[] payload)
        {
            if (payload.Length > MaximumBytes-40) throw new InvalidDataException("Save exceeds size limit.");
            using var stream = new MemoryStream();
            using var w = new BinaryWriter(stream);
            w.Write(0x4E465356); w.Write(Version); w.Write(payload);
            w.Flush();
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(stream.ToArray());
            w.Write(hash); w.Flush(); return stream.ToArray();
        }
        internal static byte[] Unpack(byte[] bytes)
        {
            SaveIO.Require(bytes != null && bytes.Length >= 40 && bytes.Length <= MaximumBytes,"Invalid save size.");
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(bytes,0,bytes.Length-32);
            SaveIO.Require(hash.SequenceEqual(bytes.Skip(bytes.Length-32)),"Save checksum mismatch.");
            using var r = new BinaryReader(new MemoryStream(bytes,false));
            SaveIO.Require(r.ReadInt32() == 0x4E465356,"Invalid save marker.");
            if (r.ReadInt32() != Version) throw new IncompatibleSaveException("Unsupported save version; preserve files.");
            return r.ReadBytes(bytes.Length-40);
        }
    }

    internal static class SaveIO
    {
        internal static void Require(bool valid, string message) { if (!valid) throw new InvalidDataException(message); }
        internal static int Count(BinaryReader r) { int n = r.ReadInt32(); Require(n >= 0 && n <= 4096,"Invalid collection count."); return n; }
        internal static string Text(BinaryReader r) { string s = r.ReadString(); Require(!string.IsNullOrWhiteSpace(s) && s.Length <= 1024,"Invalid identifier."); return s; }
        internal static T EnumValue<T>(BinaryReader r) where T : struct, Enum
        { int value = r.ReadInt32(); Require(Enum.IsDefined(typeof(T),value),"Invalid enum."); return (T)Enum.ToObject(typeof(T),value); }
        internal static void Strings(BinaryWriter w, System.Collections.Generic.IEnumerable<string> values)
        { var a = values.ToArray(); w.Write(a.Length); foreach (var s in a) w.Write(s); }
        internal static string[] Strings(BinaryReader r)
        {
            var a = new string[Count(r)]; for (int i=0;i<a.Length;i++) a[i]=Text(r);
            Require(a.Distinct(StringComparer.Ordinal).Count() == a.Length,"Duplicate identifier."); return a;
        }
        internal static void Point(BinaryWriter w, FieldPoint p) { w.Write(p.X); w.Write(p.Y); w.Write(p.Z); }
        internal static FieldPoint Point(BinaryReader r) => new FieldPoint(r.ReadDecimal(),r.ReadDecimal(),r.ReadDecimal());
        internal static void Box(BinaryWriter w, FieldBox box) { w.Write(box != null); if (box != null) { Point(w,box.Min); Point(w,box.Max); } }
        internal static FieldBox Box(BinaryReader r) => r.ReadBoolean() ? new FieldBox(Point(r),Point(r)) : null;
    }
}
