using System;
using System.Collections.Generic;
using System.Linq;

namespace Ninefold.Core.Content
{
    /// <summary>Visual-only data for compact unit-preview hints. These do not
    /// define classes, faction requirements or combat behavior.</summary>
    public sealed class UnitPreviewTagHint
    {
        public UnitPreviewTag Tag { get; }
        public string Label { get; }
        public string IconKey { get; }
        public UnitPreviewTagHint(UnitPreviewTag tag,string label,string iconKey)
        { Tag=tag; Label=label; IconKey=iconKey; }
    }

    public static class UnitPreviewTagPresentation
    {
        public static IReadOnlyList<UnitPreviewTagHint> Get(UnitPreviewTag tags)
        {
            var result=UnitPreviewTags.For(tags).Select(tag=>
                tag==UnitPreviewTag.Frontline
                    ? new UnitPreviewTagHint(tag,"Frontline","shield")
                : tag==UnitPreviewTag.Damage
                    ? new UnitPreviewTagHint(tag,"Damage","swords")
                : new UnitPreviewTagHint(tag,"Support","heart-handshake")).ToArray();
            return Array.AsReadOnly(result);
        }
    }
}
