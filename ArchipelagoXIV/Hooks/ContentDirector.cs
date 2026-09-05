using ArchipelagoXIV.Rando.Locations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
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

        public ContentDirector(ApState apState)
        {
            this.apState = apState;
        }

        public int DutyProgress { get; private set; }

        public unsafe void FrameworkUpdate()
        {
            if (!DalamudApi.DutyState.IsDutyStarted)
                return;

            if (CurrentDuty == null || CurrentDuty.Content.RowId != DalamudApi.DutyState.ContentFinderCondition.Value.RowId)
            {
                CurrentDuty = apState.AllLocations.OfType<DutyLocation>().FirstOrDefault(d => d.Content.RowId == DalamudApi.DutyState.ContentFinderCondition.Value.RowId);
                DutyProgress = 0;
            }

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
    }
}
