# Unit Collection & Progression UX — Collection Clarity 01 (PT-052)

## Intent

The user's Unity playtest found the old menus visually outdated, oversized and confusing. Previous collection layout used two-column, giant abstract unit badges, large repeated stat cards and the complete ability kit immediately after entering a detail screen. This pass makes the collection **easy to browse first, detailed on request**, while preserving all saved-state controls.

### Changes

- **Roster:** clear Units title and unlocked count; mutually exclusive **All / Owned / Locked** filters; one easily tapped full-width row per unit grouped by fixture faction. Each row shows current owned/rank or locked status, standard/Apex distinction, and the real fragment balance/next threshold.
- **Detail:** a compact identity section, ownership and fragment progress, a clearly labeled unlock/advance action that is disabled when the core offer cannot confirm, and available stat-adjustment action. A single **View stats & abilities** disclosure reveals the six existing stats and all four authored ability slots. The player can hide the information again without losing progression or modifying the unit.
- **Review change:** unlock, advancement and customization still open the existing core-generated, saved-state-aware offer preview. Before/after health, armor, initiative, movement, rank and all applicable health-effect ability power changes remain visible alongside cost and balance. Confirmation is explicit; Cancel and Back still cancel safely.
- **Scoped styles:** new `Home/CollectionClarity.uss` loaded after the older Unit stylesheet and Controls. `collection-clarity-mode` applies only to Page.Collection. Squad selection still uses `unit-mode` but **does not** receive this pass's changes.
- **Shared visual direction:** deep ink, warm ivory, restrained gold, small role labels, soft World Five-linked badge accents only on abstract badges. No invented faction heraldry, new unit illustrations or world-specific art assertions.
- **Milestone:** launcher verifies new style import and reads PT-052. This is a source label, not proof that Unity imported it.

### What remains intentionally unchanged

The underlying `CollectionPresentation`, offers, resource balances, eligibility checks, maximum-rank logic, customization IDs, unit kits, save format and economy are unchanged. `inspectedUnit` remains a presentation-only string. Filters and the disclosure bool are in-memory presentation state (no save migration). Real unit models and appearance will come from separately approved canon-consistent production art; these are **still abstract placeholders**.

### Batched Unity review

1. Pull merged main in GitHub Desktop while out of Play mode; reimport in Unity 6000.3.21f1. Launcher should show PT-052 or a newer milestone. Verify no CollectionClarity stylesheet warning or C# compilation errors.
2. Open **Units** → All, Owned, Locked. Confirm the counts, faction headings and rows reflect the actual saved profile; an empty filter shows an empty-state explanation without errors.
3. Open an owned standard unit, a locked standard unit and a locked Apex. Confirm identifier, rank, fragments, unlock cost, progression status and one-Apex-specific designation are accurate.
4. Open **View stats & abilities**. Check Health/Armor/Initiative/Movement/Attack Power/Attack Range and passive/normal/main/signature descriptions, then hide. Ensure no stats change merely from inspection.
5. Test advance/unlock/customize: inspect the Before→After preview, Cancel, and verify resources unchanged; then Confirm and verify changes persist after visiting another page and reloading. Only actual core-eligible offers can confirm.
6. Test max test rank: no redundant unavailable upgrade button is shown, and held fragment balance remains visible.
7. During a saved battle, collection navigation remains disabled as before; battle, results, squad, Home and Campaigns styling should remain unchanged.
8. Check portrait 320×568, 390×844, 540×960 and a short landscape view. **Fit the Game-view Scale** to show the frame; inspect scroll, safe areas, focus/disabled states, touch targets and long ability descriptions.
9. Take screenshots of roster, detail, and confirmation for the next art-direction pass.

### Validation limits

Code and stylesheet scope checks are useful but cannot guarantee Unity USS import or on-device fidelity. No production-resolution character imagery is introduced. This pass is an information architecture improvement in the game's **abstract playtest UI**, not a visual claim of finished AAA-quality graphics.
