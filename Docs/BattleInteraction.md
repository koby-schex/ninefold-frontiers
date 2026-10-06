# Battle interaction and at-a-glance health

`Views/BattleInteraction` is the single-threaded, engine-independent input controller above `BattlePresentation`. It is not yet a Unity UI, renderer, gesture recognizer or playable scene. Create one controller per session and route gameplay input through it. Do not drive the presentation/session separately during animation playback. Keep the existing explicit mission/reward flow outside this controller.

## Health visible without selecting units

`Read().Battle.HealthOverlays` always contains every eligible, placed, living unit with registered health: player squad, enemy and friendly non-squad units. Nothing is conditional on selection, targeting or whose turn it is. Each immutable record supplies unit/team identity, world position, body height, current/max HP and normalized fill fraction. Updates reflect committed damage, healing and movement; previews never replace actual HP. The target preview separately supplies projected HP for an optional ghost segment. Removed/dead units are excluded from new overlays; use the event batch's `Before` view to animate their disappearance. Full unit records still include their final health.

Future Unity integration must render compact health bars by default above all battlefield units, including full-health units, without a tap. Use current/max values and team/shape cues rather than color alone. Keep them readable in portrait layout and resolve overlap in screen space. Selection can open details but must not be needed to discover health. The adapter must project positions and height into screen space; this PR does not validate visual size, overlap, performance or touch readability. There is no fog-of-war filter in the current rules; add one at an explicit visibility boundary if that feature is later approved.

## Inputs

- `TapUnit` inspects a unit when no ability is selected. Inspection never changes the active actor. With an ability selected it previews that target, then confirms a second tap on the same target.
- `InspectUnit` explicitly exits targeting and opens inspection without firing. `SelectAbility` uses the active player actor and clears an earlier intent. Passive/blocked slots cannot be armed.
- `TapDestination` previews a bounded path for the active player. A second tap on the same destination confirms that exact path. Another destination replaces it. Invalid destinations clear the previous intent.
- `TapObjective` previews a mission interaction and confirms a second tap on that objective.
- A compact confirm button calls `Confirm(Pending.Id)` with the ID of the preview actually displayed. Old IDs cannot confirm a replacement intent. No preview, rejected commands and cancellations spend no resources.
- `Cancel` clears selection, ability and preview. `EndTurn` requires the current activation ID and discards any unconfirmed intent. Solo waiting never times out.

Picking, tap duration/double-tap timing, camera gestures and screen-to-world conversion belong to the Unity adapter. The controller implements sequential second-tap confirmation, not a timing threshold. The adapter must not dispatch both a single-tap command and another double-tap callback for the same gesture; suppress drags and UI-through-world taps. Use a deliberate touch hit tolerance in that adapter; world destinations here use exact coordinates. A button is the reliable alternative where precise repeated picking is inconvenient.

## Staleness, animation and recovery

Each intent binds to a private in-memory checkpoint identity and activation. Every successful battle commit or return to selection changes that identity, even within the same activation. Confirmation on a changed checkpoint returns `Stale` and clears choices; a refresh via `Read` clears stale choices before rendering. Reload or a new mission start expires the entire underlying session: old reads/commands throw and the host must construct a new controller. No revision token or UI state is serialized and no save schema changes. A wrong preview ID leaves a newer preview intact.

After a successful command with animation events, the result supplies a unique `AnimationId` and locks all gameplay/selection inputs. Reads and health overlays remain available. Play `Update.Events`, reconcile to `Update.After`, then call `CompleteAnimation` with that exact ID. Incorrect, old or repeated acknowledgements do not unlock a newer animation. With animations disabled/reduced, reconcile and acknowledge immediately. Do not auto-clear the lock on an arbitrary timer. `Advance` is also locked, preventing enemies/scheduling from running ahead of playback; when waiting for player input it preserves the current preview.

Confirmation consumes the intent before execution. Save exceptions propagate with no committed result or animation batch. Respect `MissionFlow.NeedsReload`: reopen the flow and construct a new session, presentation and interaction controller. Render the loaded state without replaying an old intent or animation. An ambiguous write may have committed. UI tokens are duplicate-input guards in one live controller, not network request IDs or an anti-cheat system. Multiple controllers and concurrent operations on one session are unsupported.

## Tests and scope

The input tests cover all-team health without selection, immutable overlays, damage/healing/removal/movement/reload, inspection, both confirmation paths, replacement/cancellation, rejection, animation locking, stale acknowledgements, duplicate confirmation, external checkpoint changes, reload, waiting, enemies, interactions, terminal phases and torn/ambiguous save recovery. Abstract fixtures only: no lore, balance, canon, visual master or monetization changes.
