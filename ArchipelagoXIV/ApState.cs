using ArchipelagoXIV.Rando;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using System.Text;
using Archipelago.MultiClient.Net.Models;
using Dalamud.Logging;
using Newtonsoft.Json;
using System.IO;
using ArchipelagoXIV.Rando.Locations;

namespace ArchipelagoXIV
{
    public class ApState
    {
        public class SaveFile
        {
            public HashSet<long> CompletedChecks = [];
        }

        internal BaseGame Game { get; set; }

        public ApState()
        {
            Game = new NGPlusGame(this);
            territory = Data.Territories[0];
        }

        internal ArchipelagoSession? session = null;

        internal int slot;
        internal ClassJob? lastJob;
        internal int lastFateCount;

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
                this.lastJob = job;
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

        public bool Hooked { get; internal set; }
        public bool Connected { get; internal set; }
        public IEnumerable<string> Items => session?.Items.AllItemsReceived.Select(i => i.ItemDisplayName) ?? Array.Empty<string>();
        public Location[] MissingLocations { get; private set; } = [];
        public Hint[] Hints { get; private set; }
        public SaveFile? localsave { get; private set; }
        public bool Syncing { get; internal set; }

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
            session.Locations.CheckedLocationsUpdated += Locations_CheckedLocationsUpdated;
            session.Socket.SocketClosed += Socket_SocketClosed;
            session.DataStorage.TrackHints(HandleHints, true);
            LoadCache();

            DalamudApi.SetStatusBar("Connected");

        }

        private void Locations_CheckedLocationsUpdated(System.Collections.ObjectModel.ReadOnlyCollection<long> newCheckedLocations)
        {
            RefreshLocations(false);
            UpdateBars();
        }

        private void LoadCache()
        {
            var file = SaveFileName();
            if (File.Exists(file))
            {
                this.localsave = JsonConvert.DeserializeObject<SaveFile>(File.ReadAllText(file));
            }
            else
            {
                this.localsave = new SaveFile();
            }
            this.session!.Locations.CompleteLocationChecksAsync([.. localsave!.CompletedChecks]);
        }

        internal void SaveCache()
        {
            File.WriteAllText(SaveFileName(), JsonConvert.SerializeObject(this.localsave));
        }

        private string SaveFileName() => Path.Combine(
            DalamudApi.PluginInterface.GetPluginConfigDirectory(),
            $"{session!.ConnectionInfo.Slot}_{session.RoomState.Seed}_{session.Players.GetPlayerName(session.ConnectionInfo.Slot)}"
        );

        private void HandleHints(Hint[] hints)
        {
            this.Hints = hints;
            foreach (var hint in hints)
            {
                if (hint.Found)
                    continue;
                var location = MissingLocations.FirstOrDefault(l => l.ApId == hint.LocationId);
                if (location != null)
                    location.HintedItem = hint;
            }
        }

        private void Socket_SocketClosed(string reason)
        {
            this.Connected = false;
            DalamudApi.PluginLog.Debug($"Lost connection to Archipelago: {reason}");
            DalamudApi.SetStatusBar("Disconnected");
        }

        private void Items_ItemReceived(ReceivedItemsHelper helper)
        {
            var item = helper.DequeueItem();
            var name = item.ItemName;
            var sender = session.Players.GetPlayerName(item.Player);
            //DalamudApi.Echo($"Recieved {name} from {sender}");
            Game.ProcessItem(item, itemName: name);
            RefreshRegions();
            this.RefreshLocations(false);
            UpdateBars();
        }

        public void UpdateBars()
        {
            var BK = true;
            var fish = false;
            var checks = 0;
            var fates = 0;
            var upfates = 0;
            var zoneTT = new StringBuilder();
            var unavailable = new StringBuilder();
            var zoneswithchecks = new HashSet<Region>();
            APData.Regions.TryGetValue(RegionContainer.LocationToRegion(this.territoryName, (ushort)this.territory.RowId), out var region);
            if (region != null)
            {
                zoneTT.AppendLine($"Available Checks in {region.Name}:");
                foreach (var l in MissingLocations)
                {
                    if (l.Completed) {
                        continue;
                    }
                    if (l.region == region)
                    {

                        if (l.IsAccessible())
                        {
                            zoneTT.AppendLine(l.DisplayText);
                            checks++;
                            if (l.Name.Contains("FATE"))
                                fates++;
                            if (DalamudApi.FateTable.Any(f => f.Name.ToString() == l.Name.Replace(" (FATE)", "")))
                                upfates++;
                            BK = false;
                        }
                        else
                        {
                            unavailable.AppendLine(l.DisplayText + "(Unavailable)");
                        }
                    }
                    else if (l.IsAccessible())
                    {
                        zoneswithchecks.Add(l.region);
                        BK = false;
                    }
                    if (!fish && l is Fish)
                        fish = true;
                }
            }
            zoneTT.AppendLine();
            zoneTT.AppendLine("Zones with checks:");
            //foreach (var zone in Items.Where(i => i.EndsWith("Access")))
            //{
            //    if (RegionContainer.CanReach(this, zone.Replace(" Access", "")))
            //        zoneTT.AppendLine(zone);
            //}
            foreach (var z in zoneswithchecks)
            {
                if (RegionContainer.CanReach(this, z))
                    zoneTT.AppendLine(z.Name);
                else
                    zoneTT.AppendLine($"{z.Name} (Unreachable)");
            }
            if (unavailable.Length > 0)
            {
                zoneTT.AppendLine().AppendLine($"Unavailable checks in {region.Name}:");
                zoneTT.Append(unavailable);
            }
            if (checks > 0)
            {
                var text = $"{checks} checks in {region.Name}";
                if (upfates > 0)
                    text += $" ({upfates} active FATEs)";
                else if (fates > 0)
                    text += $" ({fates} FATEs)";
                DalamudApi.SetStatusBar(text);
            }
            else if (region == null)
            {
                DalamudApi.SetStatusBar($"??? ({this.territoryName})");
            }
            else if (BK)
                DalamudApi.SetStatusBar("BK");
            else if (RegionContainer.CanReach(this, region))
                DalamudApi.SetStatusBar("In Logic");
            else
                DalamudApi.SetStatusBar("Out of Logic");
            DalamudApi.SetStatusTooltop(zoneTT.ToString());

            DalamudApi.SetJobStatusBar(JobText);

            var jobtt = new StringBuilder();
            jobtt.AppendLine("Job Levels:");
            foreach (var job in Data.ClassJobs)
            {
                if (job.ClassJobCategory.Value.RowId == 30 || job.ClassJobCategory.Value.RowId == 31 || (fish && job.RowId == 18))
                {
                    Game.Levels.TryGetValue(job, out var level);
                    if (level > 0)
                        jobtt.Append(job.Abbreviation).Append(": ").Append(level).AppendLine();
                }
            }

            DalamudApi.SetJobTooltop(jobtt.ToString());

            if (Syncing)
            {
                Syncing = false;
                this.session!.Locations.CompleteLocationChecksAsync([.. localsave!.CompletedChecks]).Start();
            }
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
            if (session == null)
            {
                DalamudApi.Echo("Session is null?");
                return;
            }

            if (hard || MissingLocations == null || MissingLocations.Length == 0)
                MissingLocations = session!.Locations.AllMissingLocations.Select(i => Location.Create(this, i)).ToArray();
            else
            {
                foreach (var l in MissingLocations)
                    l.stale = true;
            }
        }
    }
}
