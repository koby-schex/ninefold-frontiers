using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Missions;

namespace Ninefold.Core.Content
{
    /// <summary>
    /// Internal, non-playable campaign writing blueprint. Nothing in this file
    /// becomes lore, a purchasable unit, a mission, or a save revision.
    /// It is not connected to ContentCatalog, MissionFlow or the Unity UI.
    /// </summary>
    internal enum OpeningFieldFootprint { Close, Standard, Extended }

    public enum OpeningLesson
    {
        Movement, Terrain, Rescue, ZoneControl, Protection,
        Survival, AttackSelection, SquadPositioning, AbilityTiming,
        MultiObjectiveDecision
    }

    public sealed class WorldFiveOpeningBeat
    {
        public int Order { get; }
        public string DraftId { get; }
        public string DraftTitle { get; }
        public string Region { get; }
        public string EnvironmentKey { get; }
        public ObjectiveKind PrimaryGoal { get; }
        public OpeningLesson Lesson { get; }
        public string AuthoringIntent { get; }
        public string PlayerClarityCue { get; }
        internal OpeningFieldFootprint Footprint { get; }

        internal WorldFiveOpeningBeat(int order, string title, string region, string environmentKey,
            ObjectiveKind objective, OpeningLesson lesson, OpeningFieldFootprint footprint,
            string authoringIntent, string clarity)
        {
            Order=order;
            DraftId="w5-draft-" + order.ToString("D3",System.Globalization.CultureInfo.InvariantCulture);
            DraftTitle=title; Region=region; EnvironmentKey=environmentKey;
            PrimaryGoal=objective; Lesson=lesson; Footprint=footprint;
            AuthoringIntent=authoringIntent; PlayerClarityCue=clarity;
        }
    }

    /// <summary>
    /// Candidate internal narrative/mission progression informed by the
    /// confirmed World Five opening lens. Explicit story approval, enemy
    /// identity, combat encounter scripting and map validation are still needed.
    /// No names or titles here are locked canon.
    /// </summary>
    public static class WorldFiveOpeningCampaignDraft
    {
        public const string DraftRevision = "w5-opening-ten-draft-01";
        public const string CanonReference = "Ninefold Master Lore Archive v26.0, World Five / Initial narrative lens";
        public const string Status = "INTERNAL DESIGN CANDIDATE - NOT PLAYABLE OR CANON-LOCKED";

        // All region labels below are present in the referenced archive.
        private static readonly string[] ApprovedRegions = {
            "Anchor Basins","Pale Corridors","Driftlands"
        };

        private static WorldFiveOpeningBeat B(int n, string title, string region, string environment,
            ObjectiveKind goal, OpeningLesson lesson, OpeningFieldFootprint footprint,
            string intent, string cue)
            => new WorldFiveOpeningBeat(n,title,region,environment,goal,lesson,footprint,intent,cue);

