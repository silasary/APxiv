using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoXIV.Rando
{
    internal class FishData
    {
        //public string Name;
        public int Id;
        public int Level;
        public Region[] Regions;
        public string[] Baits;
    }

    internal class Fish : Location
    {
        public Fish(ApState apState, long id, string name) : base(apState, id, name)
        {
            Data = APData.FishData[name];
            region = Data.Regions[0];
        }

        public FishData Data { get; }


        public override bool IsAccessible()
        {
            if (Completed)
                return false;
            if (stale)
            {
                stale = false;

                if (region == null)
                    return Accessible = false;
                var allMissingLocations = apState?.session?.Locations?.AllMissingLocations;
                if (allMissingLocations == null)
                    return Accessible = false;
                if (!allMissingLocations.Contains(ApId))
                    return Accessible = false;
                if (!apState?.Game?.MeetsRequirements(this) ?? false)
                    return Accessible = false;

                return Accessible = true;
            }
            return Accessible;
        }
    }
}
