namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using XInputDotNetPure;
    using HarmonyLib;
    using Tools.ModdingCore;
    using Tools.Extensions.UnityObjects;
    using Tools.Extensions.General;
    using Tools.Extensions.Math;

    public class Various : AMod
    {
        // Section
        override protected string SectionOverride
        => Sections.VARIOUS;
        override protected string Description =>
            "Mods that haven't found their home yet!" +
            "\n\nExamples:" +
            "\n• Skip 30sec of intro logos" +
            "\n• Customize the \"Stamina Heal\" move";

        // Settings
        static private ModSetting<bool> _runInBackground;
        static private ModSetting<bool> _introLogos;
        static private ModSetting<bool> _gamepadVibrations;
        static private ModSetting<GunCrateBreakMode> _breakCratesWithGuns;
        static private ModSetting<int> _breakCratesWithGunsChance;
        static private ModSetting<bool> _irisTutorials;
        static private ModSetting<IrisCombatHelp> _irisCombatHelp;
        static private ModSetting<int> _staminaHealGain;
        static private ModSetting<int> _staminaHealDuration;
        static private ModSetting<bool> _staminaHealCancelling;
        static private ModSetting<bool> _pauseStaminaRecoveryOnGain;
        override protected void Initialize()
        {
            _runInBackground = CreateSetting(nameof(_runInBackground), false);
            _introLogos = CreateSetting(nameof(_introLogos), true);
            _gamepadVibrations = CreateSetting(nameof(_gamepadVibrations), true);

            _breakCratesWithGuns = CreateSetting(nameof(_breakCratesWithGuns), GunCrateBreakMode.Disabled);
            _breakCratesWithGunsChance = CreateSetting(nameof(_breakCratesWithGunsChance), 0, IntRange(0, 100));

            _irisTutorials = CreateSetting(nameof(_irisTutorials), true);
            _irisCombatHelp = CreateSetting(nameof(_irisCombatHelp), IrisCombatHelp.AtMaxAffinity);

            _staminaHealGain = CreateSetting(nameof(_staminaHealGain), 100, IntRange(0, 100));
            _staminaHealDuration = CreateSetting(nameof(_staminaHealDuration), 100, IntRange(50, 200));
            _staminaHealCancelling = CreateSetting(nameof(_staminaHealCancelling), true);
            _pauseStaminaRecoveryOnGain = CreateSetting(nameof(_pauseStaminaRecoveryOnGain), true);

            // Events
            _runInBackground.AddEvent(() => Application.runInBackground = _runInBackground);
        }
        override protected void SetFormatting()
        {
            _runInBackground.IsAdvanced = true;
            _runInBackground.Format("Run in background");
            _runInBackground.Description =
                "Makes the game run even when it's not focused";
            _introLogos.Format("Intro logos");
            _introLogos.Description =
                "Allows you to disable all the unskippable logo animations, as well as the input choice screen, and go straight to the main menu" +
                "\nYou'll save about 30 seconds of your precious life each time you start the game";
            _gamepadVibrations.Format("Gamepad vibrations");
            _gamepadVibrations.Description =
                "Makes your gamepad vibrate when doing almost anything in the game" +
                "\nDisable if you care for battery life, or your wrists, or both";

            _breakCratesWithGuns.Format("Break crates with guns");
            _breakCratesWithGuns.Description =
                "Allows you to break crates even if you don't have any melee weapon equipped" +
                $"\n• {GunCrateBreakMode.Disabled} - original in-game behaviour" +
                $"\n• {GunCrateBreakMode.ChancePerBullet} - every bullet has x% chance to break the crate" +
                $"\n• {GunCrateBreakMode.ChancePerDamage} - every 1 damage has x% chance to break the crate";
            using (Indent)
            {
                _breakCratesWithGunsChance.Format("chance", _breakCratesWithGuns, GunCrateBreakMode.Disabled, false);
            }

            _irisTutorials.Format("Iris tutorials");
            _irisTutorials.Description =
                "Allows you to disable most Iris tutorials" +
                "\nEstimated time savings: your entire lifetime";
            _irisCombatHelp.Format("Iris combat help");
            _irisCombatHelp.Description =
                "When should Iris start \"helping\" you in combat?";

            _staminaHealGain.Format("\"Stamina Heal\" gain");
            _staminaHealGain.Description =
                "How much stamina you recover when you perform the \"Stamina Heal\" action" +
                "\n(double tap the \"Sprint\" button while standing still)" +
                "\n\nUnit: percent of max stamina";
            _staminaHealDuration.Format("\"Stamina Heal\" duration");
            _staminaHealDuration.Description =
                "How long is the \"Stamina Heal\" animation" +
                "\n\nUnit: percent of original duration";
            _staminaHealCancelling.Format("\"Stamina Heal\" cancelling");
            _staminaHealCancelling.Description =
                "Allows you to cancel the \"Stamina Heal\" animation after merely 40% of its duration" +
                "\nDisable this to make \"Stamina Heal\" more punishable in combat";
            _pauseStaminaRecoveryOnGain.Format("Pause stamina recovery on gain");
            _pauseStaminaRecoveryOnGain.Description =
                "Pauses your natural stamina recovery whenever you gain stamina by other means";
        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(SettingsPreset.Vheos_CoopRebalance):
                    ForceApply();
                    _introLogos.Value = false;
                    _gamepadVibrations.Value = false;

                    _irisTutorials.Value = false;
                    _irisCombatHelp.Value = IrisCombatHelp.AtMaxAffinity;

                    _staminaHealGain.Value = 50;
                    _staminaHealDuration.Value = 100;
                    _staminaHealCancelling.Value = false;
                    _pauseStaminaRecoveryOnGain.Value = false;
                    break;
            }
        }

        // Privates
        private const float ORIGINAL_STAMINA_CHARGE_DURATION = 0.66f;
        static private bool PlayerHaveMeleeWeapon_Original(PlayerInfo player)
        {
            foreach (string text in PseudoSingleton<GlobalGameData>.instance.currentData.playerDataSlots[PseudoSingleton<GlobalGameData>.instance.loadedSlot].playersEquipData[player.playerNum].weapons)
                if (text != "Null"
                && PseudoSingleton<Helpers>.instance.GetWeaponObject(text).weaponClass == EquipmentClass.Melee
                && PseudoSingleton<Helpers>.instance.GetWeaponObject(text).meleeWeaponClass != MeleeWeaponClass.Shuriken)
                    return true;
            return false;
        }
        static private bool IsCommonDestructible(EnemyHitBox enemyHitBox)
        => enemyHitBox.ParentHasComponent<HoldableCrate>()
        || enemyHitBox.ParentHasComponent<DestructablePillar>();

        // Defines
        private enum GunCrateBreakMode
        {
            Disabled,
            ChancePerBullet,
            ChancePerDamage,
        }
        private enum IrisCombatHelp
        {
            AtMaxAffinity,
            Always,
            Never,
        }

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Skip intro
        [HarmonyPatch(typeof(SplashScreenScene), nameof(SplashScreenScene.Start)), HarmonyPostfix]
        static private IEnumerator SplashScreenScene_Start_Post(IEnumerator original, SplashScreenScene __instance)
        {
            if (_introLogos)
            {
                while (original.MoveNext())
                    yield return original.Current;
                yield break;
            }

#if GAMEPASS
            __instance.xboxSignedInAndLoaded = false;
            __instance.StartCoroutine("XboxSignIn");
            while (!__instance.xboxSignedInAndLoaded)
                yield return __instance.WaitForSeconds(0.1);
#endif

            Time.timeScale = 1f;
            __instance.CheckBestResolution();
            __instance.sceneLoadingObject = SceneManager.LoadSceneAsync("TitleScreen");
            __instance.sceneLoadingObject.allowSceneActivation = true;
        }

        [HarmonyPatch(typeof(TitleScreenScene), nameof(TitleScreenScene.Start)), HarmonyPostfix]
        static private void TitleScreenScene_Start_Post(TitleScreenScene __instance)
        {
            if (_introLogos)
                return;

            PressAnyKeyScreen.inputPopupAlreadyAppeared = true;
            BetaTitleScreen.logoShown = true;
        }

        // Break crates with guns
        [HarmonyPatch(typeof(EnemyHitBox), nameof(EnemyHitBox.OnEnable)), HarmonyPostfix]
        static private void EnemyHitBox_OnEnable_Post(EnemyHitBox __instance)
        {
            if (_breakCratesWithGuns.Value == GunCrateBreakMode.Disabled
            || !IsCommonDestructible(__instance))
                return;

            __instance.amPlantCollider = 1;
            __instance.myAtributes.myType = HitObjectType.SmallEnemy;
            __instance.gameObject.layer = 14;
        }

        [HarmonyPatch(typeof(EnemyHitBox), nameof(EnemyHitBox.Damage)), HarmonyPrefix]
        static private bool EnemyHitBox_Damage_Pre(EnemyHitBox __instance, Atributes hitObject)
        {
            if (_breakCratesWithGuns.Value == GunCrateBreakMode.Disabled
            || !IsCommonDestructible(__instance))
                return true;

            ObjectPoolManager.instance.ActivatePoolObject("HitParticleSmaller", 0.25f.ToVector3(), true)
              .transform.position = hitObject.transform.position;
            AudioController.Play(PseudoSingleton<GlobalGameManager>.instance.gameSounds.defendedBulletSound, 0.5f, 1f);
            if (hitObject.gameObject.TryGetComponent(out CollisionTrigger collisionTrigger))
                collisionTrigger.collisionEnterEvent?.Invoke();

            float percentChance = _breakCratesWithGunsChance;
            if (_breakCratesWithGuns == GunCrateBreakMode.ChancePerDamage)
                percentChance *= hitObject.damage;

            return percentChance.RollPercent();
        }

        [HarmonyPatch(typeof(DestructablePillar), nameof(DestructablePillar.GotHitBy), new[] { typeof(Atributes), typeof(PlayerInfo) }), HarmonyPrefix]
        static private void DestructablePillar_GotHitBy_Pre(DestructablePillar __instance, ref HitObjectType __state, Atributes hitObject)
        {
            if (_breakCratesWithGuns.Value == GunCrateBreakMode.Disabled)
            {
                __state = 0;
                return;
            }

            __state = hitObject.myType;
            hitObject.myType = HitObjectType.PlayerMelee;
        }

        [HarmonyPatch(typeof(DestructablePillar), nameof(DestructablePillar.GotHitBy), new[] { typeof(Atributes), typeof(PlayerInfo) }), HarmonyPostfix]
        static private void DestructablePillar_GotHitBy_Post(DestructablePillar __instance, ref HitObjectType __state, Atributes hitObject)
        {
            if (_breakCratesWithGuns.Value == GunCrateBreakMode.Disabled
            || __state == 0)
                return;

            hitObject.myType = __state;
        }

        // Vibrations
        [HarmonyPatch(typeof(GamePad), nameof(GamePad.SetVibration)), HarmonyPrefix]
        static private bool GamePad_SetVibration_Pre(GlobalInputManager __instance)
            => _gamepadVibrations;

        // Iris tutorials
        [HarmonyPatch(typeof(MonoBehaviour), nameof(MonoBehaviour.StartCoroutine), new[] { typeof(string) }), HarmonyPrefix]
        static private bool MonoBehaviour_StartCoroutine_Pre(MonoBehaviour __instance, string methodName)
        => _irisTutorials || methodName != "IrisTutorial";

        // Iris combat help
        [HarmonyPatch(typeof(IrisBotController), nameof(IrisBotController.FindBestTarget)), HarmonyPrefix]
        static private void IrisBotController_FindBestTarget_Pre(IrisBotController __instance, ref int __state)
        {
            if (_irisCombatHelp == IrisCombatHelp.AtMaxAffinity)
                return;

            // Cache and set temporary value
            NPCData irisData = PseudoSingleton<Helpers>.instance.GetNPCData("IrisNPC");
            __state = irisData.affinity;
            irisData.affinity = _irisCombatHelp == IrisCombatHelp.Always ? 4 : 0;
        }

        [HarmonyPatch(typeof(IrisBotController), nameof(IrisBotController.FindBestTarget)), HarmonyPostfix]
        static private void IrisBotController_FindBestTarget_Post(IrisBotController __instance, ref int __state)
        {
            if (_irisCombatHelp == IrisCombatHelp.AtMaxAffinity)
                return;

            // Restore original value
            PseudoSingleton<Helpers>.instance.GetNPCData("IrisNPC").affinity = __state;
        }

        // Stamina
        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.FillStamina)), HarmonyPrefix]
        static private void BasicCharacterController_FillStamina_Pre(BasicCharacterController __instance, ref float __state, ref float value)
        {
            if (!_pauseStaminaRecoveryOnGain)
                __state = __instance.lastStaminaUsageTime;

            if (__instance.staminaChargeEffect)
                value = _staminaHealGain / 100f * __instance.myInfo.totalStamina;
        }

        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.FillStamina)), HarmonyPostfix]
        static private void BasicCharacterController_FillStamina_Post(BasicCharacterController __instance, ref float __state)
        {
            if (!_pauseStaminaRecoveryOnGain)
                __instance.lastStaminaUsageTime = __state;
        }

        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.StaminaChargeCoroutine)), HarmonyPostfix]
        static private IEnumerator BasicCharacterController_StaminaChargeCoroutine_Post(IEnumerator original, BasicCharacterController __instance)
        {
            float frameDuration = _staminaHealDuration / 100f * ORIGINAL_STAMINA_CHARGE_DURATION / 10f;

            original.MoveNext();
            __instance.myAnimations.PlayAnimation("StaminaCharge", 1, 0, _staminaHealDuration / 100f, false, true);
            if (frameDuration > 0)
                yield return gameTime.WaitForSeconds(frameDuration * 2);

            original.MoveNext();
            __instance.staminaChargeTime = 0f;
            if (frameDuration > 0)
                yield return gameTime.WaitForSeconds(frameDuration * 2);

            original.MoveNext();
            __instance.staminaChargeEffect = !_staminaHealCancelling;
            if (frameDuration > 0)
                yield return gameTime.WaitForSeconds(frameDuration * 6);
            __instance.staminaChargeEffect = false;

            while (original.MoveNext())
                yield return original.Current;
        }
    }
}