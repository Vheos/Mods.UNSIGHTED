namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections;
    using HarmonyLib;
    using UnityEngine;
    using Tools.ModdingCore;
    using Tools.Extensions.Math;
    using Vheos.Tools.Extensions.General;
    using Vheos.Tools.Extensions.UnityObjects;

    public class Combat : AMod
    {
        // Settings
        static private ModSetting<float> _staminaChargeGain;
        static private ModSetting<float> _staminaChargeDuration;
        static private ModSetting<bool> _allowCancellingStaminaCharge;

        static private ModSetting<float> _enemyHPMultiplier;
        static private ModSetting<float> _enemyBossHPMultiplier;
        static private ModSetting<Vector2> _attackRhythmRandomRange;
        override protected void Initialize()
        {
            _staminaChargeGain = CreateSetting(nameof(_staminaChargeGain), 1f, FloatRange(0f, 1f));
            _staminaChargeDuration = CreateSetting(nameof(_staminaChargeDuration), ORIGINAL_STAMINA_CHARGE_DURATION, FloatRange(0f, 2f));
            _allowCancellingStaminaCharge = CreateSetting(nameof(_allowCancellingStaminaCharge), true);

            _enemyHPMultiplier = CreateSetting(nameof(_enemyHPMultiplier), 1f, FloatRange(0.5f, 2f));
            _enemyBossHPMultiplier = CreateSetting(nameof(_enemyBossHPMultiplier), 1f, FloatRange(0.5f, 2f));
            _attackRhythmRandomRange = CreateSetting(nameof(_attackRhythmRandomRange), new Vector2(0.75f, 0.75f));


        }
        override protected void SetFormatting()
        {
            _staminaChargeGain.Format("Stamina rush percent gain");
            _staminaChargeDuration.Format("Stamina Rush duration");
            _allowCancellingStaminaCharge.Format("Disable Stamina Rush cancelling");

            _enemyHPMultiplier.Format("Enemy HP multiplier");
            _enemyBossHPMultiplier.Format("Boss HP multiplier (extra)");
            _attackRhythmRandomRange.Format("Attack rhythm random range");


        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(Preset.Coop_NewGameExtra_HardMode):
                    ForceApply();
                    _staminaChargeGain.Value = 0.5f;
                    _staminaChargeDuration.Value = 0.66f;
                    _allowCancellingStaminaCharge.Value = true;
                    _enemyHPMultiplier.Value = 5 / 3f;
                    _enemyBossHPMultiplier.Value = 1.2f;
                    _attackRhythmRandomRange.Value = new Vector2(0f, 0.75f);
                    break;
            }
        }
        override protected string Description =>
            "Mods related to both players' and enemies' combat mechanics\n\n" +
            "Examples:\n" +
            "• Change camera zoom to see more\n" +
            "• Enable co-op screen stretching\n" +
            "• Put an end to player 2's oppression";

        // Privates
        private const float ORIGINAL_STAMINA_CHARGE_DURATION = 0.66f;
        static private bool PlayerHaveMeleeWeapon_Original(PlayerInfo player)
        {
            foreach (string text in PseudoSingleton<GlobalGameData>.instance.currentData.playerDataSlots[PseudoSingleton<GlobalGameData>.instance.loadedSlot].playersEquipData[player.playerNum].weapons)
                if (text != "Null"
                && PseudoSingleton<Helpers>.instance.GetWeaponObject(text).weaponClass == EquipmentClass.Melee
                && PseudoSingleton<Helpers>.instance.GetWeaponObject(text).meleeWeaponClass != MeleeWeaponClass.Shuriken)
                    return true;
            return false;
        }

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Enemy HP
        [HarmonyPatch(typeof(Atributes), nameof(Atributes.OnEnable)), HarmonyPrefix]
        static private bool Atributes_OnEnable_Pre(Atributes __instance)
        {
            if (__instance.myOwner == null && __instance.transform.parent != null)
                __instance.myOwner = __instance.transform.parent.GetComponent<SortingObject>();

            if (!__instance.appliedLifeDifficulty)
            {
                float multiplier = _enemyHPMultiplier;
                if (__instance.TryGetComponent<EnemyHitBox>(out var enemyHitBox)
                && enemyHitBox.bossBar)
                    multiplier *= _enemyBossHPMultiplier;

                __instance.totalLife = __instance.totalLife.Mul(multiplier).Round();
                __instance.appliedLifeDifficulty = true;
            }

            return false;
        }

        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.StaminaChargeCoroutine)), HarmonyPostfix]
        static private IEnumerator BasicCharacterController_StaminaChargeCoroutine_Post(IEnumerator original, BasicCharacterController __instance)
        {
            float frameDuration = _staminaChargeDuration / 10;

            original.MoveNext();
            __instance.myAnimations.PlayAnimation("StaminaCharge", 1, 0, _staminaChargeDuration / ORIGINAL_STAMINA_CHARGE_DURATION, false, true);
            if (frameDuration > 0)
                yield return new WaitForSeconds(frameDuration * 2);

            original.MoveNext();
            __instance.staminaChargeTime = 0f;
            if (frameDuration > 0)
                yield return new WaitForSeconds(frameDuration * 2);

            original.MoveNext();
            __instance.staminaChargeEffect = _allowCancellingStaminaCharge;
            if (frameDuration > 0)
                yield return new WaitForSeconds(frameDuration * 6);
            __instance.staminaChargeEffect = false;

            while (original.MoveNext())
                yield return original.Current;
        }

        [HarmonyPatch(typeof(EnemyController), nameof(EnemyController.AttackCoroutine)), HarmonyPostfix]
        static private IEnumerator EnemyController_AttackCoroutine_Post(IEnumerator original, EnemyController __instance, bool dontRegisterAttack, bool waitForAttackRythm)
        {
            if (waitForAttackRythm)
            {
                float rhythmInterval = UnityEngine.Random.Range(_attackRhythmRandomRange.Value.x, _attackRhythmRandomRange.Value.y);
                while (Time.time - EnemyController.lastAttackTime < rhythmInterval)
                    yield return new WaitForSeconds(0.1f);
            }

            float previousLastAttackTime = EnemyController.lastAttackTime;
            EnemyController.lastAttackTime = 0f;
            original.MoveNext();
            if (dontRegisterAttack)
                EnemyController.lastAttackTime = previousLastAttackTime;
            yield return original.Current;

            while (original.MoveNext())
                yield return original.Current;
        }


        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.FillStamina)), HarmonyPrefix]
        static private void BasicCharacterController_FillStamina_Pre(BasicCharacterController __instance, ref float value)
        {
            if (!__instance.staminaChargeEffect)
                return;

            value = _staminaChargeGain * __instance.myInfo.totalStamina;
        }
    }
}