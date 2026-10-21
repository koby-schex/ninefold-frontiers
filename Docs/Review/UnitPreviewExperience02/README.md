# Unit Preview Experience 02 — Collection UI Integration

The Collection unit detail now accepts optional serialized per-unit tag assignments in `FrontiersPlaytest` (`approvedUnitPreviewTags` in the Unity inspector). When a unit has approved tags, the detail screen displays compact Frontline, Damage, and/or Support labels below its name. Zero tags display nothing; there are no mandatory class slots or faction-wide inference rules.

**Canon constraint:** This change deliberately ships with an empty assignment array. No unit is assigned a tag automatically from faction, damage, health, or species. Populate assignments only after checking the Master Lore Archive and user approval. Tags are descriptive UI hints, never restrictions on units, squads, combat or abilities.

The visual treatment is compact text labels with reserved icon keys in core presentation data. The actual small pictogram assets have not been installed; future art integration may add them without changing combat. This touches only the Collection detail screen and its stylesheet, not the separate model pipeline. Check Unity play mode in addition to CI.
