using System;
using System.Collections.Generic;
using System.Text;

namespace ArchipelagoXIV.Rando.Locations
{
    internal class GenericFateLocation : Location
    {

        public GenericFateLocation(ApState apState, long id, string name, string zone_name, string fate_num) : base(apState, id, name)
        {
            var region = APData.Regions[zone_name];
            Name = $"{region.LocalizedName}: FATE #{fate_num}";
            this.region = region;

            if (Data.FateLevels.TryGetValue(zone_name, out var level))
                MeetsRequirements = Logic.Level(level);
        }
    }
}
