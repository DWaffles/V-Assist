using System.Text.RegularExpressions;

namespace VAssist.Common
{
    internal static partial class Util
    {
        [GeneratedRegex(@"\d+")]
        private static partial Regex NumberRegex();
        internal static void ParseNarrativePointFieldValue(string value, out ulong unixTime, out string? reason)
        {
            var matches = NumberRegex().Matches(value);
            unixTime = (ulong) ParseUlong(matches[1].Value);

            if (value.Contains("Reason: "))
            {
                int index = value.IndexOf("Reason: ");
                reason = value.Substring(index + "Reason: ".Length);
            }
            else
            {
                reason = null;
            }
        }
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