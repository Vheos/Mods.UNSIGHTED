namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using HarmonyLib;
    using Tools.ModdingCore;
    using Tools.Extensions.Math;
    using Tools.Extensions.General;
    using Tools.Extensions.Collections;

    public class ChipsCogs : AMod
    {
        // Settings
        static private ModSetting<int> _startingChipSlots;
        static private ModSetting<int> _linearChipSlotCosts;
        static private ModSetting<int> _cogSlots;
        static private ModSetting<int> _maxActiveCogTypes;
        override protected void Initialize()
        {
            _startingChipSlots = CreateSetting(nameof(_startingChipSlots), 3, IntRange(0, 14));
            _linearChipSlotCosts = CreateSetting(nameof(_linearChipSlotCosts), -1, IntRange(-1, 2000));
            _cogSlots = CreateSetting(nameof(_cogSlots), 4, IntRange(0, 6));
            _maxActiveCogTypes = CreateSetting(nameof(_maxActiveCogTypes), 4, IntRange(1, 6));

            // Events
            _linearChipSlotCosts.AddEvent(() => TrySetLinearChipSlotCosts(PseudoSingleton<LevelDatabase>.instance));
        }
        override protected void SetFormatting()
        {
            _startingChipSlots.Format("Starting chip slots");
            _startingChipSlots.Description =
                "How many free chip slots you start the game with";
            _linearChipSlotCosts.Format("Linear chip slot costs");
            _linearChipSlotCosts.Description =
                "How much more each consecutive chip slot costs" +
                "\nThe first non-starting slot costs exactly this value, the second one costs twice as much, the third one three times as much, and so on" +
                "\nSet to 0 to make all slots free to unlock" +
                "\nSet to -1 to use the original costs";
            _cogSlots.Format("Cog slots");
            _cogSlots.Description =
                "How many cog slots you have";
            _maxActiveCogTypes.Format("Max active cog types");
            _maxActiveCogTypes.Description =
                "How many cogs of different types you can have activated at the same time";
        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(Preset.Vheos_HardMode):
                    ForceApply();
                    _startingChipSlots.Value = 0;
                    _linearChipSlotCosts.Value = 750;
                    _cogSlots.Value = 6;
                    _maxActiveCogTypes.Value = 1;
                    break;
            }
        }
        override protected string ModName
        => "Chips & Cogs";
        override protected string Description =>
            "Mods related to the chip and cog systems" +
            "\n\nExamples:" +
            "\n• Change starting chip slots and unlock costs" +
            "\n• Change number of cog slots" +
            "\n• Limit number of active cog types";

        // Privates
        static private void TrySetLinearChipSlotCosts(LevelDatabase levelDatabase)
        {
            if (levelDatabase == null
            || _linearChipSlotCosts < 0)
                return;

            for (int i = 0; i <= 15; i++)
                levelDatabase.levelUpCost[i] = i.Sub(_startingChipSlots).Add(1).Mul(_linearChipSlotCosts).ClampMin(0);
        }
        static private Vector2 SetAnchoredPosition(Component component, float placementX, float placementY)
        => component.GetComponent<RectTransform>().anchoredPosition
            = new Vector2(placementX.MapClamped(-1, +1, -110, +110), placementY.MapClamped(-1, +1, -10, +60));

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Chips
        [HarmonyPatch(typeof(GlobalGameData), nameof(GlobalGameData.CreateDefaultDataSlot)), HarmonyPostfix]
        static private void GlobalGameData_CreateDefaultDataSlot_Post(GlobalGameData __instance, int slotNumber)
        => __instance.currentData.playerDataSlots[slotNumber].chipSlots = _startingChipSlots;

        [HarmonyPatch(typeof(LevelDatabase), nameof(LevelDatabase.OnEnable)), HarmonyPostfix]
        static private void LevelDatabase_OnEnable_Post(LevelDatabase __instance)
        => TrySetLinearChipSlotCosts(__instance);

        // Cogs
        [HarmonyPatch(typeof(Helpers), nameof(Helpers.GetMaxCogs)), HarmonyPrefix]
        static private bool Helpers_GetMaxCogs_Pre(Helpers __instance, ref int __result)
        {
            __result = _cogSlots;
            return false;
        }

        [HarmonyPatch(typeof(CogButton), nameof(CogButton.OnClick)), HarmonyPrefix]
        static private void CogButton_OnClick_Pre(CogButton __instance)
        {
            if (_maxActiveCogTypes >= _cogSlots
            || !__instance.buttonActive
            || !__instance.currentBuff.TryNonNull(out var buff)
            || buff.active
            || __instance.destroyButton)
                return;

            List<PlayerBuffs> buffsList = TButtonNavigation.myPlayer == 0
                                       ? PseudoSingleton<Helpers>.instance.GetPlayerData().p1Buffs
                                       : PseudoSingleton<Helpers>.instance.GetPlayerData().p2Buffs;
            if (buffsList.Any(t => t.active && t.buffType == buff.buffType))
                return;

            var activeCogTypes = new HashSet<PlayerBuffTypes> { buff.buffType };
            foreach (var otherButton in __instance.myCogsPopup.cogButtons)
                if (otherButton != __instance
                && otherButton.buttonActive
                && otherButton.currentBuff.TryNonNull(out var otherBuff)
                && otherBuff.active
                && !activeCogTypes.Contains(otherBuff.buffType))
                    if (activeCogTypes.Count >= _maxActiveCogTypes)
                        otherButton.OnClick();
                    else
                        activeCogTypes.Add(otherBuff.buffType);
        }

        [HarmonyPatch(typeof(CogsPopup), nameof(CogsPopup.OnEnable)), HarmonyPrefix]
        static private bool CogsPopup_OnEnable_Pre(CogsPopup __instance)
        {
            if (!__instance.alreadyStarted)
            {
                int maxCogs = PseudoSingleton<Helpers>.instance.GetMaxCogs();
                var unusedSlots = new HashSet<int>();
                switch (maxCogs)
                {
                    case 0:
                        unusedSlots.Add(0, 1, 2, 3, 4, 5);
                        break;
                    case 1:
                        SetAnchoredPosition(__instance.cogButtons[4], 0, 0);
                        unusedSlots.Add(0, 1, 2, 3, 5);
                        break;
                    case 2:
                        SetAnchoredPosition(__instance.cogButtons[4], 0, -1);
                        SetAnchoredPosition(__instance.cogButtons[1], 0, +1);
                        unusedSlots.Add(0, 2, 3, 5);
                        break;
                    case 3:
                        SetAnchoredPosition(__instance.cogButtons[3], -1 / 2f, -1);
                        SetAnchoredPosition(__instance.cogButtons[5], +1 / 2f, -1);
                        SetAnchoredPosition(__instance.cogButtons[1], 0, +1);
                        unusedSlots.Add(0, 2, 4);
                        break;
                    case 4:
                        SetAnchoredPosition(__instance.cogButtons[3], -1 / 2f, -1);
                        SetAnchoredPosition(__instance.cogButtons[5], +1 / 2f, -1);
                        SetAnchoredPosition(__instance.cogButtons[0], -1 / 2f, +1);
                        SetAnchoredPosition(__instance.cogButtons[2], +1 / 2f, +1);
                        unusedSlots.Add(1, 4);
                        break;
                    case 5:
                        SetAnchoredPosition(__instance.cogButtons[3], -1, -1);
                        SetAnchoredPosition(__instance.cogButtons[4], 0, -1);
                        SetAnchoredPosition(__instance.cogButtons[5], +1, -1);
                        SetAnchoredPosition(__instance.cogButtons[0], -1 / 2f, +1);
                        SetAnchoredPosition(__instance.cogButtons[2], +1 / 2f, +1);
                        unusedSlots.Add(1);
                        break;
                    case 6:
                        SetAnchoredPosition(__instance.cogButtons[3], -1, -1);
                        SetAnchoredPosition(__instance.cogButtons[4], 0, -1);
                        SetAnchoredPosition(__instance.cogButtons[5], +1, -1);
                        SetAnchoredPosition(__instance.cogButtons[0], -1, +1);
                        SetAnchoredPosition(__instance.cogButtons[1], 0, +1);
                        SetAnchoredPosition(__instance.cogButtons[2], +1, +1);
                        break;
                }

                foreach (var slot in unusedSlots)
                    __instance.cogButtons[slot].gameObject.SetActive(false);
                foreach (var slot in unusedSlots)
                    __instance.cogButtons.Remove(__instance.cogButtons[slot]);
            }

            if (__instance.alreadyStarted)
                for (int i = 0; i < __instance.cogButtons.Count; i++)
                    if (TButtonNavigation.myPlayer == 0 && __instance.cogButtons[i].buttonActive
                    && !PseudoSingleton<Helpers>.instance.GetPlayerData().p1Buffs.Contains(__instance.cogButtons[i].currentBuff)
                    || TButtonNavigation.myPlayer == 1 && __instance.cogButtons[i].buttonActive
                    && !PseudoSingleton<Helpers>.instance.GetPlayerData().p2Buffs.Contains(__instance.cogButtons[i].currentBuff))
                        __instance.cogButtons[i].ShowCogAsInactive();

            return false;
        }
    }
}