
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vheos.Helpers.Dump;
using Vheos.Helpers.RNG;
using Vheos.Mods.Core;

namespace Vheos.Mods.UNSIGHTED;
public class Debug : AMod, IUpdatable
{

    protected override void Initialize()
    {
        ForceApply();
    }
    protected override void SetFormatting()
    { }

    public void OnUpdate()
    {
        if (!Input.GetKeyDown(KeyCode.Backslash))
            return;

        if (DropControllers.IsEmpty())
        {
            Log.Debug("Loading Scenes...");
            foreach (var area in Lists.instance.areaDatabase.areas)
                foreach (var room in area.roomsDatabase.availableRooms)
                    SceneManager.LoadScene(room.sceneName);
        }
        else
        {
            Log.Debug("Dumping DropTables...");
            StringBuilder sb = new();
            DropControllers.Sort((a, b) => a.enemyNameID.CompareTo(b.enemyNameID));

            foreach (var dropController in DropControllers)
            {
                sb.Append(dropController.enemyNameID);
                sb.Append(dropController.itemList.Count);
                sb.Append('\t');
                foreach (var item in dropController.itemList)
                {
                    sb.Append(item);
                    sb.Append('\t');
                }

                sb.AppendLine();
            }

            Log.Debug(sb.ToString());
        }
    }

    public static List<DropController> DropControllers = new();

    [HarmonyPatch(typeof(EnemyController), nameof(EnemyController.OnEnable)), HarmonyPostfix]
    private static void EnemyController_OnEnable_Post(EnemyController __instance)
        => DropControllers.Add(__instance.dropController);

    [HarmonyPatch(typeof(Lists), nameof(Lists.Start)), HarmonyPostfix]
    private static void Lists_Start_Post(Lists __instance)
    {
        return;

        StringBuilder sb = new();

        sb.AppendLine($"{nameof(ItemDatabase)}");
        sb.AppendLine(typeof(ItemObject).Dump(data: MemberData.Types));
        sb.AppendLine(typeof(ItemObject).Dump());
        foreach (var obj in __instance.itemDatabase.itemList)
            sb.AppendLine(obj.Dump());

        sb.AppendLine($"{nameof(CogsDatabase)}");
        sb.AppendLine(typeof(CogObject).Dump(data: MemberData.Types));
        sb.AppendLine(typeof(CogObject).Dump());
        foreach (var obj in __instance.cogsDatabase.cogList)
            sb.AppendLine(obj.Dump());

        sb.AppendLine($"{nameof(ChipDatabase)}");
        sb.AppendLine(typeof(ChipObject).Dump(data: MemberData.Types));
        sb.AppendLine(typeof(ChipObject).Dump());
        foreach (var obj in __instance.chipDatabase.chipList)
            sb.AppendLine(obj.Dump());

        sb.AppendLine($"{nameof(WeaponDatabase)}");
        sb.AppendLine(typeof(WeaponObject).Dump(data: MemberData.Types));
        sb.AppendLine(typeof(WeaponObject).Dump());
        foreach (var obj in __instance.weaponDatabase.weaponList)
            sb.AppendLine(obj.Dump());

        sb.AppendLine($"{nameof(ArmorDatabase)}");
        sb.AppendLine(typeof(ArmorObject).Dump(data: MemberData.Types));
        sb.AppendLine(typeof(ArmorObject).Dump());
        foreach (var obj in __instance.armorDatabase.armorList)
            sb.AppendLine(obj.Dump());

        sb.AppendLine($"{nameof(NPCDatabase)}");
        sb.AppendLine(typeof(NPCObject).Dump(data: MemberData.Types));
        sb.AppendLine(typeof(NPCObject).Dump());
        foreach (var obj in __instance.npcDatabase.npcList)
            sb.AppendLine(obj.Dump());

        sb.AppendLine($"{nameof(AreasDatabase)}");
        sb.AppendLine(typeof(AreaDescription).Dump(data: MemberData.Types));
        sb.AppendLine(typeof(AreaDescription).Dump());
        foreach (var obj in __instance.areaDatabase.areas)
            sb.AppendLine(obj.Dump());

        sb.AppendLine($"{nameof(RoomsDatabase)}");
        sb.AppendLine(typeof(RoomDescription).Dump(data: MemberData.Types));
        sb.AppendLine(typeof(RoomDescription).Dump());
        foreach (var area in __instance.areaDatabase.areas)
            foreach (var obj in area.roomsDatabase.availableRooms)
                sb.AppendLine(obj.Dump());

        Log.Debug(sb.ToString());
    }


}