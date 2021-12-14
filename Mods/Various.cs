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

    public class Various : AMod
    {
        // Settings
        static private ModSetting<bool> _skipIntroLogos;
        static private ModSetting<int> _startingChipSlots;
        static private ModSetting<int> _linearChipSlotCosts;
        static private ModSetting<int> _cogSlots;
        static private ModSetting<int> _maxActiveCogTypes;
        static private ModSetting<float> _nonSpinnerMoveSpeed;
        static private ModSetting<float> _spinnerStaminaDrainRate;
        static private ModSetting<bool> _spinnerRemoveStaminaGain;
        static private ModSetting<bool> _pauseRecoveryOnStaminaGain;
        static private ModSetting<float> _runnerChipSpeedMultiplier;
        override protected void Initialize()
        {
            _skipIntroLogos = CreateSetting(nameof(_skipIntroLogos), false);
            _startingChipSlots = CreateSetting(nameof(_startingChipSlots), 3, IntRange(0, 14));
            _linearChipSlotCosts = CreateSetting(nameof(_linearChipSlotCosts), -1, IntRange(0, 2000));
            _cogSlots = CreateSetting(nameof(_cogSlots), 4, IntRange(0, 6));
            _maxActiveCogTypes = CreateSetting(nameof(_maxActiveCogTypes), 4, IntRange(0, 6));
            _nonSpinnerMoveSpeed = CreateSetting(nameof(_nonSpinnerMoveSpeed), ORIGINAL_MOVEMENT_SPEED, FloatRange(3f, 12f));
            _spinnerStaminaDrainRate = CreateSetting(nameof(_spinnerStaminaDrainRate), 1f, FloatRange(1f, 10f));
            _runnerChipSpeedMultiplier = CreateSetting(nameof(_runnerChipSpeedMultiplier), 1f, FloatRange(1f, 2f));

            _linearChipSlotCosts.AddEvent(() => TrySetLinearChipSlotCosts(PseudoSingleton<LevelDatabase>.instance));
            _nonSpinnerMoveSpeed.AddEvent(() => TrySetSpeeds(PseudoSingleton<PlayersManager>.instance));
        }
        override protected void SetFormatting()
        {
            _skipIntroLogos.Format("Skip intro logos");
            _startingChipSlots.Format("Starting chip slots");
            _linearChipSlotCosts.Format("Linear chip slot costs");
            _cogSlots.Format("Cog slots");
            _maxActiveCogTypes.Format("Max active cog types");
            _nonSpinnerMoveSpeed.Format("Movement speed (non-spinner)");
            _spinnerStaminaDrainRate.Format("Spinner stamina drain rate");
            _runnerChipSpeedMultiplier.Format("Runner chip speed multiplier");
        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(Preset.Coop_NewGameExtra_HardMode):
                    ForceApply();
                    _skipIntroLogos.Value = true;
                    _startingChipSlots.Value = 0;
                    _linearChipSlotCosts.Value = 750;
                    _cogSlots.Value = 6;
                    _maxActiveCogTypes.Value = 1;
                    _nonSpinnerMoveSpeed.Value = 6.6f;
                    _spinnerStaminaDrainRate.Value = 2f;
                    _runnerChipSpeedMultiplier.Value = 1.1f;
                    break;
            }
        }

        // Privates
        static private void TrySetLinearChipSlotCosts(LevelDatabase levelDatabase)
        {
            if (levelDatabase == null
            || _linearChipSlotCosts < 0)
                return;

            for (int i = 0; i <= 15; i++)
                levelDatabase.levelUpCost[i] = i.Sub(_startingChipSlots).Add(1).Mul(_linearChipSlotCosts).ClampMin(0);
        }
        static private void TrySetSpeeds(PlayersManager playersManager)
        {
            if (playersManager == null)
                return;

            foreach (var player in playersManager.players)
                if (player.myCharacter.TryNonNull(out var character)
                && !character.ridingSpinner)
                    character.originalSpeed = character.speed = _nonSpinnerMoveSpeed;
        }
        private const float ORIGINAL_MOVEMENT_SPEED = 6f;

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

        // Move speed
        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.SetMovement)), HarmonyPrefix]
        static private void BasicCharacterController_SetMovement_Pre(BasicCharacterController __instance)
        {
            float speed = ORIGINAL_MOVEMENT_SPEED;

            if (__instance.ridingSpinner && _spinnerStaminaDrainRate > 1f)
                __instance.DrainStamina(Time.deltaTime * (_spinnerStaminaDrainRate - 1), false);
            else
            {
                speed = _nonSpinnerMoveSpeed;
                if (__instance.running && __instance.myPhysics.height == 0f
                && PseudoSingleton<Helpers>.instance.NumberOfChipsEquipped("RunnerChip", __instance.myInfo.playerNum) > 0)
                    speed *= _runnerChipSpeedMultiplier;
            }

            __instance.originalSpeed = __instance.speed = speed;
        }


        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.FillStamina)), HarmonyPrefix]
        static private bool BasicCharacterController_FillStamina_Pre(BasicCharacterController __instance, ref float value)
        {
            if (_spinnerRemoveStaminaGain && __instance.ridingSpinner)
                return false;

            float previousStamina = __instance.myInfo.currentStamina;
            __instance.myInfo.currentStamina += value;
            __instance.myInfo.currentStamina.SetClampMax(__instance.myInfo.totalStamina);

            if (__instance.myInfo.currentStamina != previousStamina)
                __instance.lastStaminaRechargeTime = Time.time;

            if (_pauseRecoveryOnStaminaGain)
                __instance.lastStaminaUsageTime = Time.time;

            return false;
        }
    }
}