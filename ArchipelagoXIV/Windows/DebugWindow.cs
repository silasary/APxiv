using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchipelagoXIV.Rando;

namespace ArchipelagoXIV.Windows
{
    internal class DebugWindow(Plugin plugin, ApState state) : Window("Archipelego Debug", ImGuiWindowFlags.None)
    {
        public override void Draw()
        {
            ImGui.Text($"apState.territoryName: `{state.territoryName}`");

            ImGui.Separator();
            var regions = APData.Regions.OrderBy(r => r.Value.Distance ?? 999).ToList();
            foreach (var region in regions)
            {
                var distance = region.Value.Distance ?? 99;
                ImGui.Text($"{distance.ToString("D2")}: {region.Key}");
            }
        }
    }
}
