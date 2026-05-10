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
        private long BossKeyPiecesNeeded;
        private string BossKeyItemName = "";


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

            if (GoalType == VictoryType.McGuffin && (McGuffinCount = apState.Items.Count(i => i == "Memory of a Distant World")) >= GoalCount)
            {
                apState.CompleteGame();
            }
        }

        internal void OnGoalDutyCompleted()
        {
            apState.CompleteGame();
        }

        internal override void HandleSlotData(Dictionary<string, object> slotData)
        {
            base.HandleSlotData(slotData);
            this.GoalCount = (long)slotData["mcguffins_needed"];
            if (SlotData.TryGetValue("extra_dungeon_checks", out var extra_dungeon_checks))
                this.ExtraDungeonChecks = (long)extra_dungeon_checks;
            if (SlotData.TryGetValue("boss_key_pieces", out var boss_key_pieces))
                this.BossKeyPiecesNeeded = (long)boss_key_pieces;
            if (SlotData.TryGetValue("boss_key_item", out var boss_key_item))
                this.BossKeyItemName = boss_key_item as string ?? "";

            DalamudApi.Echo($"Goal is {GoalCount} Memories.");
        }

        internal override string GoalString()
        {
            if (Goal == 0)
                return $"{McGuffinCount}/{GoalCount} Memories of a Distant World recovered";

            var baseString = base.GoalString();

            if (BossKeyPiecesNeeded > 0 && !string.IsNullOrEmpty(BossKeyItemName))
            {
                var collected = apState.Items.Count(i => i == BossKeyItemName);

                if (BossKeyPiecesNeeded == 1)
                    return $"{baseString} [Key: {(collected >= 1 ? "obtained" : "missing")}]";

                return $"{baseString} [{collected}/{BossKeyPiecesNeeded} key pieces]";
            }

            return baseString;
        }

        internal string GoalDutyName => GoalType switch
        {
            VictoryType.DefeatUltimaWeapon => "The Porta Decumana",
            VictoryType.DefeatThordan => "The Singularity Reactor",
            VictoryType.DefeatNidhogg => "The Final Steps of Faith",
            VictoryType.DefeatShinryu => "The Royal Menagerie",
            VictoryType.DefeatTsukuyomi => "The Jade Stoa",
            VictoryType.DefeatHades => "The Dying Gasp",
            VictoryType.DefeatWarriorOfLight => "The Seat of Sacrifice",
            VictoryType.DefeatEndsinger => "The Final Day",
            VictoryType.DefeatZeromus => "The Abyssal Fracture",
            VictoryType.DefeatSphene => "The Interphos",
            VictoryType.DefeatNecron => "Everkeep",
            _ => "",
        };

        internal override VictoryType GoalType => Goal switch
        {
            0 => VictoryType.McGuffin,
            1 => VictoryType.DefeatUltimaWeapon,
            2 => VictoryType.DefeatThordan,
            3 => VictoryType.DefeatNidhogg,
            4 => VictoryType.DefeatShinryu,
            5 => VictoryType.DefeatTsukuyomi,
            6 => VictoryType.DefeatHades,
            7 => VictoryType.DefeatWarriorOfLight,
            8 => VictoryType.DefeatEndsinger,
            9 => VictoryType.DefeatZeromus,
            10 => VictoryType.DefeatSphene,
            11 => VictoryType.DefeatNecron,
            _ => VictoryType.McGuffin,
        };

        public override bool HasMapItems => true;

        internal override void Ready()
        {
            McGuffinCount = apState.Items.Count(i => i == "Memory of a Distant World");
        }
    }
}
