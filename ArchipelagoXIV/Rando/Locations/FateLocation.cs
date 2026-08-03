using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArchipelagoXIV.Rando.Locations
{
    enum FateType
    {
        FATE,
        FETE,
        GATE
    }
    internal class FateLocation : Location
    {
        private string serverName;
        private string fateName;
        private readonly FateType fateType;
        public uint FateID { get; }

        public FateLocation(ApState apState, long id, string name, Fate fate)
            : base(apState, id, name)
        {
            serverName = name;
            var fatetype = "";
            if (name.EndsWith(" (FATE)"))
            {
                fatetype = " (FATE)";
                this.fateType = FateType.FATE;
            }
            else if (name.EndsWith(" (FETE)"))
            {
                fatetype = " (FETE)";
                this.fateType = FateType.FETE;
            }
            else if (name.EndsWith(" (GATE)"))
            {
                fatetype = " (GATE)";
                this.fateType = FateType.GATE;
            }
            fateName = fate.Name.ExtractText().Trim();
            Name = fateName + fatetype;
            FateID = fate.RowId;
        }

        internal override void SetRequirements()
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

        override public string DisplayText
        {
            get
            {
                var fateTypeText = fateType switch
                {
                    FateType.FATE => "FATE)",
                    FateType.FETE => "FETE)",
                    FateType.GATE => "GATE)",
                    _ => ""
                };
                return $"{fateName} ({this.region.Name} {fateTypeText}";
            }
        }
    }
}
