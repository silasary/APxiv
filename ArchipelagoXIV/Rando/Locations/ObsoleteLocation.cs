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
            DalamudApi.Echo($"Warning:  Your world contains the impossible check {name}. It will be automatically sent once it's in logic.");
        }

        public override bool IsAccessible()
        {
            var accessible =  base.IsAccessible();
            if (accessible)
            {
                apState.localsave.CompletedChecks.Add(ApId);
                this.Completed = true;
                apState.Syncing = true;
            }
            return accessible;
        }
    }
}
