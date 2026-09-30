using BulletHell.Enemies;
using BulletHell.Shop;
using BulletHell.Weapons;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BulletHell.Tests
{
    public class EnemyBulletPaletteTests
    {
        private static EnemyBulletPalette Palette() => AssetDatabase.LoadAssetAtPath<EnemyBulletPalette>("Assets/Data/Enemies/EnemyBulletPalette.asset");

        [Test]
        public void ThePaletteHoldsTheSpecifiedColours()
        {
            EnemyBulletPalette palette = Palette();
            Assert.IsNotNull(palette);

            EnemyBulletPalette.Look violet = palette.LookOf(BulletStyle.Standard);
            EnemyBulletPalette.Look magenta = palette.LookOf(BulletStyle.Special);
            Assert.AreEqual("FFFFFF", ColorUtility.ToHtmlStringRGB(violet.Core));
            Assert.AreEqual("B44BFF", ColorUtility.ToHtmlStringRGB(violet.Body));
            Assert.AreEqual("1B0730", ColorUtility.ToHtmlStringRGB(violet.Outline));
            Assert.AreEqual("FFFFFF", ColorUtility.ToHtmlStringRGB(magenta.Core));
            Assert.AreEqual("FF3DCB", ColorUtility.ToHtmlStringRGB(magenta.Body));
            Assert.AreEqual("1B0730", ColorUtility.ToHtmlStringRGB(magenta.Outline));
            Assert.AreEqual(2f, palette.OutlinePixels);
            Assert.LessOrEqual(palette.GlowOpacity, 0.3f);
        }

        [Test]
        public void BulletHuesAreReservedAndOrdinaryGameColoursAreNot()
        {
            EnemyBulletPalette palette = Palette();
            Assert.IsTrue(palette.IsReservedHue(palette.LookOf(BulletStyle.Standard).Body));
            Assert.IsTrue(palette.IsReservedHue(palette.LookOf(BulletStyle.Special).Body));
            Assert.IsFalse(palette.IsReservedHue(Color.white));                       // the core is white
            Assert.IsFalse(palette.IsReservedHue(palette.LookOf(BulletStyle.Standard).Outline)); // dark
            Assert.IsFalse(palette.IsReservedHue(new Color(1f, 0.35f, 0.15f)));       // red arm
            Assert.IsFalse(palette.IsReservedHue(new Color(0.35f, 0.85f, 1f)));       // blue arm
        }

        [Test]
        public void NoArmIdColourAmmoTintOrRarityUsesABulletHue()
        {
            EnemyBulletPalette palette = Palette();
            foreach (string guid in AssetDatabase.FindAssets("t:WeaponArmData"))
            {
                var arm = AssetDatabase.LoadAssetAtPath<WeaponArmData>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.IsFalse(palette.IsReservedHue(arm.IdColor), arm.name + " ID colour clashes with the enemy bullet hues");
            }
            foreach (string guid in AssetDatabase.FindAssets("t:AmmoTypeData"))
            {
                var ammo = AssetDatabase.LoadAssetAtPath<AmmoTypeData>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.IsFalse(palette.IsReservedHue(ammo.Tint), ammo.name + " tint clashes with the enemy bullet hues");
            }
            var table = AssetDatabase.LoadAssetAtPath<RarityTable>("Assets/Data/Shop/RarityTable.asset");
            foreach (ArmamentRarity rarity in System.Enum.GetValues(typeof(ArmamentRarity)))
                Assert.IsFalse(palette.IsReservedHue(table.ColorOf(rarity)), rarity + " rarity colour clashes with the enemy bullet hues");
        }

        [Test]
        public void BossAndSniperPatternsAreSpecialAndTheRestAreStandard()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AttackPattern"))
            {
                var pattern = AssetDatabase.LoadAssetAtPath<AttackPattern>(AssetDatabase.GUIDToAssetPath(guid));
                // Boss shots (the placeholder burst, every Pumpking pattern) and the sniper shot are Hot Magenta.
                bool special = pattern.name == "Pattern_BossBurst" || pattern.name == "Pattern_SniperShot" || pattern.name.StartsWith("Pattern_Pumpking");
                Assert.AreEqual(special ? BulletStyle.Special : BulletStyle.Standard, pattern.BulletStyle, pattern.name);
            }
        }
    }
}
