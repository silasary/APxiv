using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoXIV.Rando.Locations
{
    internal class ObsoleteLocation : Location
    {
        public ObsoleteLocation(ApState apState, long id, string name) : base(apState, id, name)
        {
            var data = APData.ObsoleteChecks[name];
            this.region = APData.Regions[data["region"]];
            if (data.TryGetValue("level", out var level))
                this.Level = int.Parse(level);
            if (data.TryGetValue("requires", out var requires))
                this.MeetsRequirements = Logic.FromString(requires);
            DalamudApi.Echo($"Your world contains the impossible check {name}");
        }

        public override bool IsAccessible()
        {
            var accessible =  base.IsAccessible();
            if (accessible)
                this.Complete();
            return accessible;
        }
    }
}
