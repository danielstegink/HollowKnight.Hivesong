using ItemChanger;
using ItemChanger.Tags;
using ItemChanger.UIDefs;
using Modding;
using MonoMod.RuntimeDetour;
using SFCore;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Hivesong
{
    public class Hivesong : Mod, IMod, ILocalSettings<LocalSaveData>
    {
        public override string GetVersion() => "1.1.0.0";

        #region Save Data
        /// <summary>
        /// Data for the save file
        /// </summary>
        private static LocalSaveData localSaveData { get; set; } = new LocalSaveData();

        public void OnLoadLocal(LocalSaveData s) => localSaveData = s;

        public LocalSaveData OnSaveLocal() => localSaveData;
        #endregion

        #region Variables
        /// <summary>
        /// A list of all added charms.
        /// </summary>
        private readonly static List<Charm> charms = new List<Charm>()
        {
            SharedData.hivesong
        };

        private readonly Dictionary<string, int> charmCosts = new Dictionary<string, int>();

        private readonly Dictionary<(string key, string sheet), string> charmTextData = new Dictionary<(string key, string sheet), string>();

        /// <summary>
        /// Needs to be a function so that the game can retrieve the save data dynamically
        /// </summary>
        private readonly Dictionary<string, Func<bool, bool>> charmBoolValues = new Dictionary<string, Func<bool, bool>>();

        private readonly Dictionary<string, Action<bool>> UpdateCharmBool = new Dictionary<string, Action<bool>>();

        private readonly Dictionary<string, int> charmIds = new Dictionary<string, int>();
        #endregion

        public Hivesong() : base("Hivesong") { }

        /// <summary>
        /// Called when the mod is loaded
        /// </summary>
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            SharedData.Log("Initializing Mod.");

            // Adding mod hooks (effects that should be run at certain points in the game)\
            SharedData.Log("Applying hooks");
            ModHooks.LanguageGetHook += GetCharmText;
            ModHooks.GetPlayerBoolHook += ReadCharmBools;
            ModHooks.SetPlayerBoolHook += WriteCharmBools;
            ModHooks.GetPlayerIntHook += ReadCharmCosts;
            On.GameManager.EnterHero += OnEnterHero;
            //On.PlayerData.CountCharms += CountOurCharms;

            if (ModHooks.GetMod("DebugMod") != null)
            {
                AddToGiveAllCharms(GiveCharms);
            }
            SharedData.Log("Hooks applied");

            SharedData.Log("Initializing charms");
            foreach (Charm charm in charms)
            {
                // Add the charm to the charm list and get its new ID number
                Sprite sprite = SpriteHelper.Get(charm.InternalName());
                int charmId = CharmHelper.AddSprites(new Sprite[] { sprite })[0];
                charm.Num = charmId;
                charmIds[charm.Name] = charmId;
                SharedData.Log($"Sprite found for {charm.Name}. ID assigned: {charmId}");

                // Store charm data in save settings
                string key = charm.InternalName();
                charmCosts[$"charmCost_{charmId}"] = localSaveData.charmCost[key];

                WriteCharmText($"CHARM_NAME_{charmId}", "UI", charm.Name);
                WriteCharmText($"CHARM_DESC_{charmId}", "UI", charm.Description);

                charmBoolValues[$"gotCharm_{charmId}"] = _ => localSaveData.charmFound[key];
                charmBoolValues[$"equippedCharm_{charmId}"] = _ => localSaveData.charmEquipped[key];
                charmBoolValues[$"newCharm_{charmId}"] = _ => localSaveData.charmNew[key];

                UpdateCharmBool[$"gotCharm_{charmId}"] = value => localSaveData.charmFound[key] = value;
                UpdateCharmBool[$"equippedCharm_{charmId}"] = value => localSaveData.charmEquipped[key] = value;
                UpdateCharmBool[$"newCharm_{charmId}"] = value => localSaveData.charmNew[key] = value;

                // Apply charm effects
                charm.ApplyEffects();

                // Tag the item for ConnectionMetadataInjector, so that MapModS and other mods recognize the items we're adding as charms.
                var item = new ItemChanger.Items.CharmItem()
                {
                    charmNum = charm.Num,
                    name = charm.InternalName(),
                    UIDef = new MsgUIDef()
                    {
                        name = new LanguageString("UI", $"CHARM_NAME_{charm.Num}"),
                        shopDesc = new LanguageString("UI", $"CHARM_DESC_{charm.Num}"),
                        sprite = new ItemChangerSprite(charm.InternalName(), sprite)
                    }
                };

                var mapModTag = item.AddTag<InteropTag>();
                mapModTag.Message = "RandoSupplementalMetadata";
                mapModTag.Properties["ModSource"] = GetName();
                mapModTag.Properties["PoolGroup"] = "Charms";

                // Add the charm and its location to ItemChanger for placement
                Finder.DefineCustomItem(item);
                Finder.DefineCustomLocation(charm.Location());
            }
            SharedData.Log("Charms initialized");

            SharedData.Log("Mod initialized");
        }

        #region Charm Data and Settings
        /// <summary>
        /// Gets text data related to the charms (name and description)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="sheetName"></param>
        /// <param name="orig"></param>
        /// <returns></returns>
        private string GetCharmText(string key, string sheetName, string orig)
        {
            if (charmTextData.ContainsKey((key, sheetName)))
            {
                return charmTextData[(key, sheetName)];
            }

            return orig;
        }

        /// <summary>
        /// Stores charm text such as names and descriptions
        /// </summary>
        /// <param name="key"></param>
        /// <param name="sheetName"></param>
        /// <param name="text"></param>
        internal void WriteCharmText(string key, string sheetName, string text)
        {
            charmTextData.Add((key, sheetName), text);
        }

        /// <summary>
        /// Gets boolean values related to the charms (equipped, new, found)
        /// </summary>
        /// <param name="charmBoolKey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private bool ReadCharmBools(string charmBoolKey, bool defaultValue)
        {
            bool value = defaultValue;

            if (charmBoolValues.ContainsKey(charmBoolKey))
            {
                value = charmBoolValues[charmBoolKey](defaultValue);
            }

            return value;
        }

        /// <summary>
        /// Sets boolean values related to charms (equipped, new, found)
        /// </summary>
        /// <param name="charmBoolKey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private bool WriteCharmBools(string charmBoolKey, bool defaultValue)
        {
            if (UpdateCharmBool.ContainsKey(charmBoolKey))
            {
                UpdateCharmBool[charmBoolKey](defaultValue);
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets the costs of charms
        /// </summary>
        /// <param name="charmCostKey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private int ReadCharmCosts(string charmCostKey, int defaultValue)
        {
            if (charmCosts.ContainsKey(charmCostKey))
            {
                return charmCosts[charmCostKey];
            }
            else
            {
                return defaultValue;
            }
        }
        #endregion

        #region Charm Placement
        /// <summary>
        /// Triggers when the player enters a new area
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="additiveGateSearch"></param>
        private void OnEnterHero(On.GameManager.orig_EnterHero orig, GameManager self, bool additiveGateSearch)
        {
            //SharedData.Log($"Scene entered: {self.sceneName}");
            PlaceCharms();

            orig(self, additiveGateSearch);
        }

        /// <summary>
        /// Gets charms from ItemChanger and places them on the map
        /// </summary>
        public void PlaceCharms()
        {
            ItemChangerMod.CreateSettingsProfile(false, false);

            List<AbstractPlacement> placements = new List<AbstractPlacement>();
            foreach (Charm charm in charms)
            {
                // Only place charms player hasn't collected
                if (!localSaveData.charmFound[charm.InternalName()])
                {
                    AbstractLocation location = Finder.GetLocation(charm.InternalName());

                    AbstractPlacement placement = location.Wrap();

                    AbstractItem item = Finder.GetItem(charm.InternalName());
                    placement.Add(item);

                    placements.Add(placement);
                }
            }

            ItemChangerMod.AddPlacements(placements, PlacementConflictResolution.Replace);
        }
        #endregion

        #region Debug Mod
        /// <summary>
        /// Links the given method into the Debug mod's "GiveAllCharms" function
        /// </summary>
        /// <param name="a"></param>
        public static void AddToGiveAllCharms(Action function)
        {
            var commands = Type.GetType("DebugMod.BindableFunctions, DebugMod");
            if (commands == null)
            {
                return;
            }

            var method = commands.GetMethod("GiveAllCharms", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                return;
            }

            new Hook(method, (Action orig) =>
            {
                orig();
                function();
            }
            );
        }

        /// <summary>
        /// Adds all of the mod's charms to the player (used by Debug Mode mod)
        /// </summary>
        private void GiveCharms()
        {
            foreach (var charm in charms)
            {
                localSaveData.charmFound[charm.InternalName()] = true;
            }
        }
        #endregion
    }
}