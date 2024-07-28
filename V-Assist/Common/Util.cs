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
            unixTime = ParseInt(matches[1].Value);

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
        internal static ulong ParseInt(string str)
        {
            return ulong.Parse(NumberRegex().Match(str).Value);
        }
        internal static List<ulong> MatchNumbers(string str)
        {
            return NumberRegex().Matches(str).Select(match => ulong.Parse(match.Value)).ToList();
        }
    }
}