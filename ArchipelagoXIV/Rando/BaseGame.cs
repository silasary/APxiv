using Archipelago.MultiClient.Net.Models;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArchipelagoXIV.Rando
{
    public enum VictoryType { McGuffin, DefeatShinryu, MaskedCarnivale30, None, PotDFloor50 };

    public abstract class BaseGame(ApState apState)
    {
        protected ApState apState = apState;

        public int Goal { get; private set; }
        public abstract bool HasMapItems { get; }

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
        public int MaxLevel(ClassJob job) => MaxLevel(job.Abbreviation.ToString());
        internal int MaxLevelDHL() => DHLJobs.Max(MaxLevel);

        internal virtual void ProcessItem(ItemInfo item, string itemName)
        {
            
        }

        internal virtual void HandleSlotData(Dictionary<string, object> slotData)
        {
            //DalamudApi.PluginLog.Information($"Slot Data: {slotData}");
            this.SlotData = slotData;
            if (slotData.TryGetValue("fishsanity", out var fishsanity))
            {
                FishingMatters = (fishsanity as bool?) ?? false;
            }
            if (slotData.TryGetValue("goal", out var goal))
            {
                Goal = (int)(long)goal;
            }
        }

        internal virtual string GoalString() => GoalType switch
        {
            VictoryType.None => "No Goal set",
            VictoryType.McGuffin => "McGuffin Hunt",
            VictoryType.DefeatShinryu => "Defeat Shinryu at The Royal Menagerie",
            VictoryType.MaskedCarnivale30 => "Masked Carnivale Stage 30",
            VictoryType.PotDFloor50 => "Clear Floor 50 of Palace of the Dead",
            _ => "Unknown Goal",
        };

        internal abstract void Ready();

        internal abstract VictoryType GoalType { get; }
        public Dictionary<string, object> SlotData { get; private set; }
    }
}
