using ArchipelagoXIV.Rando;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using System.Text;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json;
using System.IO;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.Sheets;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using static FFXIVClientStructs.FFXIV.Client.Game.UI.MapMarkerData.Delegates;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Dalamud.Game.Text.SeStringHandling;

namespace ArchipelagoXIV
{
    public class ApState
    {
        public class SaveFile
        {
            public HashSet<long> CompletedChecks = [];
        }

        internal BaseGame Game { get; set; }

        public ApState(Configuration config)
        {
            Game = new SpectatorGame(this);
            LoadGame(config.GameName);
            territory = Data.Territories[0];
            this.config = config;
        }

        internal ArchipelagoSession? session = null;

        internal int slot;
        internal string slotName;
        internal ClassJob lastJob;
        internal int lastFateCount;
        private int lastUpFateCount;
        private bool saving;
        internal bool RefreshBars;

        public DeathLinkService DeathLink { get; private set; }
        private readonly Configuration config;

        public TerritoryType territory { get; internal set; }
        public string territoryName { get; internal set; }
        public string territoryRegion { get; internal set; }

        public bool CanTeleport { get; internal set; } = true;
        public bool CanReturn { get; internal set; } = true;

        public bool ApplyClassRestrictions { get => !config.IgnoreClassRestrictions; }

