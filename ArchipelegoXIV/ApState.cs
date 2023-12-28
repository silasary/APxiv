using ArchipelegoXIV.Rando;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using Dalamud.Game.Text.SeStringHandling;
using System.Text;

namespace ArchipelegoXIV
{
    public class ApState
    {
        internal BaseGame Game { get; set; }

        public ApState()
        {
            Game = new NGPlusGame(this);
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
                var localPlayer = DalamudApi.ClientState.LocalPlayer;

                var job = localPlayer.ClassJob.GameData.Abbreviation;
                return $"{territory}\n{territoryName}\n{territoryRegion}\n\nMax Level: {Game.MaxLevel()}\nMax {job}: {Game.MaxLevel(job)}";
            }
        }
        public string ToolTipText { get
            {
                var sb = new StringBuilder();
                sb.AppendLine("Job Levels:");
                foreach (var job in Data.ClassJobs)
                {
                    if (job.ClassJobCategory.Value.RowId == 30 || job.ClassJobCategory.Value.RowId == 31)
                    {
                        Game.Levels.TryGetValue(job, out int level);
                        if (level > 0)
                            sb.Append(job.Abbreviation).Append(": ").Append(level).AppendLine();
                    }
                }
                sb.AppendLine("Zones:");
                foreach (var zone in Items.Where(i=> i.EndsWith("Access")))
                {
                    sb.AppendLine(zone);
                }
                return sb.ToString();
            }
        }

        public bool Hooked { get; internal set; }
        public bool Connected { get; internal set; }
        public IEnumerable<string> Items => session?.Items.AllItemsReceived.Select(i => session.Items.GetItemName(i.Item)) ?? Array.Empty<string>();
        public IEnumerable<Location> MissingLocations { get; private set; }

        internal void Disconnect()
        {
            if (Connected && (session?.Socket?.Connected ?? false))
                session?.Socket?.DisconnectAsync()?.Wait();
        }
        internal void Connect(string address, string? player = null)
        {
            if (Connected)
            {
                Disconnect();
            }
            DalamudApi.SetStatusBar("Connecting...");
            var localPlayer = DalamudApi.ClientState.LocalPlayer;
            if (localPlayer == null)
                return;

            if (localPlayer.ClassJob.Id == Data.ClassJobs.First(j => j.Abbreviation == "BLU").RowId)
            {
                DalamudApi.Echo("Blue Mage Bingo");
                Game = new BMBGame(this);
            }
            else
            {
                Game = new NGPlusGame(this);
            }

            this.session = ArchipelagoSessionFactory.CreateSession(address);
            this.session.MessageLog.OnMessageReceived += MessageLog_OnMessageReceived;
            if (string.IsNullOrEmpty(player))
            {
                player = localPlayer.Name.ToString();
            }
            DalamudApi.Echo($"Connecting as {player} Playing {Game.Name}");
            var result = this.session.TryConnectAndLogin(Game.Name, player, Archipelago.MultiClient.Net.Enums.ItemsHandlingFlags.AllItems, tags: new string[] { "Dalamud" });
            Connected = result.Successful;
            if (!result.Successful)
            {
                foreach (var e in ((LoginFailure)result).Errors)
                    DalamudApi.Echo(e);
                DalamudApi.SetStatusBar("Connection Failed");
                return;
            }

            var loginSuccessful = (LoginSuccessful)result;
            slot = loginSuccessful.Slot;

            session.Items.ItemReceived += Items_ItemReceived;

            DalamudApi.SetStatusBar("Connected");

        }


        private void Items_ItemReceived(ReceivedItemsHelper helper)
        {
            var item = helper.DequeueItem();
            var name = session?.Items.GetItemName(item.Item);
            var sender = session.Players.GetPlayerName(item.Player);
            DalamudApi.Echo($"Recieved {name} from {sender}");
            Game.ProcessItem(item, itemName: name);
            RefreshRegions();
            this.RefreshLocations(false);
            DalamudApi.SetStatusTooltop(ToolTipText);
        }

        private static void RefreshRegions()
        {
            RegionContainer.MarkStale();
        }

        private void MessageLog_OnMessageReceived(Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage message)
        {
            DalamudApi.Echo(message.ToString());
            if (message.Parts.Any(p => p.Type == MessagePartType.Player && p.Text == session.Players.GetPlayerAlias(slot)))
                DalamudApi.ShowToast(message.ToString());
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
