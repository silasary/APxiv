using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArchipelagoXIV.Rando.Locations
{
    internal class FateLocation : Location
    {
        private string serverName;

        public FateLocation(ApState apState, long id, string name, Fate fate)
            : base(apState, id, name)
        {
            serverName = name;
            var fatetype = "";
            if (name.EndsWith(" (FATE)"))
                fatetype = " (FATE)";
            else if (name.EndsWith(" (FETE)"))
                fatetype = " (FETE)";
            else if (name.EndsWith(" (GATE)"))
                fatetype = " (GATE)";
            Name = fate.Name.ToString().Trim() + fatetype;
        }

        protected override void SetRequirements()
        {
            if (Name.EndsWith(" (FATE)"))
            {
                if (APData.FateData.TryGetValue(Name, out var fateLevel))
                    MeetsRequirements = Logic.Level(fateLevel);
                else if (APData.FateData.TryGetValue(serverName, out fateLevel))
                    MeetsRequirements = Logic.Level(fateLevel);
                else
                {
                    DalamudApi.Echo($"Could not find fate level for {Name}");
                    MeetsRequirements = Logic.Always();
                }
            }
            else if (Name.EndsWith(" (FETE)"))
            {
                MeetsRequirements = Logic.LevelDOHDOL(APData.FateData[Name]);
            }
            else if (Name.EndsWith(" (GATE)"))
            {
                MeetsRequirements = Logic.Always();
            }
        }
    }
}
