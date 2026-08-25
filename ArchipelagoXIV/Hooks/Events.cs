using Archipelago.MultiClient.Net;
using ArchipelagoXIV.Rando;
using ArchipelagoXIV.Rando.Locations;
using Dalamud.Game;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.DutyState;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using System;
using System.Linq;
using EnqueueRewardDelegate = FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentFateReward.Delegates.EnqueueReward;

namespace ArchipelagoXIV.Hooks
{
    internal class Events(ApState apState) : IDisposable
    {
        private bool amnestyTripped;
        private Hook<EnqueueRewardDelegate> EnqueueFateReward = null;

        public unsafe void Enable()
        {
            DalamudApi.DutyState.DutyStarted += DutyState_DutyStarted;
            DalamudApi.DutyState.DutyCompleted += DutyState_DutyCompleted;
            DalamudApi.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
            DalamudApi.GameInventory.ItemAdded += GameInventory_ItemAdded;
            this.EnqueueFateReward = DalamudApi.GameInteropProvider.HookFromAddress<EnqueueRewardDelegate>(AgentFateReward.MemberFunctionPointers.EnqueueReward, this.EnqueueFateRewardDetour);
            this.EnqueueFateReward?.Enable();
            RefreshTerritory();
        }

        private void GameInventory_ItemAdded(Dalamud.Game.Inventory.GameInventoryEvent type, Dalamud.Game.Inventory.InventoryEventArgTypes.InventoryEventArgs data)
        {
            if (Data.Items.TryGetValue(data.Item.BaseItemId, out var value))
            {
                var name = value.Name.ExtractText().TrimEnd();
                if (!DalamudApi.PlayerState.ClassJob.IsValid)
                    return;
                if (DalamudApi.PlayerState.ClassJob.Value.IsFisher() && APData.FishData.ContainsKey(name))
                {
                    if (apState.MissingLocations.FirstOrDefault(l => l is Fish f && f.Data.Id == data.Item.BaseItemId) is not Fish loc)
                        return;

                    APData.Regions.TryGetValue(RegionContainer.LocationToRegion(apState.territoryName, (ushort)apState.territory.RowId), out var region);
                    if (region == null || !loc.Data.Regions.Contains(region))
                    {
                        // This fish was traded, in a retainer, or otherwise obtained in a way that isn't catching it in its native habitat.
                        return;
                    }
                    if (!RegionContainer.CanReach(apState, region))
                    {
                        DalamudApi.ShowError($"{region.Name} is not in logic");
                        return;
                    }
                    if (loc != null && loc.IsAccessible())
                    {
                        // The fish is in logic, and we caught it.
                        // Note:  Because we don't want to punish free trial players for not hoarding scrip bait, we don't actually care if they used the correct bait.
                        // If they caught it suboptimally with OoL bait, it still counts.
                        loc.Complete();
                    }
                    else if (loc is Fish f && f.OutOfLogic())
                    {
                        // We caught a fish that's currently out of logic, but everything we did (Hole, bait, etc) was in logic.
                        // This is usually because the in-logic bait is suboptimal.  We send these.
                        loc.Complete();
                    }
                }
            }
        }

