using System.Text.RegularExpressions;

namespace ArchipelagoXIV.Rando
{
    internal static partial class Regexes
    {

        [GeneratedRegex("([A-Za-z ']+): FATE #(\\d+)")]
        private static partial Regex FateRegex();

        [GeneratedRegex("\\|([\\w ']+):\\s*(\\d)\\|")]
        private static partial Regex ItemRegex();

        [GeneratedRegex("^Masked Carnivale #(\\d+)$")]
        private static partial Regex CarnivaleRegex();

        [GeneratedRegex(@"(.+) \d+$")]
        private static partial Regex ExtraCheck();


        public static readonly Regex FATE = FateRegex();
        public static readonly Regex itemRegex = ItemRegex();
        public static readonly Regex Carnivale = CarnivaleRegex();
        public static readonly Regex ExtraCheckName = ExtraCheck();

    }
}
