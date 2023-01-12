﻿
using HarmonyLib;
using System.Collections;
using UnityEngine;
using Vheos.Mods.Core;

namespace Vheos.Mods.UNSIGHTED;
public class TimeMods : AMod, IUpdatable
{
    // Section
    protected override string SectionOverride
    => Sections.BALANCE;
    protected override string Description =>
        "Mods related to time, both in-game and engine" +
        "\n\nExamples:" +
        "\n• Change the whole engine speed" +
        "\n• Change the in-game timer speed" +
        "\n• Change the cinematic framestop / slowmo" +
        "\n• Override current day, hour and minute";

    // Settings
    private static ModSetting<int> _engineSpeedMultiplier;
    private static ModSetting<int> _cutsceneSpeedMultiplier;
    private static ModSetting<int> _gameToEngineTimeRatio;
    private static ModSetting<int> _fishingTimerSpeed;
    private static ModSetting<int> _parryChallengeTimeMultiplier;
    private static ModSetting<int> _frameStopDurationMultiplier;
    private static ModSetting<int> _slowMotionDurationMultiplier;
    private static ModSetting<int> _slowMotionSpeed;
    private static ModSetting<int> _day;
    private static ModSetting<int> _hour;
    private static ModSetting<int> _minute;
    protected override void Initialize()
    {
        _engineSpeedMultiplier = CreateSetting(nameof(_engineSpeedMultiplier), 100, IntRange(50, 200));
        _cutsceneSpeedMultiplier = CreateSetting(nameof(_cutsceneSpeedMultiplier), 100, IntRange(100, 400));
        _gameToEngineTimeRatio = CreateSetting(nameof(_gameToEngineTimeRatio), 48, IntRange(0, 120));
        _fishingTimerSpeed = CreateSetting(nameof(_fishingTimerSpeed), 0, IntRange(0, 100));
        _parryChallengeTimeMultiplier = CreateSetting(nameof(_parryChallengeTimeMultiplier), 0, IntRange(0, 100));

        _frameStopDurationMultiplier = CreateSetting(nameof(_frameStopDurationMultiplier), 100, IntRange(0, 200));
        _slowMotionDurationMultiplier = CreateSetting(nameof(_slowMotionDurationMultiplier), 100, IntRange(0, 200));
        _slowMotionSpeed = CreateSetting(nameof(_slowMotionSpeed), 30, IntRange(0, 100));

        _day = CreateSetting(nameof(_day), 0, IntRange(1, 100));
        _hour = CreateSetting(nameof(_hour), 0, IntRange(0, 23));
        _minute = CreateSetting(nameof(_minute), 0, IntRange(0, 59));

        // Events
        _engineSpeedMultiplier.AddEventSilently(() => Time.timeScale = 1f);
        AddEventOnEnabled(RestartTimerCoroutine);
        AddEventOnDisabled(RestartTimerCoroutine);
        AddEventOnConfigOpened(ReadGameTime);
        _day.AddEventSilently(() => GameTime.inGameDays = _day);
        _hour.AddEventSilently(() => GameTime.inGameHours = _hour);
        _minute.AddEventSilently(() => GameTime.inGameMinutes = _minute);
    }
    protected override void SetFormatting()
    {
        _engineSpeedMultiplier.Format("Engine speed");
        _engineSpeedMultiplier.Description =
            "How quickly the game runs (doesn't affect most UI)" +
            "\nLower to make the combat less dependent on quick reactions, and to make exploration under time limit less stressful" +
            "\n\nUnit: percent of original engine speed";
        using (Indent)
        {
            _cutsceneSpeedMultiplier.Format("cutscene speed");
            _cutsceneSpeedMultiplier.Description =
                "How quickly the game runs when you can't control your character" +
                $"\n\nUnit: percent of \"{_engineSpeedMultiplier.Name}\"";
        }

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
                $"\n\nUnit: percent of \"{_gameToEngineTimeRatio.Name}\"";
            _parryChallengeTimeMultiplier.Format("parry challenge multiplier");
            _parryChallengeTimeMultiplier.Description =
                "How quickly the timer progress when doing the parry challenge" +
                $"\n\nUnit: percent of \"{_gameToEngineTimeRatio.Name}\"";
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
            _slowMotionSpeed.Format("slow motion speed", _slowMotionDurationMultiplier, t => t > 0);
            _slowMotionSpeed.Description =
                "How much the game slows down after a frame stop" +
                "\nValue of 0 will simply extend the frame stop duration" +
                "\nValue of 100 will have no effect" +
                $"\n\nUnit: percent of total engine speed, accounting for \"{_engineSpeedMultiplier.Name}\"";
        }

