using Archipelago.MultiClient.Net.Models;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArchipelagoXIV.Rando
{
    public abstract class BaseGame(ApState apState)
    {
        protected ApState apState = apState;


        private readonly string[] DHLJobs = ["CRP", "BSM", "ARM", "GSM", "LTW", "WVR", "ALC", "CUL", "MIN", "BTN", "FSH"];

        public abstract string Name { get; }

        public bool FishingMatters { get; set; }

        public virtual bool MeetsRequirements(Location location)
        {
            if (location.region != null)
                return RegionContainer.CanReach(apState, location.region);
            var zone = location.Name;
            if (zone.StartsWith("Masked Carnivale"))
                zone = "Masked Carnivale";

            var match = Regexes.FATE.Match(zone);
            if (match.Success)
            {
                zone = match.Groups[1].Value;
            }
            if (!RegionContainer.CanReach(apState, zone, 0, location))
                return false;

            return true;

        }

        public Dictionary<ClassJob, int> Levels = [];

        public abstract int MaxLevel();

        public abstract int MaxLevel(string job);
        public int MaxLevel(ClassJob job) => MaxLevel(job.Abbreviation);
        internal int MaxLevelDHL() => DHLJobs.Max(MaxLevel);

        internal virtual void ProcessItem(NetworkItem item, string itemName)
        {

        }

        internal virtual void HandleSlotData(Dictionary<string, object> slotData)
        {
            PluginLog.Information("Slot Data", slotData);
            if (slotData.TryGetValue("fishsanity", out var fishsanity))
            {
                FishingMatters = (fishsanity as bool?) ?? false;
            }
        }
    }
}
