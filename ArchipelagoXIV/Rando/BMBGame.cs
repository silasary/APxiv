using Archipelago.MultiClient.Net.Models;
using Lumina.Excel.Sheets;
using System;
using System.Linq;

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

        internal override void ProcessItem(ItemInfo item, string itemName)
        {
            base.ProcessItem(item, itemName);
            if (itemName == EquipLevels)
                Levels[BLU] = MaxLevel();
        }
    }
}
