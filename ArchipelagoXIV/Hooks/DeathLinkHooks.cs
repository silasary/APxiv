using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoXIV.Hooks
{
    /// <summary>
    /// When a player dies, send a death to other games.
    /// </summary>
    internal class DeathLinkHooks(ApState apState) : IDisposable
    {
        // Shamelessly stolen from https://github.com/Kouzukii/ffxiv-deathrecap/blob/master/Events/CombatEventCapture.cs
        [Signature("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", DetourName = nameof(ProcessPacketActorControlDetour))]
        private readonly Hook<ProcessPacketActorControlDelegate> processPacketActorControlHook = null!;

        private delegate void ProcessPacketActorControlDelegate(
            uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, uint param7, uint param8, ulong targetId,
            byte param9);


        public void Enable()
        {
            DalamudApi.GameInteropProvider.InitializeFromAttributes(this);
            processPacketActorControlHook.Enable();
        }

        public void Disable()
        {
            processPacketActorControlHook.Disable();
        }

        private void ProcessPacketActorControlDetour(
            uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, uint param7, uint param8, ulong targetId,
            byte param9)
        {
            processPacketActorControlHook.Original(category, eventId, param1, param2, param3, param4, param5, param6, param7, param8, targetId, param9);
            if (targetId != DalamudApi.ObjectTable.LocalPlayer?.GameObjectId)
                return; // Only process our own deaths
            if (apState.DeathLinkEnabled == false)
                return;

            try
            {
                if (category == 0x6)
                {
                    // Death
                    var cause = $"{DalamudApi.PlayerState.CharacterName} died in {apState.territoryName}";
                    DalamudApi.ShowError("Death sent :)");
                    apState.DeathLink?.SendDeathLink(new Archipelago.MultiClient.Net.BounceFeatures.DeathLink.DeathLink(apState.slotName, cause));
                }
            }
            catch (Exception e)
            {
                DalamudApi.PluginLog.Error(e, "Caught unexpected exception");
            }
        }

        public void Dispose()
        {
            Disable();
            processPacketActorControlHook.Dispose();
        }
    }
}
