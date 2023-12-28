using Archipelago.MultiClient.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelegoXIV.Rando
{
    internal class NGPlusGame(ApState apState) : BaseGame(apState)
    {
        private readonly string[] Jobs = ["PLD", "WAR", "DRK", "GNB", "WHM", "SCH", "AST", "SGE", "MNK", "DRG", "NIN", "SAM", "RPR", "BRD", "MCH", "DNC", "BLM", "SMN", "RDM", "BLU"];

        public override string Name => "Manual_FFXIV_Silasary";

        public override int MaxLevel() => Jobs.Max(MaxLevel);

        public override int MaxLevel(string job) => apState.Items.Count(i => i == $"5 {job} Levels") * 5;

        internal override void ProcessItem(NetworkItem item, string itemName)
        {
            base.ProcessItem(item, itemName);
            if (itemName.EndsWith("Levels"))
            {
                var words = itemName.Split(' ');
                var job = Data.ClassJobs.First(j => j.Abbreviation == words[1]);
                Levels[job] = MaxLevel(job);
            }
        }
    }
}
