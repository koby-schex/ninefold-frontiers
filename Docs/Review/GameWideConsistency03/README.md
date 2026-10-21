# Game-Wide UI/UX Consistency 03 — Navigation Flow

## Change

Reselecting the currently open primary tab no longer discards its scroll-view reference. This allows the existing refresh logic to preserve the scroll position when the screen is rebuilt. Moving to a different tab continues resetting scroll appropriately. Unit-detail-to-collection-list behavior retains its existing explicit context reset.

## Boundaries

No navigation destinations, save/load state, battle controls, mission progression, asset rendering or main-tab visual geometry have changed. The existing approved Ninefold palette and UI Toolkit styles remain authoritative. This is a narrow usability fix, **not** a complete transition-animation implementation.

## Unity acceptance

In Unity 6.3 LTS, scroll Campaigns, Collection and Settings; tap the currently selected bottom tab and verify scroll does not jump. Switch to another tab and verify its correct page is shown. Open unit details and return to the collection. Check mission-path and briefing Back actions, results-to-next-mission flow, and returning to Home from an active battle. Test at 390x844 and 320x568, including reduced-motion settings. Core .NET CI does not compile Unity UI code; on-device visual testing remains necessary.
