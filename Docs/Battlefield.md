# Battlefield validation contract

This is engine-independent validation, not a playable scene or completed navigation
system. It implements free legal destination selection (game archive D041), without
mandatory grid or snap points. Snap suggestions belong to future presentation.
No unit names, models, sizes, ranges or damage values in the tests establish canon.

## Setup and commands

Configure one BattlefieldMap through BattleTurnController.ConfigureBattlefield before
round one. Register each eligible unit once before its first activation: starting
position, FieldBody, authored FieldAbility profiles, and explicit compatible healing
target IDs. Setup copies collections. Profiles bind slot, effect, amount, range and
cover eligibility so commands only supply activation ID, slot and target ID.
Healing also requires allied status under the health controller. Compatibility is
an authored allowlist for this slice, not an approved nine-faction treatment matrix.

Use Battlefield.TryPreviewMovement / TryMove for spatial movement, and
TryPreviewEffect / TryApplyEffect for spatial single-target effects. Confirmation
revalidates current state rather than accepting an old preview as authority.
Rejecting a command preserves position, health, movement, primary action, cooldowns
and Signature use. Preview/cancel costs nothing. Movement may be split around the
primary action. Removed/defeated units cease blocking movement or being targets;
their position and health records remain available. Reinforcements occupy their
registered space immediately and retain the existing next-round activation rule.

Existing low-level SpendMovement, Abilities.TryUse and Health.TryApply remain trusted
simulation primitives for nonspatial tests and future special effects. They do not
perform geometry validation. The future UI/AI command boundary must use Battlefield
for ordinary spatial moves/attacks, never expose these primitives to raw player input.
This single-threaded simulation is not an online security boundary.

## Geometry and movement

Coordinates use world meters, Y up, X/Z floor plane. Input precision is at most six
decimal places with each coordinate within +/-10,000 m. This is numeric precision,
not a gameplay grid or suggested snap spacing. Invalid authored data throws during
setup; invalid gameplay commands return a FieldFailure (and HealthActionFailure for
health-rule rejections).

FieldBody is a fixed-orientation rectangular footprint with height, plus separate
local attack-origin and target-point offsets constrained inside the proxy. Bounds
and blockers are axis-aligned volumes. Swept box checks validate the entire route,
not just destinations. Touching blockers/other bodies is conservatively blocked;
a body may touch the outer map boundary. All living eligible bodies block all other
bodies regardless of faction. Unit bodies do not block shot rays in this first slice.
The map bounds are a convex traversable volume: holes/gaps must be authored as
movement blockers, not assumed from absent ground data.

Movement accepts 1-256 destination waypoints after the current position. Duplicate
consecutive points, changes in Y, blocked segments and insufficient allowance fail.
This supplied-path API does not find a path. The destination pathfinding API now
supplies routes; see Pathfinding.md. Euclidean segment
length uses decimal square-root iteration, costs round upward per segment to 0.000001
movement units, and costs sum across the complete route. Subdivision cannot create
free travel. Difficult-ground cost applies for the portion where the moving footprint
intersects its volume; overlapping ground uses the highest rate, not a sum/product.
Rates are authored from 1 through 100 movement units per meter. Discounts, traversal
exceptions, slopes, rotations and forced displacement are not implemented.

## Targeting and cover

Range is a 3D origin-to-authored-target-point measurement, inclusive at its limit.
It is not a universal center-to-center measurement or final mesh contact test.
A closed line segment from those points intersects authored obstacle volumes:
full shot blockers invalidate the action; partial obstacles contribute their
strongest qualifying cover reduction. Cover does not stack with itself. Only profiles
explicitly marked UsesCover receive it; healing cannot be marked this way. Range and
line obstruction are checked for all profiles in this slice, including healing.
There are no indirect-fire or obstacle-bypass profiles yet.

Cover depends on the shot direction and height: repositioning around or aiming
above a volume can bypass it, without granting a flanking or elevation damage bonus.
A boundary-grazing ray conservatively intersects. This is a first authored-ray model,
not multi-sample mesh exposure, penetration, or a canonical anatomy/cover approval.
The health resolver then applies armor and cover under its configurable mitigation
policy, using the same calculation for preview and commit. No miss or critical rolls.
Temporary/status mitigation integration remains deferred; this adapter supplies cover
and zero temporary reduction rather than pretending status effects are implemented.

## Verification and remaining gates

Behavioral tests exercise free placement, swept obstacles, occupied spaces, variable
footprints, detours, terrain costs, exact range, offsets, cover/height, compatibility,
stale commands and state changes between preview and confirm. CI runs the actual Core
sources on Windows and Linux. No claim of universal platform bit-identical simulation.

Destination routing is implemented in Pathfinding.md. Later work: reachable-area generation; slope/support/navmesh data;
rotating and mesh-derived bodies, weapon origins and contact targets; ability-specific
geometry and statuses; touch overlays and optional snaps; Unity import and physical
model/device checks. Real maps must accommodate canonical bodies without shrinking
models to fit these abstract fixtures. These implementation choices remain reviewable
and do not revise the Master Lore, PF1-8 or locked visual masters.
