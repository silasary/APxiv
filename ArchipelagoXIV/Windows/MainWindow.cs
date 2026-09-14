using ArchipelagoXIV.Rando;
using ArchipelagoXIV.Rando.Locations;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ArchipelagoXIV.Windows;

public class MainWindow : SharedWindow
{
    public MainWindow(Plugin plugin, ApState state) : base(plugin, state, "Archipelago", ImGuiWindowFlags.None)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
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
            RecentConnectionsButtons();
            return;
        }

        ImGui.Spacing();
        if (state.territoryName == null)
            return;

        var regionname = RegionContainer.LocationToRegion(state.territoryName, (ushort)state.territory.RowId);
        var canReach = false;
        if (APData.Regions.TryGetValue(regionname, out var currentRegion))
            canReach = currentRegion.Reachable;

        var LogicGreen = new Vector4(0.4f, 1f, 0.4f, 1f);
        ImGui.TextColored(canReach ? LogicGreen : new Vector4(1f, 0.4f, 0.4f, 1f),
            $"Current location in logic: {canReach}");

        ImGui.TextColored(new Vector4(1f, 0.85f, 0.2f, 1f), state.Game?.GoalString() ?? "");

        if (state.DeathLinkEnabled)
        {
            ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), "Death Link is enabled.");
        }

        if (state.MissingLocations == null)
        {
            return;
        }
        if (DalamudApi.DutyState.IsDutyStarted)
        {
            var location = state.AllLocations.OfType<DutyLocation>().FirstOrDefault(l => l.Content.RowId == DalamudApi.DutyState.ContentFinderCondition.RowId);
            if (location is DutyLocation dutyLocation)
            {
                if (dutyLocation.Completed)
                    ImGui.TextColored(ImGuiColors.DalamudGrey, $"Current Duty: {dutyLocation.DisplayText} (Already completed)");
                else if (dutyLocation.IsAccessible())
                    ImGui.TextColored(LogicGreen, $"Current Duty: {dutyLocation.DisplayText}");
                else
                    ImGui.TextColored(ImGuiColors.DalamudRed, $"Current Duty: {dutyLocation.DisplayText} (Not in logic)");
            }
            else
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, $"Current Duty: {state.territoryName} (Not in seed)");
            }
        }

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.6f, 0.9f, 1f, 1f), "Available Checks:");
        ImGui.Separator();
        //ImGui.Indent(55);
        var relevantLocations = new List<Location>();
        var hintedLocations = new List<Location>();
        var otherLocations = new List<Location>();
        var queueNames = GetQueueNames();

        foreach (var location in state.MissingLocations)
        {
            if (location is DutySubLocation subLocation && !(subLocation.parent?.Completed ?? true) && subLocation.HintedItem == null)
                continue;

            if (location.Accessible)
            {
                if (location.region == currentRegion || queueNames.Contains(location.Name, StringComparer.InvariantCultureIgnoreCase))
                    relevantLocations.Add(location);
                else if (location.HintedItem != null)
                    hintedLocations.Add(location);
                else
                    otherLocations.Add(location);
            }
        }
        if (relevantLocations.Count > 0)
        {
            foreach (var location in relevantLocations)
            {
                RenderLocation(location);
            }
            ImGui.Separator();
        }
        if (hintedLocations.Count > 0)
        {
            foreach (var location in hintedLocations)
            {
                RenderLocation(location);
            }
            ImGui.Separator();
        }

        foreach (var location in otherLocations)
        {
            RenderLocation(location);
        }

        static void RenderLocation(Location location)
        {
            var name = location.DisplayText;
            ImGui.Text($"{name}");
            if (location.HintedItem != null)
            {
                var colour = ImGuiColors.DalamudGrey;
                if (location.HintedItem.Status == Archipelago.MultiClient.Net.Enums.HintStatus.Priority)
                    colour = ImGuiColors.DalamudViolet;
                if (location.HintedItem.Status == Archipelago.MultiClient.Net.Enums.HintStatus.Avoid)
                    colour = ImGuiColors.DalamudRed;

                ImGui.TextColored(colour, $" {location.HintText}");
            }
        }

        //ImGui.Unindent(55);
    }

    private unsafe string[] GetQueueNames()
    {
        var cf = ContentsFinder.Instance();
        if (cf->QueueInfo.QueueState == ContentsFinderQueueState.Queued)
        {
            var queueNames = new List<string>();
            for (var i = 0; i < cf->QueueInfo.QueuedEntries.Length; i++)
            {
                if (cf->QueueInfo.QueuedEntries[i].ContentType == FFXIVClientStructs.FFXIV.Client.UI.Agent.ContentsType.Regular)
                {
                    var queuedId = cf->QueueInfo.QueuedEntries[i].Id;
                    var content = Data.Content.First(c => c.RowId == queuedId);
                    queueNames.Add(content.Name.ExtractText());
                }
            }
            return [.. queueNames];
        }
        return [];
    }
}