        private unsafe void EnqueueFateRewardDetour(AgentFateReward* thisPtr, AgentFateReward.Reward* reward)
        {
            EnqueueFateReward.Original(thisPtr, reward);
            var success = reward->IsSuccess;
            var fateID = reward->Id;

            Location? location = null;
            switch (reward->Type)
            {
                case AgentFateReward.RewardType.FateReward:
                    location = apState.MissingLocations.OfType<FateLocation>().FirstOrDefault(f => f.FateID == fateID);
                    location ??= apState.MissingLocations.FirstOrDefault(f => f.Name.StartsWith(apState.territoryName + ": FATE #") && !f.Completed);  // FATE #N check
                    break;
                case AgentFateReward.RewardType.DynamicEventReward:
                    location = apState.MissingLocations.OfType<CriticalEncounterLocation>().FirstOrDefault(f => f.CriticalEncounter.RowId == fateID);
                    // For some reason I can't figure out yet, reward->id is always 0 for CEs, so we have to check by name instead.
                    location ??= apState.MissingLocations.FirstOrDefault(f => f.Name.Equals(reward->Name.ExtractText(), StringComparison.OrdinalIgnoreCase));
                    break;
            }
            //var fatename = DalamudApi.DataManager.GetExcelSheet<Fate>(ClientLanguage.English)[fateID].Name.ExtractText();
            //DalamudApi.PluginLog.Debug($"Fate Reward Detour: {fateID} ({fatename}) Success: {success}");
            if (success)
            {
                if (location != null)
                {
                    if (!location.IsAccessible())
                    {
                        DalamudApi.Echo($"{location.Name} currently out of logic.");
                        return;
                    }
                    if (!location.CanClearAsCurrentClass())
                    {
                        DalamudApi.Echo($"Cannot clear {location.Name} as current class");
                        return;
                    }
                    location.Complete();
                }
            }
        }

        public void Disable()
        {
            DalamudApi.DutyState.DutyStarted -= DutyState_DutyStarted;
            DalamudApi.DutyState.DutyCompleted -= DutyState_DutyCompleted;
            DalamudApi.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
            this.EnqueueFateReward?.Disable();
        }

