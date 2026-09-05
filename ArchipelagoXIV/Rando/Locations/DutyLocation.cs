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
                    return Name + $" ×{SubLocations.Length + 1}";
                }
                return Name;
            }
        }

        public override void Complete()
        {
            base.Complete();
            foreach (var sub in this.SubLocations)
            {
                sub.Complete();
            }
        }
    }

    internal class DutySubLocation : Location
    {
        public DutyLocation? parent;
        public string DutyName { get; }

        public DutySubLocation(ApState apState, long id, string name, ContentFinderCondition content) : base(apState, id, name)
        {
            this.Content = content;
            this.DutyName = Regexes.ExtraCheckName.Match(Name).Groups[1].Value;
        }


        public DutyLocation GetParent()
        {
            if (parent == null)
            {
                parent = apState.AllLocations?.FirstOrDefault(loc => loc.Name == DutyName) as DutyLocation;
                parent?.SubLocations = [.. parent.SubLocations, this];
            }
            return parent!;
        }

        internal override void SetRequirements()
        {
            var parent = GetParent();
            if (parent != null && parent.MeetsRequirements == null)
            {
                parent.SetRequirements();
            }

            MeetsRequirements = parent?.MeetsRequirements;
            if (MeetsRequirements == null && Content.RowId != 0)
            {
                MeetsRequirements = Logic.Level(Content.ClassJobLevelRequired);
            }
        }
    }
}
