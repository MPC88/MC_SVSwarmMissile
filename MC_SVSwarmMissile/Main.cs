using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace MC_SVSwarmMissile
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string pluginGuid = "mc.starvalor.swarmmissile";
        public const string pluginName = "SV Swarm Missile";
        public const string pluginVersion = "1.0.0";

        internal static int equipID = 45334;
        internal readonly static float rarityMod = 1.2f;
        internal readonly static float weaponDmgEffect = 3;
        internal readonly static float cooldown = 10f;
        internal readonly static float baseDmg = 50;
        internal readonly static float baseRadius = 200;
        internal readonly static float baseCritChance = 10;
        internal readonly static float baseCritDamage = 2;
        internal readonly static float projectileSpeed = 100;
        internal readonly static float aoe = 10;
        internal readonly static int piercing = 0;
        internal readonly static float armorPiercing = 0;
        internal readonly static float turnSpeed = 50;
        internal readonly static float range = 600;
        internal static GameObject mainMissileGO;

        private readonly static int shipID = 334;
        private readonly static int energyCost = 0;
        private readonly static float rarityCostMod = 1f;                
        private const string equipmentName = "Swarm Missile Pod";
        private readonly static string description = "Fire a missile pod containing 20 smaller warheads.  Small warheads will target all enemies in a radius around the cursor, seeking each valid target recursively.\n" +
                                           "<color=#808080>Target Selection Radius:</color> <color=#FFFFFF><RADIUS></color>\n" +
                                           "<color=#808080>Damage:</color> <color=#FFFFFF><DMG></color>\n" +
                                           "<color=#808080>Crit Chance:</color> <color=#FFFFFF><CRIT></color>\n" +
                                           "<color=#808080>Crit Damage:</color> <color=#FFFFFF><CRITDMG>x</color>\n" +
                                           "<color=#808080>Projectile Speed:</color> <color=#FFFFFF>" + projectileSpeed + "</color>\n" +
                                           "<color=#808080>Range:</color> <color=#FFFFFF>" + range + "</color>\n" +
                                           "Missiles have auto-targeting.";
        private static Sprite equipmentIcon;        
        private static GameObject buffGO;
        private static bool loaded = false;        

        public void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Main));
            Harmony.CreateAndPatchAll(typeof(AE_MCSwarmMissile));

            string pluginfolder = System.IO.Path.GetDirectoryName(GetType().Assembly.Location);
            string bundleName = "mc_svswarmmissile";
            AssetBundle assets = AssetBundle.LoadFromFile($"{pluginfolder}\\{bundleName}");
            equipmentIcon = assets.LoadAsset<Sprite>("Assets/_MyStuff/SwarmMissilePod/equipmenticon.png");
            mainMissileGO = assets.LoadAsset<GameObject>("Assets/_MyStuff/SwarmMissilePod/SwarmMissile.prefab");
        }

        [HarmonyPatch(typeof(DemoControl), "SpawnMainMenuBackground")]
        [HarmonyPostfix]
        private static void DemoControlSpawnMainMenuBackground_Post()
        {
            if (loaded)
                return;

            List<ShipBonus> sb = new List<ShipBonus>(ShipDB.GetModel(shipID).modelBonus);
            SB_BuiltInEquipment sbbie = ScriptableObject.CreateInstance<SB_BuiltInEquipment>();
            sbbie.equipmentID = equipID;
            sb.Add(sbbie);
            ShipDB.GetModel(shipID).modelBonus = sb.ToArray();
            loaded = true;
        }

        [HarmonyPatch(typeof(EquipmentDB), "LoadDatabaseForce")]
        [HarmonyPostfix]
        private static void EquipmentDBLoadDBForce_Post()
        {
            AccessTools.StaticFieldRefAccess<List<Equipment>>(typeof(EquipmentDB), "equipments").Add(CreateEquipment());
        }

        private static Equipment CreateEquipment()
        {
            Equipment equipment = ScriptableObject.CreateInstance<Equipment>();
            equipment.name = equipID + "." + equipmentName;
            equipment.id = equipID;
            equipment.refName = equipmentName;
            equipment.minShipClass = ShipClassLevel.Shuttle;
            equipment.activated = true;
            equipment.enableChangeKey = true;
            equipment.space = 10;
            equipment.energyCost = energyCost;
            equipment.energyCostPerShipClass = false;
            equipment.rarityCostMod = rarityCostMod;
            equipment.techLevel = 0;
            equipment.sortPower = 2;
            equipment.massChange = 0;
            equipment.type = EquipmentType.Device;
            equipment.effects = new List<Effect>() { new Effect() { type = 13, description = "", mod = 1f, value = weaponDmgEffect, uniqueLevel = 0 } };
            equipment.uniqueReplacement = false;
            equipment.rarityMod = rarityMod;
            equipment.sellChance = 0;
            equipment.repReq = new ReputationRequisite() { factionIndex = 0, repNeeded = 0 };
            equipment.dropLevel = DropLevel.DontDrop;
            equipment.lootChance = 0;
            equipment.spawnInArena = false;
            equipment.sprite = equipmentIcon;
            equipment.activeEquipmentIndex = equipID;
            equipment.defaultKey = KeyCode.Alpha3;
            equipment.requiredItemID = -1;
            equipment.requiredQnt = 0;
            equipment.equipName = equipmentName;
            equipment.description = description;
            equipment.craftingMaterials = null;
            if (buffGO == null)
                MakeBuffGO(equipment);
            equipment.buff = buffGO;

            return equipment;
        }

        private static void MakeBuffGO(Equipment equip)
        {
            buffGO = new GameObject { name = "SwarmMissile" };
            buffGO.AddComponent<BuffControl>();
            buffGO.GetComponent<BuffControl>().owner = null;
            buffGO.GetComponent<BuffControl>().activeEquipment = MakeActiveEquip(
                equip, null, equip.defaultKey, 1, 0);
        }

        private static AE_MCSwarmMissile MakeActiveEquip(Equipment equipment, SpaceShip ss, KeyCode key, int rarity, int qnt)
        {
            AE_MCSwarmMissile activeEquip = new AE_MCSwarmMissile
            {
                id = equipment.id,
                rarity = rarity,
                key = key,
                ss = ss,
                isPlayer = (ss != null && ss.CompareTag("Player")),
                equipment = equipment,
                qnt = qnt,
                active = false
            };
            return activeEquip;
        }

        [HarmonyPatch(typeof(AE_BuffBased), "ActivateDeactivate")]
        [HarmonyPrefix]
        internal static void ActivateDeactivate_Pre(ActiveEquipment __instance)
        {
            if (__instance == null || __instance.equipment == null ||
                __instance.equipment.id != equipID)
                return;

            if (buffGO == null)
                MakeBuffGO(__instance.equipment);

            __instance.equipment.buff = buffGO;
        }

        [HarmonyPatch(typeof(ActiveEquipment), "AddActivatedEquipment")]
        [HarmonyPrefix]
        private static bool ActiveEquipmentAdd_Pre(Equipment equipment, SpaceShip ss, KeyCode key, int rarity, int qnt, ref ActiveEquipment __result)
        {
            if (GameManager.instance != null && GameManager.instance.inGame &&
                equipment.id == equipID)
            {
                __result = MakeActiveEquip(equipment, ss, key, rarity, qnt);
                ss.activeEquips.Add(__result);
                __result.AfterConstructor();
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(EquipmentDB), nameof(EquipmentDB.GetEquipmentString))]
        [HarmonyPostfix]
        private static void EquipmentDBGetEquipmentString_Post(int id, int rarity, ref string __result)
        {
            if (id != Main.equipID)
                return;

            MissileStats stats = MissileStats.GetMissileStats(GameManager.instance.Player.GetComponent<SpaceShip>(), rarity);
            __result = __result.Replace("<RADIUS>", stats.radius.ToString());
            __result = __result.Replace("<DMG>", stats.damage.ToString());
            __result = __result.Replace("<CRIT>", stats.critchance.ToString());
            __result = __result.Replace("<CRITDMG>", stats.critdamage.ToString());
        }
    }
}
