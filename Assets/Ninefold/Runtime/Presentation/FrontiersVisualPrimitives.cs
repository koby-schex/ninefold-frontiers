using UnityEngine;
using UnityEngine.UIElements;

namespace Ninefold.Presentation
{
    /// <summary>
    /// Presentation-only bridge. A separate model-rendering system may register a
    /// preview source after its own assets are ready. Never owns prefabs, cameras,
    /// animation state or RenderTextures.
    /// </summary>
    public interface IFrontiersPreviewSource
    {
        Texture GetUnitPreview(string unitId);
        Texture GetCampaignPreview(string campaignId);
    }

    public static class FrontiersPreviewRegistry
    {
        /// <summary>Optional. Null gives the safe abstract fallback.</summary>
        public static IFrontiersPreviewSource Source { get; set; }
    }

    /// <summary>
    /// Opt-in visual building blocks. World/faction content is supplied by
    /// existing authoritative game data; no speculative canon art or unit
    /// mappings are defined here.
    /// </summary>
    internal static class FrontiersVisualPrimitives
    {
        private static Label Label(VisualElement parent, string value, string className)
        {
            var label = new Label(value) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            parent.Add(label);
            return label;
        }

        private static void PreviewTexture(VisualElement frame, Texture texture)
        {
            if (texture == null) return;
            var image = new Image
            {
                image = texture,
                scaleMode = ScaleMode.ScaleAndCrop,
                pickingMode = PickingMode.Ignore
            };
            image.AddToClassList("vif-preview-image");
            frame.Add(image);
        }

        public static void CampaignArt(VisualElement parent, string campaignId, int completed, int total, bool locked)
        {
            var frame = new VisualElement { pickingMode = PickingMode.Ignore };
            frame.AddToClassList("vif-campaign-art");
            frame.EnableInClassList("vif-preview-locked", locked);
            parent.Add(frame);

            var texture = FrontiersPreviewRegistry.Source?.GetCampaignPreview(campaignId);
            PreviewTexture(frame, texture);

            if (texture == null)
            {
                var pattern = new VisualElement { pickingMode = PickingMode.Ignore };
                pattern.AddToClassList("vif-atlas-pattern");
                frame.Add(pattern);
                var center = new VisualElement { pickingMode = PickingMode.Ignore };
                center.AddToClassList("vif-atlas-center");
                pattern.Add(center);
                Label(frame, "CAMPAIGN PATH", "vif-preview-placeholder");
            }

            var ribbon = new VisualElement { pickingMode = PickingMode.Ignore };
            ribbon.AddToClassList("vif-campaign-ribbon");
            frame.Add(ribbon);
            Label(ribbon, locked ? "LOCKED" : "EXPLORATION", "vif-campaign-type");
            Label(ribbon, completed + " / " + total, "vif-campaign-count");
        }

        public static void UnitPreview(VisualElement parent, string unitId, bool apex, bool owned)
        {
            var frame = new VisualElement { pickingMode = PickingMode.Ignore };
            frame.AddToClassList("vif-unit-preview");
            frame.EnableInClassList("vif-preview-locked", !owned);
            parent.Add(frame);

            // RenderTexture and ordinary Texture are both accepted here.
            // The producer owns the render cycle and asset lifetime.
            var texture = FrontiersPreviewRegistry.Source?.GetUnitPreview(unitId);
            PreviewTexture(frame, texture);

            if (texture == null)
            {
                var placeholder = new VisualElement { pickingMode = PickingMode.Ignore };
                placeholder.AddToClassList("vif-unit-placeholder");
                frame.Add(placeholder);
                var orbit = new VisualElement { pickingMode = PickingMode.Ignore };
                orbit.AddToClassList("vif-preview-orbit");
                placeholder.Add(orbit);
                Label(placeholder, FrontiersPlaytest.UnitName(unitId), "vif-preview-id");
                Label(frame, "3D PREVIEW PENDING", "vif-preview-placeholder");
            }

            var status = new VisualElement { pickingMode = PickingMode.Ignore };
            status.AddToClassList("vif-preview-caption");
            frame.Add(status);
            Label(status, apex ? "APEX UNIT" : "STANDARD UNIT", "vif-preview-kind");
            Label(status, owned ? "UNLOCKED" : "LOCKED", "vif-preview-state");
        }

        public static void HudChip(VisualElement parent, string value, string caption)
        {
            var chip = new VisualElement { pickingMode = PickingMode.Ignore };
            chip.AddToClassList("vif-hud-chip");
            parent.Add(chip);
            Label(chip, value, "vif-hud-value");
            Label(chip, caption, "vif-hud-label");
        }
    }
}
