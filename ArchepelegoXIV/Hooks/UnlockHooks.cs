using Dalamud.Game;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchepelegoXIV.Hooks
{
    internal class UnlockHooks : IDisposable
    {
        private readonly ApState apState;
        private bool initialized;
        private Hook<IsUnlockLinkUnlockedDelegate> isUnlockLinkUnlockedHook;

        public UnlockHooks(ApState apState)
        {
            this.apState = apState;
        }

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
