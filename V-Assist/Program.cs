using VAssist.Common;

namespace VAssist
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var config = FileHandler.ReadConfig(Path.Combine("data", "config.json"));
            VAssist bot = new(config);
            bot.Run().GetAwaiter().GetResult();
        }
    }
}