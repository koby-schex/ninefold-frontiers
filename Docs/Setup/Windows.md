# Windows setup

Use Unity Hub and Unity Editor **6000.3.21f1**. Do not silently upgrade the project.
Use Git with Git LFS (or GitHub Desktop), and a C# editor of your choice. Python 3
is optional locally for the repository validator; the GitHub check installs it.

Clone the repository, run `git lfs install` and `git lfs pull`, then add the clone
root through Hub. Do not create a nested project or overwrite this checkout with
a new template. Open with the pinned Editor and let packages resolve.

## First-import checklist

- Package Manager resolves URP 17.3.0 and its dependencies without errors.
- Console contains no script compilation errors.
- Run **Ninefold > Setup > Configure Foundation**; check its completion log.
- Confirm Project Settings: product title, portrait orientation, linear color,
  text serialization, visible meta files, and assigned URP pipeline.
- Confirm `Assets/Ninefold/Scenes/Bootstrap.unity` contains only a camera and light.
  No game interaction is expected. Verify the scene is in the build scene list.
- Run setup again: no duplicate scene/renderer/pipeline assets, no new GUIDs.
- Close and reopen: no compile or missing-reference errors.
- Review and commit generated `ProjectSettings`, `Packages/packages-lock.json`,
  `Assets/Ninefold/Settings` assets and Bootstrap scene with their `.meta` files.
  Do not commit Library, Temp, Logs, UserSettings or your credentials.
- In the follow-up PR record Editor version, OS, package resolution and any warnings.

The scaffold deliberately leaves dependency-lock resolution and complete serialized
settings to the pinned Editor. Do not fabricate a lockfile or claim an import passed
without running Unity. Once the first import is committed, a fresh clone must open
without needing the setup menu. The menu does not run automatically on launch.

Use a portrait Game view (9:16 is a starting preview, not a device safe-area test).
Real-device and other-aspect-ratio checks remain required. iPhone 14 is an initial
test device; Windows Editor success does not prove iPhone rendering or memory use.

Local scaffold check from the repository root:

```powershell
python BuildTools/validate_repository.py
```

Reference: https://unity.com/releases/editor/whats-new/6000.3.21f1
