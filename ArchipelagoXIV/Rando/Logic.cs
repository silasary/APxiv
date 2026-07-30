using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ArchipelagoXIV.Rando
{
    public static partial class Logic
    {
        public static Func<ApState, bool, bool> Always() => (state, asCurrentClass) => true;

        public static Func<ApState, bool, bool> HasItem(string Item, string? Quantity = null) => (state, asCurrentClass) =>
        {
            if (Item.StartsWith("|"))
            {
                var m = Regexes.itemRegex.Match(Item);
                return HasItem(m.Groups["ItemName"].Value, m.Groups["Quantity"]?.Value)(state, asCurrentClass);
            }
            if (Quantity != null)
            {
                var q = int.Parse(Quantity);
                return state.Items.Count(i => i == Item) >= q;
            }
            return state.Items.Contains(Item);
        };

        public static Func<ApState, bool, bool> FromString(string requires)
        {
            var rules = (from Match m in Regexes.itemRegex.Matches(requires)
                         select HasItem(m.Groups["ItemName"].Value, m.Groups["Quantity"]?.Value)).ToArray();
            if (rules.Length != 0)
                return (state, asCurrentClass) => rules.All(r => r(state, asCurrentClass));
            if (string.IsNullOrEmpty(requires))
                return Always();
            DalamudApi.Echo($"Could not parse Requires string: {requires}");
            return Always();
        }

        internal static Func<ApState, bool, bool> Level(int level) => (state, asCurrentClass) =>
        {
            if (level < 5)
                return true;
            var gLevel = asCurrentClass ? state.Game.MaxLevel(state.lastJob) : state.Game.MaxLevel();
            return gLevel >= level;
        };

        // Class quests, BLU duties, etc
        internal static Func<ApState, bool, bool>? Level(int level, string job) => (state, asCurrentClass) =>
            {
                if (asCurrentClass && state.lastJob.Abbreviation != job)
                    return false;
                if (level < 5)
                    return true;
                var gLevel = state.Game.MaxLevel(job);
                return gLevel >= level;
            };

        internal static Func<ApState, bool, bool>? LevelDOHDOL(int level) => (state, asCurrentClass) =>
        {
            var gLevel = asCurrentClass ? state.Game.MaxLevel(state.lastJob) : state.Game.MaxLevelDHL();
            return gLevel >= level;
        };

        internal static Func<ApState, bool, bool>? And(params Func<ApState, bool, bool>?[] rules) => (state, asCurrentClass) =>
        {
            return rules.Where(r => r != null).All(r => r!(state, asCurrentClass));
        };
    }
}
