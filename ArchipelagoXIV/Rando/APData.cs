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
            // Inns
            { "Mizzenmast Inn", "Limsa Lominsa"},
            { "The Roost", "Gridania"},
            { "The Hourglass", "Ul'dah"},
            { "Cloud Nine", "Ishgard"},
            { "Bokairo Inn", "Kugane"},
            { "Andron", "Old Sharlayan"},
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

        public static Dictionary<string, string> FishingHoleRegions = new()
        {
            //La Noscea
            { "Limsa Lominsa Upper Decks", "La Noscea"},
            { "Limsa Lominsa Lower Decks", "La Noscea"},
            { "Middle La Noscea", "La Noscea"},
            { "Lower La Noscea", "La Noscea"},
            { "Eastern La Noscea", "La Noscea"},
            { "Western La Noscea", "La Noscea"},
            { "Upper La Noscea", "La Noscea" },
            { "Outer La Noscea", "La Noscea" },
            { "Mist", "La Noscea"},
            //The Black Shroud,
            { "New Gridania", "The Black Shroud"},
            { "Old Gridania", "The Black Shroud"},
            { "Central Shroud", "The Black Shroud"},
            { "East Shroud", "The Black Shroud"},
            { "South Shroud", "The Black Shroud"},
            { "North Shroud", "The Black Shroud"},
            { "The Lavender Beds", "The Black Shroud" },
            //Thanalan
            { "Western Thanalan", "Thanalan"},
            { "Central Thanalan", "Thanalan"},
            { "Eastern Thanalan", "Thanalan"},
            { "Southern Thanalan", "Thanalan"},
            { "Northern Thanalan", "Thanalan"},
            { "The Goblet", "Thanalan"},
            //Coerthas
            { "Coerthas Central Highlands", "Coerthas"},
            { "Coerthas Western Highlands", "Coerthas"},
            //Mor Dhona
            { "Mor Dhona", "Mor Dhona"},
            //Abalathia's Spine
            { "The Sea of Clouds", "Abalathia's Spine" },
            { "Azys Lla", "Abalathia's Spine" },
            //Dravania
            { "The Dravanian Forelands", "Dravania" },
            { "The Dravanian Hinterlands", "Dravania" },
            { "The Churning Mists", "Dravania" },
            //Gyr Abania
            { "Rhalgr's Reach", "Gyr Abania" },
            { "The Fringes", "Gyr Abania" },
            { "The Peaks", "Gyr Abania" },
            { "The Lochs", "Gyr Abania" },
            //Othard
            { "The Ruby Sea", "Othard" },
            { "Yanxia", "Othard" },
            { "The Azim Steppe", "Othard" },
            //Hingashi
            { "Kugane", "Hingashi" },
            { "Shirogane", "Hingashi" },
            //Norvrandt
            { "The Crystarium", "Norvrandt" },
            { "Eulmore", "Norvrandt" },
            { "Lakeland", "Norvrandt" },
            { "Kholusia", "Norvrandt" },
            { "Amh Araeng", "Norvrandt" },
            { "Il Mheg", "Norvrandt" },
            { "The Rak'tika Greatwood", "Norvrandt" },
            { "The Tempest", "Norvrandt" },
            //The Northern Empty
            { "Old Sharlayan", "The Northern Empty" },
            { "Labyrinthos", "The Northern Empty" },
            //Ilsabard
            { "Radz-at-Han", "Ilsabard" },
            { "Thavnair", "Ilsabard" },
            { "Garlemald", "Ilsabard" },
            //The Sea of Stars
            { "Mare Lamentorum", "The Sea of Stars" },
            { "Ultima Thule", "The Sea of Stars" },
            //The World Unsundered
            { "Elpis", "The World Unsundered" },
            //Yok Tural
            { "Tuliyollal", "Yok Tural" },
            { "Urqopacha", "Yok Tural" },
            { "Kozama'uke", "Yok Tural" },
            { "Yak T'el", "Yok Tural" },
            //Xak Tural
            { "Solution Nine", "Xak Tural" },
            { "Shaaloani", "Xak Tural" },
            { "Heritage Found", "Xak Tural" },
            //Unlost World
            { "Living Memory", "Unlost World" },
            //The High Seas
            //{ "Galadion Bay", "The High Seas" },
            //{ "The Southern Straight or Merlthor", "The High Seas" },
            //{ "The Northern Straight or Merlthor", "The High Seas" },
            //{ "Rhotano Sea", "The High Seas" },
            //{ "The Cieldalaes", "The High Seas" },
            //{ "The Bloodbrine Sea", "The High Seas" },
            //{ "The Rothlyt Sound", "The High Seas" },
            //{ "The Siresong Sea", "The High Seas" },
            //{ "Kugane

        };
        public static String[] FishingRegions = [
            "La Noscea",
            "The Black Shroud",
            "Thanalan",
            "Coerthas",
            "Mor Dhona",
            "Abalathia's Spine",
            "Dravania",
            "Gyr Abania",
            "Othard",
            "Hingashi",
            "Norvrandt",
            "The Northern Empty",
            "Ilsabard",
            "The Sea of Stars",
            "The World Unsundered",
            "Yok Tural",
            "Xak Tural",
            "Unlost World"
            ];



        public static String[] FishingHoles = [//La Noscea
            "Limsa Lominsa Upper Decks",
            "Limsa Lominsa Lower Decks",
            "Middle La Noscea",
            "Lower La Noscea",
            "Eastern La Noscea",
            "Western La Noscea",
            "Upper La Noscea",
            "Outer La Noscea",
            "Mist",
            //The Black Shroud,
            "New Gridania",
            "Old Gridania",
            "Central Shroud",
            "East Shroud",
            "South Shroud",
            "North Shroud",
            "The Lavender Beds",
            //Thanalan
            "Western Thanalan",
            "Central Thanalan",
            "Eastern Thanalan",
            "Southern Thanalan",
            "Northern Thanalan",
            "The Goblet",
            //Coerthas
            "Coerthas Central Highlands",
            "Coerthas Western Highlands",
            //Mor Dhona
            "Mor Dhona",
            //Abalathia's Spine
            "The Sea of Clouds",
            "Azys Lla",
            //Dravania
            "The Dravanian Forelands",
            "The Dravanian Hinterlands",
            "The Churning Mists",
            //Gyr Abania
            "Rhalgr's Reach",
            "The Fringes",
            "The Peaks",
            "The Lochs",
            //Othard
            "The Ruby Sea",
            "Yanxia",
            "The Azim Steppe",
            //Hingashi
            "Kugane",
            "Shirogane",
            //Norvrandt
            "The Crystarium",
            "Eulmore",
            "Lakeland",
            "Kholusia",
            "Amh Araeng",
            "Il Mheg",
            "The Rak'tika Greatwood",
            "The Tempest",
            //The Northern Empty
            "Old Sharlayan",
            "Labyrinthos",
            //Ilsabard
            "Radz-at-Han",
            "Thavnair",
            "Garlemald",
            //The Sea of Stars
            "Mare Lamentorum",
            "Ultima Thule",
            //The World Unsundered
            "Elpis",
            //Yok Tural
            "Tuliyollal",
            "Urqopacha",
            "Kozama'uke",
            "Yak T'el",
            //Xak Tural
            "Solution Nine",
            "Shaaloani",
            "Heritage Found",
            //Unlost World
            "Living Memory"];

        public static readonly Dictionary<string, Region> Regions = [];
        public static readonly Dictionary<string, FishData> FishData = [];
        public static readonly Dictionary<string, int> FateData = [];
        public static readonly Dictionary<string, int> HuntData = [];
        public static readonly Dictionary<string, string> HuntRankData = [];

        public static Dictionary<string, Dictionary<string, string>> ObsoleteChecks { get; private set; } = [];

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
                var zone = row[2];
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
                var intuition = fish.Value<JObject>("logical_intuition");
                var holes = fish.Value<JArray>("holes");
                List<string> zoneNames = [];
                List<string> baits = [];
                List<string> intuitionbaits = [];
                List<string> holenames = [];
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
                if (holes != null)
                    holenames = holes.Where(h => h != null).Select(t => t.ToString()).ToList();
                var data = new FishData
                {
                    Level = (int)Math.Floor(fish.Value<int>("lvl") / 5.0) * 5,
                    Id = fish.Value<int>("id"),
                    Baits = [.. baits],
                    Intuition = [.. intuitionbaits],
                    Regions = zoneNames.Select(z => Regions[z]).ToArray(),
                    Holes = holenames.ToArray(),
                };
                APData.FishData[fish.Value<string>("name")] = data;

            }
        }
    }
}
