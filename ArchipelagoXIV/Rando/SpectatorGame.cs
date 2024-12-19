using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoXIV.Rando
{
    /// <summary>
    /// Text Client only
    /// </summary>
    internal class SpectatorGame : BaseGame
    {
        public SpectatorGame(ApState apState) : base(apState)
        {
        }

        public override string Name => "";

        public override int MaxLevel()
        {
            return 0;
        }

        public override int MaxLevel(string job)
        {
            return 0;
        }
    }
}
