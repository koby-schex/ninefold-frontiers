# Unit Preview Experience 01 — Compact Hint Data

Adds a Unity-core-safe presentation model to transform optional Frontline, Damage, and Support tag flags into small icon keys and short accessible labels. Supports no tags, any combination, and all three tags. This does not define classes, force faction composition, or change combat decisions.

**Not yet implemented:** Actual rendering in Unity's unit preview, nor assigning tags to any canonical units. The icons are semantic keys (`shield`, `swords`, `heart-handshake`) for the eventual UI layer to resolve, not embedded sprites or invented lore artwork. There are deliberately no automatic assignments inferred from faction or stats. Canon decisions must come from the Master Lore Archive.

Changes do not touch gameplay saves, mission balances, or 3D assets. Follow-up: wire to actual unit-preview renderer once identified, and add lore-approved unit-to-tag metadata.
