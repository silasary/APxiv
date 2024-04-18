using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;

namespace ArchipelagoXIV.Hooks
{
    internal class UnlockHooks(ApState apState) : IDisposable
    {
        private bool initialized;
        private Hook<IsUnlockLinkUnlockedDelegate> isUnlockLinkUnlockedHook;

        private unsafe delegate bool IsUnlockLinkUnlockedDelegate(UIState* ui, uint unlockLink);

        public unsafe void Enable()
        {
            //return;
            if (!initialized)
            {
                initialized = true;
                //    if (!DalamudApi.SigScanner.TryScanText("E8 ?? ?? ?? ?? 88 45 80", out nint address))
                //    {
                //        DalamudApi.Echo("Error: Could not hook");
                //        return;
                //    }

                //    isUnlockLinkUnlockedHook = Hook<IsUnlockLinkUnlockedDelegate>.FromAddress(address, IsUnlockLinkUnlocked);
                //    isUnlockLinkUnlockedHook.Enable();
                //apState.Hooked = true;

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
            apState.Hooked = false;
            //isUnlockLinkUnlockedHook.Disable();
            //isUnlockLinkUnlockedHook.Dispose();
        }
    }
}
