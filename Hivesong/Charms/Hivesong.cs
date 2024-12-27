using ItemChanger.Locations;
using ItemChanger;
using Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
using SFCore.Utils;

namespace Hivesong.Charms
{
    /// <summary>
    /// The Hivesong charm increases the power of pets by 30% (minimum 1)
    /// </summary>
    public class Hivesong : Charm
    {
        public override string Name => "Hivesong";

        public override string Description => "The soft song of the Hive Queen.\n\nIncreases the damage dealt by pets.";

        public override int DefaultCost => 2;

        public override AbstractLocation Location()
        {
            // This charm should be placed in the secret Hive tunnel where the second grub can be found
            return new CoordinateLocation()
            {
                name = InternalName(),
                sceneName = "Hive_04",
                x = 147.59f,
                y = 143.41f,
                elevation = 0f
            };
        }

        public Hivesong() { }

        public override void ApplyEffects()
        {
            ModHooks.HeroUpdateHook += BuffGrimmchild;
            On.KnightHatchling.OnEnable += BuffHatchling;
            ModHooks.ColliderCreateHook += BuffWeaverlings;
            ModHooks.ObjectPoolSpawnHook += BuffFlukes;
        }

        #region Grimmchild
        private Dictionary<GameObject, Dictionary<string, int>> buffsApplied = new Dictionary<GameObject, Dictionary<string, int>>();

        /// <summary>
        /// Increases the Grimmchild's damage.
        /// </summary>
        private void BuffGrimmchild()
        {
            // Grimmchild attacks by creating Grimmball projectiles
            // I could not figure out where Grimmball stores its damage
            //  I found a damage variable in its hitbox, but that wasn't it
            // However, I did learn that Grimmchild stores its damage values in its Control FSM as states
            // So this mod will modify those damage states instead

            List<GameObject> gameObjects = FindObjectsOfType<GameObject>()
                                            .Where(x => x.name.StartsWith("Grimmchild"))
                                            .ToList();
            foreach (GameObject gameObject in gameObjects)
            {
                // Have we buffed this Grimmchild already?
                bool isBuffed = buffsApplied.ContainsKey(gameObject);
                
                // Skip if already buffed and charm equipped, or if not buffed and charm not equipped
                if (isBuffed == IsEquipped())
                {
                    continue;
                }

                PlayMakerFSM fsm = FSMUtility.LocateFSM(gameObject, "Control");

                // If Grimmchild is not buffed, then buff it. Otherwise, debuff it
                if (!isBuffed)
                {
                    Dictionary<string, int> bonusDamageList = new Dictionary<string, int>();

                    // Skip level 1 because the Grimmchild doesn't attack at level 1
                    string[] states = new string[] { "Level 2", "Level 3", "Level 4" };
                    foreach (string state in states)
                    {
                        // Get base damage
                        HutongGames.PlayMaker.FsmInt fsmDamage = fsm.GetAction<HutongGames.PlayMaker.Actions.SetIntValue>(state, 0).intValue;
                        int baseDamage = fsmDamage.Value;

                        // Get bonus damage
                        int bonusDamage = GetBonusDamage(baseDamage);

                        // Apply bonus damage
                        fsm.GetAction<HutongGames.PlayMaker.Actions.SetIntValue>(state, 0).intValue.Value += bonusDamage;
                        //SharedData.Log($"Grimmchild {state} buffed: {baseDamage} -> {baseDamage + bonusDamage}");

                        // Add bonus damage to list
                        bonusDamageList.Add(state, bonusDamage);
                    }

                    // Add Grimmchild to list of buffed pets
                    buffsApplied.Add(gameObject, bonusDamageList);
                }
                else
                {
                    Dictionary<string, int> bonusDamageList = buffsApplied[gameObject];
                    foreach (string state in bonusDamageList.Keys)
                    {
                        // Get current and bonus damage
                        int currentDamage = fsm.GetAction<HutongGames.PlayMaker.Actions.SetIntValue>(state, 0).intValue.Value;
                        int bonusDamage = bonusDamageList[state];

                        // Remove bonus damage, for a minimum damage of 0
                        fsm.GetAction<HutongGames.PlayMaker.Actions.SetIntValue>(state, 0).intValue.Value = Math.Max(currentDamage - bonusDamage, 0);
                        //SharedData.Log($"Grimmchild {state} debuffed: {currentDamage} -> {fsm.GetAction<HutongGames.PlayMaker.Actions.SetIntValue>(state, 0).intValue.Value}");
                    }

                    // Remove Grimmchild from list of buffed pets
                    buffsApplied.Remove(gameObject);
                }
            }
        }
        #endregion

