using System.Text.Json.Serialization;

namespace VAssist.Common
{
    internal class BotConfig
    {
        [JsonPropertyName("token"), JsonInclude]
        public string Token { get; init; } = "token";

        [JsonPropertyName("prefixes"), JsonInclude]
        public string[] CommandPrefixes { get; init; } = ["%"];

        [JsonPropertyName("status"), JsonInclude]
        public string Status { get; init; } = "for snake-eyes";

        [JsonPropertyName("hex_code"), JsonInclude]
        public string HexCode { get; init; } = "bc2019";
    }

    [JsonSerializable(typeof(BotConfig))]
    internal partial class BotConfigContext : JsonSerializerContext { }
}