        CreateHeader("Game time").Description =
            "Allows you to change current day, hour and minute without affecting death timers";
        using (Indent)
        {
            _day.DisplayResetButton = false;
            _day.Format("day");
            _hour.DisplayResetButton = false;
            _hour.Format("hour");
            _minute.DisplayResetButton = false;
            _minute.Format("minute");
        }
    }
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(SettingsPreset.Vheos_CoopRebalance):
                ForceApply();
                _engineSpeedMultiplier.Value = 67;
                _cutsceneSpeedMultiplier.Value = 200;
                _gameToEngineTimeRatio.Value = 72;
                _fishingTimerSpeed.Value = 25;
                _parryChallengeTimeMultiplier.Value = 10;
                _frameStopDurationMultiplier.Value = 50;
                _slowMotionDurationMultiplier.Value = 0;
                _slowMotionSpeed.Value = 0;
                break;
        }
    }
    protected override string ModName
    => "Time";
    public void OnUpdate()
    {
        if (!_previousIsCutscene && PlayerInfo.cutscene)
            Time.timeScale = _cutsceneSpeedMultiplier / 100f;
        else if (_previousIsCutscene && !PlayerInfo.cutscene)
            Time.timeScale = 1f;

        _previousIsCutscene = PlayerInfo.cutscene;
    }

    // Privates
    private static bool _previousIsCutscene;
    private static bool IsAnyPlayerFishing()
    => PlayersManager.instance.TryNonNull(out var playerManager)
    && (playerManager.playerObjects[0].gameObject.activeInHierarchy && playerManager.playerObjects[0].myCharacter.fishing
    || playerManager.playerObjects[1].gameObject.activeInHierarchy && playerManager.playerObjects[1].myCharacter.fishing);
    private static void RestartTimerCoroutine()
    {
        if (!global::gameTime.instance.TryNonNull(out var gameTime))
            return;

        gameTime.StopCoroutine("InGameTimeCounter");
        gameTime.StartCoroutine("InGameTimeCounter");
    }
    private static void ReadGameTime()
    {
        if (UnsightedHelpers.instance == null)
            return;

        _day.SetSilently(GameTime.inGameDays);
        _hour.SetSilently(GameTime.inGameHours);
        _minute.SetSilently(GameTime.inGameMinutes);
    }
    private static GameplayTime GameTime
    => UnsightedHelpers.instance.GetPlayerData().currentGameplayTime;

    // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

    // Game speed
    [HarmonyPatch(typeof(Time), nameof(Time.timeScale), MethodType.Setter), HarmonyPrefix]
    private static void Time_timeScale_Setter_Pre(ref float value)
    => value *= _engineSpeedMultiplier / 100f;

    [HarmonyPatch(typeof(Time), nameof(Time.timeScale), MethodType.Getter), HarmonyPrefix]
    private static void Time_timeScale_Getter_Pre(ref float __result)
    => __result /= _engineSpeedMultiplier / 100f;

    // In-game time
    [HarmonyPatch(typeof(gameTime), nameof(gameTime.InGameTimeCounter)), HarmonyPostfix]
    private static IEnumerator gameTime_InGameTimeCounter_Post(IEnumerator original, gameTime __instance)
    {
        while (true)
        {
            float timeFlow = _gameToEngineTimeRatio.Value / 100f;
            if (IsAnyPlayerFishing())
                timeFlow *= _fishingTimerSpeed.Value / 100f;
            if (ParryChallenge.IsAnyGymMinigameActive())
                timeFlow *= _parryChallengeTimeMultiplier.Value / 100f;

            if (timeFlow > 0 && __instance.CanAddInGameSeconds())
            {
                if (InGameClock.instance.TryNonNull(out var clock))
                {
                    clock.UpdateClockPosition();
                    clock.UpdateClock();
                }

                UnsightedHelpers.instance.GetPlayerData().currentGameplayTime.AddInGameSeconds(1, false);
                yield return gameTime.WaitForSeconds(timeFlow.Inv());
            }
            else
                yield return gameTime.WaitForSeconds(1f);
        }
    }

    [HarmonyPatch(typeof(gameTime), nameof(gameTime.CanAddInGameSeconds)), HarmonyPrefix]
    private static bool gameTime_CanAddInGameSeconds_Pre(gameTime __instance, ref bool __result)
    {
        __result = Time.timeScale > 0f
                && LevelController.instance != null
                && !PlayerInfo.cutscene;
        return false;
    }

    // Framestop & Slowmotion
    [HarmonyPatch(typeof(gameTime), nameof(gameTime.SlowDownCoroutine)), HarmonyPostfix]
    private static IEnumerator gameTime_SlowDownCoroutine_Post(IEnumerator original, gameTime __instance, float stopTimeDuration, float duration)
    {
        original.MoveNext();
        float frameStopDuration = stopTimeDuration * (_frameStopDurationMultiplier / 100f) / (_engineSpeedMultiplier / 100f);
        if (frameStopDuration > 0f)
            yield return gameTime.IndependentWaitSeconds(frameStopDuration);

        original.MoveNext();
        Time.timeScale = _slowMotionSpeed / 100f;
        float slowMotion = duration * (_slowMotionDurationMultiplier / 100f) / (_engineSpeedMultiplier / 100f);
        if (slowMotion > 0f)
            yield return gameTime.IndependentWaitSeconds(slowMotion);

        while (original.MoveNext())
            yield return original.Current;
    }
}