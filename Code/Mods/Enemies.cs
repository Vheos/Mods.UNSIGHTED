namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using HarmonyLib;
    using Tools.ModdingCore;
    using Tools.Extensions.UnityObjects;
    using Tools.Extensions.Math;

    public class Enemies : AMod
    {
        // Section
        override protected string SectionOverride
        => Sections.BALANCE;
        override protected string Description =>
            "Mods related to enemies" +
            "\n\nExamples:" +
            "\n• Customize stagger behaviours" +
            "\n• Scale enemies' and bosses' HP" +
            "\n• Make enemies in groups attack more often";

        // Settings
        static private ModSetting<int> _staggerTimeMultiplier;
        static private ModSetting<bool> _staggerAttackingEnemies;
        static private ModSetting<StaggerImmunities> _staggerImmunities;
        static private ModSetting<int> _enemyHPMultiplier;
        static private ModSetting<int> _enemyBossHPMultiplier;
        static private ModSetting<bool> _randomizeGroupAttacks;
        override protected void Initialize()
        {
            _staggerTimeMultiplier = CreateSetting(nameof(_staggerTimeMultiplier), 100, IntRange(0, 200));
            _staggerAttackingEnemies = CreateSetting(nameof(_staggerAttackingEnemies), true);
            _staggerImmunities = CreateSetting(nameof(_staggerImmunities), (StaggerImmunities)~0);
            _enemyHPMultiplier = CreateSetting(nameof(_enemyHPMultiplier), 100, IntRange(25, 400));
            _enemyBossHPMultiplier = CreateSetting(nameof(_enemyBossHPMultiplier), 100, IntRange(25, 400));
            _randomizeGroupAttacks = CreateSetting(nameof(_randomizeGroupAttacks), false);
        }
        override protected void SetFormatting()
        {
            _staggerTimeMultiplier.Format("Stagger time");
            _staggerTimeMultiplier.Description =
                "How long is the enemies' stagger animation" +
                "\nSet to 0 to make enemies almost ignore your attacks" +
                "\nUnit: percent of original animation duration";
            _staggerAttackingEnemies.Format("Stagger attacking enemies");
            _staggerAttackingEnemies.Description =
                "Allows you to disable staggering enemies when they're attacking" +
                "\nWhen disabled, enemies will follow their default in-game behaviour";
            _staggerImmunities.Format("Stagger immunities");
            _staggerImmunities.Description =
                "Allows you to make all enemies immune to being staggered by chosen attack types" +
                "\nDisabled attack types will follow their default in-game behaviour";

            _enemyHPMultiplier.Format("HP");
            _enemyHPMultiplier.Description =
                "How much HP enemies have (also affects bosses)" +
                "\nHigher values are recommended for co-op, as enemy HP doesn't scale with player count" +
                "\n\nUnit: percent of original enemy HP";
            _enemyBossHPMultiplier.Format("Boss HP");
            _enemyBossHPMultiplier.Description =
                "How much HP bosses have" +
                $"\n\nUnit: percent of boss HP, stacks with \"{_enemyHPMultiplier.Name}\"";
            _randomizeGroupAttacks.Format("Randomize group attacks");
            _randomizeGroupAttacks.Description =
                "Randomizes the attacking \"rhythm\" of enemies' groups" +
                "\nBy default, most enemies can't attack at the same time" +
                "\nInstead, every 0.75sec one random enemy will start attacking" +
                "\nThis settings makes some enemies attack earlier than expected, sometimes simultaneously with others";
        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(SettingsPreset.Vheos_CoopRebalance):
                    ForceApply();
                    _staggerImmunities.Value = StaggerImmunities.DogAndIris | StaggerImmunities.Gun;
                    _staggerTimeMultiplier.Value = 50;
                    _staggerAttackingEnemies.Value = true;
                    _enemyHPMultiplier.Value = 200;
                    _enemyBossHPMultiplier.Value = 125;
                    _randomizeGroupAttacks.Value = true;
                    break;
            }
        }

        // Privates
        static private void TryApplyStaggerTimeMultiplier(EnemyController enemyController)
        => enemyController.hitTime *= _staggerTimeMultiplier / 100f;

        // Defines
        [Flags]
        private enum StaggerImmunities
        {
            Sword = 1 << 0,
            Axe = 1 << 1,
            Gun = 1 << 2,
            Shuriken = 1 << 3,
            DogAndIris = 1 << 4,
        }

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Stagger
        [HarmonyPatch(typeof(EnemyController), nameof(EnemyController.Start)), HarmonyPostfix]
        static private void EnemyController_Start_Post(EnemyController __instance)
        => TryApplyStaggerTimeMultiplier(__instance);

        [HarmonyPatch(typeof(ArcherEnemy), nameof(ArcherEnemy.Start)), HarmonyPostfix]
        static private void ArcherEnemy_Start_Post(ArcherEnemy __instance)
        => TryApplyStaggerTimeMultiplier(__instance);

        [HarmonyPatch(typeof(ScrapRobotEnemy), nameof(ScrapRobotEnemy.Start)), HarmonyPostfix]
        static private void ScrapRobotEnemy_Start_Post(ScrapRobotEnemy __instance)
        => TryApplyStaggerTimeMultiplier(__instance);

        [HarmonyPatch(typeof(EnemyController), nameof(EnemyController.GotHitBounceBack)), HarmonyPrefix]
        static private bool EnemyController_GotHitBounceBack_Pre(EnemyController __instance, ref Atributes hitObject)
        {
            if (__instance.attacking && !_staggerAttackingEnemies
            || hitObject.isDog && _staggerImmunities.Value.HasFlag(StaggerImmunities.DogAndIris)
            || hitObject.myType == HitObjectType.SmallBullet && _staggerImmunities.Value.HasFlag(StaggerImmunities.Gun))
                return false;

            if (hitObject.myType == HitObjectType.PlayerMelee)
                if (hitObject.meleeWeaponClass == MeleeWeaponClass.Sword && _staggerImmunities.Value.HasFlag(StaggerImmunities.Sword)
                || hitObject.meleeWeaponClass == MeleeWeaponClass.Axe && _staggerImmunities.Value.HasFlag(StaggerImmunities.Axe)
                || hitObject.meleeWeaponClass == MeleeWeaponClass.Shuriken && _staggerImmunities.Value.HasFlag(StaggerImmunities.Shuriken))
                    return false;

            return true;
        }

        // Enemy HP
        [HarmonyPatch(typeof(Atributes), nameof(Atributes.OnEnable)), HarmonyPrefix]
        static private bool Atributes_OnEnable_Pre(Atributes __instance)
        {
            if (__instance.myOwner == null && __instance.transform.parent != null)
                __instance.myOwner = __instance.transform.parent.GetComponent<SortingObject>();

            if (!__instance.appliedLifeDifficulty)
            {
                float multiplier = _enemyHPMultiplier / 100f;
                if (__instance.TryGetComponent<EnemyHitBox>(out var enemyHitBox)
                && enemyHitBox.bossBar)
                    multiplier *= _enemyBossHPMultiplier / 100f;

                __instance.totalLife = __instance.totalLife.Mul(multiplier).Round();
                __instance.appliedLifeDifficulty = true;
            }

            return false;
        }

        // Attack rhythm
        [HarmonyPatch(typeof(EnemyController), nameof(EnemyController.AttackCoroutine)), HarmonyPostfix]
        static private IEnumerator EnemyController_AttackCoroutine_Post(IEnumerator original, EnemyController __instance, bool dontRegisterAttack, bool waitForAttackRythm)
        {
            if (waitForAttackRythm)
            {
                float interval = _randomizeGroupAttacks
                               ? UnityEngine.Random.Range(0f, 0.75f)
                               : 0.75f;

                while (Time.time - EnemyController.lastAttackTime < interval)
                    yield return gameTime.WaitForSeconds(0.1f);
            }

            float previousLastAttackTime = EnemyController.lastAttackTime;
            EnemyController.lastAttackTime = 0f;
            original.MoveNext();
            if (dontRegisterAttack)
                EnemyController.lastAttackTime = previousLastAttackTime;
            yield return original.Current;

            while (original.MoveNext())
                yield return original.Current;
        }
    }
}