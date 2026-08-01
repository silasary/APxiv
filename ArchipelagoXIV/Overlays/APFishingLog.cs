using ArchipelagoXIV.Overlays.CustomNodes;
using ArchipelagoXIV.Rando;
using ArchipelagoXIV.Rando.Locations;
using Dalamud.Bindings.ImPlot;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.AccessControl;
using System.Text;

namespace ArchipelagoXIV.Overlays;

internal unsafe class APFishingLog : IDisposable
{
    private NativeListController<AddonFishingNote, ListItemData> regionListController;
    private NativeListController<AddonFishingNote, ListItemData> areaListController;

    private ApState apState;
    private APDutyIcons? apDutyIcons;
    private readonly Dictionary<uint, APDutyIcons> regionImageNodes = [];
    private readonly Dictionary<uint, APDutyIcons> areaImageNodes = [];

    public APFishingLog(ApState apState) {
        this.apState = apState;
        regionListController = new NativeListController<AddonFishingNote, ListItemData>
        {
            AddonName = "FishingNote",
            GetPopulatorNode = RegionGetPopulatorMethod,
            ShouldModifyElement = RegionShouldModifyElementMethod,
            UpdateElement = RegionUpdateElementMethod,
        };
        regionListController.Enable();
        areaListController = new NativeListController<AddonFishingNote, ListItemData>
        {
            AddonName = "FishingNote",
            GetPopulatorNode = AreaGetPopulatorMethod,
            ShouldModifyElement = AreaShouldModifyElementMethod,
            UpdateElement = AreaUpdateElementMethod,
        };
        areaListController.Enable();
        DalamudApi.PluginLog.Info("Fishing Log overlay enabled");
    }

    public void Dispose()
    {
        regionListController?.Dispose();
        areaListController?.Dispose();
    }

    private static AtkComponentListItemRenderer* RegionGetPopulatorMethod(AddonFishingNote* addonFishingNote)
    => (AtkComponentListItemRenderer*)addonFishingNote->GetNodeById(10)->GetAsAtkComponentList()->GetComponentById(2);

    //I really hope this works

    private bool RegionShouldModifyElementMethod(AddonFishingNote* unitBase, ListItemData listItem)
    {
        if (!apState.Connected)
        {
            return false;
        }
        return true;
    }

    private void RegionUpdateElementMethod(AddonFishingNote* unitBase, ListItemData listItem)
    {
        var textNode = listItem.GetNode<AtkTextNode>(0);
        var fishingLocations = apState.AvailableFishingHoles;

        APDutyIcons? node = null;
        if (!regionImageNodes.TryGetValue(listItem.NodeId, value: out node))
        {
            node = new APDutyIcons
            {
                Size = new Vector2(24.0f, 24.0f),
                Position = new Vector2(textNode->X - 12, 0.0f),
            };
            node.AttachNode(textNode, NodePosition.BeforeTarget);
            textNode->SetXFloat(textNode->X + 12);
            textNode->SetWidth((ushort)(textNode->Width - 12));

            regionImageNodes.Add(listItem.NodeId, node);
        }
        if (apState.AvailableFishingRegions.Contains(textNode->NodeText.ExtractText()))
        {
            node.IsVisible = true;
        }
        else
        {
            node.IsVisible = false;
        }
    }

    private static AtkComponentListItemRenderer* AreaGetPopulatorMethod(AddonFishingNote* addonFishingNote)
    {
        return (AtkComponentListItemRenderer*)addonFishingNote->GetNodeById(13)->GetAsAtkComponentTreeList()->GetComponentById(4);
    }

    private bool AreaShouldModifyElementMethod(AddonFishingNote* unitBase, ListItemData listItem)
    {
        if (!apState.Connected)
        {
            return false;
        }
        if (listItem.GetNode<AtkResNode>(3)->GetNodeType() == NodeType.Image)
        {
            return true;
        }
        return false;
    }

    private void AreaUpdateElementMethod(AddonFishingNote* unitBase, ListItemData listItem)
    {
        //listItem should always have an image as node 3
        var textNode = listItem.GetNode<AtkTextNode>(3);
        var fishList = apState.MissingLocations.OfType<Fish>().Where(f => f.IsAccessible());
        if (textNode != null && fishList != null)
        {
            
        }
        else
        {
            return;
        }

        APDutyIcons? node = null;
        if (!areaImageNodes.TryGetValue(listItem.NodeId, value: out node))
        {
            node = new APDutyIcons
            {
                Size = new Vector2(24.0f, 24.0f),
                Position = new Vector2(textNode->X - 24, 0.0f),
            };
            node.AttachNode(textNode, NodePosition.BeforeTarget);
            textNode->SetXFloat(textNode->X + 24);
            textNode->SetWidth((ushort)(textNode->Width - 24));

            areaImageNodes.Add(listItem.NodeId, node);
        }
        if (apState.AvailableFishingHoles.Contains(textNode->NodeText.ExtractText()))
        {
            node.IsVisible = true;
        }
        else
        {
            node.IsVisible = false;
        }
    }


}
