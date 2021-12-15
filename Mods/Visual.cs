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
    using Vheos.Tools.Extensions.Collections;

    public class Visual : AMod
    {
        // Settings
        static private ModSetting<float> _exposureLerpTarget;
        static private ModSetting<float> _exposureLerpAlpha;
        static private ModSetting<bool> _hideCriticalHitText;
        static private ModSetting<bool> _hidePerfectParryText;
        static private ModSetting<bool> _hideClockTime;
        static private ModSetting<bool> _hideClockDay;
        static private ModSetting<bool> _comboBarFormatAsPercentIncrease;
        static private ModSetting<bool> _comboBarHideProgressBar;
        override protected void Initialize()
        {
            _exposureLerpTarget = CreateSetting(nameof(_exposureLerpTarget), 1f, FloatRange(0f, 2f));
            _exposureLerpAlpha = CreateSetting(nameof(_exposureLerpAlpha), 0f, FloatRange(0f, 1f));
            _hideCriticalHitText = CreateSetting(nameof(_hideCriticalHitText), false);
            _hidePerfectParryText = CreateSetting(nameof(_hidePerfectParryText), false);
            _hideClockTime = CreateSetting(nameof(_hideClockTime), false);
            _hideClockDay = CreateSetting(nameof(_hideClockDay), false);
            _comboBarFormatAsPercentIncrease = CreateSetting(nameof(_comboBarFormatAsPercentIncrease), false);
            _comboBarHideProgressBar = CreateSetting(nameof(_comboBarHideProgressBar), false);
        }
        override protected void SetFormatting()
        {
            _exposureLerpTarget.Format("Exposure lerp target");
            _exposureLerpAlpha.Format("Exposure lerp alpha");
            _hideCriticalHitText.Format("Hide \"CRITICAL!\" text");
            _hidePerfectParryText.Format("Hide \"PERFECT!\" text");
            _hideClockTime.Format("Hide clock time");
            _hideClockDay.Format("Hide clock day");
            _comboBarFormatAsPercentIncrease.Format("Format as percent increase");
            _comboBarHideProgressBar.Format("Hide progress bar");
        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(Preset.Coop_NewGameExtra_HardMode):
                    ForceApply();
                    _exposureLerpTarget.Value = 1.5f;
                    _exposureLerpAlpha.Value = 1 / 3f;
                    _hideCriticalHitText.Value = true;
                    _hidePerfectParryText.Value = true;
                    _hideClockTime.Value = false;
                    _hideClockDay.Value = true;
                    _comboBarFormatAsPercentIncrease.Value = true;
                    _comboBarHideProgressBar.Value = false;
                    break;
            }
        }

        // Privates
        static private void UpdateClockVisibility()
        {
            if (!PseudoSingleton<InGameClock>.instance.TryNonNull(out var clock))
                return;

            clock.clockText.transform.localScale = _hideClockTime ? Vector3.zero : Vector3.one;
            clock.dayText.transform.localScale = _hideClockDay ? Vector3.zero : Vector3.one;
        }


        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Exposure
        [HarmonyPatch(typeof(Lists), nameof(Lists.Start)), HarmonyPostfix]
        static private void Lists_Start_Post(Lists __instance)
        {
            PostProcessingProfile[] GetCameraProfiles(AreaDescription areaDescription)
            => new[]
            {
                areaDescription.morningCameraProfile,
                areaDescription.dayCameraProfile,
                areaDescription.eveningCameraProfile,
                areaDescription.nightCameraProfile
            };

            var processedProfiles = new HashSet<PostProcessingProfile>();
            foreach (var areaDescription in __instance.areaDatabase.areas)
                foreach (var profile in GetCameraProfiles(areaDescription))
                    if (!processedProfiles.Contains(profile))
                    {
                        var settings = profile.colorGrading.settings;
                        settings.basic.postExposure.SetLerp(_exposureLerpTarget, _exposureLerpAlpha);
                        profile.colorGrading.settings = settings;
                        processedProfiles.Add(profile);
                    }
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

        // Hide clock
        [HarmonyPatch(typeof(InGameClock), nameof(InGameClock.Start)), HarmonyPostfix]
        static private void InGameClock_Start_Post(InGameClock __instance)
        => UpdateClockVisibility();

        [HarmonyPatch(typeof(ComboBar), nameof(ComboBar.SetComboText)), HarmonyPrefix]
        static private bool ComboBar_SetComboText_Pre(ComboBar __instance)
        {
            if (!_comboBarFormatAsPercentIncrease)
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
            // progress bar
            __instance.FindChild("EmptyBar").SetActive(!_comboBarHideProgressBar);
            __instance.barFill.gameObject.SetActive(!_comboBarHideProgressBar);

            // format as percent increase
            float comboNameScale = _comboBarFormatAsPercentIncrease ? 0.8f : 1f;
            __instance.transform.Find("ComboName").localScale = comboNameScale.ToVector3();

            return false;
        }
    }
}