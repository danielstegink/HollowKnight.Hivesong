using DanielSteginkUtils.Helpers.Charms.Pets;
using DanielSteginkUtils.Helpers.Charms.Templates;
using ItemChanger;
using ItemChanger.Locations;
using UnityEngine;

namespace Hivesong.Helpers
{
    /// <summary>
    /// The Hivesong charm increases the power of pets by 33% (minimum 1)
    /// </summary>
    public class HivesongCharm : ExaltedCharm
    {
        public HivesongCharm() : base(Hivesong.Instance.Name, true) { }

        #region Properties
        public override string name => "Hivesong";

        public override string exaltedName => "Royal Decree";

        public override string description => "The soft song of the Hive Queen.\n\n" +
                                                "Increases the damage dealt by pets.";

        public override string exaltedDescription => "Contains the divine authority of the Pale King.\n\n" +
                                                        "Greatly increases the damage dealt by pets.";

        public override Sprite icon => SpriteHelper.Get("Hivesong");

        public override Sprite exaltedIcon => SpriteHelper.Get("RoyalDecree");

        public override int cost => 2;

        public override int exaltedCost => 2;
        #endregion

        protected override Sprite GetSpriteInternal()
        {
            return SpriteHelper.Get("Hivesong");
        }

        public override AbstractLocation ItemChangerLocation()
        {
            // This charm should be placed in the secret Hive tunnel where the second grub can be found
            return new CoordinateLocation()
            {
                name = GetItemChangerId(),
                sceneName = "Hive_04",
                x = 147.59f,
                y = 143.41f,
                elevation = 0f
            };
        }

        #region Exaltation
        public override bool CanUpgrade()
        {
            return PlayerData.instance.statueStateHiveKnight.completedTier2;
        }

        public override void Upgrade()
        {
            base.Upgrade();

            if (IsEquipped)
            {
                ResetHelper();
                helper.Start();
            }
        }
        #endregion

        #region Settings
        public override void OnLoadLocal()
        {
            ExaltedCharmState charmSettings = new ExaltedCharmState()
            {
                IsEquipped = SharedData.localSaveData.charmEquipped[GetItemChangerId()],
                GotCharm = SharedData.localSaveData.charmFound[GetItemChangerId()],
                IsNew = SharedData.localSaveData.charmNew[GetItemChangerId()],
                IsUpgraded = SharedData.localSaveData.charmUpgraded
            };

            RestoreCharmState(charmSettings);
        }

        public override void OnSaveLocal()
        {
            ExaltedCharmState charmSettings = GetCharmState();
            SharedData.localSaveData.charmEquipped[GetItemChangerId()] = IsEquipped;
            SharedData.localSaveData.charmFound[GetItemChangerId()] = GotCharm;
            SharedData.localSaveData.charmNew[GetItemChangerId()] = IsNew;
            SharedData.localSaveData.charmUpgraded = IsUpgraded;
        }
        #endregion

        #region Activation
        /// <summary>
        /// Activates the charm effects
        /// </summary>
        public override void Equip()
        {
            //Hivesong.Instance.Log("Pets helper started");
            ResetHelper();
            helper.Start();
        }

        /// <summary>
        /// Deactivates the charm effects
        /// </summary>
        public override void Unequip()
        {
            ResetHelper();
        }

        /// <summary>
        /// Pet Utils
        /// </summary>
        private AllPetsHelper helper;
        
        /// <summary>
        /// Resets the AllPetsHelper
        /// </summary>
        private void ResetHelper()
        {
            if (helper != null)
            {
                helper.Stop();
            }

            helper = new AllPetsHelper(Hivesong.Instance.Name, GetItemChangerId(), GetModifier());
        }

        /// <summary>
        /// Gets damage modifier
        /// </summary>
        /// <returns></returns>
        private float GetModifier()
        {
            // Per my Utils, 1 notch is worth a 16.67% increase in pet damage
            float modifier = 2 * DanielSteginkUtils.Utilities.NotchCosts.PetDamagePerNotch();

            // Exaltation increases the value of a charm by 2 notches, or double in this case
            if (IsUpgraded)
            {
                modifier *= 2f;
            }

            return 1 + modifier;
        }
        #endregion
    }
}