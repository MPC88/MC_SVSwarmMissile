using UnityEngine;

namespace MC_SVSwarmMissile
{
    internal class MissileStats
    {
        internal float radius;
        internal float damage;
        internal float critchance;
        internal float critdamage;

        internal static MissileStats GetMissileStats(SpaceShip ss, int rarity)
        {
            return new MissileStats()
            {
                critchance = Mathf.Clamp(Main.baseCritChance + ss.stats.weaponCritBonus, 0f, 100f),
                critdamage = Main.baseCritDamage + ss.stats.weaponCritDamageBonus,
                damage = (((Main.baseDmg + PChar.SKMod(4)) *
                            (1f + (float)PChar.Char.PassiveLimited(1) * 0.01f)) *
                            ItemDB.GetRarityMod(rarity, 1f)) *
                            (1f + ss.stats.heavyWeaponBonus) *
                            ss.energyMmt.valueMod(0),
                radius = Main.baseRadius * ItemDB.GetRarityMod(rarity, 1f)
            };
        }
    }
}
