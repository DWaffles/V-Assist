using VAssist.Services;

namespace VAssist.Trackers
{
    internal class TurnTrackerModel
    {
        internal required ulong DirectorId { get; init; }
        internal required ulong? ControllerId { get; init; } = null;
        internal required int TurnNumber { get; init; }
        internal required string RotationHistory { get; init; }
        //internal required string Action { get; init; }
        internal required string TrackerVersion { get; init; }

        internal List<TurnTrackerTeamModel> Teams { get; init; } = [];
    }
    internal class TurnTrackerTeamModel
    {
        internal required string TeamName { get; init; }
        internal required List<TurnTrackerCharacterModel> Characters { get; init; } = [];
        public override string ToString()
        {
            if (Characters.Count != 0)
                return string.Join('\n', Characters.Select(character => character.ToString()));
            else
                return "Empty";
        }
    }
    internal class TurnTrackerCharacterModel
    {
        internal required string? CharacterName { get; init; }
        internal required ulong? PlayerID { get; init; }
        internal required int ReactionsAvailable { get; init; }
        internal required int ReactionsTotal { get; init; }
        internal required bool TurnAvailable { get; init; }
        public override string ToString()
        {
            string str = string.Empty;
            if (TurnAvailable)
            {
                if (ReactionsAvailable > 0)
                    str += TurnTrackerService.Green;
                else
                    str += TurnTrackerService.Blue;
            }
            else
            {
                if (ReactionsAvailable > 0)
                    str += TurnTrackerService.Orange;
                else
                    str += TurnTrackerService.Red;
            }
            str += " ";
            str += CharacterName is null ? $"<@{PlayerID}>" : CharacterName;
            str += $" [{ReactionsAvailable}/{ReactionsTotal}]";
            return str;
        }
    }
}
