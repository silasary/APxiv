using System.Text.RegularExpressions;

namespace ArchipelegoXIV.Rando
{
    internal static partial class Regexes
    {

        [GeneratedRegex("([A-Za-z ']+): FATE #(\\d+)")]
        private static partial Regex FateRegex();

        [GeneratedRegex("\\|([\\w ']+):(\\d)\\|")]
        private static partial Regex ItemRegex();


        public static readonly Regex FATE = FateRegex();
        public static readonly Regex itemRegex = ItemRegex();

    }
}
