namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections;
    using HarmonyLib;
    using UnityEngine;
    using Tools.ModdingCore;
    using Tools.Extensions.General;
    using Tools.Extensions.Math;


    public class TimeMods : AMod
    {
        // Settings
        static private ModSetting<float> _gameSpeed;
        static private ModSetting<float> _minutesPerSecond;
        static private ModSetting<float> _fishingTimeMultiplier;
        static private ModSetting<float> _parryMinigameTimeMultiplier;
        static private ModSetting<bool> _hideClockTime;
        static private ModSetting<bool> _hideClockDay;
        static private ModSetting<float> _frameStopDurationMultiplier;
        static private ModSetting<float> _slowMotionDurationMultiplier;
        static private ModSetting<float> _slowMotionValue;
        override protected void Initialize()
        {
            _gameSpeed = CreateSetting(nameof(_gameSpeed), 1f, FloatRange(0.5f, 2f));
            _minutesPerSecond = CreateSetting(nameof(_minutesPerSecond), 0.8f, FloatRange(0f, 2f));
            _fishingTimeMultiplier = CreateSetting(nameof(_fishingTimeMultiplier), 0f, FloatRange(0f, 1f));
            _parryMinigameTimeMultiplier = CreateSetting(nameof(_parryMinigameTimeMultiplier), 0f, FloatRange(0f, 1f));
            _hideClockTime = CreateSetting(nameof(_hideClockTime), false);
            _hideClockDay = CreateSetting(nameof(_hideClockDay), false);
            _frameStopDurationMultiplier = CreateSetting(nameof(_frameStopDurationMultiplier), 1f, FloatRange(0f, 2f));
            _slowMotionDurationMultiplier = CreateSetting(nameof(_slowMotionDurationMultiplier), 1f, FloatRange(0f, 2f));
            _slowMotionValue = CreateSetting(nameof(_slowMotionValue), ORIGINAL_SLOMO_VALUE, FloatRange(0f, 1f));

            _gameSpeed.AddEvent(() => Time.timeScale = 1f);
        }
        override protected void SetFormatting()
        {
            _gameSpeed.Format("Game speed");
            _minutesPerSecond.Format("Minutes per second");
            using (Indent)
            {
                _fishingTimeMultiplier.Format("Fishing time multiplier");
                _parryMinigameTimeMultiplier.Format("Parry minigame time multiplier");
            }
            _hideClockTime.Format("Hide clock time");
            _hideClockDay.Format("Hide clock day");
            _frameStopDurationMultiplier.Format("Frame stop duration multiplier");
            _slowMotionDurationMultiplier.Format("Slow motion duration multiplier");
            _slowMotionValue.Format("Slow motion value");
        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(Preset.Coop_NewGameExtra_HardMode):
                    ForceApply();
                    _gameSpeed.Value = 2 / 3f;
                    _minutesPerSecond.Value = 1.2f;
                    _fishingTimeMultiplier.Value = 0.2f;
                    _parryMinigameTimeMultiplier.Value = 0.1f;
                    _frameStopDurationMultiplier.Value = 0.75f;
                    _slowMotionDurationMultiplier.Value = 0f;
                    _slowMotionValue.Value = 0f;                    
                    _hideClockTime.Value = false;
                    _hideClockDay.Value = true;
                    break;
            }
        }
        protected override string ModName
        => "Time";

        // Privates
        private const float ORIGINAL_SLOMO_VALUE = 0.3f;
        static private bool IsAnyPlayerFishing()
        => PseudoSingleton<PlayersManager>.instance.TryNonNull(out var playerManager)
        && (playerManager.playerObjects[0].gameObject.activeInHierarchy && playerManager.playerObjects[0].myCharacter.fishing
        || playerManager.playerObjects[1].gameObject.activeInHierarchy && playerManager.playerObjects[1].myCharacter.fishing);
        static private bool IsDuringParryMinigame()
        => PseudoSingleton<GymMinigame>.instance.TryNonNull(out var gymMinigame) && gymMinigame.duringMinigame;
        static private void UpdateClockVisibility()
        {
            if (!PseudoSingleton<InGameClock>.instance.TryNonNull(out var clock))
                return;

            clock.clockText.transform.localScale = _hideClockTime ? Vector3.zero : Vector3.one;
            clock.dayText.transform.localScale = _hideClockDay ? Vector3.zero : Vector3.one;
        }

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Game speed
        [HarmonyPatch(typeof(Time), nameof(Time.timeScale), MethodType.Setter), HarmonyPrefix]
        static private void Time_timeScale_Post(ref float value)
        => value *= _gameSpeed;

        // In-game time
        [HarmonyPatch(typeof(gameTime), nameof(gameTime.InGameTimeCounter)), HarmonyPostfix]
        static private IEnumerator gameTime_InGameTimeCounter_Post(IEnumerator original, gameTime __instance)
        {
            while (true)
            {
                float timeFlow = _minutesPerSecond.Value;
                if (IsAnyPlayerFishing())
                    timeFlow *= _fishingTimeMultiplier.Value;
                if (IsDuringParryMinigame())
                    timeFlow *= _parryMinigameTimeMultiplier.Value;

                if (timeFlow > 0 && __instance.CanAddInGameSeconds())
                {
                    if (PseudoSingleton<InGameClock>.instance.TryNonNull(out var clock))
                    {
                        if (!_hideClockTime)
                            clock.clockText.transform.localScale = Vector3.one;
                        if (!_hideClockDay)
                            clock.dayText.transform.localScale = Vector3.one;
                        //clock.clockText.originalColor = new Color(1f, 1f, 1f, timeFlow / _minutesPerSecond);
                        clock.UpdateClockPosition();
                        clock.UpdateClock();
                    }
                    PseudoSingleton<Helpers>.instance.GetPlayerData().currentGameplayTime.AddInGameSeconds(1, false);
                    yield return new WaitForSeconds(timeFlow.Inv());
                }
                else
                {
                    if (PseudoSingleton<InGameClock>.instance.TryNonNull(out var clock))
                        clock.clockText.transform.localScale = clock.dayText.transform.localScale = Vector3.zero;
                    yield return new WaitForSeconds(1f);
                }
            }
        }

        [HarmonyPatch(typeof(gameTime), nameof(gameTime.CanAddInGameSeconds)), HarmonyPrefix]
        static private bool gameTime_CanAddInGameSeconds_Pre(gameTime __instance, ref bool __result)
        {
            __result = Time.timeScale >= 0f
                    && PseudoSingleton<LevelController>.instance != null
                    && !PlayerInfo.cutscene;
            return false;
        }

        // Hide clock
        [HarmonyPatch(typeof(InGameClock), nameof(InGameClock.Start)), HarmonyPostfix]
        static private void InGameClock_Start_Post(InGameClock __instance)
        => UpdateClockVisibility();

        // Framestop & Slowmotion
        [HarmonyPatch(typeof(gameTime), nameof(gameTime.SlowDownCoroutine)), HarmonyPostfix]
        static private IEnumerator gameTime_SlowDownCoroutine_Post(IEnumerator original, gameTime __instance, float stopTimeDuration, float duration)
        {
            original.MoveNext();
            float frameStopDuration = stopTimeDuration * _frameStopDurationMultiplier / _gameSpeed;
            if (frameStopDuration > 0f)
                yield return new WaitForSecondsRealtime(frameStopDuration);

            original.MoveNext();
            Time.timeScale = _slowMotionValue;
            float slowMotion = duration * _slowMotionDurationMultiplier / _gameSpeed;
            if (slowMotion > 0f)
                yield return new WaitForSecondsRealtime(slowMotion);

            while (original.MoveNext())
                yield return original.Current;
        }
    }
}