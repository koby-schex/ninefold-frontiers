using System;
using System.Collections.Generic;
using System.Linq;

namespace Ninefold.Core.Content
{
    /// <summary>
    /// Optional, non-exclusive UI hints. These are NOT classes, factions,
    /// restrictions, ability types, or squad composition requirements.
    /// Canonical units receive tags only after lore review.
    /// </summary>
    [Flags]
    public enum UnitPreviewTag
    {
        None=0,
        Frontline=1,
        Damage=2,
        Support=4
    }

    public static class UnitPreviewTags
    {
        public static IReadOnlyList<UnitPreviewTag> For(UnitPreviewTag tags)
        {
            if((tags & ~(UnitPreviewTag.Frontline|UnitPreviewTag.Damage|UnitPreviewTag.Support))!=0)
                throw new ArgumentOutOfRangeException(nameof(tags));
            return Array.AsReadOnly(new[]{
                UnitPreviewTag.Frontline,UnitPreviewTag.Damage,UnitPreviewTag.Support
            }.Where(tag=>(tags&tag)!=0).ToArray());
        }
    }
}
