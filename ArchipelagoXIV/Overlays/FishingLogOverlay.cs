using ArchipelagoXIV.Overlays.CustomNodes;
using ArchipelagoXIV.Rando;
using ArchipelagoXIV.Rando.Locations;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ArchipelagoXIV.Overlays
{
    internal unsafe class FishingLogOverlay : IDisposable
    {

        private AddonController<AddonFishingNote> addonController;
        //private NativeListController<AddonFishingNote> regionList;

        private AtkComponentButton*[] fishList = new AtkComponentButton*[10];
        private AtkTextNode*[] regions = new AtkTextNode*[19];
        private AtkTextNode*[] fishingHoles = new AtkTextNode*[26];

        //private string selectedRegion;
        //private string selectedArea;
        //private string selectedFishingHole;

        private ApState apState;
        private APDutyIcon[] fishListApIcons = new APDutyIcon[10];
        private APDutyIcon[] regionApIcons = new APDutyIcon[10];
        private APDutyIcon[] fishingHoleApIcons = new APDutyIcon[10];
        private bool isSetup = false;


        public FishingLogOverlay(ApState apState)
        {
            this.apState = apState;
            addonController = new AddonController<AddonFishingNote>()
            {
                AddonName = "FishingNote",
                OnUpdate = OnAddonUpdate,
                //OnFinalize = OnAddonClose,
            };
            addonController.Enable();
            //regionList = new NativeListController<AddonFishingNote>() {
            //    AddonName = "FishingNote",
            //    GetPopulatorNode = SetPopulatorNode,
            //};
        }

        
        private void OnAddonUpdate(AddonFishingNote* addon)
        {
            if (!apState.Connected) return;
            if (!isSetup)
            {
                isSetup = true;
                addIcons(addon);
            }

        }
        //private void OnAddonClose(AddonFishingNote* addon)
        //{
        //    regionApIcons
        //}


        private void updateRegions(AddonFishingNote* addon)
        {
            //var regionList = addon->GetComponentListById(10)->GetComponentItemRendererById(2);
            //var AreaList = addon->GetComponentListById(11)->GetComponentItemRendererById(5);

            
        }

        private void addIcons(AddonFishingNote* addon)
        {
            DalamudApi.PluginLog.Debug("Fishing Log: Adding Icons");

            //Add Icons to region list
            var offset = (uint)0;
            var list = addon->GetComponentListById(10);
            for (uint i = 0; i <= 18; i++)
            {

                if (i == 0)
                {
                    regions[0] = list->GetComponentItemRendererById(2)->GetTextNodeById(2);
                }
                else
                {
                    //DalamudApi.PluginLog.Debug("List Length: ", list->VisibleRowCount);
                    var listItem = list->GetComponentItemRendererById(i + 21000)->GetTextNodeById(20);
                    if (listItem != null)
                    {
                        regions[i] = listItem;
                    }
                    else
                    {
                        DalamudApi.PluginLog.Debug("List itme is null");
                    }
                    
                }

                //DalamudApi.PluginLog.Debug("Nodeid: {0}", (2 + i + offset));
                //regions[i] = list->GetNodeById(2)->GetAsAtkComponentList()->GetTextNodeById(2 + i + offset);
                //if( regions[i] != null)
                //{
                //    DalamudApi.PluginLog.Debug("region node is null");
                //}
                //var targetNode = regions[i];
                //var newIcon = new APDutyIcon()
                //{
                //    Position = new Vector2(targetNode->X + targetNode->Width - 18, targetNode->Y),
                //};
                //targetNode->Width -= 20;
                //targetNode->X += 15;
                //newIcon.AttachNode(targetNode, NodePosition.AfterTarget);
                //regionApIcons[i] = newIcon;
                //regionApIcons[i].IsVisible = true;

                //if (i == 0) offset = 20998;//21001
            }
            DalamudApi.PluginLog.Debug("Fishing Log: Added Region Icons");

            //Add icons to fishing hole list
            offset = (uint)0;
            list = addon->GetComponentListById(13);
            for (uint i = 0;i <=25; i++)
            {
                DalamudApi.PluginLog.Debug("Nodeid: {0}", (5 + i + offset));
                fishingHoles[i] = list->GetTextNodeById(5 + i + offset);
                fishingHoles[i] = list->GetNodeById(2)->GetAsAtkComponentListItemRenderer()->GetTextNodeById(2);
                if (fishingHoles[i] != null)
                {
                    DalamudApi.PluginLog.Debug("fishing hole node is null");
                }
                var targetNode = fishingHoles[i];
                var newIcon = new APDutyIcon()
                {
                    Position = new Vector2(targetNode->X + targetNode->Width - 18, targetNode->Y),
                };
                targetNode->Width -= 20;
                newIcon.AttachNode(targetNode, NodePosition.AfterTarget);
                fishingHoleApIcons[i] = newIcon;
                fishingHoleApIcons[i].IsVisible = true;

                if (i == 0) offset = 50995; //51001
            }
            DalamudApi.PluginLog.Debug("Fishing Log: Added fishing hole icons");

            //Add icons to fish listed in fishing hole info
            for (uint i = 0; i <= 9; i++)
            {
                fishList[i] = addon->GetComponentButtonById(i + 21);
                var targetNode = fishList[i]->GetImageNodeById(7);
                var newIcon = new APDutyIcon()
                {
                    Position = new Vector2(targetNode->X + targetNode->Width - 18, targetNode->Y),
                };
                newIcon.AttachNode(targetNode, NodePosition.AfterTarget);
                fishListApIcons[i] = newIcon;
                fishListApIcons[i].IsVisible = true;
            }
            DalamudApi.PluginLog.Debug("Fishing Log: Added Fish List Icons");
        }


        //private AtkComponentListItemRenderer* SetPopulatorNode(AddonFishingNote* addon) => addon->GetComponentListById(10)->GetComponentItemRendererById(2);

        public void Dispose()
        {
            //if (fishListApIcons != null) foreach (var item in fishListApIcons) item.Dispose();
            //if (regionApIcons != null) foreach (var item in regionApIcons) item.Dispose();
            //if (fishingHoleApIcons != null) foreach (var item in fishingHoleApIcons) item.Dispose();
            addonController.Dispose();
            //regionList.Dispose();
        }
    }
}
