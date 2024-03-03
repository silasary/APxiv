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
        private static readonly Region Menu;

        static RegionContainer()
        {
            Menu = new Region("Menu", ["Limsa Lominsa", "Gridania", "Ul'dah", "Ishgard"]);
            APData.LoadJson();
            APData.LoadDutiesCsv();
            APData.LoadFatesCsv();
        }

        internal static void MarkStale()
        {
            foreach (var region in APData.Regions.Values) {
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
                        region.Reachable = region.MeetsRequirements(ap, false);
                        region.stale = false;
                    }
                    if (region == target)
                        return region.Reachable;

                    if (!region.Reachable)
                        continue;

                    region.Connections ??= region._connections.Select(n => APData.Regions.TryGetValue(n, out var r) ? r : null).OfType<Region>().ToArray();
                    foreach (var conn in region.Connections)
                        if (!explored.Contains(conn))
                            queue.Enqueue(conn);
                }
                return false;
            }

            return target.Reachable;
        }

        internal static bool CanReach(ApState apState, string name, ushort territoryId = 0, Location? location = null)
        {
            name = LocationToRegion(name, territoryId);
            if (!APData.Regions.TryGetValue(name, out var value))
            {
                DalamudApi.SetStatusBar($"Unknown Location {name}");
                DalamudApi.Echo($"Unknown Location {name}");
                return false;
            }
            if (location != null)
                location.region = value;
            return CanReach(apState, value);
        }

        public static string LocationToRegion(string name, ushort territoryId = 0)
        {
            if (APData.Aliases.TryGetValue(name, out var alias))
            {
                name = alias;
            }

            if (!APData.Regions.ContainsKey(name) && territoryId > 0)
            {
                var duty = Data.GetDuty(territoryId);
                if (duty != null)
                {
                    name = duty.Name;
                    if (name.StartsWith("the"))
                        name = "The" + name[3..];
                    if (APData.Aliases.TryGetValue(name, out alias))
                        name = alias;
                }
            }
            if (name.StartsWith("Ocean Fishing:"))
                name = "Ocean Fishing";
            return name;
        }
    }

    public class Region
    {
        public string Name;
        public Func<ApState, bool, bool> MeetsRequirements;
        public Region[]? Connections = null;
        public string[] _connections;

        internal bool stale;
        internal bool Reachable;

        public Region(string name, string[] connections, Func<ApState, bool, bool>? requirements = null)
        {
            APData.Regions.Add(name, this);
            Name = name;
            this.stale = true;
            this._connections = connections;
            this.MeetsRequirements = requirements ?? Logic.Always();
        }
    }
}
