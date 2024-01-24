using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ArchipelegoXIV.Rando
{
    internal static class APData
    {
        public static Dictionary<string, string> Aliases = new() {
            // Cities
            { "Limsa Lominsa Lower Decks", "Limsa Lominsa"},
            { "Limsa Lominsa Upper Decks", "Limsa Lominsa"},
            { "Old Gridania", "Gridania" },
            { "New Gridania", "Gridania" },
            { "Ul'dah - Steps of Nald", "Ul'dah" },
            { "Ul'dah - Steps of Thal", "Ul'dah" },
            { "Blue Sky", "Masked Carnivale" },
            { "Maelstrom Barracks", "Limsa Lominsa"},
            { "Twin Adder Barracks", "Gridania"},
            { "Flame Barracks", "Ul'dah"},
            { "Foundation", "Ishgard"},
            { "The Pillars", "Ishgard"},
            { "Idyllshire", "The Dravanian Forelands"},
            // Inns
            { "Location Mizzenmast Inn", "Limsa Lominsa"},
            { "The Roost", "Gridania"},
            { "The Hourglass", "Ul'dah"},
            { "Cloud Nine", "Ishgard"},
            { "Bokairo Inn", "Kugane"},
            { "Andron", "Old Sharlayan"},
            // Player Housing
            { "The Goblet", "Ul'dah" },
            { "Mist", "Limsa Lominsa" },
            { "The Lavender Beds", "Gridania" },
            { "Shirogane", "Kugane" },
            { "Empyreum", "Ishgard" },
            // Gold Saucer
            { "The Gold Saucer", "Southern Thanalan" },
            { "Chocobo Square", "Southern Thanalan" },
            { "The Battlehall", "Southern Thanalan" },
            // Instanced Zone
            { "The Waking Sands", "Western Thanalan" },
            { "Fortemps Manor", "Ishgard" },
            { "Matoya's Cave", "The Dravnian Hinterlands" },
            { "The Lightfeather Proving Grounds", "Ishgard" },
            { "Ruby Bazaar Offices", "Kugane" },
            { "The Doman Enclave", "Yanxia" },
            { "The Omphalos", "Mor Dhona"  },
            { "Main Hall", "Old Sharlayan" },
            { "Elysion", "Ultima Thule"},
            // AP Checks
            { "Return to the Waking Sands", "Western Thanalan" },
        };

        public static Dictionary<string, Region> Regions = [];

        public static void LoadDutiesCsv()
        {
            string[] headers = ["", "Name", "ARR", "HW", "STB", "SHB", "EW"];
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArchipelagoXIV.duties.csv");
            using var reader = new StreamReader(stream);
            string? line = null;
            while ((line = reader.ReadLine()) != null)
            {
                var row = line.Split(',');
                if (headers.Contains(row[0].Trim()))
                    continue;
                Aliases[row[0].Trim()] = row[4].Trim();
            }
        }

        public static void LoadFatesCsv()
        {
            string[] headers = ["", "Name"];

            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArchipelagoXIV.fates.csv");
            using var reader = new StreamReader(stream);
            string? line = null;
            while ((line = reader.ReadLine()) != null)
            {
                var row = line.Split(',');
                var name = row[0].Trim();
                var zone = row[2];
                if (headers.Contains(name))
                    continue;
                if (zone == "The Firmament")
                    name += " (FETE)";
                else if (!name.EndsWith("(FATE)"))
                    name += " (FATE)";
                Aliases[name] = zone.Trim();
            }
        }

        public static void LoadJson()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArchipelagoXIV.regions.json");
            using var reader = new StreamReader(stream);
            var regions = JObject.Parse(reader.ReadToEnd());
            foreach (var region in regions)
            {
                var connections = new List<string>();
                connections.AddRange(region.Value["connects_to"]?.ToObject<List<string>>() ?? []);

                var rule = Logic.Always();
                var requires = region.Value.Value<string>("requires");
                if (requires != null)
                    rule = Logic.FromString(requires);
                _ = new Region(region.Key, connections.ToArray() ?? [], rule);
            }
        }
    }
}
