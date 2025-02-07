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
    private bool force_deathlink;
    private bool ignore_class_restrictions;

    public ConfigWindow(Plugin plugin, ApState apState) : base(
        "AP Config",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(232, 175);
        this.SizeCondition = ImGuiCond.Always;

        this.Configuration = plugin.Configuration;
        ApState = apState;
        this.slotName = Configuration.SlotName;
        this.connection = Configuration.Connection;
        this.force_deathlink = Configuration.ForceDeathlink;
        this.ignore_class_restrictions = Configuration.IgnoreClassRestrictions;
    }

    public ApState ApState { get; }

    public void Dispose() { }

    public override void OnOpen()
    {
        this.slotName = Configuration.SlotName;
        this.connection = Configuration.Connection;
        this.force_deathlink = Configuration.ForceDeathlink;
        this.ignore_class_restrictions = Configuration.IgnoreClassRestrictions;
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
        ImGui.Checkbox("Death Link always enabled", ref force_deathlink);
        ImGui.Checkbox("Ignore Class Restrictions", ref ignore_class_restrictions);
        if (ImGui.Button("Save & Connect"))
        {
            Configuration.Connection = connection;
            Configuration.SlotName = slotName;
            Configuration.ForceDeathlink = force_deathlink;
            Configuration.IgnoreClassRestrictions = ignore_class_restrictions;
            Configuration.Save();
            ApState.Connect(Configuration.Connection, Configuration.SlotName);
        }
    }
}
