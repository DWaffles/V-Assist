using System.Text.RegularExpressions;

namespace VAssist.Common
{
    internal static partial class Util
    {
        [GeneratedRegex(@"\d+")]
        private static partial Regex NumberRegex();
        [GeneratedRegex(@"<@\d{17,18}>")]
        private static partial Regex MentionRegex();
        internal static ulong ParseUlong(string str)
        {
            var match = NumberRegex().Match(str);
            if (match.Success)
            {
                return ulong.Parse(match.Value);
            }
            else
            {
                throw new ArgumentException();
            }
        }
        internal static int ParseInt(string str)
        {
            var match = NumberRegex().Match(str);
            if (match.Success)
            {
                return int.Parse(match.Value);
            }
            else
            {
                throw new ArgumentException();
            }
        }
        internal static int? ParseIntOrNull(string str)
        {
            var match = NumberRegex().Match(str);
            return match.Success ? int.Parse(match.Value) : null;
        }
        internal static ulong? ParseUlongOrNull(string str)
        {
            var match = NumberRegex().Match(str);
            return match.Success ? ulong.Parse(match.Value) : null;
        }
        internal static List<ulong> MatchNumbers(string str)
        {
            return NumberRegex().Matches(str).Select(match => ulong.Parse(match.Value)).ToList();
        }
        internal static bool TryParseMention(string str, out ulong id)
        {
            var match = MentionRegex().Match(str);
            id = match.Success ? ParseUlong(match.Value) : default;
            return match.Success;
        }
    }
}