namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.PostProcessing;
    using HarmonyLib;
    using Tools.ModdingCore;
    using Tools.Extensions.Math;
    using Tools.Extensions.General;
    using Tools.Extensions.UnityObjects;
    using System.Linq;
    using Vheos.Tools.UtilityN;
    using System.Collections;
    using System.Text;

    public class Audiovisual : AMod
    {
        // Settings
        static private ModSetting<int> _extraBrightness;
        static private ModSetting<bool> _damagePopups;
        static private ModSetting<bool> _statusEffectPopups;
        static private ModSetting<bool> _criticalPopup;
        static private ModSetting<bool> _parryPopup;
        static private ModSetting<bool> _clockTime;
        static private ModSetting<bool> _clockDay;
        static private ModSetting<bool> _comboCounter;
        static private ModSetting<bool> _comboCounterAsPercentIncrease;
        static private ModSetting<bool> _comboProgressBar;
        static private Dictionary<SFX, SFXSettings> _sfxSettingsBySFX;
        static private Dictionary<int, PaletteSettings> _paletteSettingsByPlayerID;
        static private ModSetting<bool> _applyColorPalettes;
        override protected void Initialize()
        {
            _extraBrightness = CreateSetting(nameof(_extraBrightness), 0, IntRange(0, 100));

            _damagePopups = CreateSetting(nameof(_damagePopups), false);
            _statusEffectPopups = CreateSetting(nameof(_statusEffectPopups), false);
            _criticalPopup = CreateSetting(nameof(_criticalPopup), false);
            _parryPopup = CreateSetting(nameof(_parryPopup), false);

            _comboCounter = CreateSetting(nameof(_comboCounter), false);
            _comboCounterAsPercentIncrease = CreateSetting(nameof(_comboCounterAsPercentIncrease), false);
            _comboProgressBar = CreateSetting(nameof(_comboProgressBar), false);

            _clockTime = CreateSetting(nameof(_clockTime), false);
            _clockDay = CreateSetting(nameof(_clockDay), false);

            _sfxSettingsBySFX = new Dictionary<SFX, SFXSettings>();
            foreach (var sfx in Utility.GetEnumValues<SFX>())
                _sfxSettingsBySFX[sfx] = new SFXSettings(this, sfx);

            _paletteSettingsByPlayerID = new Dictionary<int, PaletteSettings>();
            foreach (var playerID in new[] { 0, 1 })
                _paletteSettingsByPlayerID[playerID] = new PaletteSettings(this, playerID);

            _applyColorPalettes = CreateSetting(nameof(_applyColorPalettes), false);

            // Events
            AddEventOnConfigClosed(() => UpdateClockVisibility(PseudoSingleton<InGameClock>.instance));
        }
        override protected void SetFormatting()
        {
            _extraBrightness.Format("Extra brightness");
            _extraBrightness.Description =
                "Increases the light exposure of night time and dark areas" +
                "\nThe darker the original color palette, the more extra exposure it gets (nighttime gardens are affected the most)" +
                "\n(requires game restart)" +
                "\n\nUnit: arbitrary linear scale";

            CreateHeader("Combat popups").Description =
                "Allows you to hide chosen text popups to make combat less cluttered and more immersive";
            using (Indent)
            {
                _damagePopups.Format("damage numbers");
                _damagePopups.Description =
                    "Displays damage numbers";
                _statusEffectPopups.Format("status effects");
                _statusEffectPopups.Description =
                    "Displays the big \"BURN!\", \"FROZEN!\" and \"DEF. DOWN!\" popups when inflicting elemental status effects";
                _criticalPopup.Format("critical hit");
                _criticalPopup.Description =
                    "Displays the big red \"CRITICAL!\" popup when attacking a stunned enemy";
                _parryPopup.Format("parry");
                _parryPopup.Description =
                    "Displays the big green \"PERFECT!\" popup when parrying";
            }

            _comboCounter.Format("Combo counter");
            _comboCounter.Description =
                "Displays the current damage multiplier gained from combo";
            using (Indent)
            {
                _comboCounterAsPercentIncrease.Format("format as percent increase", _comboCounter);
                _comboCounterAsPercentIncrease.Description =
                    "Changes the combo counter formatting from a multiplier (eg. 1.23x) to a percent increase (eg. +23%)";
            }
            _comboProgressBar.Format("Combo progress bar");
            _comboProgressBar.Description =
                "Displays the colorful progress bar below the numerical combo value";

            _clockTime.Format("Clock time");
            _clockTime.Description =
                "Displays the hours / minutes counter";
            _clockDay.Format("Clock day");
            _clockDay.Description =
                "Displays the day counter";

            CreateHeader("Menu SFX overrides").Description =
                "Overrides the volume / pitch of chosen menu sound effects";
            using (Indent)
            {
                _sfxSettingsBySFX[SFX.MenuNavigate].Format("Select");
                _sfxSettingsBySFX[SFX.MenuNavigate].Description =
                    "Plays when you navigate the menu buttons" +
                    "\nThis sound is very high-pitched, which makes it very irritating to sensitive and tinnitus-prone people";
                _sfxSettingsBySFX[SFX.MenuEnter].Format("Confirm");
                _sfxSettingsBySFX[SFX.MenuEnter].Description =
                    "Plays when you enter a new menu screen" +
                    "\nThis sound is very loud and harsh, so it gets annoying after a few menu clicks";
                _sfxSettingsBySFX[SFX.MenuEscape].Format("Cancel");
                _sfxSettingsBySFX[SFX.MenuEscape].Description =
                    "Plays when you return to a previous menu screen";
            }

            CreateHeader("Alma's color palette").Description =
                "Allows you to override Alma's sprite colors" +
                "\nYou can define separate palettes for each player";
            using (Indent)
            {
                _applyColorPalettes.Format("Apply");
                _applyColorPalettes.Description =
                    "Instantly applies all active color overrides";
                _paletteSettingsByPlayerID[0].Format("Player 1");
                _paletteSettingsByPlayerID[1].Format("Player 2");
            }
        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(Preset.Coop_NewGameExtra_HardMode):
                    ForceApply();
                    _extraBrightness.Value = 33;
                    _statusEffectPopups.Value = true;
                    _criticalPopup.Value = true;
                    _parryPopup.Value = true;
                    _comboCounterAsPercentIncrease.Value = true;
                    _comboProgressBar.Value = false;
                    _clockTime.Value = false;
                    _clockDay.Value = true;
                    break;
            }
        }
        override protected string Description =>
            "Mods that affect the audio / visual layers of the game" +
            "\n\nExamples:" +
            "\n• Brighten up dark areas" +
            "\n• Hide combat popups" +
            "\n• Hide current day / time" +
            "\n• Customize combo display" +
            "\n• Change volume / pitch of menu SFX" +
            "\n• Customize Alma's color palette";

        // Privates
        private const float EXPOSURE_LERP_TARGET = 1.5f;
        static private void UpdateClockVisibility(InGameClock inGameClock)
        {
            if (inGameClock == null)
                return;

            inGameClock.clockText.transform.localScale = _clockTime ? Vector3.one : Vector3.zero;
            inGameClock.dayText.transform.localScale = _clockDay ? Vector3.one : Vector3.zero;
        }
        static private bool HasAnyPlayerJustParried()
        => PseudoSingleton<PlayersManager>.instance.TryNonNull(out var playerManager)
        && (playerManager.playerObjects[0].myCharacter.justDidAPerfectParry
        || playerManager.playerObjects[1].myCharacter.justDidAPerfectParry);
        static private string GetLocalizedString(string key)
        => TranslationSystem.FindTerm("Terms", key, true);
        static private string GetInternalSFXName(SFX sfx, SoundEffectDatabase sfxDatabase)
        {
            switch (sfx)
            {
                case SFX.MenuNavigate: return sfxDatabase.menuSelect;
                case SFX.MenuEnter: return sfxDatabase.menuClick;
                case SFX.MenuEscape: return sfxDatabase.menuNegative;
                default: return null;
            }
        }

        // Defines
        #region SkinSettinsg
        private class PaletteSettings
        {
            // Settings
            private readonly ModSetting<string> SerializedData;
            private readonly ModSetting<SaveLoad> SaveLoadSetting;
            private readonly ModSetting<bool> Toggle;
            private readonly ModSetting<bool> CommonToggle;
            private readonly ModSetting<bool> HairToggle;
            private readonly ModSetting<bool> SkinToggle;
            private readonly ModSetting<bool> ArmorToggle;
            private readonly ModSetting<bool> WeaponToggle;
            private readonly ModSetting<Color> Highlight;
            private readonly ModSetting<Color> HairDark;
            private readonly ModSetting<Color> HairRegular;
            private readonly ModSetting<Color> HairBright;
            private readonly ModSetting<Color> SkinDark;
            private readonly ModSetting<Color> SkinRegular;
            private readonly ModSetting<Color> SkinBright;
            private readonly ModSetting<Color> ArmorMainDark;
            private readonly ModSetting<Color> ArmorMainRegular;
            private readonly ModSetting<Color> ArmorSideDark;
            private readonly ModSetting<Color> ArmorSideRegular;
            private readonly ModSetting<Color> WeaponMain;
            private readonly ModSetting<Color> WeaponSlash;
            private readonly ModSetting<Color> WeaponFlames;
            private readonly ModSetting<Color> WeaponSparks;
            internal PaletteSettings(Audiovisual mod, int playerID)
            {
                _mod = mod;
                _playerID = playerID;
                string keyPrefix = $"Player{_playerID + 1}_";

                SerializedData = _mod.CreateSetting(keyPrefix + nameof(SerializedData), "");
                SaveLoadSetting = _mod.CreateSetting(keyPrefix + nameof(SaveLoadSetting), (SaveLoad)0);

                Toggle = _mod.CreateSetting(keyPrefix + nameof(Toggle), false);
                CommonToggle = _mod.CreateSetting(keyPrefix + nameof(CommonToggle), false);
                HairToggle = _mod.CreateSetting(keyPrefix + nameof(HairToggle), false);
                SkinToggle = _mod.CreateSetting(keyPrefix + nameof(SkinToggle), false);
                ArmorToggle = _mod.CreateSetting(keyPrefix + nameof(ArmorToggle), false);
                WeaponToggle = _mod.CreateSetting(keyPrefix + nameof(WeaponToggle), false);

                Highlight = _mod.CreateSetting(keyPrefix + nameof(Highlight), _playerID == 0 ?
                    new Color(0.9059f, 0.9451f, 0.9451f, 1f) : new Color(0.7961f, 0.7216f, 0.9843f, 1f));

                HairDark = _mod.CreateSetting(keyPrefix + nameof(HairDark), _playerID == 0 ?
                    new Color(0.6f, 0.5804f, 0.549f, 1f) : new Color(0f, 0.0784f, 0.4706f, 1f));
                HairRegular = _mod.CreateSetting(keyPrefix + nameof(HairRegular), _playerID == 0 ?
                    new Color(0.7333f, 0.7098f, 0.6784f, 1f) : new Color(0f, 0.1373f, 0.6431f, 1f));
                HairBright = _mod.CreateSetting(keyPrefix + nameof(HairBright), _playerID == 0 ?
                    new Color(0.8706f, 0.8706f, 0.8706f, 1f) : new Color(0f, 0.3843f, 0.9608f, 1f));

                SkinDark = _mod.CreateSetting(keyPrefix + nameof(SkinDark), _playerID == 0 ?
                    new Color(0.2588f, 0.1294f, 0.1333f, 1f) : new Color(0.2588f, 0.1294f, 0.1333f, 1f));
                SkinRegular = _mod.CreateSetting(keyPrefix + nameof(SkinRegular), _playerID == 0 ?
                    new Color(0.4627f, 0.2863f, 0.1765f, 1f) : new Color(0.5255f, 0.3961f, 0.2941f, 1f));
                SkinBright = _mod.CreateSetting(keyPrefix + nameof(SkinBright), _playerID == 0 ?
                    new Color(0.5255f, 0.4706f, 0.3059f, 1f) : new Color(0.7608f, 0.6353f, 0.4706f, 1f));

                ArmorMainDark = _mod.CreateSetting(keyPrefix + nameof(ArmorMainDark), new Color(0.1255f, 0.102f, 0.2314f, 1f));
                ArmorMainRegular = _mod.CreateSetting(keyPrefix + nameof(ArmorMainRegular), new Color(0.149f, 0.2275f, 0.5608f, 1f));
                ArmorSideDark = _mod.CreateSetting(keyPrefix + nameof(ArmorSideDark), new Color(0.5216f, 0.4667f, 0.6588f, 1f));
                ArmorSideRegular = _mod.CreateSetting(keyPrefix + nameof(ArmorSideRegular), new Color(0.749f, 0.6745f, 0.8078f, 1f));

                WeaponMain = _mod.CreateSetting(keyPrefix + nameof(WeaponMain), new Color(0.8902f, 0.8902f, 0.5216f, 1f));
                WeaponSlash = _mod.CreateSetting(keyPrefix + nameof(WeaponSlash), new Color(0.902f, 0.902f, 0.9216f, 1f));
                WeaponFlames = _mod.CreateSetting(keyPrefix + nameof(WeaponFlames), new Color(0.6549f, 0.5451f, 0.5608f, 1f));
                WeaponSparks = _mod.CreateSetting(keyPrefix + nameof(WeaponSparks), new Color(0.8902f, 0.7725f, 0.4549f, 1f));

                _colorsByToggle = new Dictionary<ModSetting<bool>, ModSetting<Color>[]>()
                {
                    [CommonToggle] = new[] { Highlight },
                    [HairToggle] = new[] { HairDark, HairRegular, HairBright },
                    [SkinToggle] = new[] { SkinDark, SkinRegular, SkinBright },
                    [ArmorToggle] = new[] { ArmorMainDark, ArmorMainRegular, ArmorSideDark, ArmorSideRegular },
                    [WeaponToggle] = new[] { WeaponMain, WeaponSlash, WeaponFlames, WeaponSparks },
                };

                // events
                SaveLoadSetting.AddEventSilently(SaveLoadFromConfig);
            }
            internal void Format(string name)
            {
                Toggle.Format(name.ToString());
                using (Indent)
                {
                    SerializedData.Format("Sharecode", Toggle);
                    SerializedData.Description =
                        "Allows you to share your color palette with others <3" +
                        "\"Load\" converts your settings into a sharecode" +
                        "\"Save\" converts the sharecode into settings";
                    SaveLoadSetting.Format("");
                    CommonToggle.Format("Common", Toggle);
                    using (Indent)
                    {
                        Highlight.Format("Highlight", CommonToggle);
                    }
                    HairToggle.Format("Hair", Toggle);
                    using (Indent)
                    {
                        HairDark.Format("Dark", HairToggle);
                        HairRegular.Format("Regular", HairToggle);
                        HairBright.Format("Bright", HairToggle);
                    }
                    SkinToggle.Format("Skin", Toggle);
                    using (Indent)
                    {
                        SkinDark.Format("Dark", SkinToggle);
                        SkinRegular.Format("Regular", SkinToggle);
                        SkinBright.Format("Bright", SkinToggle);
                    }
                    ArmorToggle.Format("Armor", Toggle);
                    using (Indent)
                    {
                        ArmorMainDark.Format("Main, Dark", ArmorToggle);
                        ArmorMainRegular.Format("Main, Regular", ArmorToggle);
                        ArmorSideDark.Format("Side, Dark", ArmorToggle);
                        ArmorSideRegular.Format("Side, Regular", ArmorToggle);
                    }
                    WeaponToggle.Format("Weapon", Toggle);
                    using (Indent)
                    {
                        WeaponMain.Format("Main", WeaponToggle);
                        WeaponSlash.Format("Slash", WeaponToggle);
                        WeaponFlames.Format("Flames", WeaponToggle);
                        WeaponSparks.Format("Sparks", WeaponToggle);
                    }
                }
            }
            internal string Description
            {
                get => Toggle.Description;
                set => Toggle.Description = value;
            }
            internal void Apply(PlayersManager playerManager)
            {
                // Create new palette
                Texture2D palette = playerManager.newColorPalette[_playerID];
                for (int i = 0; i < palette.width; i++)
                    if (_paletteSettingsByPlayerID[_playerID].TryGetColor(i, out var customColor))
                        palette.SetPixel(i, 0, customColor);

                // Finalize
                palette.Apply();
                Material sharedMaterial = playerManager.playerObjects[_playerID].myCharacter.myAnimations.myAnimator.mySpriteRenderer.sharedMaterial;
                sharedMaterial.SetTexture("_SwapTex", palette);
            }

            // Privates
            private readonly Audiovisual _mod;
            private readonly int _playerID;
            private readonly Dictionary<ModSetting<bool>, ModSetting<Color>[]> _colorsByToggle;
            private int _deserializationIndex;
            private bool TryGetColor(int pixelX, out Color color)
            {
                switch (pixelX)
                {
                    case 241: color = Highlight; return true;

                    case 148 when HairToggle: color = HairDark; return true;
                    case 181 when HairToggle: color = HairRegular; return true;
                    case 222 when HairToggle: color = HairBright; return true;

                    case 33 when SkinToggle: color = SkinDark; return true;
                    case 73 when SkinToggle: color = SkinRegular; return true;
                    case 120 when SkinToggle: color = SkinBright; return true;

                    case 26 when ArmorToggle: color = ArmorMainDark; return true;
                    case 58 when ArmorToggle: color = ArmorMainRegular; return true;
                    case 119 when ArmorToggle: color = ArmorSideDark; return true;
                    case 172 when ArmorToggle: color = ArmorSideRegular; return true;

                    case 227 when WeaponToggle: color = WeaponMain; return true;
                    case 230 when WeaponToggle: color = WeaponSlash; return true;
                    case 139 when WeaponToggle: color = WeaponFlames; return true;
                    case 197 when WeaponToggle: color = WeaponSparks; return true;

                    default: color = default; return false;
                }
            }
            private void SaveLoadFromConfig()
            {
                if (SaveLoadSetting.Value.HasFlag(SaveLoad.Save))
                    SerializedData.SetSilently(Serialize());
                if (SaveLoadSetting.Value.HasFlag(SaveLoad.Load))
                    Deserialize(SerializedData);
                if (PseudoSingleton<PlayersManager>.instance.TryNonNull(out var playerManager))
                    Apply(playerManager);

                SaveLoadSetting.SetSilently(0);
            }

            // De/serialize
            private string Serialize()
            {
                var builder = new StringBuilder();
                foreach (var colors in _colorsByToggle)
                {
                    builder.Append(SerializeBool(colors.Key));
                    if (colors.Key)
                        foreach (var color in colors.Value)
                            builder.Append(SerializeColor(color));
                }
                return builder.ToString();
            }
            private string SerializeBool(bool t)
            => t ? "1" : "0";
            private string SerializeColor(Color t)
            => ColorUtility.ToHtmlStringRGB(t);
            private void Deserialize(string data)
            {
                _deserializationIndex = 0;
                foreach (var colors in _colorsByToggle)
                    try
                    {
                        if (TryDeserializeNextBool(data, colors.Key) && colors.Key)
                            foreach (var color in colors.Value)
                                TryDeserializeNextColor(data, color);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return;
                    }
            }
            private bool TryDeserializeNextBool(string data, ModSetting<bool> r)
            {
                int valueSize = 1;
                string serializedValue = data.Substring(_deserializationIndex, valueSize);

                if (serializedValue == "1")
                {
                    _deserializationIndex += valueSize;
                    r.Value = true;
                    return true;
                }
                else if (serializedValue == "0")
                {
                    _deserializationIndex += valueSize;
                    r.Value = false;
                    return true;
                }

                return false;
            }
            private bool TryDeserializeNextColor(string data, ModSetting<Color> r)
            {
                int valueSize = 6;
                string serializedValue = data.Substring(_deserializationIndex, valueSize);

                if (ColorUtility.TryParseHtmlString("#" + serializedValue, out var color))
                {
                    _deserializationIndex += valueSize;
                    r.Value = color;
                    return true;
                }

                return false;
            }

            // Defines
            [Flags]
            private enum SaveLoad
            {
                Save = 1 << 0,
                Load = 1 << 1,
            }
        }
        #endregion

        #region SFXSettings
        private class SFXSettings
        {
            // Settings
            private readonly ModSetting<bool> Toggle;
            private readonly ModSetting<int> Volume;
            private readonly ModSetting<int> Pitch;
            internal SFXSettings(Audiovisual mod, SFX sfx)
            {
                _mod = mod;
                _sfx = sfx;

                string keyPrefix = $"{_sfx}_";
                Toggle = _mod.CreateSetting(keyPrefix + nameof(Toggle), false);
                Volume = _mod.CreateSetting(keyPrefix + nameof(Volume), 100, _mod.IntRange(0, 200));
                Pitch = _mod.CreateSetting(keyPrefix + nameof(Pitch), 100, _mod.IntRange(25, 400));
            }
            internal void Format(string name)
            {
                Toggle.Format(name.ToString());
                using (Indent)
                {
                    Volume.Format("Volume", Toggle);
                    Pitch.Format("Pitch", Toggle);
                }
            }
            internal string Description
            {
                get => Toggle.Description;
                set => Toggle.Description = value;
            }
            internal void TryApply(string internalSFXName, ref float volume, ref float pitch)
            {
                if (!Toggle || internalSFXName != _internalSFXName)
                    return;

                volume = Volume / 100f;
                pitch = Pitch / 100f;
            }
            internal void InitializeInternalSFXName(SoundEffectDatabase soundsDatabase)
            => _internalSFXName = GetInternalSFXName(_sfx, soundsDatabase);

            // Privates
            private readonly Audiovisual _mod;
            private readonly SFX _sfx;
            private string _internalSFXName;
        }
        #endregion

        private enum SFX
        {
            MenuNavigate,
            MenuEnter,
            MenuEscape,
        }

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Exposure
        [HarmonyPatch(typeof(Lists), nameof(Lists.Start)), HarmonyPostfix]
        static private void Lists_Start_Post(Lists __instance)
        {
            PostProcessingProfile[] GetCameraProfiles(AreaDescription areaDescription)
            => new[]
            {
                areaDescription.morningCameraProfile,
                areaDescription.dayCameraProfile,
                areaDescription.eveningCameraProfile,
                areaDescription.nightCameraProfile
            };

            var processedProfiles = new HashSet<PostProcessingProfile>();
            foreach (var areaDescription in __instance.areaDatabase.areas)
                foreach (var profile in GetCameraProfiles(areaDescription))
                    if (!processedProfiles.Contains(profile))
                    {
                        var settings = profile.colorGrading.settings;
                        settings.basic.postExposure.SetLerp(EXPOSURE_LERP_TARGET, _extraBrightness / 100f);
                        profile.colorGrading.settings = settings;
                        processedProfiles.Add(profile);
                    }
        }

        [HarmonyPatch(typeof(InGameTextController), nameof(InGameTextController.ShowText)), HarmonyPrefix]
        static private bool InGameTextController_ShowText_Pre(InGameTextController __instance, ref string text)
        {
            if (!_damagePopups && text.Any(char.IsDigit))
                return false;
            if (!_statusEffectPopups && (text == GetLocalizedString("Burn") || text == GetLocalizedString("Frozen") || text == GetLocalizedString("DefDown")))
                return false;
            if (!_criticalPopup && text.Contains("CRITICAL!\n"))
                text = text.Replace("CRITICAL!\n", null);
            if (!_parryPopup && text == "PERFECT!" && HasAnyPlayerJustParried())
                return false;

            return true;
        }

        // Hide clock
        [HarmonyPatch(typeof(InGameClock), nameof(InGameClock.UpdateClock)), HarmonyPostfix]
        static private void InGameClock_Start_Post(InGameClock __instance)
        => UpdateClockVisibility(__instance);

        [HarmonyPatch(typeof(ComboBar), nameof(ComboBar.SetComboText)), HarmonyPrefix]
        static private bool ComboBar_SetComboText_Pre(ComboBar __instance)
        {
            if (!_comboCounterAsPercentIncrease)
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
            // counter
            float comboCounterScale = _comboCounter ? 1f : 0f;
            float comboNameScale = _comboCounter
                                 ? _comboCounterAsPercentIncrease ? 0.75f : 1f
                                 : 0f;

            __instance.valueText.transform.localScale = comboCounterScale.ToVector3();
            __instance.transform.Find("ComboName").localScale = comboNameScale.ToVector3();

            // progress bar
            __instance.FindChild("EmptyBar").SetActive(_comboProgressBar);
            __instance.barFill.gameObject.SetActive(_comboProgressBar);

            return false;
        }

        // SFX
        [HarmonyPatch(typeof(GlobalGameManager), nameof(GlobalGameManager.Awake)), HarmonyPostfix]
        static private void GlobalGameManager_Awake_Post(GlobalGameManager __instance)
        {
            foreach (var settings in _sfxSettingsBySFX)
                settings.Value.InitializeInternalSFXName(__instance.gameSounds);
        }

        [HarmonyPatch(typeof(AudioController), nameof(AudioController.Play)), HarmonyPrefix]
        static private void AudioController_Play_Pre(AudioController __instance, string eventName, ref float volume, ref float pitch)
        {
            foreach (var settingsBySFX in _sfxSettingsBySFX)
                settingsBySFX.Value.TryApply(eventName, ref volume, ref pitch);
        }

        // Skin
        [HarmonyPatch(typeof(PlayersManager), nameof(PlayersManager.UpdatePlayerPalette)), HarmonyPostfix]
        static private void PlayersManager_UpdatePlayerPalette_Post(PlayersManager __instance)
        {
            foreach (var settings in _paletteSettingsByPlayerID)
                settings.Value.Apply(__instance);
        }
    }
}