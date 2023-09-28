using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchepelegoXIV.Rando
{
    public class Location
    {
        private readonly ApState apState;

        public string Name;
        public long ApId;

        public bool Accessible;

        internal bool stale;

        public bool IsAccessible()
        {
            if (stale)
            {
                stale = false;
                var allMissingLocations = apState?.session?.Locations?.AllMissingLocations;
                if (allMissingLocations == null)
                    return Accessible = false;
                if (!allMissingLocations.Contains(ApId))
                    return Accessible = false;
                if (!apState.Game.MeetsRequirements(this))
                    return Accessible = false;
                return Accessible = true;
            }
            
            return Accessible;
        }

        public bool Cleared;

        public Location(ApState apState, long i)
        {
            this.apState = apState;
            ApId = i;
            Name = apState.session.Locations.GetLocationNameFromId(i);
            stale = true;
        }
    }
}
