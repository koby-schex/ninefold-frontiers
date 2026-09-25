# Ninefold: Frontiers

Unity mobile tactical game foundation. **Setup scaffold only:** no playable missions,
production art, saves, purchases, or iOS distribution are implemented in this commit.

- Unity **6.3 LTS / 6000.3.21f1**; URP **17.3.0**.
- Development: Windows. Initial mobile platform: iPhone; Android later.
- iPhone 14 is one test device, not the minimum or only supported environment.
- Work lands through pull requests; Koby merges.

## Open the project

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
| `Tests` | Future verification plan; no gameplay tests yet |

See [FP-01 roadmap](Docs/Production/FP01.md), [architecture](Docs/Architecture.md),
[canon policy](Docs/Canon/README.md), and [cloud iOS plan](Docs/Setup/iOS.md).

The Project's current Master Lore & World-Building Archive governs all Ninefold
content. This public scaffold does not redistribute unpublished lore or visual masters.
