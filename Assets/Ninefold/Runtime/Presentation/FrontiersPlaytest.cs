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
        private string savePath, message = "Choose a test mission. All content is temporary and non-canon.";
        private bool dirty, fatal, collection, suspended, reducedMotion;
        private Coroutine playback;
        private int pointer = -1;
        private Vector2 pointerStart;

        private void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            if (ScreenStyles != null) root.styleSheets.Add(ScreenStyles);
            root.AddToClassList("root");
            safe = new VisualElement { name = "safe-area" }; root.Add(safe);
            heading = new VisualElement(); heading.AddToClassList("heading"); safe.Add(heading);
            Text(heading, "NINEFOLD: FRONTIERS", "title");
            Text(heading, "ENGINEERING PLAYTEST • NON-CANON CONTENT", "eyebrow");
            notice = Text(heading, message, "notice");
            content = new VisualElement(); content.style.flexGrow = 1; safe.Add(content);
            savePath = Path.Combine(Application.persistentDataPath, "AbstractPlaytest-v1");
            Guard(() => {
                if (BattlefieldCamera == null || FixtureMaterial == null) throw new InvalidOperationException("Run Ninefold > Playtest > Create or Open Integration Scene.");
                catalog = AbstractContentPackage.Create();
                field = new PlaytestBattlefield(transform, BattlefieldCamera, FixtureMaterial);
                menu = new MissionPresentation(catalog, new DirectoryBattleSaveFiles(savePath), new DirectoryProgressSaveFiles(savePath), "unity-abstract-v1");
                // Never treat a corrupt/partial existing save as a new profile or silently overwrite it.
                bool exists = Directory.Exists(savePath) && Directory.EnumerateFiles(savePath, "*.save").Any();
                if (exists) menu.Open(); else menu.CreateProfile();
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
            content.Clear(); surface = null; controls = null; pointer = -1;
            notice.text = message;
            if (menuView.Phase == MissionFlowPhase.Battle)
            {
                content.style.backgroundColor = Color.clear;
                input = menu.ResumeBattle(); battleView = input.Read();
                BuildBattle();
            }
            else
            {
                content.style.backgroundColor = new Color(.035f, .063f, .094f);
                input = null; battleView = null; field.Hide();
                var scroll = new ScrollView(); scroll.style.flexGrow = 1; content.Add(scroll);
                if (menuView.Phase == MissionFlowPhase.Results) BuildResults(scroll);
                else if (collection) BuildCollection(scroll);
                else BuildSelection(scroll);
            }
        }

        private void BuildSelection(VisualElement parent)
        {
            Text(parent, "Mission selection", "section");
            foreach (var m in menuView.Missions.OrderBy(m => m.Definition.Id == "fixture-opening" ? 0 : m.Definition.Id == "fixture-mixed" ? 1 : 2))
            {
                string id = m.Definition.Id;
                Button(parent, MissionName(id) + (m.IsReplay ? " • replay" : "") + (!m.Available ? " • locked" : ""), () => {
                    menu.SelectMission(id);
                    var view = menu.Read();
                    menu.SetSquad(view.Units.Where(u => u.EligibleForMission && !u.Definition.Roster.IsApex).Take(m.Definition.MaximumSquad).Select(u => u.Definition.Id));
                    message = "Choose your squad, then deploy.";
                }, m.Available);
                if (!m.Available) Text(parent, m.FactionRosterLocked ? "Own three standards from this test faction to enter." : "Complete: " + string.Join(", ", m.MissingPrerequisites.Select(MissionName)), "detail");
            }
            if (menuView.SelectedMissionId != null)
            {
                var definition = catalog.Missions[menuView.SelectedMissionId];
                Text(parent, "Squad • " + menuView.Squad.Count + "/" + definition.MaximumSquad, "section");
                foreach (var u in menuView.Units.Where(u => u.EligibleForMission))
                {
                    string id = u.Definition.Id; bool selected = menuView.Squad.Contains(id);
                    Button(parent, (selected ? "✓ " : "+ ") + UnitName(id) + (u.Definition.Roster.IsApex ? " • Apex" : "") + "\n" + Stats(u), () => {
                        var squad = menu.Read().Squad.ToList();
                        if (!squad.Remove(id)) squad.Add(id);
                        menu.SetSquad(squad);
                    });
                }
                string plan = menuView.PlanId;
                Button(parent, "Deploy — " + MissionName(menuView.SelectedMissionId), () => { input = menu.Start(plan); message = "Tap ground to preview movement. Tap a unit to inspect."; }, menuView.CanStart);
                if (!menuView.CanStart) Text(parent, "Squad needs adjustment: " + menuView.SquadFailure, "detail");
            }
            Button(parent, "Unit collection & upgrades", () => collection = true);
            foreach (var c in menuView.Campaigns)
            {
                Text(parent, c.CampaignId + " • " + c.CompletedMissions + "/" + c.TotalMissions + (c.RewardsClaimed ? " • reward claimed" : ""), "detail");
                if (c.IsComplete && !c.RewardsClaimed)
                    Button(parent, "Claim campaign completion", () => { menu.ClaimCampaign(c.CampaignId); message = "Campaign reward saved. Check your collection and available missions."; });
            }
            Text(parent, "Opening/finale: one-action stabilization tests. Combat: a short enemy encounter. These are not the production campaigns.", "detail");
            Button(parent, "Reload saved profile", Reload);
        }

        private void BuildCollection(VisualElement parent)
        {
            Button(parent, "Back to missions", () => { menu.Collection.Cancel(); collection = false; });
            var view = menu.Collection.Read();
            if (view.Pending != null)
            {
                var p = view.Pending;
                Text(parent, p.Operation + " " + UnitName(p.UnitId), "section");
                Text(parent, "Cost " + p.Cost + " / balance " + p.Balance + " fragments\nHP " + p.Before.Health + " → " + p.After.Health + " • Armor " + p.Before.Armor + " → " + p.After.Armor + "\n" + (p.CanConfirm ? "Confirm to save this change." : p.Block.ToString()), "detail");
                Button(parent, "Confirm upgrade", () => { menu.Collection.Confirm(p.Id); message = "Collection change saved."; }, p.CanConfirm);
                Button(parent, "Cancel preview", () => menu.Collection.Cancel());
            }
            foreach (var faction in view.Factions)
            {
                Text(parent, faction.Id == "fixture-a" ? "Test faction A" : "Test faction B", "section");
                foreach (var u in faction.Units)
                {
                    string id = u.Unit.Definition.Id;
                    Text(parent, UnitName(id) + (u.Unit.Owned ? " • owned" : " • locked") + "\n" + Stats(u.Unit) + "\nFragments: " + u.Fragments + " • Rank: " + u.Unit.Rank, "detail");
                    var operation = u.Unit.Owned ? CollectionOperation.Advance : CollectionOperation.Unlock;
                    var offer = u.Unit.Owned ? u.Advance : u.Unlock;
                    Button(parent, operation + " • " + offer.Cost + " fragments" + (offer.CanConfirm ? "" : " • " + offer.Block), () => menu.Collection.Preview(operation, id), offer.CanConfirm);
                    if (u.Unit.Owned)
                        Button(parent, u.Unit.CustomizationId == null ? "Preview health / armor trade-off" : "Preview reset customization", () => menu.Collection.Preview(CollectionOperation.Customize, id, u.Unit.CustomizationId == null ? "fixture-health" : null));
                }
            }
        }

        private void BuildResults(VisualElement parent)
        {
            var r = menuView.Results;
            Text(parent, r.Result.Outcome.ToString(), "section");
            Text(parent, MissionName(r.Result.MissionId) + " • round " + r.Result.Round + "\n" + r.Result.Reason, "detail");
            Text(parent, r.Grants.Count == 0 ? "No rewards this attempt. Your units and progression are kept." : string.Join("\n", r.Grants.Select(g => g.ResourceId + " +" + g.Amount)), "detail");
            Button(parent, r.Claimed ? "Rewards saved" : "Claim & save rewards", () => { menu.ClaimRewards(r.Result.AttemptId); message = "Rewards saved once for this attempt."; }, r.CanClaim);
            Button(parent, "Return to missions", () => { menu.ReturnToSelection(r.Result.AttemptId); message = "Choose your next mission."; }, r.Claimed);
        }

        private void BuildBattle()
        {
            var b = battleView.Battle;
            Text(content, MissionName(b.MissionId) + " • round " + b.Round, "section");
            Text(content, "Turn: " + UnitName(b.Activation?.UnitId) + "  |  Next: " + string.Join(" › ", b.UpcomingTurns.Take(4).Select(UnitName)), "detail");
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
            field.Show(b, battleView.Pending, surface);
            // Fixed footer height prevents previews changing the camera viewport under a second tap.
            var footer = new ScrollView(); footer.style.height = 330; footer.style.flexShrink = 0;
            content.Add(footer);
            controls = new VisualElement(); controls.AddToClassList("controls"); footer.Add(controls);
            BuildControls();
        }

        private void BuildControls()
        {
            controls.Clear();
            var b = battleView.Battle;
            bool ready = !battleView.IsAnimating && !suspended;
            foreach (var o in b.Objectives) Text(controls, o.Kind + " • " + o.Progress + "/" + o.Required + " • " + o.Status, "detail");
            if (b.Activation != null) Text(controls, "Move " + b.Activation.MovementRemaining.ToString("0.##") + " • Action " + (b.Activation.PrimaryActionAvailable ? "ready" : "spent"), "detail");
            var selected = b.Units.FirstOrDefault(u => u.Id == battleView.SelectedUnitId);
            if (selected?.Health != null) Text(controls, UnitName(selected.Id) + " • HP " + selected.Health.CurrentHealth + "/" + selected.Health.MaximumHealth + " • Armor " + selected.EffectiveArmor, "detail");
            var row = Row(controls);
            foreach (var a in b.Actions.Where(a => a.Slot != AbilitySlot.Passive))
            {
                var slot = a.Slot;
                var button = Button(row, slot == AbilitySlot.NormalAttack ? "Attack" : slot == AbilitySlot.Main ? "Heal" : "Signature", () => Handle(input.SelectAbility(slot)), ready && a.CanSelect);
                button.tooltip = a.CanSelect ? "Select, then tap a target to preview." : a.Block + " / " + a.RuleFailure;
                if (battleView.SelectedAbility == slot) button.AddToClassList("selected");
            }
            if (battleView.SelectedAbility.HasValue) Text(controls, "Tap a target to preview " + battleView.SelectedAbility + ".", "detail");
            if (battleView.Pending != null)
            {
                var p = battleView.Pending;
                string preview = p.Kind == IntentKind.Movement ? "Move cost " + p.Movement.Cost.ToString("0.##") : p.Kind == IntentKind.Interaction ? "Stabilize • uses this turn's action" : UnitName(p.Target.TargetId) + " • HP " + p.Target.Effect.Health?.HealthBefore + " → " + p.Target.Effect.Health?.HealthAfter;
                Text(controls, preview, "preview");
                Button(controls, "Confirm " + p.Kind, () => Handle(input.Confirm(p.Id)), ready);
            }
            row = Row(controls);
            Button(row, "Cancel / move", () => Handle(input.Cancel()), ready);
            if (b.Objectives.Any(o => o.Kind == ObjectiveKind.Stabilize && o.Status == ObjectiveStatus.Active))
                Button(row, "Stabilize", () => Handle(input.TapObjective("primary")), ready && b.Phase == BattleSessionPhase.PlayerInput);
            long activation = b.Activation?.ActivationId ?? 0;
            Button(row, "End turn", () => Handle(input.EndTurn(activation)), ready && b.Phase == BattleSessionPhase.PlayerInput);
            row = Row(controls);
            Button(row, "Reload / resume", Reload, ready);
            Button(row, reducedMotion ? "Motion: reduced" : "Motion: normal", () => reducedMotion = !reducedMotion, ready);
            Text(controls, "Tap twice on the same destination/target to confirm, or use Confirm. No turn timer.", "hint");
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
            message = result.Outcome == InputOutcome.Rejected ? "Unavailable: " + result.FieldFailure + " / " + result.HealthFailure + " / " + result.InteractionFailure
                : result.Outcome == InputOutcome.Previewed ? "Preview only — confirm to commit." : result.Outcome == InputOutcome.Selected ? "Selection updated." : "Saved • " + result.Outcome;
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

        private void Reload()
        {
            if (playback != null) { StopCoroutine(playback); playback = null; }
            input = null; menu.Open(); collection = false;
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
        private Button Button(VisualElement parent, string label, Action action, bool enabled = true)
        {
            var b = new Button(() => { if (!fatal && !dirty && !suspended && playback == null) Guard(() => { action(); dirty = true; }); }) { text = label };
            b.SetEnabled(enabled); parent.Add(b); return b;
        }
        private static VisualElement Row(VisualElement parent) { var row = new VisualElement(); row.AddToClassList("row"); parent.Add(row); return row; }
        private static Label Text(VisualElement parent, string text, string cls) { var l = new Label(text); l.AddToClassList(cls); parent.Add(l); return l; }
        public static string UnitName(string id) => id == null ? "—" : id.Replace("fixture-enemy-", "Enemy ").Replace("fixture-a-", "A").Replace("fixture-b-", "B");
        private static string Stats(PreparationUnit u) => "HP " + u.Health + " • Armor " + u.Armor + " • Initiative " + u.Initiative + " • Move " + u.Movement;
        private static string MissionName(string id) => id == "fixture-opening" ? "01 · Stabilization test" : id == "fixture-mixed" ? "02 · Combat test" : id == "fixture-finale" ? "03 · Starter completion test" : "04 · Faction B test";
        private void OnDestroy() { if (playback != null) StopCoroutine(playback); field?.Dispose(); }
    }
}
