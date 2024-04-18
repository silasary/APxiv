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
    }
}
