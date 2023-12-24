using System;
using System.Numerics;
using ArchipelegoXIV;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace SamplePlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    public ConfigWindow(Plugin plugin, ApState apState) : base(
        "AP Config",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(232, 75);
        this.SizeCondition = ImGuiCond.Always;

        this.Configuration = plugin.Configuration;
        ApState = apState;
    }

    public ApState ApState { get; }

    public void Dispose() { }

    public override void Draw()
    {
        // can't ref a property, so use a local copy
        //var configValue = this.ApState.CanTeleport;
        //if (ImGui.Checkbox("Allow Teleport", ref configValue))
        //{
        //    this.ApState.CanTeleport = configValue;
        //    // can save immediately on change, if you don't want to provide a "Save and Close" button
        //    this.Configuration.Save();
        //}
        ImGui.InputTextWithHint("Slot Name", DalamudApi.ClientState.LocalPlayer?.Name.ToString() ?? "", ref Configuration.SlotName, 63);
        ImGui.InputTextWithHint("Address", "archipelago.gg:", ref Configuration.Connection, 64);
        if (ImGui.Button("Save & Connect"))
        {
            Configuration.Save();
            ApState.Connect(Configuration.Connection, Configuration.SlotName);
        }
    }
}
