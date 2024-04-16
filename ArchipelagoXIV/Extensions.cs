using Archipelago.MultiClient.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
