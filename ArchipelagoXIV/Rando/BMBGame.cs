using Archipelago.MultiClient.Net.Models;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArchipelagoXIV.Rando
{
    internal partial class BMBGame(ApState apState) : BaseGame(apState)
    {
        private const string EquipLevels = "10 Equip Levels";
        private readonly ClassJob BLU = Data.ClassJobs.First(j => j.Abbreviation == "BLU");

        public override string Name => "Manual_FFXIVBMB_Pizzie";

        public override int MaxLevel() => apState.Items.Count(i => i == EquipLevels) * 10;

        public override int MaxLevel(string job)
        {
            if (job == "BLU")
                return MaxLevel();
            return 0;
        }

        internal override void ProcessItem(NetworkItem item, string itemName)
        {
            base.ProcessItem(item, itemName);
            if (itemName == EquipLevels)
                Levels[BLU] = MaxLevel();
        }
    }
}
