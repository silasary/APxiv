using Dalamud.Game.ClientState;
using Dalamud.Game.DutyState;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchepelegoXIV
{
    public class ApState
    {
        public TerritoryType territory { get; internal set; }
        internal object territoryName;
        internal object territoryRegion;

        public bool CanTeleport { get; internal set; } = true;
        public bool CanReturn { get; internal set; } = true;

        public string DebugText
        {
            get
            {
                return $"{territory}\n{territoryName}\n{territoryRegion}\n\nHooked: {Hooked}";
            }
        }

        public bool Hooked { get; internal set; }
    }
}
