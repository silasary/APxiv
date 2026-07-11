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
            if (!Data.Aetherytes.TryGetValue(aetheryteName, out var info) || !APData.Regions.TryGetValue(info.Zone, out var r))
                throw new Exception($"Attune location {name} has no valid region.");
            region = r;
            MeetsRequirements = Logic.Always();
        }
    }
}
