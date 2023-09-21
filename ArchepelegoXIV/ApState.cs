using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;

namespace ArchepelegoXIV
{
    public class ApState
    {
        internal ArchipelagoSession? session = null;

        internal int slot;

        public TerritoryType territory { get; internal set; }
        public string territoryName { get; internal set; }
        public string territoryRegion { get; internal set; }

        public bool CanTeleport { get; internal set; } = true;
        public bool CanReturn { get; internal set; } = true;

        public string DebugText
        {
            get
            {
                return $"{territory}\n{territoryName}\n{territoryRegion}\n\nHooked: {Hooked}";
            }
        }

        public bool Hooked { get; internal set; }
        public bool Connected { get; internal set; }
        public ReceivedItemsHelper Items { get; internal set; }
        public LocationCheckHelper Locations { get; internal set; }

        internal void Connect(string address)
        {
            this.session = ArchipelagoSessionFactory.CreateSession(address);
            this.session.MessageLog.OnMessageReceived += MessageLog_OnMessageReceived;
            var result = this.session.TryConnectAndLogin("Manual_FFXIVBMB_Pizzie", DalamudApi.ClientState.LocalPlayer.Name.ToString(), Archipelago.MultiClient.Net.Enums.ItemsHandlingFlags.AllItems, tags: new string[] { "dalamud" });
            Connected = result.Successful;
            if (!result.Successful)
            {
                foreach (var e in ((LoginFailure)result).Errors)
                    DalamudApi.Echo(e);
                return;
            }

            var loginSuccessful = (LoginSuccessful)result;
            slot = loginSuccessful.Slot;

            Locations = this.session.Locations;
            Items = this.session.Items;
            this.session.Items.ItemReceived += Items_ItemReceived;
        }

        private void Items_ItemReceived(ReceivedItemsHelper helper)
        {
            
        }

        private void MessageLog_OnMessageReceived(Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage message)
        {
            DalamudApi.Echo(message.ToString());
        }
    }
}
