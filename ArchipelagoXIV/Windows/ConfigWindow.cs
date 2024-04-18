using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace ArchipelagoXIV.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration Configuration;
    private string slotName;
    private string connection;

    public ConfigWindow(Plugin plugin, ApState apState) : base(
        "AP Config",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(232, 125);
        this.SizeCondition = ImGuiCond.Always;

        this.Configuration = plugin.Configuration;
        ApState = apState;
        this.slotName = Configuration.SlotName;
        this.connection = Configuration.Connection;
    }

    public ApState ApState { get; }

    public void Dispose() { }

    public override void OnOpen()
    {
        this.slotName = Configuration.SlotName;
        this.connection = Configuration.Connection;
    }

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
        ImGui.InputTextWithHint("Slot Name", DalamudApi.ClientState.LocalPlayer?.Name.ToString() ?? "", ref slotName, 63);
        ImGui.InputTextWithHint("Address", "archipelago.gg:38281", ref connection, 64);
        if (ImGui.Button("Save & Connect"))
        {
            Configuration.Connection = connection;
            Configuration.SlotName = slotName;
            Configuration.Save();
            ApState.Connect(Configuration.Connection, Configuration.SlotName);
        }
    }
}
