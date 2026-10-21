# Game-Wide UI/UX Consistency 02

A final UI Toolkit stylesheet aligns title gutters, compact menu spacing, contextual header minimum height, and main-menu card rhythm. The shared bottom navigation retains its established height and does not shrink in short screens. Existing battle HUD and gameplay code are untouched.

## Manual verification

In Unity 6.3 LTS, check USS import warnings and visually compare Home, Campaigns, Units, Settings, Mission Briefing, and Results at 540x960, 390x844 and 320x568. Check scrolling, button clipping, back navigation, and bottom-tab dimensions. Verify the battle dock and double-tap attacks remain unchanged. Core .NET CI does not test Unity rendering.

No 3D assets, models, animations, saves, or gameplay systems are changed.