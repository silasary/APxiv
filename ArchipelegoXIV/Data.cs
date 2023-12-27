using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.GeneratedSheets;

namespace ArchipelegoXIV
{
    internal static partial class Data
    {
        public static TerritoryType[] Territories { get; private set; } = [];
        public static InstanceContent[] Duties { get; private set; } = [];
        public static ClassJob[] ClassJobs { get; private set; } = [];
        public static ContentFinderCondition[] Content { get; private set; } = [];

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
        };

        public static Dictionary<string, string> DutyAliases = new()
        {
            { "Akademia Anyder", "Akadaemia Anyder" },
            { "Hell's Lid", "Hells' Lid" },
            { "Matoya’s Relict", "Matoya's Relict" },
            { "Paglth’an", "Paglth'an" },
            { "Dhon Mheg", "Dohn Mheg" },
            { "Malikah’s Well", "Malikah's Well" },
            { "The Heroes’ Gauntlet", "The Heroes' Gauntlet" },
        };
        public static void Initialize() {
            var dataManager = DalamudApi.DataManager;
            if (dataManager == null)
                return;
            Territories = [.. dataManager.GetExcelSheet<TerritoryType>()];

            Duties = [.. dataManager.GetExcelSheet<InstanceContent>()];

            ClassJobs = [.. dataManager.GetExcelSheet<ClassJob>()];

            Content = [.. dataManager.GetExcelSheet<ContentFinderCondition>()];

        }

        public static ContentFinderCondition GetDuty(ushort territoryId) {
            var territory = Territories.FirstOrDefault(row => row.RowId == territoryId);
            return territory?.ContentFinderCondition?.Value;
        }
    }
}
