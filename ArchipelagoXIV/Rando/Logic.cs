using Lumina.Excel.GeneratedSheets;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ArchipelagoXIV.Rando
{
    internal static partial class Logic
    {
        private static ClassJob? CurrentClass() => DalamudApi.ClientState.LocalPlayer?.ClassJob.GameData;
        public static Func<ApState, bool, bool> Always() => (state, asCurrentClass) => true;

        public static Func<ApState, bool, bool> HasItem(string Item) => (state, asCurrentClass) =>
        {
            if (Item.StartsWith("|"))
            {
                var m = Regexes.itemRegex.Match(Item);
                return state.Items.Contains(m.Groups[1].Value);
            }
            return state.Items.Contains(Item);
        };

        internal static Func<ApState, bool, bool> FromString(string requires)
        {
            var rules = (from Match m in Regexes.itemRegex.Matches(requires)
                         select HasItem(m.Groups[0].Value)).ToArray();
            if (rules.Any())
                return (state, asCurrentClass) => rules.All(r => r(state, asCurrentClass));
            return Always();
        }

        internal static Func<ApState, bool, bool> Level(int level) => (state, asCurrentClass) =>
        {
            if (level < 5)
                return true;
            var gLevel = asCurrentClass ? state.Game.MaxLevel(CurrentClass()) : state.Game.MaxLevel();
            return gLevel >= level;
        };

        // Class quests, BLU duties, etc
        internal static Func<ApState, bool, bool>? Level(int level, string job) => (state, asCurrentClass) =>
            {
                if (asCurrentClass && CurrentClass().Abbreviation != job)
                    return false;
                if (level < 5)
                    return true;
                var gLevel = state.Game.MaxLevel(job);
                return gLevel >= level;
            };

        internal static Func<ApState, bool, bool>? LevelDOHDOL(int level) => (state, asCurrentClass) =>
        {
            var gLevel = asCurrentClass ? state.Game.MaxLevel(CurrentClass()) : state.Game.MaxLevelDHL();
            return gLevel >= level;
        };
    }
}
