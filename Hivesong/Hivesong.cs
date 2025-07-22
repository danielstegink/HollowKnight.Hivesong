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
        public override string GetVersion() => "1.2.1.0";

        #region Save Data
        public void OnLoadLocal(LocalSaveData s) => SharedData.localSaveData = s;

        public LocalSaveData OnSaveLocal() => SharedData.localSaveData;
        #endregion

        private Sprite upgradedSprite = null;

        public Hivesong() : base("Hivesong") { }

        /// <summary>
        /// Called when the mod is loaded
        /// </summary>
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            SharedData.Log("Initializing Mod.");

            // Add mod hooks (effects that should be run at certain points in the game)\
            ModHooks.LanguageGetHook += GetCharmText;
            ModHooks.GetPlayerBoolHook += GetCharmBools;
            ModHooks.SetPlayerBoolHook += SetCharmBools;
            ModHooks.GetPlayerIntHook += GetCharmCosts;
            On.GameManager.EnterHero += OnEnterHero;
            ModHooks.SavegameSaveHook += SaveGame;
            On.CharmIconList.GetSprite += GetSprite;

            // Link supported mods
            GetOtherMods();

            // Define charm and link to ItemChanger
            AddCharmToGame();

            SharedData.Log("Mod initialized");
        }

        #region Initialize Helpers
        /// <summary>
        /// Checks if supported mods are installed
        /// </summary>
        private void GetOtherMods()
        {
            if (ModHooks.GetMod("DebugMod") != null)
            {
                AddToGiveAllCharms(GiveCharms);
            }

            if (ModHooks.GetMod("Exaltation") != null &&
                ModHooks.GetMod("ExaltationExpanded") != null)
            {
                SharedData.exaltationInstalled = true;
            }
        }

        /// <summary>
        /// Adds charm to game
        /// </summary>
        private void AddCharmToGame()
        {
            // Add the charm to the charm list and get its new ID number
            Sprite sprite = SpriteHelper.Get(SharedData.hivesong.InternalName());
            int charmId = CharmHelper.AddSprites(new Sprite[] { sprite })[0];
            SharedData.hivesong.Num = charmId;
            SharedData.Log($"Sprite found for {SharedData.hivesong.Name}. ID assigned: {charmId}");

            // Get exalted version too, in case we need it
            upgradedSprite = SpriteHelper.Get("RoyalDecree");

            // Apply charm effects
            SharedData.hivesong.ApplyEffects();

            // Add the charm and its location to ItemChanger for placement
            var item = new ItemChanger.Items.CharmItem()
            {
                charmNum = SharedData.hivesong.Num,
                name = SharedData.hivesong.InternalName(),
                UIDef = new MsgUIDef()
                {
                    name = new LanguageString("UI", $"CHARM_NAME_{SharedData.hivesong.Num}"),
                    shopDesc = new LanguageString("UI", $"CHARM_DESC_{SharedData.hivesong.Num}"),
                    sprite = new ItemChangerSprite(SharedData.hivesong.InternalName(), sprite)
                }
            };

            var mapModTag = item.AddTag<InteropTag>();
            mapModTag.Message = "RandoSupplementalMetadata";
            mapModTag.Properties["ModSource"] = GetName();
            mapModTag.Properties["PoolGroup"] = "Charms";

            Finder.DefineCustomItem(item);
            Finder.DefineCustomLocation(SharedData.hivesong.Location());
        }
        #endregion

        #region Charm Data and Settings
        /// <summary>
        /// Gets text data related to the charms (name and description)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="sheetTitle"></param>
        /// <param name="orig"></param>
        /// <returns></returns>
        private string GetCharmText(string key, string sheetTitle, string orig)
        {
            if (key.Equals($"CHARM_NAME_{SharedData.hivesong.Num}"))
            {
                return SharedData.hivesong.Name;
            }
            else if (key.Equals($"CHARM_DESC_{SharedData.hivesong.Num}"))
            {
                return SharedData.hivesong.Description;
            }

            return orig;
        }

        /// <summary>
        /// Gets boolean values related to the charms (equipped, new, found)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="orig"></param>
        /// <returns></returns>
        private bool GetCharmBools(string name, bool orig)
        {
            string charmName = SharedData.hivesong.InternalName();
            if (name.Equals($"gotCharm_{SharedData.hivesong.Num}"))
            {
                return SharedData.localSaveData.charmFound[charmName];
            }
            else if (name.Equals($"equippedCharm_{SharedData.hivesong.Num}"))
            {
                return SharedData.localSaveData.charmEquipped[charmName];
            }
            else if (name.Equals($"newCharm_{SharedData.hivesong.Num}"))
            {
                return SharedData.localSaveData.charmNew[charmName];
            }

            return orig;
        }

        /// <summary>
        /// Sets boolean values related to charms (equipped, new, found)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="orig"></param>
        /// <returns></returns>
        private bool SetCharmBools(string name, bool orig)
        {
            string charmName = SharedData.hivesong.InternalName();
            if (name.Equals($"gotCharm_{SharedData.hivesong.Num}"))
            {
                SharedData.localSaveData.charmFound[charmName] = orig;
            }
            else if (name.Equals($"equippedCharm_{SharedData.hivesong.Num}"))
            {
                SharedData.localSaveData.charmEquipped[charmName] = orig;
            }
            else if (name.Equals($"newCharm_{SharedData.hivesong.Num}"))
            {
                SharedData.localSaveData.charmNew[charmName] = orig;
            }

            return orig;
        }

        /// <summary>
        /// Gets the costs of charms
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private int GetCharmCosts(string name, int orig)
        {
            string charmName = SharedData.hivesong.InternalName();
            if (name.Equals($"charmCost_{SharedData.hivesong.Num}"))
            {
                return SharedData.localSaveData.charmCost[charmName];
            }

            return orig;
        }

        /// <summary>
        /// Gets the charm's sprite
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private Sprite GetSprite(On.CharmIconList.orig_GetSprite orig, CharmIconList self, int id)
        {
            // If the charm has been upgraded, get the upgraded version's sprite instead
            if (id == SharedData.hivesong.Num &&
                SharedData.hivesong.IsUpgraded())
            {
                return upgradedSprite;
            }

            return orig(self, id);
        }
        #endregion

        /// <summary>
        /// Upon saving, upgrade Hivesong if it is eligible.
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SaveGame(int obj)
        {
            // Confirm ascended Hive Knight has been bested
            SharedData.localSaveData.charmUpgraded = PlayerData.instance.statueStateHiveKnight.completedTier2;
        }

        #region Charm Placement
        /// <summary>
        /// Triggers when the player enters a new area
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="additiveGateSearch"></param>
        private void OnEnterHero(On.GameManager.orig_EnterHero orig, GameManager self, bool additiveGateSearch)
        {
            PlaceCharms();

            orig(self, additiveGateSearch);
        }

        /// <summary>
        /// Gets charms from ItemChanger and places them on the map
        /// </summary>
        public void PlaceCharms()
        {
            ItemChangerMod.CreateSettingsProfile(false, false);

            string charmName = SharedData.hivesong.InternalName();
            if (!SharedData.localSaveData.charmFound[charmName])
            {
                List<AbstractPlacement> placements = new List<AbstractPlacement>();

                AbstractLocation location = Finder.GetLocation(charmName);
                AbstractPlacement placement = location.Wrap();
                AbstractItem item = Finder.GetItem(charmName);
                placement.Add(item);

                placements.Add(placement);
                ItemChangerMod.AddPlacements(placements, PlacementConflictResolution.Replace);
            }

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
            SharedData.localSaveData.charmFound[SharedData.hivesong.InternalName()] = true;
        }
        #endregion
    }
}