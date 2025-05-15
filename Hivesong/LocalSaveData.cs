using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hivesong.Charms;

namespace Hivesong
{
    /// <summary>
    /// Charm-related info that gets stored in the save file
    /// </summary>
    public class LocalSaveData
    {
        public Dictionary<string, bool> charmFound = new Dictionary<string, bool>()
        {
            { SharedData.hivesong.InternalName(), false }
        };

        public Dictionary<string, bool> charmEquipped = new Dictionary<string, bool>()
        {
            { SharedData.hivesong.InternalName(), false }
        };

        public Dictionary<string, bool> charmNew = new Dictionary<string, bool>()
        {
            { SharedData.hivesong.InternalName(), false }
        };

        public Dictionary<string, int> charmCost = new Dictionary<string, int>()
        {
            { SharedData.hivesong.InternalName(), SharedData.hivesong.DefaultCost }
        };
    }
}