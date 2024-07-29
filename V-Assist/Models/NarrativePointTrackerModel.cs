using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VAssist.Models
{
    internal class NarrativePointTrackerModel
    {
        //internal string? SessionName { get; init; } = null;
        //internal DiscordUser? DirectorId { get; init; } = null;
        internal ulong? DirectorId { get; init; } = null;
        internal int PartyNarrativePoints { get; init; }
        internal int DirectorNarrativePoints { get; init; }
        //internal int TotalNarrativePoints { get; init; }
        internal (string Name, string Value) InitialPoints { get; init; }
        
        internal List<(string Name, string Value)> PointChanges { get; init; } = []; //fields // necessary?
        internal List<List<DiscordComponent>> ButtonRows { get; init; }
    }
}