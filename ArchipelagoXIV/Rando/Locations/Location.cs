using Archipelago.MultiClient.Net.Models;
using ArchipelagoXIV.Rando.Locations;
using Lumina.Excel.Sheets;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArchipelagoXIV.Rando
{
    public class Location
    {
        public static Location Create(ApState apState, long id)
        {
            var name = apState.session.Locations.GetLocationNameFromId(id);
            if (APData.ObsoleteChecks.ContainsKey(name))
                return new ObsoleteLocation(apState, id, name);
            if (APData.FishData.ContainsKey(name))
                return new Fish(apState, id, name);
            return new Location(apState, id, name);
        }
        public Location(ApState apState, long id, string name)
        {
            this.apState = apState;
            ApId = id;
            Name = name;
            if (Data.DutyAliases.TryGetValue(Name, out var value))
                Name = value;
            Level = 0;
        }

        public readonly string Name;
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
            if (Completed)
                return false;

            if (stale)
            {
                stale = false;
                var allMissingLocations = apState?.session?.Locations?.AllMissingLocations;
                if (allMissingLocations == null)
                    return Accessible = false;
                if (!allMissingLocations.Contains(ApId))
                    return Accessible = false;
                if (!apState?.Game?.MeetsRequirements(this) ?? false)
                    return Accessible = false;
                if (MeetsRequirements == null)
                {
                    content = Data.Content.FirstOrDefault(cf => cf.Name == this.Name);
                    if (content.RowId == 0 && this.Name.StartsWith("The"))
                        content = Data.Content.FirstOrDefault(cf => cf.Name == ("the" + this.Name[3..]));
                    if (content.RowId == 0 && APData.CheckNameToContentID.TryGetValue(this.Name, out var id))
                    {
                        content = Data.Content[id];
                    }
                    if (content.RowId == 0)
                    {
                        var de = Data.DynamicEvents.FirstOrDefault(de => de.Name == this.Name);
                        // Note: Currently, these are all Bozja.  This may change with DT's Field Content
                        if (de.RowId > 0)
                        {
                            this.Level = 80;
                            MeetsRequirements = Logic.Level(80);
                        }
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

        private void SetRequirements()
        {
            if (content.RowId > 0)
            {
                this.MeetsRequirements = Logic.Level(content.ClassJobLevelRequired);
            }
            else if (Regexes.FATE.Match(this.Name) is Match m && m.Success && m.Groups[1].Success && !string.IsNullOrEmpty(m.Groups[1].Value) && Data.FateLevels.TryGetValue(m.Groups[1].Value, out var level))
            {
                this.MeetsRequirements = Logic.Level(level);
            }
            else if (Name.StartsWith("Masked Carnivale #"))
            {
                m = Regexes.Carnivale.Match(this.Name);
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
            else if (Name.EndsWith(" (FATE)"))
            {
                this.MeetsRequirements = Logic.Level(APData.FateData[Name]);
            }
            else if (Name.EndsWith(" (FETE)"))
            {
                this.MeetsRequirements = Logic.LevelDOHDOL(APData.FateData[Name]);
            }
            else if (Name.EndsWith(" (GATE)"))
            {
                this.MeetsRequirements = Logic.Always();
            }
            else if (Name == "Return to the Waking Sands")
            {
                this.MeetsRequirements = Logic.Always();
            }
            else if (Level > 0)
            {
                this.MeetsRequirements = Logic.Level(Level);
            }
            else if (Name.StartsWith("Ocean Fishing"))
            {
                if (Name == "Ocean Fishing: Ruby Sea" || Name == "Ocean Fishing: One River")
                    this.MeetsRequirements = Logic.FromString("|5 FSH Levels:12| and |Kugane Access:1|");
                else
                    this.MeetsRequirements = Logic.Level(5, "FSH");
            }
            else
            {
                DalamudApi.Echo($"Unknown CF {Name}");
                this.MeetsRequirements = Logic.Always();
            }
        }

        public bool CanClearAsCurrentClass()
        {
            if (!IsAccessible())
                return false;

            if (!MeetsRequirements(apState, apState.ApplyClassRestrictions))
            {
                return false;
                DalamudApi.Echo("Warning:  Class check failed");
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
                DalamudApi.Echo("Warning:  Class check failed");
            }
            return true;
        }

        public void Complete()
        {
            this.Completed = true;
            apState.localsave!.CompletedChecks.Add(this.ApId);
            Task.Factory.StartNew(CompleteAsync);
            apState.UpdateBars();
        }
        private async void CompleteAsync()
        {
            DalamudApi.PluginLog.Information($"Marking {Name} ({ApId}) as complete");
            apState.SaveCache();
            apState.session!.Locations.CompleteLocationChecks(this.ApId);
        }

        public string DisplayText
        {
            get => Name + HintText;
        }

        public string HintText { get
            {
                if (this.HintedItem != null)
                {
                    var p = this.HintedItem.ReceivingPlayerName(apState);
                    var i = this.HintedItem.ItemName(apState);
                    return $" (Contains {p}'s {i})";
                }

                return "";
            }
        }
    }
}
