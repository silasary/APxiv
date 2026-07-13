using System;
using System.Numerics;
using ArchipelagoXIV.Rando;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System.Linq;

namespace ArchipelagoXIV.Windows;

public class MainWindow : Window
{
    private readonly ApState state;
    private readonly Plugin plugin;

    public MainWindow(Plugin plugin, ApState state) : base(
        "Archipelago", ImGuiWindowFlags.None)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.state = state;
        this.plugin = plugin;
    }

    public override void Draw()
    {
        //ImGui.Text($"The AP server is at {plugin.Configuration.Connection} (Connected: {state.Connected})");
        if (ImGui.Button("Show Settings"))
        {
            plugin.DrawConfigUI();
        }
        if (!state.Connected)
        {

            if (!plugin.Configuration.ConnectionHistory.Any() && ImGui.Button($"Reconnect to {plugin.Configuration.Connection}"))
            {
                state.Connect(plugin.Configuration.Connection, plugin.Configuration.SlotName, plugin.Configuration.Password);
                plugin.Configuration.AddToConnectionHistory();
            }

            if (ImGui.Button("View setup guide"))
            {
                var psi = new System.Diagnostics.ProcessStartInfo("https://github.com/silasary/APxiv/wiki/Getting-Started")
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                System.Diagnostics.Process.Start(psi);
            }

            if (ImGui.Button("Join Unofficial Archipelago Discord"))
            {
                var psi = new System.Diagnostics.ProcessStartInfo("https://discord.gg/TT4cZRHJ6F")
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                System.Diagnostics.Process.Start(psi);
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
                    plugin.Configuration.AddToConnectionHistory(player, password, address);
                }
            }

            return;
        }

        ImGui.Spacing();
        if (state.territoryName == null)
            return;

        var canReach = RegionContainer.CanReach(state, state.territoryName, (ushort)state.territory.RowId);

        ImGui.TextColored(canReach ? new Vector4(0.4f, 1f, 0.4f, 1f) : new Vector4(1f, 0.4f, 0.4f, 1f),
            $"Current location in logic: {canReach}");

        ImGui.TextColored(new Vector4(1f, 0.85f, 0.2f, 1f), state?.Game?.GoalString() ?? "");

        if (state.DeathLinkEnabled)
        {
            ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), "Death Link is enabled.");
        }

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.6f, 0.9f, 1f, 1f), "Available Checks:");
        ImGui.Separator();

        if (state?.MissingLocations == null)
        {
            return;
        }
        //ImGui.Indent(55);
        foreach (var location in state.MissingLocations)
        {
            if (location.IsAccessible())
            {
                var name = location.DisplayText;
                if (location.Name.EndsWith(" (FATE)"))
                {
                    name = name.Replace("(FATE)", $"({RegionContainer.LocationToRegion(name)} FATE)");
                }
                ImGui.Text($"{name}");
            }
        }

        //ImGui.Unindent(55);
    }
}
