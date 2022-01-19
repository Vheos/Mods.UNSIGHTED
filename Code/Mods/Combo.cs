namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections;
    using UnityEngine;
    using HarmonyLib;
    using Mods.Core;
    using Tools.Extensions.Math;

    public class Combo : AMod
    {
        // Section
        override protected string SectionOverride
        => Sections.BALANCE;
        override protected string Description =>
            "Mods related to the combo system" +
            "\n\nExamples:" +
            "\n• Change combo duration and decrease rate" +
            "\n• Change combo gain values per weapon type" +
            "\n• Change syringe gained along with combo";

        // Settings
        static private ModSetting<float> _duration;
        static private ModSetting<int> _decreaseRate;
        static private ModSetting<bool> _decayRateIsPercent;
        static private ModSetting<int> _gainPerSwordHit;
        static private ModSetting<int> _gainPerAxeHit;
        static private ModSetting<int> _gainPerGunHit;
        static private ModSetting<int> _gainPerShurikenHit;
        static private ModSetting<int> _gainPerParry;
        static private ModSetting<int> _lossPerHitTaken;
        static private ModSetting<bool> _lossPerHitTakenIsPercent;
        static private ModSetting<int> _comboChipBonus;
        static private ModSetting<int> _syringeGainRate;
        static private ModSetting<bool> _syringeGainRateAffectedByCombo;
        override protected void Initialize()
        {
            _duration = CreateSetting(nameof(_duration), 5f, FloatRange(0f, 5f));
            _decreaseRate = CreateSetting(nameof(_decreaseRate), 13, IntRange(0, 100));
            _decayRateIsPercent = CreateSetting(nameof(_decayRateIsPercent), false);

            _gainPerSwordHit = CreateSetting(nameof(_gainPerSwordHit), 4, IntRange(0, 25));
            _gainPerAxeHit = CreateSetting(nameof(_gainPerAxeHit), 10, IntRange(0, 25));
            _gainPerGunHit = CreateSetting(nameof(_gainPerGunHit), 2, IntRange(0, 25));
            _gainPerShurikenHit = CreateSetting(nameof(_gainPerShurikenHit), 4, IntRange(0, 25));

            _gainPerParry = CreateSetting(nameof(_gainPerParry), 5, IntRange(0, 25));
            _lossPerHitTaken = CreateSetting(nameof(_lossPerHitTaken), 60, IntRange(0, 100));
            _lossPerHitTakenIsPercent = CreateSetting(nameof(_lossPerHitTakenIsPercent), false);

            _comboChipBonus = CreateSetting(nameof(_comboChipBonus), 50, IntRange(0, 100));
            _syringeGainRate = CreateSetting(nameof(_syringeGainRate), 50, IntRange(0, 100));
            _syringeGainRateAffectedByCombo = CreateSetting(nameof(_syringeGainRateAffectedByCombo), true);
        }
        override protected void SetFormatting()
        {
            _duration.Format("Duration");
            _duration.Description =
                "How long your combo stays up (resets with every attack / parry)" +
                "\nLower values will encourage more proactive and aggressive combat style" +
                "\n\nUnit: seconds";
            _decreaseRate.Format("Decrease rate");
            _decreaseRate.Description =
                "How quickly your combo decreases after not attacking / parrying for the duration" +
                "\n\nUnit: percent points per second, or percent of last combo value per second";
            using (Indent)
                _decayRateIsPercent.Format("percent of last value", _decreaseRate, t => t > 0);

            CreateHeader("Gains per hit").Description =
                "How much combo you gain for each enemy hit with the given weapon type" +
                "\n\nUnit: percent points";
            using (Indent)
            {
                _gainPerSwordHit.Format("sword");
                _gainPerAxeHit.Format("axe");
                _gainPerGunHit.Format("gun");
                _gainPerShurikenHit.Format("shuriken");
            }

            _gainPerParry.Format("Gain per enemy parried");
            _gainPerParry.Description =
                "How much combo you gain for each successfully parried enemy";
            _lossPerHitTaken.Format("Loss on getting hit");
            _lossPerHitTaken.Description =
                "How much combo you lose when you get hit, regardless of damage taken" +
                "\n\nUnit: percent points, or percent of last combo value";
            using (Indent)
                _lossPerHitTakenIsPercent.Format("percent of last value", _lossPerHitTaken, t => t > 0);

            _comboChipBonus.Format("\"Fast Combo Chip\" bonus");
            _comboChipBonus.Description =
                "How much extra combo you get per each equipped \"Fast Combo Chip\"" +
                "\n\nUnit: percent of gained combo, stacks additively";
            _syringeGainRate.Format("Syringe gain rate");
            _syringeGainRate.Description =
                "How much of gained combo is converted to syringe refill" +
                "\nSet to 0 for a more survival (or Dark Souls) feel" +
                "\n\nUnit: percent of gained combo";
            using (Indent)
            {
                _syringeGainRateAffectedByCombo.Format("scale with combo", _syringeGainRate, t => t > 0);
                _syringeGainRateAffectedByCombo.Description =
                    "Multiplies the combo-to-syringe conversion rate by current combo";
            }
        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(SettingsPreset.Vheos_CoopRebalance):
                    ForceApply();
                    _duration.Value = 2f;
                    _decreaseRate.Value = 50;
                    _decayRateIsPercent.Value = true;

                    _gainPerSwordHit.Value = 5;
                    _gainPerAxeHit.Value = 10;
                    _gainPerGunHit.Value = 1;
                    _gainPerShurikenHit.Value = 5;
                    _gainPerParry.Value = 0;
                    _lossPerHitTaken.Value = 100;
                    _lossPerHitTakenIsPercent.Value = true;

                    _comboChipBonus.Value = 50;
                    _syringeGainRate.Value = 20;
                    _syringeGainRateAffectedByCombo.Value = false;
                    break;
            }
        }

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Combo multiplier
        [HarmonyPatch(typeof(ComboBar), nameof(ComboBar.AddComboValue),
            new[] { typeof(float) }),
            HarmonyPrefix]
        static private bool ComboBar_AddComboValue_Pre(ComboBar __instance, ref float ammountToAddInCombo)
        {
            __instance.CheckMaxComboValue();
            __instance.comboValue.SetClamp(1, __instance.maxComboValue);

            if (ammountToAddInCombo < 0f)
            {
                ammountToAddInCombo = -_lossPerHitTaken;
                if (_lossPerHitTakenIsPercent)
                    ammountToAddInCombo *= __instance.comboValue - 1;
            }
            else if (Time.time == PseudoSingleton<PlayersManager>.instance.players[__instance.playerNum].myCharacter.myCharacterCollider.lastTimePerfectParry)
                ammountToAddInCombo = _gainPerParry;

            ammountToAddInCombo /= 100f;
            float syringeGain = ammountToAddInCombo * _syringeGainRate / 2f / 100f;
            if (!_syringeGainRateAffectedByCombo)
                syringeGain /= __instance.comboValue;
            PseudoSingleton<LifeBarsManager>.instance.syringeList[__instance.playerNum].AddSyringeValue(syringeGain, false, true);

            ammountToAddInCombo *= 1f + _comboChipBonus / 100f * PseudoSingleton<Helpers>.instance.NumberOfChipsEquipped("ComboChipA", __instance.playerNum);
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

        [HarmonyPatch(typeof(ComboBar), nameof(ComboBar.AddComboValue),
            new[] { typeof(MeleeWeaponClass), typeof(bool), typeof(bool), typeof(int) }),
            HarmonyPrefix]
        static private bool ComboBar_AddComboValue2_Pre(ComboBar __instance, ref int __result, MeleeWeaponClass meleeWeaponClass, bool projectileDamage, bool enemyWasStunned, int damage)
        {
            __instance.CheckMaxComboValue();
            __instance.comboValue.SetClamp(1, __instance.maxComboValue);
            __result = damage.Mul(__instance.comboValue).Round();

            float comboIncrease = 0f;
            if (projectileDamage)
                comboIncrease = _gainPerGunHit;
            else if (meleeWeaponClass == MeleeWeaponClass.Sword)
                comboIncrease = _gainPerSwordHit;
            else if (meleeWeaponClass == MeleeWeaponClass.Axe)
                comboIncrease = _gainPerAxeHit;
            else if (meleeWeaponClass == MeleeWeaponClass.Shuriken)
                comboIncrease = _gainPerShurikenHit;

            comboIncrease /= 100f;
            float syringeGain = comboIncrease * _syringeGainRate / 2f / 100f;
            if (!_syringeGainRateAffectedByCombo)
                syringeGain /= __instance.comboValue;
            PseudoSingleton<LifeBarsManager>.instance.syringeList[__instance.playerNum].AddSyringeValue(syringeGain, false, true);

            comboIncrease *= 1f + _comboChipBonus / 100f * PseudoSingleton<Helpers>.instance.NumberOfChipsEquipped("ComboChipA", __instance.playerNum);
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
            yield return gameTime.WaitForSeconds(_duration);
            float decayBase = _decayRateIsPercent ? __instance.comboValue - 1f : 1f;
            while (__instance.comboValue >= 1f)
            {
                __instance.comboValue -= gameTime.deltaTime * _decreaseRate / 100f * decayBase;
                __instance.UpdateComboBar();
                yield return null;
            }

            __instance.comboValue = 1f;
        }

        [HarmonyPatch(typeof(ComboBar), nameof(ComboBar.OnEnable)), HarmonyPrefix]
        static private bool ComboBar_OnEnable_Pre(ComboBar __instance)
        => false;

        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.DeathCoroutine)), HarmonyPostfix]
        static private void BasicCharacterController_DeathCoroutine_Post(BasicCharacterController __instance)
        {
            ComboBar comboBar = PseudoSingleton<LifeBarsManager>.instance.comboBarList[__instance.myInfo.playerNum];
            comboBar.comboValue = 1;
            comboBar.gameObject.SetActive(false);
        }
    }
}