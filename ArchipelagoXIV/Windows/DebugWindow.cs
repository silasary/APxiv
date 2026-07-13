using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoXIV.Windows
{
    internal class DebugWindow(Plugin plugin, ApState state) : Window("Archipelego Debug", ImGuiWindowFlags.None)
    {
        public override void Draw()
        {
            ImGui.Text($"apState.territoryName: `{state.territoryName}`");
        }
    }
}
