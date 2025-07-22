using System.Reflection;

namespace Hivesong
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

        /// <summary>
        /// Gets a private field from the given input class
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <typeparam name="O"></typeparam>
        /// <param name="input"></param>
        /// <param name="fieldName"></param>
        /// <param name="isStaticOrConst"></param>
        /// <returns></returns>
        public static O GetField<I, O>(I input, string fieldName, bool isStaticOrConst = false)
        {
            BindingFlags typeFlag = BindingFlags.Instance;
            if (isStaticOrConst)
            {
                typeFlag = BindingFlags.Static;
            }

            FieldInfo fieldInfo = input.GetType()
                                       .GetField(fieldName, BindingFlags.NonPublic | typeFlag);
            return (O)fieldInfo.GetValue(input);
        }

        /// <summary>
        /// Sets a private field from the given input class
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <typeparam name="O"></typeparam>
        /// <param name="input"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <param name="isStaticOrConst"></param>
        public static void SetField<I, O>(I input, string fieldName, O value, bool isStaticOrConst = false)
        {
            BindingFlags typeFlag = BindingFlags.Instance;
            if (isStaticOrConst)
            {
                typeFlag = BindingFlags.Static;
            }

            FieldInfo fieldInfo = input.GetType()
                                       .GetField(fieldName, BindingFlags.NonPublic | typeFlag);
            fieldInfo.SetValue(input, value);
        }
    }
}