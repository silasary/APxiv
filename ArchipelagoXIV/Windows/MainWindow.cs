using System;
using System.Numerics;
using ArchipelagoXIV.Rando;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace ArchipelagoXIV.Windows;

public class MainWindow : Window
{
    private readonly ApState state;
    private readonly Plugin plugin;

    public MainWindow(Plugin plugin, ApState state) : base(
        "Archipelego", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
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
        ImGui.Text($"The AP server is at {plugin.Configuration.Connection} (Connected: {state.Connected})");

        if (ImGui.Button("Show Settings"))
        {
            plugin.DrawConfigUI();
        }
        if (!state.Connected && ImGui.Button("Reconnect to last port"))
        {
            state.Connect(plugin.Configuration.Connection, plugin.Configuration.SlotName);
        }

        ImGui.Spacing();
        if (state.territoryName == null)
            return;
        ImGui.Text($"Current location in logic: {RegionContainer.CanReach(state, state.territoryName, (ushort)state.territory.RowId)}");
        ImGui.Text(state?.Game?.GoalString() ?? "");
        ImGui.Text($"Available Checks:");
        //ImGui.Indent(55);
        if (state.MissingLocations != null)
        {
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
        }

        //ImGui.Unindent(55);
    }
}
