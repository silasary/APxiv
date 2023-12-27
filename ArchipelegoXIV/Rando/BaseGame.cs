using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArchipelegoXIV.Rando
{
    public abstract class BaseGame(ApState apState)
    {
        protected ApState apState = apState;

        public abstract string Name { get; }

        public virtual bool MeetsRequirements(Location location)
        {
            var zone = location.Name;
            if (zone.StartsWith("Masked Carnivale"))
                zone = "Masked Carnivale";

            var match = Regexes.FATE.Match(zone);
            if (match.Success)
            {
                zone = match.Groups[1].Value;
            }
            if (!RegionContainer.CanReach(apState, zone))
                return false;
            
            // TODO: Location-specific requirements.
            return true;

        }

        public abstract int MaxLevel();

        public abstract int MaxLevel(string job);
    }
}
