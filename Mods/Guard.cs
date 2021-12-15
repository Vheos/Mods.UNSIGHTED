namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections;
    using UnityEngine;
    using HarmonyLib;
    using Tools.ModdingCore;

    public class Guard : AMod
    {
        // Settings
        static private ModSetting<int> _parryDuration;
        static private ModSetting<int> _deflectDuration;
        static private ModSetting<int> _optionalDeflectDuration;
        static private ModSetting<int> _guardStaminaCost;
        static private ModSetting<bool> _guardStaminaCostIsPercent;
        static private ModSetting<int> _parryStaminaGain;
        static private ModSetting<bool> _parryStaminaGainIsPercent;
        static private ModSetting<bool> _guardWithoutMeleeWeapon;
        static private ModSetting<bool> _parryWithoutMeleeWeapon;
        override protected void Initialize()
        {
            _parryDuration = CreateSetting(nameof(_parryDuration), 175, IntRange(0, 1000));
            _deflectDuration = CreateSetting(nameof(_deflectDuration), 325, IntRange(0, 1000));
            _optionalDeflectDuration = CreateSetting(nameof(_optionalDeflectDuration), 50, IntRange(0, 1000));
            _guardStaminaCost = CreateSetting(nameof(_guardStaminaCost), ORIGINAL_GUARD_STAMINA_COST, IntRange(0, 100));
            _guardStaminaCostIsPercent = CreateSetting(nameof(_guardStaminaCostIsPercent), false);
            _parryStaminaGain = CreateSetting(nameof(_parryStaminaGain), 100, IntRange(0, 100));
            _parryStaminaGainIsPercent = CreateSetting(nameof(_parryStaminaGainIsPercent), true);
            _guardWithoutMeleeWeapon = CreateSetting(nameof(_guardWithoutMeleeWeapon), false);
            _parryWithoutMeleeWeapon = CreateSetting(nameof(_parryWithoutMeleeWeapon), true);
        }
        override protected void SetFormatting()
        {
            _parryDuration.Format("Parry duration");
            _parryDuration.Description =
                "How quickly you need to react to perform a parry" +
                "\nHigher values will make parrying easier, but root you in place for longer" +
                "\n\nUnit: milliseconds";
            _deflectDuration.Format("Deflect duration");
            _deflectDuration.Description =
                "How long (after parry window ends) you can deflect bullets and attacks" +
                "\nLower values will make guarding more responsive" +
                "\n\nUnit: milliseconds";
            _optionalDeflectDuration.Format("Max guard duration");
            _optionalDeflectDuration.Description =
                "How much you can extend the deflect window if you keep the button pressed" +
                "\n\nUnit: milliseconds";
            _guardWithoutMeleeWeapon.Format("Guard without melee weapon");
            _guardWithoutMeleeWeapon.Description =
                "Allows you to guard even if you don't have any melee weapon equipped";
            using (Indent)
            {
                _parryWithoutMeleeWeapon.Format("can parry", _guardWithoutMeleeWeapon);
                _parryWithoutMeleeWeapon.Description =
                     "Allows you to not only deflect, but also parry without a melee weapon";
            }
            _guardStaminaCost.Format("Guard stamina cost");
            _guardStaminaCost.Description =
                "How much stamina you lose when you start guarding" +
                "\n\nUnit: stamina points or percent of max stamina";
            using (Indent)
                _guardStaminaCostIsPercent.Format("percent of max stamina", _guardStaminaCost, t => t > 0);

            _parryStaminaGain.Format("Guard stamina cost");
            _parryStaminaGain.Description =
                "How much stamina you regain after a successfully parry" +
                "Set to 0 to make parrying a little less spammable" +
                "\n\nUnit: stamina points or percent of max stamina";
            using (Indent)
                _parryStaminaGainIsPercent.Format("percent of max stamina", _parryStaminaGain, t => t > 0);
        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(Preset.Coop_NewGameExtra_HardMode):
                    ForceApply();
                    _parryDuration.Value = 133;
                    _deflectDuration.Value = 267;
                    _optionalDeflectDuration.Value = 600;
                    _guardWithoutMeleeWeapon.Value = true;
                    _parryWithoutMeleeWeapon.Value = false;
                    _guardStaminaCost.Value = 6;
                    _guardStaminaCostIsPercent.Value = false;
                    _parryStaminaGain.Value = 0;
                    _parryStaminaGainIsPercent.Value = false;
                    break;
            }
        }
        override protected string Description =>
            "Mods related to the guarding mechanic" +
            "\n\nExamples:" +
            "\n• Change \"perfect\" and \"normal\" parry windows" +
            "\n• Guard longer by holding the button" +
            "\n• Guard without melee weapons" +
            "\n\nDictionary:" +
            "\n• Parry - \"perfect parry\", triggers when you guard early enough" +
            "\n• Deflect - \"normal parry\", triggers when you guard too late for parry" +
            "\n• Optional deflect - cancellable part of the guard action" +
            "\n• Guard - the whole action, consisting of the 3 parts above";

        // Privates
        static private bool PlayerHaveMeleeWeapon_Original(PlayerInfo player)
        {
            foreach (string text in PseudoSingleton<GlobalGameData>.instance.currentData.playerDataSlots[PseudoSingleton<GlobalGameData>.instance.loadedSlot].playersEquipData[player.playerNum].weapons)
                if (text != "Null"
                && PseudoSingleton<Helpers>.instance.GetWeaponObject(text).weaponClass == EquipmentClass.Melee
                && PseudoSingleton<Helpers>.instance.GetWeaponObject(text).meleeWeaponClass != MeleeWeaponClass.Shuriken)
                    return true;
            return false;
        }
        private const int ORIGINAL_GUARD_STAMINA_COST = 6;

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Parry
        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.GuardCoroutine)), HarmonyPostfix]
        static private IEnumerator BasicCharacterController_GuardCoroutine_Post(IEnumerator original, BasicCharacterController __instance)
        {
            // Stamina cost
            float extraCost = 0f;

            if (_guardStaminaCost != ORIGINAL_GUARD_STAMINA_COST)
            {
                extraCost = _guardStaminaCost - ORIGINAL_GUARD_STAMINA_COST;
                if (_guardStaminaCostIsPercent)
                    extraCost *= __instance.myInfo.totalStamina / 100f;
            }           

            if (extraCost > 0)
                __instance.DrainStamina(+extraCost);
            else if (extraCost < 0)
                __instance.FillStamina(-extraCost);

            // Coroutine
            bool hasMeleeWeapon = PlayerHaveMeleeWeapon_Original(__instance.myInfo);

            original.MoveNext();
            __instance.parryActive = hasMeleeWeapon || _parryWithoutMeleeWeapon;
            if (_parryDuration > 0)
                yield return gameTime.WaitForSeconds(_parryDuration / 1000f);

            if (!__instance.holdingObject)
            {
                original.MoveNext();
                if (_deflectDuration > 0)
                    yield return gameTime.WaitForSeconds(_deflectDuration / 1000f);

                original.MoveNext();
                if (_optionalDeflectDuration > 0
                && ButtonSystem.GetKey(PseudoSingleton<GlobalInputManager>.instance.inputData.GetInputProfile(__instance.myInfo.playerNum).guard))
                    yield return gameTime.WaitForSeconds(_optionalDeflectDuration / 1000f);
            }

            while (original.MoveNext())
                yield return original.Current;
        }

        [HarmonyPatch(typeof(PlayerInfo), nameof(PlayerInfo.PlayerHaveMeleeWeapon)), HarmonyPrefix]
        static private bool PlayerInfo_PlayerHaveMeleeWeapon_Pre(PlayerInfo __instance, ref bool __result)
        {
            if (!_guardWithoutMeleeWeapon)
                return true;

            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.FillStamina)), HarmonyPrefix]
        static private void BasicCharacterController_FillStamina_Pre(BasicCharacterController __instance, ref float value)
        {
            if (!__instance.parryActive)
                return;

            value = _parryStaminaGain;
            if (_parryStaminaGainIsPercent)
                value *= __instance.myInfo.totalStamina / 100f;
        }
    }
}