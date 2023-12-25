using ArchipelegoXIV.Rando;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Linq;

namespace ArchipelegoXIV.Hooks
{
    internal class Events(ApState apState)
    {
        public static unsafe AtkResNode* GetChildNodeByID(AtkResNode* node, uint nodeId) => GetChildNodeByID(node->GetComponent(), nodeId);
        public static unsafe AtkResNode* GetChildNodeByID(AtkComponentBase* component, uint nodeId) => GetChildNodeByID(&component->UldManager, nodeId);
        public static unsafe AtkResNode* GetChildNodeByID(AtkUldManager* uldManager, uint nodeId)
        {
            for (var i = 0; i < uldManager->NodeListCount; i++)
            {
                var n = uldManager->NodeList[i];
                if (n->NodeID != nodeId) continue;
                return n;
            }
            return null;
        }

        private ContentFinderCondition last_pop;

        public void Enable()
        {
            DalamudApi.DutyState.DutyCompleted += DutyState_DutyCompleted;
            DalamudApi.ClientState.CfPop += ClientState_CfPop;
            DalamudApi.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
            DalamudApi.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "FateReward", OnFatePostSetup);

            if (DalamudApi.ClientState.IsLoggedIn)
                ClientState_TerritoryChanged(DalamudApi.ClientState.TerritoryType);

        }

        private unsafe void OnFatePostSetup(AddonEvent type, AddonArgs args)
        {
            // TODO: Figure out how to get data out of this
            var fateRewardAddon = (AtkUnitBase*)args.Addon;
            var FateName = fateRewardAddon->GetNodeById(6)->GetAsAtkTextNode()->NodeText.ToString();
            for (uint i = 0; i <= 14; i++)
            {
                try
                {

                    if (fateRewardAddon->GetNodeById(i)->Type == NodeType.Text)
                    {
                        var text = fateRewardAddon->GetNodeById(i)->GetAsAtkTextNode()->NodeText.ToString();
                        DalamudApi.Echo($"{i} -> {text}");
                    }
                    //else if (fateRewardAddon->GetNodeById(i)->Type == NodeType.Res)
                    //{
                    //}
                    else
                    {
                        DalamudApi.Echo($"{i} -> {fateRewardAddon->GetNodeById(i)->Type}");

                    }
                }
                catch (NullReferenceException)
                {
                    DalamudApi.Echo($"{i} -> null");

                }
            }
            

            //DalamudApi.Echo(name);
        }

        private void ClientState_CfPop(ContentFinderCondition obj)
        {
            DalamudApi.Echo(obj.Name);
            this.last_pop = obj;
        }

        public void Disable() {
            DalamudApi.DutyState.DutyCompleted -= DutyState_DutyCompleted;
            DalamudApi.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
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
                DalamudApi.Echo("Marking Check");
                apState.session.Locations.CompleteLocationChecks(location.ApId);

            }
            else
            {
                DalamudApi.Echo("You do not meet the requirements, not submitting check");
            }
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
        }
    }
}
