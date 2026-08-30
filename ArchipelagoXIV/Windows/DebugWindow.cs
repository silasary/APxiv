using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchipelagoXIV.Rando;
using ArchipelagoXIV.Rando.Locations;
using Dalamud.Interface.Colors;

namespace ArchipelagoXIV.Windows
{
    internal class DebugWindow(Plugin plugin, ApState state) : Window("Archipelego Debug", ImGuiWindowFlags.None)
    {
        private bool ShowCompletedLocations = true;

        public override void Draw()
        {
            ImGui.Text($"BG Task State: {plugin.BackgroundTask.Status} (Should be WaitingForActivation)");
            if (plugin.BackgroundTask.Status == TaskStatus.Faulted)
            {
                ImGui.Text($"BG Task Exception: {plugin.BackgroundTask.Exception}");
            }
            if (plugin.BackgroundTask.IsCompleted)
            {
                if (ImGui.Button("Restart Background Task"))
                {
                    plugin.StartBGTask();
                }
            } 
            ImGui.Text($"apState.territoryName: `{state.territoryName}`");
            ImGui.Text($"apState.territory.RowId: `{state.territory.RowId}`");

            ImGui.Separator();
            ImGui.Checkbox("Show Completed Locations", ref this.ShowCompletedLocations);
            var regions = APData.Regions.OrderBy(r => r.Value.Distance ?? 999).ToList();
            var checksPerRegion = state.AllLocations.Where(l => l.region != null).GroupBy(l => l.region).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var region in regions)
            {
                var distance = region.Value.Distance ?? 99;
                ImGui.Text($"{distance.ToString("D2")}: {region.Key} (From: {region.Value.From?.Name ?? "null"})");
                if (checksPerRegion.TryGetValue(region.Value, out var checks))
                {
                    ImGui.Indent();
                    foreach (var check in checks)
                    {
                        PrintLocationInfo(check);
                    }
                    ImGui.Unindent();
                }
            }
            var nullRegionChecks = state.AllLocations.Where(l => l.region == null && !l.Completed).ToList();
            if (nullRegionChecks.Count != 0)
            {
                ImGui.Text($"??: (null Region)");
                ImGui.Indent();
                foreach (var check in nullRegionChecks)
                {
                    PrintLocationInfo(check);
                }
                ImGui.Unindent();
            }

            void PrintLocationInfo(Location check)
            {
                if (check.Completed && !this.ShowCompletedLocations)
                {
                    return;
                }
                var accessible = check.Accessible ? "Accessible" : "Inaccessible";
                var completed = check.Completed ? "Completed" : "Incomplete";
                var colour = check.Completed ? ImGuiColors.DalamudGrey : check.Accessible ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed;
                ImGui.TextColored(colour, $"- {check.Name} ({accessible}, {completed})");
            }
        }
    }
}
