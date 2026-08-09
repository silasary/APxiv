using ArchipelagoXIV.Overlays.CustomNodes;
using ArchipelagoXIV.Rando.Locations;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace ArchipelagoXIV.Overlays
{
    internal unsafe class FishGuide2Overlay : IDisposable
    {
        private AddonController<AddonFishGuide2> addonController;
        private AtkComponentRadioButton*[] pageNumberRadioButtons = new AtkComponentRadioButton*[5];
        private uint pageNumber = 0;
        private APDutyIcon[] apIcons = new APDutyIcon[100];
        private AtkComponentButton*[] fishGrid = new AtkComponentButton*[100];
        private static ExcelSheet<FishParameter> FishParameters = DalamudApi.DataManager.GetExcelSheet<FishParameter>();
        private bool isSetup = false;
        private ApState apState;

        public FishGuide2Overlay(ApState apState) {
            this.apState = apState;
            addonController = new AddonController<AddonFishGuide2>() {
                AddonName = "FishGuide2",
                //OnSetup = OnSetupFishGuide2,
                OnUpdate = OnUpdateFishGuide2,
            };
            addonController.Enable();
            DalamudApi.PluginLog.Info("Fish Guide enabled");
        }

        public void Dispose()
        {
            //foreach (var item in apIcons) item.Dispose();
            isSetup = false;
            addonController.Dispose();
        }

        //private void OnSetupFishGuide2(AddonFishGuide2* addon)
        //{
        //    throw new NotImplementedException();
        //}

        private void OnUpdateFishGuide2(AddonFishGuide2* addon)
        {
            if (!isSetup)
            {
                addIcons(addon);
                isSetup = true;
            }
            checkForPageNumberChange(addon);
            updateFishGrid(addon);
        }

        /// <summary>
        /// Adds AP icons to the grid.
        /// </summary>
        private void addIcons(AddonFishGuide2* addon)
        {
            for (uint i = 0; i <= 99; i++)
            {
                //DalamudApi.PluginLog.Debug("Fish Guide: Adding node for button {0} (NodeId:{1})", i, (i + 21));
                fishGrid[i] = addon->GetComponentButtonById(i + 21);
                var targetNode = fishGrid[i]->GetImageNodeById(8);
                var newIcon = new APDutyIcon()
                {
                    Position = new Vector2(targetNode->X + targetNode->Width - 18, targetNode->Y),
                };
                newIcon.AttachNode(targetNode, NodePosition.AfterTarget);
                apIcons[i] = newIcon;
                apIcons[i].IsVisible = false;
            }
        }

        /// <summary>
        /// Check grid for updates to check status.
        /// </summary>
        private void updateFishGrid(AddonFishGuide2* addon)
        {
            if (!apState.Connected || !isSetup)
            {
                return;
            }
            //DalamudApi.PluginLog.Debug("Updating fish grid");
            for (var i = 0; i <=99; i++)
            {
                var fishId = Data.FishParameters[(uint)(pageNumber + 100 + i)].Item.RowId;
                var checkFish = apState.MissingLocations.OfType<Fish>().FirstOrDefault(f => f.Data.Id == fishId);

                if (checkFish != null)
                {
                    apIcons[i].IsVisible = true;
                    apIcons[i].TextTooltip = checkFish.Name;
                    apIcons[i].ShowTooltip();
                }
                else
                {
                    apIcons[i].IsVisible = false;
                }
            }

        }

        /// <summary>
        /// Check if the page number has changed and set the page number if it has.
        /// </summary>
        private void checkForPageNumberChange(AddonFishGuide2* addon)
        {
            for (uint i = 0; i < 4; i++)
            {
                var node = addon->GetComponentNodeById(i + 8)->GetAsAtkComponentRadioButton();
                if (node->GetImageNodeById(5)->IsVisible())
                {
                    var page = uint.Parse(node->GetTextNodeById(4)->NodeText.ExtractText());
                    if (pageNumber != page)
                    {
                        DalamudApi.PluginLog.Debug("Changed Fish Guide page number to {0}", page);
                        pageNumber = page;
                    }
                }
            }
        }
    }
}
