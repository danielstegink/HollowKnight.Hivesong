using ItemChanger.Locations;
using ItemChanger;
using Modding;
using System;
using UnityEngine;
using System.Reflection;
using SFCore.Utils;
using HutongGames.PlayMaker;
using System.Collections.Generic;

namespace Hivesong.Charms
{
    /// <summary>
    /// The Hivesong charm increases the power of pets by 30% (minimum 1)
    /// </summary>
    public class Hivesong : Charm
    {
        public override string Name => GetName();

        public override string Description => GetDescription();

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

        public override string InternalName()
        {
            return "Hivesong";
        }

        /// <summary>
        /// Whether or not the charm has been upgraded (see Exaltation Expanded)
        /// </summary>
        public bool IsUpgraded = false;

        #region Get Data
        /// <summary>
        /// Gets charm name
        /// </summary>
        /// <returns></returns>
        private string GetName()
        {
            if (!IsUpgraded)
            {
                return "Hivesong";
            }
            else
            {
                return "Royal Decree";
            }
        }

        /// <summary>
        /// Gets charm description
        /// </summary>
        /// <returns></returns>
        private string GetDescription()
        {
            if (!IsUpgraded)
            {
                return "The soft song of the Hive Queen.\n\n" +
                        "Increases the damage dealt by pets.";
            }
            else
            {
                return "Contains the divine authority of the Pale King.\n\n" +
                        "Greatly increases the damage dealt by pets.";
            }
        }
        #endregion

        #region Hooks
        public override void ApplyEffects()
        {
            ModHooks.ObjectPoolSpawnHook += BuffGrimmchild;
            ModHooks.ObjectPoolSpawnHook += BuffHatchling;
            ModHooks.ColliderCreateHook += BuffWeaverlings;
            ModHooks.ObjectPoolSpawnHook += BuffFlukes;
        }

        #region Grimmchild
        private Dictionary<string, int> grimmchildValues = new Dictionary<string, int>();

        /// <summary>
        /// Increases damage dealt by Grimmchild
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        private GameObject BuffGrimmchild(GameObject gameObject)
        {
            // Grimmchild attacks by creating Grimmball projectiles
            // I could not figure out where Grimmball stores its damage
            //  I found a damage variable in its hitbox, but that wasn't it
            // However, I did learn that Grimmchild stores its damage values in its Control FSM as states
            // So this mod will modify those damage states instead
            // Checking for clones doesn't prevent damage stacking, so we will store the initial values
            //  and use them as a reference

            if (IsEquipped() &&
                gameObject.name.StartsWith("Grimmchild"))
            {
                PlayMakerFSM fsm = FSMUtility.LocateFSM(gameObject, "Control");

                string[] states = new string[] { "Level 2", "Level 3", "Level 4" };
                foreach (string state in states)
                {
                    // Get base damage
                    HutongGames.PlayMaker.FsmInt fsmDamage = fsm.GetAction<HutongGames.PlayMaker.Actions.SetIntValue>(state, 0).intValue;
                    int baseDamage = fsmDamage.Value;
                    if (!grimmchildValues.ContainsKey(state))
                    {
                        grimmchildValues.Add(state, baseDamage);
                    }
                    else
                    {
                        baseDamage = grimmchildValues[state];
                    }

                    // Get bonus damage
                    int bonusDamage = GetBonusDamage(baseDamage);

                    // Apply bonus damage
                    fsm.GetAction<HutongGames.PlayMaker.Actions.SetIntValue>(state, 0).intValue.Value = baseDamage + bonusDamage;
                    //SharedData.Log($"Grimmchild {state} buffed: {baseDamage} -> {baseDamage + bonusDamage}");
                }
            }

            return gameObject;
        }
        #endregion

        /// <summary>
        /// Increases damage dealt by Glowing Womb
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        private GameObject BuffHatchling(GameObject gameObject)
        {
            if (IsEquipped() &&
                gameObject.name.Equals("Knight Hatchling(Clone)"))
            {
                // Knight Hatchling damage is stored in the KnightHatchling component
                KnightHatchling component = gameObject.GetComponent<KnightHatchling>();

                // Normal damage
                int baseDamage = component.normalDetails.damage;
                int bonusDamage = GetBonusDamage(baseDamage);
                component.normalDetails.damage += bonusDamage;
                //SharedData.Log($"Hatchling Normal: {baseDamage} -> {baseDamage + bonusDamage}");

                // Dung damage
                baseDamage = component.dungDetails.damage;
                bonusDamage = GetBonusDamage(baseDamage);
                component.dungDetails.damage += bonusDamage;
                //SharedData.Log($"Hatchling Dung: {baseDamage} -> {baseDamage + bonusDamage}");
            }

            return gameObject;
        }

