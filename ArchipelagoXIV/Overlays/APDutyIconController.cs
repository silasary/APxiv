using ArchipelagoXIV;
using ArchipelagoXIV.Overlays.CustomNodes;
using ArchipelagoXIV.Rando.Locations;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
using KamiToolKit.Extensions;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using Lumina.Excel.Sheets.Experimental;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

namespace ArchipelagoXIV.Overlays;

internal unsafe class APDutyIconController : IDisposable
{

    private NativeListController<AddonContentsFinder, ListItemData> listController;

    private Dictionary<Location, object> locationNodes;
    private ApState apState;
    private APDutyIcons? apDutyIcons;
    private readonly Dictionary<uint, APDutyIcons> imageNodes = [];

    public APDutyIconController(ApState apState)
    {
        this.apState = apState;
        listController = new NativeListController<AddonContentsFinder, ListItemData>
        {
            AddonName = "ContentsFinder",
            GetPopulatorNode = GetPopulatorMethod,
            ShouldModifyElement = ShouldModifyElementMethod,
            UpdateElement = UpdateElementMethod,
        };
        listController.Enable();
        DalamudApi.PluginLog.Info("Content Finder overlay enabled");
    }

    public void Dispose()
    {
        listController?.Dispose();
    }

    private static AtkComponentListItemRenderer* GetPopulatorMethod(AddonContentsFinder* addonContentsFinder)
    => addonContentsFinder->DutyList->GetComponentItemRendererById(6);

    private bool ShouldModifyElementMethod(AddonContentsFinder* addon, ListItemData listItem)
    {
        if (!apState.Connected) {
            return false;
        }
        String dutyName = listItem.GetNode<AtkTextNode>(3)->NodeText.ExtractText();
        DalamudApi.PluginLog.Debug("Checking DutyList entry: {0}", listItem.GetNode<AtkTextNode>(3)->NodeText.ToString());
        var location = apState.MissingLocations.FirstOrDefault(l => l.Name.Equals(dutyName, StringComparison.InvariantCultureIgnoreCase));
        if (location != null && location.Accessible)
        {
            DalamudApi.PluginLog.Debug("Found Check");
            return true;
        }
        return false;
    }

    private void UpdateElementMethod(AddonContentsFinder* addon, ListItemData listItem)
    {
        String dutyName = listItem.GetNode<AtkTextNode>(3)->NodeText.ExtractText();
        var location = apState.MissingLocations.FirstOrDefault(l => l.Name.Equals(dutyName, StringComparison.InvariantCultureIgnoreCase));
        if (imageNodes.TryGetValue(listItem.NodeId, out var node))
        {
            if (location == null)
            {
                node.IsVisible = false;
            }
            else
            {
                node.IsVisible = true;
            }
        }
        else
        {
            AtkTextNode* textNode = listItem.GetNode<AtkTextNode>(3);
            var newNode = new APDutyIcons
            {
                Size = new Vector2(24.0f, 24.0f),
                Position = new Vector2(textNode->X + textNode->Width, 0.0f),
            };
            newNode.AttachNode(textNode, NodePosition.AfterTarget);

            imageNodes.Add(listItem.NodeId, newNode);
        }

    }

}