        #region Glowing Womb
        private List<KnightHatchling> buffedHatchlings = new List<KnightHatchling>();

        /// <summary>
        /// Increases the Knight Hatchling's damage
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void BuffHatchling(On.KnightHatchling.orig_OnEnable orig, KnightHatchling self)
        {
            // The Dung Defender charm is only applied when a Hatchling is created, so the Hivesong charm will
            //  also be applied when a Hatchling is made (and never un-applied)

            // This event gets called when entering a new area, so make sure not to buff a hatchling more than once
            if (IsEquipped() && !buffedHatchlings.Contains(self))
            {
                // Normal Damage
                int normal = self.normalDetails.damage;
                int bonusDamage = GetBonusDamage(normal);
                self.normalDetails.damage = normal + bonusDamage;

                // Dung Defender Damage
                int dung = self.dungDetails.damage;
                bonusDamage = GetBonusDamage(dung);
                self.dungDetails.damage = dung + bonusDamage;

                buffedHatchlings.Add(self);
                //SharedData.Log($"Hatchling buffed: {normal} -> {self.normalDetails.damage}, {dung} -> {self.dungDetails.damage}");
            }

            orig(self);
        }
        #endregion

        /// <summary>
        /// Increases the Weaverling's damage
        /// </summary>
        /// <param name="gameObject"></param>
        private void BuffWeaverlings(GameObject gameObject)
        {
            // Weaverling damage is stored in a hitbox
            // This method is called when that hitbox is made, so we can set the damage here
            //  without having to remove it later, unlike with the Grimmchild

            if (gameObject.name == "Enemy Damager" &&
                gameObject.transform.parent != null &&
                gameObject.transform.parent.name.StartsWith("Weaverling") &&
                IsEquipped())
            {
                PlayMakerFSM fsm = FSMUtility.LocateFSM(gameObject, "Attack");

                int baseDamage = fsm.FsmVariables.FindFsmInt("Damage").Value;
                fsm.FsmVariables.FindFsmInt("Damage").Value += GetBonusDamage(baseDamage);
                //SharedData.Log($"Weaverling buffed: {baseDamage} -> {fsm.FsmVariables.FindFsmInt("Damage").Value}");
            }
        }

        /// <summary>
        /// Increases the Spell Fluke's damage
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        private GameObject BuffFlukes(GameObject gameObject)
        {
            // Spell Flukes die pretty fast, so we don't really need to worry about
            //  removing the bonus damage if the player removes the charm after
            //  casting the spell

            if (gameObject.name.StartsWith("Spell Fluke") && 
                IsEquipped())
            {
                SpellFluke fluke = gameObject.GetComponent<SpellFluke>();

                // Fluke damage is stored in a private variable, so we need to
                //  get the field the variable is stored in
                FieldInfo damageField = fluke.GetType().GetField("damage", BindingFlags.NonPublic | BindingFlags.Instance);
                int baseDamage = (int)damageField.GetValue(fluke);

                int bonusDamage = GetBonusDamage(baseDamage);
                damageField.SetValue(fluke, baseDamage + bonusDamage);
                //SharedData.Log($"Fluke buffed: {baseDamage} -> {baseDamage + bonusDamage}");
            }

            return gameObject;
        }

        /// <summary>
        /// Hivesong increases the base damage by 30% (minimum 1)
        /// </summary>
        /// <param name="baseDamage"></param>
        /// <returns></returns>
        private int GetBonusDamage(int baseDamage)
        {
            float multiplier = 0.3f;

            return Math.Max((int)(baseDamage * multiplier), 1);
        }
    }
}