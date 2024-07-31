using DSharpPlus.Entities;

namespace VAssist.Models
{
    internal class NarrativePointTrackerModel
    {
        internal ulong? DirectorId { get; init; } = null;
        internal int PartyNarrativePoints { get; init; }
        internal int DirectorNarrativePoints { get; init; }
        internal (string Name, string Value) InitialPoints { get; init; }
        internal required List<List<DiscordComponent>> ButtonRows { get; init; }
    }
}