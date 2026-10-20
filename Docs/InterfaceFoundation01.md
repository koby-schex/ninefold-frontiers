# Ninefold: Frontiers — Interface Foundation 01

**Status:** Implementation foundation / review candidate. Not a new canonical visual master and not an overhaul of the approved Home screen.

## Visual sources and precedence

1. **Canon:** Current Ninefold Master Lore & World-Building Archive, Production Foundations and locked art masters govern names, physical world motifs, faction identity and all diegetic symbols.
2. **Approved game UI:** `Docs/Review/Home/README.md` and the World Five sanctuary Home assets are already approved. Do not recolor or replace them to force uniformity.
3. **Brand reference:** The live `https://ninefoldworlds.com/` Home page, read on October 20, 2026, supplied its near-black green (#111611), warm ivory (#F6EFDD), secondary (#ABA891), warm gold (#D4B47B), and the Cormorant Garamond / Manrope pairing used on the web. Its restrained atlas/map wayfinding, open layout and single selected world are the underlying design reference. The website is inspiration, not a mobile screen to duplicate.
4. **Unity feasibility:** `Assets/Ninefold/Resources/Home/DisplaySerif.ttf` is already bundled and licensed; use it sparingly for display type. Retain native legible sans controls until specifically licensed and imported alternatives pass Unity tests.

## The Living Atlas: the interface concept

A frame through which the player experiences distinct worlds. Surfaces should feel crafted and calm, not futuristic neon or conventional fantasy ornament. World-specific atmosphere belongs in the hero artwork and one occasional accent; the shared shell remains readable and recognizably Ninefold.

The *Home sanctuary* retains its approved silver-violet World Five atmosphere. World Five accent styling in `NinefoldFoundation.uss` is opt-in and does not assert an unapproved game-wide violet palette. **No new world symbols, heraldry, layouts of settlements, creatures, or Arcana behavior are canonized by this proposal.**

## Rules for UI and UX

- **One primary decision:** Use one visually emphasized main CTA per view. Secondary paths use quieter buttons or the existing four-item navigation. Never show multiple equally prominent CTAs.
- **Progressive disclosure:** Main menu surfaces only the next journey, one-line context and essential secondary routes; mission restrictions, stats and lore details belong on their respective screens or in an explicit Info surface.
- **Maximum clarity over decoration:** Prefer breathing room, silhouettes and art. Avoid persistent glows, ornamental frames around every card, fake holograms, animated starfields, excessive badge piles, or multiple gradient layers.
- **Brand constants:** Deep ink #111611; raised ink #1C241E; stone border #374437; ivory #F6EFDD; secondary #ABA891; gold #D4B47B. These are UI system references, **not a mandate to overwrite existing approved screen artwork**.
- **Typography:** Serif for one page title or campaign hero; sans for navigation, actions, data and small labels. Avoid script lettering, forced all-caps paragraphs and excessively tracked body labels.
- **Touch and hierarchy:** Adopt a 56-unit primary control, at least 50 units for ordinary buttons, and 54-unit nav items as a starting point; verify physical touch targets and safe areas in the Unity Game view / on device. Do not shrink targets to fit; shorten or scroll content instead.
- **States:** Hover is supplemental only. Every command must read clearly in normal, pressed, keyboard-focused, disabled and selected states. Never rely on a color alone to show selection or locks.
- **Motion:** Short, understated transitions only when they communicate navigation or confirmation. Do not block actions while polish animations finish. Respect the existing Reduced Motion preference.
- **Battle is different:** Maintain the battle HUD's minimal action previews, health clarity, attack ranges and feedback. Do not apply decorative menu styling to health strips, tap targets or world-space overlays indiscriminately.
- **World accents:** Each distinct world can use one restrained accent derived from its already approved visual master, reviewed separately. Do not assign speculative colors to Worlds 1–9 in code. Never override gameplay colors for ally/enemy state or warning/confirmation.

## Implementation in this PR

- `Assets/Ninefold/Resources/Home/NinefoldFoundation.uss` provides **opt-in** `nf-` classes for surfaces, text, controls, separators and navigation; no selectors target existing screens.
- The `FrontiersPlaytest` host loads the stylesheet before the existing Home/Campaign/Units/Battle/Results/Controls layers. Since no existing view adds an `nf-` class, current screens retain their approved styles.
- The next Home polish PR can adopt these tokens deliberately, preserving the approved sanctuary, in a single focused visual review. Other screens opt in independently.
- There are no rule changes, no new art/font dependency, no purchases, no persistent-state migrations and no fixture-to-canon promotions.

## Review gates

- Confirm the stylesheet imports in **Unity 6.3 LTS / 6000.3.21f1** without parser warnings and that the existing Home appears unchanged before any opt-in classes are applied.
- Confirm the package still navigates Home → Campaigns → Units → Settings and back, that Resume/Claim restrictions remain intact, and that all navigation/touch controls work.
- Test portrait 320×568, 390×844 and 540×960, a tablet and a short landscape view. Confirm safe areas and that content scrolls rather than overlapping the navigation.
- For any future adoption, compare default/pressed/focused/disabled states, all labels and icons; enable Reduced Motion and check that no decorative motion is required.
- Repo static checks are not a substitute for a Unity editor import or on-device validation. This pass deliberately makes **no claim** that those checks have run.

**Next proposed PR:** Home Screen Polish 01: selectively adopt shared interface primitives while protecting the existing approved artwork and the one-primary-action hierarchy.
