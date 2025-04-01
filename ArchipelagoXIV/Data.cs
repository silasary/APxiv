using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Lumina.Excel.Sheets;

namespace ArchipelagoXIV
{
    internal static partial class Data
    {
        public static TerritoryType[] Territories { get; private set; } = [];
        public static InstanceContent[] Duties { get; private set; } = [];
        public static ClassJob[] ClassJobs { get; private set; } = [];
        public static ContentFinderCondition[] Content { get; private set; } = [];
        public static DynamicEvent[] DynamicEvents { get; private set; } = [];
        public static ImmutableDictionary<uint, Item> Items { get; private set; } = null;
        public static IKDRoute[] IKDRoutes { get; private set; }

        public static FrozenDictionary<string, Fate> FateTable { get; private set; } = null;

        public static Dictionary<string, int> FateLevels = new()
        {
            {"Middle La Noscea", 3},
            {"Lower La Noscea", 3},
            {"Eastern La Noscea",30},
            {"Western La Noscea",10},
            {"Upper La Noscea", 20},
            {"Outer La Noscea", 30},

            {"Central Shroud",4},
            {"East Shroud", 11},
            {"South Shroud",21},
            {"North Shroud",3},

            {"Central Thanalan",5},
            {"Western Thanalan",5},
            {"Eastern Thanalan",15},
            {"Southern Thanalan",25},
            {"Northern Thanalan",49},

            {"Coerthas Central Highlands",35},
            {"Coerthas Western Highlands", 50},

            {"Mor Dhona", 44},

            {"The Sea of Clouds", 50},
            {"Azys Lla", 59},

            {"The Dravanian Forelands", 52},
            {"The Churning Mists", 54},
            {"The Dravanian Hinterlands", 58},

            {"The Fringes", 60},
            {"The Peaks", 60},
            {"The Lochs", 69},

            {"The Ruby Sea", 62},
            {"Yanxia", 64},
            {"The Azim Steppe", 65},

            {"Lakeland", 70},
            {"Kholusia", 70},
            {"Il Mheg", 72},
            {"Amh Araeng", 76},
            {"The Rak'tika Greatwood", 74},
            {"The Tempest", 79},

            {"Labyrinthos", 80},
            {"Thavnair", 80},
            {"Garlemald", 82},
            {"Mare Lamentorum", 83},
            {"Elpis", 86},
            {"Ultima Thule", 88},

            {"Urqopacha", 90},
            {"Kozama'uka", 90},
            {"Yak T'el", 94},
            {"Shaaloani", 95},
            {"Heritage Found", 97},
            {"Living Memory", 99},
        };

