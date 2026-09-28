
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;
using Ninefold.Core.Persistence;

internal static partial class Program
{
    private const string SaveRevision="abstract-content-v1";
    private static (string Name,Action Run)[] SaveTests() => new (string,Action)[]
    {
        ("Save restores split movement without refreshing action", SaveSplit),
        ("Save preserves cooldown and Signature prerequisites", SaveAbilities),
        ("Used Signature stays spent after reload", SaveSignature),
        ("Save preserves health defeat and occupied-space release", SaveDefeat),
        ("Turn queue cursor survives removal and reinforcement", SaveQueue),
        ("Initiative modifications still wait for next round after reload", SaveInitiative),
        ("Terrain cover and bodies restore identical path and attack", SaveGeometry),
        ("Enemy planning and execution match after reload", SaveEnemy),
        ("Mission interaction progress and action survive", SaveObjective),
        ("Rescue flag survives without repeating rescue", SaveRescue),
        ("Resolved and pending round checkpoints both survive", SaveRoundCheckpoint),
        ("Terminal result stays terminal and unchanged", SaveResult),
        ("Interrupted complete encounter matches uninterrupted result", SaveEncounter),
        ("Unstarted empty and externally ended battles restore", SaveLifecycle),
        ("Snapshot bytes are independent of later mutation", SaveDetached),
        ("Corrupt truncated and oversized snapshots reject", SaveCorruption),
        ("Unsupported versions and content revisions reject", SaveIncompatible),
        ("Invalid scheduler state with valid checksum rejects", SaveInvalidState),
        ("Local store selects newest generation", StoreNewest),
        ("Interrupted partial write recovers previous valid save", StoreInterrupted),
        ("Corrupt newest save falls back and permits repair", StoreRecovery),
        ("Failure after complete write still loads latest state", StoreAfterWrite),
        ("No valid copy reports failure without resetting battle", StoreAllCorrupt),
        ("Unknown save versions block overwriting either copy", StoreIncompatible),
        ("Real filesystem adapter round trips across new instances", StoreDirectory)
    };
    private static BattleTurnController Reload(BattleTurnController b) => BattleSave.Restore(BattleSave.Capture(b,SaveRevision),SaveRevision);
    private static void SameSave(BattleTurnController a,BattleTurnController b)
        => Equal(true,BattleSave.Capture(a,SaveRevision).SequenceEqual(BattleSave.Capture(b,SaveRevision)));
    private static void SaveSplit()
    {
        var b=FieldBattle(); Move(b,P(1,0)); FieldHit(b); var restored=Reload(b);
        Equal(false,restored.CurrentActivation.PrimaryActionAvailable); Equal(19m,restored.CurrentActivation.MovementRemaining);
        Equal(b.CurrentActivation.ActivationId,restored.CurrentActivation.ActivationId);
        Move(b,P(2,0)); Move(restored,P(2,0)); SameSave(b,restored);
        Equal(false,restored.Battlefield.TryApplyEffect(1,AbilitySlot.NormalAttack,"b",out _,out _,out _));
    }
    private static void SaveAbilities()
    {
        var b=HealthBattle(readiness:SignatureReadiness.AfterMainAbility); Apply(b,Heal()); var r=Reload(b);
        Equal(2,r.Abilities.GetState("a").MainCooldownRemaining); Equal(true,r.Abilities.GetState("a").SignatureRequirementMet);
        NextHealthRound(b); NextHealthRound(r); SameSave(b,r); Equal(1,r.Abilities.GetState("a").MainCooldownRemaining);
    }
    private static void SaveSignature()
    {
        var b=HealthBattle(); Apply(b,Hit(slot:AbilitySlot.Signature)); var r=Reload(b);
        NextHealthRound(r); Equal(AbilityUseFailure.SignatureAlreadyUsed,r.Abilities.GetAvailability(r.CurrentActivation.ActivationId,AbilitySlot.Signature));
    }
    private static void SaveDefeat()
    {
        var b=FieldBattle(); FieldHit(b,AbilitySlot.Signature); Move(b,P(6,0)); var r=Reload(b);
        Equal(true,r.Health.GetState("b").IsDefeated); Equal(false,r.IsUnitEligible("b")); Equal(P(6,0),r.Battlefield.GetPosition("a")); SameSave(b,r);
    }
    private static void SaveQueue()
    {
        var b=B(U("a",30),U("b",20),U("c",10)); b.StartNextRound(); b.BeginNextActivation();
        b.RemoveUnit("b"); b.RegisterUnit(U("new",100)); b.EndActivation(1); var r=Reload(b);
        Equal("c",Drain(r)); Equal("c",Drain(b)); r.StartNextRound(); b.StartNextRound(); SameSave(b,r); Equal("new,a,c",Drain(r));
    }
    private static void SaveInitiative()
    {
        var b=B(U("a",30),U("b",20)); b.StartNextRound(); b.SetInitiative("b",50); var r=Reload(b);
        Equal("a,b",Drain(r)); r.StartNextRound(); Equal("b,a",Drain(r));
    }
    private static void SaveGeometry()
    {
        var b=FieldBattle(new[] { Wall(false,.25m) },new[] { new DifficultGround(Box(-3,-3,-1,-1),2m) });
        var r=Reload(b); SameRoute(FindRoute(b,P(4,0)),FindRoute(r,P(4,0)));
        Equal(FieldHit(b).HealthChanged,FieldHit(r).HealthChanged); SameSave(b,r);
    }
    private static void SaveEnemy()
    {
        var b=EnemyBattle(); var r=Reload(b);
        var a=RunEnemy(b,"b"); var c=RunEnemy(r,"b");
        Equal(a.Effect.HealthChanged,c.Effect.HealthChanged); SameSave(b,r);
    }
    private static void SaveObjective()
    {
        var b=MissionBattle(ObjectiveDefinition.Stabilize("primary",2,InteractAt())); Interact(b,"primary"); var r=Reload(b);
        Equal(1,r.Mission.GetObjective("primary").Progress); Equal(false,r.CurrentActivation.PrimaryActionAvailable);
        NextMissionRound(b); NextMissionRound(r); Interact(b,"primary"); Interact(r,"primary"); SameSave(b,r);
    }
    private static void SaveRescue()
    {
        var b=MissionBattle(ObjectiveDefinition.Rescue("primary","c",Box(-1,7,1,9),InteractAt())); Interact(b,"primary"); var r=Reload(b);
        Equal(true,r.Mission.GetObjective("primary").Rescued); Equal(InteractionFailure.Unavailable,r.Mission.PreviewInteraction(1,"primary")); SameSave(b,r);
    }
    private static void SaveRoundCheckpoint()
    {
        var b=MissionBattle(ObjectiveDefinition.Survive("primary",3)); b.EndActivation(1); Drain(b); var pending=Reload(b);
        Throws<InvalidOperationException>(()=>pending.StartNextRound()); pending.Mission.ResolveRoundEnd(1);
        var r=Reload(pending); r.Mission.ResolveRoundEnd(1); Equal(1,r.Mission.GetObjective("primary").Progress);
        r.StartNextRound(); r.BeginNextActivation(); Equal(2,r.RoundNumber);
    }
    private static void SaveResult()
    {
        var b=MissionBattle(ObjectiveDefinition.Stabilize("primary",1,InteractAt())); Interact(b,"primary"); var r=Reload(b);
        Equal(true,r.IsBattleEnded); Equal(MissionOutcome.Victory,r.Mission.Result.Outcome); SameSave(b,r);
        Equal(true,ReferenceEquals(r.Mission.Result,r.Mission.Evaluate())); Throws<InvalidOperationException>(()=>r.StartNextRound());
    }
    private static void SaveEncounter()
    {
        var a=EnemyBattle(targetHp:80,range:10,mission:EnemyMission()); var b=Reload(a);
        RunEnemy(a,"b"); RunEnemy(b,"b"); b=Reload(b);
        AdvanceEnemyRound(a); AdvanceEnemyRound(b); b=Reload(b);
        RunEnemy(a,"b"); RunEnemy(b,"b"); b=Reload(b);
        Equal(MissionOutcome.Defeat,b.Mission.Result.Outcome); SameSave(a,b);
    }
    private static void SaveLifecycle()
    {
        SameSave(B(),Reload(B())); var b=B(U("a")); b.EndBattle(); var r=Reload(b); Equal(true,r.IsBattleEnded); SameSave(b,r);
    }
    private static void SaveDetached()
    {
        var b=FieldBattle(); byte[] bytes=BattleSave.Capture(b,SaveRevision); Move(b,P(1,0));
        var r=BattleSave.Restore(bytes,SaveRevision); Equal(P(0,0),r.Battlefield.GetPosition("a")); Equal(20m,r.CurrentActivation.MovementRemaining);
    }
    private static void SaveCorruption()
    {
        var bytes=BattleSave.Capture(FieldBattle(),SaveRevision);
        foreach(int length in new[] { 0,1,20,bytes.Length-1 })
            Throws<InvalidDataException>(()=>BattleSave.Restore(bytes.Take(length).ToArray(),SaveRevision));
        bytes[bytes.Length/2]^=1; Throws<InvalidDataException>(()=>BattleSave.Restore(bytes,SaveRevision));
        Throws<InvalidDataException>(()=>BattleSave.Restore(new byte[BattleSave.MaximumBytes+1],SaveRevision));
    }
    private static void Rehash(byte[] bytes)
    {
        using var sha=SHA256.Create(); var hash=sha.ComputeHash(bytes,0,bytes.Length-32); Array.Copy(hash,0,bytes,bytes.Length-32,32);
    }
    private static void SaveIncompatible()
    {
        var bytes=BattleSave.Capture(FieldBattle(),SaveRevision);
        Throws<IncompatibleSaveException>(()=>BattleSave.Restore(bytes,"other-content"));
        Array.Copy(BitConverter.GetBytes(2),0,bytes,4,4); Rehash(bytes);
        Throws<IncompatibleSaveException>(()=>BattleSave.Restore(bytes,SaveRevision));
    }
    private static void SaveInvalidState()
    {
        var bytes=BattleSave.Capture(B(),SaveRevision);
        using var stream=new MemoryStream(bytes); using var reader=new BinaryReader(stream);
        stream.Position=8; reader.ReadString(); reader.ReadInt32(); reader.ReadDecimal(); reader.ReadBoolean();
        Equal(0,reader.ReadInt32()); int offset=(int)stream.Position;
        Array.Copy(BitConverter.GetBytes(-1),0,bytes,offset,4); Rehash(bytes);
        Throws<InvalidDataException>(()=>BattleSave.Restore(bytes,SaveRevision));
    }
    private sealed class FakeSaveFiles : IBattleSaveFiles
    {
        internal readonly byte[][] Slots=new byte[2][];
        internal int Fault; // 1: before write, 2: torn write, 3: after durable bytes
        public byte[] Read(int slot) => Slots[slot]?.ToArray();
        public void WriteDurable(int slot,byte[] bytes)
        {
            if(Fault==1) throw new IOException("Before write");
            Slots[slot]=(Fault==2 ? bytes.Take(bytes.Length/2) : bytes).ToArray();
            if(Fault>0) throw new IOException("Interrupted write");
        }
    }
    private static void StoreNewest()
    {
        var files=new FakeSaveFiles(); var store=new LocalBattleStore(files,SaveRevision); Equal<LoadedBattle>(null,store.Load());
        var b=FieldBattle(); Equal(1L,store.Save(b)); Move(b,P(1,0)); Equal(2L,store.Save(b));
        var loaded=new LocalBattleStore(files,SaveRevision).Load(); Equal(2L,loaded.Generation); Equal(false,loaded.Recovered); SameSave(b,loaded.Battle);
    }
    private static void StoreInterrupted()
    {
        foreach(int fault in new[] { 1,2 })
        {
            var files=new FakeSaveFiles(); var store=new LocalBattleStore(files,SaveRevision); var b=FieldBattle(); store.Save(b);
            Move(b,P(1,0)); files.Fault=fault; Throws<IOException>(()=>store.Save(b));
            var loaded=store.Load(); Equal(1L,loaded.Generation); Equal(P(0,0),loaded.Battle.Battlefield.GetPosition("a"));
        }
    }
    private static void StoreRecovery()
    {
        var files=new FakeSaveFiles(); var store=new LocalBattleStore(files,SaveRevision); var b=FieldBattle();
        store.Save(b); Move(b,P(1,0)); store.Save(b); files.Slots[1][20]^=1;
        var loaded=store.Load(); Equal(true,loaded.Recovered); Equal(1L,loaded.Generation);
        store.Save(loaded.Battle); Equal(false,store.Load().Recovered); Equal(2L,store.Load().Generation);
    }
    private static void StoreAfterWrite()
    {
        var files=new FakeSaveFiles(); var store=new LocalBattleStore(files,SaveRevision); var b=FieldBattle(); store.Save(b);
        Move(b,P(1,0)); files.Fault=3; Throws<IOException>(()=>store.Save(b)); SameSave(b,store.Load().Battle); Equal(2L,store.Load().Generation);
    }
    private static void StoreAllCorrupt()
    {
        var files=new FakeSaveFiles(); files.Slots[0]=new byte[12]; files.Slots[1]=new byte[20];
        var store=new LocalBattleStore(files,SaveRevision); Throws<InvalidDataException>(()=>store.Load());
        Throws<InvalidDataException>(()=>store.Save(FieldBattle())); Equal(12,files.Slots[0].Length);
    }
    private static void StoreIncompatible()
    {
        var files=new FakeSaveFiles(); var store=new LocalBattleStore(files,SaveRevision); store.Save(FieldBattle());
        var newer=files.Slots[0].ToArray(); Array.Copy(BitConverter.GetBytes(2),0,newer,4,4); Rehash(newer); files.Slots[1]=newer;
        Throws<IncompatibleSaveException>(()=>store.Load()); Throws<IncompatibleSaveException>(()=>store.Save(FieldBattle()));
        Equal(true,newer.SequenceEqual(files.Slots[1]));
    }
    private static void StoreDirectory()
    {
        string directory=Path.Combine(Path.GetTempPath(),"ninefold-save-"+Guid.NewGuid().ToString("N"));
        try
        {
            var b=FieldBattle(); var store=new LocalBattleStore(new DirectoryBattleSaveFiles(directory),SaveRevision);
            store.Save(b); Move(b,P(1,0)); store.Save(b);
            var loaded=new LocalBattleStore(new DirectoryBattleSaveFiles(directory),SaveRevision).Load();
            SameSave(b,loaded.Battle); Equal(2L,loaded.Generation);
        }
        finally { if(Directory.Exists(directory)) Directory.Delete(directory,true); }
    }
}
