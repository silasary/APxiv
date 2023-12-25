using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ArchipelegoXIV.Rando
{
    internal static class RegionContainer
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
            { "Flame Barracks", "Uldah"},
            { "Foundation", "Ishgard"},
            { "The Pillars", "Ishgard"},
            { "Idyllshire", "The Dravnian Hinterlands"}, 
            // Inns
            { "Location Mizzenmast Inn", "Limsa Lominsa"},
            { "The Roost", "Gridania"}, 
            { "The Hourglass", "Ul'dah"},
            { "Cloud Nine", "Ishgard"}, 
            { "Bokairo Inn", "Kugane"}, 
            { "Andron", "Old Sharlayan"}, 
            // Player Housing
            { "The Goblet", "Uldah"},
            { "Mist", "Limsa Lominsa"},
            { "The Lavender Beds", "Gridania"},
            { "Shirogane", "Kugane"},
            { "Empyreum", "Ishgard"},
            // Gold Saucer
            { "The Gold Saucer", "Southern Thanalan" },
            { "Chocobo Square", "Southern Thanalan"},
            // Instanced Zone
            { "The Waking Sands", "Western Thanalan"},
            { "Fortemps Manor", "Ishgard"},
            { "Matoya's Cave", "The Dravnian Hinterlands"}, 
            { "The Lightfeather Proving Grounds", "Ishgard"},
            { "Ruby Bazaar Offices", "Kugane"},
            { "The Doman Enclave", "Yanxia" },
            { "The Omphalos", "Mor Dhona"  },
            { "Main Hall", "Old Sharlayan" },
        };

        public static Dictionary<string, Region> Regions = [];
        private static readonly Region Menu;

        static RegionContainer()
        {
            Menu = new Region("Menu", ["Limsa Lominsa", "Gridania", "Ul'dah", "Ishgard"]);
            LoadJson();
            LoadCsv();
        }

        public static void LoadJson()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArchipelegoXIV.regions.json");
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

        public static void LoadCsv()
        {
            string[] headers = ["", "Name", "ARR", "HW", "STB", "SHB", "EW"];
            // TODO: Load duties.csv, and populate Aliases with Dungeon Location -> Entrance Location.
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArchipelegoXIV.duties.csv");
            using var reader = new StreamReader(stream);
            string? line = null;
            while ((line = reader.ReadLine()) != null)
            {
                var row = line.Split(',');
                if (headers.Contains(row[0]))
                    continue;
                Aliases[row[0]] = row[4];
            }
        }

        internal static void MarkStale()
        {
            foreach (var region in Regions.Values) {
                region.stale = true;
            }
        }

        public static bool CanReach(ApState ap, Region target)
        {
            if (target.stale)
            {
                var explored = new List<Region>();
                var queue = new Queue<Region>();
                queue.Enqueue(Menu);
                while (queue.Count > 0)
                {
                    var region = queue.Dequeue();
                    explored.Add(region);
                    if (region.stale)
                    {
                        region.Reachable = region.MeetsRequirements(ap);
                        region.stale = false;
                    }
                    if (region == target)
                        return region.Reachable;

                    if (!region.Reachable)
                        continue;

                    region.Connections ??= region._connections.Select(n => Regions.TryGetValue(n, out var r) ? r : null).OfType<Region>().ToArray();
                    foreach (var conn in region.Connections)
                        if (!explored.Contains(conn))
                            queue.Enqueue(conn);
                }
                return false;
            }

            return target.Reachable;
        }

        internal static bool CanReach(ApState apState, string name, ushort territoryId = 0)
        {
            if (Aliases.TryGetValue(name, out var alias))
            {
                name = alias;
            }

            if (!Regions.ContainsKey(name) && territoryId > 0)
            {
                var duty = Data.GetDuty(territoryId);
                if (duty != null)
                {
                    name = duty.Name;
                    if (name.StartsWith("the"))
                        name = "The" + name[3..];
                    if (Aliases.TryGetValue(name, out alias))
                        name = alias;
                }
            }
            if (!Regions.TryGetValue(name, out var value))
            { 
                DalamudApi.SetStatusBar($"Unknown Location {name}");
                DalamudApi.Echo($"Unknown Location {name}");
                return false;
            }
            return CanReach(apState, value);
        }
    }

    internal class Region
    {
        public string Name;
        public Func<ApState, bool> MeetsRequirements;
        public Region[]? Connections = null;
        public string[] _connections;

        internal bool stale;
        internal bool Reachable;

        public Region(string name, string[] connections, Func<ApState, bool> requirements = null)
        {
            RegionContainer.Regions.Add(name, this);
            Name = name;
            this.stale = true;
            this._connections = connections;
            this.MeetsRequirements = requirements ?? ((ApState state) => true);
        }
    }
}
