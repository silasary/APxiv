using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;
using System.Collections.Generic;
using System.Linq;

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

                var allMissingLocations = apState?.session?.Locations?.AllMissingLocations;
                if (allMissingLocations == null)
                    return Accessible = false;
                if (!allMissingLocations.Contains(ApId))
                    return Accessible = false;
                if (!Logic.Level(Data.Level, "FSH")(apState, false))
                    return Accessible = false;
                var region = Data.Regions.FirstOrDefault(r => RegionContainer.CanReach(apState, r));
                if (region == null)
                    return Accessible = false;
                else
                    this.region = region;
                if (!Data.Baits.Any(b => apState.Items.Contains(b)))
                    return Accessible = false;
                return Accessible = true;
            }
            return Accessible;
        }

        internal unsafe bool OutOfLogic()
        {
            var currentBait = PlayerState.Instance()->FishingBait;
            var currentBaitName = ArchipelagoXIV.Data.Items[currentBait].Name.ToString();
            APData.Regions.TryGetValue(RegionContainer.LocationToRegion(apState.territoryName, (ushort)apState.territory.RowId), out var region);
            //DalamudApi.Echo($"{Name} with {currentBaitName} in {region.Name}");
            if (!apState.Items.Contains(currentBaitName))
            {
                DalamudApi.Echo($"{currentBaitName} is not in logic");
                return false;
            }
            if (!Data.Regions.Contains(region))
            {
                // Retainer fish
                return false;
            }
            if (!RegionContainer.CanReach(apState, region))
            {
                DalamudApi.Echo($"{region.Name} is not in logic");
                return false;
            }
            
            return true;

        }
    }
}
