# Act One reliability gate — regression registration

The World Five batch test suite previously contained authored checks for the Ashbound debut, six NPC-only roles, four playable traditions and Afterecho status links, but did not include those methods in its registered test array. This PR **activates those regression tests** in CI and adds integrated-catalog checks for Mission 11 ordering, Defeat objective, nonplayable enemies, save isolation and the tutorial's Ashbound-free state.

No simulation or game model changes. This is an integrity gate to avoid adding larger scene/AI work on top of an untested combat/catalog integration.

Next iteration should cover real round-by-round battle victory/resume behavior, actor perception AI tests, and special ability animation/rig integration; these are not claimed to be complete here.
