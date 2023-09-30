using ArchepelegoXIV.Rando;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArchepelegoXIV
{
    public class ApState
    {
        internal BaseGame Game { get; set; }

        public ApState()
        {
            Game = new BMBGame(this);
            territory = Data.Territories[0];
        }

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
        public IEnumerable<string> Items => session?.Items.AllItemsReceived.Select(i => session.Items.GetItemName(i.Item)) ?? Array.Empty<string>();
        public IEnumerable<Location> MissingLocations { get; private set; }

        internal void Connect(string address)
        {
            var localPlayer = DalamudApi.ClientState.LocalPlayer;
            if (localPlayer == null)
                return;

            if (localPlayer.ClassJob.Id == Data.ClassJobs.First(j => j.Abbreviation == "BLU").RowId)
            {
                DalamudApi.Echo("Blue Mage Bingo");
                Game = new BMBGame(this);
            }

            this.session = ArchipelagoSessionFactory.CreateSession(address);
            this.session.MessageLog.OnMessageReceived += MessageLog_OnMessageReceived;
            DalamudApi.Echo($"Connecting as {localPlayer.Name} Playing {Game.Name}");
            var result = this.session.TryConnectAndLogin(Game.Name, localPlayer.Name.ToString(), Archipelago.MultiClient.Net.Enums.ItemsHandlingFlags.AllItems, tags: new string[] { "dalamud" });
            Connected = result.Successful;
            if (!result.Successful)
            {
                foreach (var e in ((LoginFailure)result).Errors)
                    DalamudApi.Echo(e);
                return;
            }

            var loginSuccessful = (LoginSuccessful)result;
            slot = loginSuccessful.Slot;

            this.session.Items.ItemReceived += Items_ItemReceived;
        }

        private void Items_ItemReceived(ReceivedItemsHelper helper)
        {
            var item = helper.DequeueItem();
            var name = session?.Items.GetItemName(item.Item);
            //DalamudApi.Echo($"{name}");
            this.RefreshLocations(false);
        }

        private void MessageLog_OnMessageReceived(Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage message)
        {
            DalamudApi.Echo(message.ToString());
        }

        public void RefreshLocations(bool hard)
        {
            if (hard || MissingLocations == null || !MissingLocations.Any())
                MissingLocations = session?.Locations.AllMissingLocations.Select(i => new Location(this, i)) ?? Array.Empty<Location>();
            else
            {
                foreach (var l in MissingLocations)
                    l.stale = true;
            }

        }
    }
}
