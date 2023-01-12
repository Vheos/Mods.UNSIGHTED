
using HarmonyLib;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vheos.Helpers.RNG;
using Vheos.Mods.Core;

namespace Vheos.Mods.UNSIGHTED;
public class Various : AMod
{
    // Section
    protected override string SectionOverride
    => Sections.VARIOUS;
    protected override string Description =>
        "Mods that haven't found their home yet!" +
        "\n\nExamples:" +
        "\n• Set your current bolts and meteor dusts" +
        "\n• Disable Iris's tutorials and combat help" +
        "\n• Break crates with guns" +
        "\n• Skip 30sec of intro logos" +
        "\n• Customize the \"Stamina Heal\" move";

    // Settings
    private static ModSetting<bool> _runInBackground;
    private static ModSetting<bool> _introLogos;
    private static ModSetting<bool> _irisTutorials;
    private static ModSetting<int> _bolts;
    private static ModSetting<int> _meteorDusts;
    private static ModSetting<GunCrateBreakMode> _breakCratesWithGuns;
    private static ModSetting<int> _breakCratesWithGunsChance;
    private static ModSetting<int> _staminaHealGain;
    private static ModSetting<int> _staminaHealDuration;
    private static ModSetting<bool> _staminaHealCancelling;
    private static ModSetting<bool> _pauseStaminaRecoveryOnGain;
    protected override void Initialize()
    {
        _runInBackground = CreateSetting(nameof(_runInBackground), false);
        _introLogos = CreateSetting(nameof(_introLogos), true);
        _irisTutorials = CreateSetting(nameof(_irisTutorials), true);

        _bolts = CreateSetting(nameof(_bolts), 0, IntRange(0, 100000));
        _meteorDusts = CreateSetting(nameof(_meteorDusts), 0, IntRange(0, 100));

        _breakCratesWithGuns = CreateSetting(nameof(_breakCratesWithGuns), GunCrateBreakMode.Disabled);
        _breakCratesWithGunsChance = CreateSetting(nameof(_breakCratesWithGunsChance), 100, IntRange(0, 100));

        _staminaHealGain = CreateSetting(nameof(_staminaHealGain), 100, IntRange(0, 100));
        _staminaHealDuration = CreateSetting(nameof(_staminaHealDuration), 100, IntRange(50, 200));
        _staminaHealCancelling = CreateSetting(nameof(_staminaHealCancelling), true);
        _pauseStaminaRecoveryOnGain = CreateSetting(nameof(_pauseStaminaRecoveryOnGain), true);

        // Events
        _runInBackground.AddEvent(() => Application.runInBackground = _runInBackground);
        AddEventOnConfigOpened(TryReadBoltsAndMeteorDusts);
        _bolts.AddEventSilently(TrySetBolts);
        _meteorDusts.AddEventSilently(TrySetMeteorDusts);
    }
    protected override void SetFormatting()
    {
        _runInBackground.IsAdvanced = true;
        _runInBackground.Format("Run in background");
        _runInBackground.Description =
            "Makes the game run even when it's not focused";
        _introLogos.Format("Intro logos");
        _introLogos.Description =
            "Allows you to disable all the unskippable logo animations, as well as the input choice screen, and go straight to the main menu" +
            "\nYou'll save about 30 seconds of your precious life each time you start the game";
        _irisTutorials.Format("Iris tutorials");
        _irisTutorials.Description =
            "Allows you to disable most Iris tutorials" +
            "\nEstimated time savings: your entire lifetime";

        CreateHeader("Override currency").Description =
            "Allows you to override your current amount of bolts and meteor dusts";
        using (Indent)
        {
            _bolts.DisplayResetButton = false;
            _bolts.Format("bolts");
            _meteorDusts.DisplayResetButton = false;
            _meteorDusts.Format("meteor dusts");
        }

        _breakCratesWithGuns.Format("Break crates with guns");
        _breakCratesWithGuns.Description =
            "Allows you to break crates even if you don't have any melee weapon equipped" +
            $"\n• {GunCrateBreakMode.Disabled} - original in-game behaviour" +
            $"\n• {GunCrateBreakMode.ChancePerBullet} - every bullet has x% chance to break the crate" +
            $"\n• {GunCrateBreakMode.ChancePerDamage} - every point of damage has x% chance to break the crate" +
            $"\n(requires area change to take effect)";
        using (Indent)
            _breakCratesWithGunsChance.Format("chance", _breakCratesWithGuns, GunCrateBreakMode.Disabled, false);

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
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(SettingsPreset.Vheos_CoopRebalance):
                ForceApply();
                _introLogos.Value = false;

                _breakCratesWithGuns.Value = GunCrateBreakMode.ChancePerBullet;
                _breakCratesWithGunsChance.Value = 50;

                _irisTutorials.Value = false;

                _staminaHealGain.Value = 50;
                _staminaHealDuration.Value = 100;
                _staminaHealCancelling.Value = false;
                _pauseStaminaRecoveryOnGain.Value = false;
                break;
        }
    }

    // Privates
    private const float ORIGINAL_STAMINA_CHARGE_DURATION = 0.66f;
    private const string METEOR_DUST_NAME = "MeteorDust";
    private static bool PlayerHaveMeleeWeapon_Original(PlayerInfo player)
    {
        foreach (string text in GlobalGameData.instance.currentData.playerDataSlots[GlobalGameData.instance.loadedSlot].playersEquipData[player.playerNum].weapons)
            if (text != "Null"
            && UnsightedHelpers.instance.GetWeaponObject(text).weaponClass == EquipmentClass.Melee
            && UnsightedHelpers.instance.GetWeaponObject(text).meleeWeaponClass != MeleeWeaponClass.Shuriken)
                return true;
        return false;
    }
    private static bool IsCommonDestructible(EnemyHitBox enemyHitBox)
    => enemyHitBox.ParentHasComponent<HoldableCrate>()
    || enemyHitBox.ParentHasComponent<DestructablePillar>();
    private static void TryReadBoltsAndMeteorDusts()
    {
        if (PlayersManager.instance.TryNonNull(out var playerManager))
            _bolts.SetSilently(playerManager.players[0].currentPlayerStats.playerMoney);

        if (UnsightedHelpers.instance.TryNonNull(out var helpers))
            _meteorDusts.SetSilently(helpers.PlayerHaveItem(METEOR_DUST_NAME));
    }
    private static void TrySetBolts()
    {
        if (!PlayersManager.instance.TryNonNull(out var playerManager))
            return;

        playerManager.players[0].currentPlayerStats.playerMoney = _bolts;
        BoltHUDController.instance?.UpdateBar();
    }
    private static void TrySetMeteorDusts()
    {
        if (!UnsightedHelpers.instance.TryNonNull(out var helpers))
            return;

        if (helpers.GetPlayerData().playerItems.TryFind(t => t.itemName == METEOR_DUST_NAME, out var meteorDust))
            meteorDust.quanty = _meteorDusts;
        else
            helpers.AddPlayerItem(METEOR_DUST_NAME, _meteorDusts);
    }

    // Defines
    private enum GunCrateBreakMode
    {
        Disabled,
        ChancePerBullet,
        ChancePerDamage,
    }

    // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

    // Skip intro
    [HarmonyPatch(typeof(SplashScreenScene), nameof(SplashScreenScene.Start)), HarmonyPostfix]
    private static IEnumerator SplashScreenScene_Start_Post(IEnumerator original, SplashScreenScene __instance)
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
    private static void TitleScreenScene_Start_Post(TitleScreenScene __instance)
    {
        if (_introLogos)
            return;

        PressAnyKeyScreen.inputPopupAlreadyAppeared = true;
        BetaTitleScreen.logoShown = true;
    }

    // Iris tutorials
    [HarmonyPatch(typeof(MonoBehaviour), nameof(MonoBehaviour.StartCoroutine), new[] { typeof(string) }), HarmonyPrefix]
    private static bool MonoBehaviour_StartCoroutine_Pre(MonoBehaviour __instance, string methodName)
    => _irisTutorials || methodName != "IrisTutorial";

    // Break crates with guns
    [HarmonyPatch(typeof(EnemyHitBox), nameof(EnemyHitBox.OnEnable)), HarmonyPostfix]
    private static void EnemyHitBox_OnEnable_Post(EnemyHitBox __instance)
    {
        if (_breakCratesWithGuns.Value == GunCrateBreakMode.Disabled
        || !IsCommonDestructible(__instance))
            return;

        __instance.amPlantCollider = 1;
        __instance.myAtributes.myType = HitObjectType.SmallEnemy;
        __instance.gameObject.layer = 14;
    }

    [HarmonyPatch(typeof(EnemyHitBox), nameof(EnemyHitBox.Damage)), HarmonyPrefix]
    private static bool EnemyHitBox_Damage_Pre(EnemyHitBox __instance, Atributes hitObject)
    {
        if (_breakCratesWithGuns.Value == GunCrateBreakMode.Disabled
        || !IsCommonDestructible(__instance))
            return true;

        AudioController.Play(GlobalGameManager.instance.gameSounds.defendedBulletSound, 0.5f, 1f);

        float percentChance = _breakCratesWithGunsChance;
        if (_breakCratesWithGuns == GunCrateBreakMode.ChancePerDamage)
            percentChance *= hitObject.damage;
        if (percentChance.RollPercent())
            return true;

        ObjectPoolManager.instance.ActivatePoolObject("HitParticleSmaller", 0.25f.ToVector3(), true)
            .transform.position = hitObject.transform.position;
        if (hitObject.gameObject.TryGetComponent(out CollisionTrigger collisionTrigger))
            collisionTrigger.collisionEnterEvent?.Invoke();
        return false;

    }

    [HarmonyPatch(typeof(DestructablePillar), nameof(DestructablePillar.GotHitBy), new[] { typeof(Atributes), typeof(PlayerInfo) }), HarmonyPrefix]
    private static void DestructablePillar_GotHitBy_Pre(DestructablePillar __instance, ref HitObjectType __state, Atributes hitObject)
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
    private static void DestructablePillar_GotHitBy_Post(DestructablePillar __instance, ref HitObjectType __state, Atributes hitObject)
    {
        if (_breakCratesWithGuns.Value == GunCrateBreakMode.Disabled
        || __state == 0)
            return;

        hitObject.myType = __state;
    }

    // Stamina
    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.FillStamina)), HarmonyPrefix]
    private static void BasicCharacterController_FillStamina_Pre(BasicCharacterController __instance, ref float __state, ref float value)
    {
        if (!_pauseStaminaRecoveryOnGain)
            __state = __instance.lastStaminaUsageTime;

        if (__instance.staminaChargeEffect)
            value = _staminaHealGain / 100f * __instance.myInfo.totalStamina;
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.FillStamina)), HarmonyPostfix]
    private static void BasicCharacterController_FillStamina_Post(BasicCharacterController __instance, ref float __state)
    {
        if (!_pauseStaminaRecoveryOnGain)
            __instance.lastStaminaUsageTime = __state;
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.StaminaChargeCoroutine)), HarmonyPostfix]
    private static IEnumerator BasicCharacterController_StaminaChargeCoroutine_Post(IEnumerator original, BasicCharacterController __instance)
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