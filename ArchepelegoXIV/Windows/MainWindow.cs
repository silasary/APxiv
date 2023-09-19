using System;
using System.Numerics;
using ArchepelegoXIV;
using Dalamud.Game.DutyState;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;

namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private ApState state;
    private Plugin Plugin;

    public MainWindow(Plugin plugin, ApState state) : base(
        "My Amazing Window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.state = state;
        this.Plugin = plugin;
    }

    public void Dispose()
    {
        
    }

    public override void Draw()
    {
        ImGui.Text($"The random config bool is {this.Plugin.Configuration.AllowTeleport}");

        if (ImGui.Button("Show Settings"))
        {
            this.Plugin.DrawConfigUI();
        }

        ImGui.Spacing();

        ImGui.Text($"DutyState: {state.DebugText}");
        ImGui.Indent(55);
        //ImGui.Image(this.GoatImage.ImGuiHandle, new Vector2(this.GoatImage.Width, this.GoatImage.Height));

        ImGui.Unindent(55);
    }
}
