using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;

internal static partial class Program
{
    private static (string Name, Action Run)[] PathfindingTests() => new (string, Action)[]
    {
        ("Destination routing retains exact free position without spending", RouteFreeDestination),
        ("Automatic detour clears every segment of a wall", RouteWall),
        ("Routing accounts for living ally and enemy footprints", RouteUnits),
        ("Narrow passage admits only a fitting body", RouteClearance),
        ("Map-spanning wall reports no path atomically", RouteDisconnected),
        ("Occupied and out-of-map destinations reject", RouteIllegalDestination),
        ("Straight-line and detour budgets reject without spending", RouteBudget),
        ("Expensive ground makes a longer route cheaper", RouteTerrain),
        ("Small terrain penalty can favor crossing instead of detouring", RouteTerrainCrossing),
        ("Repeated equal-cost searches are deterministic", RouteDeterminism),
        ("Map enumeration order does not change route", RouteMapOrder),
        ("Confirmation replans when a reinforcement blocks old route", RouteRevalidation),
        ("Defeat releases a shorter direct route", RouteDefeated),
        ("Destination moves retain split action budgets", RouteSplit),
        ("Search limit is distinct from unreachable", RouteLimits),
        ("Stale ended and missing-position searches preserve state", RouteLifecycle),
        ("No-op and elevation-changing destinations reject", RouteInvalidInput),
        ("Routing permits border destinations and ignores overhead walls", RouteEdges)
    };
    private static MovementPreview FindRoute(BattleTurnController b, FieldPoint target)
    {
        var turn = b.CurrentActivation; var position = b.Battlefield.GetPosition(turn.UnitId);
        Equal(true,b.Battlefield.TryFindPath(turn.ActivationId,target,out var route,out var failure));
        Equal(FieldFailure.None,failure); Equal(position,b.Battlefield.GetPosition(turn.UnitId));
        Equal(turn.MovementRemaining,b.CurrentActivation.MovementRemaining); Equal(turn.PrimaryActionAvailable,b.CurrentActivation.PrimaryActionAvailable);
        Equal(target,route.Path[route.Path.Count-1]);
        Equal(true,b.Battlefield.TryPreviewMovement(turn.ActivationId,route.Path,out var checkedRoute,out _));
        Equal(route.Cost,checkedRoute.Cost); return route;
    }
    private static void RejectRoute(BattleTurnController b, FieldPoint target, FieldFailure expected, int limit = 256)
    {
        var turn = b.CurrentActivation; var position = b.Battlefield.GetPosition(turn.UnitId);
        Equal(false,b.Battlefield.TryMoveTo(turn.ActivationId,target,out var result,out var failure,limit));
        Equal<MovementPreview>(null,result); Equal(expected,failure); Equal(position,b.Battlefield.GetPosition(turn.UnitId));
        Equal(turn.MovementRemaining,b.CurrentActivation.MovementRemaining); Equal(turn.PrimaryActionAvailable,b.CurrentActivation.PrimaryActionAvailable);
        Equal(0,b.Abilities.GetState(turn.UnitId).MainCooldownRemaining); Equal(false,b.Abilities.GetState(turn.UnitId).SignatureUsed);
    }
    private static MovementPreview MoveTo(BattleTurnController b, FieldPoint target)
    {
        Equal(true,b.Battlefield.TryMoveTo(b.CurrentActivation.ActivationId,target,out var result,out var failure));
        Equal(FieldFailure.None,failure); Equal(target,b.Battlefield.GetPosition(b.CurrentActivation.UnitId)); return result;
    }
    private static void RouteFreeDestination()
    {
        var b = FieldBattle(); var route = FindRoute(b,P(1.123456m,0));
        Equal(1,route.Path.Count); Equal(1.123456m,route.Cost); Equal(route.Cost,MoveTo(b,P(1.123456m,0)).Cost);
    }
    private static void RouteWall()
    {
        var b = FieldBattle(new[] { Wall() }); var route = FindRoute(b,P(4,0));
        Equal(true,route.Path.Count > 1); Equal(true,route.Cost > 4m && route.Cost < 8m);
        Equal(route.Cost,MoveTo(b,P(4,0)).Cost); Equal(20m-route.Cost,b.CurrentActivation.MovementRemaining);
    }
    private static void RouteUnits()
    {
        foreach (var target in new[] { P(8,0),P(0,8) })
        {
            var b = FieldBattle(); var route = FindRoute(b,target);
            Equal(true,route.Path.Count > 1); Equal(true,route.Cost > 8m); MoveTo(b,target);
        }
    }
    private static void RouteClearance()
    {
        var walls = new[] { new FieldObstacle(Box(2,.6m,3,20),true,false),new FieldObstacle(Box(2,-20,3,-.6m),true,false) };
        Equal(1,FindRoute(FieldBattle(walls),P(4,0)).Path.Count);
        RejectRoute(FieldBattle(walls,body:Body(.75m)),P(4,0),FieldFailure.PathNotFound);
    }
    private static void RouteDisconnected()
    {
        var b = FieldBattle(new[] { new FieldObstacle(Box(2,-20,3,20),true,false) });
        RejectRoute(b,P(4,0),FieldFailure.PathNotFound);
    }
    private static void RouteIllegalDestination()
    {
        var b = FieldBattle(); RejectRoute(b,P(6,0),FieldFailure.IllegalDestination);
        RejectRoute(b,P(20,0),FieldFailure.IllegalDestination);
    }
    private static void RouteBudget()
    {
        RejectRoute(FieldBattle(budget:2),P(3,0),FieldFailure.InsufficientMovement);
        RejectRoute(FieldBattle(new[] { Wall() },budget:4),P(4,0),FieldFailure.InsufficientMovement);
        Equal(2m,MoveTo(FieldBattle(budget:2),P(2,0)).Cost);
        RejectRoute(FieldBattle(budget:0),P(.000001m,0),FieldFailure.InsufficientMovement);
    }
    private static void RouteTerrain()
    {
        var b = FieldBattle(ground:new[] { new DifficultGround(Box(1,-1,3,1),10m) },budget:8);
        var route = FindRoute(b,P(4,0)); Equal(true,route.Path.Count > 1); Equal(true,route.Cost < 8m);
        Equal(route.Cost,MoveTo(b,P(4,0)).Cost);
    }
    private static void RouteTerrainCrossing()
    {
        var b = FieldBattle(ground:new[] { new DifficultGround(Box(1,-1,3,1),1.1m) });
        var route = FindRoute(b,P(4,0)); Equal(1,route.Path.Count); Equal(4.25m,route.Cost);
    }
    private static void SameRoute(MovementPreview a, MovementPreview b)
    {
        Equal(a.Cost,b.Cost); Equal(a.Path.Count,b.Path.Count);
        for (int i=0;i<a.Path.Count;i++) Equal(a.Path[i],b.Path[i]);
    }
    private static void RouteDeterminism()
    {
        var b = FieldBattle(new[] { Wall() }); var first = FindRoute(b,P(4,0));
        for (int i=0;i<8;i++) SameRoute(first,FindRoute(b,P(4,0)));
    }
    private static void RouteMapOrder()
    {
        var walls = new[] { Wall(), new FieldObstacle(Box(-5,-4,-4,-3),true,false) };
        SameRoute(FindRoute(FieldBattle(walls),P(4,0)),FindRoute(FieldBattle(walls.Reverse().ToArray()),P(4,0)));
    }
    private static void RouteRevalidation()
    {
        var b = FieldBattle(); Equal(1,FindRoute(b,P(4,0)).Path.Count);
        b.RegisterUnit(U("new")); b.Battlefield.Register("new",P(2,0),Body(),Profiles());
        Equal(true,MoveTo(b,P(4,0)).Path.Count > 1);
        var occupied = FieldBattle(); FindRoute(occupied,P(4,0)); occupied.RegisterUnit(U("new"));
        occupied.Battlefield.Register("new",P(4,0),Body(),Profiles()); RejectRoute(occupied,P(4,0),FieldFailure.IllegalDestination);
    }
    private static void RouteDefeated()
    {
        var b = FieldBattle(); Equal(true,FindRoute(b,P(8,0)).Path.Count > 1);
        FieldHit(b,AbilitySlot.Signature); var after = FindRoute(b,P(8,0)); Equal(1,after.Path.Count); Equal(8m,after.Cost);
    }
    private static void RouteSplit()
    {
        var b = FieldBattle(); MoveTo(b,P(1,0)); FieldHit(b); MoveTo(b,P(2,0));
        Equal(18m,b.CurrentActivation.MovementRemaining); Equal(false,b.CurrentActivation.PrimaryActionAvailable);
    }
    private static void RouteLimits()
    {
        var b = FieldBattle(new[] { Wall() }); RejectRoute(b,P(4,0),FieldFailure.SearchLimitExceeded,2);
        RejectRoute(b,P(4,0),FieldFailure.InvalidRequest,1); RejectRoute(b,P(4,0),FieldFailure.InvalidRequest,257);
        FindRoute(b,P(4,0));
        Equal(true,FieldBattle().Battlefield.TryFindPath(1,P(1,0),out _,out _,2));
    }
    private static void RouteLifecycle()
    {
        var b = FieldBattle(); long old = b.CurrentActivation.ActivationId; b.EndActivation(old); b.BeginNextActivation();
        Equal(false,b.Battlefield.TryMoveTo(old,P(2,2),out _,out var failure)); Equal(FieldFailure.InactiveTurn,failure);
        b.EndBattle(); Equal(false,b.Battlefield.TryFindPath(old,P(2,2),out _,out failure)); Equal(FieldFailure.InactiveTurn,failure);
        var empty = B(U("a")); var field = empty.ConfigureBattlefield(new BattlefieldMap(Box(-10,-10,10,10),Array.Empty<FieldObstacle>()));
        empty.StartNextRound(); empty.BeginNextActivation();
        Equal(false,field.TryFindPath(1,P(2,2),out _,out failure)); Equal(FieldFailure.MissingPosition,failure);
    }
    private static void RouteInvalidInput()
    {
        var b = FieldBattle(); RejectRoute(b,P(0,0),FieldFailure.InvalidRequest);
        RejectRoute(b,P(1,0,1),FieldFailure.UnsupportedElevation);
    }
    private static void RouteEdges()
    {
        var overhead = new FieldObstacle(new FieldBox(P(2,-2,3),P(3,2,4)),true,false);
        var b = FieldBattle(new[] { overhead },budget:30); Equal(1,FindRoute(b,P(4,0)).Path.Count);
        Equal(19.75m,MoveTo(b,P(0,-19.75m)).Cost);
    }
}
