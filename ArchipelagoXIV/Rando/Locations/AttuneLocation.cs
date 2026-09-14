using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArchipelagoXIV.Rando.Locations
{
    internal class AttuneLocation : Location
    {
        public AttuneLocation(ApState apState, long id, string name) : base(apState, id, name)
        {
            var aetheryteName = name.Replace("Attune ", "").Trim();
            if (!APData.Aetherytes.TryGetValue((uint)id, out var info) || !APData.RegionsByTerritoryType.TryGetValue(info.Territory.RowId, out var r))
                throw new Exception($"Attune location {name} has no valid region.");
            Aetheryte = info;
            region = r;
            MeetsRequirements = Logic.Always();
        }

        public APData.AetheryteInfo Aetheryte { get; }
    }
}
