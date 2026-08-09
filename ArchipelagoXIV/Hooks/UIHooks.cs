using ArchipelagoXIV.Overlays.CustomNodes;
using ArchipelagoXIV.Rando.Locations;
using Dalamud.Game;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;

namespace ArchipelagoXIV.Hooks
{
    internal unsafe partial class UIHooks(ApState apState) : IDisposable
    {

        private Dictionary<uint, APDutyIcon> icons = new();
        private AtkComponentButton*[] grid = new AtkComponentButton*[100];
        //private APDutyIcon[] fishGideIcons = new APDutyIcon[100];
        //private AtkComponentRadioButton*[] fishGuidePageNumberRadioButtons = new AtkComponentRadioButton*[5];
        //private uint fishGuidePageNumber = 0;
        //private static ExcelSheet<FishParameter> FishParameters = DalamudApi.DataManager.GetExcelSheet<FishParameter>();

        public void Enable()
        {
            DalamudApi.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "ContentsFinder", OnContentsFinderRefresh);
            DalamudApi.AddonLifecycle.RegisterListener(AddonEvent.PostClose, "ContentsFinder", OnContentsFinderClose);
            //DalamudApi.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "Bait", OnOpenBaitList);
            //DalamudApi.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "ContentsFinder", OnContentsFinderPostSetup);
            //DalamudApi.AddonLifecycle.RegisterListener(AddonEvent.PostOpen, "FishGuide2", OnOpenFishingGuide);
            //DalamudApi.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "FishGuide2", OnUpdateFishingGuide);
        }

        //private void OnContentsFinderPostSetup(AddonEvent type, AddonArgs args)
        //{
        //    var addon = (AddonContentsFinder*)args.Addon;
        //    foreach (var itemRenderer in addon->DutyList->Items.Span)
        //    {
        //        var componentNode = itemRenderer.Value->Renderer->AtkDragDropInterface.ComponentNode;
        //        if (componentNode is null) continue;
        //        var textNode = (AtkTextNode*)componentNode->Component->GetTextNodeById(5);
        //        var levelNode = (AtkTextNode*)componentNode->Component->GetTextNodeById(15);
        //        var hollowsImageNode = componentNode->Component->GetImageNodeById(8);
        //        DalamudApi.EventManager.AddEvent((nint)addon, (nint)hollowsImageNode, AddonEventType.MouseOver, TooltipHandler);
        //        DalamudApi.EventManager.AddEvent((nint)addon, (nint)hollowsImageNode, AddonEventType.MouseOut, TooltipHandler);
        //    }
        //}

