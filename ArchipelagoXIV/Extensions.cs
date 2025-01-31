using Archipelago.MultiClient.Net.Models;

namespace ArchipelagoXIV
{
    public static class Extensions
    {
        public static string ReceivingPlayerName(this Hint hint, ApState state)
        {
            return state.session.Players.GetPlayerAliasAndName(hint.ReceivingPlayer);
        }

        public static string ItemName(this Hint hint, ApState state)
        {
            return state.session.Items.GetItemName(hint.ItemId);
        }

        public static ushort APColourToUIColour(this Color color)
        {
            if (color == Color.Red)
                return 17;
            if (color == Color.Blue)
                return 38;
            if (color == Color.Green)
                return 45;
            if (color == Color.Yellow)
                return 62;
            if (color == Color.Plum)
                return 49;
            if (color == Color.SlateBlue)
                return 57;
            if (color == Color.Cyan)
                return 69;
            if (color == Color.Salmon)
                return 537;
            DalamudApi.PluginLog.Error($"Unknown color {color}");
            return 1;

        }
    }
}
