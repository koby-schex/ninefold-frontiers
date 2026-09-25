# Verification

With .NET SDK 8 installed, from the repository root:

```sh
dotnet run --project Tests/Ninefold.Core.Tests/Ninefold.Core.Tests.csproj --configuration Release
```

This dependency-free executable compiles the actual Core C# files, runs 45 named
behavioral scenarios and returns nonzero on failure. It is intentionally run with
`dotnet run`, not `dotnet test` (no test SDK/framework dependency). CI runs it on
Windows and Linux. The library targets .NET Standard 2.1 with C# 9; .NET 8 is the
external test host, not a change to Unity's runtime or a DLL imported into Assets.

Coverage: fixed round order and deterministic ties, one activation per unit,
deferred initiative and reinforcements, split movement, single action, invalid
commands, stale activation IDs, resource reset, removal, terminal battle state,
immutable views and repeated rounds. IDs/stats in tests are abstract fixtures,
not canonical unit stats. Ability scenarios additionally verify owner-turn cooldowns, readiness triggers,
Signature use limits, preview purity, kit registration and rejected-use atomicity.
Stun resolution, saves, effects, geometry and rendering remain unimplemented.

BuildTools/validate_repository.py checks repository integrity only. Unity import,
IL2CPP/iPhone and performance remain separate future checks.
