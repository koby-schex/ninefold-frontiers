# Touch Combat Clarity 01 — PT-065

## Why this pass

User PT-064 playtest found noticeable improvements in the battlefield but excessive floating health badges, redundant Attack/Passive buttons, insufficient single-tap unit inspection, generic visual treatment of battle buttons, and different Home vs Campaigns/Units/Settings bottom-nav heights. The goal is a cohesive, modern touch-first portrait tactics interface, **not** more permanent buttons.

## Implemented

### Battle information without floating HP clutter
- The presentation-owned `PlaytestBattlefield` renderer no longer attaches or lays out per-unit health overlays above units. It retains only selection/target rings, live objective markers and short damage/healing feedback.
- The **active unit's HP value, proportional meter, armor, remaining movement and action readiness** sit above the ability strip in a dedicated stable lower HUD.
- Tapping any eligible unit once, enemy **or** friendly, uses the core's **inspect-only** action. It highlights the unit on the battlefield and adds a small lower-HUD inspection line with team, name, live HP/maximum, effective armor and normal attack power/range if present. This is read-only: inspection cannot consume a turn or spend resources.
- The current unit is already visible at all times. Inspecting that unit also opens the detailed attack line; different units appear underneath it.
- Pending attacks/abilities still show the authoritative health-before/after preview in the same lower HUD. No gameplay accuracy, cover mitigation, health or armor rules change.

### Normal attack gesture
- The **ATTACK** button is completely removed.
- First tap on any friendly/enemy unit = **inspect and highlight only**.
- If that unit is an enemy, a second tap of that same enemy within **0.65 seconds** during a player activation, when the NormalAttack is actually available, selects the trusted NormalAttack slot, requests its validated target preview and commits that **same** preview through the existing core transaction. A first tap never attacks. A second tap on a different unit, an ally, an unavailable normal attack or outside the timing window does not attack.
- Tapping the ground still legally previews and commits normal movement; it resets the recent inspect gesture. Attacking/ending a turn also resets it. These taps retain the old authoritative no-miss/damage, obstacle, action-cost, initiative and save behavior.
- Main/Signature **ABILITY / ULT** buttons remain available for explicit special-ability selection with existing target confirmation, exactly as before. Their selection resets the double-tap gesture.
- Deliberate interaction with gold mission objectives remains a distinct core action. Mission 003 rescue-marker prioritization remains intact.

### Passive and future canonical info
- The large PASSIVE button becomes a small accessible `ⓘ` info control in a 47px touch target. It opens the existing unit detail overlay, showing unit info, attack readiness and passive status. It is not a passive *activation* button.
- In the present `AbstractFixture` kit, the passive may be a **placeholder with no authored effect**; the panel explicitly says so. It never invents a canonical ability description or says an unsupported passive can be activated.
- No 3D model art, unit prefab, rig, ability-content canon or generated animation is changed. The eventual Work chat models can replace the temporary pawn renderers without changing how the lower HUD reads health.

### Modern touch controls
- New last-loaded `Home/TouchCombatClarity01.uss` + tracked `.meta` gives abilities a stronger modern dark jade/bronze beveled-border look, more coherent selected/disabled/touch states, softer rounded corners, an icon-like passive information button, and a compact, high-contrast active/inspected unit status surface.
- The **Home and all other main-screen bottom tabs are given the same 64px outer frame and 56px tap targets**, matching icon, label, padding, border and active-underline geometry. No change to Home/Campaigns/Units/Settings peer navigation or their shared sanctuary atmosphere.
- No new permanent scrolling controls, draggable scrollbars, or camera view overlays.

### Safety and source boundaries
- Only `FrontiersPlaytest.cs`, `PlaytestBattlefield.cs`, the new USS/meta, `PlaytestLauncher.cs`, `PlaytestRevision.cs` and this doc change.
- No battle engine, simulation, content catalog, save format, shard balance, campaign data, three-vs-five squad limits, mission layouts, World Collision sizing or Work-owned model-generation files changed.
- The source milestone is **PT-065**. Automated Windows/Linux .NET tests still cover the unchanged gameplay core, but GitHub Actions does **not** compile the Unity UI presentation or certify on-device touch/visual quality.

## Unity manual acceptance checklist

1. Pull PR after merge with Play mode stopped; reopen in Unity 6.3 LTS, confirm PT-065 and **zero C# compile or UI Toolkit stylesheet warnings**.
2. Compare Home/Campaigns/Units/Settings bottom tab bars at 540×960, 390×844, 320×568. Heights, selected underline, padding, icon centers and label baselines must be identical. All four screens must remain proper peer-tab destinations.
3. Open any standard battle and verify **no numeric/health bars hover above any unit**. Only minimal selection and damage cues should remain. Active HP, health meter, armor, move and action readiness should appear directly above the ability controls. There is **no ATTACK button**.
4. Tap any enemy **once**: expect highlight and lower enemy info (HP, armor, ATK/RNG). No damage, action spend or unintended turn advance. Tap a different enemy or ally once: info changes; no attack.
5. Double-tap the same valid enemy (two taps less than approximately 0.65s apart): expect **exactly one** validated normal attack, with correct damage/action consumption and saved checkpoint. Repeat out of attack range, through blocked line-of-sight and when action already used; none should accidentally deal damage. If an attack is invalid, existing rejection guidance should explain why.
6. Tap ally once, then again: expect inspection only and never friendly-fire normal attack. Tap enemy slowly twice with a long pause: expect inspection only.
7. Tap ground between unit taps: movement follows existing limits, partial legal movement and obstacles. Ensure a previous target cannot be accidentally attacked afterwards.
8. Select ABILITY or ULT, tap valid target once: special-action preview, no premature spending; target again or Confirm commits once. Cancel preserves charges. Confirm the buttons' new active/disabled/pressed styles.
9. Tap small **ⓘ** info button: overlay opens and provides current active-unit info. If passive is a prototype placeholder, it clearly says it has no authored effect. Close and continue battle; battle state is unchanged.
10. Inspect a friendly then enemy during saved/read-only browsing and after turn advancement. Verify the selected ring follows the current inspect target, disabled enemy turns don't let the player attack, and pending combat preview doesn't silently disappear.
11. Resume an old checkpoint; check accurate live HP after movement, damage, healing and saved reload. Complete missions including rescue, protection, secure and survive; check mission objective/radar and rewards unaffected.
12. Take matched screenshots of bottom navigation on Home vs Units and a battle with the active-vitals / inspected-enemy strip. Please report any styling contrast, touch drift, small-font or clipping at narrow phone widths.

## Known limits

- An immediate verified double-tap normal attack is a deliberate UX change: it removes the usual preview pause. It still uses the validated authoritative preview and commit token under the hood. If touch testing reveals accidental double-taps, the gesture timing and targeting radius should be tuned rather than adding an ATTACK button back automatically.
- Prototype unit identities and passive abilities remain abstract. Production art/names/ability descriptions require canonical approval and the other Work chat's high-quality model/animation pipeline.
- The dock is deliberately slightly taller (about 160–169 UI units) to fit the vital information; in battle it replaces floating HP bars, not total screen readability. The 320×568 view requires careful manual review before locking any final mobile layout.
