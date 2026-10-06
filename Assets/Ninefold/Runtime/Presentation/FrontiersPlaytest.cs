using System;
using System.Collections;
using System.IO;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;
using Ninefold.Core.Persistence;
using Ninefold.Core.Progression;
using Ninefold.Core.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninefold.Presentation
{
    /// <summary>Abstract integration host, not production content. All rules and writes go through core views.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class FrontiersPlaytest : MonoBehaviour
    {
        public Camera BattlefieldCamera;
        public Material FixtureMaterial;
        public StyleSheet ScreenStyles;
        private ContentCatalog catalog;
        private MissionPresentation menu;
        private BattleInteraction input;
        private MissionMenuView menuView;
        private InteractionView battleView;
        private PlaytestBattlefield field;
        private VisualElement root, safe, content, surface, controls, heading;
        private Label notice;
        private ScrollView menuScroll;
        private Page renderedPage;
        private string renderedUnit;
        private bool renderedOffer;
        private string savePath, inspectedUnit, message = "Your progress saves automatically.";
        private enum Page { Home, Battles, Briefing, Collection, Settings }
        private Page page;
        private bool dirty, fatal, suspended, reducedMotion, inBattle, legacyProfile;
        private Coroutine playback;
        private int pointer = -1;
        private Vector2 pointerStart;

        private void Start()
        {
            reducedMotion = PlayerPrefs.GetInt("Ninefold.ReducedMotion", 0) == 1;
            var document = GetComponent<UIDocument>();
            if (document.panelSettings == null)
            {
                Debug.LogError("Playtest UI has no Panel Settings. Stop Play and run Ninefold > Playtest > Create or Open Integration Scene to repair the reference.", this);
                enabled = false; return;
            }
            root = document.rootVisualElement;
            if (ScreenStyles != null) root.styleSheets.Add(ScreenStyles);
            root.AddToClassList("root");
            safe = new VisualElement { name = "safe-area" }; root.Add(safe);
            heading = new VisualElement(); heading.AddToClassList("heading"); safe.Add(heading);
            Text(heading, "NINEFOLD", "brand");
            Text(heading, "F R O N T I E R S", "eyebrow");
            notice = Text(heading, message, "notice");
            content = new VisualElement { name = "screen-content" }; safe.Add(content);
            savePath = Path.Combine(Application.persistentDataPath, "AbstractPlaytest-v1");
            Guard(() => {
                if (BattlefieldCamera == null || FixtureMaterial == null) throw new InvalidOperationException("Run Ninefold > Playtest > Create or Open Integration Scene.");
                field = new PlaytestBattlefield(transform, BattlefieldCamera, FixtureMaterial);
                OpenProfile();
                Refresh();
            });
        }

        private void Update()
        {
            if (root == null || root.panel == null) return;
            var r = Screen.safeArea;
            var tl = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(r.xMin, Screen.height - r.yMax));
            var br = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(r.xMax, Screen.height - r.yMin));
            safe.style.left = tl.x; safe.style.top = tl.y;
            safe.style.width = br.x - tl.x; safe.style.height = br.y - tl.y;
            if (dirty && !fatal) { dirty = false; Guard(Refresh); }
            if (fatal || suspended || input == null || playback != null || battleView == null) return;
            if (battleView.Battle.Phase == BattleSessionPhase.EnemyInput || battleView.Battle.Phase == BattleSessionPhase.AdvanceRequired)
                Guard(() => Handle(input.Advance(new EnemyPerception(
                    battleView.Battle.Units.Where(u => u.IsEligible && u.IsPlayerSquad).Select(u => u.Id),
                    battleView.Battle.Units.Where(u => u.IsEligible && u.IsEnemyControlled).Select(u => u.Id)))));
        }

        private void LateUpdate()
        {
            if (!fatal && surface != null && surface.panel != null && field != null)
                field.Layout(surface, root);
        }

        private void Refresh()
        {
            if (playback != null) return;
            menuView = menu.Read();
            if (legacyProfile && menuView.Phase == MissionFlowPhase.Selection) { OpenProfile(); menuView = menu.Read(); }
            bool offerOpen = page == Page.Collection && menuView.Phase == MissionFlowPhase.Selection && menu.Collection.Read().Pending != null;
            var scrollOffset = menuScroll != null && renderedPage == page && renderedUnit == inspectedUnit && renderedOffer == offerOpen ? menuScroll.scrollOffset : Vector2.zero;
            menuScroll = null;
            content.Clear(); surface = null; controls = null; pointer = -1;
            notice.text = message;
            root.EnableInClassList("battle-mode", menuView.Phase == MissionFlowPhase.Battle && inBattle);
            if (menuView.Phase == MissionFlowPhase.Battle && inBattle)
            {
                content.style.backgroundColor = Color.clear;
                input = menu.ResumeBattle(); battleView = input.Read();
                BuildBattle();
            }
            else
            {
                content.style.backgroundColor = new Color(.035f, .063f, .094f);
                input = null; battleView = null; field.Hide();
                var scroll = new ScrollView(); scroll.AddToClassList("page-scroll"); content.Add(scroll);
                if (menuView.Phase == MissionFlowPhase.Results) BuildResults(scroll);
                else if (page == Page.Settings) BuildSettings(scroll);
                else if (menuView.Phase == MissionFlowPhase.Battle || page == Page.Home) BuildHome(scroll);
                else if (page == Page.Collection) BuildCollection(scroll);
                else if (page == Page.Briefing) BuildBriefing(scroll);
                else BuildSelection(scroll);
                menuScroll = scroll; renderedPage = page; renderedUnit = inspectedUnit; renderedOffer = offerOpen;
                scroll.schedule.Execute(() => scroll.scrollOffset = scrollOffset);
                if (menuView.Phase != MissionFlowPhase.Results && page != Page.Briefing) BuildNavigation(content);
            }
        }

        private void BuildHome(VisualElement parent)
        {
            bool active = menuView.Phase == MissionFlowPhase.Battle;
            var hero = Card(parent, "hero");
            Text(hero, "YOUR NEXT STEP", "eyebrow");
            Text(hero, active ? "Return to the field" : "Choose your frontier", "display-title");
            Text(hero, active ? "Your battle is saved. Continue where you left off." : "Advance through a campaign or jump into a standalone encounter.", "detail");
            if (active)
            {
                Text(hero, MissionName(menuView.Resume.MissionId), "section");
                Primary(hero, "Resume battle  ›", () => { inBattle = true; message = "Battle resumed."; });
            }
            else Primary(hero, "Explore campaigns  ›", () => Navigate(Page.Battles));
            Text(parent, "PLAY YOUR WAY", "eyebrow");
            var quick = Card(parent);
            Text(quick, "Quick battle", "section");
            Text(quick, "Mix factions. Defeat the opposition. Shared unit progression.", "detail");
            Button(quick, "Prepare squad  ›", () => SelectMission("fixture-mixed"), !active);
            if (active) Text(quick, "Finish your saved battle to begin another.", "hint");
            var collection = Card(parent);
            Text(collection, "Your units", "section");
            Text(collection, menuView.Units.Count(u => u.Owned) + " unlocked • inspect stats, fragments and upgrades", "detail");
            Button(collection, "Open collection  ›", () => Navigate(Page.Collection), !active);
            Text(parent, "CAMPAIGN PROGRESS", "eyebrow");
            foreach (var c in menuView.Campaigns)
            {
                var card = Card(parent);
                Text(card, c.CampaignId == "fixture-starter" ? "Test campaign A" : "Test campaign B", "section");
                Meter(card, c.CompletedMissions, c.TotalMissions);
                Text(card, c.CompletedMissions + " / " + c.TotalMissions + " missions completed", "hint");
                if (!active && c.IsComplete && !c.RewardsClaimed)
                    Primary(card, "Collect completion reward", () => { menu.ClaimCampaign(c.CampaignId); message = "Campaign rewards collected. New units and missions are available."; });
            }
        }

        private void SelectMission(string id)
        {
            menu.SelectMission(id); var view = menu.Read(); var mission = catalog.Missions[id];
            menu.SetSquad(view.Units.Where(u => u.EligibleForMission && !u.Definition.Roster.IsApex).Take(mission.MaximumSquad).Select(u => u.Definition.Id));
            page = Page.Briefing; message = "Review the mission and your squad, then deploy.";
        }

        private void BuildSelection(VisualElement parent)
        {
            Text(parent, "Campaigns", "display-title");
            Text(parent, "Choose a mission, prepare your squad, then deploy.", "detail");
            foreach (var group in menuView.Missions.Where(m => m.Definition.IsCampaign).GroupBy(m => m.Definition.FactionId))
            {
                Text(parent, group.Key == "fixture-a" ? "Test campaign A" : "Test campaign B", "section");
                foreach (var m in group.OrderBy(m => m.Definition.Id == "fixture-opening" ? 0 : 1))
                {
                    string id = m.Definition.Id;
                    var card = Card(parent, m.Available ? "mission-card" : "locked-card");
                    Text(card, m.IsReplay ? "COMPLETED · REPLAY AVAILABLE" : !m.Available ? "LOCKED" : "READY TO PLAY", "eyebrow");
                    Text(card, MissionName(id), "section");
                    Text(card, Description(id), "detail");
                    Text(card, "Faction squad • " + m.Definition.MinimumSquad + "–" + m.Definition.MaximumSquad + " units", "hint");
                    if (!m.Available) Text(card, m.FactionRosterLocked ? "Unlock three standard units from this faction." : "Complete " + string.Join(", ", m.MissingPrerequisites.Select(MissionName)) + " first.", "hint");
                    Button(card, m.IsReplay ? "Replay mission  ›" : "Prepare squad  ›", () => SelectMission(id), m.Available);
                }
            }
        }

        private void BuildBriefing(VisualElement parent)
        {
            Button(parent, "‹ Back", () => Navigate(catalog.Missions[menuView.SelectedMissionId].IsCampaign ? Page.Battles : Page.Home));
            var definition = catalog.Missions[menuView.SelectedMissionId];
            Text(parent, "MISSION BRIEFING", "eyebrow");
            Text(parent, MissionName(definition.Id), "display-title");
            var brief = Card(parent);
            Text(brief, "Your objective", "section"); Text(brief, Description(definition.Id), "detail");
            BuildMapPreview(brief, definition);
            Text(brief, "No timer • progress retained after defeat", "hint");
            Text(parent, "Choose your squad", "section");
            Text(parent, menuView.Squad.Count + " / " + definition.MaximumSquad + " selected • maximum one Apex", "detail");
            var roster = Row(parent); roster.AddToClassList("unit-grid");
            foreach (var u in menuView.Units.Where(u => u.EligibleForMission))
            {
                string id = u.Definition.Id; bool selected = menuView.Squad.Contains(id);
                bool canAdd = selected || (menuView.Squad.Count < definition.MaximumSquad && (!u.Definition.Roster.IsApex || !menuView.Units.Any(x => menuView.Squad.Contains(x.Definition.Id) && x.Definition.Roster.IsApex)));
                var tile = Button(roster, "", () => {
                    var squad = menu.Read().Squad.ToList(); if (!squad.Remove(id)) squad.Add(id); menu.SetSquad(squad);
                }, canAdd);
                tile.AddToClassList("unit-tile"); tile.EnableInClassList("selected", selected);
                UnitBadge(tile, id, u.Definition.Roster.IsApex);
                Text(tile, UnitName(id), "section");
                Text(tile, u.Definition.Roster.IsApex ? "APEX" : "STANDARD", "eyebrow");
                Text(tile, "HP " + u.Health + " • Armor " + u.Armor, "detail");
                Text(tile, "Move " + u.Movement + " • Initiative " + u.Initiative, "hint");
                Text(tile, selected ? "SELECTED" : canAdd ? "+ ADD TO SQUAD" : "SQUAD LIMIT", "tile-state");
            }
            string plan = menuView.PlanId;
            var deploy = Card(content, "deploy-dock");
            Text(deploy, menuView.Squad.Count + " units ready • " + MissionName(definition.Id), "hint");
            Primary(deploy, "Deploy squad  ›", () => { input = menu.Start(plan); inBattle = true; message = "Tap ground to preview a move. Select an ability to find targets."; }, menuView.CanStart);
            if (!menuView.CanStart) Text(parent, "Select between " + definition.MinimumSquad + " and " + definition.MaximumSquad + " eligible units, with at most one Apex. " + menuView.SquadFailure, "detail");
        }

        private void BuildCollection(VisualElement parent)
        {
            var view = menu.Collection.Read();
            Text(parent, "Your collection", "display-title");
            Text(parent, "Select a unit to inspect its kit and progression.", "detail");
            var units = view.Factions.SelectMany(f => f.Units).ToArray();
            if (inspectedUnit == null || !units.Any(u => u.Unit.Definition.Id == inspectedUnit)) inspectedUnit = units.First().Unit.Definition.Id;
            var current = units.First(u => u.Unit.Definition.Id == inspectedUnit);
            var detail = Card(parent, "unit-detail");
            UnitBadge(detail, inspectedUnit, current.Unit.Definition.Roster.IsApex);
            Text(detail, UnitName(inspectedUnit), "display-title");
            Text(detail, (current.Unit.Owned ? "UNLOCKED" : "LOCKED") + (current.Unit.Definition.Roster.IsApex ? " · APEX" : " · STANDARD"), "eyebrow");
            var stats = Row(detail); stats.AddToClassList("stat-row");
            Stat(stats, "HEALTH", current.Unit.Health.ToString()); Stat(stats, "ARMOR", current.Unit.Armor.ToString());
            Stat(stats, "INITIATIVE", current.Unit.Initiative.ToString()); Stat(stats, "MOVEMENT", current.Unit.Movement.ToString("0.##"));
            Text(detail, "Rank " + current.Unit.Rank + " • " + current.Fragments + " fragments", "section");
            var offer = current.Unit.Owned ? current.Advance : current.Unlock;
            Meter(detail, current.Fragments, Math.Max(1, offer.Cost));
            Text(detail, offer.CanConfirm ? "Ready to " + (current.Unit.Owned ? "advance" : "unlock") : CollectionReason(offer), "hint");
            var operation = current.Unit.Owned ? CollectionOperation.Advance : CollectionOperation.Unlock;
            Primary(detail, (current.Unit.Owned ? "Advance unit" : "Unlock unit") + " · " + offer.Cost + " fragments", () => menu.Collection.Preview(operation, inspectedUnit), offer.CanConfirm);
            if (current.Unit.Owned)
                Button(detail, current.Unit.CustomizationId == null ? "Adjust health / armor" : "Reset stat adjustment", () => menu.Collection.Preview(CollectionOperation.Customize, inspectedUnit, current.Unit.CustomizationId == null ? "fixture-health" : null));
            Text(detail, "ABILITY KIT", "eyebrow");
            foreach (var ability in current.Unit.Abilities)
                Text(detail, AbilityName(ability.Slot) + " · " + (ability.IsPassivePlaceholder ? "Not implemented in this test kit" : ability.Slot == AbilitySlot.Passive ? "Automatic effect" : ability.Effect.Kind + " " + ability.Effect.Amount + " · Range " + ability.Effect.Range), "detail");
            if (view.Pending != null)
            {
                detail.style.display = DisplayStyle.None;
                var p = view.Pending;
                var confirmation = Card(parent, "confirmation");
                Text(confirmation, "Review change · " + UnitName(p.UnitId), "section");
                Text(confirmation, "Health  " + p.Before.Health + " → " + p.After.Health + "\nArmor  " + p.Before.Armor + " → " + p.After.Armor + "\nRank  " + p.RankBefore + " → " + p.RankAfter, "detail");
                Text(confirmation, p.ResourceId == null ? "No fragment cost" : "Spend " + p.Cost + " of your " + p.Balance + " fragments", "preview");
                Primary(confirmation, "Confirm & save", () => { menu.Collection.Confirm(p.Id); message = "Unit updated. Changes apply across modes."; }, p.CanConfirm);
                Button(confirmation, "Cancel", () => menu.Collection.Cancel());
                return;
            }
            foreach (var faction in view.Factions)
            {
                Text(parent, faction.Id == "fixture-a" ? "Test faction A" : "Test faction B", "section");
                var roster = Row(parent); roster.AddToClassList("unit-grid");
                foreach (var u in faction.Units)
                {
                    string id = u.Unit.Definition.Id;
                    var tile = Button(roster, "", () => { menu.Collection.Cancel(); inspectedUnit = id; });
                    tile.AddToClassList("unit-tile"); tile.EnableInClassList("selected", id == inspectedUnit);
                    UnitBadge(tile, id, u.Unit.Definition.Roster.IsApex);
                    Text(tile, UnitName(id), "section");
                    Text(tile, u.Unit.Owned ? "Rank " + u.Unit.Rank : "Locked", "detail");
                    Text(tile, u.Fragments + " fragments", "hint");
                }
            }
        }

        private void BuildResults(VisualElement parent)
        {
            var r = menuView.Results;
            var summary = Card(parent, "hero");
            Text(summary, "BATTLE COMPLETE", "eyebrow");
            Text(summary, r.Result.Outcome.ToString(), "display-title");
            Text(summary, MissionName(r.Result.MissionId) + " • round " + r.Result.Round + "\n" + r.Result.Reason, "detail");
            Text(summary, "Your units and progression are retained.", "hint");
            var rewards = Card(parent);
            Text(rewards, "Battle rewards", "section");
            Text(rewards, r.Grants.Count == 0 ? "No rewards this attempt." : string.Join("\n", r.Grants.Select(g => "+" + g.Amount + "  " + ResourceName(g.ResourceId))), "reward-list");
            Primary(rewards, r.Claimed ? "Rewards saved" : "Collect rewards", () => { menu.ClaimRewards(r.Result.AttemptId); message = "Rewards saved."; }, r.CanClaim);
            Primary(parent, "Return home", () => { menu.ReturnToSelection(r.Result.AttemptId); page = Page.Home; inBattle = false; message = "Battle complete. Choose your next activity."; }, r.Claimed);
        }

        private void BuildBattle()
        {
            var b = battleView.Battle;
            var top = Card(content, "battle-header");
            var line = Row(top);
            Text(line, MissionName(b.MissionId) + " · Round " + b.Round, "section");
            Button(line, "Home", () => { input.Cancel(); inBattle = false; Navigate(Page.Home); }, !battleView.IsAnimating);
            foreach (var o in b.Objectives)
                Text(top, (o.Kind == ObjectiveKind.Stabilize ? "Stabilize the gold objective" : "Defeat the opposition") + " · " + o.Progress + "/" + o.Required, "objective-text");
            var turns = Row(top); turns.AddToClassList("turn-strip");
            var actor = b.Units.FirstOrDefault(u => u.Id == b.Activation?.UnitId);
            Text(turns, (actor?.IsEnemyControlled == true ? "ENEMY · " : "NOW · ") + UnitName(b.Activation?.UnitId), "current-turn");
            foreach (var id in b.UpcomingTurns.Take(3)) Text(turns, UnitName(id), "turn-chip");
            surface = new VisualElement { name = "battle-surface" }; surface.style.flexGrow = 1; surface.style.minHeight = 100;
            surface.style.overflow = Overflow.Hidden; content.Add(surface);
            surface.RegisterCallback<PointerDownEvent>(e => {
                if (e.button != 0 || !e.isPrimary || pointer != -1) { pointer = -1; return; }
                pointer = e.pointerId; pointerStart = e.position;
            });
            surface.RegisterCallback<PointerCancelEvent>(_ => pointer = -1);
            surface.RegisterCallback<PointerLeaveEvent>(_ => pointer = -1);
            surface.RegisterCallback<PointerUpEvent>(e => {
                bool tap = pointer == e.pointerId && e.button == 0 && e.isPrimary && Vector2.Distance(pointerStart, (Vector2)e.position) <= 12;
                pointer = -1; e.StopPropagation();
                if (tap && !dirty && playback == null && !suspended) Guard(() => Pick((Vector2)e.position));
            });
            field.Show(battleView, catalog.Missions[b.MissionId].Objectives, surface);
            // Fixed footer height prevents previews changing the camera viewport under a second tap.
            var footer = new ScrollView(); footer.AddToClassList("battle-dock");
            content.Add(footer);
            controls = new VisualElement(); controls.AddToClassList("controls"); footer.Add(controls);
            BuildControls();
        }

        private void BuildControls()
        {
            controls.Clear();
            var b = battleView.Battle;
            bool ready = !battleView.IsAnimating && !suspended;
            if (b.Activation != null) Text(controls, UnitName(b.Activation.UnitId) + " · Movement " + b.Activation.MovementRemaining.ToString("0.##") + " · " + (b.Activation.PrimaryActionAvailable ? "1 action ready" : "Action spent"), "actor-status");
            var selected = b.Units.FirstOrDefault(u => u.Id == battleView.SelectedUnitId);
            if (selected?.Health != null) Text(controls, UnitName(selected.Id) + " • HP " + selected.Health.CurrentHealth + "/" + selected.Health.MaximumHealth + " • Armor " + selected.EffectiveArmor, "detail");
            var row = Row(controls);
            foreach (var a in b.Actions.Where(a => a.Slot != AbilitySlot.Passive))
            {
                var slot = a.Slot;
                var button = Button(row, AbilityName(slot) + "\n" + (a.CanSelect ? slot == AbilitySlot.Signature ? "Once per battle" : "Select target" : ActionReason(a)), () => Handle(input.SelectAbility(slot)), ready && a.CanSelect);
                button.AddToClassList("ability-button");
                button.tooltip = a.CanSelect ? "Select, then tap a target to preview." : a.Block + " / " + a.RuleFailure;
                if (battleView.SelectedAbility == slot) button.AddToClassList("selected");
            }
            if (battleView.SelectedAbility.HasValue)
            {
                var actor = b.Units.First(u => u.Id == b.Activation.UnitId);
                var ability = actor.Abilities.First(a => a.Slot == battleView.SelectedAbility.Value);
                Text(controls, "Range " + ability.Range + " · " + battleView.Targets.Count(t => t.IsValid) + " valid targets marked in gold", "hint");
            }
            if (battleView.Pending != null)
            {
                var p = battleView.Pending;
                string preview = p.Kind == IntentKind.Movement ? "Move cost " + p.Movement.Cost.ToString("0.##") + (p.RequestedDestination.HasValue && !p.RequestedDestination.Value.Equals(p.Movement.Path.Last()) ? " • stops at this turn’s movement limit" : "") : p.Kind == IntentKind.Interaction ? "Stabilize • uses this turn's action" : UnitName(p.Target.TargetId) + " • HP " + p.Target.Effect.Health?.HealthBefore + " → " + p.Target.Effect.Health?.HealthAfter;
                var confirm = Row(controls); confirm.AddToClassList("intent-row");
                Text(confirm, preview, "preview");
                Primary(confirm, p.Kind == IntentKind.Movement ? "Move here" : p.Kind == IntentKind.Interaction ? "Stabilize" : "Use ability", () => Handle(input.Confirm(p.Id)), ready);
            }
            else Text(controls, battleView.IsAnimating ? "Resolving action…" : b.Phase != BattleSessionPhase.PlayerInput ? "Enemy turn…" : battleView.SelectedAbility.HasValue ? "Tap a marked unit to preview the effect." : "Tap the ground to preview movement.", "hint");
            row = Row(controls);
            Button(row, "Cancel / move", () => Handle(input.Cancel()), ready);
            if (b.Objectives.Any(o => o.Kind == ObjectiveKind.Stabilize && o.Status == ObjectiveStatus.Active))
                Button(row, "Stabilize", () => Handle(input.TapObjective("primary")), ready && b.Phase == BattleSessionPhase.PlayerInput);
            long activation = b.Activation?.ActivationId ?? 0;
            Button(row, "End turn", () => Handle(input.EndTurn(activation)), ready && b.Phase == BattleSessionPhase.PlayerInput);
            Text(controls, "Tap again to confirm · Cyan: allies · Red: enemies · Gold: objective / target", "legend");
        }

        private void Pick(Vector2 panelPosition)
        {
            string id = field.PickUnit(panelPosition, root);
            if (id != null) { Handle(input.TapUnit(id)); return; }
            if (!field.PickGround(panelPosition, root, out var destination)) return;
            // Tolerate finger drift only inside the visible destination marker; preserve core preview tokens.
            var pending = battleView.Pending;
            if (pending?.Kind == IntentKind.Movement && Vector2.Distance(field.Project(PlaytestBattlefield.Vector(pending.Movement.Path.Last()), root), panelPosition) < 22)
                Handle(input.Confirm(pending.Id));
            else Handle(input.TapDestination(destination));
        }

        private void Handle(BattleInputResult result)
        {
            message = result.Outcome == InputOutcome.Rejected ? RejectionReason(result)
                : result.Outcome == InputOutcome.Previewed ? "Preview ready. Tap again or confirm below." : result.Outcome == InputOutcome.Selected ? "Choose a target or destination." : result.Outcome == InputOutcome.Committed ? "Progress saved." : "Ready.";
            notice.text = message;
            if (result.AnimationId != null)
            {
                battleView = input.Read(); BuildControls();
                playback = StartCoroutine(Animate(result));
            }
            else dirty = true;
        }

        private IEnumerator Animate(BattleInputResult result)
        {
            // Always yield once so the coroutine field is set before completion, including reduced motion.
            yield return null;
            var frames = field.Play(result.Update, reducedMotion ? 0 : .4f);
            while (true)
            {
                bool more = false; object frame = null; Exception error = null;
                try { more = frames.MoveNext(); if (more) frame = frames.Current; }
                catch (Exception e) { error = e; }
                if (error != null) { Guard(() => throw error); yield break; }
                if (!more) break;
                yield return frame;
            }
            input.CompleteAnimation(result.AnimationId);
            playback = null; dirty = true;
        }

        private void OpenProfile()
        {
            var profile = PlaytestProfile.Open(new DirectoryBattleSaveFiles(savePath),
                new DirectoryBattleSaveFiles(Path.Combine(savePath,"Layouts-v2")),new DirectoryProgressSaveFiles(savePath));
            catalog = profile.Catalog; menu = profile.Menu; legacyProfile = profile.IsLegacy;
            if (legacyProfile) message = "Your saved battle is preserved. Finish it to use the new battlefield layouts.";
        }
        private void Reload()
        {
            if (playback != null) { StopCoroutine(playback); playback = null; }
            input = null; menu.Open();
            message = "Resumed the latest saved state. Unconfirmed previews are discarded.";
        }
        private void OnApplicationPause(bool paused)
        {
            suspended = paused; pointer = -1;
            if (!paused && menu != null && !fatal) Guard(() => { Reload(); dirty = true; });
        }
        private void OnApplicationFocus(bool focused) { pointer = -1; suspended = !focused; }
        private void Guard(Action action)
        {
            try { action(); }
            catch (Exception e)
            {
                fatal = true; pointer = -1;
                if (playback != null) { StopCoroutine(playback); playback = null; }
                Debug.LogException(e, this);
                if (notice != null) notice.text = "Playtest stopped: " + e.Message;
                content.Clear(); surface = null; field?.Hide();
                Text(content, "Your save has not been reset. See the Console for the error. Stop Play mode before copying the save folder for diagnosis.", "detail");
                Text(content, savePath ?? "", "detail");
            }
        }
        private void Navigate(Page destinationPage)
        {
            menu.Collection.Cancel(); page = destinationPage; menuScroll = null;
            message = "Your progress saves automatically.";
        }
        private void BuildNavigation(VisualElement parent)
        {
            var nav = Row(parent); nav.AddToClassList("navigation");
            bool activeBattle = menuView.Phase == MissionFlowPhase.Battle;
            foreach (var target in new[] { Page.Home, Page.Battles, Page.Collection, Page.Settings })
            {
                var destinationPage = target;
                var label = target == Page.Battles ? "Campaigns" : target == Page.Collection ? "Units" : target.ToString();
                var button = Button(nav, label, () => Navigate(destinationPage), !activeBattle || target == Page.Home || target == Page.Settings);
                button.EnableInClassList("selected", page == target);
            }
        }
        private void BuildSettings(VisualElement parent)
        {
            Text(parent, "Settings & help", "display-title");
            var motion = Card(parent);
            Text(motion, "Battle animations", "section");
            Text(motion, "Reduced motion skips movement and action playback without changing the battle rules.", "detail");
            Button(motion, reducedMotion ? "Reduced motion: ON" : "Reduced motion: OFF", () => {
                reducedMotion = !reducedMotion; PlayerPrefs.SetInt("Ninefold.ReducedMotion", reducedMotion ? 1 : 0); PlayerPrefs.Save();
            });
            var help = Card(parent);
            Text(help, "How to play", "section");
            Text(help, "1. Tap ground to preview a move.\n2. Tap the same destination again, or select Move here.\n3. Select an ability, then a marked target. Review the effect before confirming.\n4. End your turn when ready. There is no timer.", "detail");
            Text(help, "Long moves stop at your remaining movement allowance. Walls, units and arena boundaries still limit valid destinations.", "detail");
            Text(help, "Cyan units are allies; red units are enemies. Gold marks objectives and valid targets. Striped ground costs extra movement.", "detail");
            Text(parent, "DEVELOPMENT BUILD", "eyebrow");
            Text(parent, "Units, maps and badges in this build are abstract test content. They are not the final Ninefold art or canonical faction designs.", "detail");
        }
        private static string AbilityName(AbilitySlot slot) => slot == AbilitySlot.NormalAttack ? "Attack" : slot == AbilitySlot.Main ? "Main ability" : slot == AbilitySlot.Passive ? "Passive" : "Signature";
        private static string ActionReason(BattleActionView action)
        {
            if (action.Block == ActionBlock.NotPlayerTurn) return "Wait for your turn";
            switch (action.RuleFailure)
            {
                case AbilityUseFailure.PrimaryActionSpent: return "Action spent";
                case AbilityUseFailure.MainOnCooldown: return "On cooldown";
                case AbilityUseFailure.SignatureNotReady: return "Attack to prepare";
                case AbilityUseFailure.SignatureAlreadyUsed: return "Already used";
                default: return "Unavailable";
            }
        }
        private static string CollectionReason(CollectionOffer offer)
        {
            switch (offer.Block)
            {
                case CollectionBlock.InsufficientFragments: return offer.MissingFragments + " more fragments needed";
                case CollectionBlock.MaximumRank: return "Maximum test rank reached";
                case CollectionBlock.NotAtSelection: return "Finish your saved battle first";
                default: return "No upgrade available";
            }
        }
        private static string RejectionReason(BattleInputResult result)
        {
            switch (result.FieldFailure)
            {
                case FieldFailure.IllegalDestination: return "Choose an unoccupied spot inside the battlefield.";
                case FieldFailure.OutOfRange: return "Target is out of range. Move closer first.";
                case FieldFailure.Obstructed: return "Terrain blocks this attack. Find a clear angle.";
                case FieldFailure.BlockedPath: case FieldFailure.PathNotFound: return "There is no clear route to that spot.";
                case FieldFailure.InsufficientMovement: return "No movement remaining for that route.";
                case FieldFailure.InvalidTarget: return "Choose a valid target for this ability.";
                default: return result.InteractionFailure != InteractionFailure.None ? "Move into reach of the objective and keep your action available." : "That action is unavailable. Check your target and remaining action.";
            }
        }
        private static VisualElement Card(VisualElement parent, string extra = null)
        {
            var card = new VisualElement(); card.AddToClassList("card");
            if (extra != null) card.AddToClassList(extra); parent.Add(card); return card;
        }
        private static void BuildMapPreview(VisualElement parent, MissionContent mission)
        {
            Text(parent, "BATTLEFIELD OVERVIEW", "eyebrow");
            var map = new VisualElement { pickingMode = PickingMode.Ignore }; map.AddToClassList("map-preview"); parent.Add(map);
            var bounds = mission.Map.Bounds;
            float width = (float)(bounds.Max.X-bounds.Min.X), depth = (float)(bounds.Max.Z-bounds.Min.Z);
            void Mark(float x, float z, float w, float d, string cls)
            {
                var item = new VisualElement { pickingMode = PickingMode.Ignore }; item.AddToClassList(cls);
                item.style.position = Position.Absolute;
                item.style.left = Length.Percent((x-(float)bounds.Min.X)/width*100);
                item.style.top = Length.Percent(((float)bounds.Max.Z-z-d)/depth*100);
                item.style.width = Length.Percent(w/width*100); item.style.height = Length.Percent(d/depth*100); map.Add(item);
            }
            foreach (var ground in mission.Map.Ground)
                Mark((float)ground.Bounds.Min.X,(float)ground.Bounds.Min.Z,(float)(ground.Bounds.Max.X-ground.Bounds.Min.X),(float)(ground.Bounds.Max.Z-ground.Bounds.Min.Z),"map-slow");
            foreach (var obstacle in mission.Map.Obstacles)
                Mark((float)obstacle.Bounds.Min.X,(float)obstacle.Bounds.Min.Z,(float)(obstacle.Bounds.Max.X-obstacle.Bounds.Min.X),(float)(obstacle.Bounds.Max.Z-obstacle.Bounds.Min.Z),"map-cover");
            foreach (var objective in mission.Objectives.Where(o=>o.Interaction!=null))
                Mark((float)objective.Interaction.Point.X-.5f,(float)objective.Interaction.Point.Z-.5f,1,1,"map-objective");
            foreach (var point in mission.Deployment)
                Mark((float)point.X-.2f,(float)point.Z-.2f,.4f,.4f,"map-deploy");
            Text(parent, "Cyan: deployment · Gray: cover · Amber: slow ground · Gold: objective", "hint");
        }
        private Button Primary(VisualElement parent, string label, Action action, bool enabled = true)
        { var button = Button(parent, label, action, enabled); button.AddToClassList("primary"); return button; }
        private static void Meter(VisualElement parent, long value, long maximum)
        {
            var track = new VisualElement(); track.AddToClassList("progress-track"); parent.Add(track);
            var fill = new VisualElement(); fill.AddToClassList("progress-fill");
            fill.style.width = Length.Percent(maximum <= 0 ? 0 : Mathf.Clamp01((float)value / maximum) * 100); track.Add(fill);
        }
        private static void Stat(VisualElement parent, string label, string value)
        { var item = Card(parent, "stat"); Text(item, value, "section"); Text(item, label, "hint"); }
        private static void UnitBadge(VisualElement parent, string id, bool apex)
        {
            // Abstract ID badges, not faction emblems or substitutes for locked visual masters.
            var badge = new VisualElement { pickingMode = PickingMode.Ignore }; badge.AddToClassList("unit-badge");
            badge.EnableInClassList("apex-badge", apex); badge.EnableInClassList("faction-b", id.StartsWith("fixture-b-"));
            Text(badge, UnitName(id), "badge-id").pickingMode = PickingMode.Ignore; parent.Add(badge);
        }
        private Button Button(VisualElement parent, string label, Action action, bool enabled = true)
        {
            var b = new Button(() => { if (!fatal && !dirty && !suspended && playback == null) Guard(() => { action(); dirty = true; }); }) { text = label };
            b.SetEnabled(enabled); parent.Add(b); return b;
        }
        private static VisualElement Row(VisualElement parent) { var row = new VisualElement(); row.AddToClassList("row"); parent.Add(row); return row; }
        private static Label Text(VisualElement parent, string text, string cls) { var l = new Label(text); l.AddToClassList(cls); parent.Add(l); return l; }
        public static string UnitName(string id) => id == null ? "—" : id.Replace("fixture-enemy-", "Enemy ").Replace("fixture-a-", "A").Replace("fixture-b-", "B");
        private static string ResourceName(string id) => id.StartsWith("fragment-") ? UnitName(id.Substring(9)) + " fragments" : "Test supplies";
        private static string Description(string id) => id == "fixture-mixed" ? "Defeat the enemy. Use either side of the central cover island to approach." : id == "fixture-opening" ? "Stabilize the marked objective. An open center with offset side cover." : id == "fixture-finale" ? "Stabilize the objective. A northern barrier and slow central ground change your routes." : "Stabilize the objective with faction B units. Offset walls create an asymmetric arena.";
        private static string MissionName(string id) => id == "fixture-opening" ? "Stabilization" : id == "fixture-mixed" ? "Quick combat" : id == "fixture-finale" ? "Campaign finale" : "Faction B: stabilization";
        private void OnDestroy() { if (playback != null) StopCoroutine(playback); field?.Dispose(); }
    }
}