        public string? JobText
        {
            get
            {
                var localPlayer = DalamudApi.ClientState.LocalPlayer;
                if (localPlayer == null)
                    return null;
                var job = localPlayer.ClassJob.Value;
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
        public bool Loading { get; private set; }
        public bool DeathLinkEnabled { get; private set; }

        internal void Disconnect()
        {
            if (Connected && (session?.Socket?.Connected ?? false))
                session?.Socket?.DisconnectAsync()?.Wait();
        }

        internal void Connect(string address, string? player = null, string? password = null)
        {
            if (Connected)
            {
                Disconnect();
            }
            DalamudApi.SetStatusBar("Connecting...");
            var localPlayer = DalamudApi.ClientState.LocalPlayer;
            if (localPlayer == null || !localPlayer.ClassJob.IsValid)
                return;

            this.session = ArchipelagoSessionFactory.CreateSession(address);
            this.session.MessageLog.OnMessageReceived += MessageLog_OnMessageReceived;
            if (string.IsNullOrEmpty(player))
            {
                player = localPlayer.Name.ToString();
            }
            DeathLinkEnabled = false;
            var tags = new string[] { "Dalamud" };
            slotName = player;
            if (this.Game is SpectatorGame)
                tags = ["TextOnly"];
            else
                DalamudApi.Echo($"Connecting as {player} Playing {Game.Name}");

            if (string.IsNullOrWhiteSpace(password))
                password = null;

            var result = this.session.TryConnectAndLogin(Game.Name, player, Archipelago.MultiClient.Net.Enums.ItemsHandlingFlags.AllItems, tags: tags, password: password);
            Connected = result.Successful;
            if (!result.Successful)
            {
                var failure = result as LoginFailure;
                foreach (var e in failure.Errors)
                    DalamudApi.Echo(e);
                if (failure.ErrorCodes.Length != 0 && failure.ErrorCodes.First() == Archipelago.MultiClient.Net.Enums.ConnectionRefusedError.InvalidGame)
                {
                    this.Game = new SpectatorGame(this);
                    Connect(address, player, password);
                    return;

                }
                DalamudApi.SetStatusBar("Connection Failed");
                return;
            }

            this.Loading = true;

            var loginSuccessful = (LoginSuccessful)result;
            slot = loginSuccessful.Slot;
            this.Game.HandleSlotData(loginSuccessful.SlotData);

            if (this.Game is SpectatorGame)
            {
                var game = session.ConnectionInfo.Game;
                LoadGame(game);
                if (this.Game is not SpectatorGame)
                {
                    Connect(address, player, password);
                    return;
                }
                DalamudApi.Echo($"Spectating {game}");
            }
            config.GameName = this.Game.Name;
            config.Save();
            this.DeathLink = session.CreateDeathLinkService();
            if (loginSuccessful.SlotData.TryGetValue("death_link", out var deathlink)) {
                if ((long)deathlink == 1)
                {
                    DeathLinkEnabled = true;
                    DalamudApi.Echo($"Enabling Deathlink");
                    this.DeathLink.EnableDeathLink();
                }
            }
            if (!DeathLinkEnabled && config.ForceDeathlink)
            {
                DeathLinkEnabled = true;
                DalamudApi.Echo($"Enabling Deathlink");
                this.DeathLink.EnableDeathLink();
            }

            session.Items.ItemReceived += Items_ItemReceived;
            session.Locations.CheckedLocationsUpdated += Locations_CheckedLocationsUpdated;
            session.Socket.SocketClosed += Socket_SocketClosed;
            session.DataStorage.TrackHints(HandleHints, true);
            this.DeathLink.OnDeathLinkReceived += Deathlink_DeathLinkReceived;
            LoadCache();

            DalamudApi.SetStatusBar("Connected");

        }

        private void Deathlink_DeathLinkReceived(DeathLink death)
        {
            DalamudApi.PluginLog.Information($"{death}, {death.Source}, {death.Cause}");
            if (string.IsNullOrEmpty(death.Cause))
                DalamudApi.Echo($"{death.Source} has died.");
            else
                DalamudApi.Echo(death.Cause);
        }

        private void LoadGame(string game)
        {
            switch (game)
            {
                case "Manual_FFXIVBMB_Pizzie":
                    this.Game = new BMBGame(this);
                    break;
                case "Manual_FFXIV_Silasary":
                    this.Game = new NGPlusGame(this, true);
                    break;
                case "Final Fantasy XIV":
                    this.Game = new NGPlusGame(this, false);
                    break;
                default:
                    break;
            }
        }

        internal void CompleteGame()
        {
            var statusUpdatePacket = new StatusUpdatePacket();
            statusUpdatePacket.Status = ArchipelagoClientState.ClientGoal;
            this.session.Socket.SendPacket(statusUpdatePacket);
        }

        private void Locations_CheckedLocationsUpdated(System.Collections.ObjectModel.ReadOnlyCollection<long> newCheckedLocations)
        {
            RefreshLocations(false);
            RefreshBars = true;
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
            if (saving)
                return;
            saving = true;
            File.WriteAllText(SaveFileName(), JsonConvert.SerializeObject(this.localsave));
            saving = false;
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
            try
            {
                Game.ProcessItem(item, itemName: name);
            }
            catch (Exception e)
            {
                DalamudApi.PluginLog.Error(e.ToString());
            }
            if (Loading)
                return;

            RefreshRegions();
            this.RefreshLocations(false);
            RefreshBars = true;
        }

        public void UpdateBars()
        {
            var BK = true;
            var fish = false;
            var fisher = this.lastJob.Abbreviation == "FSH";
            var checks = 0;
            var fates = 0;
            var upfates = 0;
            var activeFates = new StringBuilder();
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
                            if (DalamudApi.FateTable.Any(f => f.Name.ToString().Equals(l.Name.Replace(" (FATE)", ""), StringComparison.OrdinalIgnoreCase)))
                            {
                                upfates++;
                                activeFates.AppendLine(l.Name);
                                if (this.lastUpFateCount == 0)
                                {
                                    DalamudApi.ShowToast($"{l.Name} is up");
                                    DalamudApi.Echo($"{l.Name} is up");
                                    UIGlobals.PlayChatSoundEffect(3);
                                }
                            }

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
            if (upfates > 0)
            {
                zoneTT.Insert(0, "Active Fates:\n" + activeFates.ToString() + '\n');
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
                zoneTT.AppendLine().AppendLine($"Unavailable checks in {region?.Name}:");
                zoneTT.Append(unavailable);
            }
            if (checks > 0)
            {
                var text = $"{checks} checks in {region?.Name}";
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
                    if (!Game.Levels.TryGetValue(job, out var level))
                    {
                        level = Game.MaxLevel(job);
                    }
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
            this.lastUpFateCount = upfates;
        }

        private static void RefreshRegions()
        {
            RegionContainer.MarkStale();
        }

        private void MessageLog_OnMessageReceived(Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage message)
        {
            var messagetext = new SeStringBuilder();
            foreach (var part in message.Parts)
            {
                if (!part.Color.Equals(Color.White))
                    messagetext.AddUiForeground(part.Text, part.Color.APColourToUIColour());
                else
                    messagetext.Append(part.Text);
            }

            DalamudApi.PvPTeam(messagetext.Build(), "AP");
            if (message.Parts.Any(p => p.Type == MessagePartType.Player && p.Text == session.Players.GetPlayerAlias(slot)))
                DalamudApi.ShowToast(message.ToString());

            if (Loading)
            {
                Loading = false;
                RefreshRegions();
                this.RefreshLocations(false);
                Game.Ready();
                RefreshBars = true;
            }
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