        public static Dictionary<string, string> DutyAliases = new()
        { // Mostly typos in previous APWorlds
            { "Akademia Anyder", "Akadaemia Anyder" },
            { "Hell's Lid", "Hells' Lid" },
            { "Matoya’s Relict", "Matoya's Relict" },
            { "Paglth’an", "Paglth'an" },
            { "Dhon Mheg", "Dohn Mheg" },
            { "Malikah’s Well", "Malikah's Well" },
            { "The Heroes’ Gauntlet", "The Heroes' Gauntlet" },
            { "Hero on the Halfshell", "Hero on the Half Shell" },
            { "Satasha", "Sastasha" },
            { "The Dying Gasp (Extreme)", "The Minstrel's Ballad: Hades's Elegy" },
            { "A Relic Reborn: The Hydra", "A Relic Reborn: the Hydra" },
            { "A Relic Reborn: The Chimera", "A Relic Reborn: the Chimera" },
            { "The Fist of the Father", "Alexander - The Fist of the Father" },
            { "The Cuff of the Father", "Alexander - The Cuff of the Father" },
            { "The Arm of the Father", "Alexander - The Arm of the Father" },
            { "The Burden of the Father", "Alexander - The Burden of the Father" },
            { "The Fist of the Son", "Alexander - The Fist of the Son" },
            { "The Cuff of the Son", "Alexander - The Cuff of the Son" },
            { "The Arm of the Son", "Alexander - The Arm of the Son" },
            { "The Burden of the Son", "Alexander - The Burden of the Son" },
            { "The Eyes of the Creator", "Alexander - The Eyes of the Creator" },
            { "The Breath of the Creator", "Alexander - The Breath of the Creator" },
            { "The Heart of the Creator", "Alexander - The Heart of the Creator" },
            { "The Soul of the Creator", "Alexander - The Soul of the Creator" },
            { "Castrum Lacus Litore", "The Battle of Castrum Lacus Litore" },
            { "Harder Bigger Faster Stronger (FATE)", "Harder, Bigger, Faster, Stronger (FATE)"},
            { "A Mad Mad Mad Sergeant (FATE)", "A Mad, Mad, Mad Sergeant (FATE)" },
            { "Hello Work (FATE)", "Hello, Work (FATE)"},
            { "Please Hammers Don't Hurt Them (FATE)", "Please Hammers, Don't Hurt Them (FATE)"},
            { "Spring Forward Fall Back (FATE)", "Spring Forward, Fall Back (FATE)"},
            { "Blood Augur Hex Magicks (FATE)", "Blood, Augur, Hex, Magicks (FATE)"},
            { "Tempered Tempered (FATE)", "Tempered, Tempered (FATE)"},
            { "Go Go Gorgimera (FATE)", "Go, Go, Gorgimera (FATE)"},
            { "Another Day Another Demon (FATE)", "Another Day, Another Demon (FATE)"},
            { "Long Hands Long Fingers (FATE)", "Long Hands, Long Fingers (FATE)"},
            { "This Is a Da This Is a Peng (FATE)", "This Is a Da, This Is a Peng (FATE)"},
            { "A Tisket A Tasket (FATE)", "A Tisket, A Tasket (FATE)"},
            { "No Ifs Ands or Butts (FATE)", "No Ifs, Ands, or Butts (FATE)"},
            { "Sugar Sugar (FATE)", "Sugar, Sugar (FATE)"},
            { "One Whiskey One Goat One Spear (FATE)", "One Whiskey, One Goat, One Spear (FATE)"},
            { "Run Riders Run (FATE)", "Run, Riders, Run (FATE)"},
            { "OK Golem (FATE)", "OK, Golem (FATE)"},
            { "Dude Where's My Karya (FATE)", "Dude, Where's My Karya (FATE)"},
            { "Love Me Tindalos (FATE)", "Love Me, Tindalos (FATE)"},
            { "Wham Bam Thank You Mammoth (FATE)", "Wham, Bam, Thank You, Mammoth (FATE)"},
            { "Yes This Is Dogs (FATE)", "Yes, This Is Dogs (FATE)"},
            { "Feed Me See More (FATE)", "Feed Me, See More (FATE)"},
            { "The Second Coil of Bahamut - Turn 1 (Savage)", "The Second Coil of Bahamut (Savage) - Turn 1"},
            { "The Second Coil of Bahamut - Turn 2 (Savage)", "The Second Coil of Bahamut (Savage) - Turn 2"},
            { "The Second Coil of Bahamut - Turn 3 (Savage)", "The Second Coil of Bahamut (Savage) - Turn 3"},
            { "The Second Coil of Bahamut - Turn 4 (Savage)", "The Second Coil of Bahamut (Savage) - Turn 4"},
            { "Consigned Sealed and Undelivered (FATE)", "Consigned, Sealed, and Undelivered (FATE)"},
        };

        public static void Initialize() {
            var dataManager = DalamudApi.DataManager;
            if (dataManager == null)
                return;
            Territories = [.. dataManager.GetExcelSheet<TerritoryType>()];

            Duties = [.. dataManager.GetExcelSheet<InstanceContent>()];

            ClassJobs = [.. dataManager.GetExcelSheet<ClassJob>()];

            Content = [.. dataManager.GetExcelSheet<ContentFinderCondition>()];

            DynamicEvents = [.. dataManager.GetExcelSheet<DynamicEvent>()];

            Items = dataManager.GetExcelSheet<Item>().ToImmutableDictionary(i => i.RowId);

            IKDRoutes = [.. dataManager.GetExcelSheet<IKDRoute>()];

            FateTable = DalamudApi.DataManager.GetExcelSheet<Fate>().DistinctBy(f => f.Name.ToString()).ToFrozenDictionary(f => f.Name.ToString().ToLower().Replace(",", "").Trim());
        }

        public static ContentFinderCondition GetDuty(ushort territoryId) {
            var territory = Territories.FirstOrDefault(row => row.RowId == territoryId);
            return territory.ContentFinderCondition.Value;
        }
    }
}
