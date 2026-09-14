using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ArchipelagoXIV.Windows
{
    public abstract class SharedWindow(Plugin plugin, ApState state, string title, ImGuiWindowFlags flags) : Window(title, flags)
    {
        protected readonly Plugin plugin = plugin;
        protected readonly ApState state = state;

        protected void RecentConnectionsButtons()
        {
            if (plugin.Configuration.ConnectionHistory.Count == 0)
            {
                return;
            }
            ImGui.Separator();
            foreach (var item in plugin.Configuration.ConnectionHistory.ToArray())
            {
                if (ImGui.Button($"Reconnect to {item}"))
                {
                    var parts = item.Split("@");
                    var address = parts[1];
                    var player = parts[0].Split(":")[0];
                    var password = parts[0].Split(":")[1];
                    state.Connect(address, player, password);
                }
            }
        }
    }
}
