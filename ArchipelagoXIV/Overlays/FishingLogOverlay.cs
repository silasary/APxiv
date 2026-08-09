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
        private AtkTextNode*[] areas = new AtkTextNode*[26];

        //private string selectedRegion;
        //private string selectedArea;
        //private string selectedFishingHole;

        private ApState apState;
        private APDutyIcon[] apIcons = new APDutyIcon[10];
        private bool isSetup = false;


        public FishingLogOverlay(ApState apState)
        {
            this.apState = apState;
            addonController = new AddonController<AddonFishingNote>()
            {
                AddonName = "FishingNote",
                OnUpdate = OnAddonUpdate,
            };
            addonController.Enable();
            //regionList = new NativeListController<AddonFishingNote>() {
            //    AddonName = "FishingNote",
            //    GetPopulatorNode = SetPopulatorNode,
            //};
        }

        private void OnAddonUpdate(AddonFishingNote* addon)
        {
            if (!isSetup)
            {
                addIcons(addon);
                isSetup = true;
            }

        }

        private void updateRegions(AddonFishingNote* addon)
        {
            //var regionList = addon->GetComponentListById(10)->GetComponentItemRendererById(2);
            //var AreaList = addon->GetComponentListById(11)->GetComponentItemRendererById(5);

            
        }

        private void addIcons(AddonFishingNote* addon)
        {
            for (uint i = 0; i <=9; i++)
            {
                fishList[i] = addon->GetComponentButtonById(i + 21);
                var targetNode = fishList[i]->GetImageNodeById(7);
                var newIcon = new APDutyIcon()
                {
                    Position = new Vector2(targetNode->X + targetNode->Width - 18, targetNode->Y),
                };
                newIcon.AttachNode(targetNode, NodePosition.AfterTarget);
                apIcons[i] = newIcon;
                apIcons[i].IsVisible = true;
            }
            var offset = (uint)0;
            var itemRenderer = addon->GetComponentListById(13)->GetItemRenderer(4);
            for (uint i = 0;i <=25; i++)
            {
                itemRenderer->
            }
        }


        //private AtkComponentListItemRenderer* SetPopulatorNode(AddonFishingNote* addon) => addon->GetComponentListById(10)->GetComponentItemRendererById(2);

        public void Dispose()
        {
            addonController.Dispose();
            //regionList.Dispose();
        }
    }
}
