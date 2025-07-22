using Hivesong.Charm_Helpers;
using ItemChanger;
using ItemChanger.Locations;
using Modding;
using System;
using UnityEngine;

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
        public bool IsUpgraded()
        {
            return SharedData.localSaveData.charmUpgraded &&
                    SharedData.exaltationInstalled;
        }

        #region Get Data
        /// <summary>
        /// Gets charm name
        /// </summary>
        /// <returns></returns>
        private string GetName()
        {
            if (!IsUpgraded())
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
            if (!IsUpgraded())
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
            On.HutongGames.PlayMaker.Actions.IntOperator.OnEnter += BuffGrimmchild;

            On.HealthManager.Hit += BuffHatchlings;

            On.HutongGames.PlayMaker.Actions.IntOperator.OnEnter += BuffWeaverlings;

            ModHooks.ObjectPoolSpawnHook += BuffFlukes;

            dungFlukeHelper = new DungFlukeHelper(1 - GetModifier(), true);
            dungFlukeHelper.Start();
        }

        /// <summary>
        /// Grimmchild fires Grimmballs that store damage from Grimmchild's FSM. Modifying Grimmchild's FSM hasn't
        /// proven effective, but what has worked is going into the Hit state of the Grimmball's Enemy Damager child
        /// object's Attack FSM, and modifying the Damage variable there.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void BuffGrimmchild(On.HutongGames.PlayMaker.Actions.IntOperator.orig_OnEnter orig, HutongGames.PlayMaker.Actions.IntOperator self)
        {
            //SharedData.Log($"Entering Int Operator: {self.Fsm.Name}, {self.Fsm.GameObjectName}, " +
            //                $"{self.Fsm.GameObject.transform.parent.gameObject.name}, {self.State.Name}");
            if (self.Fsm.Name.Equals("Attack") &&
                self.Fsm.GameObject.name.Equals("Enemy Damager") &&
                self.Fsm.GameObject.transform.parent.gameObject.name.Contains("Grimmball") &&
                self.State.Name.Equals("Hit"))
            {
                int baseDamage = self.Fsm.GetFsmInt("Damage").Value;
                self.Fsm.GetFsmInt("Damage").Value += GetBonusDamage(baseDamage);
                SharedData.Log($"Grimmball Hit state found. Damage increased from {baseDamage} to {self.Fsm.GetFsmInt("Damage").Value}");
            }

            orig(self);
        }

        /// <summary>
        /// Glowing Womb Hatchlings create a Damager object that triggers a HitInstance, so I can just
        /// find the HitInstance, confirm it came from a Knight Hatchling, and buff its damage
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="hitInstance"></param>
        private void BuffHatchlings(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            string parentName = "";
            if (hitInstance.Source.gameObject.name.Equals("Damager"))
            {
                // Check if the parent of the Damager object is a Knight Hatchling
                try
                {
                    parentName = hitInstance.Source.gameObject.transform.parent.name;
                }
                catch { }

                if (parentName.Contains("Knight Hatchling"))
                {
                    int baseDamage = hitInstance.DamageDealt;
                    hitInstance.DamageDealt += GetBonusDamage(baseDamage);
                    SharedData.Log($"Hatchling damage increased from {baseDamage} to {hitInstance.DamageDealt}");
                }
            }

            orig(self, hitInstance);
            //SharedData.Log($"{self.gameObject.name} " +
            //                $"hit by {hitInstance.Source.name} ({parentName}) " +
            //                $"for {hitInstance.DamageDealt}");
        }

        /// <summary>
        /// Weaverlings, like Grimmballs, store their damage in an Enemy Damager object
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void BuffWeaverlings(On.HutongGames.PlayMaker.Actions.IntOperator.orig_OnEnter orig, HutongGames.PlayMaker.Actions.IntOperator self)
        {
            int baseDamage = 0;
            if (self.Fsm.Name.Equals("Attack") &&
                self.Fsm.GameObject.name.Equals("Enemy Damager") &&
                self.Fsm.GameObject.transform.parent.gameObject.name.Contains("Weaverling") &&
                self.State.Name.Equals("Hit"))
            {
                baseDamage = self.Fsm.GetFsmInt("Damage").Value;
                self.Fsm.GetFsmInt("Damage").Value += GetBonusDamage(baseDamage);
                SharedData.Log($"Weaverling damage increased from {baseDamage} to {self.Fsm.GetFsmInt("Damage").Value}");
            }

            orig(self);

            // Reset damage afterwards; Weaverling FSMs don't reset like Grimmballs
            if (baseDamage > 0)
            {
                self.Fsm.GetFsmInt("Damage").Value = baseDamage;
            }
        }

        /// <summary>
        /// Used for handling damage buff for Dung Flukes
        /// </summary>
        private DungFlukeHelper dungFlukeHelper;

        /// <summary>
        /// Increases damage dealt by Flukenest
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        private GameObject BuffFlukes(GameObject gameObject)
        {
            // Spell Flukes die pretty fast, so we don't really need to worry about
            //  removing the bonus damage if the player removes the charm after
            //  casting the spell
            if (gameObject.name.StartsWith("Spell Fluke") &&
               gameObject.name.Contains("Clone"))
            {
                // Dung Flukes have to be handled separately
                if (!gameObject.name.Contains("Dung"))
                {
                    SpellFluke fluke = gameObject.GetComponent<SpellFluke>();

                    // Fluke damage is stored in a private variable, so we need to
                    //  get the field the variable is stored in
                    int baseDamage = SharedData.GetField<SpellFluke, int>(fluke, "damage");
                    int bonusDamage = GetBonusDamage(baseDamage);

                    SharedData.SetField(fluke, "damage", baseDamage + bonusDamage);
                    SharedData.Log($"Fluke damage increased from {baseDamage} to {baseDamage + bonusDamage}");
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
            if (IsUpgraded())
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