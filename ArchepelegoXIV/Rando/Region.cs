using System;
using System.Collections.Generic;
using System.Linq;

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

        public static Dictionary<string, Region> Regions;
        private static readonly Region Menu;
        private static readonly Region Limsa;
        private static readonly Region Gridania;
        private static readonly Region NorthShroud;
        private static readonly Region SouthShroud;
        private static readonly Region Uldah;
        private static readonly Region MaskedCarnivale;
        private static readonly Region WesternThanalan;
        private static readonly Region Fringes;
        private static readonly Region Lochs;
        private static readonly Region Peaks;
        private static readonly Region Crystarium;
        private static readonly Region Lakeland;
        private static readonly Region Kholusia;
        private static readonly Region Eulmore;
        private static readonly Region AmhAraeng;
        private static readonly Region IlMheg;
        private static readonly Region Tempest;

        static RegionContainer()
        {
            Regions = [];
            Menu = new Region("Menu", ["Limsa Lominsa", "Gridania", "Ul'dah", "Ishgard"]);
            Limsa = new Region("Limsa Lominsa", ["Lower La Noscea", "Middle La Noscea", "Western La Noscea", "Eastern La Noscea", "Western Thanalan", "Kugane"], Logic.HasItem("Limsa Lominsa and Middle La Noscea Access"));

            // Shroud
            Gridania = new Region("Gridania", ["Central Shroud", "East Shroud", "North Shroud"], Logic.HasItem("Gridania and Central Shroud Access"));
            NorthShroud = new Region("North Shroud", ["Central Shroud", "Gridania", "Coerthas Central Highlands"], Logic.HasItem("North Shroud Access"));
            SouthShroud = new Region("South Shroud", ["Central Shroud", "East Shroud", "Eastern Thanalan"], Logic.HasItem("South Shroud Access"));

            // Thanalan
            Uldah = new Region("Ul'dah", ["Western Thanalan", "Central Thanalan", "Masked Carnivale"], Logic.HasItem("Ul'dah and Central Thanalan Access"));
            MaskedCarnivale = new Region("Masked Carnivale", [], Logic.Level(50));
            WesternThanalan = new Region("Western Thanalan", ["Ul'dah", "Central Thanalan", "Limsa Lominsa"], Logic.HasItem("Western Thanalan Access"));

            // Gyr Abania
            Fringes = new Region("The Fringes", ["Rhalgr's Reach", "The Peaks"], Logic.HasItem("The Fringes Access"));
            Peaks = new Region("The Peaks", ["Rhalgr's Reach", "The Fringes", "The Lochs"], Logic.HasItem("The Peaks Access"));
            Lochs = new Region("The Lochs", [], Logic.HasItem("The Lochs Access"));

            // First
            Crystarium = new Region("The Crystarium", ["Lakeland", "Kholusia", "Eulmore"], Logic.HasItem("The Crystarium Access"));
            Lakeland = new Region("Lakeland", ["Il Mheg", "Amh Araeng", "The Crystarium", "The Rak'tika Greatwood"], Logic.HasItem("Lakeland Access"));
            Kholusia = new Region("Kholusia", ["The Tempest"], Logic.HasItem("Kholusia Access"));
            Eulmore = new Region("Eulmore", [], Logic.HasItem("Eulmore Access"));
            AmhAraeng = new Region("Amh Araeng", [], Logic.HasItem("Amh Araeng Access"));
            IlMheg = new Region("Il Mheg", [], Logic.HasItem("Il Mheg Access"));
            Tempest = new Region("The Tempest", [], Logic.HasItem("The Tempest Access"));
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