        private static readonly IReadOnlyList<WorldFiveOpeningBeat> Proposal =
            Array.AsReadOnly(new[] {
                B(1,"The Wrong Arrival","Pale Corridors","w5-pale-entry",
                    ObjectiveKind.Stabilize,OpeningLesson.Movement,OpeningFieldFootprint.Close,
                    "A small Continuance team checks a historically dependable route when its endpoint ceases to agree with records. Begin locally and without explaining the cause.",
                    "One gold anchor ahead, reachable via an unobstructed short approach."),
                B(2,"Hold the Passage","Pale Corridors","w5-pale-side-route",
                    ObjectiveKind.Stabilize,OpeningLesson.Terrain,OpeningFieldFootprint.Close,
                    "The team keeps a nearby connection usable while learning that terrain and shelter affect approach.",
                    "Two readable approach options; distinguish cover from difficult terrain."),
                B(3,"Return the Stranded","Pale Corridors","w5-pale-rescue",
                    ObjectiveKind.RescueAndExtract,OpeningLesson.Rescue,OpeningFieldFootprint.Standard,
                    "A localized route disruption has left travelers out of position; rescue comes before speculation.",
                    "Rescue target and extraction zone appear with separate, persistent markers."),
                B(4,"The Safe Boundary","Anchor Basins","w5-basin-perimeter",
                    ObjectiveKind.Secure,OpeningLesson.ZoneControl,OpeningFieldFootprint.Standard,
                    "A defensive perimeter around an established anchor is tested as nearby routes shift.",
                    "Mark the holding zone clearly and visualize the required remaining turns."),
                B(5,"Shelter First","Anchor Basins","w5-basin-shelter",
                    ObjectiveKind.Protect,OpeningLesson.Protection,OpeningFieldFootprint.Close,
                    "Guard a vulnerable non-player party through a short, high-pressure shelter interval without defining the crisis' cause.",
                    "Protectee health and destination remain obvious even with multiple actors."),
                B(6,"Wait for the Window","Pale Corridors","w5-pale-hazard-window",
                    ObjectiveKind.Survive,OpeningLesson.Survival,OpeningFieldFootprint.Standard,
                    "A field team must endure a temporary closure until the route is safe enough to resume.",
                    "Show survival rounds and safe lanes; never invent a gold destination."),
                B(7,"Clear the Approach","Pale Corridors","w5-pale-blocked-access",
                    ObjectiveKind.Defeat,OpeningLesson.AttackSelection,OpeningFieldFootprint.Standard,
                    "Present a bounded tactical opponent or hazard proxy only after its world-appropriate identity receives canon/content approval.",
                    "Mark actual hostile targets and damage previews; never imply all obstruction is hostile."),
                B(8,"The Divided Route","Driftlands","w5-drift-branch",
                    ObjectiveKind.Secure,OpeningLesson.SquadPositioning,OpeningFieldFootprint.Extended,
                    "Uneven route access forces the party to maintain a clear control area and protect its spacing.",
                    "Two readable corridors; visible control zone and restrained camera framing."),
                B(9,"Keep the Bond","Anchor Basins","w5-basin-return-lane",
                    ObjectiveKind.RescueAndExtract,OpeningLesson.AbilityTiming,OpeningFieldFootprint.Standard,
                    "Rescue and return under pressure while introducing the difference between basic actions and a carefully timed main ability. Bonded-life behavior remains trust-first.",
                    "Show rescued state, extraction marker and ready/used main ability indicators."),
                B(10,"A Line That Holds","Pale Corridors","w5-pale-stability-line",
                    ObjectiveKind.Stabilize,OpeningLesson.MultiObjectiveDecision,OpeningFieldFootprint.Extended,
                    "Close the tutorial act with a difficult but local stabilizing task and unresolved route discrepancy. Broader explanations and protected mysteries stay withheld.",
                    "One primary stabilization point, at most two optional goals and no ambiguous paths.")
            });

        public static IReadOnlyList<WorldFiveOpeningBeat> Missions => Proposal;

        /// <summary>
        /// Strong author-side assertions. Nothing in this file automatically
        /// converts draft beats into playable missions or approved canon.
        /// </summary>
        public static void Validate()
        {
            if (Missions.Count != 10) throw new InvalidOperationException("Opening outline must contain ten review beats.");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            var scenes=new HashSet<string>(StringComparer.Ordinal);
            var kinds=new HashSet<ObjectiveKind>();
            var lessons=new HashSet<OpeningLesson>();
            foreach (var beat in Missions)
            {
                if (beat.Order < 1 || beat.Order > Missions.Count ||
                    !ids.Add(beat.DraftId) ||
                    beat.DraftId != "w5-draft-" + beat.Order.ToString("D3",System.Globalization.CultureInfo.InvariantCulture))
                    throw new InvalidOperationException("Unstable opening mission ID/order.");
                if (!ApprovedRegions.Contains(beat.Region))
                    throw new InvalidOperationException("Draft location is not a confirmed World Five region.");
                if (string.IsNullOrWhiteSpace(beat.DraftTitle) ||
                    string.IsNullOrWhiteSpace(beat.AuthoringIntent) ||
                    string.IsNullOrWhiteSpace(beat.PlayerClarityCue) ||
                    string.IsNullOrWhiteSpace(beat.EnvironmentKey) ||
                    !beat.EnvironmentKey.StartsWith("w5-",StringComparison.Ordinal))
                    throw new InvalidOperationException("Missing draft direction or incorrect world scene key.");
                if (beat.Footprint == OpeningFieldFootprint.Extended && beat.Order < 8)
                    throw new InvalidOperationException("Do not teach on an extended field in the opening tutorial.");
                if (!scenes.Add(beat.EnvironmentKey))
                    throw new InvalidOperationException("Opening beat reused a visual scene identity.");
                kinds.Add(beat.PrimaryGoal); lessons.Add(beat.Lesson);
            }
            if (kinds.Count < 5 || lessons.Count < 8)
                throw new InvalidOperationException("Insufficient core tutorial/objective variety.");
            if (Missions[0].PrimaryGoal != ObjectiveKind.Stabilize ||
                Missions[0].Lesson != OpeningLesson.Movement ||
                Missions[9].PrimaryGoal != ObjectiveKind.Stabilize)
                throw new InvalidOperationException("The opening narrative/tutorial sequence has changed unexpectedly.");
        }
    }
}
