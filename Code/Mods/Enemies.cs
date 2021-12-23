namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections;
    using UnityEngine;
    using HarmonyLib;
    using Tools.ModdingCore;
    using Tools.Extensions.UnityObjects;
    using Tools.Extensions.Math;
    using System.Collections.Generic;
    using Vheos.Tools.UtilityN;

    public class Enemies : AMod
    {
        // Section
        override protected string SectionOverride
        => Sections.BALANCE;
        override protected string Description =>
            "Mods related to enemies" +
            "\n\nExamples:" +
            "\n• Customize stagger system" +
            "\n• Scale enemies' and bosses' HP" +
            "\n• Make enemies in groups attack more often";

        // Settings
        static private ModSetting<int> _staggerTimeMultiplier;
        static private Dictionary<EnemyState, ModSetting<AttackTypes>> _staggerImmunitiesTable;
        static private ModSetting<int> _hpMultiplier;
        static private ModSetting<int> _bossHPMultiplier;
        static private ModSetting<bool> _randomizeGroupAttacks;
        override protected void Initialize()
        {
            _staggerTimeMultiplier = CreateSetting(nameof(_staggerTimeMultiplier), 100, IntRange(0, 200));
            _staggerImmunitiesTable = new Dictionary<EnemyState, ModSetting<AttackTypes>>();
            foreach (var enemyState in Utility.GetEnumValues<EnemyState>())
                _staggerImmunitiesTable[enemyState] = CreateSetting(nameof(_staggerImmunitiesTable) + enemyState, (AttackTypes)0);

            _hpMultiplier = CreateSetting(nameof(_hpMultiplier), 100, IntRange(25, 400));
            _bossHPMultiplier = CreateSetting(nameof(_bossHPMultiplier), 100, IntRange(25, 400));

            _randomizeGroupAttacks = CreateSetting(nameof(_randomizeGroupAttacks), false);
        }
        override protected void SetFormatting()
        {
            _staggerTimeMultiplier.Format("Stagger time");
            _staggerTimeMultiplier.Description =
                "How long is the enemies' stagger animation" +
                "\nSet to 0 to make enemies almost ignore your attacks" +
                "\nUnit: percent of original animation duration";
            CreateHeader("Stagger immunities").Description =
                "Define when enemies will be unstaggerable by chosen attack types" +
                $"\nFor example, if you tick \"{AttackTypes.DogAndIris}\" in the \"{EnemyState.Attacking}\" row, " +
                "dogs and Iris won't be able to stop the enemy when it's already attacking" +
                "\nThis is useful when you like their extra help, but don't want them to mess up your parry timings";
            using (Indent)
            {
                _staggerImmunitiesTable[EnemyState.Idle].Format("idle");
                _staggerImmunitiesTable[EnemyState.Idle].Description =
                    "Enemy is not attacking or preparing an attack";
                _staggerImmunitiesTable[EnemyState.Preparing].Format("preparing");
                _staggerImmunitiesTable[EnemyState.Preparing].Description =
                    "Enemy is preparing to attack";
                _staggerImmunitiesTable[EnemyState.Attacking].Format("attacking");
                _staggerImmunitiesTable[EnemyState.Attacking].Description =
                    "Enemy is attacking";
            }

            _hpMultiplier.Format("HP");
            _hpMultiplier.Description =
                "How much HP enemies have (also affects bosses)" +
                "\nHigher values are recommended for co-op, as enemy HP doesn't scale with player count" +
                "\n\nUnit: percent of original enemy HP";
            using (Indent)
            {
                _bossHPMultiplier.Format("boss HP");
                _bossHPMultiplier.Description =
                    "How much HP bosses have" +
                    $"\n\nUnit: percent of boss HP, stacks with \"{_hpMultiplier.Name}\"";
            }

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
                    _staggerTimeMultiplier.Value = 75;
                    _staggerImmunitiesTable[EnemyState.Idle].Value = 0;
                    _staggerImmunitiesTable[EnemyState.Preparing].Value = AttackTypes.Gun | AttackTypes.DogAndIris;
                    _staggerImmunitiesTable[EnemyState.Attacking].Value = (AttackTypes)~0 & ~AttackTypes.Axe;
                    _hpMultiplier.Value = 200;
                    _bossHPMultiplier.Value = 125;
                    _randomizeGroupAttacks.Value = true;
                    break;
            }
        }

        // Privates
        private const float ORIGINAL_SHORT_STAGGER_DURATION = 0.3f;

        // Defines
        private enum EnemyState
        {
            Idle,
            Preparing,
            Attacking,
        }
        [Flags]
        private enum AttackTypes
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
        => __instance.hitTime *= _staggerTimeMultiplier / 100f;

        [HarmonyPatch(typeof(ArcherEnemy), nameof(ArcherEnemy.Start)), HarmonyPostfix]
        static private void ArcherEnemy_Start_Post(ArcherEnemy __instance)
        => __instance.hitTime = ORIGINAL_SHORT_STAGGER_DURATION * _staggerTimeMultiplier / 100f;

        [HarmonyPatch(typeof(ScrapRobotEnemy), nameof(ScrapRobotEnemy.Start)), HarmonyPostfix]
        static private void ScrapRobotEnemy_Start_Post(ScrapRobotEnemy __instance)
        => __instance.hitTime = ORIGINAL_SHORT_STAGGER_DURATION * _staggerTimeMultiplier / 100f;

        static private bool StaggerImmunityTest(EnemyController enemyController, Atributes attributes)
        {
            EnemyState enemyState = enemyController.attacking ? EnemyState.Attacking
                                  : enemyController.preparingAttack ? EnemyState.Preparing
                                  : EnemyState.Idle;
            AttackTypes attackTypes = _staggerImmunitiesTable[enemyState];
            return _staggerTimeMultiplier == 0
                || attackTypes != 0
                    && (attributes.isDog && attackTypes.HasFlag(AttackTypes.DogAndIris)
                    || attributes.myType == HitObjectType.SmallBullet && attackTypes.HasFlag(AttackTypes.Gun)
                    || attributes.myType == HitObjectType.PlayerMelee
                        && (attributes.meleeWeaponClass == MeleeWeaponClass.Sword && attackTypes.HasFlag(AttackTypes.Sword)
                        || attributes.meleeWeaponClass == MeleeWeaponClass.Axe && attackTypes.HasFlag(AttackTypes.Axe)
                        || attributes.meleeWeaponClass == MeleeWeaponClass.Shuriken && attackTypes.HasFlag(AttackTypes.Shuriken)));
        }

        [HarmonyPatch(typeof(EnemyController), nameof(EnemyController.GotHit)), HarmonyPrefix]
        static private bool EnemyController_GotHit_Pre(EnemyController __instance, ref Atributes hitObject)
        {
            bool staggerImmunity = StaggerImmunityTest(__instance, hitObject);
            if (staggerImmunity)
                __instance.GotHitSound(hitObject);
            return !staggerImmunity;
        }

        [HarmonyPatch(typeof(EnemyController), nameof(EnemyController.GotHitBounceBack)), HarmonyPrefix]
        static private bool EnemyController_GotHitBounceBack_Pre(EnemyController __instance, ref Atributes hitObject)
        => !StaggerImmunityTest(__instance, hitObject);


        // Enemy HP
        [HarmonyPatch(typeof(Atributes), nameof(Atributes.OnEnable)), HarmonyPrefix]
        static private bool Atributes_OnEnable_Pre(Atributes __instance)
        {
            if (__instance.myOwner == null && __instance.transform.parent != null)
                __instance.myOwner = __instance.transform.parent.GetComponent<SortingObject>();

            if (!__instance.appliedLifeDifficulty)
            {
                float multiplier = _hpMultiplier / 100f;
                if (__instance.TryGetComponent<EnemyHitBox>(out var enemyHitBox)
                && enemyHitBox.bossBar)
                    multiplier *= _bossHPMultiplier / 100f;

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