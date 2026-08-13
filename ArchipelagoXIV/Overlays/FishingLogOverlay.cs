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
        private NativeListController<AddonFishingNote> regionList;

        private AtkComponentButton*[] fishList = new AtkComponentButton*[10];
        private AtkTextNode*[] regions = new AtkTextNode*[19];
        private AtkTextNode*[] fishingHoles = new AtkTextNode*[26];


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
                OnFinalize = OnAddonClose,
            };
            addonController.Enable();
            regionList = new NativeListController<AddonFishingNote>()
            {
                AddonName = "FishingNote",
                GetPopulatorNode = SetPopulatorNode,
            };
        }


        /// <summary>
        /// Updates APDuty icons in Fish list
        /// </summary>
        /// <param name="addon"></param>
        private void OnAddonUpdate(AddonFishingNote* addon)
        {
            if (!apState.Connected) return;
            if (!isSetup)
            {
                isSetup = true;
                addIcons(addon);
            }
            for (uint i = 0; i <= 9; i++)
            {
                
            }
        }

        /// <summary>
        /// Clears icons when the fishing log is closed
        /// </summary>
        /// <param name="addon"></param>
        private void OnAddonClose(AddonFishingNote* addon)
        {
            regionApIcons = new APDutyIcon[1];
        }


        /// <summary>
        /// Adds icons to hole fish list
        /// </summary>
        /// <param name="addon"></param>
        private void addIcons(AddonFishingNote* addon)
        {
            DalamudApi.PluginLog.Debug("Fishing Log: Adding Icons");

            //Add icons to fish listed in fishing hole info
            for (uint i = 0; i <= 9; i++)
            {
                fishList[i] = addon->GetComponentButtonById(i + 27);
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


        private AtkComponentListItemRenderer* SetPopulatorNode(AddonFishingNote* addon) => addon->GetComponentListById(10)->GetComponentItemRendererById(2);

        public void Dispose()
        {
            //if (fishListApIcons != null) foreach (var item in fishListApIcons) item.Dispose();
            //if (regionApIcons != null) foreach (var item in regionApIcons) item.Dispose();
            //if (fishingHoleApIcons != null) foreach (var item in fishingHoleApIcons) item.Dispose();
            addonController.Dispose();
            regionList.Dispose();
        }
    }
}
