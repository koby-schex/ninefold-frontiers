# Unit Combat Identity 03 — Independent Unit Identities and Optional Preview Tags

**Design rule:** The Ninefold factions have no universal classes. Frontline, Damage, and Support are small, optional, non-exclusive **unit-preview tags** only. A unit can have zero, one, two, or all three tags, and a faction is not required to field any particular tag. No squad composition or ability availability may depend on these tags.

`UnitPreviewTag` and `UnitPreviewTags.For` provide the tag vocabulary for a later compact UI treatment. PR #107 does **not** yet render icons on the unit-preview screen or assign tags to canon units. Approved lore must determine labels, names, abilities, visuals, and unit identity. An Olyra-and-partner unit could receive both Frontline and Support hints if its actual lore and mechanics support that reading.

The remaining `UnitCombatRolesV4` code is an opt-in **noncanonical engineering fixture** demonstrating individually tuned defensive and offensive mechanics, not a faction class framework. It is not wired into live World Five V4 or existing player saves. Unique maximum HP, normal attack, and signature attack values remain required. The sample numbers are not assigned to any canonical creature or faction.

Next implementation pass: assess the approved master lore before integrating tiny tags into the actual unit preview and before designing canon-specific kits. Avoid enforcing tanks/healers/damage dealers per faction.
