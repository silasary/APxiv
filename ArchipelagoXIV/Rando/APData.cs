using ArchipelagoXIV.Rando.Locations;
using Lumina.Excel.Sheets;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ArchipelagoXIV.Rando
{
    internal static class APData
    {
        internal record AetheryteInfo(uint apid, string Name, TerritoryType Territory, uint AttunePlace);

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
            // Inns
            { "Mizzenmast Inn", "Limsa Lominsa"},
            { "The Roost", "Gridania"},
            { "The Hourglass", "Ul'dah"},
            { "Cloud Nine", "Ishgard"},
            { "Bokairo Inn", "Kugane"},
            { "The Pendants Personal Suite", "The Crystarium"},
            { "Andron", "Old Sharlayan"},
            { "The For'ard Cabins", "Tuliyollal"},
            // Gold Saucer
            { "Chocobo Square", "The Gold Saucer" },
            { "The Battlehall", "The Gold Saucer" },
            // Instanced Zone
            { "The Waking Sands", "Western Thanalan" },
            { "Fortemps Manor", "Ishgard" },
            { "Sacrificial Chamber", "The Dravanian Forelands" },
            { "Matoya's Cave", "The Dravanian Hinterlands" },
            { "The Lightfeather Proving Grounds", "Ishgard" },
            { "Ruby Bazaar Offices", "Kugane" },
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
            { 1066, "The Merchant's Tale" },
        };

        public static Dictionary<string, ushort> CheckNameToContentID = new()
        {
            { "The Thousand Maws of Toto-Rak", 1 },
            { "The Tam-Tara Deepcroft", 2 },
            { "The Tam-Tara Deepcroft (Hard)", 24 },
            { "The Merchant's Tale", 1066 },
        };

        public static readonly Dictionary<string, Region> Regions = [];
        public static readonly Dictionary<uint, Region> RegionsByTerritoryType = [];
        public static readonly Dictionary<string, FishData> FishData = [];
        public static readonly Dictionary<string, int> FateData = [];
        public static readonly Dictionary<string, int> HuntData = [];
        public static readonly Dictionary<string, string> HuntRankData = [];

        public static Dictionary<string, Dictionary<string, string>> ObsoleteChecks { get; private set; } = [];
        public static FrozenDictionary<uint, AetheryteInfo> Aetherytes { get; private set; }

        public static void LoadDutiesCsv()
        {
            string[] headers;
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArchipelagoXIV.duties.csv");
            using var reader = new StreamReader(stream);
            string? line = null;
            headers = reader.ReadLine()?.Split(',') ?? [];
            var iName = Array.IndexOf(headers, "Name");
            var iLocation = Array.IndexOf(headers, "Location");
            var iContentFinderID = Array.IndexOf(headers, "ContentFinderID");
            while ((line = reader.ReadLine()) != null)
            {
                var row = line.Split(',');
                if (string.IsNullOrWhiteSpace(row[iName].Trim()))
                    continue;

                if (row[iName].StartsWith('"'))
                    row[iName] = row[iName].Trim('"');
                Aliases[row[iName].Trim()] = row[iLocation].Trim();
                if (ushort.TryParse(row[iContentFinderID].Trim(), out var contentFinderId))
                {
                    CheckNameToContentID[row[iName].Trim()] = contentFinderId;
                    ContentIDToLocationName[contentFinderId] = row[iName].Trim();
                }
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
                if (name.Contains("Removed"))
                    continue;

                if (Data.FateTable.TryGetValue(name.Replace(" (FATE)", "").Replace(",", "").Trim('"').Trim().ToString().ToLower(), out var fate))
                {
                    name = fate.Name.ToString().Trim();
                    // Yes, the trim is necessary. I am not respecting the whitespace in "Butterfly Kisses of Death ".
                }
                else
                    DalamudApi.Echo($"Missing Fate: {name}");

                var level = int.Parse(row[1].Trim());
                level = Math.Max(level - 5, (int)Math.Floor(level / 10.0) * 10);
                var zone = row[2].Trim();
                if (zone == "The Firmament")
                    name += " (FETE)";
                else if (!name.EndsWith("(FATE)"))
                    name += " (FATE)";
                Aliases[name] = zone.Trim();
                FateData[name] = level;
            }
        }

        public static void LoadHuntsCsv()
        {
            // hunts.csv columns: BNpcNameId,Name,Rank,Location,Level,Expansion
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArchipelagoXIV.hunts.csv");
            using var reader = new StreamReader(stream);
            Regex CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
            string? line = null;

            while ((line = reader.ReadLine()) != null)
            {
                var row = CSVParser.Split(line);
                if (row[0].Trim() == "BNpcNameId")
                    continue;

                var name = $"Hunt {row[1].Trim()}";
                var rank = row[2].Trim();
                var zone = row[3].Trim();
                var level = int.Parse(row[4].Trim());

                Aliases[name] = zone;
                HuntData[name] = level;
                HuntRankData[name] = rank;
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
                var territoryTypeIds = region.Value["ids"]?.ToObject<uint[]>() ?? [];
                _ = new Region(region.Key, connections.ToArray() ?? [], rule, territoryTypeIds);
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
                var intuition = fish.Value<JObject>("logical_intuition");
                List<string> zoneNames = [];
                List<string> baits = [];
                List<string> intuitionbaits = [];
                foreach (var z in zones)
                {
                    var zbaits = z.Value.Values<string>().ToArray();
                    if (zbaits.Length == 0)
                        continue;
                    zoneNames.Add(z.Key);
                    baits.AddRange(zbaits);
                    //This is extremely hacky. Someone pls fix this should be very easy. Intuition only has a single zone/hole as of DT
                    foreach (var y in intuition)
                    {
                        var zintuition = y.Value.Values<string>().ToArray();
                        if (zintuition.Length != 0)
                            intuitionbaits.AddRange(zintuition);
                    }
                }
                baits = baits.Distinct().ToList();
                intuitionbaits = intuitionbaits.Distinct().ToList();
                var data = new FishData
                {
                    Level = (int)Math.Floor(fish.Value<int>("lvl") / 5.0) * 5,
                    Id = fish.Value<int>("id"),
                    Baits = [.. baits],
                    Intuition = [.. intuitionbaits],
                    Regions = zoneNames.Select(z => Regions[z]).ToArray(),
                };
                APData.FishData[fish.Value<string>("name")] = data;

            }
        }

        internal static void LoadAetherytes()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArchipelagoXIV.aetherytes.json");
            using var reader = new StreamReader(stream);
            var aetheryte_data = JArray.Parse(reader.ReadToEnd());
            var aetherytes = new Dictionary<uint, AetheryteInfo>();
            var gamedata = DalamudApi.DataManager.GetExcelSheet<Aetheryte>()
                .Where(a => a.PlaceName.RowId > 10 && a.IsAetheryte).ToDictionary(a => a.RowId);

            foreach (JObject aetheryte in aetheryte_data)
            {
                var id = aetheryte.Value<uint>("id");
                var apid = 50000 + id;
                var name = gamedata[id].PlaceName.Value.Name.ExtractText();
                var territory = gamedata[id].Territory.Value;
                var attunePlace = aetheryte.Value<uint>("place_id");
                var info = new AetheryteInfo(apid, name, territory, attunePlace);
                aetherytes[apid] = info;

            }
            APData.Aetherytes = aetherytes.ToFrozenDictionary();
        }
    }
}
