﻿
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vheos.Mods.Core;

namespace Vheos.Mods.UNSIGHTED;
public class Guard : AMod
{
    // Section
    protected override string SectionOverride
    => Sections.BALANCE;
    protected override string Description =>
        "Mods related to the guarding mechanic" +
        "\n\nExamples:" +
        "\n• Change \"perfect\" and \"normal\" parry windows" +
        "\n• Guard longer by holding the button" +
        "\n• Guard without melee weapons" +
        "\n• Change stun damage multipliers" +
        "\n\nDictionary:" +
        "\n• Parry - \"perfect parry\", triggers when you guard early enough" +
        "\n• Deflect - \"normal parry\", triggers when you guard too late for parry" +
        "\n• Optional deflect - cancellable part of the guard action" +
        "\n• Guard - the whole action, consisting of the 3 parts above";

    // Settings
    private static ModSetting<int> _parryDuration;
    private static ModSetting<int> _deflectDuration;
    private static ModSetting<int> _optionalDeflectDuration;
    private static ModSetting<float> _parryHitboxGrowth;
    private static ModSetting<bool> _guardWithoutMeleeWeapon;
    private static ModSetting<bool> _parryWithoutMeleeWeapon;
    private static Dictionary<StunDamageType, ModSetting<int>> _stunDamageMultiplierByType;
    private static ModSetting<int> _guardStaminaCost;
    private static ModSetting<bool> _guardStaminaCostIsPercent;
    private static ModSetting<int> _parryStaminaGain;
    private static ModSetting<bool> _parryStaminaGainIsPercent;
    protected override void Initialize()
    {
        _parryDuration = CreateSetting(nameof(_parryDuration), 175, IntRange(0, 1000));
        _parryHitboxGrowth = CreateSetting(nameof(_parryHitboxGrowth), 3f, FloatRange(0f, 5f));
        _deflectDuration = CreateSetting(nameof(_deflectDuration), 325, IntRange(0, 1000));
        _optionalDeflectDuration = CreateSetting(nameof(_optionalDeflectDuration), 50, IntRange(0, 1000));
        _guardWithoutMeleeWeapon = CreateSetting(nameof(_guardWithoutMeleeWeapon), false);
        _parryWithoutMeleeWeapon = CreateSetting(nameof(_parryWithoutMeleeWeapon), true);
        _stunDamageMultiplierByType = new Dictionary<StunDamageType, ModSetting<int>>();
        _stunDamageMultiplierByType[StunDamageType.Axe] = CreateSetting(nameof(_stunDamageMultiplierByType) + StunDamageType.Axe, 300, IntRange(100, 500));
        _stunDamageMultiplierByType[StunDamageType.SwordAndShuriken] = CreateSetting(nameof(_stunDamageMultiplierByType) + StunDamageType.SwordAndShuriken, 500, IntRange(100, 500));
        _stunDamageMultiplierByType[StunDamageType.ParryMasterChip] = CreateSetting(nameof(_stunDamageMultiplierByType) + StunDamageType.ParryMasterChip, 150, IntRange(100, 500));
        _guardStaminaCost = CreateSetting(nameof(_guardStaminaCost), ORIGINAL_GUARD_STAMINA_COST, IntRange(0, 100));
        _guardStaminaCostIsPercent = CreateSetting(nameof(_guardStaminaCostIsPercent), false);
        _parryStaminaGain = CreateSetting(nameof(_parryStaminaGain), 100, IntRange(0, 100));
        _parryStaminaGainIsPercent = CreateSetting(nameof(_parryStaminaGainIsPercent), true);
    }
    protected override void SetFormatting()
    {
        _parryDuration.Format("Parry duration");
        _parryDuration.Description =
            "How quickly you need to react to perform a parry" +
            "\nHigher values will make parrying easier, but root you in place for longer" +
            "\n\nUnit: milliseconds";
        _deflectDuration.Format("Deflect duration");
        _deflectDuration.Description =
            "How long after parry window you can still deflect bullets and attacks" +
            "\nLower values will make guarding more responsive" +
            "\n\nUnit: milliseconds";
        _optionalDeflectDuration.Format("Max guard duration");
        _optionalDeflectDuration.Description =
            "How much longer you can deflect if you keep the button pressed" +
            "\n\nUnit: milliseconds";
        _parryHitboxGrowth.Format("Parry hitbox growth");
        _parryHitboxGrowth.Description =
            "How much bigger your hitbox gets during the parry window" +
            "\n\nUnit: Unity-defined units";

        _guardWithoutMeleeWeapon.Format("Guard without melee weapon");
        _guardWithoutMeleeWeapon.Description =
            "Allows you to guard even if you don't have any melee weapon equipped";
        using (Indent)
        {
            _parryWithoutMeleeWeapon.Format("can parry", _guardWithoutMeleeWeapon);
            _parryWithoutMeleeWeapon.Description =
                 "Allows you to not only deflect, but also parry without a melee weapon";
        }

        CreateHeader("Stun damage multiplier").Description =
            "How much more damage you deal when hitting a stunned enemy (after parry)" +
            "\n\nUnit: percent of original damage";
        using (Indent)
        {
            _stunDamageMultiplierByType[StunDamageType.Axe].Format("axe");
            _stunDamageMultiplierByType[StunDamageType.SwordAndShuriken].Format("sword & shuriken");
            _stunDamageMultiplierByType[StunDamageType.ParryMasterChip].Format("\"Parry Master\" chip");
            _stunDamageMultiplierByType[StunDamageType.ParryMasterChip].Description =
                "Stacks multiplicatively with sword/axe multiplier";
        }

        _guardStaminaCost.Format("Guard stamina cost");
        _guardStaminaCost.Description =
            "How much stamina you lose when you start guarding" +
            "\n\nUnit: stamina points, or percent of max stamina";
        using (Indent)
            _guardStaminaCostIsPercent.Format("percent of max stamina", _guardStaminaCost, t => t > 0);
        _parryStaminaGain.Format("Parry stamina gain");
        _parryStaminaGain.Description =
            "How much stamina you regain after a parry" +
            "\nSet to 0 to make parrying a little less spammable" +
            "\n\nUnit: stamina points, or percent of max stamina";
        using (Indent)
            _parryStaminaGainIsPercent.Format("percent of max stamina", _parryStaminaGain, t => t > 0);
    }
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(SettingsPreset.Vheos_CoopRebalance):
                ForceApply();
                _parryDuration.Value = 160;
                _deflectDuration.Value = 240;
                _optionalDeflectDuration.Value = 600;
                _parryHitboxGrowth.Value = 1f;
                _guardWithoutMeleeWeapon.Value = true;
                _parryWithoutMeleeWeapon.Value = false;
                _stunDamageMultiplierByType[StunDamageType.Axe].Value = 200;
                _stunDamageMultiplierByType[StunDamageType.SwordAndShuriken].Value = 300;
                _stunDamageMultiplierByType[StunDamageType.ParryMasterChip].Value = 150;
                _guardStaminaCost.Value = 6;
                _guardStaminaCostIsPercent.Value = false;
                _parryStaminaGain.Value = 0;
                _parryStaminaGainIsPercent.Value = false;
                break;
        }
    }

    // Privates
    private static bool PlayerHaveMeleeWeapon_Original(PlayerInfo player)
    {
        foreach (string text in GlobalGameData.instance.currentData.playerDataSlots[GlobalGameData.instance.loadedSlot].playersEquipData[player.playerNum].weapons)
            if (text != "Null"
            && UnsightedHelpers.instance.GetWeaponObject(text).weaponClass == EquipmentClass.Melee
            && UnsightedHelpers.instance.GetWeaponObject(text).meleeWeaponClass != MeleeWeaponClass.Shuriken)
                return true;
        return false;
    }
    private const int ORIGINAL_GUARD_STAMINA_COST = 6;
    private static readonly Dictionary<StunDamageType, float> ORIGINAL_STUN_DAMAGE_MULTIPLIERS_BY_TYPE = new()
    {
        [StunDamageType.Axe] = 3,
        [StunDamageType.SwordAndShuriken] = 5,
        [StunDamageType.ParryMasterChip] = 1.5f,
    };

    // Defines
    private enum StunDamageType
    {
        Axe,
        SwordAndShuriken,
        ParryMasterChip,
    }

    // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

    // Parry
    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.GuardCoroutine)), HarmonyPostfix]
    private static IEnumerator BasicCharacterController_GuardCoroutine_Post(IEnumerator original, BasicCharacterController __instance)
    {
        // Stamina cost
        float extraCost = 0f;

        if (_guardStaminaCost != ORIGINAL_GUARD_STAMINA_COST)
        {
            extraCost = _guardStaminaCost - ORIGINAL_GUARD_STAMINA_COST;
            if (_guardStaminaCostIsPercent)
                extraCost *= __instance.myInfo.totalStamina / 100f;
        }

        if (extraCost > 0)
            __instance.DrainStamina(+extraCost);
        else if (extraCost < 0)
            __instance.FillStamina(-extraCost);

        // Coroutine
        original.MoveNext();
        __instance.parryActive = _parryWithoutMeleeWeapon || PlayerHaveMeleeWeapon_Original(__instance.myInfo);

        __instance.parryCollider.SetActive(__instance.parryActive);
        __instance.parryCollider.GetComponent<BoxCollider2D>().size = new Vector2(1.1f, 0.6f) + _parryHitboxGrowth.Value.ToVector2();
        if (_parryDuration > 0)
            yield return gameTime.WaitForSeconds(_parryDuration / 1000f);

        if (!__instance.holdingObject)
        {
            original.MoveNext();
            if (_deflectDuration > 0)
                yield return gameTime.WaitForSeconds(_deflectDuration / 1000f);

            original.MoveNext();
            if (_optionalDeflectDuration > 0
            && ButtonSystem.GetKey(GlobalInputManager.instance.inputData.GetInputProfile(__instance.myInfo.playerNum).guard))
                yield return gameTime.WaitForSeconds(_optionalDeflectDuration / 1000f);
        }

        while (original.MoveNext())
            yield return original.Current;
    }

    [HarmonyPatch(typeof(PlayerInfo), nameof(PlayerInfo.PlayerHaveMeleeWeapon)), HarmonyPrefix]
    private static bool PlayerInfo_PlayerHaveMeleeWeapon_Pre(PlayerInfo __instance, ref bool __result)
    {
        if (!_guardWithoutMeleeWeapon)
            return true;

        __result = true;
        return false;
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.FillStamina)), HarmonyPrefix]
    private static void BasicCharacterController_FillStamina_Pre(BasicCharacterController __instance, ref float value)
    {
        if (!__instance.parryActive)
            return;

        value = _parryStaminaGain;
        if (_parryStaminaGainIsPercent)
            value *= __instance.myInfo.totalStamina / 100f;
    }

    [HarmonyPatch(typeof(EnemyHitBox), nameof(EnemyHitBox.ApplyCombo)), HarmonyPostfix]
    private static void EnemyHitBox_ApplyCombo_Post(EnemyHitBox __instance, ref int __result)
    {
        if (!__instance.stunned
        || !__instance.hitCharacterController.TryNonNull(out var hitCharacterController))
            return;

        MeleeWeaponClass weaponType = UnsightedHelpers.instance.GetWeaponObject(hitCharacterController.myInfo.GetCurrentEquippedWeapon()).meleeWeaponClass;
        bool hasParryMasterChipEquipped = UnsightedHelpers.instance.NumberOfChipsEquipped("ParryMasterChip", hitCharacterController.myInfo.playerNum) > 0;
        float originalMultiplier = weaponType == MeleeWeaponClass.Axe
                                 ? ORIGINAL_STUN_DAMAGE_MULTIPLIERS_BY_TYPE[StunDamageType.Axe]
                                 : ORIGINAL_STUN_DAMAGE_MULTIPLIERS_BY_TYPE[StunDamageType.SwordAndShuriken];
        float customMultiplier = weaponType == MeleeWeaponClass.Axe
                               ? _stunDamageMultiplierByType[StunDamageType.Axe] / 100f
                               : _stunDamageMultiplierByType[StunDamageType.SwordAndShuriken] / 100f;
        if (hasParryMasterChipEquipped)
        {
            originalMultiplier *= ORIGINAL_STUN_DAMAGE_MULTIPLIERS_BY_TYPE[StunDamageType.ParryMasterChip];
            customMultiplier *= _stunDamageMultiplierByType[StunDamageType.ParryMasterChip] / 100f;
        }

        __result = __result.Mul(customMultiplier).Div(originalMultiplier).Round();
    }
}