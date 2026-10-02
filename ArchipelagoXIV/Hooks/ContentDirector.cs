using ArchipelagoXIV.Rando.Locations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArchipelagoXIV.Hooks
{
    internal class ContentDirector
    {

        private ApState apState;
        private DutyLocation? CurrentDuty;
        private uint currentcf = 0;

        public ContentDirector(ApState apState)
        {
            this.apState = apState;
        }

        public int DutyProgress { get; private set; }

        public unsafe void FrameworkUpdate()
        {
            if (!DalamudApi.DutyState.IsDutyStarted)
                return;

            if (currentcf != DalamudApi.DutyState.ContentFinderCondition.Value.RowId)
            {
                currentcf = DalamudApi.DutyState.ContentFinderCondition.Value.RowId;
                CurrentDuty = apState.AllLocations.OfType<DutyLocation>().FirstOrDefault(d => d.Content.RowId == DalamudApi.DutyState.ContentFinderCondition.Value.RowId);
                DutyProgress = 0;
            }

            
            if (DalamudApi.DutyState.ContentFinderCondition.Value.ContentType.Value.RowId == 29 || DalamudApi.DutyState.ContentFinderCondition.Value.ContentType.Value.RowId == 38 )
            {
                DynamicContentUpdate();
            }
            if (CurrentDuty == null)
                return;

            var contentType = DalamudApi.DutyState.ContentFinderCondition.Value.ContentType.Value;

            if (contentType.RowId == 21)
            {
                DeepDungeonUpdate();
            }
            else if (contentType.RowId == 2)
            {
                InstanceContentUpdate();
            }
            else
            {
                // Not a supported content type for progress tracking
                return;
            }
        }

        private unsafe void SendSubCheck()
        {
            if (CurrentDuty != null && DutyProgress > 0 && CurrentDuty.SubLocations.Length >= DutyProgress)
            {
                CurrentDuty.SubLocations[DutyProgress - 1].Complete();
            }
        }

        private unsafe void InstanceContentUpdate()
        {
            var contentDirector = EventFramework.Instance()->GetInstanceContentDirector();
            if (contentDirector == null)
                return;
            var todos = contentDirector->GetDirectorTodos();
            var progress = 0;
            foreach (var todo in todos->ToArray())
            {
                if (!todo.Enabled)
                    continue;
                var complete = todo.Complete;
                if (todo.NeededCount > 0 && todo.NeededCount == todo.CurrentCount)
                    complete = true;
                if (complete)
                    progress++;
            }
            if (progress != DutyProgress)
            {
                DutyProgress = progress;
                DalamudApi.Echo($"Duty Progress: {DutyProgress}");
                SendSubCheck();
            }
        }

        private unsafe void DeepDungeonUpdate()
        {
            
            
            var contentDirector = EventFramework.Instance()->GetInstanceContentDeepDungeon();
            if (contentDirector == null)
                return;

            int dutyProgress = contentDirector->Floor;
            if (dutyProgress > 10)
            {
                dutyProgress = dutyProgress % 10;
                if (dutyProgress == 0)
                    dutyProgress = 10;
            }

            if (dutyProgress != DutyProgress)
            {
                DutyProgress = dutyProgress;
                DalamudApi.Echo($"Deep Dungeon Floor: {contentDirector->Floor}");
                SendSubCheck();
            }
        }

        private unsafe void DynamicContentUpdate()
        {
            var bozja = PublicContentBozja.GetInstance();
            var occult = PublicContentOccultCrescent.GetInstance();
            DynamicEventContainer* container = null;
            if (bozja != null)
            {
                container = &bozja->DynamicEventContainer;
            }
            else if (occult != null)
            {
                container = &occult->DynamicEventContainer;
            }
            if (container != null)
            {
                var currentId = container->CurrentEventId;
                foreach(var dynamicEvent in container->Events)
                {
                    //var dynamicEvent = container->Events[i];
                    var name = dynamicEvent.Name.ToString();
                    var dynamicId = dynamicEvent.DynamicEventId;
                    //DalamudApi.Echo(dynamicId.ToString());
                    if (dynamicEvent.Progress == 100 && currentId == dynamicId && DutyProgress != dynamicEvent.Progress)
                    {
                        DalamudApi.PluginLog.Debug("Dynamic Content Update: {0} ({1})", dynamicId, name);
                        Location? location = apState.MissingLocations.OfType<CriticalEncounterLocation>().FirstOrDefault(f => f.CriticalEncounter.RowId == dynamicId);
                        if (location != null)
                        {
                            if (!location.IsAccessible())
                            {
                                DalamudApi.Echo($"{location.Name} currently out of logic.");
                                return;
                            }
                            if (!location.CanClearAsCurrentClass())
                            {
                                DalamudApi.Echo($"Cannot clear {location.Name} as current class");
                                return;
                            }
                            location.Complete();
                        }
                        DutyProgress = dynamicEvent.Progress;
                    }
                    //if (dynamicId == 64)
                    //{
                    //    DalamudApi.Echo(dynamicEvent.Progress.ToString());
                    //    DalamudApi.Echo(i.ToString());
                    //}
                    else if(DutyProgress != 0 && currentId == 0)
                    {
                        //Force DutyProgress back to 0 upon leaving the Dynamic Raid
                        DutyProgress = 0;
                    }
                }
            }
        }
    }
}
