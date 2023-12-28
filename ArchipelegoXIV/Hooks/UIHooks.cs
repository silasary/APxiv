using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
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

namespace ArchipelegoXIV.Hooks
{
    internal partial class UIHooks(ApState apState)
    {
        public void Enable()
        {
            DalamudApi.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "ContentsFinder", OnContentsFinderRefresh);
        }
        public void Disable()
        {
            DalamudApi.AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "ContentsFinder", OnContentsFinderRefresh);
        }

        private unsafe void OnContentsFinderRefresh(AddonEvent type, AddonArgs args)
        {
            
            var addon = (AddonContentsFinder*)args.Addon;
            foreach (var itemRenderer in addon->DutyList->Items.Span)
            {
                var componentNode = itemRenderer.Value->Renderer->AtkDragDropInterface.ComponentNode;
                if (componentNode is null) continue;

                var textNode = (AtkTextNode*)componentNode->Component->GetTextNodeById(5);
                var levelNode = (AtkTextNode*)componentNode->Component->GetTextNodeById(15);
                var hollowsImageNode = componentNode->Component->GetImageNodeById(8);
                if (levelNode is null || textNode is null) continue;

                var name = textNode->NodeText.ToString();
                if (apState.MissingLocations.Where(l => l.IsAccessible()).Select(l => l.Name).Contains(name))
                {
                    hollowsImageNode->ToggleVisibility(true);
                    // todo: Replace the texture, maybe check if it's hinted?
                }
            }
        }
    }
}
