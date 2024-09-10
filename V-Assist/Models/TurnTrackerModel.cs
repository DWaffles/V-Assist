using VAssist.Services;

namespace VAssist.Trackers
{
    internal class TurnTrackerModel
    {
        internal required ulong DirectorId { get; set; }
        internal required ulong? ControllerId { get; set; } = null;
        internal required int TurnNumber { get; set; }
        internal required string RotationHistory { get; set; }
        internal required string TrackerVersion { get; init; }
        internal List<TurnTrackerTeamModel> Teams { get; init; } = [];
    }
    internal class TurnTrackerTeamModel
    {
        internal required string TeamName { get; set; }
        internal required List<TurnTrackerCharacterModel> Characters { get; init; } = [];
        public override string ToString()
        {
            if (Characters.Count != 0)
                return string.Join('\n', Characters.Select(character => character.ToString()));
            else
                return Resources.TurnTracker.TeamFieldValueDefault;
        }
    }
    internal class TurnTrackerCharacterModel
    {
        internal required string? CharacterName { get; init; }
        internal required ulong? PlayerID { get; init; }
        internal required int ReactionsAvailable { get; set; }
        internal required int ReactionsMax { get; set; }
        internal required bool TurnAvailable { get; set; }
        internal required bool SelectedByDirector { get; set; }
        internal string? Mention()
        {
            return PlayerID == null ? null : $"<@{PlayerID}>";
        }
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
            str += Mention() ?? CharacterName;
            str += $" [{ReactionsAvailable}/{ReactionsMax}]";
            return str;
        }
        public override bool Equals(object? obj)
        {
            return obj is TurnTrackerCharacterModel model &&
                   CharacterName == model.CharacterName &&
                   PlayerID == model.PlayerID;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(CharacterName, PlayerID);
        }
    }
}
