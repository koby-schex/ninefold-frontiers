# Home Screen Polish 01 — portrait menu review (PT-048)

**Status:** Focused presentation refinement. The [approved World Five sanctuary artwork and its accompanying provenance](../Home/README.md) are unchanged. No canon change.

## Visual direction

Adopt the opt-in [Ninefold interface foundation](../../InterfaceFoundation01.md) from PR #59, informed by the live [Ninefold Worlds](https://ninefoldworlds.com) website's deep ink, ivory and muted gold. Preserve the unique approved World Five sanctuary atmosphere, silver-violet illumination and existing scenic composition.

This is a **hierarchy and clarity** pass, not a wholesale aesthetic replacement.

## Implementation

- `Assets/Ninefold/Resources/Home/HomePolish.uss` loads **after** shared Controls and overrides only selectors rooted in `.home-mode`.
- The Home action panel opts into semantic typography/control classes from `NinefoldFoundation.uss`; no other screen adopts these classes.
- Preserve the four-tab navigation and single priority-driven CTA:
  1. saved battle → Resume;
  2. claimable campaign completion → Collect;
  3. available campaign mission → Continue;
  4. otherwise → Browse.
- Keep Quick Battle and Your Units as *secondary* shortcuts. Reduce their height, avoid 170-unit minimum card widths on narrow phones and suppress nonessential shortcut subtitles only at compact sizes.
- Reduce long-form typography and vertical chrome slightly, preserving more visible sanctuary artwork and more breathing room.
- Use silver-violet only for the selected Home nav state; warm ivory and muted gold for shared identity. No new insignia, fictional geography, World Five structures or unit art is introduced.
- Slightly shorten the saved-battle Home notice. Battle, economy, ownership and reward logic are untouched.
- PT-048 distinguishes this source milestone from PT-047. This label is **not** a remote update detector or proof of actual local import.

## Browser approximation

Open [Preview.html](Preview.html) from a local checkout to inspect a self-contained *approximation* referencing the same approved sanctuary JPEG. Query parameter `?state=resume`, `?state=claim` or `?state=browse` switches the primary state in the mockup. The preview does not run Unity, launch missions, claim rewards or modify saves. Style differences versus Unity UI Toolkit remain possible.

## Test checklist after merge

1. **In Unity 6.3 LTS 6000.3.21f1**, pull latest `main`, let assets reimport, open the existing integration playtest. Verify no USS import errors or missing-resource warnings for `Home/HomePolish`.
2. Confirm the approved sanctuary image remains visible and unmodified; Home has one dominant CTA, two quieter secondary shortcuts, and four clear tabs. No new lore claims or fake faction symbols.
3. Verify layout at 320×568, 390×844, 540×960, a larger tablet and short landscape. Try actual iPhone safe areas when feasible. Content should scroll without overlapping navigation; two shortcuts should remain reachable; tooltips are supplemental.
4. Test Resume with saved battle, Claim with a pending campaign reward, Continue with an available campaign, and Browse when no next mission exists. The correct CTA should be unique; actions remain unchanged.
5. During a saved battle Quick Battle, Units and Campaigns must stay disabled; Home and Settings remain available. Verify existing one-time claim behavior and duplicate-prevention logic.
6. Verify selected navigation, pressed, keyboard focus and disabled treatments. Touch targets should be comfortable and labels legible; test Reduced Motion even though this pass introduces no motion.
7. Confirm Campaigns, Units, Results, Settings and the battle HUD look as before. This is a Home-only pass.

Static GitHub comparison only establishes file scope. **Unity compilation, touch behavior and device rendering have not been run by the authoring agent.**

## Next

After the user reviews an actual Unity capture, adjust any legibility/safe-area issues, then tackle the same restrained treatments in campaign navigation on a separate PR.
