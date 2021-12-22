namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using HarmonyLib;
    using Tools.ModdingCore;
    using Tools.Extensions.UnityObjects;
    using Tools.Extensions.Math;
    using Vheos.Tools.Extensions.General;
    using Vheos.Tools.Extensions.Collections;
    using UnityEngine.UI;

    public class Various : AMod
    {
        // Section
        override protected string SectionOverride
        => Sections.VARIOUS;
        override protected string Description =>
            "Mods that haven't found their home yet!" +
            "\n\nExamples:" +
            "\n• Skip 30sec of intro logos" +
            "\n• Customize the \"Stamina Heal\" move" +
            "\n• Scale enemies' and bosses' HP" +
            "\n• Make enemies in groups attack more often";

        // Settings
        static private ModSetting<bool> _runInBackground;
        static private ModSetting<bool> _skipIntroLogos;
        static private ModSetting<int> _staminaHealGain;
        static private ModSetting<int> _staminaHealDuration;
        static private ModSetting<bool> _staminaHealCancelling;
        static private ModSetting<bool> _pauseStaminaRecoveryOnGain;
        static private ModSetting<int> _enemyHPMultiplier;
        static private ModSetting<int> _enemyBossHPMultiplier;
        static private ModSetting<bool> _randomizeEnemyGroupAttackRhythm;
        override protected void Initialize()
        {
            _runInBackground = CreateSetting(nameof(_runInBackground), false);
            _skipIntroLogos = CreateSetting(nameof(_skipIntroLogos), false);

            _staminaHealGain = CreateSetting(nameof(_staminaHealGain), 100, IntRange(0, 100));
            _staminaHealDuration = CreateSetting(nameof(_staminaHealDuration), 100, IntRange(50, 200));
            _staminaHealCancelling = CreateSetting(nameof(_staminaHealCancelling), true);
            _pauseStaminaRecoveryOnGain = CreateSetting(nameof(_pauseStaminaRecoveryOnGain), true);

            _enemyHPMultiplier = CreateSetting(nameof(_enemyHPMultiplier), 100, IntRange(25, 400));
            _enemyBossHPMultiplier = CreateSetting(nameof(_enemyBossHPMultiplier), 100, IntRange(25, 400));
            _randomizeEnemyGroupAttackRhythm = CreateSetting(nameof(_randomizeEnemyGroupAttackRhythm), false);

            // Events
            _runInBackground.AddEvent(() => Application.runInBackground = _runInBackground);
        }
        override protected void SetFormatting()
        {
            _runInBackground.IsAdvanced = true;
            _runInBackground.Format("Run in background");
            _runInBackground.Description =
                "Makes the game run even when it's not focused";
            _skipIntroLogos.Format("Skip intro logos");
            _skipIntroLogos.Description =
                "Skips all the unskippable logo animations, as well as the input choice screen, and goes straight to the main menu" +
                "\nYou'll save about 30 seconds of your precious life each time you start the game";

            _staminaHealGain.Format("\"Stamina Heal\" gain");
            _staminaHealGain.Description =
                "How much stamina you recover when you perform the \"Stamina Heal\" action" +
                "\n(double tap the \"Sprint\" button while standing still)" +
                "\n\nUnit: percent of max stamina";
            _staminaHealDuration.Format("\"Stamina Heal\" duration");
            _staminaHealDuration.Description =
                "How long is the \"Stamina Heal\" animation" +
                "\n\nUnit: percent of original duration";
            _staminaHealCancelling.Format("\"Stamina Heal\" cancelling");
            _staminaHealCancelling.Description =
                "Allows you to cancel the \"Stamina Heal\" animation after merely 40% of its duration" +
                "\nDisable this to make \"Stamina Heal\" more punishable in combat";
            _pauseStaminaRecoveryOnGain.Format("Pause stamina recovery on gain");
            _pauseStaminaRecoveryOnGain.Description =
                "Pauses your natural stamina recovery whenever you gain stamina by other means";

            _enemyHPMultiplier.Format("Enemy HP");
            _enemyHPMultiplier.Description =
                "How much HP enemies have (also affects bosses)" +
                "\nHigher values are recommended for co-op, as enemy HP doesn't scale with player count" +
                "\n\nUnit: percent of original enemy HP";
            _enemyBossHPMultiplier.Format("Boss HP");
            _enemyBossHPMultiplier.Description =
                "How much HP bosses have" +
                $"\n\nUnit: percent of boss HP, accounting for \"{_enemyHPMultiplier.Name}\"";
            _randomizeEnemyGroupAttackRhythm.Format("Randomize enemy group attack rhythm");
            _randomizeEnemyGroupAttackRhythm.Description =
                "Randomizes the attacking \"rhythm\" of enemies' groups" +
                "\nBy default, most enemies can't attack at the same time" +
                "\nInstead, every 0.75sec one random enemy will start attacking" +
                "\nThis settings makes some enemies attack earlier than expected, sometimes simultaneously with others";
        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(SettingsPreset.Vheos_HardMode):
                    ForceApply();
                    _skipIntroLogos.Value = true;

                    _staminaHealGain.Value = 50;
                    _staminaHealDuration.Value = 100;
                    _staminaHealCancelling.Value = false;
                    _pauseStaminaRecoveryOnGain.Value = false;

                    _enemyHPMultiplier.Value = 200;
                    _enemyBossHPMultiplier.Value = 125;
                    _randomizeEnemyGroupAttackRhythm.Value = true;
                    break;
            }
        }

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

        // Skip intro
        [HarmonyPatch(typeof(SplashScreenScene), nameof(SplashScreenScene.Start)), HarmonyPostfix]
        static private IEnumerator SplashScreenScene_Start_Post(IEnumerator original, SplashScreenScene __instance)
        {
            if (!_skipIntroLogos)
            {
                while (original.MoveNext())
                    yield return original.Current;
                yield break;
            }

#if UNITY2019
            __instance.xboxSignedInAndLoaded = false;
            __instance.StartCoroutine("XboxSignIn");
            while (!__instance.xboxSignedInAndLoaded)
                yield return __instance.WaitForSeconds(0.1);
#endif

            Time.timeScale = 1f;
            __instance.CheckBestResolution();
            __instance.sceneLoadingObject = SceneManager.LoadSceneAsync("TitleScreen");
            __instance.sceneLoadingObject.allowSceneActivation = true;
        }

        [HarmonyPatch(typeof(TitleScreenScene), nameof(TitleScreenScene.Start)), HarmonyPostfix]
        static private void TitleScreenScene_Start_Post(TitleScreenScene __instance)
        {
            if (!_skipIntroLogos)
                return;

            PressAnyKeyScreen.inputPopupAlreadyAppeared = true;
            BetaTitleScreen.logoShown = true;
        }

        // Stamina
        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.FillStamina)), HarmonyPrefix]
        static private void BasicCharacterController_FillStamina_Pre(BasicCharacterController __instance, ref float __state, ref float value)
        {
            if (!_pauseStaminaRecoveryOnGain)
                __state = __instance.lastStaminaUsageTime;

            if (__instance.staminaChargeEffect)
                value = _staminaHealGain / 100f * __instance.myInfo.totalStamina;
        }

        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.FillStamina)), HarmonyPostfix]
        static private void BasicCharacterController_FillStamina_Post(BasicCharacterController __instance, ref float __state)
        {
            if (!_pauseStaminaRecoveryOnGain)
                __instance.lastStaminaUsageTime = __state;
        }

        // Enemy HP
        [HarmonyPatch(typeof(Atributes), nameof(Atributes.OnEnable)), HarmonyPrefix]
        static private bool Atributes_OnEnable_Pre(Atributes __instance)
        {
            if (__instance.myOwner == null && __instance.transform.parent != null)
                __instance.myOwner = __instance.transform.parent.GetComponent<SortingObject>();

            if (!__instance.appliedLifeDifficulty)
            {
                float multiplier = _enemyHPMultiplier / 100f;
                if (__instance.TryGetComponent<EnemyHitBox>(out var enemyHitBox)
                && enemyHitBox.bossBar)
                    multiplier *= _enemyBossHPMultiplier / 100f;

                __instance.totalLife = __instance.totalLife.Mul(multiplier).Round();
                __instance.appliedLifeDifficulty = true;
            }

            return false;
        }

        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.StaminaChargeCoroutine)), HarmonyPostfix]
        static private IEnumerator BasicCharacterController_StaminaChargeCoroutine_Post(IEnumerator original, BasicCharacterController __instance)
        {
            float frameDuration = _staminaHealDuration / 100f * ORIGINAL_STAMINA_CHARGE_DURATION / 10f;

            original.MoveNext();
            __instance.myAnimations.PlayAnimation("StaminaCharge", 1, 0, _staminaHealDuration / 100f, false, true);
            if (frameDuration > 0)
                yield return gameTime.WaitForSeconds(frameDuration * 2);

            original.MoveNext();
            __instance.staminaChargeTime = 0f;
            if (frameDuration > 0)
                yield return gameTime.WaitForSeconds(frameDuration * 2);

            original.MoveNext();
            __instance.staminaChargeEffect = !_staminaHealCancelling;
            if (frameDuration > 0)
                yield return gameTime.WaitForSeconds(frameDuration * 6);
            __instance.staminaChargeEffect = false;

            while (original.MoveNext())
                yield return original.Current;
        }

        // Attack rhythm
        [HarmonyPatch(typeof(EnemyController), nameof(EnemyController.AttackCoroutine)), HarmonyPostfix]
        static private IEnumerator EnemyController_AttackCoroutine_Post(IEnumerator original, EnemyController __instance, bool dontRegisterAttack, bool waitForAttackRythm)
        {
            if (waitForAttackRythm)
            {
                float interval = _randomizeEnemyGroupAttackRhythm
                               ? UnityEngine.Random.Range(0f, 0.75f)
                               : 0.75f;

                while (Time.time - EnemyController.lastAttackTime < interval)
                    yield return gameTime.WaitForSeconds(0.1f);
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