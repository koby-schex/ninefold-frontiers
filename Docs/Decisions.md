# Technical decisions

| Date | Decision | Status |
|---|---|---|
| 2026-09-25 | Windows development, no local Mac; Unity engine; GitHub source repository | Confirmed by Koby |
| 2026-09-25 | Unity 6.3 LTS, exact Editor 6000.3.21f1 | User-supplied installed version; project pin |
| 2026-09-25 | iPhone 14 is one test device; test additional environments | Confirmed; not a minimum-device lock |
| 2026-09-25 | URP 17.3.0 | Initial implementation choice; Editor import pending |
| 2026-09-25 | Feature branch and PR workflow, Koby merges | Confirmed |
| 2026-09-25 | Hosted iOS builds / TestFlight | Proposed; provider, signing and upload not configured |

Editor upgrades require a deliberate PR recording package changes and validation.
No universal device support, frame rate, launch date or cloud pricing is promised.
