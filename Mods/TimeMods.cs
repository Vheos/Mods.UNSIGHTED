namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections;
    using UnityEngine;
    using HarmonyLib;
    using Tools.ModdingCore;
    using Tools.Extensions.General;
    using Tools.Extensions.Math;

    public class TimeMods : AMod
    {
        // Settings
        static private ModSetting<int> _engineSpeedMultiplier;
        static private ModSetting<int> _gameToEngineTimeRatio;
        static private ModSetting<int> _fishingTimerSpeed;
        static private ModSetting<int> _parryChallengeTimeMultiplier;
        static private ModSetting<int> _frameStopDurationMultiplier;
        static private ModSetting<int> _slowMotionDurationMultiplier;
        static private ModSetting<int> _slowMotionSpeed;
        override protected void Initialize()
        {
            _engineSpeedMultiplier = CreateSetting(nameof(_engineSpeedMultiplier), 100, IntRange(50, 200));
            _gameToEngineTimeRatio = CreateSetting(nameof(_gameToEngineTimeRatio), 48, IntRange(0, 120));
            _fishingTimerSpeed = CreateSetting(nameof(_fishingTimerSpeed), 0, IntRange(0, 100));
            _parryChallengeTimeMultiplier = CreateSetting(nameof(_parryChallengeTimeMultiplier), 0, IntRange(0, 100));
            _frameStopDurationMultiplier = CreateSetting(nameof(_frameStopDurationMultiplier), 100, IntRange(0, 200));
            _slowMotionDurationMultiplier = CreateSetting(nameof(_slowMotionDurationMultiplier), 100, IntRange(0, 200));
            _slowMotionSpeed = CreateSetting(nameof(_slowMotionSpeed), 30, IntRange(0, 100));

            // Events
            _engineSpeedMultiplier.AddEvent(() => Time.timeScale = 1f);
        }
        override protected void SetFormatting()
        {
            _engineSpeedMultiplier.Format("Engine speed");
            _engineSpeedMultiplier.Description =
                "How quickly the game runs (doesn't affect most UI)" +
                "\nLower to make the combat less dependent on quick reactions, and to make exploration under time limit less stressful" +
                "\n\nUnit: percent of original engine speed";
            _gameToEngineTimeRatio.Format("Timer speed");
            _gameToEngineTimeRatio.Description =
                "How quickly the timer (minutes, hours and days) progresses" +
                "\nValue of 100 means that 1 engine second (or minute, or hour) will progress the timer by 100 seconds (or minutes, or hours)" +
                "\nSet to 0 to stop all death timers, as well as the day cycle" +
                "\n\nUnit: ratio, essentialy game seconds per 1 engine second";
            using (Indent)
            {
                _fishingTimerSpeed.Format("fishing multiplier");
                _fishingTimerSpeed.Description =
                    "How quickly the timer progress when fishing" +
                    $"\n\nUnit: percent of total timer speed, accounting for \"{_gameToEngineTimeRatio.Name}\"";
                _parryChallengeTimeMultiplier.Format("parry challenge multiplier");
                _parryChallengeTimeMultiplier.Description =
                    "How quickly the timer progress when doing the parry challenge" +
                    $"\n\nUnit: percent of total timer speed, accounting for \"{_gameToEngineTimeRatio.Name}\"";
            }
            _frameStopDurationMultiplier.Format("Frame stop duration");
            _frameStopDurationMultiplier.Description =
                "How long the game freezes for when taking damage, performing heavy attacks, etc." +
                "\nLower values will make combat smoother and more predicatble in co-op, but less cinematic" +
                "\n\nUnit: percent of original frame stop duration";
            _slowMotionDurationMultiplier.Format("Slow motion duration");
            _slowMotionDurationMultiplier.Description =
                "How long the game remains in slow motion after a frame stop" +
                "\n\nUnit: percent of original slow motion duration ";
            using (Indent)
            {
                _slowMotionSpeed.Format("Slow motion speed", _slowMotionDurationMultiplier, t => t > 0);
                _slowMotionSpeed.Description =
                    "How much the game slows down after a frame stop" +
                    "\nValue of 0 will simply extend the frame stop duration" +
                    "\nValue of 100 will have no effect" +
                    $"\n\nUnit: percent of total engine speed, accounting for \"{_engineSpeedMultiplier.Name}\"";
            }
        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(Preset.Coop_NewGameExtra_HardMode):
                    ForceApply();
                    _engineSpeedMultiplier.Value = 67;
                    _gameToEngineTimeRatio.Value = 72;
                    _fishingTimerSpeed.Value = 20;
                    _parryChallengeTimeMultiplier.Value = 10;
                    _frameStopDurationMultiplier.Value = 75;
                    _slowMotionDurationMultiplier.Value = 0;
                    _slowMotionSpeed.Value = 0;
                    break;
            }
        }
        override protected string ModName
        => "Time";
        override protected string Description =>
            "Mods related to time, both in-game and engine" +
            "\n\nExamples:" +
            "\n• Change the whole engine speed" +
            "\n• Change the in-game timer speed" +
            "\n• Change the cinematic framestop / slowmo";

        // Privates
        static private bool IsAnyPlayerFishing()
        => PseudoSingleton<PlayersManager>.instance.TryNonNull(out var playerManager)
        && (playerManager.playerObjects[0].gameObject.activeInHierarchy && playerManager.playerObjects[0].myCharacter.fishing
        || playerManager.playerObjects[1].gameObject.activeInHierarchy && playerManager.playerObjects[1].myCharacter.fishing);
        static private bool IsDuringParryMinigame()
        => PseudoSingleton<GymMinigame>.instance.TryNonNull(out var gymMinigame) && gymMinigame.duringMinigame;

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Game speed
        [HarmonyPatch(typeof(Time), nameof(Time.timeScale), MethodType.Setter), HarmonyPrefix]
        static private void Time_timeScale_Setter_Pre(ref float value)
        => value *= _engineSpeedMultiplier / 100f;

        [HarmonyPatch(typeof(Time), nameof(Time.timeScale), MethodType.Getter), HarmonyPrefix]
        static private void Time_timeScale_Getter_Pre(ref float __result)
        => __result /= _engineSpeedMultiplier / 100f;

        // In-game time
        [HarmonyPatch(typeof(gameTime), nameof(gameTime.InGameTimeCounter)), HarmonyPostfix]
        static private IEnumerator gameTime_InGameTimeCounter_Post(IEnumerator original, gameTime __instance)
        {
            while (true)
            {
                float timeFlow = _gameToEngineTimeRatio.Value / 100f;
                if (IsAnyPlayerFishing())
                    timeFlow *= _fishingTimerSpeed.Value / 100f;
                if (IsDuringParryMinigame())
                    timeFlow *= _parryChallengeTimeMultiplier.Value / 100f;

                if (timeFlow > 0 && __instance.CanAddInGameSeconds())
                {
                    if (PseudoSingleton<InGameClock>.instance.TryNonNull(out var clock))
                    {
                        clock.UpdateClockPosition();
                        clock.UpdateClock();
                    }
                    PseudoSingleton<Helpers>.instance.GetPlayerData().currentGameplayTime.AddInGameSeconds(1, false);
                    yield return gameTime.WaitForSeconds(timeFlow.Inv());
                }
                else
                {
                    if (PseudoSingleton<InGameClock>.instance.TryNonNull(out var clock))
                        clock.clockText.transform.localScale = clock.dayText.transform.localScale = Vector3.zero;
                    yield return gameTime.WaitForSeconds(1f);
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

        // Framestop & Slowmotion
        [HarmonyPatch(typeof(gameTime), nameof(gameTime.SlowDownCoroutine)), HarmonyPostfix]
        static private IEnumerator gameTime_SlowDownCoroutine_Post(IEnumerator original, gameTime __instance, float stopTimeDuration, float duration)
        {
            original.MoveNext();
            float frameStopDuration = stopTimeDuration * (_frameStopDurationMultiplier / 100f) / (_engineSpeedMultiplier / 100f);
            if (frameStopDuration > 0f)
                yield return gameTime.IndependentWaitSeconds(frameStopDuration);

            original.MoveNext();
            Time.timeScale = _slowMotionSpeed;
            float slowMotion = duration * (_slowMotionDurationMultiplier / 100f) / (_engineSpeedMultiplier / 100f);
            if (slowMotion > 0f)
                yield return gameTime.IndependentWaitSeconds(slowMotion);

            while (original.MoveNext())
                yield return original.Current;
        }
    }
}