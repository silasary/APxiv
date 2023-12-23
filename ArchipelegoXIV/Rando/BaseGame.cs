using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelegoXIV.Rando
{
    public abstract class BaseGame(ApState apState)
    {
        protected ApState apState = apState;

        public abstract string Name { get; }

        public abstract bool MeetsRequirements(Location location);
    }
}
