namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using HarmonyLib;
    using Mods.Core;
    using Tools.Extensions.Math;
    using Tools.Extensions.General;
    using Tools.Extensions.UnityObjects;
    using Tools.Extensions.Collections;

    public class UI : AMod
    {
        // Section
        override protected string SectionOverride
        => Sections.QOL;
        override protected string Description =>
            "Mods related to the UI (mostly text and popups)" +
            "\n\nExamples:" +
            "\n• Hide combat popups" +
            "\n• Hide current day / time" +
            "\n• Customize crosshair" +
            "\n• Customize combo display" +
            "\n• Reduce the intensity of hit effects" +
            "\n• Override controller icons";

        // Settings
        static private ModSetting<bool> _damagePopups;
        static private ModSetting<bool> _statusEffectPopups;
        static private ModSetting<bool> _criticalPopup;
        static private ModSetting<bool> _parryPopup;
        static private ModSetting<bool> _comboCounter;
        static private ModSetting<bool> _comboCounterAsPercentIncrease;
        static private ModSetting<bool> _comboProgressBar;
        static private ModSetting<bool> _clockTime;
        static private ModSetting<bool> _clockDay;
        static private ModSetting<Color> _crosshairBigDiamondColor;
        static private ModSetting<Color> _crosshairSmallDiamondColor;
        static private ModSetting<Color> _crosshairDotColor;
        static private ModSetting<bool> _displayTotalCogUses;
        static private ModSetting<int> _hitEffectIntensity;
        static private ModSetting<ControllerType> _controllerIcons;
        override protected void Initialize()
        {
            _damagePopups = CreateSetting(nameof(_damagePopups), true);
            _statusEffectPopups = CreateSetting(nameof(_statusEffectPopups), true);
            _criticalPopup = CreateSetting(nameof(_criticalPopup), true);
            _parryPopup = CreateSetting(nameof(_parryPopup), true);

            _crosshairBigDiamondColor = CreateSetting(nameof(_crosshairBigDiamondColor), Color.white);
            _crosshairSmallDiamondColor = CreateSetting(nameof(_crosshairSmallDiamondColor), Color.white);
            _crosshairDotColor = CreateSetting(nameof(_crosshairDotColor), Color.white);

            _comboCounter = CreateSetting(nameof(_comboCounter), true);
            _comboCounterAsPercentIncrease = CreateSetting(nameof(_comboCounterAsPercentIncrease), false);
            _comboProgressBar = CreateSetting(nameof(_comboProgressBar), true);

            _clockTime = CreateSetting(nameof(_clockTime), true);
            _clockDay = CreateSetting(nameof(_clockDay), true);

            _displayTotalCogUses = CreateSetting(nameof(_displayTotalCogUses), true);
            _hitEffectIntensity = CreateSetting(nameof(_hitEffectIntensity), 100, IntRange(0, 100));
            _controllerIcons = CreateSetting(nameof(_controllerIcons), ControllerType.AsDetected);

            // Events
            _clockTime.AddEvent(() => TryUpdateClockVisibility(PseudoSingleton<InGameClock>.instance));
            _clockDay.AddEvent(() => TryUpdateClockVisibility(PseudoSingleton<InGameClock>.instance));
            _hitEffectIntensity.AddEvent(() => TrySetHitEffectColors(PseudoSingleton<LowHealthEffect>.instance));
        }
        override protected void SetFormatting()
        {
            CreateHeader("Combat popups").Description =
                "Allows you to hide chosen text popups to make combat less cluttered and more immersive";
            using (Indent)
            {
                _damagePopups.Format("damage numbers");
                _damagePopups.Description =
                    "Displays damage numbers";
                _statusEffectPopups.Format("status effects");
                _statusEffectPopups.Description =
                    "Displays the big \"BURN!\", \"FROZEN!\" and \"DEF. DOWN!\" popups when inflicting elemental status effects";
                _criticalPopup.Format("critical hit");
                _criticalPopup.Description =
                    "Displays the big red \"CRITICAL!\" popup when attacking a stunned enemy";
                _parryPopup.Format("parry");
                _parryPopup.Description =
                    "Displays the big green \"PERFECT!\" popup when parrying";
            }

            CreateHeader("Crosshair").Description =
                "Allows you to change the colors of each crosshair parts" +
                "\nIn order to hide a part just set its color's alpha to 0" +
                "\n(requires entering and leaving menu to take effect)";
            using (Indent)
            {
                _crosshairBigDiamondColor.Format("big diamond");
                _crosshairSmallDiamondColor.Format("small diamond");
                _crosshairDotColor.Format("dot");
            }

            _comboCounter.Format("Combo counter");
            _comboCounter.Description =
                "Displays the current damage multiplier gained from combo";
            using (Indent)
            {
                _comboCounterAsPercentIncrease.Format("format as percent increase", _comboCounter);
                _comboCounterAsPercentIncrease.Description =
                    "Changes the combo counter formatting from a multiplier (eg. 1.23x) to a percent increase (eg. +23%)";
            }
            _comboProgressBar.Format("Combo progress bar");
            _comboProgressBar.Description =
                "Displays the colorful progress bar below the numerical combo value";

            _clockTime.Format("Clock time");
            _clockTime.Description =
                "Displays the hours / minutes counter";
            _clockDay.Format("Clock day");
            _clockDay.Description =
                "Displays the day counter";

            _displayTotalCogUses.Format("Display total cog uses");
            _displayTotalCogUses.Description =
                "Displays the original amount of cog uses next to the remaining cog uses (eg. 11/20)" +
                "\nDisable to display only the remaining uses (eg. 11)";
            _hitEffectIntensity.Format("Hit effect intensity");
            _hitEffectIntensity.Description =
                "How visible is the overlay (circuits and vignette) when taking damage, getting frozen or being at low health";
            _controllerIcons.Format("Controller icons");
            _controllerIcons.Description =
                "Allows you to override the icons used for controller prompts" +
                "\nUseful when the game doesn't correctly identify your controller, such as when using third party software to map PlayStation input to Xbox output";
        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(SettingsPreset.Vheos_UI):
                    ForceApply();
                    _damagePopups.Value = true;
                    _statusEffectPopups.Value = false;
                    _criticalPopup.Value = false;
                    _parryPopup.Value = false;

                    _crosshairBigDiamondColor.Value = new Color(0, 0, 0, 0);
                    _crosshairSmallDiamondColor.Value = new Color(1, 1, 1, 0.75f);
                    _crosshairDotColor.Value = new Color(0, 0, 0, 0);

                    _comboCounter.Value = true;
                    _comboCounterAsPercentIncrease.Value = true;
                    _comboProgressBar.Value = true;

                    _clockTime.Value = true;
                    _clockDay.Value = false;

                    _displayTotalCogUses.Value = false;
                    _hitEffectIntensity.Value = 50;
                    _controllerIcons.Value = ControllerType.DualShock4;
                    break;
            }
        }

        // Privates
        #region HitEffectColors
        static private readonly Color HIT_OVERLAY_COLOR = new Color(1f, 0f, 0.1862f, 0.853f);
        static private readonly Color LOW_HEALTH_OVERLAY_COLOR_1 = new Color(1f, 0f, 0.7655f, 0.666f);
        static private readonly Color LOW_HEALTH_OVERLAY_COLOR_2 = new Color(1f, 0f, 0.1862f, 0.691f);
        static private readonly Color FROZEN_OVERLAY_COLOR = new Color(1f, 1f, 1f, 0.384f);
        static private readonly Color HIT_VIGNETTE_COLOR = new Color(0f, 0f, 0f, 0.291f);
        static private readonly Color LOW_HEALTH_VIGNETTE_COLOR_1 = new Color(0f, 0f, 0f, 0.366f);
        static private readonly Color LOW_HEALTH_VIGNETTE_COLOR_2 = new Color(0f, 0f, 0f, 0.122f);
        static private readonly Color FROZEN_VIGNETTE_COLOR = new Color(0.8186f, 0.7941f, 1f, 0.153f);
        #endregion
        static private void TryUpdateClockVisibility(InGameClock inGameClock)
        {
            if (inGameClock == null)
                return;

            inGameClock.clockText.transform.localScale = _clockTime ? Vector3.one : Vector3.zero;
            inGameClock.dayText.transform.localScale = _clockDay ? Vector3.one : Vector3.zero;
        }
        static private bool HasAnyPlayerJustParried()
        => PseudoSingleton<PlayersManager>.instance.TryNonNull(out var playerManager)
        && (playerManager.playerObjects[0].myCharacter.justDidAPerfectParry
        || playerManager.playerObjects[1].myCharacter.justDidAPerfectParry);
        static private string GetLocalizedString(string key)
        => TranslationSystem.FindTerm("Terms", key, true);
        static private readonly Dictionary<ControllerType, int[]> CONTROLLER_BUTTON_ICONS_MAP = new Dictionary<ControllerType, int[]>
        {
            [ControllerType.DualShock4] = new[] { 1, 0, 3, 2, 4, 5, 6, 7, 10, 11, 8, 9, 13 },
            [ControllerType.XboxOne] = new[] { 0, 2, 3, 1, 4, 5, 19, 18, 8, 9, 6, 7, -1 },
        };
        static private void TrySetHitEffectColors(LowHealthEffect lowHealthEffect)
        {
            if (lowHealthEffect == null)
                return;

            var multiplier = new Color(1, 1, 1, _hitEffectIntensity / 100f);
            lowHealthEffect.cablesHitColor = HIT_OVERLAY_COLOR * multiplier;
            lowHealthEffect.cablesLowHealthColor1 = LOW_HEALTH_OVERLAY_COLOR_1 * multiplier;
            lowHealthEffect.cablesLowHealthColor2 = LOW_HEALTH_OVERLAY_COLOR_2 * multiplier;
            lowHealthEffect.cablesFrozen = FROZEN_OVERLAY_COLOR * multiplier;
            lowHealthEffect.vignetteHitColor = HIT_VIGNETTE_COLOR * multiplier;
            lowHealthEffect.vignetteLowHealthColor1 = LOW_HEALTH_VIGNETTE_COLOR_1 * multiplier;
            lowHealthEffect.vignetteLowHealthColor2 = LOW_HEALTH_VIGNETTE_COLOR_2 * multiplier;
            lowHealthEffect.vignetteFrozen = FROZEN_VIGNETTE_COLOR * multiplier;
        }

        // Defines
        private enum ControllerType
        {
            AsDetected = 0,
            DualShock4 = 1,
            XboxOne = 2,
        }

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Text
        [HarmonyPatch(typeof(InGameTextController), nameof(InGameTextController.ShowText)), HarmonyPrefix]
        static private bool InGameTextController_ShowText_Pre(InGameTextController __instance, ref string text)
        {
            if (!_damagePopups && text.Any(char.IsDigit))
                return false;
            if (!_statusEffectPopups && (text == GetLocalizedString("Burn") || text == GetLocalizedString("Frozen") || text == GetLocalizedString("DefDown")))
                return false;
            if (!_criticalPopup && text.Contains("CRITICAL!\n"))
                text = text.Replace("CRITICAL!\n", null);
            if (!_parryPopup && text == "PERFECT!" && HasAnyPlayerJustParried())
                return false;

            return true;
        }

        // Hide clock
        [HarmonyPatch(typeof(InGameClock), nameof(InGameClock.UpdateClock)), HarmonyPostfix]
        static private void InGameClock_Start_Post(InGameClock __instance)
        => TryUpdateClockVisibility(__instance);

        [HarmonyPatch(typeof(ComboBar), nameof(ComboBar.SetComboText)), HarmonyPrefix]
        static private bool ComboBar_SetComboText_Pre(ComboBar __instance)
        {
            if (!_comboCounterAsPercentIncrease)
                return true;

            float percentIncrease = __instance.comboValue.Sub(1).Mul(100).Round().ClampMin(1);

            __instance.valueText.text = $"+{percentIncrease:F0}%";
            __instance.valueText.keepSizeLarge = true;
            __instance.valueText.ApplyText(false, true, "", true);
            return false;
        }

        [HarmonyPatch(typeof(ComboBar), nameof(ComboBar.OnEnable)), HarmonyPrefix]
        static private bool ComboBar_OnEnable_Post(ComboBar __instance)
        {
            // counter
            float comboCounterScale = _comboCounter ? 1f : 0f;
            float comboNameScale = _comboCounter
                                 ? _comboCounterAsPercentIncrease ? 0.75f : 1f
                                 : 0f;

            __instance.valueText.transform.localScale = comboCounterScale.ToVector3();
            __instance.transform.Find("ComboName").localScale = comboNameScale.ToVector3();

            // progress bar
            __instance.FindChild("EmptyBar").SetActive(_comboProgressBar);
            __instance.barFill.gameObject.SetActive(_comboProgressBar);

            return false;
        }

        // Crosshair
        [HarmonyPatch(typeof(PlayerAimInterface), nameof(PlayerAimInterface.OnEnable)), HarmonyPostfix]
        static private void PlayerAimInterface_OnEnable_Post(PlayerAimInterface __instance)
        {
            foreach (var image in __instance.mouseCursor.GetComponentsInChildren<Image>(true))
                image.color = image.name.Contains("1") ? _crosshairDotColor : _crosshairBigDiamondColor;

            foreach (var image in __instance.directionCursor.GetComponentsInChildren<Image>(true))
                image.color = _crosshairSmallDiamondColor;
        }

        // Don't display total uses
        [HarmonyPatch(typeof(BuffIconController), nameof(BuffIconController.ShowBuff)), HarmonyPostfix]
        static private void BuffIconController_ShowBuff_Post(BuffIconController __instance, PlayerBuffs currentBuff, int playerNum)
        {
            if (_displayTotalCogUses
            || currentBuff.usesSeconds)
                return;

            var ftext = __instance.buffText;
            ftext.text = __instance.GetAllRemainingUsesFrom(currentBuff.buffType, playerNum).ToString();
            ftext.ApplyText(true, true, "", true);
        }

        [HarmonyPatch(typeof(CogButton), nameof(CogButton.ShowCog)), HarmonyPostfix]
        static private void BuffIconController_ShowBuff_Post(CogButton __instance, PlayerBuffs buff)
        {
            if (_displayTotalCogUses
            || buff.usesSeconds)
                return;

            var ftext = __instance.cogDuration;
            ftext.text = buff.remainingUses.ToString();
            ftext.ApplyText(true, true, "", true);
        }

        // Hit effect colors
        [HarmonyPatch(typeof(LowHealthEffect), nameof(LowHealthEffect.OnEnable)), HarmonyPrefix]
        static private void LowHealthEffect_OnEnable_Pre(LowHealthEffect __instance)
        => TrySetHitEffectColors(__instance);

        // Input device icons
        [HarmonyPatch(typeof(DeviceIconDatabase), nameof(DeviceIconDatabase.GetDeviceIcon)), HarmonyPrefix]
        static private void DeviceIconDatabase_GetDeviceIcon_Pre(DeviceIconDatabase __instance, ref InputType targetDevice)
        {
            if (_controllerIcons.Value == ControllerType.AsDetected)
                return;

            targetDevice = (InputType)_controllerIcons.Value;
        }

        [HarmonyPatch(typeof(DeviceIconDatabase), nameof(DeviceIconDatabase.GetDeviceButtonIcon)), HarmonyPrefix]
        static private void DeviceIconDatabase_GetDeviceButtonIcon_Pre(DeviceIconDatabase __instance, ref InputType targetDevice, ref int buttonNum)
        {
            ControllerType to = _controllerIcons;
            if (to != ControllerType.DualShock4
            && to != ControllerType.XboxOne)
                return;

            ControllerType from = (ControllerType)targetDevice;
            if (to == from
            || !CONTROLLER_BUTTON_ICONS_MAP[from].TryFindIndex(buttonNum, out var index)
            || !CONTROLLER_BUTTON_ICONS_MAP[to].TryGet(index, out var mappedButton)
            || mappedButton == -1)
                return;

            targetDevice = (InputType)_controllerIcons.Value;
            buttonNum = mappedButton;
        }
    }
}