using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArchipelagoXIV.Rando
{
    internal class NGPlusGame : BaseGame
    {
        public NGPlusGame(ApState apState, bool IsManual) : base(apState)
        {
            this.GoalCount = 50;
            isManual = IsManual;
        }

        private readonly string[] Jobs = ["PLD", "WAR", "DRK", "GNB", "WHM", "SCH", "AST", "SGE", "MNK", "DRG", "NIN", "SAM", "RPR", "BRD", "MCH", "DNC", "BLM", "SMN", "RDM", "BLU"];
        private readonly bool isManual;
        private long GoalCount;
        private long McGuffinCount;


        public override string Name => isManual ? "Manual_FFXIV_Silasary" : "Final Fantasy XIV";
        public long ExtraDungeonChecks { get; private set; } = 0;

        public override int MaxLevel() => Jobs.Max(MaxLevel);

        public override int MaxLevel(string job) => apState.Items.Count(i => i == $"5 {job} Levels") * 5;

        internal override void ProcessItem(ItemInfo item, string itemName)
        {
            base.ProcessItem(item, itemName);
            if (itemName.EndsWith("Levels"))
            {
                var words = itemName.Split(' ');
                var job = Data.ClassJobs.First(j => j.Abbreviation == words[1]);
                Levels[job] = MaxLevel(job);
            }
            
            if ((McGuffinCount = apState.Items.Count(i => i == "Memory of a Distant World")) >= GoalCount)
            {
                apState.CompleteGame();
            }
        }

        internal override void HandleSlotData(Dictionary<string, object> slotData)
        {
            base.HandleSlotData(slotData);
            this.GoalCount = (long)slotData["mcguffins_needed"];
            if (SlotData.TryGetValue("extra_dungeon_checks", out var extra_dungeon_checks))
                this.ExtraDungeonChecks = (long)extra_dungeon_checks;
            DalamudApi.Echo($"Goal is {GoalCount} Memories.");
        }

        internal override string GoalString()
        {
            return Goal switch
            {
                0 => $"{McGuffinCount}/{GoalCount} Memories of a Distant World recovered",
                _ => base.GoalString(),
            };
        }

        internal override VictoryType GoalType => Goal switch
        {
            0 => VictoryType.McGuffin,
            1 => VictoryType.DefeatShinryu,
            _ => VictoryType.McGuffin,
        };

        public override bool HasMapItems => true;

        internal override void Ready()
        {
            McGuffinCount = apState.Items.Count(i => i == "Memory of a Distant World");
        }
    }
}
