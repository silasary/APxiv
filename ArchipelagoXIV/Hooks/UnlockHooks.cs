using ArchipelagoXIV.Rando;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using ExecuteEmoteDelegate = FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentEmote.Delegates.ExecuteEmote;
using InteractWithObjectDelegate = FFXIVClientStructs.FFXIV.Client.Game.Control.TargetSystem.Delegates.InteractWithObject;
using IsUnlockLinkUnlockedDelegate = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Delegates.IsUnlockLinkUnlocked;

namespace ArchipelagoXIV.Hooks
{
    internal class UnlockHooks : IDisposable
    {
        public unsafe UnlockHooks(ApState apState)
        {
            this.apState = apState;
            //this.execute_emote = DalamudApi.GameInteropProvider.HookFromAddress<ExecuteEmoteDelegate>(AgentEmote.MemberFunctionPointers.ExecuteEmote, this.ExecuteEmoteDetour);
            //this.isUnlockLinkUnlockedHook = DalamudApi.GameInteropProvider.HookFromAddress<IsUnlockLinkUnlockedDelegate>(UIState.MemberFunctionPointers.IsUnlockLinkUnlocked, this.IsUnlockLinkUnlocked);
            this.interactWithObject = DalamudApi.GameInteropProvider.HookFromAddress<InteractWithObjectDelegate>(TargetSystem.MemberFunctionPointers.InteractWithObject, this.InteractWithObjectDetour);

        }

        private unsafe ulong InteractWithObjectDetour(TargetSystem* thisPtr, GameObject* obj, bool checkLineOfSight)
        {
            if (apState.Connected && obj->GetObjectKind() == ObjectKind.Aetheryte && apState.CurrentLocationInLogic)
            {
                var area = TerritoryInfo.Instance()->SubAreaPlaceNameId;
                var areaname = DalamudApi.DataManager.GetExcelSheet<PlaceName>().GetRow(area).Name.ExtractText();
                if (string.IsNullOrEmpty(areaname))
                    areaname = apState.territoryName;
                DalamudApi.PluginLog.Debug($"InteractWithObjectDetour called with obj: {obj->NameString}, baseid: {obj->BaseId}, checkLineOfSight: {checkLineOfSight}, area: {areaname}");

                if (apState.Game.AttunedAetherytes.Add(areaname))
                {
                    var name = $"Attune {areaname}";
                    if (Data.DutyAliases.TryGetValue(name, out var alias))
                    {
                        name = alias;
                    }
                    var loc = apState.MissingLocations.FirstOrDefault(l => l.Name == name);
                    if (loc != null)
                    {
                        loc.Complete();
                    }
                    else
                    {
                        DalamudApi.PluginLog.Info($"Could not find location for {name} in missing locations.");
                    }
                }
            }
            return interactWithObject!.Original(thisPtr, obj, checkLineOfSight);
        }

        private unsafe void ExecuteEmoteDetour(AgentEmote* thisPtr, ushort emoteId, EmoteController.PlayEmoteOption* playEmoteOption, bool addToHistory, bool liveUpdateHistory)
        {
            //DalamudApi.PluginLog.Debug($"ExecuteEmoteDetour called with emoteId: {emoteId}");
            execute_emote!.Original(thisPtr, emoteId, playEmoteOption, addToHistory, liveUpdateHistory);
        }

        private bool initialized;
        private readonly ApState apState;

        private readonly Hook<IsUnlockLinkUnlockedDelegate>? isUnlockLinkUnlockedHook = null;
        private readonly Hook<ExecuteEmoteDelegate>? execute_emote = null;
        private readonly Hook<InteractWithObjectDelegate>? interactWithObject = null;

        public unsafe void Enable()
        {
            //return;
            if (!initialized)
            {
                execute_emote?.Enable();
                isUnlockLinkUnlockedHook?.Enable();
                interactWithObject?.Enable();

                initialized = true;
            }
        }

        private unsafe bool IsUnlockLinkUnlocked(UIState* ui, uint unlockLink)
        {
            var x = this.isUnlockLinkUnlockedHook.Original(ui, unlockLink);
            if (unlockLink == 4 && !apState.CanTeleport)
            {
                // Teleport
                return false;
            }
            else if (unlockLink == 1 && !apState.CanReturn)
            {
                // Return
                return false;
            }
            //else if (unlockLink == 14)
            //{
            //    // Mounts
            //    return false;
            //}
            return x;
        }

        public void Dispose()
        {
            initialized = false;
            execute_emote?.Dispose();
            interactWithObject?.Dispose();
            //isUnlockLinkUnlockedHook.Disable();
            //isUnlockLinkUnlockedHook.Dispose();
        }
    }
}
