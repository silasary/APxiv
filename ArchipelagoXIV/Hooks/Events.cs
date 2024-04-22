using ArchipelagoXIV.Rando;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Linq;

namespace ArchipelagoXIV.Hooks
{
    internal class Events(ApState apState)
    {
        private bool AmnestyTripped;

        public void Enable()
        {
            DalamudApi.DutyState.DutyCompleted += DutyState_DutyCompleted;
            DalamudApi.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
            DalamudApi.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "FateReward", OnFatePreFinalize);
            DalamudApi.GameInventory.ItemAdded += GameInventory_ItemAdded;
            RefreshTerritory();
        }

        private void GameInventory_ItemAdded(Dalamud.Game.Inventory.GameInventoryEvent type, Dalamud.Game.Inventory.InventoryEventArgTypes.InventoryEventArgs data)
        {
            if (Data.Items.TryGetValue(data.Item.ItemId, out var value))
            {
                var name = value.Name;
                if (APData.FishData.ContainsKey(name))
                {
                    //DalamudApi.Echo($"Caught a {name}!");
                    var loc = apState.MissingLocations.FirstOrDefault(l => l.Name == name);
                    if (loc != null && loc.IsAccessible())
                    {
                        loc.Complete();
                    }
                }
            }
        }

        private unsafe void OnFatePreFinalize(AddonEvent type, AddonArgs args)
        {
            var fateRewardAddon = (AtkUnitBase*)args.Addon;
            var fateName = fateRewardAddon->GetNodeById(6)->GetAsAtkTextNode()->NodeText.ToString();
            var success = ((AddonFateReward*)fateRewardAddon)->AtkTextNode248->AtkResNode.IsVisible || ((AddonFateReward*)fateRewardAddon)->AtkTextNode250->AtkResNode.IsVisible;
            var locName = fateName + " (FATE)";
            if (!success)
                return;

            var loc = apState.MissingLocations.FirstOrDefault(f=> f.Name == locName);  // FATEsanity check
            loc ??= apState.MissingLocations.FirstOrDefault(f => f.Name.StartsWith(apState.territoryName + ": FATE #") && !f.Completed);  // FATE #N check
            if (loc == null)
            {
                PluginLog.Information($"Fate {locName} not available or already completed");
                return;
            }
            if (!loc.IsAccessible())
            {
                DalamudApi.Echo("FATE currently out of logic.");
                return;
            }
            if (!loc.CanClearAsCurrentClass())
            {
                DalamudApi.Echo("Cannot clear FATE as current class");
                return;
            }

            loc.Complete();

        }

        public void Disable() {
            DalamudApi.DutyState.DutyCompleted -= DutyState_DutyCompleted;
            DalamudApi.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
            DalamudApi.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "FateReward", OnFatePreFinalize);
        }

        private void DutyState_DutyCompleted(object? sender, ushort e)
        {
            if (!apState.Connected)
                return;
            var territory = apState.territory = Data.Territories.FirstOrDefault(row => row.RowId == e);
            var duty = Data.GetDuty(e);
            if (!APData.ContentIDToLocationName.TryGetValue(duty.Content, out var name))
            {
                name = duty.Name;
                if (name.StartsWith("the"))
                    name = "The" + name[3..];
            }
            DalamudApi.Echo($"{name} Completed");
            PluginLog.Information("Completed Duty {0} (cf={1} tt={2})", name, duty.Content, e);
            var canReach = RegionContainer.CanReach(apState, apState.territoryName, e);
            if (canReach && Logic.Level(duty.ClassJobLevelRequired)(apState, true))
            {
                var location = apState.MissingLocations.FirstOrDefault(l => l.Name == name);
                if (location == null)
                {
                    DalamudApi.Echo("Location already completed or not in seed, nothing to do.");
                    return;
                }
                PluginLog.Debug("Marking Check {1}", name);
                location.Complete();

            }
            else
            {
                DalamudApi.Echo("You do not meet the requirements, not submitting check");
            }
        }

        /// <summary>
        /// Rerun On-enter events.  Do this when we log in, or otherwise need to recalculate state
        /// </summary>
        public void RefreshTerritory()
        {
            if (DalamudApi.ClientState.IsLoggedIn)
                ClientState_TerritoryChanged(DalamudApi.ClientState.TerritoryType);
        }

        private void ClientState_TerritoryChanged(ushort e)
        {
            var territory = apState.territory = Data.Territories.First(row => row.RowId == e);
            apState.territoryName = territory.PlaceName?.Value?.Name ?? "Unknown";
            apState.territoryRegion = territory.PlaceNameRegion?.Value?.Name ?? "Unknown";

            if (!apState.Connected)
            {
                // Check if known location
                RegionContainer.CanReach(apState, apState.territoryName);
                return;
            }

            apState.UpdateBars();


            if (apState.territoryName == "The Waking Sands")
            {
                var PrayReturn = apState.MissingLocations.FirstOrDefault(l => l.Name == "Return to the Waking Sands");
                PrayReturn?.Complete();
            }
        }

        public unsafe void CheckAmnesty()
        {
            var cf = ContentsFinder.Instance();
            if (cf->QueueInfo.QueueState == ContentsFinderQueueInfo.QueueStates.Queued)
            {
                if (AmnestyTripped)
                    return;
                var diff = DateTime.UtcNow.Subtract(cf->QueueInfo.GetEnteredQueueDateTime());
                if (diff.TotalMinutes > 20)
                {
                    this.AmnestyTripped = true;
                    DalamudApi.Echo("Waiting: " + diff.TotalMinutes);

                    Send(apState, cf->QueueInfo.QueuedContentFinderConditionId1);
                    if (cf->QueueInfo.QueuedContentFinderConditionId2 > 0)
                        Send(apState, cf->QueueInfo.QueuedContentFinderConditionId2);
                    if (cf->QueueInfo.QueuedContentFinderConditionId3 > 0)
                        Send(apState, cf->QueueInfo.QueuedContentFinderConditionId3);
                    if (cf->QueueInfo.QueuedContentFinderConditionId4 > 0)
                        Send(apState, cf->QueueInfo.QueuedContentFinderConditionId4);
                    if (cf->QueueInfo.QueuedContentFinderConditionId5 > 0)
                        Send(apState, cf->QueueInfo.QueuedContentFinderConditionId5);
                }
            }
            else if (AmnestyTripped)
                AmnestyTripped = false;

            static unsafe void Send(ApState apState, byte queuedId)
            {
                var content = Data.Content.First(c => c.RowId == queuedId);
                var location = apState.MissingLocations.FirstOrDefault(l => l.Name == content.Name);

                if (location == null)
                    return;

                if (location.CanClearAsAnyClass())
                {
                    var message = $"Granted Queue Amnesty for {content.Name}";
                    DalamudApi.ToastGui.ShowQuest(message, new Dalamud.Game.Gui.Toast.QuestToastOptions { PlaySound = true });
                    DalamudApi.Echo(message);
                    location.Complete();
                    UIModule.PlayChatSoundEffect(6);
                }
                else
                {
                    DalamudApi.Echo($"Couldn't grant Queue Amnesty for {content.Name}, requirements not met.");
                }
            }
        }
    }
}
