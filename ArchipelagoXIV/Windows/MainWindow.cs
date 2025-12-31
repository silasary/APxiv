using System;
using System.Numerics;
using ArchipelagoXIV.Rando;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

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

            if (ImGui.Button($"Reconnect to {plugin.Configuration.Connection}"))
            {
                state.Connect(plugin.Configuration.Connection, plugin.Configuration.SlotName, plugin.Configuration.Password);
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

            if (ImGui.Button("Join Support Discord"))
            {
                var psi = new System.Diagnostics.ProcessStartInfo("https://discord.gg/TT4cZRHJ6F")
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                System.Diagnostics.Process.Start(psi);
            }

            return;
        }

        ImGui.Spacing();
        if (state.territoryName == null)
            return;
        ImGui.Text($"Current location in logic: {RegionContainer.CanReach(state, state.territoryName, (ushort)state.territory.RowId)}");
        ImGui.Text(state?.Game?.GoalString() ?? "");
        if (state.DeathLinkEnabled)
        {
            ImGui.Text($"Death Link is enabled.");
        }
        ImGui.Text($"Available Checks:");
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
