using Archipelago.MultiClient.Net.Models;
using ArchipelagoXIV.Rando.Locations;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArchipelagoXIV.Rando
{
    public enum VictoryType
    {
        McGuffin,
        DefeatUltimaWeapon,
        DefeatThordan,
        DefeatNidhogg,
        DefeatShinryu,
        DefeatTsukuyomi,
        DefeatHades,
        DefeatWarriorOfLight,
        DefeatEndsinger,
        DefeatZeromus,
        DefeatSphene,
        DefeatNecron,
        DefeatTwintania,
        DefeatNael,
        DefeatBahamutPrime,
        DefeatManipulator,
        DefeatBruteJustice,
        DefeatAlexanderPrime,
        DefeatExdeath,
        DefeatKefka,
        DefeatOmegaMF,
        DefeatTitanEden,
        DefeatShivaEden,
        DefeatEdensPromise,
        DefeatHesperos,
        DefeatHephaistos,
        DefeatAthena,
        DefeatWickedThunder,
        DefeatHowlingBlade,
        DefeatLindwurm,
        MaskedCarnivale30,
        None,
        Unknown,
        PotDFloor50,
    };

    public abstract class BaseGame(ApState apState)
    {
        protected ApState apState = apState;

        public int Goal { get; protected set; }
        public abstract bool HasMapItems { get; }

        private readonly string[] DHLJobs = ["CRP", "BSM", "ARM", "GSM", "LTW", "WVR", "ALC", "CUL", "MIN", "BTN", "FSH"];

        public readonly List<ClassJob> ProgJobs = [];

        public readonly HashSet<uint> AttunedAetherytes = [];

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
            if (slotData.TryGetValue("world_version", out var version_obj))
            {
                var version_string = version_obj as string ?? "0.0.0";
                this.WorldVersion = Version.Parse(version_string);
            }
            else
            {
                this.WorldVersion = new Version(0, 0, 0);
            }
        }

        internal virtual string GoalString() => GoalType switch
        {
            VictoryType.None => "No Goal set",
            VictoryType.McGuffin => "McGuffin Hunt",
            VictoryType.DefeatUltimaWeapon => "Defeat the Ultima Weapon at The Porta Decumana",
            VictoryType.DefeatThordan => "Defeat King Thordan at The Singularity Reactor",
            VictoryType.DefeatNidhogg => "Defeat Nidhogg at The Final Steps of Faith",
            VictoryType.DefeatShinryu => "Defeat Shinryu at The Royal Menagerie",
            VictoryType.DefeatTsukuyomi => "Defeat Tsukuyomi at Castrum Fluminis",
            VictoryType.DefeatHades => "Defeat Hades at The Dying Gasp",
            VictoryType.DefeatWarriorOfLight => "Defeat the Warrior of Light at The Seat of Sacrifice",
            VictoryType.DefeatEndsinger => "Defeat the Endsinger at The Final Day",
            VictoryType.DefeatZeromus => "Defeat Zeromus at The Abyssal Fracture",
            VictoryType.DefeatSphene => "Defeat Sphene at The Interphos",
            VictoryType.DefeatNecron => "Defeat Necron at The Ageless Necropolis",
            VictoryType.DefeatTwintania => "Defeat Twintania at The Binding Coil of Bahamut - Turn 5",
            VictoryType.DefeatNael => "Defeat Nael deus Darnus at The Second Coil of Bahamut - Turn 4",
            VictoryType.DefeatBahamutPrime => "Defeat Bahamut Prime at The Final Coil of Bahamut - Turn 4",
            VictoryType.DefeatManipulator => "Defeat The Manipulator at Alexander - The Burden of the Father",
            VictoryType.DefeatBruteJustice => "Defeat Brute Justice at Alexander - The Burden of the Son",
            VictoryType.DefeatAlexanderPrime => "Defeat Alexander Prime at Alexander - The Soul of the Creator",
            VictoryType.DefeatExdeath => "Defeat Exdeath at Deltascape V4.0",
            VictoryType.DefeatKefka => "Defeat Kefka at Sigmascape V4.0",
            VictoryType.DefeatOmegaMF => "Defeat Omega-M and Omega-F at Alphascape V4.0",
            VictoryType.DefeatTitanEden => "Defeat Titan at Eden's Gate: Sepulture",
            VictoryType.DefeatShivaEden => "Defeat Shiva at Eden's Verse: Refulgence",
            VictoryType.DefeatEdensPromise => "Defeat Eden's Promise at Eden's Promise: Eternity",
            VictoryType.DefeatHesperos => "Defeat Hesperos at Asphodelos: The Fourth Circle",
            VictoryType.DefeatHephaistos => "Defeat Hephaistos at Abyssos: The Eighth Circle",
            VictoryType.DefeatAthena => "Defeat Athena at Anabaseios: The Twelfth Circle",
            VictoryType.DefeatWickedThunder => "Defeat Wicked Thunder at AAC Light-heavyweight M4",
            VictoryType.DefeatHowlingBlade => "Defeat Howling Blade at AAC Cruiserweight M4",
            VictoryType.DefeatLindwurm => "Defeat Lindwurm at AAC Heavyweight M4",
            VictoryType.MaskedCarnivale30 => "Masked Carnivale Stage 30",
            VictoryType.PotDFloor50 => "Clear Floor 50 of Palace of the Dead",

            _ => "Unknown Goal",
        };

        internal abstract void Ready();

        internal abstract VictoryType GoalType { get; }
        public Dictionary<string, object> SlotData { get; private set; }
        public Version WorldVersion { get; private set; }
    }
}
