using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchepelegoXIV.Rando
{
    public abstract class BaseGame
    {
        protected ApState apState;

        public BaseGame(ApState apState)
        {
            this.apState = apState;
        }

        public abstract string Name { get; }

        public abstract bool MeetsRequirements(Location location);
    }
}
