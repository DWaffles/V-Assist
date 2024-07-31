using System.Text.RegularExpressions;

namespace VAssist.Common
{
    internal static partial class Util
    {
        [GeneratedRegex(@"\d+")]
        private static partial Regex NumberRegex();
        internal static ulong? ParseUlong(string str)
        {
            var match = NumberRegex().Match(str);
            if (match.Success)
            {
                return ulong.Parse(match.Value);
            }
            else
            {
                return null;
            }
        }
        internal static List<ulong> MatchNumbers(string str)
        {
            return NumberRegex().Matches(str).Select(match => ulong.Parse(match.Value)).ToList();
        }
    }
}