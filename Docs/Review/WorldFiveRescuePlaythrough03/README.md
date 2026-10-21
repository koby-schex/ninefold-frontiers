# World Five Rescue Playthrough 03

Adds a chained integration test that completes World Five prototype missions 001 and 002 using the actual public battle interaction layer, starts mission 003, navigates via authoritative reachable movement samples, performs the rescue pickup, and reopens the active extraction-phase mission from persisted data.

The test deliberately stops at the pickup and does not claim to validate reaching the extraction zone or unlocking mission 004. This is an incremental check of the two-stage carrier system, rather than a complete rescue playthrough. Future work: complete extraction, verify rewards and unlock mission 004.

No production gameplay, saves, balance, UI or Work-owned 3D assets change.
