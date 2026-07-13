using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArchipelagoXIV.Rando.Locations
{
    internal class CriticalEncounterLocation : Location
    {
        private readonly DynamicEvent CriticalEncounter;

        public CriticalEncounterLocation(ApState apState, long id, string name) : base(apState, id, name)
        {
            this.CriticalEncounter = Data.DynamicEvents[name];
            if (CriticalEncounter.RowId > 32)
            {
                Level = 100;
            }
            else if (CriticalEncounter.RowId > 0)
            {
                Level = 80;
            }
            MeetsRequirements = Logic.Level(Level);
        }
    }
}
