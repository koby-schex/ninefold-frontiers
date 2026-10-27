using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ninefold.Presentation
{
    /// <summary>
    /// Explicit, opt-in link between authoritative gameplay UnitIds and approved
    /// runtime visual prefabs. Never guesses mappings from art names or lore IDs.
    /// The model-development branch continues to own meshes, rigs and materials.
    /// </summary>
    [CreateAssetMenu(fileName="BattleUnitVisuals",menuName="Ninefold/Frontiers Battle Visual Catalog")]
    public sealed class FrontiersBattleVisualCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Exact gameplay UnitId. Do not enter the model source-art ID.")]
            public string UnitId;

            [Tooltip("Runtime prefab, not a review scene or editor-only art source.")]
            public GameObject Prefab;

            [Tooltip("Enable only after canon, prefab and mobile-performance review.")]
            public bool ApprovedForBattle;

            [Tooltip("Maximum visible width/depth in tactical world units.")]
            [Range(.5f,3.5f)] public float MaximumWidth = 1.35f;

            [Tooltip("Maximum visible height in tactical world units.")]
            [Range(.6f,2.5f)] public float MaximumHeight = 1.45f;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public bool TryGetApproved(string unitId,out Entry entry)
        {
            entry=null;
            if(string.IsNullOrEmpty(unitId)) return false;
            // Duplicate IDs disable auto-selection instead of arbitrarily
            // picking whichever potentially outdated asset appears first.
            Entry found=null;
            foreach(var candidate in entries)
            {
                if(candidate==null || !string.Equals(candidate.UnitId,unitId,StringComparison.Ordinal))
                    continue;
                if(found!=null) return false;
                found=candidate;
            }
            if(found==null || !found.ApprovedForBattle || found.Prefab==null)
                return false;
            entry=found;
            return true;
        }
    }
}
