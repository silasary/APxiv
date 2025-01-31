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

        private delegate void ProcessPacketActorControlDelegate(uint entityId, uint type, uint statusId, uint amount, uint a5, uint source, uint a7, uint a8, ulong a9, byte flag);


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
    uint entityId, uint type, uint statusId, uint amount, uint a5, uint source, uint a7, uint a8, ulong a9, byte flag)
        {
            processPacketActorControlHook.Original(entityId, type, statusId, amount, a5, source, a7, a8, a9, flag);
            if (entityId != DalamudApi.ObjectTable[0]?.GameObjectId)
                return; // Only process our own deaths
            if (apState.DeathLinkEnabled == false)
                return;

            try
            {
                if (type == 0x6)
                {
                    // Death
                    var cause = $"{DalamudApi.ObjectTable[0].Name} died in {apState.territoryName}";
                    DalamudApi.ShowError("Death sent :)");
                    apState.DeathLink.SendDeathLink(new Archipelago.MultiClient.Net.BounceFeatures.DeathLink.DeathLink(apState.slotName, cause));
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
