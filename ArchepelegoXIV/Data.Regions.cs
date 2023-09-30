using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchepelegoXIV
{
    partial class Data
    {
        static Data()
        {
            DungeonEntrances = new Dictionary<string, string>()
            {
                { "Satasha", "Western La Noscea" },
                { "The Tam-Tara Deepcroft", "Central Shroud" },
                { "Copperbell Mines", "Western Thanalan" },
                { "Halatali", "Eastern Thanalan" },
                { "The Thousand Maws of Toto-Rak", "South Shroud" },
                { "Haukke Manor", "Central Shroud" },
                { "Brayflox’s Longstop", "Eastern La Noscea" },
                { "The Sunken Temple of Qarn", "Southern Thanalan" },
                { "Cutter's Cry", "Central Thanalan" },
                { "The Stone Vigil", "Coerthas Central Highlands" },
                { "Dzemael Darkhold", "Coerthas Central Highlands" },
                { "The Aurum Vale", "Coerthas Central Highlands" },
                { "Castrum Meridianum", "Northern Thanalan" },
                { "The Praetorium", "Northern Thanalan" },
            };
        }
    }
}
