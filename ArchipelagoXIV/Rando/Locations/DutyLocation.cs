using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArchipelagoXIV.Rando.Locations
{
    internal class DutyLocation : Location
    {
        public Location[] SubLocations = [];

        public DutyLocation(ApState apState, long id, string name, ContentFinderCondition content) : base(apState, id, name)
        {
            this.Content = content;
        }

        public override string DisplayText {
            get
            {
                if (SubLocations.Length > 0)
                {
                    return Name + $" ×{SubLocations.Length + 1}" + HintText;
                }
                return Name + HintText;
            }
        }
    }

    internal class DutySubLocation : Location
    {
        public DutyLocation? parent;
        public DutySubLocation(ApState apState, long id, string name, ContentFinderCondition content) : base(apState, id, name)
        {
            this.Content = content;
        }

        public DutyLocation GetParent()
        {
            if (parent == null)
            {
                var dutyname = Regexes.ExtraCheckName.Match(Name).Groups[1].Value;
                parent = apState.AllLocations?.FirstOrDefault(loc => loc.Name == dutyname) as DutyLocation;
            }
            return parent!;
        }

        protected override void SetRequirements()
        {
            var dutyname = Regexes.ExtraCheckName.Match(Name).Groups[1].Value;
            var parent = apState.AllLocations.FirstOrDefault(loc => loc.Name == dutyname);
            if (parent is DutyLocation dutyParent)
            {
                dutyParent.SubLocations = [.. dutyParent.SubLocations, this];
                this.parent = dutyParent;
            }
            MeetsRequirements = parent?.MeetsRequirements ?? Logic.Always();
        }
    }
}
