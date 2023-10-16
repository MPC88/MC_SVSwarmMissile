using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MC_SVSwarmMissile
{
    public class AE_MCSwarmMissile : AE_BuffBased
    {
        protected override bool showBuffIcon
        {
            get
            {
                return this.isPlayer;
            }
        }

        private MissileStats instanceStats = null;
        private List<Transform> instanceTargets = null;

        public AE_MCSwarmMissile()
        {
            this.targetIsSelf = true;
            this.saveState = true;
            this.saveCooldownID = this.id;
            this.cooldownTime = Main.cooldown;
        }

        public override void ActivateDeactivate(bool shiftPressed, Transform target)
        {
            // Get stats and target list
            instanceStats = MissileStats.GetMissileStats(this.ss, this.rarity);
            instanceTargets = GetTargets(instanceStats, this.ss);
            if (instanceTargets == null)
            {
                InfoPanelControl.inst.ShowWarning("No valid targets.", 1, false);
                return;
            }

            this.startEnergyCost = this.equipment.energyCost;
            base.ActivateDeactivate(shiftPressed, target);
        }

        public override void AfterActivate()
        {
            base.AfterActivate();

            // Spawn main missile
            GameObject instanceGO = GameObject.Instantiate(Main.mainMissileGO, this.ss.transform.position, this.ss.transform.rotation);
            SwarmMissileController instanceController = instanceGO.AddComponent<SwarmMissileController>();
            instanceController.stats = instanceStats;
            instanceController.owner = this.ss;
            instanceController.targets = instanceTargets;
            int speed = 100;
            instanceController.carrierRb = instanceGO.transform.GetChild(0).GetComponent<Rigidbody>();
            instanceController.carrierRb.AddForce(instanceGO.transform.forward * speed);
            instanceGO.transform.GetChild(1).GetComponent<Rigidbody>().AddForce(instanceGO.transform.forward * speed);
            instanceGO.transform.GetChild(2).GetComponent<Rigidbody>().AddForce(instanceGO.transform.forward * speed);
            instanceGO.transform.GetChild(3).GetComponent<Rigidbody>().AddForce(instanceGO.transform.forward * speed);
            base.ActivateDeactivate(false, null);
        }

        private List<Transform> GetTargets(MissileStats stats, SpaceShip owner)
        {
            Vector3 mousePos = (Vector3)typeof(PlayerControl).GetField("mousePosition", AccessTools.all).GetValue(PlayerControl.inst);
            Collider[] colliders = Physics.OverlapSphere(mousePos, stats.radius, 8704);

            if (colliders.Length == 0)
                return null;

            colliders = colliders.OrderBy(c => (mousePos - c.transform.position).sqrMagnitude).ToArray();
            List<Transform> targets = new List<Transform>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Entity entity = ((!colliders[i].CompareTag("Collider")) ? colliders[i].GetComponent<Entity>() : colliders[i].GetComponent<ColliderControl>().ownerEntity);

                if (entity && entity != owner && owner.ffSys.TargetIsEnemy(entity.ffSys))
                    targets.Add(entity.transform);

                if (targets.Count == 20)
                    break;
            }

            if (targets.Count > 0)
                return targets;
            else
                return null;
        }

        [HarmonyPatch(typeof(SpaceShip), "CalculateShipStats")]
        [HarmonyPostfix]
        private static void SpaceShipCalculateShipStats_Post(SpaceShip __instance)
        {
            if (__instance == null || __instance.shipData == null || __instance.shipData.builtInData == null)
                return;

            foreach (BuiltInEquipmentData bied in __instance.shipData.builtInData)
            {
                if (bied.equipmentID == Main.equipID)
                {
                    __instance.dmgBonus += (Main.weaponDmgEffect / 100) * (Main.rarityMod * bied.rarity);
                    break;
                }
            }
        }
    }
}
