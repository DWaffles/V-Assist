using Serilog;
using System.Text;
using System.Text.Json;

namespace VAssist.Common
{
    /// <summary>
    /// Internal helper class for handling all the file I/O operations of the bot.
    /// </summary>
    internal static class FileHandler
    {
        /// <summary>
        /// Returns a <see cref="BotConfig"/> from the given path.
        /// </summary>
        /// <param name="configName">Relative path and name to the config file.</param>
        /// <exception cref="FileNotFoundException">The given file could not be found.</exception>
        /// <returns><see cref="BotConfig"/></returns>
        internal static BotConfig? ReadConfig(string configName)
        {
            if (!File.Exists(configName))
            {
                WriteDefaultToFile(configName);
                Log.Error($"Missing {configName}, created template config file.");
                throw new FileNotFoundException($"{configName} does not exist, created template config file at location.");
            }
            else
                return ReadJsonFile(configName);
        }

        /// <summary>
        /// Verifies if a <see cref="BotConfig"/> is a valid config.
        /// </summary>
        /// <remarks>
        /// Checks if the token is: null, empty, or 'token'. Checks if there is at least one non-whitespace prefix.
        /// </remarks>
        /// <param name="config">Config to check validity for.</param>
        /// <returns><see cref="bool"/></returns>
        internal static bool VerifyConfig(BotConfig? config)
        {
            return !(false
                || config == null // invalid
                || String.IsNullOrEmpty(config?.Token) // invalid
                || config.Token.Equals("token", StringComparison.OrdinalIgnoreCase) // invalid
                || config.CommandPrefixes.Length == 0 // invalid
                || string.IsNullOrWhiteSpace(config.CommandPrefixes[0])); // invalid
        }

        /// <summary>
        /// Will parse the given file and return a constructed object of the given generic type from the data.
        /// </summary>
        /// <remarks>Will update the file with missing object fields where applicable.</remarks>
        /// <typeparam name="Type">Generic type to parse and return.</typeparam>
        /// <param name="fileName">Relative path and name to the file.</param>
        /// <exception cref="FileNotFoundException">The given file could not be found.</exception>
        /// <returns>Object of given type.</returns>
        internal static BotConfig? ReadJsonFile(string fileName)
        {
            FileInfo file = new(fileName);
            if (!file.Exists)
            {
                throw new FileNotFoundException($"Was not able to find {fileName} at specified location.");
            }

            string json = File.ReadAllText(file.FullName, new UTF8Encoding(false));
            BotConfig? readObject = JsonSerializer.Deserialize<BotConfig>(json, BotConfigContext.Default.BotConfig);

            // Updating config with new fields
            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
            };
            json = JsonSerializer.Serialize(readObject, options);
            File.WriteAllText(file.FullName, json, new UTF8Encoding(false));

            return readObject;
        }

        /// <summary>
        /// Will overwrite or create a file using the given name with a default object of the generic type.
        /// </summary>
        /// <typeparam name="Type">Generic type to output the default for.</typeparam>
        /// <param name="fileName">Relative path and name to the file.</param>
        internal static void WriteDefaultToFile(string fileName)
        {
            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
            };
            string template = JsonSerializer.Serialize(new BotConfig(), options);

            var file = new FileInfo(fileName);
            file.Directory?.Create();
            File.WriteAllText(fileName, template, new UTF8Encoding(false));
        }
    }
}