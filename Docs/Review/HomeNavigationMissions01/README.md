# Navigation Revision — Home / Campaigns / Missions / Units

The four bottom destinations are now Home, Campaigns, Missions (a placeholder for modes other than campaigns), and Units. Settings remains fully usable but appears as a small Home masthead gear shortcut with a tiny Settings caption; it is no longer a bottom tab.

The existing internal `Page.Missions` is the campaign mission path and remains intact; the new `Page.OtherModes` is the separate public Missions tab. No new game modes are implied to be playable.

Navigation is constructed identically on Home and other screens, dropping the extra Home-only `nf-nav` class. The shared style fixes nav height and four equal-width targets across pages. Test portrait layouts and safe areas on real devices/Unity; cross-platform core CI doesn't screenshot the interface.

No battle rules, saved progression, model assets, unit equipment, or lore changes.
