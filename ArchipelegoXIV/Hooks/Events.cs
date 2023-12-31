using ArchipelegoXIV.Rando;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Linq;

namespace ArchipelegoXIV.Hooks
{
    internal class Events(ApState apState)
    {
        public void Enable()
        {
            DalamudApi.DutyState.DutyCompleted += DutyState_DutyCompleted;
            DalamudApi.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
            DalamudApi.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "FateReward", OnFatePreFinalize);
            
            RefreshTerritory();
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
            loc ??= apState.MissingLocations.FirstOrDefault(f => f.Name.StartsWith(apState.territoryName + ": FATE #"));  // FATE #N check
            if (loc == null)
            {
                PluginLog.Information($"Fate {locName} not available or already completed");
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
            var territory = apState.territory = Data.Territories.FirstOrDefault(row => row.RowId == e);
            var duty = Data.GetDuty(e);
            string name = duty.Name;
            if (name.StartsWith("the"))
                name = "The" + name[3..];
            DalamudApi.Echo($"{name} Completed");
            if (!apState.Connected)
                return;
            var canReach = RegionContainer.CanReach(apState, apState.territoryName, e);
            if (canReach && Logic.Level(duty.ClassJobLevelRequired)(apState))
            {
                var location = apState.MissingLocations.FirstOrDefault(l => l.Name == name);
                if (location == null)
                {
                    DalamudApi.Echo("Location already completed, nothing to do.");
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

            var canReach = RegionContainer.CanReach(apState, apState.territoryName, e);
            if (canReach)
                DalamudApi.SetStatusBar("In Logic");
            else
                DalamudApi.SetStatusBar("Out of Logic");

            if (apState.territoryName == "The Waking Sands")
            {
                var PrayReturn = apState.MissingLocations.FirstOrDefault(l => l.Name == "Return to the Waking Sands");
                PrayReturn?.Complete();
            }
        }
    }
}