        private unsafe void DutyState_DutyCompleted(IDutyStateEventArgs args)
        {
            var territoryType = args.TerritoryType;
            if (!apState.Connected)
                return;
            var territory = apState.territory = Data.Territories.FirstOrDefault(row => row.RowId == territoryType.RowId);
            var duty = args.ContentFinderCondition.Value;
            Location? location = apState.AllLocations.OfType<DutyLocation>().FirstOrDefault(l => l.Content.RowId == duty.Content.RowId);

            var name = duty.Name.ExtractText();
            if (name == "Ocean Fishing")
            {
                var oceanfishing = EventFramework.Instance()->GetInstanceContentOceanFishing();
                var route = Data.IKDRoutes.FirstOrDefault(r => r.RowId == oceanfishing->CurrentRoute);
                name = "Ocean Fishing: " + route.Name.ExtractText();
            }
            if (name.Contains("(Unreal)"))
            {
                // We will never have unreals in the seed, because they're not permanent content.
                // But if you clear it, I'll absolutely give you credit for the associated Extreme.
                name = name.Replace("(Unreal)", "(Extreme)");
            }
            if (name.StartsWith("the"))
            {
                // It looks nicer
                name = "The" + name[3..];
            }
            if (name.StartsWith("Crystalline Conflict (Custom Match - "))
            {
                name = name[37..^1];
            }

            DalamudApi.Echo($"{name} Completed");
            DalamudApi.PluginLog.Information("Completed Duty {0} (cf={1} tt={2})", name, duty.Content.RowId, territoryType.RowId);
            var canReach = RegionContainer.CanReach(apState, apState.territoryName, territoryType.Value.RowId);

            var atLevel = Logic.Level(duty.ClassJobLevelRequired)(apState, apState.ApplyClassRestrictions);

            var currentLevel = DalamudApi.ObjectTable.LocalPlayer?.Level ?? DalamudApi.PlayerState.Level;
            var isSynced = duty.ClassJobLevelSync == 0 || currentLevel <= duty.ClassJobLevelSync;

            if (canReach && atLevel && (!apState.RequireSyncedDuties || isSynced))
            {
                if (apState.Game is NGPlusGame ngGame && ngGame.GoalDutyName == name)
                    ngGame.OnGoalDutyCompleted();

                if (apState.Game is NGPlusGame state)
                {
                    for (var i = 2; i < state.ExtraDungeonChecks + 2; i++)
                    {
                        DalamudApi.PluginLog.Debug($"looking for {name} {i}");
                        var extraLocation = apState.MissingLocations.FirstOrDefault(l => l.Name.Equals($"{name} {i}", StringComparison.InvariantCultureIgnoreCase));
                        extraLocation?.Complete();
                    }
                }

                location ??= apState.MissingLocations.FirstOrDefault(l => l.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                if (location == null)
                {
                    DalamudApi.Echo("Location not in seed, nothing to do.");
                    return;
                }
                else if (location.Completed)
                {
                    DalamudApi.Echo("Location already completed, nothing to do.");
                    return;
                }

                DalamudApi.PluginLog.Debug("Marking Check {0}", name);
                location.Complete();
                apState.Syncing = true;
            }
            else
            {
                if (apState.RequireSyncedDuties && !isSynced)
                    DalamudApi.Echo("Duty completed unsynced, not submitting check (Require Synced Duties is enabled).");
                else
                    DalamudApi.Echo("You do not meet the requirements, not submitting check");
            }
        }

        /// <summary>
        /// Rerun On-enter events.  Do this when we log in, or otherwise need to recalculate state
        /// </summary>
        public void RefreshTerritory()
        {
            if (DalamudApi.ClientState.IsLoggedIn)
            {
                ClientState_TerritoryChanged(DalamudApi.ClientState.TerritoryType);
            }
        }

        private void ClientState_TerritoryChanged(uint e)
        {
            if (!DalamudApi.DutyState.IsDutyStarted)
            {
                var territory = apState.territory = Data.Territories.First(row => row.RowId == e);
                apState.territoryName = territory.PlaceName.Value.Name.ExtractText();
                apState.RefreshBars = true;

                if (!apState.Connected)
                {
                    // Check if known location
                    //RegionContainer.CanReach(apState, apState.territoryName);
                    return;
                }

                if (apState.territoryName == "The Waking Sands")
                {
                    var PrayReturn = apState.MissingLocations.FirstOrDefault(l => l.Name == "Return to the Waking Sands");
                    PrayReturn?.Complete();
                }
            }
            else
            {
                var duty = DalamudApi.DutyState.ContentFinderCondition.Value;
                var name = duty.Name.ExtractText();
                if (name.StartsWith("the"))
                    name = "The" + name[3..];
                if (name.StartsWith("Crystalline Conflict (Custom Match - "))
                {
                    name = name[37..^1];
                }

                apState.territoryName = name;
                apState.RefreshBars = true;
            }
        }

        private unsafe void DutyState_DutyStarted(IDutyStateEventArgs args)
        {
            var duty = args.ContentFinderCondition.Value;
            var name = duty.Name.ExtractText();
            if (name.StartsWith("the"))
                    name = "The" + name[3..];
            apState.territoryName = name;
            apState.RefreshBars = true;
        }

        public unsafe void CheckAmnesty()
        {
            var cf = ContentsFinder.Instance();
            if (cf->QueueInfo.QueueState == ContentsFinderQueueState.Queued)
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
                        if (cf->QueueInfo.QueuedEntries[i].Id == 0)
                            continue;
                        Send(apState, cf->QueueInfo.QueuedEntries[i].Id);
                    }
                }
            }
            else if (amnestyTripped)
                amnestyTripped = false;

            static void Send(ApState apState, uint queuedId)
            {
                var content = Data.Content.First(c => c.RowId == queuedId);
                var name = content.Name.ExtractText();
                if (name.StartsWith("the"))
                    name = "The" + name[3..];

                var location = apState.AllLocations.FirstOrDefault(l => l.Name == name);


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

                    if (apState.Game is NGPlusGame state)
                    {
                        for (var i = 2; i < state.ExtraDungeonChecks + 2; i++)
                        {
                            DalamudApi.PluginLog.Debug($"looking for {name} {i}");
                            var extraLocation = apState.MissingLocations.FirstOrDefault(l => l.Name == $"{name} {i}");
                            extraLocation?.Complete();
                        }
                    }
                    location.Complete();
                    UIGlobals.PlayChatSoundEffect(6);
                }
                else
                {
                    DalamudApi.Echo($"Couldn't grant Queue Amnesty for {name}, requirements not met.");
                }
            }
        }

        public void Dispose()
        {
            this.EnqueueFateReward?.Dispose();
        }
    }
}
