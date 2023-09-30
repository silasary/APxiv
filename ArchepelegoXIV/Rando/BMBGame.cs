using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArchepelegoXIV.Rando
{
    internal class BMBGame : BaseGame
    {
        readonly Regex FATE = new("([A-Za-z ]+): FATE #(\\d+)");

        public BMBGame(ApState apState) : base(apState)
        {
        }

        public override string Name => "Manual_FFXIVBMB_Pizzie";

        public override bool MeetsRequirements(Location location)
        {
            if (location.Name.StartsWith("Masked Carnivale"))
                return apState.Items.Contains("Ul'dah and Central Thanalan Access");
            
            var match = FATE.Match(location.Name);
            if (match.Success)
            {
                var zone = match.Groups[1].Value;

                return HaveZoneAccess(zone);
            }
            if (Data.DungeonEntrances.TryGetValue(location.Name, out var value) && value != null)
            {
                return HaveZoneAccess(value);
            }
            // We don't know this location yet?
            //DalamudApi.Echo($"Unknown Location {location.Name}");
            return false;
        }

        private bool HaveZoneAccess(string zone)
        {
            if (zone == "Central Shroud")
                zone = "Gridania and Central Shroud";
            if (zone == "Central Thanalan")
                zone = "Ul'dah and Central Thanalan";
            if (zone == "Middle La Noscea")
                zone = "Limsa Lominsa and Middle La Noscea";
            if (apState.Items.Contains($"{zone} Access"))
                return true;
            return false;
        }
    }
}
