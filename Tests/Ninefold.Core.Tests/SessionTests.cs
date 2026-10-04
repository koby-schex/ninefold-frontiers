using System;
using System.IO;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Persistence;

internal static partial class Program
{
    private static (string Name,Action Run)[] SessionTests() => new (string,Action)[] {
        ("Session waits indefinitely for player without saves", SessionWait),
        ("Session checkpoints movement and abilities without ending player turn", SessionCommands),
        ("Session rejects invalid player commands without checkpoint", SessionInvalid),
        ("Session requires correct owner and activation", SessionOwnership),
        ("Session runs one enemy activation using explicit perception", SessionEnemy),
        ("Session holds friendly NPCs without giving player control", SessionNpc),
        ("Session resolves round objective exactly once", SessionRounds),
        ("Session advances unstarted battles one boundary at a time", SessionUnstarted),
        ("Session victory stops before later enemy activation", SessionVictory),
        ("Session enemy defeat stops scheduling and retains ownership", SessionDefeat),
        ("Session interaction result connects reward claim and selection", SessionInteract),
        ("Session resumes player commands without refreshing costs", SessionResume),
        ("Session resumes enemy turn without executing it twice", SessionEnemyResume),
        ("Session torn enemy checkpoint restores whole prior turn", SessionTorn),
        ("Session ambiguous checkpoint requires reload and restores committed turn", SessionAmbiguous),
        ("Session duration and passive triggers are not doubled on resume", SessionStatuses),
        ("Session snapshots and perception inputs are detached", SessionDetached),
        ("Session rejects simultaneous player and AI ownership", SessionConflict),
        ("Session cannot send stale commands into a new mission attempt", SessionAttempt),
        ("Session bounded steps finish a complete survival encounter", SessionEncounter)
    };
    private sealed class SessionFixture
    {
        internal readonly FakeSaveFiles Files=new FakeSaveFiles();
        internal readonly ProgressFiles Profiles=new ProgressFiles();
        internal MissionFlow Flow;
        internal BattleSession Session;
        private readonly MissionEntry entry;
        internal SessionFixture(bool enemy=true,bool started=true, int rounds=2,bool stabilize=false,int playerHp=100,int enemyHp=100,bool conflict=false,bool status=false)
        {
            entry=new MissionEntry("session-m",1,3,null,false,Rewards("session-m"),(attempt,squad)=> {
                var b=B(U("a",30),U("b",20),U("c",10));
                foreach(string id in new[] { "a","b","c" })
                {
                    b.Health.RegisterHealth(id,new UnitHealthDefinition(id=="b"?"enemy":"ally",100,0),id=="a"?playerHp:id=="b"?enemyHp:100);
                    b.Abilities.RegisterKit(id,new UnitAbilityDefinition(2,SignatureReadiness.ReadyAtDeployment));
                }
                var f=b.ConfigureBattlefield(new BattlefieldMap(Box(-20,-20,20,20),Array.Empty<FieldObstacle>()));
                f.Register("a",P(0,0),Body(),Profiles(),new[] { "a","c" }); f.Register("b",P(6,0),Body(),new[] { new FieldAbility(AbilitySlot.NormalAttack,HealthEffectKind.Damage,20,10,true) },new[] { "b" }); f.Register("c",P(0,6),Body(),Profiles(),new[] { "a","c" });
                if(enemy) f.RegisterEnemy("b",new EnemyBehavior(EnemyStyle.Aggressive,6));
                if(conflict) f.RegisterEnemy("a",new EnemyBehavior(EnemyStyle.Aggressive,6));
                if(status) { b.Statuses.RegisterStatus(new StatusDefinition("s",1000,0,2,3,StatusStacking.AddStackAndRefresh)); b.Statuses.RegisterPassive("a",new PassiveDefinition(PassiveTrigger.OwnerActivationStarted,"s")); }
                b.ConfigureMission(new MissionDefinition("session-m",attempt,squad,stabilize?ObjectiveDefinition.Stabilize("primary",1,InteractAt()):rounds==0?ObjectiveDefinition.Defeat("primary",new[] { "b" }):ObjectiveDefinition.Survive("primary",rounds),OutcomePriority.FailureFirst));
                if(started) { b.StartNextRound(); b.BeginNextActivation(); } return b;
            });
            Flow=Make(); Flow.CreateProfile(new[] { "a","c","d" }); Flow.Start("session-m",new[] { "a" }); Session=new BattleSession(Flow);
        }
        private MissionFlow Make()=>new MissionFlow(Files,Profiles,"session-profile","session-v1",new[] { entry },FlowRoster());
        internal void Reopen() { Flow=Make(); Flow.Open(); Session=new BattleSession(Flow); }
        internal string Bytes()=>string.Join("|",Files.Slots.Select(s=>s==null?"null":Convert.ToBase64String(s)));
        internal long Id=>Session.Read().Activation.ActivationId;
        internal void ToEnemy() { Session.EndPlayerTurn(Id); Session.Advance(); }
    }
    private static EnemyPerception Seen()=>new EnemyPerception(new[] { "a" },Array.Empty<string>());
    private static void SessionWait()
    { var x=new SessionFixture(); string before=x.Bytes(); for(int i=0;i<5;i++) Equal(BattleSessionPhase.PlayerInput,x.Session.Advance().Phase); Equal(before,x.Bytes()); }
    private static void SessionCommands()
    {
        var x=new SessionFixture(); Equal(true,x.Session.TryMove(x.Id,new[] { P(1,0) },out _,out _)); Equal(true,x.Session.TryUseAbility(x.Id,AbilitySlot.NormalAttack,"b",out _,out _,out _));
        Equal(BattleSessionPhase.PlayerInput,x.Session.Read().Phase); Equal(4m,x.Session.Read().Activation.MovementRemaining); Equal(false,x.Session.Read().Activation.PrimaryActionAvailable);
    }
    private static void SessionInvalid()
    {
        var x=new SessionFixture(); string before=x.Bytes(); Equal(false,x.Session.TryMove(x.Id,new[] { P(19,19) },out _,out _)); Equal(false,x.Session.TryUseAbility(x.Id,AbilitySlot.NormalAttack,"a",out _,out _,out _)); Equal(false,x.Session.TryInteract(x.Id,"unknown",out _)); Equal(before,x.Bytes()); Equal(false,x.Flow.NeedsReload);
    }
    private static void SessionOwnership()
    {
        var x=new SessionFixture(); Throws<InvalidOperationException>(()=>x.Session.EndPlayerTurn(999)); x.ToEnemy(); string before=x.Bytes(); Throws<InvalidOperationException>(()=>x.Session.TryUseAbility(x.Id,AbilitySlot.NormalAttack,"a",out _,out _,out _)); Equal(before,x.Bytes());
    }
    private static void SessionEnemy()
    {
        var x=new SessionFixture(); x.ToEnemy(); Equal(BattleSessionPhase.EnemyInput,x.Session.Read().Phase); string before=x.Bytes(); x.Session.Advance(); Equal(before,x.Bytes());
        Equal(BattleSessionPhase.AdvanceRequired,x.Session.Advance(Seen()).Phase); Equal(80,x.Flow.ReadBattle().Health.GetState("a").CurrentHealth); Equal(1L,x.Flow.ReadBattle().GetActivationCount("b"));
    }
    private static void SessionNpc()
    {
        var x=new SessionFixture(enemy:false); x.ToEnemy(); Equal("b",x.Session.Read().Activation.UnitId); Equal(BattleSessionPhase.AdvanceRequired,x.Session.Read().Phase); x.Session.Advance(); Equal<ActivationView>(null,x.Session.Read().Activation); Equal(100,x.Flow.ReadBattle().Health.GetState("a").CurrentHealth);
    }
    private static void FinishSessionRound(SessionFixture x)
    { x.Session.EndPlayerTurn(x.Id); x.Session.Advance(); x.Session.Advance(Seen()); x.Session.Advance(); x.Session.Advance(); }
    private static void SessionRounds()
    {
        var x=new SessionFixture(); FinishSessionRound(x); Equal(true,x.Flow.ReadBattle().IsRoundComplete); x.Session.Advance(); Equal(1,x.Flow.ReadBattle().Mission.GetObjective("primary").Progress);
        x.Reopen(); x.Session.Advance(); Equal(2,x.Session.Read().Round); Equal(1,x.Flow.ReadBattle().Mission.GetObjective("primary").Progress); x.Session.Advance(); Equal(BattleSessionPhase.PlayerInput,x.Session.Read().Phase);
    }
    private static void SessionUnstarted()
    {
        var x=new SessionFixture(started:false); Equal(0,x.Session.Read().Round); x.Session.Advance(); Equal(1,x.Session.Read().Round); Equal<ActivationView>(null,x.Session.Read().Activation); x.Session.Advance(); Equal(BattleSessionPhase.PlayerInput,x.Session.Read().Phase);
    }
    private static void SessionVictory()
    {
        var x=new SessionFixture(rounds:0,enemyHp:20); x.Session.TryUseAbility(x.Id,AbilitySlot.NormalAttack,"b",out _,out _,out _); Equal(BattleSessionPhase.Results,x.Session.Read().Phase); string before=x.Bytes(); x.Session.Advance(Seen()); Equal(before,x.Bytes()); Equal(0L,x.Flow.ReadBattle().GetActivationCount("b"));
    }
    private static void SessionDefeat()
    { var x=new SessionFixture(playerHp:10); x.ToEnemy(); x.Session.Advance(Seen()); Equal(MissionOutcome.Defeat,x.Session.Read().Result.Outcome); Equal(true,x.Flow.Progress.Owns("a")); Equal(0L,x.Flow.ReadBattle().GetActivationCount("c")); }
    private static void SessionInteract()
    {
        var x=new SessionFixture(stabilize:true); Equal(true,x.Session.TryInteract(x.Id,"primary",out _)); x.Flow.ClaimRewards(); x.Flow.ReturnToSelection(); Equal(BattleSessionPhase.Selection,x.Session.Advance().Phase); Equal(1,x.Flow.Progress.Claims.Count);
    }
    private static void SessionResume()
    {
        var x=new SessionFixture(); long id=x.Id; x.Session.TryUseAbility(id,AbilitySlot.NormalAttack,"b",out _,out _,out _); x.Reopen(); Equal(id,x.Id); Equal(false,x.Session.Read().Activation.PrimaryActionAvailable); Equal(false,x.Session.TryUseAbility(id,AbilitySlot.NormalAttack,"b",out _,out _,out _));
    }
    private static void SessionEnemyResume()
    { var x=new SessionFixture(); x.ToEnemy(); x.Session.Advance(Seen()); x.Reopen(); x.Session.Advance(Seen()); Equal(80,x.Flow.ReadBattle().Health.GetState("a").CurrentHealth); Equal("c",x.Session.Read().Activation.UnitId); }
    private static void SessionTorn()
    {
        var x=new SessionFixture(); x.ToEnemy(); x.Files.Fault=2; Throws<IOException>(()=>x.Session.Advance(Seen())); Equal(true,x.Flow.NeedsReload); x.Files.Fault=0; x.Reopen(); Equal(BattleSessionPhase.EnemyInput,x.Session.Read().Phase); Equal(100,x.Flow.ReadBattle().Health.GetState("a").CurrentHealth); x.Session.Advance(Seen()); Equal(80,x.Flow.ReadBattle().Health.GetState("a").CurrentHealth);
    }
    private static void SessionAmbiguous()
    {
        var x=new SessionFixture(); x.ToEnemy(); x.Files.Fault=3; Throws<IOException>(()=>x.Session.Advance(Seen())); Throws<InvalidOperationException>(()=>x.Session.Read()); x.Files.Fault=0; x.Reopen(); Equal<ActivationView>(null,x.Session.Read().Activation); Equal(80,x.Flow.ReadBattle().Health.GetState("a").CurrentHealth);
    }
    private static void SessionStatuses()
    {
        var x=new SessionFixture(status:true); Equal(1,x.Flow.ReadBattle().Statuses.GetStatuses("a").Single().Stacks); x.Session.EndPlayerTurn(x.Id); x.Reopen(); Equal(1,x.Flow.ReadBattle().Statuses.GetStatuses("a").Single().RemainingOwnerActivations); x.Session.Advance(); Equal(1,x.Flow.ReadBattle().Statuses.GetStatuses("a").Single().Stacks);
    }
    private static void SessionDetached()
    {
        var a=new[] { "a" }; var p=new EnemyPerception(a,Array.Empty<string>()); a[0]="c"; Equal("a",p.Opponents[0]); Throws<ArgumentException>(()=>new EnemyPerception(Enumerable.Repeat("a",9),Array.Empty<string>()));
        var x=new SessionFixture(); var view=x.Session.Read(); x.Session.EndPlayerTurn(x.Id); Equal(BattleSessionPhase.PlayerInput,view.Phase); Equal(true,view.Activation.PrimaryActionAvailable);
    }
    private static void SessionConflict()
    { var x=new SessionFixture(conflict:true); Throws<InvalidOperationException>(()=>x.Session.Read()); }
    private static void SessionAttempt()
    {
        var x=new SessionFixture(stabilize:true); long id=x.Id; x.Session.TryInteract(id,"primary",out _); x.Flow.ClaimRewards(); x.Flow.ReturnToSelection(); x.Flow.Start("session-m",new[] { "a" });
        Throws<InvalidOperationException>(()=>x.Session.TryInteract(id,"primary",out _)); Equal(MissionFlowPhase.Battle,x.Flow.Phase); x.Session=new BattleSession(x.Flow); Equal(BattleSessionPhase.PlayerInput,x.Session.Read().Phase);
    }
    private static void SessionEncounter()
    {
        var x=new SessionFixture(rounds:2);
        for(int steps=0;steps<30 && x.Session.Read().Phase!=BattleSessionPhase.Results;steps++)
        { if(x.Session.Read().Phase==BattleSessionPhase.PlayerInput) x.Session.EndPlayerTurn(x.Id); else x.Session.Advance(Seen()); }
        Equal(MissionOutcome.Victory,x.Session.Read().Result.Outcome); Equal(2,x.Session.Read().Round); Equal(60,x.Flow.ReadBattle().Health.GetState("a").CurrentHealth);
    }
}
