using UnityEngine;

namespace Hivesong.Helpers
{
    public static class SpriteHelper
    {
        /// <summary>
        /// Gets the charm's sprite (icon) from the mod's embedded resources
        /// </summary>
        /// <returns></returns>
        public static Sprite Get(string spriteFileName)
        {
            return DanielSteginkUtils.Helpers.SpriteHelper.GetLocalSprite($"Hivesong.Resources.{spriteFileName}.png", "Hivesong");
        }
    }
}