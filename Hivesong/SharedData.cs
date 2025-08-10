using Hivesong.Helpers;

namespace Hivesong
{
    /// <summary>
    /// Stores variables and functions used by multiple files in this project
    /// </summary>
    public static class SharedData
    {
        /// <summary>
        /// The central charm of the mod
        /// </summary>
        public static HivesongCharm hivesongCharm;

        /// <summary>
        /// Data for the save file
        /// </summary>
        public static LocalSaveData localSaveData { get; set; } = new LocalSaveData();
    }
}