using System;
using System.Linq;

namespace ArchepelegoXIV.Rando
{
    internal static class Logic
    {
        public static Func<ApState, bool> Always() => (state) => true;

        public static Func<ApState, bool> HasItem(string Item) => (state) => state.Items.Contains(Item);

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
    }
}
