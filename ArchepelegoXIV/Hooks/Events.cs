using Dalamud.Game.ClientState;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchepelegoXIV.Hooks
{
    internal class Events
    {
        private readonly ApState apState;

        public Events(ApState apState)
        {
            this.apState = apState;
        }

        public void Enable()
        {
            DalamudApi.DutyState.DutyCompleted += DutyState_DutyCompleted;
            DalamudApi.ClientState.TerritoryChanged += ClientState_TerritoryChanged;

            if (DalamudApi.ClientState.IsLoggedIn)
                ClientState_TerritoryChanged(DalamudApi.ClientState.TerritoryType);
        }

        public void Disable() { 
            DalamudApi.DutyState.DutyCompleted -= DutyState_DutyCompleted;
            DalamudApi.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        }

        private void DutyState_DutyCompleted(object? sender, ushort e)
        {
            var territory = apState.territory = Data.Territories.FirstOrDefault(row => row.RowId == e);
            DalamudApi.Echo($"{territory.Name} Completed");

        }

        private void ClientState_TerritoryChanged(ushort e)
        {
            var territory = apState.territory = Data.Territories.First(row => row.RowId == e);
            apState.territoryName = territory.PlaceName?.Value?.Name ?? "Unknown";
            apState.territoryRegion = territory.PlaceNameRegion?.Value?.Name ?? "Unknown";
        }
    }
}
