using Modding;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Hivesong
{
    public class Hivesong : Mod, IMod, ILocalSettings<LocalSaveData>
    {
        public static Hivesong Instance;

        public override string GetVersion() => "1.3.1.0";

        #region Save Data
        public void OnLoadLocal(LocalSaveData s)
        {
            SharedData.localSaveData = s;

            if (SharedData.hivesongCharm != null)
            {
                SharedData.hivesongCharm.OnLoadLocal();
            }
        }

        public LocalSaveData OnSaveLocal()
        {
            if (SharedData.hivesongCharm != null)
            {
                SharedData.hivesongCharm.OnSaveLocal();
            }

            return SharedData.localSaveData;
        }
        #endregion

        public Hivesong() : base("Hivesong") { }

        /// <summary>
        /// Called when the mod is loaded
        /// </summary>
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            Instance = new Hivesong();

            // Link supported mods
            GetOtherMods();

            // Define charm and link to ItemChanger
            SharedData.hivesongCharm = new Helpers.HivesongCharm();

            Log("Initialized");
        }

        /// <summary>
        /// Checks if supported mods are installed
        /// </summary>
        private void GetOtherMods()
        {
            if (ModHooks.GetMod("DebugMod") != null)
            {
                AddToGiveAllCharms(GiveCharms);
            }
        }

        #region Debug Mod
        /// <summary>
        /// Links the given method into the Debug mod's "GiveAllCharms" function
        /// </summary>
        /// <param name="a"></param>
        public static void AddToGiveAllCharms(Action function)
        {
            var debugCommands = Type.GetType("DebugMod.BindableFunctions, DebugMod");
            if (debugCommands == null)
            {
                return;
            }

            var giveAllCharms = debugCommands.GetMethod("GiveAllCharms", BindingFlags.Public | BindingFlags.Static);
            if (giveAllCharms == null)
            {
                return;
            }

            new Hook(giveAllCharms, (Action orig) =>
            {
                orig();
                function();
            });
        }

        /// <summary>
        /// Adds all of the mod's charms to the player (used by Debug Mode mod)
        /// </summary>
        private void GiveCharms()
        {
            SharedData.hivesongCharm.GiveCharm();
        }
        #endregion
    }
}