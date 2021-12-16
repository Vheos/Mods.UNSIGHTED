namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.PostProcessing;
    using HarmonyLib;
    using Tools.ModdingCore;
    using Tools.Extensions.Math;
    using Tools.Extensions.General;
    using Tools.Extensions.UnityObjects;
    using System.Linq;
    using Vheos.Tools.UtilityN;
    using System.Collections;
    using System.Text;

    public class UI : AMod
    {
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
        override protected void Initialize()
        {
            _damagePopups = CreateSetting(nameof(_damagePopups), true);
            _statusEffectPopups = CreateSetting(nameof(_statusEffectPopups), true);
            _criticalPopup = CreateSetting(nameof(_criticalPopup), true);
            _parryPopup = CreateSetting(nameof(_parryPopup), true);

            _comboCounter = CreateSetting(nameof(_comboCounter), true);
            _comboCounterAsPercentIncrease = CreateSetting(nameof(_comboCounterAsPercentIncrease), true);
            _comboProgressBar = CreateSetting(nameof(_comboProgressBar), true);

            _clockTime = CreateSetting(nameof(_clockTime), true);
            _clockDay = CreateSetting(nameof(_clockDay), true);

            // Events
            _clockTime.AddEvent(() => UpdateClockVisibility(PseudoSingleton<InGameClock>.instance));
            _clockDay.AddEvent(() => UpdateClockVisibility(PseudoSingleton<InGameClock>.instance));
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
        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(Preset.Vheos_UI):
                    ForceApply();
                    _damagePopups.Value = true;
                    _statusEffectPopups.Value = false;
                    _criticalPopup.Value = false;
                    _parryPopup.Value = false;

                    _comboCounter.Value = true;
                    _comboCounterAsPercentIncrease.Value = true;
                    _comboProgressBar.Value = true;

                    _clockTime.Value = true;
                    _clockDay.Value = false;
                    break;
            }
        }
        override protected string Description =>
            "Mods related to the UI (mostly text and popups)" +
            "\n\nExamples:" +
            "\n• Hide combat popups" +
            "\n• Hide current day / time" +
            "\n• Customize combo display";

        // Privates
        static private void UpdateClockVisibility(InGameClock inGameClock)
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

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

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
        => UpdateClockVisibility(__instance);

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
    }
}