        public void Disable()
        {
            DalamudApi.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "ContentsFinder", OnContentsFinderRefresh);
            DalamudApi.AddonLifecycle.UnregisterListener(AddonEvent.PostClose, "ContentsFinder", OnContentsFinderClose);
            //DalamudApi.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "Bait", OnOpenBaitList);
            //DalamudApi.AddonLifecycle.UnregisterListener(AddonEvent.PostOpen, "FishGuide2", OnOpenFishingGuide);
            //DalamudApi.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "FishGuide2", OnUpdateFishingGuide);
        }

        private void OnContentsFinderRefresh(AddonEvent type, AddonArgs args)
        {
            //var hints = apState.Hints.Select(h => h.LocationId).ToArray();
            var addon = (AddonContentsFinder*)args.Addon.Address;
            if (addon->DutyList == null)
                return;
            foreach (var itemRenderer in addon->DutyList->Items.AsSpan())
            {
                var componentNode = itemRenderer.Value->Renderer->AtkDragDropInterface.ComponentNode;
                if (componentNode is null) continue;

                var textNode = componentNode->Component->GetTextNodeById(6);

                //var levelNode = (AtkTextNode*)componentNode->Component->GetTextNodeById(18);
                var targetNode = componentNode->Component->GetNodeById(14);

                if (textNode is null)
                    continue;

                var found = icons.TryGetValue(componentNode->NodeId, out var icon);
                var name = textNode->NodeText.ExtractText();
                var loc = apState.AllLocations.Where(l => l.IsAccessible()).FirstOrDefault(l => l.Name == name);
                if (loc != null)
                {
                    var visible = loc.Accessible && !loc.Completed;
                    if (!found)
                    {
                        if (!visible)
                            continue;  // Don't create the icon until we'd need to show it

                        //DalamudApi.PluginLog.Debug($"Creating new icon for {componentNode->NodeId} ({textNode->NodeText.ExtractText()})");
                        icon = new APDutyIcon
                        {
                            Position = new Vector2(targetNode->X - 18 - 2, targetNode->Y + ((targetNode->Height - 18) / 2))
                        };
                        icons[componentNode->NodeId] = icon;
                        targetNode->Width -= (ushort)(18 + 2);
                        icon.AttachNode(targetNode, NodePosition.AfterTarget);
                    }
                    //DalamudApi.PluginLog.Debug($"Setting icon visibility for {componentNode->NodeId} ({name}) to {visible}");
                    icon.Node->ToggleVisibility(visible); //why would icon be null here??

                    // todo: Replace the texture, maybe check if it's hinted?
                    //if (hints.Contains(loc.ApId))
                    //    hollowsImageNode->GetAsAtkImageNode()->LoadIconTexture(60004, 0); // Hunt Target tonberry
                    //else
                    //    hollowsImageNode->GetAsAtkImageNode()->LoadIconTexture(60849, 0); //

                }
                else
                {
                    // Not an Archipelago location, hide the icon if it exists
                    if (icons.TryGetValue(componentNode->NodeId, out icon))
                    {
                        icon.Node->ToggleVisibility(false);
                    }
                }
            }
        }

        private void OnContentsFinderClose(AddonEvent type, AddonArgs args)
        {
            foreach (var icon in icons.Values)
            {
                icon.Dispose();
            }
            icons.Clear();
        }

        private void OnOpenBaitList(AddonEvent type, AddonArgs args)
        {
            //AtkUnitBase* addon = (AddonContentsFinder*)args.Addon;
            //addon->GetNodeById(13)->;
        }

        //private void setPageNumber(AddonFishGuide2* addon) { //I think this gets called too often
        //    for (uint i = 0; i < 4; i++)
        //    {
        //        fishGuidePageNumberRadioButtons[i] = addon->GetComponentNodeById(i + 8)->GetAsAtkComponentRadioButton();
        //        if (fishGuidePageNumberRadioButtons[i]->GetImageNodeById(5)->IsVisible() && uint.Parse(fishGuidePageNumberRadioButtons[i]->GetTextNodeById(4)->NodeText.ExtractText()) != fishGuidePageNumber)
        //        {
        //            fishGuidePageNumber = uint.Parse(fishGuidePageNumberRadioButtons[i]->GetTextNodeById(4)->NodeText.ExtractText());
        //            DalamudApi.PluginLog.Debug("Set fish guide page to {0}", fishGuidePageNumber);
        //        }
        //    }
        //}
        //private void setupGridIcons(AddonFishGuide2* addon)
        //{
        //    for (uint i = 0; i < 99; i++)
        //    {
        //        DalamudApi.PluginLog.Debug("Fish Guide: Adding node for button {0} (NodeId:{1})", i, (i + 21));
        //        grid[i] = addon->GetComponentButtonById(i + 21);
        //        var targetNode = grid[i]->GetImageNodeById(8);
        //        var newIcon = new APDutyIcon()
        //        {
        //            Position = new Vector2(targetNode->X, targetNode->Y + targetNode->Width - 18),
        //        };
        //        newIcon.AttachNode(targetNode, NodePosition.AfterTarget);
        //    }
        //}

        //private Fish getFishByGrid(uint page, uint gridNumber)
        //{
        //    if (!(page >= 0 && page <=17 && gridNumber >= 0 && gridNumber <= 99))
        //    {
        //        DalamudApi.PluginLog.Error("Fish lookup out of range");
        //        throw new ArgumentException("Fish lookup out of range");
        //    }
        //    var fishid = Data.FishParameters[page * 100 + gridNumber + 1].Item.RowId;
        //    var fish = apState.MissingLocations.OfType<Fish>().FirstOrDefault(f => f.Data.Id == fishid);
        //    return fish;
        //}

        //private void OnOpenFishingGuide(AddonEvent type, AddonArgs args)
        //{
        //    DalamudApi.PluginLog.Debug("Fish Guide opened");
        //    AddonFishGuide2* addon = (AddonFishGuide2*)args.Addon.Address;
        //    if (addon == null) {
        //        DalamudApi.PluginLog.Error("Addon \'FishGuide2\' is Null");
        //    }

        //    setPageNumber(addon);
        //    setupGridIcons(addon);


        //}
        //private void OnUpdateFishingGuide(AddonEvent type, AddonArgs args)
        //{
        //    var addon = (AddonFishGuide2*)args.Addon.Address;
        //    setPageNumber(addon); // could be more efficient to not do this here?

        //}

        public void Dispose()
        {
            foreach (var icon in icons.Values) icon.Dispose();
            icons.Clear();
        }
    }
}
