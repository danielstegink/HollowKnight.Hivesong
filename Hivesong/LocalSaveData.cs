using System.Collections.Generic;

namespace Hivesong
{
    /// <summary>
    /// Charm-related info that gets stored in the save file
    /// </summary>
    public class LocalSaveData
    {
        public Dictionary<string, bool> charmFound = new Dictionary<string, bool>()
        {
            { "Hivesong", false }
        };

        public Dictionary<string, bool> charmEquipped = new Dictionary<string, bool>()
        {
            { "Hivesong", false }
        };

        public Dictionary<string, bool> charmNew = new Dictionary<string, bool>()
        {
            { "Hivesong", false }
        };

        public Dictionary<string, int> charmCost = new Dictionary<string, int>()
        {
            { "Hivesong", 2 }
        };

        public bool charmUpgraded = false;
    }
}