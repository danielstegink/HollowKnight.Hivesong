﻿namespace Hivesong
{
    /// <summary>
    /// Stores variables and functions used by multiple files in this project
    /// </summary>
    public static class SharedData
    {
        public static Charms.Hivesong hivesong = new Charms.Hivesong();

        /// <summary>
        /// Data for the save file
        /// </summary>
        public static LocalSaveData localSaveData { get; set; } = new LocalSaveData();

        private static Hivesong _logger = new Hivesong();

        /// <summary>
        /// Logs message to the shared mod log at AppData\LocalLow\Team Cherry\Hollow Knight\ModLog.txt
        /// </summary>
        /// <param name="message"></param>
        public static void Log(string message)
        {
            _logger.Log(message);
        }

        public static bool exaltationInstalled = false;
    }
}