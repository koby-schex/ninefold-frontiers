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
        private VisualElement root, safe, content, surface, controls, heading, homeBackdrop, battleRadar;
        private Label notice;
        private ScrollView menuScroll;
        private Page renderedPage;
        private string renderedUnit;
        private bool renderedOffer;
        private string savePath, inspectedUnit, message = "Your progress saves automatically.";
        private enum CollectionFilter { All, Owned, Locked }
        private CollectionFilter collectionFilter = CollectionFilter.All;
        private bool collectionDetailsExpanded;
        private bool battleExitPrompt;
        private enum Page { Home, Battles, Missions, Briefing, Squad, Collection, Settings }
        private Page page;
        private string selectedCampaign, squadInspection;
        private bool dirty, fatal, suspended, reducedMotion, inBattle, legacyProfile, worldFiveSlice;
        private bool showDiagnostics;
        private Coroutine playback;
        private int pointer = -1;
        private Vector2 pointerStart;
        private bool battleDetails;
        private bool sharedControlsLoaded;
        private string battleRejection;
        private string lastTapUnitId;
        private float lastTapUnitTime;
        private const float AttackDoubleTapWindow = 0.65f;

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
            // Opt-in brand primitives load before approved per-screen styles. No screen
            // acquires nf- classes in this pass, so existing presentation is unchanged.
            var foundationStyles = Resources.Load<StyleSheet>(PlaytestRevision.FoundationResource);
            if (foundationStyles != null) root.styleSheets.Add(foundationStyles);
            else Debug.LogWarning("Ninefold interface foundation styles are missing. Check project import.", this);
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
            // Home-only accents come after shared controls so the approved sanctuary
            // can adopt the interface foundation without changing any other screen.
            var homePolish = Resources.Load<StyleSheet>("Home/HomePolish");
            if (homePolish != null) root.styleSheets.Add(homePolish);
            else Debug.LogWarning("Home polish styles are missing. Check project import.", this);
            // Scoped to the campaign selection overview. Load last so its compact
            // cards and nav intentionally override the older full-screen prototype.
            var campaignClarity = Resources.Load<StyleSheet>("Home/CampaignClarity");
            if (campaignClarity != null) root.styleSheets.Add(campaignClarity);
            else Debug.LogWarning("Campaign clarity styles are missing. Check project import.", this);
            // Mission list and briefing get a strictly scoped layer of their own.
            var missionClarity = Resources.Load<StyleSheet>("Home/MissionClarity");
            if (missionClarity != null) root.styleSheets.Add(missionClarity);
            else Debug.LogWarning("Mission clarity styles are missing. Check project import.", this);
            // Collection-specific density and disclosure rules load last.
            // They never affect squad cards, battle or other menu views.
            var collectionClarity = Resources.Load<StyleSheet>("Home/CollectionClarity");
            if (collectionClarity != null) root.styleSheets.Add(collectionClarity);
            else Debug.LogWarning("Collection clarity styles are missing. Check project import.", this);
            // Consolidate bottom tabs and the remaining non-battle menu density.
            // Loaded after all screen-specific sheets for explicit priority.
            var mobileShell = Resources.Load<StyleSheet>("Home/MobileShell");
            if (mobileShell != null) root.styleSheets.Add(mobileShell);
            else Debug.LogWarning("Mobile shell styles are missing. Check project import.", this);
            // Final presentation layer: native game-lobby actions and tactical
            // console UI. No combat HUD selectors or new canon art.
            var gameApp = Resources.Load<StyleSheet>("Home/GameApp");
            if (gameApp != null) root.styleSheets.Add(gameApp);
            else Debug.LogWarning("Game app styles are missing. Check project import.", this);
            // Scoped visual-first primitives. The separate 3D-model pipeline
            // may provide textures through FrontiersPreviewRegistry later.
            var visualFoundation = Resources.Load<StyleSheet>("Home/VisualFoundation02");
            if (visualFoundation != null) root.styleSheets.Add(visualFoundation);
            else Debug.LogWarning("Visual foundation 02 styles are missing. Check project import.", this);
            // Roster thumbnails belong to Collection only and are safely
            // optional until production preview textures are registered.
            var collectionVisual = Resources.Load<StyleSheet>("Home/CollectionVisual03");
            if (collectionVisual != null) root.styleSheets.Add(collectionVisual);
            else Debug.LogWarning("Collection visual styles are missing. Check project import.", this);
            // Applies only to Campaign overview, Mission Path and Briefing.
            // Unit previews/model systems and battlefield gameplay are untouched.
            var campaignMissionVisual = Resources.Load<StyleSheet>("Home/CampaignMissionVisual03");
            if (campaignMissionVisual != null) root.styleSheets.Add(campaignMissionVisual);
            else Debug.LogWarning("Campaign/mission visual styles are missing. Check project import.", this);
            // Battle-only, read-only objective wayfinding. The Work chat's
            // model and rendering assets are intentionally untouched.
            var battleGuidance = Resources.Load<StyleSheet>("Home/BattleObjectiveGuidance01");
            if (battleGuidance != null) root.styleSheets.Add(battleGuidance);
            else Debug.LogWarning("Battle objective guidance styles are missing. Check project import.", this);
            var nativeMobile = Resources.Load<StyleSheet>("Home/NativeMobileExperience01");
            if (nativeMobile != null) root.styleSheets.Add(nativeMobile);
            else Debug.LogWarning("Native mobile experience styles are missing. Check project import.", this);
            // Unified primary-tab shell is additive and leaves the approved
            // sanctuary source image and all Work-owned 3D assets untouched.
            var mainShell = Resources.Load<StyleSheet>("Home/MainShellConvergence01");
            if (mainShell != null) root.styleSheets.Add(mainShell);
            else Debug.LogWarning("Main shell convergence styles are missing. Check project import.", this);
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
            // Saved encounters may be inspected from other menus. The battlefield
            // renders only while deliberately resumed; browsing never replaces it.
            bool home = menuView.Phase != MissionFlowPhase.Results && page == Page.Home &&
                !(menuView.Phase == MissionFlowPhase.Battle && inBattle);
            bool resultsScreen = menuView.Phase == MissionFlowPhase.Results;
            bool browsing = menuView.Phase == MissionFlowPhase.Selection ||
                (menuView.Phase == MissionFlowPhase.Battle && !inBattle);
            bool campaignScreen = resultsScreen || page == Page.Settings ||
                (browsing && (page == Page.Battles || page == Page.Missions ||
                    page == Page.Briefing || page == Page.Squad || page == Page.Collection));
            root.EnableInClassList("campaign-mode", campaignScreen);
            // Campaign overview has its own restrained hierarchy; mission briefing
            // and squad pages keep their existing presentation.
            root.EnableInClassList("campaign-overview-mode",
                browsing && page == Page.Battles);
            root.EnableInClassList("mission-path-mode",
                browsing && page == Page.Missions);
            root.EnableInClassList("mission-briefing-clarity-mode",
                menuView.Phase == MissionFlowPhase.Selection && page == Page.Briefing);
            root.EnableInClassList("results-mode", resultsScreen);
            root.EnableInClassList("unit-mode", browsing && (page == Page.Squad || page == Page.Collection));
            // Collection UI refinement is separate from Squad and the battle HUD.
            root.EnableInClassList("collection-clarity-mode",
                browsing && page == Page.Collection);
            // The remaining prototype-heavy menu screens use one consistent
            // compact portrait shell; combat and polished screens are excluded.
            root.EnableInClassList("support-menu-mode",
                resultsScreen || page == Page.Settings ||
                (menuView.Phase == MissionFlowPhase.Selection && page == Page.Squad));
            root.EnableInClassList("briefing-mode", campaignScreen && page == Page.Briefing);
            root.EnableInClassList("home-mode", home);
            // Home, Campaigns, Units roster and Settings are sibling tabs in
            // one game. The backdrop is shared atmospheric chrome, not
            // campaign-specific artwork or a claimed canonical unit preview.
            bool mainTab = !resultsScreen && !home &&
                !(menuView.Phase == MissionFlowPhase.Battle && inBattle) &&
                (page == Page.Battles || page == Page.Settings ||
                    (page == Page.Collection && inspectedUnit == null));
            root.EnableInClassList("primary-tab-mode", mainTab);
            bool ambientMenu = !resultsScreen && !home &&
                !(menuView.Phase == MissionFlowPhase.Battle && inBattle) &&
                (page == Page.Battles || page == Page.Missions ||
                    page == Page.Briefing || page == Page.Squad ||
                    page == Page.Collection || page == Page.Settings);
            root.EnableInClassList("ambient-menu-mode", ambientMenu);
            heading.style.display = DisplayStyle.None;
            homeBackdrop.style.display = home || ambientMenu ? DisplayStyle.Flex : DisplayStyle.None;
            root.EnableInClassList("native-screen-mode",
                !home && !(menuView.Phase == MissionFlowPhase.Battle && inBattle));
            root.EnableInClassList("battle-mode", menuView.Phase == MissionFlowPhase.Battle && inBattle);
            root.EnableInClassList("visual-hud-mode", menuView.Phase == MissionFlowPhase.Battle && inBattle);
            if (menuView.Phase == MissionFlowPhase.Battle && inBattle)
            {
                content.style.backgroundColor = Color.clear;
                input = menu.ResumeBattle(); battleView = input.Read();
                BuildBattle();
            }
            else
            {
                content.style.backgroundColor = ambientMenu ? Color.clear :
                    page == Page.Collection ? new Color(.063f, .094f, .078f)
                    : page == Page.Settings ? new Color(.068f, .077f, .077f)
                    : new Color(.045f, .075f, .071f);
                input = null; battleView = null; field.Hide();
                if (home)
                {
                    content.style.backgroundColor = Color.clear;
                    BuildHomeScreen();
                    return;
                }
                BuildMobilePageHeader(content);
                var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("page-scroll");
                scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                content.Add(scroll);
                if (menuView.Phase == MissionFlowPhase.Results) BuildResults(scroll);
                else if (page == Page.Settings) BuildSettings(scroll);
                else if (page == Page.Collection) BuildCollection(scroll);
                else if (page == Page.Briefing) BuildBriefing(scroll);
                else if (page == Page.Squad) BuildSquad(scroll);
                else if (page == Page.Missions) BuildMissionList(scroll);
                else BuildSelection(scroll);
                menuScroll = scroll; renderedPage = page; renderedUnit = inspectedUnit; renderedOffer = offerOpen;
                scroll.schedule.Execute(() => scroll.scrollOffset = scrollOffset);
                // Even nested briefing / squad screens retain a reliable way Home
                // or to another main tab; the fixed deploy dock remains functional.
                if (menuView.Phase != MissionFlowPhase.Results) BuildNavigation(content);
            }
        }

        private static string CampaignName(string id) => id == WorldFivePlayableSlice.CampaignId
            ? "World Five · Opening Prototype" : id == "fixture-starter" ? "Test campaign A" : "Test campaign B";

        private void ClaimCampaign(string id)
        {
            menu.ClaimCampaign(id);
            message = "Completion rewards collected. Your campaign progress is saved.";
        }

        private void BuildHomeScreen()
        {
            // A game lobby, not a web landing page. Preserve the approved sanctuary
            // art; a small functional HUD and mission-command dock frame the view.
            var masthead = new VisualElement();
            masthead.AddToClassList("home-masthead");
            masthead.AddToClassList("game-lobby-masthead");
            content.Add(masthead);
            var hud = Row(masthead);
            hud.AddToClassList("game-lobby-hud");
            Text(hud, "WORLD 05 · SANCTUARY", "game-lobby-hud-world");
            Text(hud, menuView.Progress.OwnedUnits.Count + " UNITS", "game-lobby-hud-roster");
            Text(masthead, "NINEFOLD", "home-wordmark");
            Text(masthead, "FRONTIERS", "home-submark");

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("home-scroll");
            scroll.AddToClassList("game-lobby-scroll");
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            content.Add(scroll);
            scroll.contentContainer.style.flexGrow = 1;
            scroll.contentViewport.RegisterCallback<GeometryChangedEvent>(evt =>
                scroll.contentContainer.style.minHeight = Mathf.Max(0, evt.newRect.height));
            var vista = new VisualElement { pickingMode = PickingMode.Ignore };
            vista.AddToClassList("home-vista");
            vista.AddToClassList("game-lobby-vista");
            scroll.Add(vista);
            // Subtle gameplay wayfinding, not a map location or invented emblem.
            Text(vista, "THE CONTINUANCE  /  WORLD FIVE", "game-lobby-scene-label").pickingMode = PickingMode.Ignore;

            var panel = new VisualElement();
            panel.AddToClassList("home-actions");
            panel.AddToClassList("game-lobby-dock");
            scroll.Add(panel);
            bool active = menuView.Phase == MissionFlowPhase.Battle;
            var next = menuView.NextCampaignMission;
            string claim = menuView.ClaimableCampaignId;
            string status = active ? "SAVED ENCOUNTER"
                : claim != null ? "COMPLETION REWARD"
                : next != null ? "NEXT DEPLOYMENT" : "READY FOR ORDERS";
            var header = Row(panel); header.AddToClassList("game-lobby-status-line");
            Text(header, "●", "game-lobby-signal");
            Text(header, status, "home-kicker");

            if (active && battleExitPrompt)
            {
                Text(panel, "End this battle?", "home-title").AddToClassList("game-lobby-title");
                Text(panel, "Your saved encounter will end as a defeat, with no battle rewards. Your units, unlocks and completed missions are kept. This cannot be undone.",
                    "home-summary").AddToClassList("game-lobby-description");
                var end = Button(panel, "END BATTLE · CHOOSE ANOTHER  ›", () => {
                    // Full transaction: terminal defeat, zero-reward receipt,
                    // return to selection. Never delete battle files or profiles.
                    menu.AbandonBattle();
                    input = null; inBattle = false; battleExitPrompt = false;
                    Navigate(Page.Home);
                    message = "Battle ended. Choose a campaign or Quick Battle.";
                });
                end.AddToClassList("game-lobby-danger");
                var keep = Button(panel, "KEEP SAVED BATTLE", () => battleExitPrompt = false);
                keep.AddToClassList("game-lobby-cancel");
                BuildNavigation(content);
                return;
            }

            string headline = active ? "Return to battle"
                : claim != null ? "Rewards await"
                : next != null ? "Continue the journey" : "Choose your mission";
            string detail = active ? MissionName(menuView.Resume.MissionId) + " · Checkpoint saved"
                : claim != null ? CampaignName(claim) + " · Ready to claim"
                : next != null ? CampaignName(next.CampaignId) + " · " + MissionName(next.Definition.Id)
                : "Choose a campaign, or enter a quick encounter.";
            Text(panel, headline, "home-title").AddToClassList("game-lobby-title");
            Text(panel, detail, "home-summary").AddToClassList("game-lobby-description");
            if (worldFiveSlice)
                Text(panel, "PROTOTYPE CAMPAIGN · Separate save · Abstract units", "fixture-note");

            Button main;
            string actionLabel;
            if (active)
            {
                actionLabel = "RESUME BATTLE";
                main = Button(panel, "", () => { inBattle = true; message = "Battle resumed."; });
            }
            else if (claim != null)
            {
                actionLabel = "COLLECT REWARDS";
                main = Button(panel, "", () => ClaimCampaign(claim));
            }
            else if (next != null)
            {
                actionLabel = "VIEW NEXT MISSION";
                main = Button(panel, "", () => SelectMission(next.Definition.Id));
            }
            else
            {
                actionLabel = "EXPLORE CAMPAIGNS";
                main = Button(panel, "", () => Navigate(Page.Battles));
            }
            main.AddToClassList("home-primary");
            main.AddToClassList("game-lobby-primary");
            HomeIcon(main, "routes");
            Text(main, actionLabel, "game-lobby-primary-label");
            Text(main, "›", "game-lobby-primary-chevron");

            // A saved game can be browsed read-only. Starting another requires
            // an explicit confirmation, not a silent checkpoint overwrite.
            var secondary = Button(panel, active ? "NEW BATTLE  ›" : "QUICK BATTLE  ›",
                active ? (Action)(() => battleExitPrompt = true)
                    : () => SelectMission("fixture-mixed"));
            secondary.AddToClassList("home-quick-link");
            secondary.AddToClassList("game-lobby-secondary");
            // Always leave this destination tappable. A saved-battle lock must
            // explain itself instead of silently presenting a dead button.
            // When selection is available, take the player DIRECTLY to the
            // six-mission campaign path, not back to the same Home screen.
            var switchMode = Button(panel,
                worldFiveSlice ? "RETURN TO ORIGINAL PLAYTEST  ›" : "TRY WORLD FIVE MISSIONS 001–006  ›",
                OpenCampaignPrototypeFromHome);
            switchMode.AddToClassList("home-quick-link");
            switchMode.AddToClassList("world-five-slice-switch");
            switchMode.tooltip = menuView.Phase == MissionFlowPhase.Selection
                ? "Open the six-mission prototype · independent saved progress"
                : "Resolve your saved battle before changing prototypes";
            if (menuView.Phase == MissionFlowPhase.Battle)
                Text(panel, "To switch playtest campaigns, finish your saved encounter first.",
                    "home-status").AddToClassList("game-lobby-assurance");
            if (active)
                Text(panel, "Campaigns and Units can be explored while your battle is saved.",
                    "home-status").AddToClassList("game-lobby-assurance");
            else if (message != "Your progress saves automatically.")
                Text(panel, message, "home-status").AddToClassList("game-lobby-assurance");

            BuildNavigation(content);
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
            // Present a clear next step without turning a fixture campaign list
            // into a collection of competing, oversized calls to action.
            // Top-level campaign title and atmospheric identity live in the
            // shared hero above the scroll; do not repeat a desktop page title.
            // Actual eligibility remains on each locked campaign card.
            Text(parent, "UNLOCK: 3 STANDARD UNITS / FACTION", "detail")
                .AddToClassList("campaign-overview-intro");
            bool readOnly = menuView.Phase == MissionFlowPhase.Battle;
            if (readOnly)
                Text(parent, "BATTLE SAVED · Browse freely. Finish or end your battle to launch missions or claim rewards.", "detail")
                    .AddToClassList("game-saved-ribbon");

            string recommendedId = menuView.ClaimableCampaignId
                ?? menuView.NextCampaignMission?.CampaignId
                ?? menuView.Campaigns.FirstOrDefault(c => c.CanEnter)?.CampaignId;

            foreach (var c in menuView.Campaigns)
            {
                string id = c.CampaignId;
                var missions = catalog.Campaigns.Single(x => x.Id == id).Missions
                    .Select(node => menuView.Missions.Single(m => m.Definition.Id == node.MissionId)).ToArray();
                var next = missions.FirstOrDefault(m => m.Available && !m.IsReplay);
                bool featured = !readOnly && c.CanEnter && id == recommendedId;
                int ownedStandards = missions.Length > 0 ? Math.Min(3, missions[0].OwnedFactionStandards) : 0;

                var card = Card(parent, "campaign-panel");
                card.AddToClassList("campaign-choice");
                card.EnableInClassList("locked-card", !c.CanEnter);
                card.EnableInClassList("campaign-choice-featured", featured);
                card.EnableInClassList("campaign-choice-locked", !c.CanEnter);
                // The approved World Five sanctuary is only used on the
                // World Five slice card, never as false fixture-faction art.
                card.EnableInClassList("shell-worldfive-campaign", id == WorldFivePlayableSlice.CampaignId);
                FrontiersVisualPrimitives.CampaignArt(card, id, c.CompletedMissions, c.TotalMissions, !c.CanEnter);

                string status = !c.CanEnter ? "LOCKED"
                    : c.IsComplete ? c.RewardsClaimed ? "COMPLETED" : "REWARDS READY"
                    : c.CompletedMissions > 0 ? "IN PROGRESS" : "READY TO BEGIN";
                Text(card, status, "campaign-status").AddToClassList("campaign-choice-status");
                Text(card, CampaignName(id), "campaign-name").AddToClassList("campaign-choice-name");
                Text(card, c.CompletedMissions + " / " + c.TotalMissions + " CLEARED", "detail")
                    .AddToClassList("campaign-choice-progress");
                Meter(card, c.CompletedMissions, c.TotalMissions);

                if (!c.CanEnter)
                {
                    // A locked campaign still permits a read-only mission preview;
                    // ownership and Apex restrictions remain enforced by the core.
                    Text(card, ownedStandards + " / 3 standard units · Unlock more in Units.", "lock-reason")
                        .AddToClassList("campaign-choice-lock");
                    var preview = Button(card, "Preview missions  ›", () => { selectedCampaign = id; Navigate(Page.Missions); });
                    preview.AddToClassList("campaign-choice-secondary");
                    continue;
                }

                if (c.IsComplete && !c.RewardsClaimed)
                {
                    var claim = Button(card, "Collect rewards  ›", () => ClaimCampaign(id), !readOnly);
                    claim.AddToClassList("campaign-choice-action");
                    if (featured) claim.AddToClassList("primary");
                }
                else if (next != null)
                {
                    Text(card, "NEXT · " + MissionName(next.Definition.Id), "next-mission-label")
                        .AddToClassList("campaign-choice-next");
                    string nextId = next.Definition.Id;
                    var proceed = Button(card, "Continue campaign  ›", () => SelectMission(nextId), !readOnly);
                    proceed.AddToClassList("campaign-choice-action");
                    if (featured) proceed.AddToClassList("primary");
                }
                else if (c.IsComplete)
                {
                    Text(card, "All missions cleared · Replays available", "hint")
                        .AddToClassList("campaign-choice-next");
                }

                // This remains available even when a completion reward is waiting,
                // but it is visually secondary to the current recommended action.
                var viewAll = Button(card,
                    c.IsComplete && c.RewardsClaimed ? "Replay missions  ›" : "View all missions  ›",
                    () => { selectedCampaign = id; Navigate(Page.Missions); });
                viewAll.AddToClassList("campaign-choice-secondary");
            }
            Text(parent, "PLAYTEST · Campaign names and mission counts are temporary.", "fixture-note")
                .AddToClassList("campaign-overview-footnote");
        }

        private void BuildMissionList(VisualElement parent)
        {
            // A compact, legible path from the campaign overview to one briefing.
            // Campaign order and availability continue to come from the core.
            var back = Button(parent, "‹ Campaigns", () => Navigate(Page.Battles));
            back.AddToClassList("back-link"); back.AddToClassList("mission-path-back");
            Text(parent, "CAMPAIGN PATH", "eyebrow").AddToClassList("mission-path-overline");
            Text(parent, CampaignName(selectedCampaign), "display-title").AddToClassList("mission-path-title");
            var progress = menuView.Campaigns.Single(c => c.CampaignId == selectedCampaign);
            bool readOnly = menuView.Phase == MissionFlowPhase.Battle;
            if (readOnly)
                Text(parent, "BATTLE SAVED · Mission launches are paused until this encounter ends.", "detail")
                    .AddToClassList("game-saved-ribbon");
            Text(parent, progress.CompletedMissions + " / " + progress.TotalMissions + " CLEARED", "detail")
                .AddToClassList("mission-path-progress");
            Meter(parent, progress.CompletedMissions, progress.TotalMissions);

            var authored = catalog.Campaigns.Single(c => c.Id == selectedCampaign);
            string nextId = authored.Missions.Select(n => menuView.Missions.Single(m => m.Definition.Id == n.MissionId))
                .FirstOrDefault(m => m.Available && !m.IsReplay)?.Definition.Id;
            int number = 0;
            foreach (var node in authored.Missions)
            {
                var mission = menuView.Missions.Single(m => m.Definition.Id == node.MissionId);
                string missionId = mission.Definition.Id;
                bool highlighted = missionId == nextId;
                var card = Card(parent, "mission-entry");
                card.AddToClassList("mission-path-card");
                card.EnableInClassList("locked-card", !mission.Available);
                card.EnableInClassList("mission-path-current", highlighted);
                card.EnableInClassList("mission-path-locked", !mission.Available);
                var heading = Row(card); heading.AddToClassList("mission-heading");
                // A navigational route marker replaces the oversized text-only
                // mission number; pin state is derived from authoritative progress.
                number++;
                var pin = new VisualElement { pickingMode = PickingMode.Ignore };
                pin.AddToClassList("vmp-route-pin");
                pin.EnableInClassList("vmp-route-cleared", mission.IsReplay);
                pin.EnableInClassList("vmp-route-locked", !mission.Available);
                pin.EnableInClassList("vmp-route-current", highlighted);
                heading.Add(pin);
                Text(pin, mission.IsReplay ? "✓" : number.ToString("00"), "vmp-route-number");
                var identity = new VisualElement(); identity.AddToClassList("mission-identity"); heading.Add(identity);
                Text(identity, !mission.Available ? "LOCKED" : mission.IsReplay ? "CLEARED · REPLAY"
                    : highlighted ? "UP NEXT" : "AVAILABLE", "campaign-status").AddToClassList("mission-path-status");
                Text(identity, MissionName(missionId), "section").AddToClassList("mission-path-name");
                if (mission.Definition.Encounter == EncounterKind.WorldCollision)
                    Text(identity, "WORLD COLLISION", "eyebrow").AddToClassList("vmp-world-collision");
                if (!mission.Available)
                {
                    string reason = mission.FactionRosterLocked
                        ? "Requires 3 standard faction units (" + mission.OwnedFactionStandards + " / 3 owned)."
                        : mission.MissingPrerequisites.Count > 0
                            ? "First complete: " + string.Join(", ", mission.MissingPrerequisites.Select(MissionName))
                            : "This mission is currently unavailable.";
                    Text(card, reason, "lock-reason").AddToClassList("mission-path-lock");
                    // Locked missions remain visible, never selectable or launchable.
                    continue;
                }

                if (highlighted)
                {
                    // Show the real authored battlefield layout only on the next
                    // mission, avoiding heavyweight mini-maps for 100+ entries.
                    var scout = new VisualElement { pickingMode = PickingMode.Ignore };
                    scout.AddToClassList("vmp-next-scout");
                    card.Add(scout);
                    Text(scout, "FIELD SCOUT", "eyebrow").AddToClassList("vmp-scout-label");
                    BuildMapPreview(scout, catalog.Missions[missionId], true);
                    Text(scout, Description(missionId), "detail").AddToClassList("vmp-scout-objective");
                }
                var choose = Button(card, mission.IsReplay ? "Review replay  ›" : "View briefing  ›",
                    () => SelectMission(missionId), !readOnly);
                choose.AddToClassList("mission-path-action");
                if (highlighted) choose.AddToClassList("primary");
            }
            Text(parent, "PLAYTEST · Missions and maps are temporary content.", "fixture-note")
                .AddToClassList("mission-path-footnote");
        }

        private void PreparationHeader(VisualElement parent, bool squad)
        {
            Text(parent, squad ? "1  BRIEFING   /   2  SQUAD" : "1  BRIEFING   /   2  SQUAD NEXT", "step-label");
            Text(parent, MissionName(menuView.SelectedMissionId), "display-title");
        }

        private void BuildBriefing(VisualElement parent)
        {
            var definition = catalog.Missions[menuView.SelectedMissionId];
            // Present what the player is about to do before any secondary detail.
            PreparationHeader(parent, false);
            if (definition.Encounter == EncounterKind.WorldCollision)
                Text(parent, "WORLD COLLISION", "eyebrow").AddToClassList("vmp-world-collision");
            var mission = menuView.Missions.Single(m => m.Definition.Id == definition.Id);
            if (mission.IsReplay)
                Text(parent, "REPLAY · COMPLETED MISSION", "eyebrow")
                    .AddToClassList("mission-brief-context");

            var objective = Card(parent, "briefing-objective");
            objective.AddToClassList("mission-brief-objective");
            Text(objective, "OBJECTIVE", "eyebrow").AddToClassList("mission-brief-overline");
            Text(objective, Description(definition.Id), "detail").AddToClassList("mission-brief-description");

            var terrain = Card(parent, "briefing-terrain");
            terrain.AddToClassList("mission-brief-terrain");
            BuildMapPreview(terrain, definition);

            var rewards = Card(parent);
            rewards.AddToClassList("mission-brief-rewards");
            Text(rewards, mission.IsReplay ? "Replay rewards" : "Victory rewards", "section")
                .AddToClassList("mission-brief-reward-title");
            if (mission.VictoryRewards.Count == 0)
                Text(rewards, "No resource rewards for this mission.", "hint").AddToClassList("mission-brief-reward-note");
            foreach (var grant in mission.VictoryRewards)
            {
                var line = Row(rewards); line.AddToClassList("briefing-reward");
                Text(line, ResourceName(grant.ResourceId), "reward-name");
                Text(line, "+" + grant.Amount, "reward-amount");
            }
            Text(rewards, "Awarded after victory", "hint")
                .AddToClassList("mission-brief-reward-note");

            // Continue remains fixed outside the scroll area. No mission starts,
            // rewards transfer, or squad draft changes on mere briefing view.
            var dock = Card(content, "deploy-dock");
            dock.AddToClassList("mission-brief-dock");
            var deploymentStats = Row(dock);
            deploymentStats.AddToClassList("vmp-deploy-facts");
            FrontiersVisualPrimitives.HudChip(deploymentStats, definition.MinimumSquad + "–" + definition.MaximumSquad, "UNITS");
            FrontiersVisualPrimitives.HudChip(deploymentStats, definition.IsCampaign ? "FACTION" : "MIXED", "SQUAD");
            FrontiersVisualPrimitives.HudChip(deploymentStats, "NONE", "TIMER");
            var chooseSquad = Primary(dock, "CHOOSE SQUAD  ›", () => Navigate(Page.Squad));
            chooseSquad.AddToClassList("mission-brief-next");
        }

        private void BuildSquad(VisualElement parent)
        {
            var definition = catalog.Missions[menuView.SelectedMissionId];
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
            var units = view.Factions.SelectMany(f => f.Units).ToArray();
            bool readOnly = menuView.Phase == MissionFlowPhase.Battle;
            var selected = units.FirstOrDefault(u => u.Unit.Definition.Id == inspectedUnit);
            if (readOnly)
                Text(parent, "BATTLE SAVED · Unit inspection is available. Unlocks and upgrades resume after this battle.", "detail")
                    .AddToClassList("game-saved-ribbon");

            if (selected == null)
            {
                inspectedUnit = null;
                collectionDetailsExpanded = false;
                // The tab hero owns the page title; keep the useful count
                // and native touch filters directly above roster content.
                Text(parent, units.Count(u => u.Unit.Owned) + " of " + units.Length + " unlocked", "detail")
                    .AddToClassList("collection-overview-summary");

                var filters = Row(parent); filters.AddToClassList("collection-filters");
                CollectionFilterButton(filters, "All", CollectionFilter.All);
                CollectionFilterButton(filters, "Owned", CollectionFilter.Owned);
                CollectionFilterButton(filters, "Locked", CollectionFilter.Locked);
                BuildCollectionRoster(parent, view);
                Text(parent, "PLAYTEST · Unit badges and faction labels are temporary.", "fixture-note")
                    .AddToClassList("collection-footnote");
                return;
            }

            // Confirmation is intentionally separate from inspection. No resources
            // are consumed until the player explicitly confirms the pending offer.
            if (view.Pending != null)
            {
                var p = view.Pending;
                var confirmation = Card(parent, "confirmation");
                confirmation.AddToClassList("collection-confirmation");
                Text(confirmation, "REVIEW CHANGE", "eyebrow").AddToClassList("collection-overline");
                Text(confirmation, UnitName(p.UnitId), "display-title").AddToClassList("collection-page-title");
                Text(confirmation, p.Operation == CollectionOperation.Unlock ? "Unlock this unit"
                    : p.Operation == CollectionOperation.Advance ? "Advance this unit's rank"
                    : "Adjust this unit's stats", "detail").AddToClassList("collection-overview-summary");
                Text(confirmation, "BEFORE  →  AFTER", "eyebrow").AddToClassList("collection-overline");
                var comparison = Row(confirmation); comparison.AddToClassList("stat-row");
                Stat(comparison, "HEALTH", p.Before.Health + " → " + p.After.Health);
                Stat(comparison, "ARMOR", p.Before.Armor + " → " + p.After.Armor);
                Stat(comparison, "INITIATIVE", p.Before.Initiative + " → " + p.After.Initiative);
                Stat(comparison, "MOVEMENT", p.Before.Movement + " → " + p.After.Movement);
                Stat(comparison, "RANK", p.RankBefore + " → " + p.RankAfter);
                foreach (var after in p.After.Abilities.Where(a => a.Effect != null && a.Effect.HasHealthEffect))
                {
                    var before = p.Before.Abilities.Single(a => a.Slot == after.Slot);
                    Stat(comparison, AbilityName(after.Slot).ToUpperInvariant() + " POWER",
                        before.Effect.Amount + " → " + after.Effect.Amount);
                }
                Text(confirmation, p.ResourceId == null ? "No fragment cost"
                    : "Spend " + p.Cost + " fragments · " + p.Balance + " → " + (p.Balance - p.Cost) + " remaining",
                    "preview").AddToClassList("collection-confirm-cost");
                Text(confirmation, "Your change is saved across all game modes after confirmation.", "hint")
                    .AddToClassList("collection-confirm-note");
                if (!p.CanConfirm)
                    Text(confirmation, CollectionReason(p), "lock-reason").AddToClassList("collection-detail-reason");
                var confirm = Primary(confirmation, "CONFIRM & SAVE", () => {
                    menu.Collection.Confirm(p.Id);
                    message = "Unit updated. Changes apply across modes.";
                }, p.CanConfirm);
                confirm.AddToClassList("collection-primary");
                var cancel = Button(confirmation, "Cancel", () => menu.Collection.Cancel());
                cancel.AddToClassList("collection-secondary");
                return;
            }

            var detail = Card(parent, "unit-detail");
            detail.AddToClassList("collection-detail-card");
            FrontiersVisualPrimitives.UnitPreview(detail, inspectedUnit, selected.Unit.Definition.Roster.IsApex, selected.Unit.Owned);
            var identity = Row(detail); identity.AddToClassList("collection-detail-identity");
            // The preview now serves as the unit's visual identity. Avoid
            // repeating an oversized prototype ID badge beneath the render.
            var copy = new VisualElement(); copy.AddToClassList("collection-detail-copy"); identity.Add(copy);
            Text(copy, selected.Unit.Definition.Roster.IsApex ? "APEX UNIT" : "STANDARD UNIT", "eyebrow")
                .AddToClassList("collection-overline");
            Text(copy, UnitName(inspectedUnit), "display-title").AddToClassList("collection-detail-title");
            Text(copy, selected.Unit.Owned ? "UNLOCKED · RANK " + selected.Unit.Rank : "LOCKED", "detail")
                .AddToClassList("collection-detail-status");

            var offer = selected.Unit.Owned ? selected.Advance : selected.Unlock;
            if (offer.Block != CollectionBlock.MaximumRank)
            {
                Text(detail, selected.Fragments + " / " + offer.Cost + " fragments · "
                    + (selected.Unit.Owned ? "Next rank" : "To unlock"), "detail")
                    .AddToClassList("collection-fragments");
                Meter(detail, selected.Fragments, Math.Max(1, offer.Cost));
            }
            else
                Text(detail, "Maximum test rank · " + selected.Fragments + " fragments held", "detail")
                    .AddToClassList("collection-fragments");

            Text(detail, offer.CanConfirm
                ? selected.Unit.Owned ? "Ready to advance" : "Ready to unlock"
                : CollectionReason(offer), "hint").AddToClassList("collection-detail-reason");

            var operation = selected.Unit.Owned ? CollectionOperation.Advance : CollectionOperation.Unlock;
            if (offer.Block != CollectionBlock.MaximumRank)
            {
                var upgrade = Button(detail, (selected.Unit.Owned ? "Advance unit" : "Unlock unit")
                    + " · " + offer.Cost + " fragments",
                    () => menu.Collection.Preview(operation, inspectedUnit), offer.CanConfirm);
                upgrade.AddToClassList("collection-primary");
            }
            if (selected.Unit.Owned && selected.Customizations.Any(o => o.Id == "fixture-health"))
            {
                var adjust = Button(detail, selected.Unit.CustomizationId == null
                    ? "Adjust health / armor" : "Reset stat adjustment",
                    () => menu.Collection.Preview(CollectionOperation.Customize, inspectedUnit,
                        selected.Unit.CustomizationId == null ? "fixture-health" : null), !readOnly);
                adjust.AddToClassList("collection-secondary");
            }

            // The full six-stat comparison and four-slot ability kit remain
            // accessible, but are not dumped on players by default.
            var glance = Row(detail); glance.AddToClassList("vif-unit-glance");
            FrontiersVisualPrimitives.HudChip(glance, selected.Unit.Health.ToString(), "HP");
            FrontiersVisualPrimitives.HudChip(glance, selected.Unit.Armor.ToString(), "ARMOR");
            FrontiersVisualPrimitives.HudChip(glance, selected.Unit.Movement.ToString("0.##"), "MOVE");
            var expand = Button(detail, collectionDetailsExpanded ? "Hide stats & abilities  ‹"
                : "View stats & abilities  ›", () => collectionDetailsExpanded = !collectionDetailsExpanded);
            expand.AddToClassList("collection-secondary");
            if (collectionDetailsExpanded)
            {
                var extended = new VisualElement(); extended.AddToClassList("collection-expanded"); detail.Add(extended);
                BuildUnitStats(extended, selected.Unit);
                BuildAbilityKit(extended, selected.Unit);
            }
            Text(parent, "PLAYTEST · Unit badges and stats are provisional.", "fixture-note")
                .AddToClassList("collection-footnote");
        }

        private void CollectionFilterButton(VisualElement parent, string title, CollectionFilter filter)
        {
            var button = Button(parent, title, () => {
                // Prevent retaining an out-of-range scroll position after
                // filtering down to fewer units.
                collectionFilter = filter;
                menuScroll = null;
            });
            button.AddToClassList("collection-filter");
            button.EnableInClassList("selected", collectionFilter == filter);
        }

        private void BuildCollectionRoster(VisualElement parent, CollectionView view)
        {
            bool anyVisible = false;
            foreach (var faction in view.Factions)
            {
                var filtered = faction.Units.Where(u => collectionFilter == CollectionFilter.All
                    || (collectionFilter == CollectionFilter.Owned && u.Unit.Owned)
                    || (collectionFilter == CollectionFilter.Locked && !u.Unit.Owned)).ToArray();
                if (filtered.Length == 0) continue;
                anyVisible = true;
                Text(parent, faction.Id == "fixture-a" ? "Test faction A" : "Test faction B", "section")
                    .AddToClassList("collection-faction-title");
                foreach (var entry in filtered)
                {
                    string id = entry.Unit.Definition.Id;
                    var tile = Button(parent, "", () => {
                        menu.Collection.Cancel();
                        inspectedUnit = id;
                        collectionDetailsExpanded = false;
                    });
                    tile.AddToClassList("collection-roster-entry");
                    tile.AddToClassList("vif-roster-tile");
                    tile.tooltip = UnitName(id) + (entry.Unit.Owned ? " · Unlocked" : " · Locked");
                    var line = Row(tile); line.AddToClassList("collection-roster-row");
                    FrontiersVisualPrimitives.UnitPortrait(line, id,
                        entry.Unit.Definition.Roster.IsApex, entry.Unit.Owned);
                    var identity = new VisualElement(); identity.AddToClassList("collection-roster-copy"); line.Add(identity);
                    Text(identity, UnitName(id), "section").AddToClassList("collection-roster-title");
                    Text(identity, entry.Unit.Owned ? "RANK " + entry.Unit.Rank : "LOCKED", "detail")
                        .AddToClassList("collection-roster-state");
                    var offer = entry.Unit.Owned ? entry.Advance : entry.Unlock;
                    if (offer.Block == CollectionBlock.MaximumRank)
                    {
                        Text(identity, "MAX RANK", "hint").AddToClassList("collection-roster-fragments");
                    }
                    else
                    {
                        // Resource quantity and visual meter reflect the existing
                        // core offer; this never changes balance or eligibility.
                        var progress = Row(identity); progress.AddToClassList("vif-roster-resource");
                        Text(progress, entry.Fragments + " / " + offer.Cost, "hint")
                            .AddToClassList("collection-roster-fragments");
                        if (offer.CanConfirm)
                            Text(progress, "READY", "hint").AddToClassList("vif-roster-ready");
                        Meter(identity, entry.Fragments, Math.Max(1, offer.Cost));
                    }
                    Text(line, "›", "section").AddToClassList("collection-roster-arrow");
                }
            }
            if (!anyVisible)
                Text(parent, "No units in this view.", "detail").AddToClassList("collection-empty-state");
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
            var exit = Button(line, "‹", () => { input.Cancel(); inBattle = false; battleDetails = false; Navigate(Page.Home); }, !battleView.IsAnimating);
            exit.AddToClassList("mobile-battle-back"); exit.tooltip = "Return Home · battle saved";
            // The primary objective always reflects the real objective kind;
            // bonus goals remain a small secondary progress counter.
            BattleObjectiveGuidance.Header(line, b.Objectives);
            long turnId = b.Activation?.ActivationId ?? 0;
            var end = Button(line, "END", () => Handle(input.EndTurn(turnId)),
                !battleView.IsAnimating && b.Phase == BattleSessionPhase.PlayerInput &&
                battleView.Pending == null);
            end.AddToClassList("mobile-battle-end"); end.tooltip = "End this unit's turn";
            var stage = new VisualElement(); stage.AddToClassList("battle-stage"); content.Add(stage);
            surface = new VisualElement { name = "battle-surface" }; surface.style.flexGrow = 1; surface.style.minHeight = 100;
            surface.style.overflow = Overflow.Hidden; stage.Add(surface);
            surface.RegisterCallback<PointerDownEvent>(e => {
                // The inset is informational, never a second command surface.
                if (battleRadar != null && battleRadar.worldBound.Contains(e.position)) { pointer = -1; return; }
                if (e.button != 0 || !e.isPrimary || pointer != -1) { pointer = -1; return; }
                pointer = e.pointerId; pointerStart = e.position;
            });
            surface.RegisterCallback<PointerCancelEvent>(_ => pointer = -1);
            surface.RegisterCallback<PointerLeaveEvent>(_ => pointer = -1);
            surface.RegisterCallback<PointerUpEvent>(e => {
                bool inset = battleRadar != null && battleRadar.worldBound.Contains(e.position);
                bool tap = !inset && pointer == e.pointerId && e.button == 0 &&
                    e.isPrimary && Vector2.Distance(pointerStart, (Vector2)e.position) <= 12;
                pointer = -1; e.StopPropagation();
                if (tap && !dirty && playback == null && !suspended && !battleDetails) Guard(() => Pick((Vector2)e.position));
            });
            var mission = catalog.Missions[b.MissionId];
            field.Show(battleView, input.ReadPlanning(), mission.Objectives, surface, mission.EnvironmentKey);
            // A read-only overhead schematic reinforces world-specific map
            // wayfinding without inventing collision routes or adding UI prose.
            battleRadar = BattleObjectiveGuidance.Radar(surface, mission, b);
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
            footer.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            stage.Add(footer);
            controls = new VisualElement(); controls.AddToClassList("controls"); footer.Add(controls);
            BuildControls();
            if (battleDetails)
            {
                var details = new ScrollView(ScrollViewMode.Vertical); details.AddToClassList("battle-details");
                details.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                details.verticalScrollerVisibility = ScrollerVisibility.Hidden;
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
                Text(details, "Tap a unit once to inspect health, armor and attacks. Double-tap an enemy for a normal attack. Choose Ability or Ult to target special actions. Tap ground to move and objectives to interact.", "detail");
                Button(details, "Back to battle", () => battleDetails = false);
            }
        }

        private void BuildControls()
        {
            controls.Clear();
            var b = battleView.Battle;
            bool ready = !battleView.IsAnimating && !suspended && !battleDetails &&
                b.Phase == BattleSessionPhase.PlayerInput;
            var active = b.Units.FirstOrDefault(u => u.Id == b.Activation?.UnitId);
            // No world-space HP bars: the active unit's vital state is always
            // visible above abilities. Tapping any other unit adds its own
            // inspect-only line, without granting control or spending actions.
            var status = new VisualElement(); status.AddToClassList("mobile-combat-status"); controls.Add(status);
            var current = Row(status); current.AddToClassList("mobile-active-unit");
            Text(current, "ACTIVE  " + UnitName(active?.Id), "mobile-actor-name");
            Text(current, (b.Activation?.MovementRemaining.ToString("0.#") ?? "0") + " MOVE",
                "mobile-actor-move");
            Text(current, b.Activation?.PrimaryActionAvailable == true ? "READY" : "USED",
                "mobile-actor-action");
            if (active?.Health != null)
            {
                var vitals = Row(status); vitals.AddToClassList("mobile-health-row");
                Text(vitals, "HP  " + active.Health.CurrentHealth + " / " +
                    active.Health.MaximumHealth, "mobile-hp-value");
                Meter(vitals, active.Health.CurrentHealth, active.Health.MaximumHealth);
                Text(vitals, "ARMOR  " + (active.EffectiveArmor?.ToString() ?? "—"),
                    "mobile-armor-value");
            }
            var inspected = b.Units.FirstOrDefault(u =>
                u.Id == battleView.SelectedUnitId && u.IsEligible);
            if (inspected != null && inspected.Id != active?.Id)
            {
                var selected = Row(status); selected.AddToClassList("mobile-inspected-unit");
                Text(selected, (inspected.IsEnemyControlled ? "ENEMY  " : "ALLY  ") +
                    UnitName(inspected.Id), "mobile-inspected-name");
                if (inspected.Health != null)
                    Text(selected, "HP " + inspected.Health.CurrentHealth + "/" +
                        inspected.Health.MaximumHealth + "  •  ARM " +
                        (inspected.EffectiveArmor?.ToString() ?? "—"), "mobile-inspected-vitals");
                var attack = inspected.Abilities.FirstOrDefault(a =>
                    a.Slot == AbilitySlot.NormalAttack);
                if (attack != null)
                    Text(selected, "ATK " + (attack.HasHealthEffect ? attack.Amount.ToString() : "STATUS") +
                        " · RNG " + attack.Range.ToString("0.#"), "mobile-inspected-attack");
            }

            // Ground taps move and objective taps interact. Do not keep
            // redundant Move/Stabilize/End Turn button rows on the screen.
            var row = Row(controls); row.AddToClassList("mobile-ability-strip");
            if (battleView.Pending != null)
            {
                var pending = battleView.Pending;
                var preview = pending.Kind == IntentKind.Movement
                    ? "MOVE " + pending.Movement.Cost.ToString("0.#")
                    : pending.Kind == IntentKind.Interaction ? "INTERACT"
                    : TargetSummary(pending.Target);
                Text(row, preview, "mobile-action-preview");
                var confirm = Primary(row, "CONFIRM", () => Handle(input.Confirm(pending.Id)), ready);
                confirm.AddToClassList("mobile-confirm");
                var cancel = Button(row, "CANCEL", () => Handle(input.Cancel()), ready);
                cancel.AddToClassList("mobile-cancel");
            }
            else
            {
                // Normal attacks are touch-only: tap to inspect, then
                // double-tap an enemy to preview and commit safely.
                foreach (var a in b.Actions.Where(a => a.Slot == AbilitySlot.Main ||
                    a.Slot == AbilitySlot.Signature))
                {
                    var slot = a.Slot;
                    var title = slot == AbilitySlot.Main ? "ABILITY" : "ULT";
                    var button = Button(row, title, () => {
                        lastTapUnitId = null;
                        Handle(input.SelectAbility(slot));
                    }, ready && a.CanSelect);
                    button.AddToClassList("mobile-ability-button");
                    button.tooltip = AbilityName(slot) + " · " + AbilityReadiness(a,active);
                    button.EnableInClassList("selected", battleView.SelectedAbility == slot);
                }
                var passive = Button(row, "ⓘ", () => battleDetails = !battleDetails);
                passive.AddToClassList("mobile-passive-button");
                passive.tooltip = AbilityName(AbilitySlot.Passive) + " · Passive and unit details";
            }
            if (battleRejection != null)
                Text(controls, battleRejection, "mobile-battle-feedback");
            else if (battleView.Pending != null)
                Text(controls, "Tap target again to confirm", "mobile-battle-feedback");
        }

        private void Pick(Vector2 panelPosition)
        {
            // The rescue prototype places a friendly field proxy at its own
            // recovery marker. The objective must be tappable over that proxy;
            // a normal unit pick would otherwise swallow the interaction.
            if (worldFiveSlice && battleView != null &&
                battleView.Battle.MissionId == WorldFivePlayableSlice.MissionIds[2])
            {
                var rescueObjective = catalog.Missions[battleView.Battle.MissionId].Objectives
                    .FirstOrDefault(o => o.Interaction != null && input.ReadPlanning().Objectives
                        .Any(p => p.Id == o.Id && p.CanInteract) &&
                        Vector2.Distance(field.Project(PlaytestBattlefield.Vector(o.Interaction.Point),root),
                            panelPosition) < 32f);
                if (rescueObjective != null)
                {
                    var ready = input.TapObjective(rescueObjective.Id);
                    if (ready.Outcome == InputOutcome.Previewed)
                        ready = input.Confirm(input.Read().Pending.Id);
                    Handle(ready);
                    return;
                }
            }
            string id = field.PickUnit(panelPosition,root);
            if (id != null)
            {
                var unit = battleView.Battle.Units.FirstOrDefault(u => u.Id == id);
                // Special-ability target taps preserve their established
                // preview/confirm behavior. Default selection never attacks.
                if (battleView.SelectedAbility.HasValue)
                {
                    lastTapUnitId = null;
                    Handle(input.TapUnit(id));
                    return;
                }
                bool doubleTap = id == lastTapUnitId &&
                    Time.unscaledTime - lastTapUnitTime <= AttackDoubleTapWindow;
                lastTapUnitId = id;
                lastTapUnitTime = Time.unscaledTime;
                bool canAttack = unit?.IsEnemyControlled == true &&
                    battleView.Battle.Phase == BattleSessionPhase.PlayerInput &&
                    battleView.Pending == null &&
                    battleView.Battle.Actions.Any(a =>
                        a.Slot == AbilitySlot.NormalAttack && a.CanSelect);
                if (doubleTap && canAttack)
                {
                    // Validated double-tap normal attack: select trusted slot,
                    // preview the selected enemy, then commit that *same*
                    // preview through the authoritative transaction.
                    lastTapUnitId = null;
                    var selected = input.SelectAbility(AbilitySlot.NormalAttack);
                    if (selected.Outcome == InputOutcome.Selected)
                    {
                        var preview = input.TapUnit(id);
                        Handle(preview.Outcome == InputOutcome.Previewed
                            ? input.TapUnit(id) : preview);
                        return;
                    }
                    Handle(selected);
                    return;
                }
                // First tap of any friendly/enemy unit is inspection only.
                Handle(input.InspectUnit(id));
                return;
            }
            lastTapUnitId = null;
            // A ready objective can be interacted with by tapping its on-field
            // marker. No additional permanent Stabilize button is required.
            var mission = catalog.Missions[battleView.Battle.MissionId];
            var objective = input.ReadPlanning().Objectives.FirstOrDefault(o => o.CanInteract &&
                mission.Objectives.Any(x => x.Id == o.Id && x.Interaction != null &&
                    Vector2.Distance(field.Project(PlaytestBattlefield.Vector(x.Interaction.Point),root),
                        panelPosition) < 28));
            if (objective != null)
            {
                var result = input.TapObjective(objective.Id);
                if (result.Outcome == InputOutcome.Previewed)
                    result = input.Confirm(input.Read().Pending.Id);
                Handle(result);
                return;
            }
            if (!field.PickGround(panelPosition,root,out var destination)) return;
            // Intentional touch-to-move: use the SAME preview legality and
            // versioned commit/save path as the old Confirm button, including
            // automatically clipped partial movement.
            var move = input.TapDestination(destination);
            if (move.Outcome == InputOutcome.Previewed)
                move = input.Confirm(input.Read().Pending.Id);
            Handle(move);
        }

        private void Handle(BattleInputResult result)
        {
            battleRejection = result.Outcome == InputOutcome.Rejected ? RejectionReason(result) : null;
            message = result.Outcome == InputOutcome.Rejected ? RejectionReason(result)
                : result.Outcome == InputOutcome.Previewed ? "Tap target again to confirm."
                : result.Outcome == InputOutcome.Selected ? "Tap a target."
                : result.Outcome == InputOutcome.Committed ? "Saved." : "Ready.";
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
            if (worldFiveSlice)
            {
                // New campaign prototype NEVER reads or writes existing fixture
                // battle/profile slots; both can be resumed independently.
                catalog = WorldFivePlayableSlice.Create();
                menu = WorldFivePlayableSlice.Open(catalog,
                    new DirectoryBattleSaveFiles(savePath),new DirectoryProgressSaveFiles(savePath));
                legacyProfile = false;
                return;
            }
            var profile = PlaytestProfile.Open(new DirectoryBattleSaveFiles(savePath),
                new DirectoryBattleSaveFiles(Path.Combine(savePath,"Layouts-v2")),new DirectoryProgressSaveFiles(savePath),
                new DirectoryBattleSaveFiles(Path.Combine(savePath,"Layouts-v3")));
            catalog = profile.Catalog; menu = profile.Menu; legacyProfile = profile.IsLegacy;
            if (legacyProfile) message = "Your saved battle is preserved. Finish it to use the new battlefield layouts.";
        }
        private void OpenCampaignPrototypeFromHome()
        {
            // Never let the shared Button wrapper silently swallow an
            // unavailable prototype switch. Offer the existing explicit
            // retreat confirmation, without forfeiting a battle on tap.
            if (menu.Read().Phase == MissionFlowPhase.Battle)
            {
                battleExitPrompt = true;
                message = "Your current battle is saved. Finish it or explicitly end it before switching campaigns.";
                return;
            }
            if (menu.Read().Phase != MissionFlowPhase.Selection)
            {
                message = "Claim your battle result before changing campaigns.";
                return;
            }
            SwitchCampaignPrototype();
        }

        private void SwitchCampaignPrototype()
        {
            if (menu.Read().Phase != MissionFlowPhase.Selection)
                throw new InvalidOperationException("Finish your active battle before changing playtest campaigns.");
            bool openWorldFive = !worldFiveSlice;
            // A failure opening the optional prototype must not strand the
            // entire app in an unusable fatal screen. Preserve the previously
            // loaded profile, catalog and path, then display a visible error.
            bool previousMode = worldFiveSlice;
            string previousPath = savePath;
            var previousMenu = menu;
            var previousCatalog = catalog;
            bool previousLegacy = legacyProfile;
            try
            {
                worldFiveSlice = openWorldFive;
                savePath = Path.Combine(Application.persistentDataPath,
                    worldFiveSlice ? "WorldFiveSlice-v3" : "AbstractPlaytest-v1");
                input = null; battleView = null; inBattle = false;
                battleExitPrompt = false; inspectedUnit = null;
                squadInspection = null; collectionDetailsExpanded = false;
                OpenProfile();
                if (openWorldFive && !menu.Read().Campaigns.Any(
                    c => c.CampaignId == WorldFivePlayableSlice.CampaignId))
                    throw new InvalidOperationException("The World Five mission path is missing from the prototype catalog.");
                // Do not leave the player on Home after a successful switch.
                // Display all six missions immediately; the first is unlocked.
                selectedCampaign = openWorldFive ? WorldFivePlayableSlice.CampaignId : null;
                page = openWorldFive ? Page.Missions : Page.Home;
                message = openWorldFive
                    ? "World Five missions 001–006 · Select a mission to view its briefing."
                    : "Original playtest · previous progress restored";
            }
            catch (Exception e)
            {
                worldFiveSlice = previousMode;
                savePath = previousPath;
                menu = previousMenu;
                catalog = previousCatalog;
                legacyProfile = previousLegacy;
                page = Page.Home;
                selectedCampaign = null;
                Debug.LogException(e, this);
                message = "World Five could not open. See the Console; your original profile is unchanged.";
            }
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
                root.RemoveFromClassList("primary-tab-mode");
                root.RemoveFromClassList("ambient-menu-mode");
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
            battleExitPrompt = false;
            message = "Your progress saves automatically.";
        }
        private void BuildMobilePageHeader(VisualElement parent)
        {
            if (menuView.Phase == MissionFlowPhase.Results) return;
            bool topLevel = page == Page.Battles || page == Page.Settings ||
                (page == Page.Collection && inspectedUnit == null);
            if (topLevel)
            {
                // Full-width hero rather than a top Home/back button: the
                // persistent bottom bar is the ONLY top-level navigation.
                var hero = new VisualElement { pickingMode = PickingMode.Ignore };
                hero.AddToClassList("shell-tab-hero");
                parent.Add(hero);
                var copy = new VisualElement { pickingMode = PickingMode.Ignore };
                copy.AddToClassList("shell-tab-hero-copy");
                hero.Add(copy);
                Text(copy, worldFiveSlice ? "WORLD FIVE · OPENING STUDY" :
                    "NINEFOLD · FRONTIERS", "shell-tab-eyebrow").pickingMode = PickingMode.Ignore;
                string title = page == Page.Battles ? "Campaigns" :
                    page == Page.Collection ? "Units" : "Settings";
                Text(copy, title, "shell-tab-title").pickingMode = PickingMode.Ignore;
                string sub = page == Page.Battles ? "YOUR JOURNEY THROUGH THE NINE WORLDS" :
                    page == Page.Collection ? "YOUR TACTICAL ROSTER" : "PREFERENCES & PLAYTEST TOOLS";
                Text(copy, sub, "shell-tab-subtitle").pickingMode = PickingMode.Ignore;
                return;
            }
            var header = Row(parent);
            header.AddToClassList("native-page-bar");
            bool unitDetail = page == Page.Collection && inspectedUnit != null;
            bool canBack = unitDetail || page == Page.Missions || page == Page.Briefing || page == Page.Squad;
            if (canBack)
            {
                var back = Button(header, "‹", () => {
                    if (unitDetail)
                    {
                        menu.Collection.Cancel();
                        inspectedUnit = null;
                        collectionDetailsExpanded = false;
                    }
                    else if (page == Page.Squad) Navigate(Page.Briefing);
                    else if (page == Page.Briefing)
                        Navigate(catalog.Missions[menuView.SelectedMissionId].IsCampaign ? Page.Missions : Page.Home);
                    else Navigate(Page.Battles);
                });
                back.AddToClassList("native-top-back");
                back.tooltip = unitDetail ? "Back to units" : "Back";
            }
            else
            {
                var brand = Text(header, "N", "native-top-emblem");
                brand.pickingMode = PickingMode.Ignore;
            }
            string label = page == Page.Collection ? "UNITS" : page == Page.Settings ? "SETTINGS"
                : page == Page.Squad ? "SQUAD" : page == Page.Briefing ? "MISSION"
                : page == Page.Missions ? "CAMPAIGN PATH" : "CAMPAIGNS";
            Text(header, label, "native-top-title").pickingMode = PickingMode.Ignore;
            // Nested screens have one contextual Back button only.
            // Home is always accessible as a sibling tab at the bottom.
            var spacer = new VisualElement { pickingMode = PickingMode.Ignore };
            spacer.AddToClassList("native-top-spacer"); header.Add(spacer);
        }

        private void BuildNavigation(VisualElement parent)
        {
            var nav = Row(parent); nav.AddToClassList("navigation");
            nav.AddToClassList("unified-navigation");
            bool homeNavigation = root.ClassListContains("home-mode");
            if (homeNavigation) nav.AddToClassList("nf-nav");
            foreach (var target in new[] { Page.Home, Page.Battles, Page.Collection, Page.Settings })
            {
                var destinationPage = target;
                var label = target == Page.Battles ? "Campaigns" : target == Page.Collection ? "Units" : target.ToString();
                // Browsing is permitted during a saved battle; progression and
                // new deployments remain guarded until it has been resolved.
                var button = Button(nav, "", () => {
                    if (destinationPage == Page.Collection)
                    {
                        inspectedUnit = null;
                        collectionDetailsExpanded = false;
                    }
                    Navigate(destinationPage);
                });
                button.AddToClassList("nav-button");
                button.AddToClassList("unified-nav-button");
                if (homeNavigation) button.AddToClassList("nf-nav-item");
                HomeIcon(button, target == Page.Home ? "home" : target == Page.Settings ? "settings" : target == Page.Collection ? "units" : "routes");
                Text(button, label, "nav-label"); button.tooltip = label;
                button.EnableInClassList("selected",
                    page == target || (target == Page.Battles &&
                    (page == Page.Missions || page == Page.Briefing || page == Page.Squad)));
            }
        }
        private void BuildSettings(VisualElement parent)
        {
            var motion = Card(parent);
            motion.AddToClassList("shell-settings-card");
            Text(motion, "GAMEPLAY PREFERENCES", "eyebrow");
            Text(motion, "Battle animations", "section");
            Text(motion, "Reduced motion skips movement and action playback without changing the battle rules.", "detail");
            Button(motion, reducedMotion ? "Reduced motion: ON" : "Reduced motion: OFF", () => {
                reducedMotion = !reducedMotion; PlayerPrefs.SetInt("Ninefold.ReducedMotion", reducedMotion ? 1 : 0); PlayerPrefs.Save();
            });
            var help = Card(parent);
            help.AddToClassList("shell-settings-card");
            Text(help, "How to play", "section");
            Text(help, "Tap the field to move. Tap a unit once to inspect it; double-tap an enemy for a normal attack. Choose Ability or Ult for special attacks. Tap gold objectives to interact when in reach. Use END when finished.", "detail");
            Text(help, "Long moves stop at your remaining movement allowance. Walls, units and arena boundaries still limit valid destinations.", "detail");
            Text(help, "Cyan units are allies; red units are enemies. Gold marks objectives and valid targets. Striped ground costs extra movement.", "detail");
            // Diagnostics are useful in testing, but should never dominate
            // the default consumer-facing Settings experience.
            var tools = Card(parent, "shell-diagnostics-card");
            Text(tools, "PLAYTEST TOOLS", "eyebrow");
            var toggle = Button(tools,
                showDiagnostics ? "Hide diagnostics  −" : "Show diagnostics  +",
                () => showDiagnostics = !showDiagnostics);
            toggle.AddToClassList("shell-diagnostics-toggle");
            if (showDiagnostics)
            {
                Text(tools, PlaytestRevision.Id + " · " + PlaytestRevision.Features, "detail");
                BuildDiagnostics(tools);
                Text(tools, "Units, maps and badges are temporary abstract test content.", "detail");
            }
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
        private static void BuildMapPreview(VisualElement parent, MissionContent mission, bool compact = false)
        {
            if (!compact) Text(parent, "BATTLEFIELD OVERVIEW", "eyebrow");
            var map = new VisualElement { pickingMode = PickingMode.Ignore }; map.AddToClassList("map-preview");
            if (compact) map.AddToClassList("vmp-scout-map");
            parent.Add(map);
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
            if (!compact)
            {
                var legend = Row(parent); legend.AddToClassList("vmp-map-legend");
                MapLegendEntry(legend, "DEPLOY", "vmp-key-deploy");
                MapLegendEntry(legend, "COVER", "vmp-key-cover");
                MapLegendEntry(legend, "SLOW", "vmp-key-slow");
                MapLegendEntry(legend, "OBJECTIVE", "vmp-key-objective");
            }
        }
        private static void MapLegendEntry(VisualElement parent, string title, string swatchClass)
        {
            var entry = new VisualElement { pickingMode = PickingMode.Ignore };
            entry.AddToClassList("vmp-legend-entry"); parent.Add(entry);
            var mark = new VisualElement { pickingMode = PickingMode.Ignore };
            mark.AddToClassList("vmp-legend-swatch"); mark.AddToClassList(swatchClass); entry.Add(mark);
            Text(entry, title, "vmp-legend-text").pickingMode = PickingMode.Ignore;
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
        private string Description(string id)
        {
            if (worldFiveSlice)
            {
                if (id == WorldFivePlayableSlice.MissionIds[0])
                    return "Approach the marked route point and stabilize it. Follow the gold objective.";
                if (id == WorldFivePlayableSlice.MissionIds[1])
                    return "Use the open or covered approach to stabilize the next route point.";
                if (id == WorldFivePlayableSlice.MissionIds[2])
                    return "Rescue the stranded traveler, then bring them back to the gold extraction zone using your first deployed unit.";
                if (id == WorldFivePlayableSlice.MissionIds[3])
                    return "Occupy the marked zone for two consecutive rounds. A test opponent can contest it.";
                if (id == WorldFivePlayableSlice.MissionIds[4])
                    return "Protect the friendly traveler for two rounds. Keep the test threat away from them.";
                if (id == WorldFivePlayableSlice.MissionIds[5])
                    return "Keep at least one squad unit alive through three rounds. No destination is required.";
            }
            if (catalog.Manifest.ContentRevision != "fixture-layouts-v3")
                return id == "fixture-mixed" ? "Defeat the enemy. Use either side of the central cover island to approach."
                    : id == "fixture-opening" ? "Stabilize the marked objective. An open center with offset side cover."
                    : id == "fixture-finale" ? "Stabilize the objective. A northern barrier and slow central ground change your routes."
                    : "Stabilize the objective with faction B units. Offset walls create an asymmetric arena.";
            return id == "fixture-mixed" ? "Defeat the enemy. Cover divides two approaches."
                : id == "fixture-opening" ? "Reach and stabilize the marked objective beyond the entry cover."
                : id == "fixture-finale" ? "Advance through staggered cover or the slow central route to stabilize."
                : "Stabilize the marked objective beyond the asymmetric cover.";
        }
        private static string MissionName(string id) =>
            id == WorldFivePlayableSlice.MissionIds[0] ? "The Wrong Arrival" :
            id == WorldFivePlayableSlice.MissionIds[1] ? "Hold the Passage" :
            id == WorldFivePlayableSlice.MissionIds[2] ? "Return the Stranded" :
            id == WorldFivePlayableSlice.MissionIds[3] ? "The Safe Boundary" :
            id == WorldFivePlayableSlice.MissionIds[4] ? "Shelter First" :
            id == WorldFivePlayableSlice.MissionIds[5] ? "Wait for the Window" :
            id == "fixture-opening" ? "Stabilization" : id == "fixture-mixed" ? "Quick combat" :
            id == "fixture-finale" ? "Campaign finale" : "Faction B: stabilization";
        private void OnDestroy() { if (playback != null) StopCoroutine(playback); field?.Dispose(); }
    }
}
