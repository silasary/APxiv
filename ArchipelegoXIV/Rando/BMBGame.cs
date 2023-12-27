using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArchipelegoXIV.Rando
{
    internal partial class BMBGame(ApState apState) : BaseGame(apState)
    {
        public override string Name => "Manual_FFXIVBMB_Pizzie";

        public override int MaxLevel() => apState.Items.Count(i => i == "10 Equip Levels") * 10;

        public override int MaxLevel(string job)
        {
            if (job == "BLU")
                return MaxLevel();
            return 0;
        }
    }
}
