using ArchipelagoXIV.Rando;
using ArchipelagoXIV.Rando.Locations;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Linq;

namespace ArchipelagoXIV.Hooks
{
    internal class Events(ApState apState)
    {
        private bool amnestyTripped;

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
                var name = value.Name.ToString();
                if (APData.FishData.ContainsKey(name))
                {
                    //DalamudApi.Echo($"Caught a {name}!");
                    var loc = apState.MissingLocations.FirstOrDefault(l => l.Name == name);
                    if (loc != null && loc.IsAccessible())
                    {
                        loc.Complete();
                    }
                    else if (loc is Fish f && f.OutOfLogic())
                    {
                        loc.Complete();
                    }
                }
            }
        }

        private unsafe void OnFatePreFinalize(AddonEvent type, AddonArgs args)
        {
            var fateRewardAddon = (AtkUnitBase*)args.Addon.Address;
            var fateName = fateRewardAddon->GetNodeById(6)->GetAsAtkTextNode()->NodeText.ToString();
            var success = ((AddonFateReward*)fateRewardAddon)->AtkTextNode248->AtkResNode.IsVisible() || ((AddonFateReward*)fateRewardAddon)->AtkTextNode250->AtkResNode.IsVisible();
            string locName;
            if (apState.territoryName == "The Firmament")
                locName = fateName + " (FETE)";
            else
                locName = fateName + " (FATE)";

            if (!success)
                return;

            var loc = apState.MissingLocations.FirstOrDefault(f => f.Name.Equals(locName, StringComparison.OrdinalIgnoreCase));  // FATEsanity check
            loc ??= apState.MissingLocations.FirstOrDefault(f => f.Name.StartsWith(apState.territoryName + ": FATE #") && !f.Completed);  // FATE #N check
            if (loc == null)
            {
                DalamudApi.PluginLog.Information($"Fate `{locName}` not in world or already completed");
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
            DalamudApi.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "FateReward", OnFatePreFinalize);
        }

        private unsafe void DutyState_DutyCompleted(object? sender, ushort e)
        {
            if (!apState.Connected)
                return;
            var territory = apState.territory = Data.Territories.FirstOrDefault(row => row.RowId == e);
            var duty = Data.GetDuty(e);
            if (!APData.ContentIDToLocationName.TryGetValue(duty.Content.RowId, out var name))
            {
                name = duty.Name.ToString();
                if (name.StartsWith("the"))
                    name = "The" + name[3..];
                name = name.Replace("<italic(1)>", "").Replace("<italic(0)>", "");
            }
            if (name == "Ocean Fishing")
            {
                var oceanfishing = EventFramework.Instance()->GetInstanceContentOceanFishing();
                var route = Data.IKDRoutes.FirstOrDefault(r => r.RowId == oceanfishing->CurrentRoute);
                name = "Ocean Fishing: " + route.Name.ToString();
            }
            DalamudApi.Echo($"{name} Completed");
            DalamudApi.PluginLog.Information("Completed Duty {0} (cf={1} tt={2})", name, duty.Content, e);
            var canReach = RegionContainer.CanReach(apState, apState.territoryName, e);
            var atLevel = Logic.Level(duty.ClassJobLevelRequired)(apState, apState.ApplyClassRestrictions);
            if (canReach && atLevel)
            {
                var location = apState.MissingLocations.FirstOrDefault(l => l.Name == name);
                if (location == null)
                {
                    DalamudApi.Echo("Location already completed or not in seed, nothing to do.");
                    return;
                }
                DalamudApi.PluginLog.Debug("Marking Check {1}", name);
                location.Complete();
                if (apState.Game.GoalType == VictoryType.DefeatShinryu && name == "The Royal Menagerie")
                {
                    apState.CompleteGame();
                }
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
            apState.territoryName = territory.PlaceName.Value.Name.ToString();
            apState.territoryRegion = territory.PlaceNameRegion.Value.Name.ToString();

            if (!apState.Connected)
            {
                // Check if known location
                //RegionContainer.CanReach(apState, apState.territoryName);
                return;
            }

            apState.RefreshBars = true;


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
                if (amnestyTripped)
                    return;
                var diff = DateTime.UtcNow.Subtract(cf->QueueInfo.GetEnteredQueueDateTime());
                if (diff.TotalMinutes > 20)
                {
                    this.amnestyTripped = true;
                    DalamudApi.Echo("Waiting: " + diff.TotalMinutes);

                    for (var i = 0; i < cf->QueueInfo.QueuedEntries.Length; i++)
                    {
                        if (cf->QueueInfo.QueuedEntries[i].ConditionId == 0)
                            continue;
                        Send(apState, cf->QueueInfo.QueuedEntries[i].ConditionId);
                    }
                }
            }
            else if (amnestyTripped)
                amnestyTripped = false;

            static unsafe void Send(ApState apState, uint queuedId)
            {
                var content = Data.Content.First(c => c.RowId == queuedId);
                var name = content.Name.ToString();
                if (name.StartsWith("the"))
                    name = "The" + name[3..];

                var location = apState.MissingLocations.FirstOrDefault(l => l.Name == name);
                

                if (location == null)
                {
                    DalamudApi.PluginLog.Information("Couldn't grant Amnesty for {0}", name);
                    return;
                }

                if (location.CanClearAsAnyClass())
                {
                    var message = $"Granted Queue Amnesty for {name}";
                    DalamudApi.ToastGui.ShowQuest(message, new Dalamud.Game.Gui.Toast.QuestToastOptions { PlaySound = true });
                    DalamudApi.Echo(message);
                    location.Complete();
                    UIGlobals.PlayChatSoundEffect(6);
                }
                else
                {
                    DalamudApi.Echo($"Couldn't grant Queue Amnesty for {name}, requirements not met.");
                }
            }
        }
    }
}
