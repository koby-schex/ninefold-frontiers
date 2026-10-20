using UnityEngine;

namespace Ninefold.Presentation
{
    /// <summary>
    /// Internal encounter art study. It never defines gameplay geometry,
    /// canonical locations, lore, squad size, or a player-facing map scale.
    /// Each world and mode can eventually supply its own approved scenery kit.
    /// </summary>
    internal sealed class BattlefieldSceneDirection
    {
        internal readonly string Study;
        internal readonly Color Floor, Cover, Stone, Ceramic, Distant, Haze, Plants, Sky, Fill;
        internal readonly int HorizonDetail, PlantDetail;
        internal readonly float SilhouetteHeight;

        private BattlefieldSceneDirection(string study, Color floor, Color cover, Color stone,
            Color ceramic, Color distant, Color haze, Color plants, Color sky, Color fill,
            int horizonDetail, int plantDetail, float silhouetteHeight)
        {
            Study=study; Floor=floor; Cover=cover; Stone=stone; Ceramic=ceramic;
            Distant=distant; Haze=haze; Plants=plants; Sky=sky; Fill=fill;
            HorizonDetail=horizonDetail; PlantDetail=plantDetail; SilhouetteHeight=silhouetteHeight;
        }

        internal static BattlefieldSceneDirection ForMission(string id, string environmentKey)
        {
            // Authored content may choose a scene mood without coupling the
            // renderer to faction/gameplay IDs. The old fixture ID is a fallback
            // only while recovering a previous layout revision.
            string key = environmentKey ?? id;
            // Avarin/Continuance World Five-inspired material *studies* only;
            // neither represents a canon-approved named site. Different
            // encounters must not reuse a flat backdrop and identical shelves.
            if (key == "study-w5-survey" || key == "fixture-opening")
                return new BattlefieldSceneDirection("W5 study · pale survey corridor",
                    C(.37f,.34f,.37f),C(.54f,.50f,.52f),C(.38f,.31f,.36f),
                    C(.68f,.62f,.63f),C(.31f,.27f,.38f),C(.24f,.22f,.31f),
                    C(.34f,.40f,.34f),C(.16f,.13f,.21f),C(.81f,.74f,.89f),7,15,.8f);
            if (key == "study-w5-fracture" || key == "fixture-finale")
                return new BattlefieldSceneDirection("W5 study · deep mineral fracture",
                    C(.28f,.26f,.33f),C(.46f,.43f,.51f),C(.29f,.25f,.38f),
                    C(.58f,.53f,.62f),C(.23f,.19f,.34f),C(.16f,.14f,.25f),
                    C(.25f,.31f,.28f),C(.10f,.09f,.17f),C(.65f,.58f,.84f),11,9,1.35f);

            // Other abstract fixtures deliberately avoid claiming a specific
            // world's canon before that world's campaign and art are approved.
            if (key == "study-neutral-other" || key == "fixture-next")
                return new BattlefieldSceneDirection("Neutral faction-B fixture",
                    C(.33f,.34f,.34f),C(.50f,.51f,.50f),C(.32f,.35f,.36f),
                    C(.61f,.63f,.62f),C(.25f,.33f,.37f),C(.20f,.26f,.29f),
                    C(.33f,.39f,.37f),C(.12f,.18f,.21f),C(.69f,.81f,.85f),8,11,1.1f);
            return new BattlefieldSceneDirection("Neutral mixed-mode fixture",
                C(.36f,.34f,.32f),C(.53f,.48f,.44f),C(.37f,.31f,.27f),
                C(.64f,.57f,.51f),C(.37f,.29f,.27f),C(.26f,.21f,.20f),
                C(.35f,.34f,.28f),C(.18f,.15f,.16f),C(.83f,.72f,.66f),6,7,.7f);
        }

        private static Color C(float r,float g,float b) => new Color(r,g,b);
    }
}
