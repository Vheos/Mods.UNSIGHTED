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
        static private ModSetting<float> _parryDuration;
        static private ModSetting<float> _guardMinDuration;
        static private ModSetting<float> _guardMaxDuration;
        static private ModSetting<float> _parryStaminaGain;
        static private ModSetting<bool> _allowGuardWithoutMeleeWeapon;
        static private ModSetting<bool> _allowParryWithoutMeleeWeapon;

        static private ModSetting<float> _staminaRushPercentGain;
        static private ModSetting<float> _staminaRushDuration;
        static private ModSetting<bool> _disableStaminaRushCancelling;

        static private ModSetting<float> _enemyHPMultiplier;
        static private ModSetting<float> _enemyBossHPMultiplier;
        static private ModSetting<Vector2> _attackRhythmRandomRange;

        static private ModSetting<bool> _hideCriticalHitText;
        static private ModSetting<bool> _hidePerfectParryText;

        override protected void Initialize()
        {
            _parryDuration = CreateSetting(nameof(_parryDuration), ORIGINAL_PARRY_DURATION, FloatRange(0f, 1f));
            _guardMinDuration = CreateSetting(nameof(_guardMinDuration), ORIGINAL_GUARD_MIN_DURATION, FloatRange(0, 1f));
            _guardMaxDuration = CreateSetting(nameof(_guardMaxDuration), ORIGINAL_GUARD_MAX_DURATION - ORIGINAL_GUARD_MIN_DURATION, FloatRange(0, 1f));
            _allowGuardWithoutMeleeWeapon = CreateSetting(nameof(_allowGuardWithoutMeleeWeapon), false);
            _allowParryWithoutMeleeWeapon = CreateSetting(nameof(_allowParryWithoutMeleeWeapon), false);
            _parryStaminaGain = CreateSetting(nameof(_parryStaminaGain), 100f, FloatRange(0f, 100f));

            _staminaRushPercentGain = CreateSetting(nameof(_staminaRushPercentGain), 1f, FloatRange(0f, 1f));
            _staminaRushDuration = CreateSetting(nameof(_staminaRushDuration), ORIGINAL_STAMINA_RUSH_DURATION, FloatRange(0f, 2f));
            _disableStaminaRushCancelling = CreateSetting(nameof(_disableStaminaRushCancelling), false);
            _enemyHPMultiplier = CreateSetting(nameof(_enemyHPMultiplier), 1f, FloatRange(0.5f, 2f));
            _enemyBossHPMultiplier = CreateSetting(nameof(_enemyBossHPMultiplier), 1f, FloatRange(0.5f, 2f));
            _attackRhythmRandomRange = CreateSetting(nameof(_attackRhythmRandomRange), new Vector2(0.75f, 0.75f));

            _hideCriticalHitText = CreateSetting(nameof(_hideCriticalHitText), false);
            _hidePerfectParryText = CreateSetting(nameof(_hidePerfectParryText), false);

            _guardMinDuration.AddEvent(() => _guardMaxDuration.SetSilently(_guardMaxDuration.Value.ClampMin(_guardMinDuration)));
            _guardMaxDuration.AddEvent(() => _guardMinDuration.SetSilently(_guardMinDuration.Value.ClampMax(_guardMaxDuration)));
        }
        override protected void SetFormatting()
        {
            _parryDuration.Format("Parry duration");
            _guardMinDuration.Format("Min guard duration");
            _guardMaxDuration.Format("Max guard duration");
            _allowGuardWithoutMeleeWeapon.Format("Allow guarding with guns");
            _allowParryWithoutMeleeWeapon.Format("Allow parrying with guns");
            _parryStaminaGain.Format("Parry stamina gain");

            _staminaRushPercentGain.Format("Stamina rush percent gain");
            _staminaRushDuration.Format("Stamina Rush duration");
            _disableStaminaRushCancelling.Format("Disable Stamina Rush cancelling");
            _enemyHPMultiplier.Format("Enemy HP multiplier");
            _enemyBossHPMultiplier.Format("Boss HP multiplier (extra)");
            _attackRhythmRandomRange.Format("Attack rhythm random range");

            _hideCriticalHitText.Format("Hide \"CRITICAL!\" text");
            _hidePerfectParryText.Format("Hide \"PERFECT!\" text");
        }

        // Privates
        private const float ORIGINAL_PARRY_DURATION = 0.175f;
        private const float ORIGINAL_GUARD_MIN_DURATION = 0.325f;
        private const float ORIGINAL_GUARD_MAX_DURATION = 0.375f;
        private const float ORIGINAL_STAMINA_RUSH_DURATION = 0.66f;
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

        // Parry
        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.GuardCoroutine)), HarmonyPostfix]
        static private IEnumerator BasicCharacterController_GuardCoroutine_Post(IEnumerator original, BasicCharacterController __instance)
        {
            bool hasMeleeWeapon = PlayerHaveMeleeWeapon_Original(__instance.myInfo);

            original.MoveNext();
            __instance.parryActive = hasMeleeWeapon || _allowParryWithoutMeleeWeapon;
            if (_parryDuration > 0f)
                yield return gameTime.WaitForSeconds(_parryDuration);

            if (!__instance.holdingObject)
            {
                original.MoveNext();
                if (_guardMinDuration > 0f)
                    yield return gameTime.WaitForSeconds(_guardMinDuration);

                original.MoveNext();
                float guardOptionalDuration = _guardMaxDuration - _guardMinDuration;
                if (guardOptionalDuration > 0f
                && ButtonSystem.GetKey(PseudoSingleton<GlobalInputManager>.instance.inputData.GetInputProfile(__instance.myInfo.playerNum).guard))
                    yield return gameTime.WaitForSeconds(guardOptionalDuration);
            }

            while (original.MoveNext())
                yield return original.Current;
        }

        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.FillStamina)), HarmonyPrefix]
        static private bool BasicCharacterController_FillStamina_Pre(BasicCharacterController __instance, ref float value)
        {
            if (__instance.ridingSpinner)
                return false;

            if (__instance.parryActive)
                value = _parryStaminaGain;
            else if (__instance.staminaChargeEffect)
                value = _staminaRushPercentGain * __instance.myInfo.totalStamina;

            float previousStamina = __instance.myInfo.currentStamina;
            __instance.myInfo.currentStamina += value;
            __instance.myInfo.currentStamina.SetClampMax(__instance.myInfo.totalStamina);
            if (__instance.myInfo.currentStamina != previousStamina)
                __instance.lastStaminaRechargeTime = UnityEngine.Time.time;

            return false;
        }

        [HarmonyPatch(typeof(PlayerInfo), nameof(PlayerInfo.PlayerHaveMeleeWeapon)), HarmonyPrefix]
        static private bool PlayerInfo_PlayerHaveMeleeWeapon_Pre(PlayerInfo __instance, ref bool __result)
        {
            if (!_allowGuardWithoutMeleeWeapon)
                return true;

            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.StaminaChargeCoroutine)), HarmonyPostfix]
        static private IEnumerator BasicCharacterController_StaminaChargeCoroutine_Post(IEnumerator original, BasicCharacterController __instance)
        {
            float frameDuration = _staminaRushDuration / 10;

            original.MoveNext();
            __instance.myAnimations.PlayAnimation("StaminaCharge", 1, 0, _staminaRushDuration / ORIGINAL_STAMINA_RUSH_DURATION, false, true);
            if (frameDuration > 0)
                yield return new WaitForSeconds(frameDuration * 2);

            original.MoveNext();
            __instance.staminaChargeTime = 0f;
            if (frameDuration > 0)
                yield return new WaitForSeconds(frameDuration * 2);

            original.MoveNext();
            __instance.staminaChargeEffect = _disableStaminaRushCancelling;
            if (frameDuration > 0)
                yield return new WaitForSeconds(frameDuration * 6);
            __instance.staminaChargeEffect = false;

            while (original.MoveNext())
                yield return original.Current;
        }

        // Hide text
        [HarmonyPatch(typeof(EnemyHitBox), nameof(EnemyHitBox.ShowDamageNumbers)), HarmonyPrefix]
        static private void EnemyHitBox_ShowDamageNumbers_Pre(EnemyHitBox __instance)
        {
            if (!_hideCriticalHitText)
                return;

            __instance.lastHitWasParry = false;
            if (PseudoSingleton<GymMinigame>.instance.TryNonNull(out var gymMinigame))
                gymMinigame.EnemyCriticalHit();
        }

        [HarmonyPatch(typeof(InGameTextController), nameof(InGameTextController.ShowText)), HarmonyPrefix]
        static private bool InGameTextController_ShowText_Pre(InGameTextController __instance, string text)
        => !_hidePerfectParryText || text != "PERFECT!";

        [HarmonyPatch(typeof(EnemyController), nameof(EnemyController.AttackCoroutine)), HarmonyPostfix]
        static private IEnumerator EnemyController_AttackCoroutine_Post(IEnumerator original, EnemyController __instance, bool dontRegisterAttack, bool waitForAttackRythm)
        {
            if (waitForAttackRythm)
            {
                float rhythmInterval = UnityEngine.Random.Range(_attackRhythmRandomRange.Value.x, _attackRhythmRandomRange.Value.y);
                while (UnityEngine.Time.time - EnemyController.lastAttackTime < rhythmInterval)
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
    }
}