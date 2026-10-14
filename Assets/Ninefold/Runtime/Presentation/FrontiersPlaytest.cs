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
    public sealed partial class FrontiersPlaytest : MonoBehaviour
    {
        public Camera BattlefieldCamera;
        public Material FixtureMaterial;
        public StyleSheet ScreenStyles;
        private ContentCatalog catalog;
        private MissionPresentation menu;
        private BattleInteraction input;
        private MissionMenuView menuView;
        private InteractionView battleView;
        private Label turnCue;
        private string announcedAttempt;
        private long announcedActivation;
        private float turnCueUntil;
        private PlaytestBattlefield field;
        private VisualElement root, safe, content, surface, controls, heading, homeBackdrop;
        private Label notice;
        private ScrollView menuScroll;
        private Page renderedPage;
        private string renderedUnit;
        private bool renderedOffer;
        private string savePath, inspectedUnit, message = "Your progress saves automatically.";
        private enum Page { Home, Battles, Missions, Briefing, Squad, Collection, Settings }
        private Page page;
        private string selectedCampaign, squadInspection;
        private bool dirty, fatal, suspended, reducedMotion, inBattle, legacyProfile;
        private Coroutine playback;
        private int pointer = -1;
        private Vector2 pointerStart;
        private bool battleDetails;
        private bool sharedControlsLoaded;
        private string battleRejection;

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
            var homeStyles = Resources.Load<StyleSheet>("Home/Home");
            if (homeStyles != null) root.styleSheets.Add(homeStyles);
            var campaignStyles = Resources.Load<StyleSheet>("Home/Campaign");
            if (campaignStyles != null) root.styleSheets.Add(campaignStyles);
            var unitStyles = Resources.Load<StyleSheet>("Home/Units");
            if (unitStyles != null) root.styleSheets.Add(unitStyles);
            var battleStyles = Resources.Load<StyleSheet>("Home/Battle");
            if (battleStyles != null) root.styleSheets.Add(battleStyles);
            var resultStyles = Resources.Load<StyleSheet>("Home/Results");
            if (resultStyles != null) root.styleSheets.Add(resultStyles);
            var sharedControls = Resources.Load<StyleSheet>(PlaytestRevision.ControlsResource);
            sharedControlsLoaded = sharedControls != null;
            if (sharedControlsLoaded) root.styleSheets.Add(sharedControls);
            else Debug.LogError("Shared playtest controls are missing. Pull the complete project and check import errors.", this);
            homeBackdrop = new VisualElement { name = "home-backdrop", pickingMode = PickingMode.Ignore };
            root.Add(homeBackdrop);
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
            SampleDiagnostics();
            if (root == null || root.panel == null) return;
            var r = Screen.safeArea;
            var tl = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(r.xMin, Screen.height - r.yMax));
            var br = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(r.xMax, Screen.height - r.yMin));
            safe.style.left = tl.x; safe.style.top = tl.y;
            safe.style.width = br.x - tl.x; safe.style.height = br.y - tl.y;
            root.EnableInClassList("compact-home", br.y - tl.y < 850);
            root.EnableInClassList("short-home", br.y - tl.y < 500);
            if (dirty && !fatal) { dirty = false; Guard(Refresh); }
            if (turnCue != null && Time.unscaledTime >= turnCueUntil) turnCue.style.display = DisplayStyle.None;
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
            if ((page == Page.Briefing || page == Page.Squad) && menuView.SelectedMissionId == null) page = Page.Home;
            bool offerOpen = page == Page.Collection && menuView.Phase == MissionFlowPhase.Selection && menu.Collection.Read().Pending != null;
            var scrollOffset = menuScroll != null && renderedPage == page && renderedUnit == inspectedUnit && renderedOffer == offerOpen ? menuScroll.scrollOffset : Vector2.zero;
            menuScroll = null;
            content.Clear(); surface = null; controls = null; pointer = -1;
            notice.text = menuView.Phase == MissionFlowPhase.Results ? menuView.Results.Claimed ? "Result and rewards saved." : "Battle finished. Review and save your result." : message;
            bool home = menuView.Phase != MissionFlowPhase.Results && page != Page.Settings &&
                !(menuView.Phase == MissionFlowPhase.Battle && inBattle) &&
                (menuView.Phase == MissionFlowPhase.Battle || page == Page.Home);
            bool resultsScreen = menuView.Phase == MissionFlowPhase.Results;
            bool campaignScreen = resultsScreen || page == Page.Settings || menuView.Phase == MissionFlowPhase.Selection &&
                (page == Page.Battles || page == Page.Missions || page == Page.Briefing || page == Page.Squad || page == Page.Collection || page == Page.Settings);
            root.EnableInClassList("campaign-mode", campaignScreen);
            root.EnableInClassList("results-mode", resultsScreen);
            root.EnableInClassList("unit-mode", menuView.Phase == MissionFlowPhase.Selection && (page == Page.Squad || page == Page.Collection));
            root.EnableInClassList("briefing-mode", campaignScreen && page == Page.Briefing);
            root.EnableInClassList("home-mode", home);
            heading.style.display = home || (menuView.Phase == MissionFlowPhase.Battle && inBattle) ? DisplayStyle.None : DisplayStyle.Flex;
            homeBackdrop.style.display = home || campaignScreen ? DisplayStyle.Flex : DisplayStyle.None;
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
                if (home)
                {
                    content.style.backgroundColor = Color.clear;
                    BuildHomeScreen();
                    return;
                }
                if (campaignScreen) content.style.backgroundColor = Color.clear;
                var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("page-scroll");
                scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden; content.Add(scroll);
                if (menuView.Phase == MissionFlowPhase.Results) BuildResults(scroll);
                else if (page == Page.Settings) BuildSettings(scroll);
                else if (page == Page.Collection) BuildCollection(scroll);
                else if (page == Page.Briefing) BuildBriefing(scroll);
                else if (page == Page.Squad) BuildSquad(scroll);
                else if (page == Page.Missions) BuildMissionList(scroll);
                else BuildSelection(scroll);
                menuScroll = scroll; renderedPage = page; renderedUnit = inspectedUnit; renderedOffer = offerOpen;
                scroll.schedule.Execute(() => scroll.scrollOffset = scrollOffset);
                if (menuView.Phase != MissionFlowPhase.Results && page != Page.Briefing && page != Page.Squad) BuildNavigation(content);
            }
        }

        private static string CampaignName(string id) => id == "fixture-starter" ? "Test campaign A" : "Test campaign B";

        private void ClaimCampaign(string id)
        {
            menu.ClaimCampaign(id);
            message = "Completion rewards collected. Your campaign progress is saved.";
        }

        private void BuildHomeScreen()
        {
            var masthead = new VisualElement(); masthead.AddToClassList("home-masthead"); content.Add(masthead);
            Text(masthead, "NINEFOLD", "home-wordmark");
            Text(masthead, "F R O N T I E R S", "home-submark");
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("home-scroll");
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            content.Add(scroll);
            // Fill tall portraits with scenery; short/landscape windows scroll without hiding actions.
            scroll.contentContainer.style.flexGrow = 1;
            scroll.contentViewport.RegisterCallback<GeometryChangedEvent>(evt =>
                scroll.contentContainer.style.minHeight = Mathf.Max(0, evt.newRect.height));
            var vista = new VisualElement { pickingMode = PickingMode.Ignore }; vista.AddToClassList("home-vista"); scroll.Add(vista);
            var panel = new VisualElement(); panel.AddToClassList("home-actions"); scroll.Add(panel);
            bool active = menuView.Phase == MissionFlowPhase.Battle;
            var next = menuView.NextCampaignMission;
            string claim = menuView.ClaimableCampaignId;
            Text(panel, "WORLD 5 · THE CONTINUANCE", "home-kicker");
            // Keep test content honest: the artwork is approved, the fixture campaign is not the story campaign.
            Text(panel, active ? "Return to the field" : claim != null ? "Campaign complete" : next != null ? CampaignName(next.CampaignId) : "Explore the frontiers", "home-title");
            Text(panel, active ? MissionName(menuView.Resume.MissionId) + " · Battle saved" : claim != null ? CampaignName(claim) + " · One-time rewards ready" : next != null ? MissionName(next.Definition.Id) + " · Your next mission" : "Replay missions or discover campaign requirements.", "home-summary");
            Button main;
            if (active) main = Button(panel, "RESUME BATTLE  ›", () => { inBattle = true; message = "Battle resumed."; });
            else if (claim != null) main = Button(panel, "COLLECT REWARDS  ›", () => ClaimCampaign(claim));
            else if (next != null) main = Button(panel, "CONTINUE CAMPAIGN  ›", () => SelectMission(next.Definition.Id));
            else main = Button(panel, "BROWSE CAMPAIGNS  ›", () => Navigate(Page.Battles));
            main.AddToClassList("home-primary");
            var shortcuts = Row(panel); shortcuts.AddToClassList("home-shortcuts");
            HomeShortcut(shortcuts, "Quick Battle", "Mixed-faction missions", "routes", () => SelectMission("fixture-mixed"), !active);
            HomeShortcut(shortcuts, "Your Units", menuView.Units.Count(u => u.Owned) + " unlocked · Collection", "units", () => Navigate(Page.Collection), !active);
            if (active) Text(panel, "Finish or resume your current battle. Campaigns and unit upgrades are locked until it ends.", "home-status");
            else if (message != "Your progress saves automatically.") Text(panel, message, "home-status");
            Text(panel, PlaytestRevision.Id + " · Abstract missions and units", "home-fixture");
            BuildNavigation(content);
        }

        private void HomeShortcut(VisualElement parent, string title, string subtitle, string icon, Action action, bool enabled)
        {
            var button = Button(parent, "", action, enabled); button.AddToClassList("home-shortcut");
            HomeIcon(button, icon); Text(button, title, "home-shortcut-title"); Text(button, subtitle, "home-shortcut-detail");
            button.tooltip = title;
        }

        private static void HomeIcon(VisualElement parent, string icon)
        {
            var image = new Image { image = Resources.Load<Texture2D>("Home/" + icon), pickingMode = PickingMode.Ignore };
            image.AddToClassList("home-icon"); parent.Add(image);
        }

        private void SelectMission(string id)
        {
            var view = menu.Read();
            if (view.SelectedMissionId != id)
            {
                menu.SelectMission(id); view = menu.Read();
                menu.SetSquad(view.Units.Where(u => u.EligibleForMission && !u.Definition.Roster.IsApex).Take(catalog.Missions[id].MaximumSquad).Select(u => u.Definition.Id));
            }
            selectedCampaign = view.Missions.Single(m => m.Definition.Id == id).CampaignId;
            Navigate(Page.Briefing); message = "Review the objective, then choose your squad.";
        }

        private void BuildSelection(VisualElement parent)
        {
            Text(parent, "CHOOSE YOUR CAMPAIGN", "eyebrow");
            Text(parent, "Campaigns", "display-title");
            Text(parent, "Unlock three standard units from a faction to enter its campaign.", "detail");
            foreach (var c in menuView.Campaigns)
            {
                string id = c.CampaignId;
                var missions = catalog.Campaigns.Single(x => x.Id == id).Missions
                    .Select(node => menuView.Missions.Single(m => m.Definition.Id == node.MissionId)).ToArray();
                var next = missions.FirstOrDefault(m => m.Available && !m.IsReplay);
                var card = Card(parent, "campaign-panel"); card.EnableInClassList("locked-card", !c.CanEnter);
                Text(card, !c.CanEnter ? "FACTION LOCKED" : c.IsComplete ? "CAMPAIGN COMPLETED" : "AVAILABLE", "campaign-status");
                Text(card, CampaignName(id), "campaign-name");
                Text(card, c.CompletedMissions + " / " + c.TotalMissions + " missions completed", "detail");
                Meter(card, c.CompletedMissions, c.TotalMissions);
                Text(card, Math.Min(3, missions[0].OwnedFactionStandards) + " / 3 standard units for entry", "hint");
                if (!c.CanEnter) Text(card, "Collect this faction’s unit fragments to unlock more standard units. Apex units do not count toward entry.", "lock-reason");
                else if (c.IsComplete && !c.RewardsClaimed)
                    Primary(card, "Collect completion rewards  ›", () => ClaimCampaign(id));
                else if (next != null)
                {
                    Text(card, "NEXT · " + MissionName(next.Definition.Id), "next-mission-label");
                    string nextId = next.Definition.Id;
                    Primary(card, "Continue campaign  ›", () => SelectMission(nextId));
                }
                else Text(card, "Completed missions remain available to replay.", "hint");
                Button(card, c.CanEnter ? "View all missions  ›" : "Preview locked missions  ›", () => { selectedCampaign = id; Navigate(Page.Missions); });
            }
            Text(parent, "PLAYTEST · Campaign lengths and faction labels are abstract fixtures.", "fixture-note");
        }

        private void BuildMissionList(VisualElement parent)
        {
            Button(parent, "‹ All campaigns", () => Navigate(Page.Battles)).AddToClassList("back-link");
            Text(parent, "CAMPAIGN MISSIONS", "eyebrow");
            Text(parent, CampaignName(selectedCampaign), "display-title");
            var progress = menuView.Campaigns.Single(c => c.CampaignId == selectedCampaign);
            Text(parent, progress.CompletedMissions + " / " + progress.TotalMissions + " completed · Untimed solo battles", "detail");
            Meter(parent, progress.CompletedMissions, progress.TotalMissions);
            var authored = catalog.Campaigns.Single(c => c.Id == selectedCampaign);
            string nextId = authored.Missions.Select(n => menuView.Missions.Single(m => m.Definition.Id == n.MissionId))
                .FirstOrDefault(m => m.Available && !m.IsReplay)?.Definition.Id;
            int number = 0;
            foreach (var node in authored.Missions)
            {
                var m = menuView.Missions.Single(x => x.Definition.Id == node.MissionId);
                string id = m.Definition.Id;
                var card = Card(parent, "mission-entry");
                card.EnableInClassList("locked-card", !m.Available);
                card.EnableInClassList("next-mission", id == nextId);
                var line = Row(card); line.AddToClassList("mission-heading");
                Text(line, (++number).ToString("00"), "mission-number");
                var words = new VisualElement(); words.AddToClassList("mission-identity"); line.Add(words);
                Text(words, !m.Available ? "LOCKED" : m.IsReplay ? "COMPLETED · REPLAY" : id == nextId ? "UP NEXT" : "AVAILABLE", "campaign-status");
                Text(words, MissionName(id), "section");
                Text(card, Description(id), "detail");
                if (!m.Available) Text(card, m.FactionRosterLocked ? "Unlock three standard units from this faction (" + m.OwnedFactionStandards + " / 3)." : "Complete " + string.Join(", ", m.MissingPrerequisites.Select(MissionName)) + " first.", "lock-reason");
                var button = Button(card, m.IsReplay ? "Review replay  ›" : !m.Available ? "Mission locked" : "View briefing  ›", () => SelectMission(id), m.Available);
                if (id == nextId) button.AddToClassList("primary");
            }
        }

        private void PreparationHeader(VisualElement parent, bool squad)
        {
            Text(parent, squad ? "1  BRIEFING   /   2  SQUAD" : "1  BRIEFING   /   2  SQUAD NEXT", "step-label");
            Text(parent, MissionName(menuView.SelectedMissionId), "display-title");
        }

        private void BuildBriefing(VisualElement parent)
        {
            var definition = catalog.Missions[menuView.SelectedMissionId];
            Button(parent, definition.IsCampaign ? "‹ Mission list" : "‹ Home", () => Navigate(definition.IsCampaign ? Page.Missions : Page.Home)).AddToClassList("back-link");
            PreparationHeader(parent, false);
            var terrain = Card(parent, "briefing-terrain");
            BuildMapPreview(terrain, definition);
            var brief = Card(parent, "briefing-objective");
            Text(brief, "YOUR OBJECTIVE", "eyebrow"); Text(brief, Description(definition.Id), "detail");
            Text(brief, "Untimed turns • no character or progression loss on defeat", "hint");
            var rewards = Card(parent);
            var mission = menuView.Missions.Single(m => m.Definition.Id == definition.Id);
            Text(rewards, mission.IsReplay ? "Replay victory rewards" : "First-clear victory rewards", "section");
            if (mission.VictoryRewards.Count == 0) Text(rewards, "No resource rewards for this mission.", "hint");
            foreach (var grant in mission.VictoryRewards)
            {
                var row = Row(rewards); row.AddToClassList("briefing-reward");
                Text(row, ResourceName(grant.ResourceId), "reward-name");
                Text(row, "+" + grant.Amount, "reward-amount");
            }
            Text(rewards, "Collect after victory. Campaign completion rewards are separate and one-time.", "hint");
            var dock = Card(content, "deploy-dock");
            Text(dock, definition.MinimumSquad + "–" + definition.MaximumSquad + " units • " + (definition.IsCampaign ? "faction squad" : "mixed factions allowed"), "hint");
            Primary(dock, "CHOOSE SQUAD  ›", () => Navigate(Page.Squad));
        }

        private void BuildSquad(VisualElement parent)
        {
            var definition = catalog.Missions[menuView.SelectedMissionId];
            Button(parent, "‹ Mission briefing", () => Navigate(Page.Briefing)).AddToClassList("back-link");
            PreparationHeader(parent, true);
            Text(parent, "Choose your squad", "display-title");
            Text(parent, "Select " + definition.MinimumSquad + "–" + definition.MaximumSquad + " units • maximum one Apex", "detail");
            Text(parent, "Tap a selected unit to remove it. Tap an available unit to add it.", "hint");
            bool hasApex = menuView.Units.Any(x => menuView.Squad.Contains(x.Definition.Id) && x.Definition.Roster.IsApex);
            var summary = Card(parent, "team-summary");
            Text(summary, "YOUR TEAM · " + menuView.Squad.Count + " / " + definition.MaximumSquad, "eyebrow");
            Text(summary, menuView.Squad.Count == 0 ? "No units selected" : string.Join("  •  ", menuView.Squad.Select(UnitName)), "section");
            Text(summary, (hasApex ? "1 / 1 Apex selected" : "0 / 1 Apex selected") + " · " + (definition.FactionId == null ? "Mixed factions allowed" : "This campaign's faction only"), "hint");
            Text(parent, "PLAYTEST · Abstract units and badges; not canonical faction designs.", "fixture-note");
            foreach (var u in menuView.Units.Where(u => definition.FactionId == null || u.Definition.Roster.FactionId == definition.FactionId))
            {
                string id = u.Definition.Id; bool selected = menuView.Squad.Contains(id);
                string blocked = !u.Owned ? "LOCKED · unlock in collection" : u.Definition.Roster.IsApex && hasApex ? "ONE APEX LIMIT · remove your selected Apex" : menuView.Squad.Count >= definition.MaximumSquad ? "SQUAD FULL · remove a unit first" : null;
                bool canAdd = u.EligibleForMission && (selected || blocked == null);
                var entry = Card(parent, "roster-entry");
                var tile = Button(entry, "", () => {
                    var squad = menu.Read().Squad.ToList(); if (!squad.Remove(id)) squad.Add(id); menu.SetSquad(squad);
                }, canAdd);
                tile.AddToClassList("squad-card"); tile.EnableInClassList("selected", selected);
                var row = Row(tile); UnitBadge(row, id, u.Definition.Roster.IsApex);
                var identity = new VisualElement(); identity.AddToClassList("unit-identity"); row.Add(identity);
                Text(identity, UnitName(id), "section");
                Text(identity, (u.Definition.Roster.IsApex ? "APEX" : "STANDARD") + " · Rank " + u.Rank, "eyebrow");
                Text(tile, "Health " + u.Health + " • Armor " + u.Armor, "detail");
                var attack = u.Abilities.Single(a => a.Slot == AbilitySlot.NormalAttack).Effect;
                Text(tile, "Move " + u.Movement + " • Initiative " + u.Initiative + " • Range " + attack.Range, "hint");
                Text(tile, selected ? "SELECTED · tap to remove" : blocked ?? "+ ADD TO SQUAD", "tile-state");
                Button(entry, squadInspection == id ? "Close unit details" : "Inspect stats & abilities", () => squadInspection = squadInspection == id ? null : id).AddToClassList("inspect-button");
                if (squadInspection == id) { BuildUnitStats(entry, u); BuildAbilityKit(entry, u); }
            }
            string plan = menuView.PlanId;
            var deploy = Card(content, "deploy-dock");
            Text(deploy, menuView.Squad.Count + " / " + definition.MaximumSquad + " selected • " + (menuView.CanStart ? "Ready to deploy" : "Choose at least " + definition.MinimumSquad + " eligible units"), "hint");
            Primary(deploy, "Deploy squad  ›", () => { input = menu.Start(plan); inBattle = true; message = "Tap ground to preview a move. Select an ability to find targets."; }, menuView.CanStart);
        }

        private void BuildCollection(VisualElement parent)
        {
            var view = menu.Collection.Read();
            Text(parent, "Your collection", "display-title");
            Text(parent, "Select a unit to inspect its kit and progression.", "detail");
            var units = view.Factions.SelectMany(f => f.Units).ToArray();
            if (!units.Any(u => u.Unit.Definition.Id == inspectedUnit))
            {
                inspectedUnit = null;
                Text(parent, "PLAYTEST · Abstract units and badges; not canonical faction designs.", "fixture-note");
                BuildCollectionRoster(parent, view);
                return;
            }
            Button(parent, "‹ All units", () => { menu.Collection.Cancel(); inspectedUnit = null; }).AddToClassList("back-link");
            var current = units.First(u => u.Unit.Definition.Id == inspectedUnit);
            var detail = Card(parent, "unit-detail");
            Text(parent, "PLAYTEST · Abstract units and badges; not canonical faction designs.", "fixture-note");
            UnitBadge(detail, inspectedUnit, current.Unit.Definition.Roster.IsApex);
            Text(detail, UnitName(inspectedUnit), "display-title");
            Text(detail, (current.Unit.Owned ? "UNLOCKED" : "LOCKED") + (current.Unit.Definition.Roster.IsApex ? " · APEX" : " · STANDARD"), "eyebrow");
            BuildUnitStats(detail, current.Unit);
            Text(detail, "Rank " + current.Unit.Rank + " • " + current.Fragments + " fragments", "section");
            var offer = current.Unit.Owned ? current.Advance : current.Unlock;
            if (offer.Block != CollectionBlock.MaximumRank)
            {
                Text(detail, current.Fragments + " / " + offer.Cost + " fragments · " + (current.Unit.Owned ? "next rank" : "unlock requirement"), "hint");
                Meter(detail, current.Fragments, Math.Max(1, offer.Cost));
            }
            Text(detail, offer.CanConfirm ? "Ready to " + (current.Unit.Owned ? "advance" : "unlock") : CollectionReason(offer), "hint");
            var operation = current.Unit.Owned ? CollectionOperation.Advance : CollectionOperation.Unlock;
            if (offer.Block != CollectionBlock.MaximumRank) Primary(detail, (current.Unit.Owned ? "Advance unit" : "Unlock unit") + " · " + offer.Cost + " fragments", () => menu.Collection.Preview(operation, inspectedUnit), offer.CanConfirm);
            if (current.Unit.Owned && current.Customizations.Any(o => o.Id == "fixture-health"))
                Button(detail, current.Unit.CustomizationId == null ? "Adjust health / armor" : "Reset stat adjustment", () => menu.Collection.Preview(CollectionOperation.Customize, inspectedUnit, current.Unit.CustomizationId == null ? "fixture-health" : null));
            BuildAbilityKit(detail, current.Unit);
            if (view.Pending != null)
            {
                detail.style.display = DisplayStyle.None;
                var p = view.Pending;
                var confirmation = Card(parent, "confirmation");
                Text(confirmation, "Review change · " + UnitName(p.UnitId), "section");
                Text(confirmation, p.Operation == CollectionOperation.Unlock ? "Unlock this unit for your roster" : p.Operation == CollectionOperation.Advance ? "Advance this unit's rank" : "Adjust this unit's stats", "eyebrow");
                Text(confirmation, "CURRENT  →  AFTER CONFIRMATION", "hint");
                var comparison = Row(confirmation); comparison.AddToClassList("stat-row");
                Stat(comparison, "HEALTH", p.Before.Health + " → " + p.After.Health);
                Stat(comparison, "ARMOR", p.Before.Armor + " → " + p.After.Armor);
                Stat(comparison, "INITIATIVE", p.Before.Initiative + " → " + p.After.Initiative);
                Stat(comparison, "MOVEMENT", p.Before.Movement + " → " + p.After.Movement);
                Stat(comparison, "RANK", p.RankBefore + " → " + p.RankAfter);
                foreach (var after in p.After.Abilities.Where(a => a.Effect != null && a.Effect.HasHealthEffect))
                {
                    var before = p.Before.Abilities.Single(a => a.Slot == after.Slot);
                    Stat(comparison, AbilityName(after.Slot).ToUpperInvariant() + " POWER", before.Effect.Amount + " → " + after.Effect.Amount);
                }
                Text(confirmation, p.ResourceId == null ? "No fragment cost" : "Spend " + p.Cost + " fragments · " + p.Balance + " → " + (p.Balance - p.Cost) + " remaining", "preview");
                Text(confirmation, "Nothing changes until you confirm. Progression is shared across modes.", "hint");
                if (!p.CanConfirm) Text(confirmation, CollectionReason(p), "lock-reason");
                Primary(confirmation, "Confirm & save", () => { menu.Collection.Confirm(p.Id); message = "Unit updated. Changes apply across modes."; }, p.CanConfirm);
                Button(confirmation, "Cancel", () => menu.Collection.Cancel());
                return;
            }
        }

        private void BuildCollectionRoster(VisualElement parent, CollectionView view)
        {
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
                    Text(tile, (u.Unit.Definition.Roster.IsApex ? "APEX · " : "STANDARD · ") + (u.Unit.Owned ? "Rank " + u.Unit.Rank : "Locked"), "detail");
                    Text(tile, u.Unit.Owned && u.Advance.Block == CollectionBlock.MaximumRank ? u.Fragments + " fragments held" : u.Fragments + " / " + (u.Unit.Owned ? u.Advance.Cost : u.Unlock.Cost) + " fragments", "hint");
                    Text(tile, u.Unit.Owned && u.Advance.Block == CollectionBlock.MaximumRank ? "Maximum test rank" : "Inspect unit  ›", "tile-state");
                }
            }
        }

        private static void BuildUnitStats(VisualElement parent, PreparationUnit unit)
        {
            var stats = Row(parent); stats.AddToClassList("stat-row");
            Stat(stats, "HEALTH", unit.Health.ToString()); Stat(stats, "ARMOR", unit.Armor.ToString());
            Stat(stats, "INITIATIVE", unit.Initiative.ToString()); Stat(stats, "MOVEMENT", unit.Movement.ToString("0.##"));
            var attack = unit.Abilities.Single(a => a.Slot == AbilitySlot.NormalAttack).Effect;
            Stat(stats, "ATTACK POWER", attack.Amount.ToString()); Stat(stats, "ATTACK RANGE", attack.Range.ToString("0.##"));
            Text(parent, "Power is before target defenses. Initiative changes turn order, not the number of turns.", "hint");
        }

        private static void BuildAbilityKit(VisualElement parent, PreparationUnit unit)
        {
            Text(parent, "FOUR ABILITY SLOTS", "eyebrow");
            foreach (var ability in unit.Abilities.OrderBy(a => a.Slot))
            {
                var card = Card(parent, "ability-card");
                Text(card, AbilityName(ability.Slot), "section");
                if (ability.Slot == AbilitySlot.Passive)
                {
                    Text(card, ability.IsPassivePlaceholder ? "Not implemented in this test kit." : "Automatic · " + ability.Passive.Trigger + " · applies " + ability.Passive.StatusId, "detail");
                    Text(card, "No action required", "hint");
                    continue;
                }
                var effect = ability.Effect;
                Text(card, (effect.HasHealthEffect ? effect.Kind + " power " + effect.Amount + " · " : "") + "Range " + effect.Range, "detail");
                Text(card, "Target: " + (effect.Target == AbilityTarget.AllyOrSelf ? "ally or self" : effect.Target.ToString().ToLowerInvariant()) + (effect.StatusId == null ? "" : " · applies " + effect.StatusId), "hint");
                string timing = "Available each turn";
                if (ability.Slot == AbilitySlot.Main)
                    timing = "Reusable · ready again " + unit.Availability.MainCooldownTurns + " of this unit's turns after use";
                else if (ability.Slot == AbilitySlot.Signature)
                {
                    switch (unit.Availability.SignatureRequirement)
                    {
                        case SignatureReadiness.ReadyAtDeployment: timing = "Ready at deployment"; break;
                        case SignatureReadiness.AfterNormalAttack: timing = "Ready after this unit uses its normal attack"; break;
                        case SignatureReadiness.AfterMainAbility: timing = "Ready after this unit uses its main ability"; break;
                        default: timing = "Requires its authored battle condition"; break;
                    }
                    timing = "Once per battle · " + timing;
                }
                Text(card, timing + " · Costs one action", "ability-timing");
            }
        }

        private void BuildResults(VisualElement parent)
        {
            var r = menuView.Results;
            string attempt = r.Result.AttemptId;
            bool victory = r.Result.Outcome == MissionOutcome.Victory;
            var summary = Card(parent, "result-summary");
            summary.EnableInClassList("defeat-summary", !victory);
            Text(summary, "BATTLE COMPLETE", "eyebrow");
            Text(summary, victory ? "Victory" : "Defeat", "display-title");
            Text(summary, MissionName(r.Result.MissionId) + " · Round " + r.Result.Round, "section");
            Text(summary, victory ? "Objective achieved." : "Regroup and try a new approach.", "detail");
            Text(summary, "Your units and progression are retained.", "result-assurance");
            Text(summary, r.Claimed ? "SAVED" : "READY TO SAVE", "result-status");

            var rewards = Card(parent, "result-rewards");
            Text(rewards, !victory ? "Battle rewards" : r.IsFirstClear ? "First-clear rewards" : "Replay rewards", "section");
            if (r.Grants.Count == 0) Text(rewards, "No resources awarded this attempt.", "detail");
            foreach (var grant in r.Grants)
            {
                var row = Row(rewards); row.AddToClassList("result-reward-row");
                var identity = new VisualElement(); identity.AddToClassList("reward-identity"); row.Add(identity);
                Text(identity, ResourceName(grant.ResourceId), "detail");
                Text(identity, (r.Claimed ? "Total held: " : "Currently held: ") + menuView.Progress.Balance(grant.ResourceId), "hint");
                Text(row, "+" + grant.Amount, "reward-amount");
            }
            Text(rewards, r.Claimed ? r.Grants.Count == 0 ? "Your battle result is saved." : "These rewards are saved to your profile." : "Save this result before continuing.", "hint");
            Primary(rewards, r.Claimed ? "Result saved" : r.Grants.Count == 0 ? "Save result" : "Collect rewards", () => {
                menu.ClaimRewards(attempt); menuScroll = null; message = "Result saved. Choose your next step.";
            }, r.CanClaim);

            if (!r.Claimed)
            {
                Text(parent, "After saving, continue your campaign, replay, or review your units.", "hint");
                return;
            }

            var collection = menu.Collection.Read();
            foreach (var unit in collection.Factions.SelectMany(f => f.Units).Where(u => r.Grants.Any(g => g.ResourceId == u.FragmentResourceId)))
            {
                var progress = Card(parent, "reward-unit-progress");
                string id = unit.Unit.Definition.Id;
                var offer = unit.Unit.Owned ? unit.Advance : unit.Unlock;
                bool nextRank = offer.RankAfter > offer.RankBefore;
                bool available = !unit.Unit.Owned || nextRank;
                bool enough = available && unit.Fragments >= offer.Cost;
                Text(progress, UnitName(id) + " · " + (unit.Unit.Owned ? "Rank " + unit.Unit.Rank : "Locked"), "section");
                Text(progress, available ? unit.Fragments + " / " + offer.Cost + " fragments" : unit.Fragments + " fragments held", "detail");
                if (available) Meter(progress, unit.Fragments, Math.Max(1, offer.Cost));
                Text(progress, enough ? unit.Unit.Owned ? "Fragments ready for advancement" : "Fragments ready to unlock" : available ? offer.MissingFragments + " more fragments needed" : "No further advancement available in this test kit.", "hint");
                Button(progress, enough ? "Review unit  ›" : "Inspect unit  ›", () => LeaveResults(attempt, Page.Collection, unitId: id));
            }

            var next = Card(parent, "result-next");
            Text(next, "WHAT’S NEXT", "eyebrow");
            var mission = menuView.Missions.Single(m => m.Definition.Id == r.Result.MissionId);
            var campaign = menuView.Campaigns.FirstOrDefault(c => c.CampaignId == mission.CampaignId);
            var nextMission = campaign?.AvailableMissions.Select(id => menuView.Missions.Single(m => m.Definition.Id == id))
                .FirstOrDefault(m => m.Available && !m.IsReplay);
            if (campaign != null && campaign.IsComplete && !campaign.RewardsClaimed)
            {
                Text(next, "Campaign complete", "section");
                Text(next, "Your separate, one-time campaign rewards are ready to collect from Home.", "detail");
                Primary(next, "Review campaign rewards  ›", () => LeaveResults(attempt, Page.Home));
            }
            else if (victory && nextMission != null)
                Primary(next, "Next mission · " + MissionName(nextMission.Definition.Id), () => LeaveResults(attempt, Page.Briefing, nextMission.Definition.Id));
            if (mission.Available)
                Button(next, victory ? "Replay mission" : "Prepare another attempt", () => LeaveResults(attempt, Page.Briefing, r.Result.MissionId));
            Button(next, "View unit collection", () => LeaveResults(attempt, Page.Collection));
            Button(next, "Browse campaigns", () => LeaveResults(attempt, Page.Battles));
            Button(next, "Return Home", () => LeaveResults(attempt, Page.Home));
        }

        private void LeaveResults(string attempt, Page destinationPage, string missionId = null, string unitId = null)
        {
            menu.ReturnToSelection(attempt);
            inBattle = false; inspectedUnit = unitId;
            Navigate(destinationPage);
            if (missionId != null) SelectMission(missionId);
        }

        private void BuildBattle()
        {
            var b = battleView.Battle;
            var top = Card(content, "battle-header");
            var line = Row(top); line.AddToClassList("battle-title-row");
            var actor = b.Units.FirstOrDefault(u => u.Id == b.Activation?.UnitId);
            Button(line, "Home", () => { input.Cancel(); inBattle = false; battleDetails = false; Navigate(Page.Home); }, !battleView.IsAnimating);
            Text(line, string.Join(" · ", b.Objectives.Select(o => (o.Kind == ObjectiveKind.Stabilize ? "Stabilize" : "Defeat enemies") + " " + o.Progress + "/" + o.Required)), "objective-text");
            Button(line, battleDetails ? "Close" : "Info", () => { battleDetails = !battleDetails; });
            var stage = new VisualElement(); stage.AddToClassList("battle-stage"); content.Add(stage);
            surface = new VisualElement { name = "battle-surface" }; surface.style.flexGrow = 1; surface.style.minHeight = 100;
            surface.style.overflow = Overflow.Hidden; stage.Add(surface);
            surface.RegisterCallback<PointerDownEvent>(e => {
                if (e.button != 0 || !e.isPrimary || pointer != -1) { pointer = -1; return; }
                pointer = e.pointerId; pointerStart = e.position;
            });
            surface.RegisterCallback<PointerCancelEvent>(_ => pointer = -1);
            surface.RegisterCallback<PointerLeaveEvent>(_ => pointer = -1);
            surface.RegisterCallback<PointerUpEvent>(e => {
                bool tap = pointer == e.pointerId && e.button == 0 && e.isPrimary && Vector2.Distance(pointerStart, (Vector2)e.position) <= 12;
                pointer = -1; e.StopPropagation();
                if (tap && !dirty && playback == null && !suspended && !battleDetails) Guard(() => Pick((Vector2)e.position));
            });
            field.Show(battleView, input.ReadPlanning(), catalog.Missions[b.MissionId].Objectives, surface);
            if (b.Activation != null)
            {
                if (announcedAttempt != b.AttemptId || announcedActivation != b.Activation.ActivationId)
                { announcedAttempt = b.AttemptId; announcedActivation = b.Activation.ActivationId; turnCueUntil = Time.unscaledTime + 1.6f; }
                turnCue = Text(surface, (actor?.IsEnemyControlled == true ? "ENEMY TURN" : "YOUR TURN") + " · " + UnitName(b.Activation.UnitId), "turn-cue");
                turnCue.pickingMode = PickingMode.Ignore;
                turnCue.style.display = Time.unscaledTime < turnCueUntil ? DisplayStyle.Flex : DisplayStyle.None;
            }
            // Fixed footer height prevents previews changing the camera viewport under a second tap.
            var footer = new ScrollView(ScrollViewMode.Vertical); footer.AddToClassList("battle-dock");
            footer.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            stage.Add(footer);
            controls = new VisualElement(); controls.AddToClassList("controls"); footer.Add(controls);
            BuildControls();
            if (battleDetails)
            {
                var details = new ScrollView(ScrollViewMode.Vertical); details.AddToClassList("battle-details");
                details.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
                details.RegisterCallback<PointerUpEvent>(e => e.StopPropagation());
                surface.Add(details);
                Text(details, MissionName(b.MissionId) + " · Round " + b.Round, "section");
                Text(details, "Next: " + string.Join(" → ", b.UpcomingTurns.Take(4).Select(UnitName)), "detail");
                var inspected = b.Units.FirstOrDefault(u => u.Id == battleView.SelectedUnitId) ?? actor;
                if (inspected?.Health != null) Text(details, UnitName(inspected.Id) + " · HP " + inspected.Health.CurrentHealth + "/" + inspected.Health.MaximumHealth + " · Armor " + inspected.EffectiveArmor, "detail");
                foreach (var action in b.Actions.Where(a => a.Slot != AbilitySlot.Passive))
                {
                    var ability = actor?.Abilities.FirstOrDefault(a => a.Slot == action.Slot);
                    Text(details, AbilityName(action.Slot) + " · " + AbilityReadiness(action, actor) + (ability == null ? "" : " · Range " + ability.Range), "detail");
                }
                foreach (var objective in input.ReadPlanning().Objectives)
                    Text(details, "Stabilize · " + ObjectiveReadiness(objective.Failure), "detail");
                Text(details, "Tap ground to plan movement. Choose an attack, then a marked target. Tap the preview again or Confirm. Every health strip stays visible. Orders always use the current turn’s unit.", "detail");
                Button(details, "Back to battle", () => battleDetails = false);
            }
        }

        private void BuildControls()
        {
            controls.Clear();
            var b = battleView.Battle;
            bool ready = !battleView.IsAnimating && !suspended && !battleDetails;
            var activeUnit = b.Units.FirstOrDefault(u => u.Id == b.Activation?.UnitId);
            Text(controls, (activeUnit?.IsEnemyControlled == true ? "ENEMY · " : "") + UnitName(b.Activation?.UnitId) + " · Move " + (b.Activation?.MovementRemaining.ToString("0.#") ?? "0") + " · " + (b.Activation?.PrimaryActionAvailable == true ? "1 action" : "Action spent"), "actor-status");
            var row = Row(controls); row.AddToClassList("ability-row");
            if (battleView.Pending != null)
            {
                var p = battleView.Pending;
                string preview = p.Kind == IntentKind.Movement ? "Move · " + p.Movement.Cost.ToString("0.#") + (p.RequestedDestination.HasValue && !p.RequestedDestination.Value.Equals(p.Movement.Path.Last()) ? " · partial route" : "") : p.Kind == IntentKind.Interaction ? "Stabilize objective" : TargetSummary(p.Target);
                Text(row, preview, "compact-preview");
                Primary(row, "Confirm", () => Handle(input.Confirm(p.Id)), ready);
                Button(row, "Cancel", () => Handle(input.Cancel()), ready);
            }
            else
            {
                foreach (var a in b.Actions.Where(a => a.Slot != AbilitySlot.Passive))
                {
                    var slot = a.Slot;
                    var button = Button(row, AbilityName(slot), () => Handle(input.SelectAbility(slot)), ready && a.CanSelect);
                    button.tooltip = AbilityReadiness(a, activeUnit);
                    button.EnableInClassList("selected", battleView.SelectedAbility == slot);
                }
            }
            row = Row(controls); row.AddToClassList("utility-row");
            Button(row, "Move", () => Handle(input.Cancel()), ready && b.Phase == BattleSessionPhase.PlayerInput)
                .EnableInClassList("selected", !battleView.SelectedAbility.HasValue && battleView.Pending == null);
            foreach (var objective in input.ReadPlanning().Objectives.Where(o => b.Objectives.Any(x => x.Id == o.Id && x.Status == ObjectiveStatus.Active)))
            {
                string id = objective.Id;
                var button = Button(row, "Stabilize", () => Handle(input.TapObjective(id)), ready && objective.CanInteract);
                button.tooltip = ObjectiveReadiness(objective.Failure);
            }
            long activation = b.Activation?.ActivationId ?? 0;
            Button(row, "End turn", () => Handle(input.EndTurn(activation)), ready && b.Phase == BattleSessionPhase.PlayerInput);
            string hint = battleView.IsAnimating ? "Resolving…" : b.Phase != BattleSessionPhase.PlayerInput ? "Enemy turn…" : battleView.Pending != null ? "Tap the preview again or Confirm" : battleView.SelectedAbility.HasValue ? (battleView.Targets.Any(t => t.IsValid) ? "Tap a marked target" : "No valid targets · Move to a clear position") : "Tap ground to move";
            Text(controls, battleRejection ?? hint, "battle-message");
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
            battleRejection = result.Outcome == InputOutcome.Rejected ? RejectionReason(result) : null;
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
            var frames = field.Play(result.Update, reducedMotion ? 0 : .85f);
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
                heading.style.display = DisplayStyle.Flex;
                root.RemoveFromClassList("battle-mode");
                root.RemoveFromClassList("home-mode");
                root.RemoveFromClassList("campaign-mode");
                root.RemoveFromClassList("results-mode");
                root.RemoveFromClassList("unit-mode");
                root.RemoveFromClassList("briefing-mode");
                homeBackdrop.style.display = DisplayStyle.None;
                if (playback != null) { StopCoroutine(playback); playback = null; }
                Debug.LogException(e, this);
                if (notice != null) notice.text = "Playtest stopped: " + e.Message;
                content.Clear(); surface = null; field?.Hide();
                Text(content, "Your save has not been reset. See the Console for the error. Stop Play mode before copying the save folder for diagnosis.", "detail");
                Text(content, savePath ?? "", "detail");
                var diagnosticsScroll = new ScrollView(ScrollViewMode.Vertical);
                diagnosticsScroll.style.flexGrow = 1; diagnosticsScroll.style.minHeight = 0;
                content.Add(diagnosticsScroll); BuildDiagnostics(diagnosticsScroll);
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
                var button = Button(nav, "", () => Navigate(destinationPage), !activeBattle || target == Page.Home || target == Page.Settings);
                button.AddToClassList("nav-button");
                HomeIcon(button, target == Page.Home ? "home" : target == Page.Settings ? "settings" : target == Page.Collection ? "units" : "routes");
                Text(button, label, "nav-label"); button.tooltip = label;
                button.EnableInClassList("selected", page == target || target == Page.Battles && page == Page.Missions);
            }
        }
        private void BuildSettings(VisualElement parent)
        {
            Text(parent, "Settings & help", "display-title");
            Text(parent, PlaytestRevision.Id + " · " + PlaytestRevision.Features, "detail");
            BuildDiagnostics(parent);
            var motion = Card(parent);
            Text(motion, "Battle animations", "section");
            Text(motion, "Reduced motion skips movement and action playback without changing the battle rules.", "detail");
            Button(motion, reducedMotion ? "Reduced motion: ON" : "Reduced motion: OFF", () => {
                reducedMotion = !reducedMotion; PlayerPrefs.SetInt("Ninefold.ReducedMotion", reducedMotion ? 1 : 0); PlayerPrefs.Save();
            });
            var help = Card(parent);
            Text(help, "How to play", "section");
            Text(help, "1. Tap ground to preview a move.\n2. Tap the same destination again, or select Confirm.\n3. Select an ability, then a marked target. Review the effect before confirming.\n4. End your turn when ready. There is no timer.", "detail");
            Text(help, "Long moves stop at your remaining movement allowance. Walls, units and arena boundaries still limit valid destinations.", "detail");
            Text(help, "Cyan units are allies; red units are enemies. Gold marks objectives and valid targets. Striped ground costs extra movement.", "detail");
            Text(parent, "DEVELOPMENT BUILD", "eyebrow");
            Text(parent, "Units, maps and badges in this build are abstract test content. They are not the final Ninefold art or canonical faction designs.", "detail");
        }
        private static string AbilityName(AbilitySlot slot) => slot == AbilitySlot.NormalAttack ? "Attack" : slot == AbilitySlot.Main ? "Main ability" : slot == AbilitySlot.Passive ? "Passive" : "Signature";
        private static string TargetSummary(TargetPreview preview)
        {
            var health = preview.Effect.Health;
            if (health == null) return UnitName(preview.TargetId) + " · Apply status " + preview.Effect.Status?.StatusId;
            int change = health.HealthAfter-health.HealthBefore;
            return UnitName(preview.TargetId) + " · " + (change < 0 ? -change + " damage" : change > 0 ? change + " healing" : "No health change") + "\nHP " + health.HealthBefore + " → " + health.HealthAfter + (health.HealthAfter == 0 ? " · Defeated" : "");
        }
        public static string ObjectiveReadiness(InteractionFailure failure)
        {
            switch (failure)
            {
                case InteractionFailure.None: return "Ready · 1 action";
                case InteractionFailure.ActionSpent: return "Action spent";
                case InteractionFailure.OutOfReach: return "Move closer";
                case InteractionFailure.Obstructed: return "Path blocked";
                case InteractionFailure.IneligibleActor: return "Needs another unit";
                case InteractionFailure.Unavailable: return "Unavailable";
                default: return "Wait for your turn";
            }
        }
        private string AbilityReadiness(BattleActionView action, BattleUnitView actor)
        {
            if (action.Block == ActionBlock.NotPlayerTurn) return "Wait for turn";
            if (action.CanSelect) return action.Slot == AbilitySlot.Signature ? "Ready · once" : "Ready · 1 action";
            switch (action.RuleFailure)
            {
                case AbilityUseFailure.PrimaryActionSpent: return "Action spent";
                case AbilityUseFailure.MainOnCooldown: return "Cooldown: " + actor?.Kit?.MainCooldownRemaining + " turns";
                case AbilityUseFailure.SignatureNotReady:
                    if (actor != null && catalog.Units.TryGetValue(actor.Id, out var unit))
                    {
                        switch (catalog.Kits[unit.KitId].Availability.SignatureRequirement)
                        {
                            case SignatureReadiness.AfterNormalAttack: return "Attack to ready";
                            case SignatureReadiness.AfterMainAbility: return "Main to ready";
                        }
                    }
                    return "Condition needed";
                case AbilityUseFailure.SignatureAlreadyUsed: return "Used this battle";
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
            b.AddToClassList("game-button"); b.SetEnabled(enabled); parent.Add(b); return b;
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
