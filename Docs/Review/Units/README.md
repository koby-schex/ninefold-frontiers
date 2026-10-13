# Squad and collection presentation review

This change extends the existing sanctuary interface treatment to squad selection and the unit collection. It reuses the approved background and font; it introduces no production art, factions, lore or balance changes.

## In the Unity host

- Squad summary shows selected names, capacity, faction eligibility and Apex occupancy. Existing faction, ownership, capacity and one-Apex gates remain enforced by the core flow.
- Inspect stats and all four ability slots directly inside a squad card, including locked units, without adding that unit or changing the draft.
- Collection opens as a faction roster. Open a unit for stats, fragments, unlock/advance requirements, adjustment options and ability timing. All units returns to the roster.
- Ability descriptions use prepared power/range values and kit availability rules. Main cooldown counts the owner's turns; signature readiness follows the authored enum. Placeholder passives remain explicitly unimplemented.
- Confirmation uses CollectionOffer.Before/After for every displayed stat and health-effect power, with cost and remaining balance. Cancel never spends resources. Existing preview tokens and save flow remain intact.
- The Deploy control remains outside the scrolling list. Unit styles load through Resources for existing integration scenes.

## Browser review

Open Preview.html locally. Query screens: Squad, Collection, Detail, Confirm, Adjust. The samples use abstract fixture values; A1 has five fragments to demonstrate advancement. Preview interactions do not edit a save. Confirm opens an explicit preview-only message.

This HTML is a browser layout approximation, not a Unity screenshot or substitute for testing UI Toolkit. It includes review navigation shortcuts. The runtime reads live profile and catalog data.

Checked at 320×568, 390×844, 768×1024 and 844×390: no page horizontal overflow, reachable footer, scrollable content and buttons at least 44 physical pixels high. Checked squad removal/reselection, four-slot expansion, roster-to-detail navigation and confirmation cancellation. Phone screenshots were visually inspected.

Repository structure, Unity metadata and existing art validation pass locally. Core rules run in GitHub CI. Unity is unavailable in this environment: C# presentation compilation, USS import and actual device rendering remain unverified.

## Next Unity playtest

1. Open Units: inspect an owned standard, locked standard and locked Apex; check fragment thresholds and all four slots.
2. Advance or adjust a unit: verify comparison against the resulting stats; cancel first and verify no change. Reach maximum test rank and verify the maximum-rank message.
3. Prepare a campaign squad and a mixed-faction squad. Inspect without selecting; remove/re-add; check capacity and one-Apex feedback. Return to briefing and confirm the draft survives.
4. Check portrait and landscape safe areas, scrolling to the last unit/ability, keyboard focus and fixed Deploy visibility. Deploy and finish a battle; verify collection changes persist across modes/reload.
