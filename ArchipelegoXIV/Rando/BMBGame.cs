using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArchipelegoXIV.Rando
{
    internal partial class BMBGame(ApState apState) : BaseGame(apState)
    {
        private readonly Regex FATE = FateRegex();

        public override string Name => "Manual_FFXIVBMB_Pizzie";

        public override bool MeetsRequirements(Location location)
        {
            var zone = location.Name;
            if (zone.StartsWith("Masked Carnivale"))
                zone = "Masked Carnivale";

            var match = FATE.Match(zone);
            if (match.Success)
            {
                zone = match.Groups[1].Value;
            }
            if (!HaveZoneAccess(zone))
                return false;
            // TODO: Location-specific requirements.
            return true;
        }

        private bool HaveZoneAccess(string zone)
        {
            return RegionContainer.CanReach(apState, zone);
        }

        [GeneratedRegex("([A-Za-z ]+): FATE #(\\d+)")]
        private static partial Regex FateRegex();
    }
}
