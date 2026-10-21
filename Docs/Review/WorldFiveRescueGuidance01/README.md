# World Five Rescue Objective Guidance 01

The battlefield objective heading now gives an explicit, concise instruction at each stage of RescueAndExtract: reach the marked target and interact before pickup; after pickup, bring the rescued target to the marked extraction zone. Existing objective state (`Rescued`) determines the stage, and the preexisting schematic changes its destination to extraction automatically.

The guidance is read-only, does not infer a legal route through cover, and is hidden once the objective is no longer active. It does not alter combat, saves, mission content, rewards or models. End-to-end mission 003 pickup and extraction playthrough is still not certified; this PR does not claim otherwise. Validate screen wrapping on small portrait devices in Unity in addition to CI.
