using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.GeneratedSheets;

namespace ArchepelegoXIV
{
    internal partial class Data
    {
        public static TerritoryType[] Territories { get; private set; } = Array.Empty<TerritoryType>();
        public static InstanceContent[] Duties { get; private set; } = Array.Empty<InstanceContent>();
        public static ClassJob[] ClassJobs { get; private set; } = Array.Empty<ClassJob>();

        public static Dictionary<string, string> DungeonEntrances { get; private set; }

        public static void Initialize() {
            var dataManager = DalamudApi.DataManager;
            if (dataManager == null)
                return;
            Territories = dataManager.GetExcelSheet<TerritoryType>().ToArray();

            Duties = dataManager.GetExcelSheet<InstanceContent>().ToArray();

            ClassJobs = dataManager.GetExcelSheet<ClassJob>().ToArray();
            
        }
    }
}
