# Ashbound Enemy Readability 01 — Mission 11–14

**Scope:** precise UI-facing identification for the six already-approved ordinary Ashbound enemy templates. This is not an art approval, AI behavior change, new character class, new lore, or promise of future abilities.

## Problem

Before this change the live battle dock and enemy-turn banner used raw actor instance identifiers such as `ashbound-encounter-forager-1`. A new player could not easily tell a Forager from a Striker, or inspect enemy roles during the first Ashbound encounter. Mission briefings did not say which of the six types were present.

## Implemented

- `WorldFiveAshboundThreatReadout` maps **only the six exact technical UnitIds** approved in `WorldFiveActOnePrototype.EnemyTemplateIds` to friendly names and cautious battlefield interpretations:
  - **Rift Forager:** fast pursuer
  - **Parted Striker:** close pressure
  - **Seam Castigator:** ranged disruption
  - **Anchor Wrecker:** armored defender
  - **Afterecho Seer:** long-range pressure
  - **Camp Witness:** camp support
- Mission briefings reveal the **authored, counted enemy composition**, using the real `MissionContent.Actors` roster. Encounter types are not inferred from actor display names or hero unit attributes.
- During combat, the **enemy turn banner, turn-order details, active actor label and tap-to-inspect panel** display the correct identity.
- Tap-to-inspect retains **real HP, armor, normal attack amount and attack range from the current battle view**. The role and tactical sentence help interpretation but do not supersede those authoritative live values.
- The names are not used to change or override visual model registrations, enemy AI, mission victory rules, actions, status effects, save data, hit certainty, squad setup, or damage. Generic tutorial NPCs remain generic and World Five wildlife are never mislabeled as Ashbound.
- No "classes" are added to player or enemy gameplay. The short role descriptions are UI help, not a global role taxonomy, and no new abilities/buffs are claimed.

## Manual acceptance in Unity 6000.3.21f1

1. Select the opt-in World Five Act One / Encounter Variety prototype and open Mission 11's briefing. It should show exactly **Rift Forager × 1** and **Parted Striker × 1**.
2. In later missions, verify actual Forager/Striker/Castigator/Wrecker/Seer/Witness compositions and counts.
3. Enter battle; on enemy activation the turn cue should identify its archetype rather than its spawn ID.
4. Tap an enemy once. The dock should show the archetype, short role, neutral counterplay prompt, **actual** HP, armor, normal attack amount and range. No gameplay command should execute just from inspection.
5. On the battle Details view, verify turn order uses the same names, and the displayed stats still come from current battle data.
6. Replay tutorial Missions 1–10 and native wildlife Missions 7/8. No Ashbound briefing or named enemy should be falsely displayed.
7. Review at 320×568 and 390×844. Inspect that descriptions wrap naturally without covering ability buttons, and enemy names do not clip the footer.
8. Confirm no Unity Console compile errors. GitHub Core CI does **not** compile the runtime Unity UI script.

## Regression coverage

`WorldFiveAshboundThreatReadoutTests` executes in Windows/Ubuntu Core checks. It verifies all six template IDs and human-readable names, identity-exact matching, no leakage to wildlife/tutorial, coverage of the actual Missions 11–14 actor definitions, and refusal to claim unsupported mechanics or canon.

**Next material art milestone:** once this UI is accepted, complete a substantial visual refinement of the **actual in-battle** Ashbound models, with strong silhouettes, natural mantle anatomy, detailed UV/PBR, correct rigged contact motion, and device profiling. PR #171 improved provisional runtime fallbacks, but it did not deliver final sculpted models, and the unfinished separate rig branch should remain unapproved until native visual inspection.
