namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections;
    using HarmonyLib;
    using UnityEngine;
    using Tools.ModdingCore;
    using Tools.Extensions.Math;
    using Vheos.Tools.Extensions.General;
    using Vheos.Tools.Extensions.UnityObjects;

    public class Combo : AMod
    {
        // Settings
        static private ModSetting<float> _decayDelay;
        static private ModSetting<float> _decayRate;
        static private ModSetting<bool> _decayRateIsPercentOfMaz;

        static private ModSetting<float> _gainPerGunHit;
        static private ModSetting<float> _gainPerSwordHit;
        static private ModSetting<float> _gainPerAxeHit;
        static private ModSetting<float> _gainPerShurikenHit;
        static private ModSetting<float> _gainPerParry;
        static private ModSetting<float> _gainPerHitTaken;
        static private ModSetting<float> _syringeGainRate;

        static private ModSetting<bool> _comboBarFormatAsPercentIncrease;
        static private ModSetting<bool> _comboBarHideProgressBar;

        override protected void Initialize()
        {
            _decayDelay = CreateSetting(nameof(_decayDelay), 5f, FloatRange(0f, 10f));
            _decayRate = CreateSetting(nameof(_decayRate), 0.125f, FloatRange(0f, 1f));
            _decayRateIsPercentOfMaz = CreateSetting(nameof(_decayRateIsPercentOfMaz), false);

            _gainPerSwordHit = CreateSetting(nameof(_gainPerSwordHit), 0.04f, FloatRange(0f, 1f));
            _gainPerAxeHit = CreateSetting(nameof(_gainPerAxeHit), 0.1f, FloatRange(0f, 1f));
            _gainPerShurikenHit = CreateSetting(nameof(_gainPerShurikenHit), 0.04f, FloatRange(0f, 1f));
            _gainPerGunHit = CreateSetting(nameof(_gainPerGunHit), 0.015f, FloatRange(0f, 1f));
            _gainPerParry = CreateSetting(nameof(_gainPerParry), 0.05f, FloatRange(0f, 1f));
            _gainPerHitTaken = CreateSetting(nameof(_gainPerHitTaken), -0.6f, FloatRange(0f, 1f));
            _syringeGainRate = CreateSetting(nameof(_syringeGainRate), 0.25f, FloatRange(0f, 1f));

            _comboBarFormatAsPercentIncrease = CreateSetting(nameof(_comboBarFormatAsPercentIncrease), false);
            _comboBarHideProgressBar = CreateSetting(nameof(_comboBarHideProgressBar), false);
        }
        override protected void SetFormatting()
        {
            _decayDelay.Format("Decay delay");
            _decayRate.Format("Decay rate");
             Indent++;
            {                
                _decayRateIsPercentOfMaz.Format("percent of max value");
                Indent--;
            }

            _gainPerSwordHit.Format("per sword hit");
            _gainPerAxeHit.Format("per axe hit");
            _gainPerShurikenHit.Format("per shuriken hit");
            _gainPerGunHit.Format("per gun hit");
            _gainPerParry.Format("per parry");
            _gainPerHitTaken.Format("per hit taken");
            _syringeGainRate.Format("combo to syringe rate");

            _comboBarFormatAsPercentIncrease.Format("Format as percent increase");
            _comboBarHideProgressBar.Format("Hide progress bar");
        }

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Combo multiplier
        [HarmonyPatch(typeof(ComboBar), nameof(ComboBar.AddComboValue), new[] { typeof(float) }), HarmonyPrefix]
        static private bool ComboBar_AddComboValue_Pre(ComboBar __instance, ref float ammountToAddInCombo)
        {
            if (UnityEngine.Time.time == PseudoSingleton<PlayersManager>.instance.players[__instance.playerNum].myCharacter.myCharacterCollider.lastTimePerfectParry)
                ammountToAddInCombo = _gainPerParry;
            else if (ammountToAddInCombo < 0f)
                ammountToAddInCombo = _gainPerHitTaken;

            PseudoSingleton<LifeBarsManager>.instance.syringeList[__instance.playerNum].AddSyringeValue(ammountToAddInCombo * _syringeGainRate, false, true);
            ammountToAddInCombo *= 1f + 0.5f * PseudoSingleton<Helpers>.instance.NumberOfChipsEquipped("ComboChipA", __instance.playerNum);
            __instance.CheckMaxComboValue();
            __instance.comboValue += ammountToAddInCombo;
            __instance.comboValue.SetClamp(1, __instance.maxComboValue);

            if (__instance.comboValue > 1f)
            {
                __instance.gameObject.SetActive(true);
                __instance.StopCoroutine("DecreaseComboBar");
                __instance.StartCoroutine("DecreaseComboBar");
                __instance.ShakeComboBar();
            }
            else
                __instance.gameObject.SetActive(false);

            return false;
        }

        [HarmonyPatch(typeof(ComboBar), nameof(ComboBar.AddComboValue), new[] { typeof(MeleeWeaponClass), typeof(bool), typeof(bool), typeof(int) }), HarmonyPrefix]
        static private bool ComboBar_AddComboValue2_Pre(ComboBar __instance, ref int __result, MeleeWeaponClass meleeWeaponClass, bool projectileDamage, bool enemyWasStunned, int damage)
        {
            float comboIncrease = 0f;
            if (projectileDamage)
                comboIncrease = _gainPerGunHit;
            else if (meleeWeaponClass == MeleeWeaponClass.Sword)
                comboIncrease = _gainPerSwordHit;
            else if (meleeWeaponClass == MeleeWeaponClass.Axe)
                comboIncrease = _gainPerAxeHit;
            else if (meleeWeaponClass == MeleeWeaponClass.Shuriken)
                comboIncrease = _gainPerShurikenHit;

            PseudoSingleton<LifeBarsManager>.instance.syringeList[__instance.playerNum].AddSyringeValue(comboIncrease * _syringeGainRate, false, true);
            comboIncrease *= 1f + 0.5f * PseudoSingleton<Helpers>.instance.NumberOfChipsEquipped("ComboChipA", __instance.playerNum);

            __result = damage.Mul(__instance.comboValue).Round();
            __instance.CheckMaxComboValue();
            __instance.comboValue += comboIncrease;
            __instance.comboValue.SetClamp(1, __instance.maxComboValue);

            if (__instance.comboValue > 1f)
            {
                __instance.gameObject.SetActive(true);
                __instance.StopCoroutine("DecreaseComboBar");
                __instance.StartCoroutine("DecreaseComboBar");
                __instance.ShakeComboBar();
            }
            else
                __instance.gameObject.SetActive(false);

            return false;
        }

        [HarmonyPatch(typeof(ComboBar), nameof(ComboBar.DecreaseComboBar)), HarmonyPostfix]
        static private IEnumerator ComboBar_DecreaseComboBar_Post(IEnumerator original, ComboBar __instance)
        {
            __instance.UpdateComboBar();
            yield return gameTime.WaitForSeconds(_decayDelay);
            float decayBase = _decayRateIsPercentOfMaz ? __instance.comboValue - 1f : 1f;
            while (__instance.comboValue >= 1f)
            {
                __instance.comboValue -= UnityEngine.Time.deltaTime * _decayRate * decayBase;
                __instance.UpdateComboBar();
                yield return null;
            }

            __instance.comboValue = 1f;
        }

        [HarmonyPatch(typeof(ComboBar), nameof(ComboBar.SetComboText)), HarmonyPrefix]
        static private bool ComboBar_SetComboText_Pre(ComboBar __instance)
        {
            if (!_comboBarFormatAsPercentIncrease)
                return true;

            float percentIncrease = __instance.comboValue.Sub(1).Mul(100).Round().ClampMin(1);

            __instance.valueText.text = $"+{percentIncrease:F0}%";
            __instance.valueText.keepSizeLarge = true;
            __instance.valueText.ApplyText(false, true, "", true);
            return false;
        }

        [HarmonyPatch(typeof(ComboBar), nameof(ComboBar.OnEnable)), HarmonyPrefix]
        static private bool ComboBar_OnEnable_Post(ComboBar __instance)
        {
            // progress bar
            __instance.FindChild("EmptyBar").SetActive(!_comboBarHideProgressBar);
            __instance.barFill.gameObject.SetActive(!_comboBarHideProgressBar);

            // format as percent increase
            float comboNameScale = _comboBarFormatAsPercentIncrease ? 0.8f : 1f;
            __instance.transform.Find("ComboName").localScale = comboNameScale.ToVector3();

            return false;
        }
    }
}