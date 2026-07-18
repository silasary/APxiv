using Archipelago.MultiClient.Net.Models;
using Lumina.Excel.Sheets;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArchipelagoXIV.Rando.Locations
{
    public class Location
    {
        public static Location Create(ApState apState, long id)
        {
            var name = apState.session.Locations.GetLocationNameFromId(id);
            if (Data.DutyAliases.TryGetValue(name, out var value))
                name = value;

            if (APData.ObsoleteChecks.ContainsKey(name))
                return new ObsoleteLocation(apState, id, name);
            if (APData.FishData.ContainsKey(name))
                return new Fish(apState, id, name);
            if (name.StartsWith("Attune "))
                return new AttuneLocation(apState, id, name);
            if (Data.DynamicEvents.ContainsKey(name))
                return new CriticalEncounterLocation(apState, id, name);
            if (Data.FateTable.TryGetValue(name.Replace(" (FATE)", "").Replace(",", "").Trim('"').Trim().ToString().ToLower(), out var fate))
            {
                return new FateLocation(apState, id, name, fate);
            }

            return new Location(apState, id, name);
        }

        public Location(ApState apState, long id, string name)
        {
            this.apState = apState;
            ApId = id;
            Name = name;

            Level = 0;
        }

        public string Name { get; protected set; }
        protected readonly ApState apState;
        public readonly long ApId;
        public int Level;
        public Region region = null;

        public bool Accessible;

        public bool Completed { get; protected set; }

        internal bool stale = true;

        public Func<ApState, bool, bool>? MeetsRequirements = null;

        private ContentFinderCondition content;
        public Hint? HintedItem { get; set; } = null;

        public virtual bool IsAccessible()
        {
            if (stale)
            {
                stale = false;
                var allLocations = apState?.session?.Locations?.AllLocations;
                if (allLocations == null)
                    return Accessible = false;
                if (!allLocations.Contains(ApId))
                    return Accessible = false;
                if (!apState?.Game?.MeetsRequirements(this) ?? false)
                    return Accessible = false;
                if (MeetsRequirements == null)
                {
                    content = Data.Content.FirstOrDefault(cf => cf.Name == Name);
                    if (content.RowId == 0 && Name.StartsWith("The"))
                        content = Data.Content.FirstOrDefault(cf => cf.Name.ExtractText() == "the" + Name[3..]);
                    if (content.RowId == 0)
                    {
                        // Bonus checks come in the form "Sastasha 2"
                        var match = Regexes.ExtraCheckName.Match(Name);
                        if (match.Success && match.Groups.Count > 1)
                            content = Data.Content.FirstOrDefault(cf => cf.Name.ExtractText() == match.Groups[1].Value);
                    }
                    if (content.RowId == 0 && APData.CheckNameToContentID.TryGetValue(Name, out var id))
                    {
                        content = Data.Content[id];
                    }
                    if (MeetsRequirements == null)
                        SetRequirements();
                }
                if (!MeetsRequirements(apState, false))
                    return Accessible = false;
                return Accessible = true;
            }

            return Accessible;
        }

        protected virtual void SetRequirements()
        {
            if (content.RowId > 0)
            {
                MeetsRequirements = Logic.Level(content.ClassJobLevelRequired);
            }
            else if (Regexes.FATE.Match(Name) is Match m && m.Success && m.Groups[1].Success && !string.IsNullOrEmpty(m.Groups[1].Value) && Data.FateLevels.TryGetValue(m.Groups[1].Value, out var level))
            {
                MeetsRequirements = Logic.Level(level);
            }
            else if (Name.StartsWith("Masked Carnivale #"))
            {
                m = Regexes.Carnivale.Match(Name);
                var stage = int.Parse(m.Groups[1].Value);
                if (stage <= 25)
                    MeetsRequirements = Logic.Level(50, "BLU");
                else if (stage <= 30)
                    MeetsRequirements = Logic.Level(60, "BLU");
                else if (stage == 31)
                    MeetsRequirements = Logic.Level(70, "BLU");
                else if (stage == 32)
                    MeetsRequirements = Logic.Level(80, "BLU");
                else
                    DalamudApi.Echo($"Unknown stage {Name}");
            }
            else if (Name.EndsWith(" (GATE)"))
            {
                MeetsRequirements = Logic.Always();
            }
            else if (APData.HuntData.TryGetValue(Name, out var huntLevel))
            {
                MeetsRequirements = Logic.Level(huntLevel);
            }
            else if (Name == "Return to the Waking Sands")
            {
                MeetsRequirements = Logic.Always();
            }
            else if (Level > 0)
            {
                MeetsRequirements = Logic.Level(Level);
            }
            else if (Name.StartsWith("Ocean Fishing"))
            {
                if (Name == "Ocean Fishing: Ruby Sea" || Name == "Ocean Fishing: One River" ||Name == "Ocean Fishing: Thavnairian Coast")
                    MeetsRequirements = Logic.And(Logic.Level(60, "FSH"), Logic.HasItem("Kugane Access"));
                else
                    MeetsRequirements = Logic.Level(5, "FSH");
            }
            else
            {
                DalamudApi.Echo($"Could not identify the check `{Name}`");
                MeetsRequirements = Logic.Always();
            }
        }

        public bool CanClearAsCurrentClass()
        {
            if (!IsAccessible())
                return false;

            if (!MeetsRequirements(apState, apState.ApplyClassRestrictions))
            {
                return false;
            }
            return true;
        }

        public bool CanClearAsAnyClass()
        {
            if (!IsAccessible())
                return false;

            if (!MeetsRequirements(apState, false))
            {
                return false;
            }
            return true;
        }

        public void Complete()
        {
            DalamudApi.PluginLog.Information($"Marking {Name} ({ApId}) as complete");
            Completed = true;
            apState.localsave!.CompletedChecks.Add(ApId);
            apState.RefreshBars = true;
            apState.Syncing = true;
        }

        public string DisplayText
        {
            get
            {
                if (APData.HuntRankData.TryGetValue(Name, out var rank) && APData.Aliases.TryGetValue(Name, out var zone))
                    return $"{Name} ({rank}-Rank, {zone}){HintText}";
                return Name + HintText;
            }
        }

        public string HintText { get
            {
                if (HintedItem != null)
                {
                    var p = HintedItem.ReceivingPlayerName(apState);
                    var i = HintedItem.ItemName(apState);
                    return $" (Contains {p}'s {i})";
                }

                return "";
            }
        }
    }
}
