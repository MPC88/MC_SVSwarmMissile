using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace MC_SVSwarmMissile
{
    internal class SmallMissileObject
    {
        private static FieldInfo f_ProjectileControl_entity = null;
        private static FieldInfo f_ProjectileControl_rb = null;

        internal GameObject go;
        internal GameObject thruster;
        internal Collider collider;
        internal AudioSource audio;

        internal void Fire(MissileStats stats, SpaceShip owner, Transform target, Vector3 baseVelocity)
        {
            ProjectileControl projCont = this.go.AddComponent<ProjectileControl>();
            projCont.aoe = owner.stats.explodeBoostChance > 0 && Random.Range(1, 101) <= owner.stats.explodeBoostChance ? Main.aoe * (1f + owner.stats.explodeBoost) : Main.aoe;
            projCont.autoTargeting = true;
            projCont.canHitProjectiles = false;
            projCont.damage = stats.damage;
            projCont.damageType = DamageType.Normal;
            projCont.explodeFxType = ObjectType.ExplodeSmallFX;
            projCont.explodeOnDestroy = true;
            projCont.ffSys = owner.ffSys;
            projCont.hasEntity = true;
            projCont.homing = true;
            projCont.impact = WeaponImpact.normal;
            projCont.piercing = Main.piercing;
            projCont.speed = Main.projectileSpeed;
            projCont.target = target;
            projCont.tDmg = new TDamage(stats.critchance, stats.critdamage, Main.armorPiercing + owner.stats.armorPenBonus, false, false);
            projCont.timeToDestroy = Main.range / Main.projectileSpeed;
            projCont.turnSpeed = Main.turnSpeed;

            if (f_ProjectileControl_entity == null || f_ProjectileControl_rb == null)
            {
                f_ProjectileControl_entity = typeof(ProjectileControl).GetField("entity", AccessTools.all);
                f_ProjectileControl_rb = typeof(ProjectileControl).GetField("rb", AccessTools.all);
            }
            f_ProjectileControl_rb.SetValue(projCont, this.go.GetComponent<Rigidbody>());
            f_ProjectileControl_entity.SetValue(projCont, this.go.AddComponent<Missle>());
            projCont.SetRbVelocity(baseVelocity);

            this.thruster.SetActive(true);
            this.audio.volume = SoundSys.SFXvolume;
            this.audio.Play();
            this.collider.enabled = true;
            projCont.Fire();
        }
    }
}
