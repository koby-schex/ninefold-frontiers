# World Five Mission 11 — live Core battle journey regression

Two new registered tests exercise the World Five boundary and actual battle interaction flow.

**Production 14-mission catalog:** New profiles start with Mission 1 available. Mission 11 remains locked before the tutorial's Mission 10 prerequisite is cleared, even after reopening the profile. An attempted early deployment is rejected.

**Isolated test-only single-encounter harness:** The exact production Mission 11 map, NPC templates, objectives, ability/status definitions and squad units are run through MissionPresentation and BattleInteraction. The harness changes only the campaign prerequisite list so that the combat encounter can be regression-tested without spending ten minutes replaying the tutorial on every CI invocation. Normal unit attacks use select, double-tap preview/confirm, and legal movement samples. A saved, active battle is reopened; victory is followed by one real reward claim, duplicate claim detection and a fresh selection reload.

To keep this deterministic, enemies receive empty perception during this particular victory-path test and therefore cannot attack unseen player units. This validates actual authored player-side combat and persistence, **not** the intended Ashbound AI difficulty or a complete 1–14 uninterrupted win streak. Live Unity rendering/controls remain a separate manual verification requirement.

A later end-to-end certification pass should replay tutorial objectives against the integrated wildlife catalog, exercise enemy-perception combat with defeat/retry, and track gameplay balance.
