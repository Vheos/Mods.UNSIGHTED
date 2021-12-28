namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using HarmonyLib;
    using UnityEngine;
    using Tools.ModdingCore;
    using Tools.Extensions.Math;
    using Tools.Extensions.Collections;
    using Tools.Extensions.General;
    using Random = UnityEngine.Random;

    public class ParryChallenge : AMod
    {
        // Section
        override protected string SectionOverride
        => Sections.VARIOUS;
        override protected string ModName
        => "Parry Challenge";
        override protected string Description =>
            "Allows you to customize the parry challenge" +
            "\n\nExamples:" +
            "\n• Change spawns and thresholds for each wave" +
            "\n• Change thresholds for getting rewards" +
            "\n• Try out the 5 predefined presets";

        // Settings
        static private ModSetting<bool> _allowAttacking;
        static private ModSetting<int> _spawnInterval;
        static private ModSetting<bool> _spawnAnywhere;
        static private ModSetting<int> _spawnDistanceFromPlayers;
        static private ModSetting<bool> _separateFirstTimeRewards;
        static private ModSetting<bool> _resetFirstTimeRewards;
        static private ModSetting<SpawnPreset> _preset;
        static private ModSetting<Vector3> _rewardThresholds;
        static private ModSetting<Vector4>[] _spawnTable;
        override protected void Initialize()
        {
            _allowAttacking = CreateSetting(nameof(_allowAttacking), true);
            _spawnInterval = CreateSetting(nameof(_spawnInterval), 3, IntRange(0, 10));
            _spawnAnywhere = CreateSetting(nameof(_spawnAnywhere), false);
            _spawnDistanceFromPlayers = CreateSetting(nameof(_spawnDistanceFromPlayers), 10, IntRange(0, 50));
            _separateFirstTimeRewards = CreateSetting(nameof(_separateFirstTimeRewards), true);
            _resetFirstTimeRewards = CreateSetting(nameof(_resetFirstTimeRewards), false);
            _preset = CreateSetting(nameof(_preset), SpawnPreset.Vanilla);
            _rewardThresholds = CreateSetting(nameof(_rewardThresholds), new Vector3(15, 35, 80));
            _spawnTable = new ModSetting<Vector4>[9];
            for (int i = 0; i < _spawnTable.Length; i++)
                _spawnTable[i] = CreateSetting(nameof(_spawnTable) + (i + 1), new Vector4(0, 0, 0, 0));

            _preset.AddEvent(LoadSpawnPreset);
            _resetFirstTimeRewards.AddEvent(ResetFirstTimeRewardsFromConfig);
        }
        override protected void SetFormatting()
        {
            _allowAttacking.Format("Allow attacking");
            _allowAttacking.Description =
                "Allows attacking during the challenge, making it possible to control the number of attacking enemies by either killing or freezing them";
            _spawnInterval.Format("Spawn interval");
            _spawnInterval.Description =
                "How long is the pause between spawning new enemies after previous ones have been killed or new wave has started" +
                "\nSet to 0 to instantly spawn (and respawn) all enemies" +
                "\n\nUnit: seconds";
            _spawnAnywhere.Format("Spawn anywhere");
            _spawnAnywhere.Description =
                "Allows enemies to spawn anywhere within the ring" +
                "\nBy default, enemies only spawn in one of 2 points - whichever is further away from Player 1 at the time of spawn";
            using (Indent)
            {
                _spawnDistanceFromPlayers.Format("Spawn anywhere", _spawnAnywhere);
                _spawnDistanceFromPlayers.Description =
                    "Enemies can't spawn within this radius around the players" +
                    "\n\nUnit: percent of ring height";
            }
            _separateFirstTimeRewards.Format("Per-preset first-time rewards");
            _separateFirstTimeRewards.Description =
                "Each preset will keep track of their own first-time rewards";
            using (Indent)
            {
                _resetFirstTimeRewards.IsAdvanced = true;
                _resetFirstTimeRewards.Format("reset");
                _resetFirstTimeRewards.Description =
                    "Resets all first-time reward tags, allowing you to collect them again";
            }
            _preset.Format("Preset");
            _preset.Description =
                $"{SpawnPreset.Vanilla} - the original parry challenge" +
                $"\n{SpawnPreset.Spiders} - only spiders, +3 per wave" +
                $"\n{SpawnPreset.Gunners} - only gunners, +2 per wave" +
                $"\n{SpawnPreset.Sharks} - only sharks, +1 per wave" +
                $"\n{SpawnPreset.MixEasy} - spawns more spiders and gunners per wave, but only 1 shark" +
                $"\n{SpawnPreset.MixHard} - spawns more spiders, gunners, and sharks per wave";
            _rewardThresholds.Format("Reward thresholds");
            _rewardThresholds.Description =
                "How many parries you need to get each reward tier" +
                "\nX - first-time 100 bolts, then 25" +
                "\nY - first-time 500 bolts, then 100" +
                "\nZ - first-time 1500 bolts, then 500" +
                "\n\nEach tier also gives you all lower tier rewards" +
                "\nThat is, if you reach the highest tier on your first try ever, you'll get a total of 2100 bolts (1500 + 500 + 100)";
            CreateHeader("Waves").Description =
                "What enemies spawn in each wave and when does it start" +
                "\nX - number of parries required to start the wave" +
                "\nY - number of spiders in the wave" +
                "\nZ - number of gunner in the wave" +
                "\nW - number of sharks in the wave" +
                "\n\nRequired parries must be in ascending order (from lowest to highest)";
            using (Indent)
            {
                _spawnTable[0].Format($"#1");
                _spawnTable[1].Format($"#2");
                for (int i = 2; i < _spawnTable.Length; i++)
                    _spawnTable[i].Format($"#{i + 1}", _spawnTable[i - 1], t => t.x > 0);
            }
        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(SettingsPreset.Vheos_CoopRebalance):
                    ForceApply();
                    _allowAttacking.Value = true;
                    _spawnInterval.Value = 1;
                    _spawnAnywhere.Value = true;
                    _spawnDistanceFromPlayers.Value = 50;
                    _separateFirstTimeRewards.Value = true;
                    _preset.Value = SpawnPreset.MixHard;
                    break;
            }
        }

        // Privates
        static private readonly Rect SPAWN_RECT = Rect.MinMaxRect(-19, -9, +1, +1);
        static private void ResetFirstTimeRewardsFromConfig()
        {
            if (!_resetFirstTimeRewards)
                return;

            _resetFirstTimeRewards.SetSilently(false);
            PseudoSingleton<Helpers>.instance.GetPlayerData().dataStrings.RemoveAll(t => t.Contains("EnduranceReward"));
        }
        static private void LoadSpawnPreset()
        {
            foreach (var row in _spawnTable)
                row.Reset();

            switch (_preset.Value)
            {
                case SpawnPreset.Vanilla:
                    _rewardThresholds.Value = new Vector3(15, 35, 80);
                    _spawnTable[0].Value = new Vector4(0, 2, 0, 0);
                    _spawnTable[1].Value = new Vector4(6, 0, 2, 0);
                    _spawnTable[2].Value = new Vector4(16, 0, 0, 1);
                    _spawnTable[3].Value = new Vector4(26, 0, 0, 2);
                    _spawnTable[4].Value = new Vector4(36, 1, 1, 1);
                    _spawnTable[5].Value = new Vector4(51, 1, 2, 1);
                    _spawnTable[6].Value = new Vector4(101, 2, 2, 2);
                    break;
                case SpawnPreset.Spiders:
                    _spawnTable[0].Value = new Vector4(0, 3, 0, 0);
                    _spawnTable[1].Value = new Vector4(6, 6, 0, 0);
                    _spawnTable[2].Value = new Vector4(18, 9, 0, 0);
                    _spawnTable[3].Value = new Vector4(36, 12, 0, 0);
                    _spawnTable[4].Value = new Vector4(60, 15, 0, 0);
                    _spawnTable[5].Value = new Vector4(90, 18, 0, 0);
                    _spawnTable[6].Value = new Vector4(126, 21, 0, 0);
                    _spawnTable[7].Value = new Vector4(168, 24, 0, 0);
                    _spawnTable[8].Value = new Vector4(216, 27, 0, 0);
                    break;
                case SpawnPreset.Gunners:
                    _spawnTable[0].Value = new Vector4(0, 0, 2, 0);
                    _spawnTable[1].Value = new Vector4(8, 0, 4, 0);
                    _spawnTable[2].Value = new Vector4(24, 0, 6, 0);
                    _spawnTable[3].Value = new Vector4(48, 0, 8, 0);
                    _spawnTable[4].Value = new Vector4(80, 0, 10, 0);
                    _spawnTable[5].Value = new Vector4(120, 0, 12, 0);
                    _spawnTable[6].Value = new Vector4(168, 0, 14, 0);
                    _spawnTable[7].Value = new Vector4(224, 0, 16, 0);
                    _spawnTable[8].Value = new Vector4(288, 0, 18, 0);
                    break;
                case SpawnPreset.Sharks:
                    _spawnTable[0].Value = new Vector4(0, 0, 0, 1);
                    _spawnTable[1].Value = new Vector4(2, 0, 0, 2);
                    _spawnTable[2].Value = new Vector4(6, 0, 0, 3);
                    _spawnTable[3].Value = new Vector4(12, 0, 0, 4);
                    _spawnTable[4].Value = new Vector4(20, 0, 0, 5);
                    _spawnTable[5].Value = new Vector4(30, 0, 0, 6);
                    _spawnTable[6].Value = new Vector4(42, 0, 0, 7);
                    _spawnTable[7].Value = new Vector4(56, 0, 0, 8);
                    _spawnTable[8].Value = new Vector4(72, 0, 0, 9);
                    break;
                case SpawnPreset.MixEasy:
                    _spawnTable[0].Value = new Vector4(0, 1, 1, 1);
                    _spawnTable[1].Value = new Vector4(8, 2, 1, 1);
                    _spawnTable[2].Value = new Vector4(18, 2, 2, 1);
                    _spawnTable[3].Value = new Vector4(32, 3, 2, 1);
                    _spawnTable[4].Value = new Vector4(48, 3, 3, 1);
                    _spawnTable[5].Value = new Vector4(68, 4, 3, 1);
                    _spawnTable[6].Value = new Vector4(90, 4, 4, 1);
                    _spawnTable[7].Value = new Vector4(116, 5, 4, 1);
                    _spawnTable[8].Value = new Vector4(144, 5, 5, 1);
                    break;
                case SpawnPreset.MixHard:
                    _spawnTable[0].Value = new Vector4(0, 3, 0, 0);
                    _spawnTable[1].Value = new Vector4(6, 3, 2, 0);
                    _spawnTable[2].Value = new Vector4(20, 3, 2, 1);
                    _spawnTable[3].Value = new Vector4(36, 6, 2, 1);
                    _spawnTable[4].Value = new Vector4(58, 6, 4, 1);
                    _spawnTable[5].Value = new Vector4(88, 6, 4, 2);
                    _spawnTable[6].Value = new Vector4(120, 9, 4, 2);
                    _spawnTable[7].Value = new Vector4(158, 9, 6, 2);
                    _spawnTable[8].Value = new Vector4(204, 9, 6, 3);
                    break;
            }

            if (_preset != SpawnPreset.Vanilla)
                _rewardThresholds.Value = new Vector3(_spawnTable[2].Value.x, _spawnTable[4].Value.x, _spawnTable[6].Value.x);
        }
        static internal bool IsAnyGymMinigameActive()
        => PseudoSingleton<GymMinigame>.instance.TryNonNull(out var gymMinigame)
        && gymMinigame.duringMinigame;
        static internal bool IsParryChallengeActive()
        => IsAnyGymMinigameActive()
        && PseudoSingleton<GymMinigame>.instance.currentMinigameType == GymMinigameType.Endurance;

        // Defines
        private enum SpawnPreset
        {
            Vanilla = 0,
            Spiders,
            Gunners,
            Sharks,
            MixEasy,
            MixHard,
        }

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Skip intro
        [HarmonyPatch(typeof(GymMinigame), nameof(GymMinigame.EnduranceMinigame)), HarmonyPostfix]
        static private IEnumerator GymMinigame_EnduranceMinigame_Post(IEnumerator original, GymMinigame __instance)
        {
            yield return original.MoveNextThenGetCurrent();
            yield return original.MoveNextThenGetCurrent();
            while (PseudoSingleton<PopupManager>.instance.currentPopups.Count > 0)
                yield return original.MoveNextThenGetCurrent();

            // Dialogues
            DialogueSystem dialogueSystem = PseudoSingleton<DialogueSystem>.instance;
            int dialogueIndex = __instance.numberOfParries.AsFloat().ChooseThresholdValue
            (
                1,
                (_rewardThresholds.Value.x, 2),
                (_rewardThresholds.Value.y, 3),
                (_rewardThresholds.Value.z, 4)
            );
            yield return __instance.StartCoroutine(dialogueSystem.OpenDialogueBox(__instance.transform.position));
            yield return __instance.StartCoroutine(dialogueSystem.ShowDialogue(__instance.myNPC.npcName, $"Results{dialogueIndex}", __instance.gameObject, PortraitType.LeftPortrait, __instance.myNPC.portraitPrefab, "Idle", true, false, false, "", 0));
            yield return __instance.StartCoroutine(dialogueSystem.CloseDialogueBox());

            // Rewards
            List<string> playerDataStrings = PseudoSingleton<Helpers>.instance.GetPlayerData().dataStrings;
            string presetPostfix = _separateFirstTimeRewards && _preset == SpawnPreset.Vanilla
                                 ? _preset.Value.ToString()
                                 : "";
            if (__instance.numberOfParries >= _rewardThresholds.Value.z)
            {
                __instance.rewardName = playerDataStrings.TryAddUnique("HighEnduranceReward" + presetPostfix) ? "Bolts4" : "Bolts3";
                yield return __instance.GivePlayerEnduranceReward();
            }
            if (__instance.numberOfParries >= _rewardThresholds.Value.y)
            {
                __instance.rewardName = playerDataStrings.TryAddUnique("MediumEnduranceReward" + presetPostfix) ? "Bolts3" : "Bolts2";
                yield return __instance.GivePlayerEnduranceReward();
            }
            if (__instance.numberOfParries >= _rewardThresholds.Value.x)
            {
                __instance.rewardName = playerDataStrings.TryAddUnique("LowEnduranceReward" + presetPostfix) ? "Bolts2" : "Bolts1";
                yield return __instance.GivePlayerEnduranceReward();
            }

            __instance.myNPC.duringInteraction = true;
            yield return __instance.StartCoroutine(dialogueSystem.OpenDialogueBox(__instance.transform.position));
            yield return __instance.StartCoroutine(dialogueSystem.ShowDialogue(__instance.myNPC.npcName, "ChallengeTryAgain", __instance.gameObject, PortraitType.LeftPortrait, __instance.myNPC.portraitPrefab, "Idle", true, false, false, "", 0));
        }

        [HarmonyPatch(typeof(GymMinigame), nameof(GymMinigame.EnemySpawnCycle)), HarmonyPostfix]
        static private IEnumerator GymMinigame_EnemySpawnCycle_Post(IEnumerator original, GymMinigame __instance)
        {
            original.MoveNext();
            if (__instance.currentMinigameType != GymMinigameType.Endurance)
                yield return original.Current;

            original.MoveNextThenGetCurrent();

            while (true)
            {
                for (int i = _spawnTable.Length - 1; i >= 0; i--)
                    if (i == 0
                    || _spawnTable[i].IsVisible
                    && _spawnTable[i].Value.x > 0
                    && __instance.numberOfParries >= _spawnTable[i].Value.x)
                    {
                        __instance.maxSpiders = _spawnTable[i].Value.y.Round();
                        __instance.maxSlugs = _spawnTable[i].Value.z.Round();
                        __instance.maxSharks = _spawnTable[i].Value.w.Round();
                        break;
                    }

                __instance.CheckForDeadEnemies();
                if (__instance.spawnedSpiders < __instance.maxSpiders)
                    __instance.SpawnEnemy(__instance.spiderPrefab);
                else if (__instance.spawnedSlugs < __instance.maxSlugs)
                    __instance.SpawnEnemy(__instance.slugPrefab);
                else if (__instance.spawnedSharks < __instance.maxSharks)
                    __instance.SpawnEnemy(__instance.sharkPrefab);

                if (_spawnInterval > 0)
                    yield return gameTime.WaitForSeconds(_spawnInterval);
            }
        }

        [HarmonyPatch(typeof(GymMinigame), nameof(GymMinigame.EnemyParry)), HarmonyPrefix]
        static private bool GymMinigame_EnemyParry_Pre(GymMinigame __instance)
        {
            if (__instance.currentMinigameType != GymMinigameType.Endurance)
                return false;

            __instance.numberOfParries++;
            __instance.UpdateText();

            for (int i = _spawnTable.Length - 1; i >= 0; i--)
                if (i == 0
                || _spawnTable[i].IsVisible
                && _spawnTable[i].Value.x > 0
                && __instance.numberOfParries >= _spawnTable[i].Value.x)
                {
                    PseudoSingleton<FmodMusicController>.instance.SetMusicParameter("parrychallenge", i);
                    break;
                }

            return false;
        }

        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.DetectFireInput)), HarmonyPrefix]
        static private bool BasicCharacterController_DetectFireInput_Pre(BasicCharacterController __instance)
        => _allowAttacking || !IsParryChallengeActive();

        [HarmonyPatch(typeof(GymMinigame), nameof(GymMinigame.GetPositionAwayFromPlayer)), HarmonyPrefix]
        static private bool GymMinigame_GetPositionAwayFromPlayer_Pre(GymMinigame __instance, ref Vector3 __result)
        {
            int i = 0;
            bool isValid = false;
            while (!isValid)
            {
                __result = new Vector2(Random.Range(SPAWN_RECT.xMin, SPAWN_RECT.xMax),
                                       Random.Range(SPAWN_RECT.yMin, SPAWN_RECT.yMax));
                float minDistance = SPAWN_RECT.height * _spawnDistanceFromPlayers / 100f;

                i++;
                isValid = true;
                foreach (var player in PseudoSingleton<PlayersManager>.instance.players)
                    if (__result.DistanceTo(player.myCharacter.myPosition) <= minDistance)
                    {

                        isValid = false;
                        break;
                    }
            }
            return false;
        }
    }
}