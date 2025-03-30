using ArchipelagoXIV.Rando.Locations;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ArchipelagoXIV.Rando
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
            { "Mizzenmast Inn", "Limsa Lominsa"},
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
            { "The Backroom", "Solution Nine" },
            // AP Checks
            { "Return to the Waking Sands", "Western Thanalan" },
        };

        public static Dictionary<uint, string> ContentIDToLocationName = new()
        {
            { 1, "The Thousand Maws of Toto-Rak" }, // Yes, this is correct.
            { 2, "The Tam-Tara Deepcroft" },
            { 24, "The Tam-Tara Deepcroft (Hard)" },
        };

        public static Dictionary<string, ushort> CheckNameToContentID = new()
        {
            { "The Thousand Maws of Toto-Rak", 1 },
            { "The Tam-Tara Deepcroft", 2 },
            { "The Tam-Tara Deepcroft (Hard)", 24 },
        };

        public static readonly Dictionary<string, Region> Regions = [];
        public static readonly Dictionary<string, FishData> FishData = [];
        public static readonly Dictionary<string, int> FateData = [];

        public static Dictionary<string, Dictionary<string, string>> ObsoleteChecks { get; private set; }

        public static void LoadDutiesCsv()
        {
            string[] headers = ["", "Name", "ARR", "HW", "STB", "SHB", "EW", "DT"];
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
                Regex CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

                var row = CSVParser.Split(line);
                var name = row[0].Trim();
                if (headers.Contains(name))
                    continue;

                var level = int.Parse(row[1].Trim());
                level = Math.Max(level - 5, (int)Math.Floor(level / 10.0) * 10);
                var zone = row[2];
                if (zone == "The Firmament")
                    name += " (FETE)";
                else if (!name.EndsWith("(FATE)"))
                    name += " (FATE)";
                Aliases[name] = zone.Trim();
                FateData[name] = level;
            }
        }

        public static void LoadRegions()
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

        public static void LoadRemoved()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArchipelagoXIV.removed_locations.json");
            using var reader = new StreamReader(stream!);
            var locations = JObject.Parse(reader.ReadToEnd());
            ObsoleteChecks = locations.ToObject<Dictionary<string, Dictionary<string, string>>>()!;
        }



        public static void LoadFish()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArchipelagoXIV.fish.json");
            using var reader = new StreamReader(stream);
            var fishsanity = JObject.Parse(reader.ReadToEnd()).Values();
            foreach (JObject fish in fishsanity)
            {
                var zones = fish.Value<JObject>("zones");
                List<string> zoneNames = [];
                List<string> baits = [];
                foreach (var z in zones)
                {
                    var zbaits = z.Value.Values<string>().ToArray();
                    if (zbaits.Length == 0)
                        continue;
                    zoneNames.Add(z.Key);
                    baits.AddRange(zbaits);
                }
                baits = baits.Distinct().ToList();
                var data = new FishData
                {
                    Level = (int)Math.Floor(fish.Value<int>("lvl") / 5.0) * 5,
                    Id = fish.Value<int>("id"),
                    Baits = [.. baits],
                    Regions = zoneNames.Select(z => Regions[z]).ToArray(),
                };
                APData.FishData[fish.Value<string>("name")] = data;
            }
        }
    }
}
