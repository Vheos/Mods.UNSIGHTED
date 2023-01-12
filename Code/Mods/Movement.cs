
using HarmonyLib;
using UnityEngine;
using Vheos.Mods.Core;

namespace Vheos.Mods.UNSIGHTED;
public class Movement : AMod
{
    // Section
    protected override string SectionOverride
    => Sections.BALANCE;
    protected override string Description =>
        "Mods related to movement (speed and stamina)" +
        "\n\nExamples:" +
        "\n• Change move/run speed" +
        "\n• Change run/spin stamina drain" +
        "\n• Customize \"Runner Chip\"";

    // Settings
    private static ModSetting<int> _moveSpeedMultiplier;
    private static ModSetting<int> _runSpeedMultiplier;
    private static ModSetting<int> _runnerChipSpeedBonus;
    private static ModSetting<bool> _runnerChipStaminaRecovery;
    private static ModSetting<float> _runStaminaDrain;
    private static ModSetting<float> _spinStaminaDrain;
    private static ModSetting<bool> _spinnerStaminaGains;
    protected override void Initialize()
    {
        _moveSpeedMultiplier = CreateSetting(nameof(_moveSpeedMultiplier), 100, IntRange(50, 200));
        _runSpeedMultiplier = CreateSetting(nameof(_runSpeedMultiplier), 150, IntRange(100, 300));
        _runnerChipSpeedBonus = CreateSetting(nameof(_runnerChipSpeedBonus), 0, IntRange(0, 50));
        _runnerChipStaminaRecovery = CreateSetting(nameof(_runnerChipStaminaRecovery), true);
        _runStaminaDrain = CreateSetting(nameof(_runStaminaDrain), ORIGINAL_RUN_STAMINA_DRAIN, FloatRange(0f, 5f));
        _spinStaminaDrain = CreateSetting(nameof(_spinStaminaDrain), ORIGINAL_SPINNER_STAMINA_DRAIN, FloatRange(0f, 5f));
        _spinnerStaminaGains = CreateSetting(nameof(_spinnerStaminaGains), true);
    }
    protected override void SetFormatting()
    {
        _moveSpeedMultiplier.Format("Move speed");
        _moveSpeedMultiplier.Description =
            "How fast you move by default (not running, spinning, etc.)" +
            "\n\nUnit: percent of original move speed";
        _runSpeedMultiplier.Format("Run speed");
        _runSpeedMultiplier.Description =
            "How fast you run" +
            $"\n\nUnit: percent of \"{_moveSpeedMultiplier.Name}\"";
        _runnerChipSpeedBonus.Format("\"Runner Chip\" speed bonus");
        _runnerChipSpeedBonus.Description =
            "How much faster you run with the \"Runner Chip\" equipped" +
            $"\n\nUnit: percent of \"{_runSpeedMultiplier.Name}\", stacks multiplicatively";
        using (Indent)
        {
            _runnerChipStaminaRecovery.Format("allow stamina recovery");
            _runnerChipStaminaRecovery.Description =
                "Allows stamina to recover naturally when running with the \"Runner Chip\" equipped";
        }

        _runStaminaDrain.Format("Run stamina drain");
        _runStaminaDrain.Description =
            "How much stamina you lose when running" +
            "\n\nUnit: stamina points per second";
        _spinStaminaDrain.Format("Spin stamina drain");
        _spinStaminaDrain.Description =
            "How much stamina you lose when spinning" +
            "\n\nUnit: stamina points per second";
        _spinnerStaminaGains.Format("Spinner stamina gains");
        _spinnerStaminaGains.Description =
            "Makes you regain some stamina when you stop spinning or skip on water";
    }
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(SettingsPreset.Vheos_CoopRebalance):
                ForceApply();
                _moveSpeedMultiplier.Value = 100;
                _runSpeedMultiplier.Value = 167;
                _runnerChipSpeedBonus.Value = 10;
                _runnerChipStaminaRecovery.Value = false;
                _runStaminaDrain.Value = 2f;
                _spinStaminaDrain.Value = 3f;
                _spinnerStaminaGains.Value = false;
                break;
        }
    }

    // Privates
    private const float ORIGINAL_MOVE_SPEED = 6f;
    private const float ORIGINAL_RUN_STAMINA_DRAIN = 1.5f;
    private const float ORIGINAL_SPINNER_STAMINA_DRAIN = 1f;

    // Hooks

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.SetMovement)), HarmonyPrefix]
    private static void BasicCharacterController_SetMovement_Pre(BasicCharacterController __instance)
    {
        // Quit
        if (__instance.hookshotMovement
        || __instance.gotHit
        || __instance.bouncingBack
        || __instance.dashing
        || __instance.companionTime
        || __instance.meeleAttacking
        || __instance.landing
        || __instance.liftingObject
        || __instance.throwingObject
        || __instance.guarding
        || __instance.beginGuarding
        || __instance.fallingIntoHole
        || __instance.fishing
        || __instance.usingShovel
        || __instance.climbing)
            return;

        bool runnerChipEquipped = UnsightedHelpers.instance.NumberOfChipsEquipped("RunnerChip", __instance.myInfo.playerNum) > 0;

        // Stamina drain
        float extraDrain = 0f;

        if (_runStaminaDrain != ORIGINAL_RUN_STAMINA_DRAIN
        && !runnerChipEquipped
        && __instance.running
        && __instance.myPhysics.height == 0f)
            extraDrain = _runStaminaDrain - ORIGINAL_RUN_STAMINA_DRAIN;

        if (_spinStaminaDrain != ORIGINAL_SPINNER_STAMINA_DRAIN
        && __instance.ridingSpinner)
            extraDrain = _spinStaminaDrain - ORIGINAL_SPINNER_STAMINA_DRAIN;

        if (extraDrain > 0)
            __instance.DrainStamina(gameTime.deltaTime * +extraDrain);
        else if (extraDrain < 0)
            __instance.FillStamina(gameTime.deltaTime * -extraDrain);

        // Speed
        float speed = ORIGINAL_MOVE_SPEED;
        if (!__instance.ridingSpinner)
        {
            speed *= _moveSpeedMultiplier / 100f;
            if (__instance.running
            && __instance.myPhysics.height == 0f)
            {
                speed *= _runSpeedMultiplier / 100f / 1.5f;
                if (runnerChipEquipped)
                {
                    speed *= 1 + _runnerChipSpeedBonus / 100f;
                    if (!_runnerChipStaminaRecovery)
                    {
                        __instance.lastStaminaUsageTime = Time.time;
                        __instance.lastStaminaRechargeTime = Time.time;
                    }
                }
            }
        }

        __instance.originalSpeed = __instance.speed = speed;
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.FillStamina)), HarmonyPrefix]
    private static void BasicCharacterController_FillStamina_Pre(BasicCharacterController __instance, ref float value)
    {
        if (!_spinnerStaminaGains
        && __instance.ridingSpinner
        && !__instance.spinnerGrinding)
            value = 0f;
    }
}