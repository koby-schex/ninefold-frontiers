# Presentation

Unity presentation code depends on Core, never the reverse.

`FrontiersPlaytest` owns one `MissionPresentation` and its cached `BattleInteraction`.
It refreshes immutable views after input/checkpoint changes, not every frame. UI Toolkit
handles buttons and a separate battlefield pointer surface. The host exposes fixture
mission selection, squad preparation, battle controls, results and collection upgrades.

`PlaytestBattlefield` renders temporary 3D tokens, movement previews and all living
units' health overlays. Effects consume post-save events; they cannot mutate simulation.
It reconciles after playback and on reload. Reduced motion still acknowledges the
exact animation token. Player input has no time limit; scheduler steps run only when
the core reports a non-player phase and no animation is pending.

`PlaytestSetup` is an explicit Editor menu command that generates an integration scene,
panel settings and a referenced URP material. No startup hook overwrites scenes.
The default UI theme is referenced explicitly, and the UI is transparent over the camera.

This is an abstract fixture host, not final UI, art, game balance or campaign content.
See `Docs/UnityPlaytest.md` for setup, controls, save location, validation and known limits.
