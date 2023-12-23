using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ArchipelegoXIV.Rando
{
    internal static partial class Logic
    {
        private static readonly Regex itemRegex = ItemRegex();
        public static Func<ApState, bool> Always() => (state) => true;

        public static Func<ApState, bool> HasItem(string Item) => (state) =>
        {
            if (Item.StartsWith("|"))
            {
                var m = itemRegex.Match(Item);
                return state.Items.Contains(m.Groups[1].Value);
            }
            return state.Items.Contains(Item);
        };

        internal static Func<ApState, bool> Level(int level)
        {
            return (state) =>
            {
                if (state.Game.Name == "Manual_FFXIVBMB_Pizzie")
                {
                    var gLevel = state.Items.Count(i => i == "10 Equip Levels") * 10;
                    return gLevel >= level;
                }
                throw new NotImplementedException();
                return false;
            };
        }

        [GeneratedRegex("\\|([\\w ]+):(\\d)\\|")]
        private static partial Regex ItemRegex();
    }
}
