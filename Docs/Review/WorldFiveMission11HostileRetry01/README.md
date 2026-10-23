# World Five Mission 11 — active hostile AI and defeat/retry regression

Builds on the merged Mission 11 battle-victory journey. The new scenario uses the **actual Mission 11 encounter and its authored Ashbound enemy templates**, but isolates that one mission in a test-only campaign catalog (no fake tutorial completion or write to player saves). It deploys a two-standard-unit squad and deliberately lets the hostile AI act with explicit perceived opponents, rather than withholding enemy perception as the prior deterministic victory test does.

The regression verifies a hostile activation, actual combat damage and movement, reopening the saved fight after an enemy attack, a terminal defeat, zero defeat rewards, idempotent defeat receipt, reloaded mission eligibility, and starting/resuming a fresh attempt with a different attempt ID.

**Important limitation:** The test supplies all surviving player units to the enemy AI as perceived (a full-visibility fixture), since automatic line-of-sight sensing is a separate workstream. It is a pressure/retry reliability regression, not an endorsement of the encounter's exact player difficulty. A real 10-tutorial-mission sequential playthrough and a fair Mission 11 victory against live enemies still need their own tests and device tuning.
