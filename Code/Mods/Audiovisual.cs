
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.PostProcessing;
using Vheos.Mods.Core;

namespace Vheos.Mods.UNSIGHTED;
public class Audiovisual : AMod
{
    // Section
    protected override string Description =>
        "Mods that affect the graphics and sound" +
        "\n\nExamples:" +
        "\n• Brighten up dark areas" +
        "\n• Customize loadout-switching text and SFX" +
        "\n• Customize Alma's color palette" +
        "\n• Change volume and pitch of menu SFX";
    protected override string SectionOverride
    => Sections.QOL;

    // Settings
    private static ModSetting<int> _extraBrightness;
    private static ModSetting<LoadoutText> _loadoutText;
    private static ModSetting<LetterCase> _loadoutTextCase;
    private static ModSetting<float> _loadoutTextDuration;
    private static ModSetting<int> _loadoutTextSize;
    private static ModSetting<int> _loadoutTextStartHeight;
    private static ModSetting<int> _loadoutTextFloatUpSpeed;
    private static ModSetting<LoadoutSFX> _loadoutSFX;
    private static ModSetting<int> _loadoutSFXVolume;
    private static ModSetting<int> _loadoutSFXPitch;
    private static Dictionary<int, PaletteSettings> _paletteSettingsByPlayerID;
    private static Dictionary<SFX, SFXSettings> _sfxSettingsBySFX;
    protected override void Initialize()
    {
        _extraBrightness = CreateSetting(nameof(_extraBrightness), 0, IntRange(0, 100));

        _loadoutSFX = CreateSetting(nameof(_loadoutSFX), LoadoutSFX.Original);
        _loadoutSFXVolume = CreateSetting(nameof(_loadoutSFXVolume), 100, IntRange(0, 100));
        _loadoutSFXPitch = CreateSetting(nameof(_loadoutSFXPitch), 100, IntRange(25, 400));
        _loadoutText = CreateSetting(nameof(_loadoutText), LoadoutText.Original);
        _loadoutTextCase = CreateSetting(nameof(_loadoutTextCase), LetterCase.Upper);
        _loadoutTextDuration = CreateSetting(nameof(_loadoutTextDuration), 0.5f, FloatRange(0.5f, 2f));
        _loadoutTextSize = CreateSetting(nameof(_loadoutTextSize), 100, IntRange(50, 200));
        _loadoutTextStartHeight = CreateSetting(nameof(_loadoutTextStartHeight), 83, IntRange(0, 200));
        _loadoutTextFloatUpSpeed = CreateSetting(nameof(_loadoutTextFloatUpSpeed), 70, IntRange(0, 100));

        _paletteSettingsByPlayerID = new Dictionary<int, PaletteSettings>();
        for (int playerID = 0; playerID < 2; playerID++)
            _paletteSettingsByPlayerID[playerID] = new PaletteSettings(this, playerID);

        _sfxSettingsBySFX = new Dictionary<SFX, SFXSettings>();
        foreach (var sfx in Utility.GetEnumValues<SFX>())
            _sfxSettingsBySFX[sfx] = new SFXSettings(this, sfx);
    }
    protected override void SetFormatting()
    {
        _extraBrightness.Format("Extra brightness");
        _extraBrightness.Description =
            "Increases the light exposure of night time and dark areas" +
            "\nThe darker the original color palette, the more extra exposure it gets (nighttime gardens are affected the most)" +
            "\n(requires game restart to take effect)" +
            "\n\nUnit: arbitrary linear scale";

        CreateHeader("Loadouts").Description =
            "Allows you to customize the SFX and text pop-up that accompany switching loadouts";
        using (Indent)
        {
            _loadoutText.Format("Text");
            _loadoutText.Description =
                "What is actually displayed in the text pop-up" +
                $"\n• {LoadoutText.None} - don't display anything" +
                $"\n• {LoadoutText.LetterOnly} - display loadout letter only (eg. \"A\")" +
                $"\n• {LoadoutText.Original} - the original format (eg. \"Loadout A\")" +
                $"\n• {LoadoutText.FirstWeapon} - display the first weapon's name (eg. \"Iron Edge\")" +
                $"\n• {LoadoutText.BothWeapons} - display both weapons' names, vertically";
            using (Indent)
            {
                _loadoutTextCase.Format("letter case", _loadoutText, LoadoutText.None, false);
                _loadoutTextCase.Description =
                    "What case is used in the text pop-up:" +
                    $"\n• {LetterCase.Lower} - lower case (eg. \"loadout a\", \"iron edge\")" +
                    $"\n• {LetterCase.Proper} - proper case (eg. \"Loadout A\", \"Iron Edge\")" +
                    $"\n• {LetterCase.Upper} - upper case (eg. \"LOADOUT A\", \"IRON EDGE\")";
                _loadoutTextDuration.Format("duration", _loadoutText, LoadoutText.None, false);
                _loadoutTextDuration.Description =
                    "How long the text pop-up stays up before disappearing" +
                    "\n\nUnit: seconds";
                _loadoutTextSize.Format("size", _loadoutText, LoadoutText.None, false);
                _loadoutTextSize.Description =
                     "How big the text pop-up is" +
                    "\n\nUnit: percent of original scale";
                _loadoutTextStartHeight.Format("height above sprite", _loadoutText, LoadoutText.None, false);
                _loadoutTextStartHeight.Description =
                    "How high above Alma's sprite the text pops up" +
                    "\n\nUnit: arbitrary, Unity-defined units";
                _loadoutTextFloatUpSpeed.Format("float-up speed", _loadoutText, LoadoutText.None, false);
                _loadoutTextFloatUpSpeed.Description =
                    "How fast the text moves upwards" +
                    "\n\nUnit: arbitrary linear scale";
            }

            _loadoutSFX.Format("SFX");
            _loadoutSFX.Description =
                "What SFX is actually played" +
                $"\n• {LoadoutSFX.None} - don't play any SFX" +
                $"\n• {LoadoutSFX.Original} - the original, high-pitched menu SFX" +
                $"\n• {LoadoutSFX.GunReload} - SFX used when performing a perfect reload" +
                $"\n• {LoadoutSFX.SwordZing} - SFX used when starting to charge a spin attack";
            using (Indent)
            {
                _loadoutSFXVolume.Format("volume", _loadoutSFX, LoadoutSFX.None, false);
                _loadoutSFXPitch.Format("pitch", _loadoutSFX, LoadoutSFX.None, false);
            }
        }

        CreateHeader("Color palettes").Description =
            "Allows you to override Alma's sprite colors for each player";
        using (Indent)
            foreach (var settings in _paletteSettingsByPlayerID)
                settings.Value.Format();

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
    }
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(SettingsPreset.Vheos_UI):
                ForceApply();
                _extraBrightness.Value = 33;
                _loadoutSFX.Value = LoadoutSFX.GunReload;
                _loadoutSFXVolume.Value = 75;
                _loadoutSFXPitch.Value = 150;
                _loadoutText.Value = LoadoutText.BothWeapons;
                _loadoutTextCase.Value = LetterCase.Upper;
                _loadoutTextDuration.Value = 1f;
                _loadoutTextSize.Value = 50;
                _loadoutTextStartHeight.Value = 50;
                _loadoutTextFloatUpSpeed.Value = 25;
                break;
        }
    }

    // Privates
    private const float EXPOSURE_LERP_TARGET = 1.5f;
    private const float ORIGINAL_STAMINA_BAR_HIDE_DELAY = 1.15f;
    private static string GetLocalizedString(string key)
    => TranslationSystem.FindTerm("Terms", key, true);

    // Defines
    #region SkinSettinsg
    private class PaletteSettings : PerPlayerSettings<Audiovisual>
    {
        // Settings
        private readonly ModSetting<string> SerializedData;
        private readonly ModSetting<Actions> ActionsSetting;
        private readonly ModSetting<bool> CommonToggle;
        private readonly ModSetting<bool> HairToggle;
        private readonly ModSetting<bool> SkinToggle;
        private readonly ModSetting<bool> ArmorToggle;
        private readonly ModSetting<bool> WeaponToggle;
        private readonly ColorSettings Highlight;
        private readonly ColorSettings HairDark;
        private readonly ColorSettings HairRegular;
        private readonly ColorSettings HairBright;
        private readonly ColorSettings SkinDark;
        private readonly ColorSettings SkinRegular;
        private readonly ColorSettings SkinBright;
        private readonly ColorSettings ArmorMainDark;
        private readonly ColorSettings ArmorMainRegular;
        private readonly ColorSettings ArmorSideDark;
        private readonly ColorSettings ArmorSideRegular;
        private readonly ColorSettings WeaponMain;
        private readonly ColorSettings WeaponSlash;
        private readonly ColorSettings WeaponFlames;
        private readonly ColorSettings WeaponSparks;
        internal PaletteSettings(Audiovisual mod, int playerID) : base(mod, playerID)
        {
            SerializedData = _mod.CreateSetting(PlayerPrefix + nameof(SerializedData), "");
            ActionsSetting = _mod.CreateSetting(PlayerPrefix + nameof(ActionsSetting), (Actions)0);

            CommonToggle = _mod.CreateSetting(PlayerPrefix + nameof(CommonToggle), false);
            HairToggle = _mod.CreateSetting(PlayerPrefix + nameof(HairToggle), false);
            SkinToggle = _mod.CreateSetting(PlayerPrefix + nameof(SkinToggle), false);
            ArmorToggle = _mod.CreateSetting(PlayerPrefix + nameof(ArmorToggle), false);
            WeaponToggle = _mod.CreateSetting(PlayerPrefix + nameof(WeaponToggle), false);

            Highlight = new ColorSettings(_mod, PlayerPrefix + nameof(Highlight), _playerID == 0 ?
                new Color(0.9059f, 0.9451f, 0.9451f) : new Color(0.7961f, 0.7216f, 0.9843f));

            HairDark = new ColorSettings(_mod, PlayerPrefix + nameof(HairDark), _playerID == 0 ?
                new Color(0.6f, 0.5804f, 0.549f) : new Color(0f, 0.0784f, 0.4706f));
            HairRegular = new ColorSettings(_mod, PlayerPrefix + nameof(HairRegular), _playerID == 0 ?
                new Color(0.7333f, 0.7098f, 0.6784f) : new Color(0f, 0.1373f, 0.6431f));
            HairBright = new ColorSettings(_mod, PlayerPrefix + nameof(HairBright), _playerID == 0 ?
                new Color(0.8706f, 0.8706f, 0.8706f) : new Color(0f, 0.3843f, 0.9608f));

            SkinDark = new ColorSettings(_mod, PlayerPrefix + nameof(SkinDark), _playerID == 0 ?
                new Color(0.2588f, 0.1294f, 0.1333f) : new Color(0.2588f, 0.1294f, 0.1333f));
            SkinRegular = new ColorSettings(_mod, PlayerPrefix + nameof(SkinRegular), _playerID == 0 ?
                new Color(0.4627f, 0.2863f, 0.1765f) : new Color(0.5255f, 0.3961f, 0.2941f));
            SkinBright = new ColorSettings(_mod, PlayerPrefix + nameof(SkinBright), _playerID == 0 ?
                new Color(0.5255f, 0.4706f, 0.3059f) : new Color(0.7608f, 0.6353f, 0.4706f));

            ArmorMainDark = new ColorSettings(_mod, PlayerPrefix + nameof(ArmorMainDark), _playerID == 0 ?
                new Color(0.1255f, 0.102f, 0.2314f) : new Color(0.2157f, 0.1686f, 0f));
            ArmorMainRegular = new ColorSettings(_mod, PlayerPrefix + nameof(ArmorMainRegular), _playerID == 0 ?
                new Color(0.149f, 0.2275f, 0.5608f) : new Color(0.4353f, 0.3567f, 0.01569f));
            ArmorSideDark = new ColorSettings(_mod, PlayerPrefix + nameof(ArmorSideDark), _playerID == 0 ?
                new Color(0.5216f, 0.4667f, 0.6588f) : new Color(0.6588f, 0.6314f, 0.4667f));
            ArmorSideRegular = new ColorSettings(_mod, PlayerPrefix + nameof(ArmorSideRegular), _playerID == 0 ?
                new Color(0.749f, 0.6745f, 0.8078f) : new Color(0.8078f, 0.8f, 0.6745f));

            WeaponMain = new ColorSettings(_mod, PlayerPrefix + nameof(WeaponMain), new Color(0.8902f, 0.8902f, 0.5216f));
            WeaponSlash = new ColorSettings(_mod, PlayerPrefix + nameof(WeaponSlash), new Color(0.902f, 0.902f, 0.9216f));
            WeaponFlames = new ColorSettings(_mod, PlayerPrefix + nameof(WeaponFlames), new Color(0.6549f, 0.5451f, 0.5608f));
            WeaponSparks = new ColorSettings(_mod, PlayerPrefix + nameof(WeaponSparks), new Color(0.8902f, 0.7725f, 0.4549f));

            _colorsByToggle = new Dictionary<ModSetting<bool>, ModSetting<Color>[]>()
            {
                [CommonToggle] = new ModSetting<Color>[] { Highlight },
                [HairToggle] = new ModSetting<Color>[] { HairDark, HairRegular, HairBright },
                [SkinToggle] = new ModSetting<Color>[] { SkinDark, SkinRegular, SkinBright },
                [ArmorToggle] = new ModSetting<Color>[] { ArmorMainDark, ArmorMainRegular, ArmorSideDark, ArmorSideRegular },
                [WeaponToggle] = new ModSetting<Color>[] { WeaponMain, WeaponSlash, WeaponFlames, WeaponSparks },
            };

            // events
            ActionsSetting.AddEventSilently(SaveLoadResetFromConfig);
        }
        public override void Format()
        {
            base.Format();
            using (Indent)
            {
                SerializedData.DisplayResetButton = false;
                SerializedData.Format("Sharecode", Toggle);
                SerializedData.Description =
                    "Allows you to share your color palette with others <3" +
                    "\n\"Load\" converts your settings into a sharecode" +
                    "\n\"Save\" converts the sharecode into settings" +
                    "\n\"Reset\" resets all settings to their defaults";
                ActionsSetting.DisplayResetButton = false;
                ActionsSetting.Format("", Toggle);

                CommonToggle.DisplayResetButton = false;
                CommonToggle.Format("Common", Toggle);
                using (Indent)
                {
                    Highlight.Format("Highlight", CommonToggle);
                }

                HairToggle.DisplayResetButton = false;
                HairToggle.Format("Hair", Toggle);
                using (Indent)
                {
                    HairDark.Format("Dark", HairToggle);
                    HairRegular.Format("Regular", HairToggle);
                    HairBright.Format("Bright", HairToggle);
                }

                SkinToggle.DisplayResetButton = false;
                SkinToggle.Format("Skin", Toggle);
                using (Indent)
                {
                    SkinDark.Format("Dark", SkinToggle);
                    SkinRegular.Format("Regular", SkinToggle);
                    SkinBright.Format("Bright", SkinToggle);
                }

                ArmorToggle.DisplayResetButton = false;
                ArmorToggle.Format("Armor", Toggle);
                using (Indent)
                {
                    ArmorMainDark.Format("Main, Dark", ArmorToggle);
                    ArmorMainRegular.Format("Main, Regular", ArmorToggle);
                    ArmorSideDark.Format("Side, Dark", ArmorToggle);
                    ArmorSideRegular.Format("Side, Regular", ArmorToggle);
                }

                WeaponToggle.DisplayResetButton = false;
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

        // Publics
        internal void TryApply(PlayersManager playerManager)
        {
            if (!playerManager.newColorPalette.TryGetNonNull(_playerID, out var palette))
                return;

            for (int i = 0; i < palette.width; i++)
                if (_paletteSettingsByPlayerID[_playerID].TryGetColor(i, out var customColor))
                    palette.SetPixel(i, 0, customColor);

            // Finalize
            palette.Apply();
            Material sharedMaterial = playerManager.playerObjects[_playerID].myCharacter.myAnimations.myAnimator.mySpriteRenderer.sharedMaterial;
            sharedMaterial.SetTexture("_SwapTex", palette);
        }

        // Privates
        private readonly Dictionary<ModSetting<bool>, ModSetting<Color>[]> _colorsByToggle;
        private int _deserializationIndex;
        private bool TryGetColor(int pixelX, out Color color)
        {
            switch (pixelX)
            {
                case 241 when CommonToggle: color = Highlight; return true;

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
        private void SaveLoadResetFromConfig()
        {
            if (ActionsSetting.Value.HasFlag(Actions.Save))
                SerializedData.SetSilently(Serialize());
            if (ActionsSetting.Value.HasFlag(Actions.Load))
            {
                SerializedData.Value = new string(SerializedData.Value.Where(t => t.IsHex()).ToArray());
                Deserialize(SerializedData);
            }

            if (ActionsSetting.Value.HasFlag(Actions.Reset))
            {
                SerializedData.SetSilently("");
                foreach (var colors in _colorsByToggle)
                {
                    colors.Key.Reset();
                    foreach (var color in colors.Value)
                        color.Reset();
                }
            }

            if (PlayersManager.instance.TryNonNull(out var playerManager))
                TryApply(playerManager);

            ActionsSetting.SetSilently(0);
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
        internal enum Actions
        {
            Save = 1 << 0,
            Load = 1 << 1,
            Reset = 1 << 2,
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

        // Publics
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
        private string GetInternalSFXName(SFX sfx, SoundEffectDatabase sfxDatabase)
        => sfx switch
        {
            SFX.MenuNavigate => sfxDatabase.menuSelect,
            SFX.MenuEnter => sfxDatabase.menuClick,
            SFX.MenuEscape => sfxDatabase.menuNegative,
            _ => null,
        };
    }
    #endregion

    private enum SFX
    {
        MenuNavigate,
        MenuEnter,
        MenuEscape,
    }

    private enum LetterCase
    {
        Lower,
        Proper,
        Upper,
    }
    private enum LoadoutSFX
    {
        None,
        Original,
        GunReload,
        SwordZing,
    }
    private enum LoadoutText
    {
        None,
        LetterOnly,
        Original,
        FirstWeapon,
        BothWeapons,
    }

    // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

    // Exposure
    [HarmonyPatch(typeof(Lists), nameof(Lists.Start)), HarmonyPostfix]
    private static void Lists_Start_Post(Lists __instance)
    {
        static PostProcessingProfile[] GetCameraProfiles(AreaDescription areaDescription)
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

    // Loadouts
    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.ShowCurrentLoadout)), HarmonyPrefix]
    private static bool BasicCharacterController_ShowCurrentLoadout_Pre(BasicCharacterController __instance)
    {
        // Text
        if (_loadoutText != LoadoutText.None)
        {
            var playerData = UnsightedHelpers.instance.GetPlayerData();
            var playerID = __instance.myInfo.playerNum;
            int currentLoadoutID = playerID == 0 ? playerData.currentLoadoutP1 : playerData.currentLoadoutP2;
            string text = "";
            switch (_loadoutText.Value)
            {
                case LoadoutText.Original:
                    switch (currentLoadoutID)
                    {
                        case 0: text = "Loadout A"; break;
                        case 1: text = "Loadout B"; break;
                        case 2: text = "Loadout C"; break;
                    }

                    break;
                case LoadoutText.LetterOnly:
                    switch (currentLoadoutID)
                    {
                        case 0: text = "A"; break;
                        case 1: text = "B"; break;
                        case 2: text = "C"; break;
                    }

                    break;
                case LoadoutText.FirstWeapon:
                    text = TranslationSystem.FindTerm("ItemNames", playerData.playersEquipData[playerID].weapons[0].Replace(" ", ""), false);
                    break;
                case LoadoutText.BothWeapons:
                    text = TranslationSystem.FindTerm("ItemNames", playerData.playersEquipData[playerID].weapons[0].Replace(" ", ""), false) + "\n\n"
                         + TranslationSystem.FindTerm("ItemNames", playerData.playersEquipData[playerID].weapons[1].Replace(" ", ""), false);
                    break;
            }

            // case
            if (_loadoutTextCase == LetterCase.Lower)
                text = text.ToLower();
            else if (_loadoutTextCase == LetterCase.Upper)
                text = text.ToUpper();

            // offset
            float popupOffset = __instance.myPhysics.globalHeight + __instance.myPhysics.Zsize + _loadoutTextStartHeight / 100f;
            if (Time.time - __instance.lastStaminaRechargeTime <= ORIGINAL_STAMINA_BAR_HIDE_DELAY)
                popupOffset += 0.5f;

            // display
            InGameTextController.instance.ShowText
                (text, __instance.transform.position + Vector3.up * popupOffset,
                _loadoutTextDuration, ColorNames.White, 0f, _loadoutTextFloatUpSpeed * 2 / 100f, false, ColorNames.White, _loadoutTextSize / 100f, ColorNames.Black);
        }

        // SFX
        if (_loadoutSFX != LoadoutSFX.None)
        {
            var gameSounds = GlobalGameManager.instance.gameSounds;
            float volume = _loadoutSFXVolume / 100f;
            float pitch = _loadoutSFXPitch / 100f;
            switch (_loadoutSFX.Value)
            {
                case LoadoutSFX.Original: AudioController.Play(gameSounds.menuSelect, volume, pitch); break;
                case LoadoutSFX.GunReload: AudioController.Play(gameSounds.playerReloadSound, volume, pitch).setParameterValue("perfectReload", 1f); break;
                case LoadoutSFX.SwordZing: AudioController.Play(gameSounds.playerDeathSheen, volume, pitch); break;
            }
        }

        return false;
    }

    // Skin
    [HarmonyPatch(typeof(PlayersManager), nameof(PlayersManager.UpdatePlayerPalette)), HarmonyPostfix]
    private static void PlayersManager_UpdatePlayerPalette_Post(PlayersManager __instance, int playerNum)
    {
        if (!__instance.playerObjects[playerNum].gameObject.activeInHierarchy
        || !_paletteSettingsByPlayerID[playerNum].Toggle)
            return;

        _paletteSettingsByPlayerID[playerNum].TryApply(__instance);
    }

    // SFX
    [HarmonyPatch(typeof(GlobalGameManager), nameof(GlobalGameManager.Awake)), HarmonyPostfix]
    private static void GlobalGameManager_Awake_Post(GlobalGameManager __instance)
    {
        foreach (var settings in _sfxSettingsBySFX)
            settings.Value.InitializeInternalSFXName(__instance.gameSounds);
    }

    [HarmonyPatch(typeof(AudioController), nameof(AudioController.Play)), HarmonyPrefix]
    private static void AudioController_Play_Pre(AudioController __instance, string eventName, ref float volume, ref float pitch)
    {
        foreach (var settingsBySFX in _sfxSettingsBySFX)
            settingsBySFX.Value.TryApply(eventName, ref volume, ref pitch);
    }
}