using System;
using System.Linq;
using Lumina.Excel.GeneratedSheets;

namespace ArchepelegoXIV
{
    internal class Data
    {
        public static TerritoryType[] Territories { get; private set; } = Array.Empty<TerritoryType>();
        public static InstanceContent[] Duties { get; private set; } = Array.Empty<InstanceContent>();

        public static void Initialize() {
            var territoryTypes = DalamudApi.DataManager.GetExcelSheet<TerritoryType>();
            Territories = territoryTypes.ToArray();

            var duties = DalamudApi.DataManager.GetExcelSheet<InstanceContent>();
            Duties = duties.ToArray();
        }
    }
}
