using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;

internal static partial class Program
{
    private static (string Name, Action Run)[] BattlefieldTests() => new (string, Action)[]
    {
        ("Free fractional destinations and pure movement preview", FreePosition),
        ("Movement splits around resolved attack", FieldSplitMove),
        ("Blocked segment cannot tunnel between legal endpoints", NoTunneling),
        ("Detour costs entire route and avoids wall", FieldDetour),
        ("Large footprints cannot squeeze through narrow clearance", BodyClearance),
        ("Occupied endpoints and crossing units block movement", FieldOccupancy),
        ("Defeated units release space without losing health record", DefeatedSpace),
        ("Boundary footprint and unsupported elevation rejected", FieldBounds),
        ("Invalid and oversized paths preserve resources", InvalidPaths),
        ("Insufficient movement is atomic", FieldBudget),
        ("Difficult ground charges footprint overlap only", TerrainCost),
        ("Overlapping ground uses strongest cost", TerrainOverlap),
        ("Diagonal paths cannot gain allowance by subdivision", DiagonalCost),
        ("Exact range boundary uses authored attack origin", AttackOrigin),
        ("Out of range Signature preserves use and primary action", RangeReject),
        ("Full obstruction rejects even when partial cover also exists", ShotBlock),
        ("Partial cover changes damage and is bypassed by repositioning", DirectionalCover),
        ("Low cover does not protect a higher target ray", TallRay),
        ("Non-cover effects ignore partial mitigation", CoverEligibility),
        ("Healing checks compatibility and team before cost", FieldHealing),
        ("Missing profiles and unknown targets fail safely", MissingFieldInputs),
        ("Stale and ended commands cannot mutate positions or effects", FieldLifecycle),
        ("Movement confirmation recalculates occupied space", MoveRevalidation),
        ("Effect confirmation recalculates range after movement", EffectRevalidation),
        ("Map data and movement snapshots are immutable", FieldSnapshots),
        ("Setup rejects replacement and illegal deployment", FieldSetup),
        ("Geometry inputs reject invalid extents and profiles", InvalidGeometry),
        ("Pending reinforcements block space before next round", FieldReinforcement)
    };
    private static FieldPoint P(decimal x, decimal z, decimal y = 0m) => new FieldPoint(x,y,z);
    private static FieldBox Box(decimal x1, decimal z1, decimal x2, decimal z2, decimal height = 5m)
        => new FieldBox(P(x1,z1),P(x2,z2,height));
    private static FieldBody Body(decimal width = .25m, decimal height = 2m)
        => new FieldBody(width,width,height,P(0,0,height/2m),P(0,0,height/2m));
    private static FieldAbility[] Profiles(decimal range = 10m, bool cover = true) => new[]
    {
        new FieldAbility(AbilitySlot.NormalAttack,HealthEffectKind.Damage,40,range,cover),
        new FieldAbility(AbilitySlot.Main,HealthEffectKind.Healing,30,range,false),
        new FieldAbility(AbilitySlot.Signature,HealthEffectKind.Damage,200,range,cover)
    };
    private static BattleTurnController FieldBattle(FieldObstacle[] walls = null, DifficultGround[] ground = null,
        decimal budget = 20m, decimal range = 10m, bool cover = true, FieldBody body = null,
        bool compatible = true, bool start = true)
    {
        var b = B(U("a",30,budget),U("b",20),U("c",10));
        foreach (var id in new[] { "a","b","c" })
        {
            b.Abilities.RegisterKit(id,new UnitAbilityDefinition(2,SignatureReadiness.ReadyAtDeployment));
            b.Health.RegisterHealth(id,new UnitHealthDefinition(id == "b" ? "enemy" : "ally",100,0),id == "c" ? 50 : 100);
        }
        var f = b.ConfigureBattlefield(new BattlefieldMap(Box(-20,-20,20,20,20),walls ?? Array.Empty<FieldObstacle>(),ground));
        f.Register("a",P(0,0),body ?? Body(),Profiles(range,cover),compatible ? new[] { "a","c" } : null);
        f.Register("b",P(6,0),Body(),Profiles()); f.Register("c",P(0,6),Body(),Profiles());
        if (start) { b.StartNextRound(); b.BeginNextActivation(); }
        return b;
    }
    private static MovementPreview Move(BattleTurnController b, params FieldPoint[] path)
    {
        Equal(true,b.Battlefield.TryMove(b.CurrentActivation.ActivationId,path,out var result,out var failure));
        Equal(FieldFailure.None,failure); return result;
    }
    private static void RejectMove(BattleTurnController b, FieldFailure expected, params FieldPoint[] path)
    {
        var turn = b.CurrentActivation; var pos = b.Battlefield.GetPosition(turn.UnitId);
        Equal(false,b.Battlefield.TryMove(turn.ActivationId,path,out var result,out var failure));
        Equal<MovementPreview>(null,result); Equal(expected,failure);
        Equal(pos,b.Battlefield.GetPosition(turn.UnitId)); Equal(turn.MovementRemaining,b.CurrentActivation.MovementRemaining);
        Equal(turn.PrimaryActionAvailable,b.CurrentActivation.PrimaryActionAvailable);
    }
    private static HealthEffectPreview FieldHit(BattleTurnController b, AbilitySlot slot = AbilitySlot.NormalAttack, string target = "b")
    {
        Equal(true,b.Battlefield.TryApplyEffect(b.CurrentActivation.ActivationId,slot,target,out var result,out var failure,out var health));
        Equal(FieldFailure.None,failure); Equal(HealthActionFailure.None,health); return result;
    }
    private static void RejectEffect(BattleTurnController b, FieldFailure expected, AbilitySlot slot = AbilitySlot.Signature,
        string target = "b", HealthActionFailure expectedHealth = HealthActionFailure.None)
    {
        var turn = b.CurrentActivation; var state = b.Abilities.GetState(turn.UnitId);
        Equal(false,b.Battlefield.TryApplyEffect(turn.ActivationId,slot,target,out var result,out var failure,out var health));
        Equal<HealthEffectPreview>(null,result); Equal(expected,failure); Equal(expectedHealth,health);
        Equal(turn.MovementRemaining,b.CurrentActivation.MovementRemaining); Equal(turn.PrimaryActionAvailable,b.CurrentActivation.PrimaryActionAvailable);
        Equal(state.SignatureUsed,b.Abilities.GetState(turn.UnitId).SignatureUsed);
        Equal(state.MainCooldownRemaining,b.Abilities.GetState(turn.UnitId).MainCooldownRemaining);
    }
    private static void FreePosition()
    {
        var b = FieldBattle(); var path = new[] { P(1.125m,0) };
        Equal(true,b.Battlefield.TryPreviewMovement(b.CurrentActivation.ActivationId,path,out var view,out _));
        Equal(1.125m,view.Cost); Equal(P(0,0),b.Battlefield.GetPosition("a")); Equal(20m,b.CurrentActivation.MovementRemaining);
        Move(b,path); Equal(P(1.125m,0),b.Battlefield.GetPosition("a"));
    }
    private static void FieldSplitMove()
    {
        var b = FieldBattle(); Move(b,P(1,0)); FieldHit(b); Move(b,P(2,0));
        Equal(18m,b.CurrentActivation.MovementRemaining); Equal(false,b.CurrentActivation.PrimaryActionAvailable);
    }
    private static FieldObstacle Wall(bool shots = true, decimal cover = 0m)
        => new FieldObstacle(Box(2,-1,3,1),true,shots,cover);
    private static void NoTunneling() { var b = FieldBattle(new[] { Wall() }); RejectMove(b,FieldFailure.BlockedPath,P(4,0)); }
    private static void FieldDetour()
    {
        var b = FieldBattle(new[] { Wall() }); var result = Move(b,P(0,2),P(4,2),P(4,0)); Equal(8m,result.Cost);
    }
    private static void BodyClearance()
    {
        var walls = new[] { new FieldObstacle(Box(2,.6m,3,4),true,false) };
        Move(FieldBattle(walls),P(4,0)); RejectMove(FieldBattle(walls,body:Body(.75m)),FieldFailure.BlockedPath,P(4,0));
        RejectMove(FieldBattle(walls,body:Body(.6m)),FieldFailure.BlockedPath,P(4,0));
    }
    private static void FieldOccupancy()
    {
        var b = FieldBattle(); RejectMove(b,FieldFailure.IllegalDestination,P(6,0)); RejectMove(b,FieldFailure.BlockedPath,P(8,0));
        RejectMove(b,FieldFailure.BlockedPath,P(0,8));
    }
    private static void DefeatedSpace()
    {
        var b = FieldBattle(); FieldHit(b,AbilitySlot.Signature); Move(b,P(6,0));
        Equal(true,b.Health.GetState("b").IsDefeated); Equal(false,b.IsUnitEligible("b"));
    }
    private static void FieldBounds()
    {
        var b = FieldBattle(budget:100); RejectMove(b,FieldFailure.IllegalDestination,P(19.9m,0));
        RejectMove(b,FieldFailure.UnsupportedElevation,P(1,0,1)); Move(b,P(0,-19.75m));
    }
    private static void InvalidPaths()
    {
        var b = FieldBattle(); RejectMove(b,FieldFailure.InvalidRequest,null); RejectMove(b,FieldFailure.InvalidRequest);
        RejectMove(b,FieldFailure.InvalidRequest,P(0,0));
        RejectMove(b,FieldFailure.InvalidRequest,Enumerable.Repeat(P(1,1),257).ToArray());
    }
    private static void FieldBudget() { var b = FieldBattle(budget:2); RejectMove(b,FieldFailure.InsufficientMovement,P(3,0)); Move(b,P(2,0)); }
    private static void TerrainCost()
    {
        var ground = new[] { new DifficultGround(Box(1,-1,3,1),2m) };
        var b = FieldBattle(ground:ground); Equal(6.5m,Move(b,P(4,0)).Cost);
    }
    private static void TerrainOverlap()
    {
        var b = FieldBattle(ground:new[] { new DifficultGround(Box(1,-1,3,1),2),new DifficultGround(Box(1,-1,3,1),3) });
        Equal(9m,Move(b,P(4,0)).Cost);
    }
    private static void DiagonalCost()
    {
        var a = FieldBattle(); var b = FieldBattle();
        var direct = Move(a,P(1,1)).Cost; var split = Move(b,P(.5m,.5m),P(1,1)).Cost;
        Equal(1.414214m,direct); Equal(true,split >= direct);
    }
    private static void AttackOrigin()
    {
        var body = new FieldBody(1,.25m,2,P(1,0,1),P(0,0,1));
        var b = FieldBattle(range:5,body:body); Equal(40,FieldHit(b).HealthChanged);
        RejectEffect(FieldBattle(range:4.999m,body:body),FieldFailure.OutOfRange);
    }
    private static void RangeReject() { RejectEffect(FieldBattle(range:5),FieldFailure.OutOfRange); }
    private static void ShotBlock()
    {
        var b = FieldBattle(new[] { new FieldObstacle(Box(1,-1,1.5m,1),false,false,.25m),Wall() });
        RejectEffect(b,FieldFailure.Obstructed); Equal(100,b.Health.GetState("b").CurrentHealth);
    }
    private static void DirectionalCover()
    {
        var walls = new[] { Wall(false,.25m) }; var b = FieldBattle(walls);
        Equal(true,b.Battlefield.TryPreviewEffect(b.CurrentActivation.ActivationId,AbilitySlot.NormalAttack,"b",out var preview,out _,out _));
        Equal(30,preview.HealthChanged); Equal(30,FieldHit(b).HealthChanged);
        var flank = FieldBattle(walls); Move(flank,P(0,3),P(6,3)); Equal(40,FieldHit(flank).HealthChanged);
    }
    private static void TallRay()
    {
        var b = FieldBattle(new[] { new FieldObstacle(Box(2,-1,3,1,.5m),true,false,.25m) });
        Equal(40,FieldHit(b).HealthChanged);
    }
    private static void CoverEligibility() { Equal(40,FieldHit(FieldBattle(new[] { Wall(false,.25m) },cover:false)).HealthChanged); }
    private static void FieldHealing()
    {
        var b = FieldBattle(); Equal(30,FieldHit(b,AbilitySlot.Main,"c").HealthChanged); Equal(2,b.Abilities.GetState("a").MainCooldownRemaining);
        RejectEffect(FieldBattle(compatible:false),FieldFailure.HealthRejected,AbilitySlot.Main,"c",HealthActionFailure.Incompatible);
        RejectEffect(FieldBattle(),FieldFailure.HealthRejected,AbilitySlot.Main,"b",HealthActionFailure.WrongTeam);
    }
    private static void MissingFieldInputs()
    {
        var b = FieldBattle(); RejectEffect(b,FieldFailure.InvalidTarget,target:"unknown"); RejectEffect(b,FieldFailure.InvalidTarget,target:null);
        RejectEffect(b,FieldFailure.InvalidRequest,AbilitySlot.Passive);
        b.RegisterUnit(U("missing")); b.Abilities.RegisterKit("missing",new UnitAbilityDefinition(2,SignatureReadiness.ReadyAtDeployment)); b.EndActivation(b.CurrentActivation.ActivationId); Drain(b); b.StartNextRound();
        while (b.BeginNextActivation().UnitId != "missing") b.EndActivation(b.CurrentActivation.ActivationId);
        Equal(false,b.Battlefield.TryMove(b.CurrentActivation.ActivationId,new[] { P(1,1) },out _,out var failure));
        Equal(FieldFailure.MissingPosition,failure); RejectEffect(b,FieldFailure.MissingPosition);
    }
    private static void FieldLifecycle()
    {
        var b = FieldBattle(); long old = b.CurrentActivation.ActivationId; b.EndActivation(old); b.BeginNextActivation();
        Equal(false,b.Battlefield.TryMove(old,new[] { P(2,2) },out _,out var failure)); Equal(FieldFailure.InactiveTurn,failure);
        Equal(false,b.Battlefield.TryApplyEffect(old,AbilitySlot.Signature,"b",out _,out failure,out _)); Equal(FieldFailure.InactiveTurn,failure);
        b.EndBattle(); Equal(false,b.Battlefield.TryMove(old,new[] { P(2,2) },out _,out failure)); Equal(FieldFailure.InactiveTurn,failure);
    }
    private static void MoveRevalidation()
    {
        var b = FieldBattle(); var path = new[] { P(2,0) };
        Equal(true,b.Battlefield.TryPreviewMovement(b.CurrentActivation.ActivationId,path,out _,out _));
        b.RegisterUnit(U("new")); b.Battlefield.Register("new",P(2,0),Body(),Profiles());
        RejectMove(b,FieldFailure.IllegalDestination,path);
    }
    private static void EffectRevalidation()
    {
        var b = FieldBattle(range:6); Equal(true,b.Battlefield.TryPreviewEffect(b.CurrentActivation.ActivationId,AbilitySlot.NormalAttack,"b",out _,out _,out _));
        Move(b,P(-1,0)); RejectEffect(b,FieldFailure.OutOfRange,AbilitySlot.NormalAttack);
    }
    private static void FieldSnapshots()
    {
        var walls = new[] { Wall() }; var b = FieldBattle(walls); walls[0] = Wall(false,.25m);
        RejectEffect(b,FieldFailure.Obstructed);
        var path = new[] { P(0,2) }; b.Battlefield.TryPreviewMovement(b.CurrentActivation.ActivationId,path,out var preview,out _);
        path[0] = P(0,3); Equal(P(0,2),preview.Path[0]);
        Throws<NotSupportedException>(() => ((IList<FieldPoint>)preview.Path)[0] = P(1,1));
    }
    private static void FieldSetup()
    {
        var b = FieldBattle(start:false); var f = b.Battlefield;
        Throws<InvalidOperationException>(() => b.ConfigureBattlefield(f.Map));
        Throws<InvalidOperationException>(() => f.Register("a",P(2,2),Body(),Profiles()));
        b.RegisterUnit(U("new")); Throws<ArgumentException>(() => f.Register("new",P(0,0),Body(),Profiles()));
        f.Register("new",P(2,2),Body(),Profiles());
        b.StartNextRound(); b.BeginNextActivation(); Throws<InvalidOperationException>(() => f.Register("a",P(3,3),Body(),Profiles()));
    }
    private static void InvalidGeometry()
    {
        Throws<ArgumentOutOfRangeException>(() => P(10001,0));
        Throws<ArgumentOutOfRangeException>(() => P(decimal.MinValue,0));
        Throws<ArgumentOutOfRangeException>(() => P(.0000001m,0));
        Throws<ArgumentException>(() => new FieldBox(P(0,0),P(0,0)));
        Throws<ArgumentOutOfRangeException>(() => Body(0));
        Throws<ArgumentException>(() => new FieldBody(1,1,1,P(2,0),P(0,0)));
        Throws<ArgumentOutOfRangeException>(() => new DifficultGround(Box(0,0,1,1),.9m));
        Throws<ArgumentOutOfRangeException>(() => new FieldObstacle(Box(0,0,1,1),false,false,2));
        Throws<ArgumentOutOfRangeException>(() => new FieldAbility(AbilitySlot.Passive,HealthEffectKind.Damage,1,2,false));
        Throws<ArgumentException>(() => new FieldAbility(AbilitySlot.Main,HealthEffectKind.Healing,1,2,true));
    }
    private static void FieldReinforcement()
    {
        var b = FieldBattle(); b.RegisterUnit(U("new",100)); b.Battlefield.Register("new",P(2,0),Body(),Profiles());
        RejectMove(b,FieldFailure.BlockedPath,P(4,0)); Equal(false,b.RoundOrder.Contains("new"));
    }
}
