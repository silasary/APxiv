using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ArchepelegoXIV.Rando
{
    internal static class RegionContainer
    {
        public static Dictionary<string, string> Aliases = new() {
            // Cities
            { "Limsa Lominsa Lower Decks", "Limsa Lominsa"},
            { "Limsa Lominsa Upper Decks", "Limsa Lominsa"},
            // Dungeons
            { "Satasha", "Western La Noscea" },
            { "The Tam-Tara Deepcroft", "Central Shroud" },
            { "Copperbell Mines", "Western Thanalan" },
            { "Halatali", "Eastern Thanalan" },
            { "The Thousand Maws of Toto-Rak", "South Shroud" },
            { "Haukke Manor", "Central Shroud" },
            { "Brayflox’s Longstop", "Eastern La Noscea" },
            { "The Sunken Temple of Qarn", "Southern Thanalan" },
            { "Cutter's Cry", "Central Thanalan" },
            { "The Stone Vigil", "Coerthas Central Highlands" },
            { "Dzemael Darkhold", "Coerthas Central Highlands" },
            { "The Aurum Vale", "Coerthas Central Highlands" },
            { "Castrum Meridianum", "Northern Thanalan" },
            { "The Praetorium", "Northern Thanalan" },
            { "The Dying Gasp", "The Tempest" },
            { "The Heroes’ Gauntlet", "Eulmore" },
            { "Akademia Anyder", "The Tempest" },
            { "Anamnesis Anyder", "Kholusia" },
            { "Paglth’an", "Ul'dah" },
            { "Matoya’s Relict", "The Dravanian Hinterlands" },
            { "Amaurot", "The Tempest" },
            { "The Twinning", "The Crystarium" },
            { "The Qitana Ravel", "The Rak'tika Greatwood" },
            { "Dhon Mheg", "Il Mheg" },
            { "Holminster Switch", "Lakeland" },
            // Trials
            { "The Dancing Plague", "Il Mheg" },
            { "The Crown of the Immaculate", "Kholusia" },
            { "Cinder Drift", "The Lochs" },
            { "Castrum Marinum", "Western Thanalan" },
            { "The Cloud Deck", "The Lochs" },


        };

        public static Dictionary<string, Region> Regions = [];
        private static readonly Region Menu;

        static RegionContainer()
        {
            Menu = new Region("Menu", ["Limsa Lominsa", "Gridania", "Ul'dah", "Ishgard"]);
            LoadJson();
        }

        public static void LoadJson()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArchepelegoXIV.regions.json");
            using var reader = new StreamReader(stream);
            var regions = JObject.Parse(reader.ReadToEnd());
            foreach (var region in regions)
            {
                var connections = new List<string>();
                connections.AddRange(region.Value["connects_to"]?.ToObject<List<string>>() ?? []);

                var rule = Logic.Always();
                var requires = region.Value.Value<string>("requires");
                if (requires != null)
                    rule = Logic.HasItem(requires);
                _ = new Region(region.Key, connections.ToArray() ?? [], rule);
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

        internal static bool CanReach(ApState apState, string name)
        {
            if (Aliases.TryGetValue(name, out var alias))
            {
                name = alias;
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
