using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Lumina.Excel.Sheets;

namespace ArchipelagoXIV
{
    public static class Extensions
    {
        public static string ReceivingPlayerName(this Hint hint, ApState state)
        {
            var playerinfo = state.session?.Players.GetPlayerInfo(hint.ReceivingPlayer);
            if (playerinfo == null) {
                return $"Unknown Player {hint.ReceivingPlayer}";
            }
            if (playerinfo.Alias != null && playerinfo.Alias != playerinfo.Name)
                return $"{playerinfo.Alias} ({playerinfo.Name})";

            return playerinfo.Name;
        }

        public static string ItemName(this Hint hint, ApState state)
        {
            if (state.session == null)
                return $"Unknown Item {hint.ItemId}";
            var playerInfo = state.session.Players.GetPlayerInfo(hint.ReceivingPlayer);
            if (playerInfo == null)
                return $"Unknown Item {hint.ItemId}";
            return state.session.Items.GetItemName(hint.ItemId, playerInfo.Game);
        }

        public static ushort APColourToUIColour(this Color color)
        {
            if (color == Color.Red)
                return 17;
            if (color == Color.Green)
                return 45;
            if (color == Color.Yellow)
                return 62;
            if (color == Color.Blue)
                return 38;
            if (color == Color.Magenta)
                return 522;
            if (color == Color.Cyan)
                return 69;
            if (color == Color.Black)
                return 0;
            if (color == Color.White)
                return 1;
            if (color == Color.SlateBlue)
                return 57;
            if (color == Color.Salmon)
                return 537;
            if (color == Color.Plum)
                return 49;
            DalamudApi.PluginLog.Error($"Unknown color {color.R},{color.G},{color.B}");
            return 1;
            //var v1 = new Vector4(color.R, color.G, color.B, 255);
            //return (ushort)ImGui.ColorConvertFloat4ToU32(v1);
        }

        /// <summary>
        /// Is this classjob Row 18 (FSH)?
        /// </summary>
        /// (This is entirely for code readability, because I don't want magic numbers everywhere)
        /// <param name="classJob"></param>
        /// <returns></returns>
        public static bool IsFisher(this ClassJob classJob) => classJob.RowId == 18;
    }
}
