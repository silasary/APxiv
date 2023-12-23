using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.GeneratedSheets;

namespace ArchipelegoXIV
{
    internal partial class Data
    {
        public static TerritoryType[] Territories { get; private set; } = [];
        public static InstanceContent[] Duties { get; private set; } = [];
        public static ClassJob[] ClassJobs { get; private set; } = [];

        public static void Initialize() {
            var dataManager = DalamudApi.DataManager;
            if (dataManager == null)
                return;
            Territories = [.. dataManager.GetExcelSheet<TerritoryType>()];

            Duties = [.. dataManager.GetExcelSheet<InstanceContent>()];

            ClassJobs = [.. dataManager.GetExcelSheet<ClassJob>()];

        }
    }
}
