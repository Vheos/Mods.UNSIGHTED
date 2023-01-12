﻿
using DG.Tweening;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vheos.Helpers.RNG;
using Vheos.Mods.Core;

namespace Vheos.Mods.UNSIGHTED;
public class Fishing : AMod
{
    // Section
    protected override string SectionOverride
    => Sections.BALANCE;
    protected override string Description =>
        "Mods related to the fishing minigame" +
        "\n\nExamples:" +
        "\n• Customize cues" +
        "\n• Change durations of each minigame stage" +
        "\n• Change required reaction times" +
        "\n• Customize fishing spot de/respawning ";

    // Settings
    private static ModSetting<Vector2> _initialWaitTime;
    private static ModSetting<Vector2> _minigameMaxDuration;
    private static ModSetting<Vector2> _bobInterval;
    private static ModSetting<int> _linearIntervalMultiplier;
    private static ModSetting<int> _chancePerFishingSpot;
    private static ModSetting<bool> _stopAfterMiss;
    private static ModSetting<bool> _startCue;
    private static ModSetting<CatchCues> _catchCues;
    private static ModSetting<int> _spotDespawnChance;
    private static ModSetting<int> _spotRespawnTime;
    private static ModSetting<Vector2> _thresholdsMagneticRod;
    private static ModSetting<Vector2> _thresholdsNeodymiumRod;
    protected override void Initialize()
    {
        _initialWaitTime = CreateSetting(nameof(_initialWaitTime), new Vector2(1, 6));
        _minigameMaxDuration = CreateSetting(nameof(_minigameMaxDuration), new Vector2(0, 8));
        _bobInterval = CreateSetting(nameof(_bobInterval), new Vector2(2, 2));
        _linearIntervalMultiplier = CreateSetting(nameof(_linearIntervalMultiplier), 100, IntRange(25, 400));

        _chancePerFishingSpot = CreateSetting(nameof(_chancePerFishingSpot), 100, IntRange(0, 200));
        _stopAfterMiss = CreateSetting(nameof(_stopAfterMiss), true);
        _startCue = CreateSetting(nameof(_startCue), true);
        _catchCues = CreateSetting(nameof(_catchCues), CatchCues.Visual | CatchCues.Audio | CatchCues.Haptic);

        _spotDespawnChance = CreateSetting(nameof(_spotDespawnChance), 50, IntRange(0, 100));
        _spotRespawnTime = CreateSetting(nameof(_spotRespawnTime), 10, IntRange(0, 120));

        _thresholdsMagneticRod = CreateSetting(nameof(_thresholdsMagneticRod), new Vector2(0.65f, 0.34f));
        _thresholdsNeodymiumRod = CreateSetting(nameof(_thresholdsNeodymiumRod), new Vector2(0.8f, 0.49f));

        // Events
        _catchCues.AddEvent(TryCacheRandomCues);
    }
    protected override void SetFormatting()
    {
        _initialWaitTime.Format("Initial wait time");
        _initialWaitTime.Description =
            "How long you have to wait for the minigame to actually start after you cast your fishing rod" +
            "\n\nUnit: seconds (random range)";
        _minigameMaxDuration.Format("Minigame duration");
        _minigameMaxDuration.Description =
            "How much time you have to catch something before it's gone for good" +
            "\n\nUnit: seconds (random range)";
        _bobInterval.Format("Bob interval");
        _bobInterval.Description =
            "How long is the wait between each bob (rod movement), whether it's a catch opportunity or not" +
            "\n\nUnit: seconds (random range)";
        using (Indent)
        {
            _linearIntervalMultiplier.Format("scale with time");
            _linearIntervalMultiplier.Description =
                "How long the intervals wll be by end of the minigame" +
                "\nSet to less than 100% to make the minigame slow down with time, so the end isn't as abrupt and obvious" +
                "\nEven though this setting might makes the minigame duration longer (or shorter), it does NOT affect the probabilities per fishing spot" +
                "\n\nUnit: percent of original interval duration";
        }

        _chancePerFishingSpot.Format("Average chance per spot");
        _chancePerFishingSpot.Description =
            "How likely you are to catch something per fishing spot if you play the minigame for its entire duration" +
            "\nValue of 50$ means that, on average, you'll get 1 item out of every 2 fishing spots" +
            "\nValue of 100$ does NOT ensure 1 item in every fishing spot (but it's likely that will be the case)" +
            "\n\nUnit: percent";
        _stopAfterMiss.Format("Stop after miss");
        _stopAfterMiss.Description =
            "Stops the minigame after missing your first catch opportunity" +
            $"\nUseful when playing with limited \"{_catchCues.Name}\"";
        _startCue.Format("Start cue");
        _startCue.Description =
            "Displays a green question mark to signal when the minigame starts, and waits for one maximum interval before the first bob";
        _catchCues.Format("Catch cues");
        _catchCues.Description =
            "How you will get know when to reel in" +
            "\n• Visual - red highlight + red exclamation mark" +
            "\n• Audio - distinct sound effect" +
            "\n• Haptic - vibrations (gamepads only)" +
            "\n• Random - one random cue out of the chosen ones";

        _spotDespawnChance.Format("Despawn chance");
        _spotDespawnChance.Description =
            "How often the fishing spots disappear after being used" +
             "\n\nUnit: percent";
        _spotRespawnTime.Format("Respawn time");
        _spotRespawnTime.Description =
            "How long you have to wait after exhausting a fishing spot before you can use it again" +
            "\n\nUnit: engine minutes";

        CreateHeader("Reaction times").Description =
            "How quickly you must reel in after a catch cue to get certain reward:" +
            "\n• X - normal reward" +
            "\n• Y - rare reward" +
            "\n\nUnit: seconds";
        using (Indent)
        {
            _thresholdsMagneticRod.Format("magnetic rod");
            _thresholdsNeodymiumRod.Format("neodymium rod");
        }
    }
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(SettingsPreset.Vheos_CoopRebalance):
                ForceApply();
                _initialWaitTime.Value = new Vector2(1.5f, 3);
                _minigameMaxDuration.Value = new Vector2(3, 6);
                _bobInterval.Value = new Vector2(2 / 3f, 4 / 3f);
                _linearIntervalMultiplier.Value = 200;
                _chancePerFishingSpot.Value = 150;
                _stopAfterMiss.Value = false;
                _startCue.Value = false;
                _catchCues.Value = CatchCues.Visual | CatchCues.Audio | CatchCues.Random;
                _spotDespawnChance.Value = 100;
                _spotRespawnTime.Value = 30;
                _thresholdsMagneticRod.Value = new Vector2(0.45f, 0.3f);
                _thresholdsNeodymiumRod.Value = new Vector2(0.6f, 0.4f);
                break;
        }
    }

    // Privates
    private static readonly List<CatchCues> _cachedRandomCues = new();
    private static void TryCacheRandomCues()
    {
        if (!_catchCues.Value.HasFlag(CatchCues.Random))
            return;

        _cachedRandomCues.Clear();
        _catchCues.Value.ForEachSetFlag(t =>
        {
            if (t != CatchCues.Random)
                _cachedRandomCues.Add(t);
        });
    }

    // Defines
    [Flags]
    private enum CatchCues
    {
        Visual = 1 << 0,
        Audio = 1 << 1,
        Haptic = 1 << 2,
        Random = 1 << 3,
    }

    // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

    // Skip intro

    // Fishing
    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.DetectFishCycle)), HarmonyPostfix]
    private static IEnumerator BasicCharacterController_DetectFishCycle_Post(IEnumerator original, BasicCharacterController __instance)
    {
        // Local function
        void BobAnimation()
        {
            AudioController.Play(GlobalGameManager.instance.gameSounds.rodMovement, 1.5f, 1f);
            __instance.myAnimations.myAnimator.mySpriteRenderer.transform.localPosition = new Vector3(0f, __instance.myAnimations.myAnimator.mySpriteRenderer.transform.localPosition.y, 0f);
            __instance.myAnimations.myAnimator.mySpriteRenderer.transform.DOMoveX(__instance.transform.position.x + 0.1f, 0.05f, false).SetLoops(2, LoopType.Yoyo);
            //GlobalInputManager.instance.WeakVibration(__instance.myInfo.playerNum, 0.05f);
        }

        void CatchOpportunityAnimation()
        {
            var cues = _catchCues.Value.HasFlag(CatchCues.Random)
                     ? _cachedRandomCues.RandomElement()
                     : _catchCues.Value;

            // Visual cue
            if (cues.HasFlag(CatchCues.Visual))
            {
                __instance.myAnimations.myAnimator.mySpriteRenderer.color = Color.red;
                __instance.myAnimations.myAnimator.mySpriteRenderer.DOColor(Color.white, 0.3f).SetLoops(1, LoopType.Yoyo);
                GameObject.Instantiate<GameObject>(Lists.instance.playerFoundReaction, __instance.myAnimations.myAnimator.mySpriteRenderer.transform.parent)
                    .transform.localPosition = new Vector3(0f, 1.6f, 0f);
            }

            // Audio cue
            var gameSounds = GlobalGameManager.instance.gameSounds;
            AudioController.Play(cues.HasFlag(CatchCues.Audio) ? gameSounds.rodCaught : gameSounds.rodMovement, 2f, 1f);

            // Haptic cue
            if (cues.HasFlag(CatchCues.Haptic))
                GlobalInputManager.instance.StrongVibration(__instance.myInfo.playerNum, 0.12f);

            __instance.myAnimations.myAnimator.mySpriteRenderer.transform.DOKill(false);
            __instance.myAnimations.myAnimator.mySpriteRenderer.transform.localPosition = new Vector3(0f, __instance.myAnimations.myAnimator.mySpriteRenderer.transform.localPosition.y, 0f);
            __instance.myAnimations.myAnimator.mySpriteRenderer.transform.DOMoveX(__instance.transform.position.x + 0.15f, 0.05f, false).SetLoops(2, LoopType.Yoyo);
        }

        yield return gameTime.WaitForSeconds(_initialWaitTime.Value.RandomRange());
        __instance.currentFishSpot.AddUse();
        if (_startCue)
        {
            GameObject.Instantiate<GameObject>(Lists.instance.doubtReaction, __instance.myAnimations.myAnimator.mySpriteRenderer.transform.parent)
                .transform.localPosition = new Vector3(0f, 1.6f, 0f);
            yield return gameTime.WaitForSeconds(_bobInterval.Value.CompMax());
        }

        float maxDuration = _minigameMaxDuration.Value.RandomRange();
        float chancePerBob = _chancePerFishingSpot / 100f * _bobInterval.Value.CompAvg() / maxDuration;
        float elapsed = 0f;
        while (elapsed < maxDuration)
        {
            float interval = _bobInterval.Value.RandomRange();
            elapsed += interval;
            float speedMultiplier = 1.Lerp(_linearIntervalMultiplier / 100f, elapsed / maxDuration);
            if (chancePerBob.Roll())
            {
                __instance.fishTime = Time.time;
                CatchOpportunityAnimation();
                if (_stopAfterMiss)
                    yield break;

                // prevent 2 opportunities in a row                 
                yield return gameTime.WaitForSeconds(_bobInterval.Value.RandomRange() * speedMultiplier);
            }

            BobAnimation();
            yield return gameTime.WaitForSeconds(interval * speedMultiplier);
        }
    }

    [HarmonyPatch(typeof(FishSpot), nameof(FishSpot.AddUse)), HarmonyPrefix]
    private static bool FishSpot_AddUse_Pre(FishSpot __instance)
    {
        if (__instance.uses == 0)
            __instance.SaveFishSpotUse();

        if (_spotDespawnChance.Value.Div(100f).Roll())
            __instance.uses = byte.MaxValue;

        return false;
    }

    [HarmonyPatch(typeof(FishSpot), nameof(FishSpot.FishSpotAvailable)), HarmonyPrefix]
    private static bool FishSpot_FishSpotAvailable_Pre(FishSpot __instance, ref bool __result)
    {
        __result = true;
        FishLocation fishSpotSave = __instance.GetFishSpotSave(MapManager.instance.playerRoom.sceneName, __instance.gameObject.name);
        if (fishSpotSave == null)
            return false;

        var playerData = UnsightedHelpers.instance.GetPlayerData();
        float currentMinute = playerData.currentGameplayTime.hours * 60 + playerData.currentGameplayTime.minutes;
        float despawnMinute = fishSpotSave.hours * 60 + fishSpotSave.minutes;
        if (currentMinute - despawnMinute >= _spotRespawnTime)
        {
            playerData.usedFishLocations.Remove(fishSpotSave);
            return false;
        }

        __result = false;
        return false;
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.StopFishingInput)), HarmonyPrefix]
    private static bool BasicCharacterController_StopFishingInput_Pre(BasicCharacterController __instance)
    {
        __instance.endFishingAnimation = true;
        float elapsed = Time.time - __instance.fishTime;
        Vector2 thresholds = UnsightedHelpers.instance.PlayerHaveItem("GoldenRod") > 0
                           ? _thresholdsNeodymiumRod
                           : _thresholdsMagneticRod;

        __instance.fishLevel = elapsed <= thresholds.y ? 2
                             : elapsed <= thresholds.x ? 1
                             : elapsed <= 2 * thresholds.x - thresholds.y ? -1
                             : 0;
        return false;
    }
}