        /// <summary>
        /// Increases damage dealt by Weaversong
        /// </summary>
        /// <param name="gameObject"></param>
        private void BuffWeaverlings(GameObject gameObject)
        {
            // Weaverling damage is stored in a hitbox
            // This method is called when that hitbox is made, so we can set the damage here
            //  without having to remove it later

            if (gameObject.name == "Enemy Damager" &&
                gameObject.transform.parent != null &&
                gameObject.transform.parent.name.Equals("Weaverling(Clone)") &&
                IsEquipped())
            {
                PlayMakerFSM fsm = FSMUtility.LocateFSM(gameObject, "Attack");

                int baseDamage = fsm.FsmVariables.FindFsmInt("Damage").Value;
                fsm.FsmVariables.FindFsmInt("Damage").Value += GetBonusDamage(baseDamage);
                //SharedData.Log($"Weaverling buffed: {baseDamage} -> {fsm.FsmVariables.FindFsmInt("Damage").Value}");
            }
        }

        /// <summary>
        /// Increases damage dealt by Flukesong
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        private GameObject BuffFlukes(GameObject gameObject)
        {
            // Spell Flukes die pretty fast, so we don't really need to worry about
            //  removing the bonus damage if the player removes the charm after
            //  casting the spell
            if (IsEquipped() &&
                gameObject.name.StartsWith("Spell Fluke") &&
                gameObject.name.Contains("Clone")) // Only modify clones
            {
                //SharedData.Log($"Fluke found: {gameObject.name}");
                if (gameObject.name.Contains("Dung"))
                {
                    // At Lv 1, the Dung Fluke stores the dung cloud at step 8 of the Blow state in its Control FSM
                    int step = 8;
                    if (gameObject.name.Contains("Lv2")) // At level 2, the dung cloud is stored in step 10
                    {
                        step = 10;
                    }

                    // Get the Dung Fluke's Control FSM
                    PlayMakerFSM fsm = FSMUtility.LocateFSM(gameObject, "Control");

                    // Get the dung cloud object from the Blow state
                    FsmOwnerDefault fsmDungCloudOwner = fsm.GetAction<HutongGames.PlayMaker.Actions.ActivateGameObject>("Blow", step).gameObject;
                    FsmGameObject fsmDungCloud = fsmDungCloudOwner.GameObject;
                    GameObject dungCloudPrefab = fsmDungCloud.Value;
                    //SharedData.Log($"Dung Cloud Prefab found: {dungCloudPrefab.name}");

                    // We can't easily control how much damage the dung cloud does, but
                    // we CAN increase its damage rate, which is more reliable anyway
                    dungCloudPrefab.GetComponent<DamageEffectTicker>().damageInterval *= 1 - GetModifier();

                    // Then we just have to put the modified dung cloud back into the FSM
                    fsmDungCloud.Value = dungCloudPrefab;
                    fsmDungCloudOwner.GameObject = fsmDungCloud;
                    fsm.GetAction<HutongGames.PlayMaker.Actions.ActivateGameObject>("Blow", step).gameObject = fsmDungCloudOwner;
                }
                else
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
            }

            return gameObject;
        }

        /// <summary>
        /// Gets damage modifier
        /// </summary>
        /// <returns></returns>
        private float GetModifier()
        {
            float modifier = 0.3f;
            if (IsUpgraded)
            {
                modifier = 0.4f;
            }

            return modifier;
        }

        /// <summary>
        /// Hivesong increases pet damage by 30% (minimum 1)
        /// </summary>
        /// <param name="baseDamage"></param>
        /// <returns></returns>
        private int GetBonusDamage(int baseDamage)
        {
            float newDamage = baseDamage * GetModifier();

            return Math.Max((int)newDamage, 1);
        }
        #endregion
    }
}