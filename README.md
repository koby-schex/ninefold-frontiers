# Ninefold: Frontiers

Unity mobile tactical game foundation with engine-independent turns, ability availability, health-effect resolution, and battlefield validation.
No playable missions, production art, saves, purchases or iOS distribution yet.

- Unity **6.3 LTS / 6000.3.21f1**; URP **17.3.0**.
- Development: Windows. Initial mobile platform: iPhone; Android later.
- iPhone 14 is one test device, not the minimum or only supported environment.
- Work lands through pull requests; Koby merges.

## Develop without opening Unity

The combat turn controller compiles against .NET Standard 2.1. A standalone .NET 8
executable runs behavioral tests against the exact source used by Unity. GitHub
Actions runs these checks on Windows and Linux; no Unity installation/license is
needed for the Core checks. Unity rendering/import/device validation remains deferred.

```sh
dotnet run --project Tests/Ninefold.Core.Tests/Ninefold.Core.Tests.csproj --configuration Release
```

See [the turn-system contract](Docs/CombatTurns.md) and
[ability-use contract](Docs/AbilityUse.md), and
[health-resolution contract](Docs/HealthResolution.md), and
[battlefield contract](Docs/Battlefield.md), and
[destination pathfinding](Docs/Pathfinding.md), and
[mission objectives](Docs/Missions.md). Clone/pull and review PRs as
normal. The following Editor steps can wait until we are ready for integration.

## Open the project later

1. Clone this repository with Git LFS installed and run `git lfs pull`.
2. In Unity Hub, add the cloned repository root and open with **6000.3.21f1**.
3. Let Package Manager finish restoring packages and scripts compile.
4. Run **Ninefold > Setup > Configure Foundation** once. This explicitly creates
   the URP renderer/pipeline, configures portrait defaults and creates an empty
   `Bootstrap` scene with camera/light. It is not a game demonstration.
5. Follow [the import checklist](Docs/Setup/Windows.md) and submit the generated
   settings, package lock and rendering assets in a follow-up PR.

Unity is not installed in the authoring environment used for this scaffold. Editor
import, compilation and rendering are **pending**, not claimed as passing. Do not
use this commit as a cloud build baseline until the first-import checklist passes.

## Project map

| Path | Purpose |
|---|---|
| `Assets/Ninefold/Runtime/Core` | Engine-independent rules boundary |
| `Assets/Ninefold/Runtime/Presentation` | Unity views, effects and interaction |
| `Assets/Ninefold/Editor` | Explicit project setup tooling |
| `Assets/Ninefold/Content` | Future mission, unit and ability definitions |
| `Assets/Ninefold/Settings` | Generated, then versioned URP assets |
| `Assets/Ninefold/Scenes` | Generated bootstrap; future game scenes |
| `SourceArt` | Editable production sources; large binary files use LFS |
| `BuildTools` | Repository validation; future build automation |
| `Docs` | Setup, architecture, roadmap, decisions and canon policy |
| `Tests` | Automated Core behavioral tests |

See [FP-01 roadmap](Docs/Production/FP01.md), [architecture](Docs/Architecture.md),
[canon policy](Docs/Canon/README.md), and [cloud iOS plan](Docs/Setup/iOS.md).

The Project's current Master Lore & World-Building Archive governs all Ninefold
content. This public scaffold does not redistribute unpublished lore or visual masters.
