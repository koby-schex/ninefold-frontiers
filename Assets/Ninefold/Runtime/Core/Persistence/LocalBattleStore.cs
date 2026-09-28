
using System;
using System.IO;
using System.Linq;
using Ninefold.Core.Combat;

namespace Ninefold.Core.Persistence
{
    /// <summary>Single-writer storage boundary; null means absent. Tests inject interrupted writes here.</summary>
    public interface IBattleSaveFiles
    {
        byte[] Read(int slot);
        void WriteDurable(int slot, byte[] bytes);
    }
    public sealed class DirectoryBattleSaveFiles : IBattleSaveFiles
    {
        private readonly string directory;
        public DirectoryBattleSaveFiles(string directory)
        {
            if(string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Save directory required.");
            this.directory=Path.GetFullPath(directory);
        }
        private string Name(int slot)
        {
            if(slot<0 || slot>1) throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(directory,"battle."+slot+".save");
        }
        public byte[] Read(int slot)
        {
            try
            {
                using var file=new FileStream(Name(slot),FileMode.Open,FileAccess.Read,FileShare.Read);
                if(file.Length>BattleSave.MaximumBytes) throw new InvalidDataException("Save exceeds size limit.");
                using var bytes=new MemoryStream(); file.CopyTo(bytes); return bytes.ToArray();
            }
            catch(FileNotFoundException) { return null; }
            catch(DirectoryNotFoundException) { return null; }
        }
        public void WriteDurable(int slot, byte[] bytes)
        {
            Directory.CreateDirectory(directory);
            using var file=new FileStream(Name(slot),FileMode.Create,FileAccess.Write,FileShare.None);
            file.Write(bytes,0,bytes.Length); file.Flush(true);
        }
    }
    public sealed class LoadedBattle
    {
        public BattleTurnController Battle { get; }
        public long Generation { get; }
        public bool Recovered { get; }
        internal LoadedBattle(BattleTurnController battle,long generation,bool recovered)
        { Battle=battle; Generation=generation; Recovered=recovered; }
    }
    public sealed class LocalBattleStore
    {
        private sealed class Record
        {
            internal int Slot; internal long Generation; internal BattleTurnController Battle;
        }
        private readonly IBattleSaveFiles files;
        private readonly string revision;
        private readonly object gate=new object();
        public LocalBattleStore(IBattleSaveFiles files,string contentRevision)
        {
            this.files=files ?? throw new ArgumentNullException(nameof(files));
            if(string.IsNullOrWhiteSpace(contentRevision) || contentRevision.Length>1024) throw new ArgumentException("Content revision required.");
            revision=contentRevision;
        }
        public LoadedBattle Load()
        {
            lock(gate)
            {
                var record=Latest(out var recovered);
                return record==null ? null : new LoadedBattle(record.Battle,record.Generation,recovered);
            }
        }
        public long Save(BattleTurnController battle)
        {
            lock(gate)
            {
                byte[] snapshot=BattleSave.Capture(battle,revision);
                var previous=Latest(out _);
                long generation=checked((previous?.Generation ?? 0)+1);
                int slot=previous==null ? 0 : 1-previous.Slot;
                using var stream=new MemoryStream();
                using(var w=new BinaryWriter(stream,System.Text.Encoding.UTF8,true))
                { w.Write(generation); w.Write(snapshot.Length); w.Write(snapshot); }
                // Only the other slot is overwritten. A torn write cannot damage the current valid generation.
                files.WriteDurable(slot,BattleSave.Pack(stream.ToArray()));
                return generation;
            }
        }
        private Record Latest(out bool recovered)
        {
            bool damaged=false;
            var records=new Record[2];
            for(int slot=0;slot<2;slot++)
            {
                try
                {
                    byte[] bytes=files.Read(slot);
                    if(bytes==null) continue;
                    using var stream=new MemoryStream(BattleSave.Unpack(bytes),false);
                    using var r=new BinaryReader(stream);
                    long generation=r.ReadInt64(); int length=r.ReadInt32();
                    SaveIO.Require(generation>0 && length>=40 && length<=BattleSave.MaximumBytes && length==stream.Length-stream.Position,"Invalid save record.");
                    records[slot]=new Record { Slot=slot, Generation=generation, Battle=BattleSave.Restore(r.ReadBytes(length),revision) };
                }
                catch(InvalidDataException) { damaged=true; }
                catch(EndOfStreamException) { damaged=true; }
            }
            var ordered=records.Where(x=>x!=null).OrderByDescending(x=>x.Generation).ToArray();
            SaveIO.Require(ordered.Length!=0 || !damaged,"No valid local battle save remains.");
            SaveIO.Require(ordered.Length!=2 || ordered[0].Generation!=ordered[1].Generation,"Conflicting save generations.");
            recovered=damaged;
            return ordered.FirstOrDefault();
        }
    }
}
