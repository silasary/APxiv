using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Interface.Internal;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static FFXIVClientStructs.FFXIV.Client.UI.AddonRelicNoteBook;
using static Lumina.Data.Parsing.Uld.NodeData;

namespace ArchipelagoXIV.Hooks
{
    internal unsafe partial class UIHooks(ApState apState)
    {
        public void Enable()
        {
            DalamudApi.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "ContentsFinder", OnContentsFinderRefresh);
            //DalamudApi.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "ContentsFinder", OnContentsFinderPostSetup);
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
            DalamudApi.AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "ContentsFinder", OnContentsFinderRefresh);
        }

        private void OnContentsFinderRefresh(AddonEvent type, AddonArgs args)
        {
            //var hints = apState.Hints.Select(h => h.LocationId).ToArray();
            var addon = (AddonContentsFinder*)args.Addon;
            if (addon->DutyList == null)
                return;
            foreach (var itemRenderer in addon->DutyList->Items.Span)
            {
                var componentNode = itemRenderer.Value->Renderer->AtkDragDropInterface.ComponentNode;
                if (componentNode is null) continue;

                var textNode = (AtkTextNode*)componentNode->Component->GetTextNodeById(5);
                var levelNode = (AtkTextNode*)componentNode->Component->GetTextNodeById(15);
                var hollowsImageNode = componentNode->Component->GetImageNodeById(8);
                if (levelNode is null || textNode is null) continue;

                var name = textNode->NodeText.ToString();
                var loc = apState.MissingLocations.Where(l => l.IsAccessible()).FirstOrDefault(l => l.Name == name);
                if (loc != null)
                {
                    hollowsImageNode->ToggleVisibility(true);

                    // todo: Replace the texture, maybe check if it's hinted?
                    //if (hints.Contains(loc.ApId))
                    //    hollowsImageNode->GetAsAtkImageNode()->LoadIconTexture(60004, 0); // Hunt Target tonberry
                    //else
                    //    hollowsImageNode->GetAsAtkImageNode()->LoadIconTexture(60849, 0); //

                }
            }
        }


        private void TooltipHandler(AddonEventType type, IntPtr addon, IntPtr node)
        {
            var addonId = ((AtkUnitBase*)addon)->ID;

            switch (type)
            {
                case AddonEventType.MouseOver:
                    AtkStage.GetSingleton()->TooltipManager.ShowTooltip(addonId, (AtkResNode*)node, "Archipelago check available");
                    break;

                case AddonEventType.MouseOut:
                    AtkStage.GetSingleton()->TooltipManager.HideTooltip(addonId);
                    break;
            }
        }
    }
}
