using ArchipelagoXIV.Rando;
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
using Archipelago.MultiClient.Net.Models;
using Dalamud.Logging;
using System.ComponentModel.Design;

namespace ArchipelagoXIV
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
        internal bool territoryReachable;

        public TerritoryType territory { get; internal set; }
        public string territoryName { get; internal set; }
        public string territoryRegion { get; internal set; }

        public bool CanTeleport { get; internal set; } = true;
        public bool CanReturn { get; internal set; } = true;

        public string JobText
        {
            get
            {
                var localPlayer = DalamudApi.ClientState.LocalPlayer;
                if (localPlayer == null)
                    return null;
                var job = localPlayer.ClassJob.GameData;
                var sb = new StringBuilder();

                var joblvl = Game.MaxLevel(job);
                sb.Append(job.Abbreviation).Append(": ");
                if (localPlayer.Level < joblvl)
                    sb.Append(localPlayer.Level).Append('/');
                sb.Append(joblvl);

                var maxlvl = Game.MaxLevel();
                if (joblvl < maxlvl)
                    sb.Append(" (Best ").Append(maxlvl).Append(')');
                return sb.ToString();
            }
        }
        public string JobTooltip
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine("Job Levels:");
                foreach (var job in Data.ClassJobs)
                {
                    if (job.ClassJobCategory.Value.RowId == 30 || job.ClassJobCategory.Value.RowId == 31)
                    {
                        Game.Levels.TryGetValue(job, out var level);
                        if (level > 0)
                            sb.Append(job.Abbreviation).Append(": ").Append(level).AppendLine();
                    }
                }
                return sb.ToString();
            }
        }

        public bool Hooked { get; internal set; }
        public bool Connected { get; internal set; }
        public IEnumerable<string> Items => session?.Items.AllItemsReceived.Select(i => session.Items.GetItemName(i.Item)) ?? Array.Empty<string>();
        public Location[] MissingLocations { get; private set; } = [];
        public Hint[] Hints { get; private set; }

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
            this.Game.HandleSlotData(loginSuccessful.SlotData);

            session.Items.ItemReceived += Items_ItemReceived;
            session.Socket.SocketClosed += Socket_SocketClosed;
            session.DataStorage.TrackHints(hints => this.Hints = hints, true);

            DalamudApi.SetStatusBar("Connected");

        }

        private void Socket_SocketClosed(string reason)
        {
            this.Connected = false;
            PluginLog.Debug($"Lost connection to Archipelago: {reason}");
            DalamudApi.SetStatusBar("Disconnected");
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
            UpdateBars();
        }

        public void UpdateBars()
        {
            var checks = 0;
            var zoneTT = new StringBuilder();
            APData.Regions.TryGetValue(RegionContainer.LocationToRegion(this.territoryName, (ushort)this.territory.RowId), out var region);
            if (region != null)
            {
                zoneTT.AppendLine($"Available Checks in {region.Name}:");
                foreach (var l in MissingLocations.Where(l => l.region == region))
                {
                    if (l.IsAccessible())
                    {
                        zoneTT.AppendLine(l.Name);
                        checks++;
                    }
                    else
                    {
                        zoneTT.AppendLine(l.Name + "(Unavailable)");
                    }
                }
            }
            zoneTT.AppendLine();
            zoneTT.AppendLine("Zones:");
            foreach (var zone in Items.Where(i => i.EndsWith("Access")))
            {
                zoneTT.AppendLine(zone);
            }

            if (territoryReachable && checks > 0)
                DalamudApi.SetStatusBar($"{checks} checks in {region.Name}");
            else if (territoryReachable)
                DalamudApi.SetStatusBar("In Logic");
            else
                DalamudApi.SetStatusBar("Out of Logic");
            DalamudApi.SetStatusTooltop(zoneTT.ToString());

            DalamudApi.SetJobStatusBar(JobText);
            DalamudApi.SetJobTooltop(JobTooltip);
        }

        private static void RefreshRegions()
        {
            RegionContainer.MarkStale();
        }

        private void MessageLog_OnMessageReceived(Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage message)
        {
            DalamudApi.PvPTeam(message.ToString(), "AP");
            if (message.Parts.Any(p => p.Type == MessagePartType.Player && p.Text == session.Players.GetPlayerAlias(slot)))
                DalamudApi.ShowToast(message.ToString());
        }

        public void RefreshLocations(bool hard)
        {
            if (hard || MissingLocations == null || !MissingLocations.Any())
                MissingLocations = session?.Locations.AllMissingLocations.Select(i => new Location(this, i)).ToArray() ?? [];
            else
            {
                foreach (var l in MissingLocations)
                    l.stale = true;
            }
        }
    }
}
