using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArchipelegoXIV.Rando
{
    public class Location
    {
        public Location(ApState apState, long id)
        {
            this.apState = apState;
            ApId = id;
            Name = apState.session.Locations.GetLocationNameFromId(id);
            if (Data.DutyAliases.TryGetValue(Name, out var value))
                Name = value;
        }

        public string Name;
        private readonly ApState apState;
        public long ApId;

        public bool Accessible;

        public bool Completed { get; private set; }

        internal bool stale = true;

        public Func<ApState, bool, bool>? MeetsRequirements = null;

        private ContentFinderCondition? content;

        public bool IsAccessible()
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
                    if (content == null && this.Name.StartsWith("The"))
                        content = Data.Content.FirstOrDefault(cf => cf.Name == ("the" + this.Name[3..]));
                    if (content == null)
                    {
                        var de = Data.DynamicEvents.FirstOrDefault(de => de.Name == this.Name);
                        // Note: Currently, these are all Bozja.  This may change with DT's Field Content
                        if (de != null)
                            MeetsRequirements = Logic.Level(80);
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
            if (content != null)
            {
                this.MeetsRequirements = Logic.Level(content.ClassJobLevelRequired);
            }
            else if (Regexes.FATE.Match(this.Name) is Match m && m.Success && m.Groups[1].Success && !string.IsNullOrEmpty(m.Groups[1].Value))
            {
                this.MeetsRequirements = Logic.Level(Data.FateLevels[m.Groups[1].Value]);
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
            else if (Name.EndsWith(" (FATE)") || Name.EndsWith(" (GATE)") || Name.EndsWith(" (FETE)"))
            {
                this.MeetsRequirements = Logic.Always();
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
            
            if (!MeetsRequirements(apState, true))
            {
                return false;
            }
            return true;
        }

        public void Complete()
        {
            this.Accessible = false;
            this.Completed = true;
            apState.session.Locations.CompleteLocationChecksAsync(this.ApId).ContinueWith((_) => apState.RefreshLocations(true)).Start();
        }
    }
}
