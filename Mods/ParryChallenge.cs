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

    public class ParryChallenge : AMod
    {
        // Settings
        static private ModSetting<SpawnPreset> _preset;
        static private ModSetting<float> _spawnInterval;
        static private ModSetting<Vector4>[] _spawnData;
        static private ModSetting<Vector3> _rewardThresholds;
        static private ModSetting<bool> _removeRewardPlayerStrings;
        override protected void Initialize()
        {
            _preset = CreateSetting(nameof(_preset), SpawnPreset.Vanilla);
            _spawnInterval = CreateSetting(nameof(_spawnInterval), 3f, FloatRange(0f, 5f));
            _rewardThresholds = CreateSetting(nameof(_rewardThresholds), new Vector3(15, 35, 80));
            _spawnData = new ModSetting<Vector4>[9];
            for (int i = 0; i < _spawnData.Length; i++)
                _spawnData[i] = CreateSetting(nameof(_spawnData) + (i + 1), new Vector4(-1, 0, 0, 0));
            _removeRewardPlayerStrings = CreateSetting(nameof(_removeRewardPlayerStrings), false);


            _removeRewardPlayerStrings.AddEvent(RemoveRewardPlayerStrings);
            _preset.AddEvent(LoadPreset);
        }
        override protected void SetFormatting()
        {
            _spawnInterval.Format("Spawn interval");
            _removeRewardPlayerStrings.Format("Remove reward player strings");
            _preset.Format("Preset");
            for (int i = 0; i < _spawnData.Length; i++)
                _spawnData[i].Format($"Spawn data {i + 1}");
            _rewardThresholds.Format("Reward thresholds");
        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(Preset.Coop_NewGameExtra_HardMode):
                    ForceApply();
                    _preset.Value = SpawnPreset.Vanilla;
                    _spawnInterval.Value = 1f;
                    break;
            }
        }

        // Privates
        void RemoveRewardPlayerStrings()
        {
            if (!_removeRewardPlayerStrings)
                return;

            PseudoSingleton<Helpers>.instance.GetPlayerData().dataStrings.RemoveAll(t => t.Contains("EnduranceReward"));
            _removeRewardPlayerStrings.SetSilently(false);
        }
        static private void LoadPreset()
        {
            foreach (var spawnData in _spawnData)
                spawnData.Reset();

            switch (_preset.Value)
            {
                case SpawnPreset.Vanilla:
                    _spawnData[0].Value = new Vector4(0, 2, 0, 0);
                    _spawnData[1].Value = new Vector4(6, 0, 2, 0);
                    _spawnData[2].Value = new Vector4(16, 0, 0, 1);
                    _spawnData[3].Value = new Vector4(26, 0, 0, 2);
                    _spawnData[4].Value = new Vector4(36, 1, 1, 1);
                    _spawnData[5].Value = new Vector4(51, 1, 2, 1);
                    _spawnData[6].Value = new Vector4(101, 2, 2, 2);
                    _rewardThresholds.Value = new Vector3(15, 35, 80);
                    _spawnInterval.Value = 3f;
                    break;
                case SpawnPreset.Spiders:
                    _spawnData[0].Value = new Vector4(0, 3, 0, 0);
                    _spawnData[1].Value = new Vector4(6, 6, 0, 0);
                    _spawnData[2].Value = new Vector4(18, 9, 0, 0);
                    _spawnData[3].Value = new Vector4(36, 12, 0, 0);
                    _spawnData[4].Value = new Vector4(60, 15, 0, 0);
                    _spawnData[5].Value = new Vector4(90, 18, 0, 0);
                    _spawnData[6].Value = new Vector4(126, 21, 0, 0);
                    _spawnData[7].Value = new Vector4(168, 24, 0, 0);
                    _spawnData[8].Value = new Vector4(216, 27, 0, 0);
                    break;
                case SpawnPreset.Gunners:
                    _spawnData[0].Value = new Vector4(0, 0, 2, 0);
                    _spawnData[1].Value = new Vector4(8, 0, 4, 0);
                    _spawnData[2].Value = new Vector4(24, 0, 6, 0);
                    _spawnData[3].Value = new Vector4(48, 0, 8, 0);
                    _spawnData[4].Value = new Vector4(80, 0, 10, 0);
                    _spawnData[5].Value = new Vector4(120, 0, 12, 0);
                    _spawnData[6].Value = new Vector4(168, 0, 14, 0);
                    _spawnData[7].Value = new Vector4(224, 0, 16, 0);
                    _spawnData[8].Value = new Vector4(288, 0, 18, 0);
                    break;
                case SpawnPreset.Sharks:
                    _spawnData[0].Value = new Vector4(0, 0, 0, 0);
                    _spawnData[1].Value = new Vector4(2, 0, 0, 0);
                    _spawnData[2].Value = new Vector4(6, 0, 0, 0);
                    _spawnData[3].Value = new Vector4(12, 0, 0, 0);
                    _spawnData[4].Value = new Vector4(20, 0, 0, 0);
                    _spawnData[5].Value = new Vector4(30, 0, 0, 0);
                    _spawnData[6].Value = new Vector4(42, 0, 0, 0);
                    _spawnData[7].Value = new Vector4(56, 0, 0, 0);
                    _spawnData[8].Value = new Vector4(72, 0, 0, 0);
                    break;
                case SpawnPreset.CombinedEasy:
                    _spawnData[0].Value = new Vector4(0, 1, 1, 1);
                    _spawnData[1].Value = new Vector4(8, 2, 1, 1);
                    _spawnData[2].Value = new Vector4(18, 2, 2, 1);
                    _spawnData[3].Value = new Vector4(32, 3, 2, 1);
                    _spawnData[4].Value = new Vector4(48, 3, 3, 1);
                    _spawnData[5].Value = new Vector4(68, 4, 3, 1);
                    _spawnData[6].Value = new Vector4(90, 4, 4, 1);
                    _spawnData[7].Value = new Vector4(116, 5, 4, 1);
                    _spawnData[8].Value = new Vector4(144, 5, 5, 1);
                    break;
                case SpawnPreset.CombinedHard:
                    _spawnInterval.Value = 3f;
                    _spawnData[0].Value = new Vector4(0, 3, 0, 0);
                    _spawnData[1].Value = new Vector4(6, 3, 2, 0);
                    _spawnData[2].Value = new Vector4(20, 3, 2, 1);
                    _spawnData[3].Value = new Vector4(36, 6, 2, 1);
                    _spawnData[4].Value = new Vector4(58, 6, 4, 1);
                    _spawnData[5].Value = new Vector4(88, 6, 4, 2);
                    _spawnData[6].Value = new Vector4(120, 9, 4, 2);
                    _spawnData[7].Value = new Vector4(158, 9, 6, 2);
                    _spawnData[8].Value = new Vector4(204, 9, 6, 3);
                    break;
            }

            if (_preset != SpawnPreset.Vanilla)
                _rewardThresholds.Value = new Vector3(_spawnData[2].Value.x, _spawnData[4].Value.x, _spawnData[6].Value.x);
        }

        // Defines
        private enum SpawnPreset
        {
            Vanilla = 0,
            Spiders,
            Gunners,
            Sharks,
            CombinedEasy,
            CombinedHard,
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
            string presetPostfix = _preset == SpawnPreset.Vanilla ? "" : _preset.Value.ToString();
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
                for (int i = _spawnData.Length - 1; i >= 0; i--)
                    if (_spawnData[i].Value.x >= 0
                    && __instance.numberOfParries >= _spawnData[i].Value.x)
                    {
                        __instance.maxSpiders = _spawnData[i].Value.y.Round();
                        __instance.maxSlugs = _spawnData[i].Value.z.Round();
                        __instance.maxSharks = _spawnData[i].Value.w.Round();
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
                    yield return new WaitForSeconds(_spawnInterval);
            }
        }

        [HarmonyPatch(typeof(GymMinigame), nameof(GymMinigame.EnemyParry)), HarmonyPrefix]
        static private bool GymMinigame_EnemyParry_Pre(GymMinigame __instance)
        {
            if (__instance.currentMinigameType != GymMinigameType.Endurance)
                return false;

            __instance.numberOfParries++;
            __instance.UpdateText();

            for (int i = _spawnData.Length - 1; i >= 0; i--)
                if (_spawnData[i].Value.x >= 0
                && __instance.numberOfParries >= _spawnData[i].Value.x)
                {
                    PseudoSingleton<FmodMusicController>.instance.SetMusicParameter("parrychallenge", i);
                    break;
                }

            return false;
        }
    }
}