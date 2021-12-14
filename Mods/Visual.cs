namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.PostProcessing;
    using HarmonyLib;
    using Tools.ModdingCore;
    using Tools.Extensions.Math;
    using Tools.Extensions.General;
    using Tools.Extensions.Collections;

    public class Visual : AMod
    {
        // Settings
        static private ModSetting<float> _exposureLerpTarget;
        static private ModSetting<float> _exposureLerpAlpha;
        static private ModSetting<bool> _hideCriticalHitText;
        static private ModSetting<bool> _hidePerfectParryText;
        override protected void Initialize()
        {
            _exposureLerpTarget = CreateSetting(nameof(_exposureLerpTarget), 1f, FloatRange(0f, 2f));
            _exposureLerpAlpha = CreateSetting(nameof(_exposureLerpAlpha), 0f, FloatRange(0f, 1f));
            _hideCriticalHitText = CreateSetting(nameof(_hideCriticalHitText), false);
            _hidePerfectParryText = CreateSetting(nameof(_hidePerfectParryText), false);
        }
        override protected void SetFormatting()
        {
            _exposureLerpTarget.Format("Exposure lerp target");
            _exposureLerpAlpha.Format("Exposure lerp alpha");
            _hideCriticalHitText.Format("Hide \"CRITICAL!\" text");
            _hidePerfectParryText.Format("Hide \"PERFECT!\" text");
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
                    break;
            }
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
    }
}