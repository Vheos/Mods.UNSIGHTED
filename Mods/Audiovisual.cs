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
        static private Dictionary<int, PaletteSettings> _paletteSettingsByPlayerID;
        static private Dictionary<SFX, SFXSettings> _sfxSettingsBySFX;
        override protected void Initialize()
        {
            _extraBrightness = CreateSetting(nameof(_extraBrightness), 0, IntRange(0, 100));

            _paletteSettingsByPlayerID = new Dictionary<int, PaletteSettings>();
            for (int playerID = 0; playerID < 2; playerID++)
                _paletteSettingsByPlayerID[playerID] = new PaletteSettings(this, playerID);

            _sfxSettingsBySFX = new Dictionary<SFX, SFXSettings>();
            foreach (var sfx in Utility.GetEnumValues<SFX>())
                _sfxSettingsBySFX[sfx] = new SFXSettings(this, sfx);
        }
        override protected void SetFormatting()
        {
            _extraBrightness.Format("Extra brightness");
            _extraBrightness.Description =
                "Increases the light exposure of night time and dark areas" +
                "\nThe darker the original color palette, the more extra exposure it gets (nighttime gardens are affected the most)" +
                "\n(requires game restart to take effect)" +
                "\n\nUnit: arbitrary linear scale";

            CreateHeader("Alma's color palette").Description =
                "Allows you to override Alma's sprite colors" +
                "\nYou can define separate palettes for each player";
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
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(Preset.Vheos_UI):
                    ForceApply();
                    _extraBrightness.Value = 33;
                    break;
            }
        }
        override protected string Description =>
            "Mods that affect the graphics and sound" +
            "\n\nExamples:" +
            "\n• Brighten up dark areas" +
            "\n• Customize Alma's color palette" +
            "\n• Change volume / pitch of menu SFX";

        // Privates
        private const float EXPOSURE_LERP_TARGET = 1.5f;
        static private string GetLocalizedString(string key)
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
            internal PaletteSettings(Audiovisual mod, int playerID) : base(mod, playerID)
            {
                SerializedData = _mod.CreateSetting(PlayerPrefix + nameof(SerializedData), "");
                ActionsSetting = _mod.CreateSetting(PlayerPrefix + nameof(ActionsSetting), (Actions)0);

                CommonToggle = _mod.CreateSetting(PlayerPrefix + nameof(CommonToggle), false);
                HairToggle = _mod.CreateSetting(PlayerPrefix + nameof(HairToggle), false);
                SkinToggle = _mod.CreateSetting(PlayerPrefix + nameof(SkinToggle), false);
                ArmorToggle = _mod.CreateSetting(PlayerPrefix + nameof(ArmorToggle), false);
                WeaponToggle = _mod.CreateSetting(PlayerPrefix + nameof(WeaponToggle), false);

                Highlight = _mod.CreateSetting(PlayerPrefix + nameof(Highlight), _playerID == 0 ?
                    new Color(0.9059f, 0.9451f, 0.9451f, 1f) : new Color(0.7961f, 0.7216f, 0.9843f, 1f));

                HairDark = _mod.CreateSetting(PlayerPrefix + nameof(HairDark), _playerID == 0 ?
                    new Color(0.6f, 0.5804f, 0.549f, 1f) : new Color(0f, 0.0784f, 0.4706f, 1f));
                HairRegular = _mod.CreateSetting(PlayerPrefix + nameof(HairRegular), _playerID == 0 ?
                    new Color(0.7333f, 0.7098f, 0.6784f, 1f) : new Color(0f, 0.1373f, 0.6431f, 1f));
                HairBright = _mod.CreateSetting(PlayerPrefix + nameof(HairBright), _playerID == 0 ?
                    new Color(0.8706f, 0.8706f, 0.8706f, 1f) : new Color(0f, 0.3843f, 0.9608f, 1f));

                SkinDark = _mod.CreateSetting(PlayerPrefix + nameof(SkinDark), _playerID == 0 ?
                    new Color(0.2588f, 0.1294f, 0.1333f, 1f) : new Color(0.2588f, 0.1294f, 0.1333f, 1f));
                SkinRegular = _mod.CreateSetting(PlayerPrefix + nameof(SkinRegular), _playerID == 0 ?
                    new Color(0.4627f, 0.2863f, 0.1765f, 1f) : new Color(0.5255f, 0.3961f, 0.2941f, 1f));
                SkinBright = _mod.CreateSetting(PlayerPrefix + nameof(SkinBright), _playerID == 0 ?
                    new Color(0.5255f, 0.4706f, 0.3059f, 1f) : new Color(0.7608f, 0.6353f, 0.4706f, 1f));

                ArmorMainDark = _mod.CreateSetting(PlayerPrefix + nameof(ArmorMainDark), new Color(0.1255f, 0.102f, 0.2314f, 1f));
                ArmorMainRegular = _mod.CreateSetting(PlayerPrefix + nameof(ArmorMainRegular), new Color(0.149f, 0.2275f, 0.5608f, 1f));
                ArmorSideDark = _mod.CreateSetting(PlayerPrefix + nameof(ArmorSideDark), new Color(0.5216f, 0.4667f, 0.6588f, 1f));
                ArmorSideRegular = _mod.CreateSetting(PlayerPrefix + nameof(ArmorSideRegular), new Color(0.749f, 0.6745f, 0.8078f, 1f));

                WeaponMain = _mod.CreateSetting(PlayerPrefix + nameof(WeaponMain), new Color(0.8902f, 0.8902f, 0.5216f, 1f));
                WeaponSlash = _mod.CreateSetting(PlayerPrefix + nameof(WeaponSlash), new Color(0.902f, 0.902f, 0.9216f, 1f));
                WeaponFlames = _mod.CreateSetting(PlayerPrefix + nameof(WeaponFlames), new Color(0.6549f, 0.5451f, 0.5608f, 1f));
                WeaponSparks = _mod.CreateSetting(PlayerPrefix + nameof(WeaponSparks), new Color(0.8902f, 0.7725f, 0.4549f, 1f));

                _colorsByToggle = new Dictionary<ModSetting<bool>, ModSetting<Color>[]>()
                {
                    [CommonToggle] = new[] { Highlight },
                    [HairToggle] = new[] { HairDark, HairRegular, HairBright },
                    [SkinToggle] = new[] { SkinDark, SkinRegular, SkinBright },
                    [ArmorToggle] = new[] { ArmorMainDark, ArmorMainRegular, ArmorSideDark, ArmorSideRegular },
                    [WeaponToggle] = new[] { WeaponMain, WeaponSlash, WeaponFlames, WeaponSparks },
                };

                // events
                ActionsSetting.AddEventSilently(SaveLoadResetFromConfig);
            }
            override public void Format()
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
                    Deserialize(SerializedData);
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

                if (PseudoSingleton<PlayersManager>.instance.TryNonNull(out var playerManager))
                    Apply(playerManager);

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
            {
                switch (sfx)
                {
                    case SFX.MenuNavigate: return sfxDatabase.menuSelect;
                    case SFX.MenuEnter: return sfxDatabase.menuClick;
                    case SFX.MenuEscape: return sfxDatabase.menuNegative;
                    default: return null;
                }
            }
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

        // Skin
        [HarmonyPatch(typeof(PlayersManager), nameof(PlayersManager.UpdatePlayerPalette)), HarmonyPostfix]
        static private void PlayersManager_UpdatePlayerPalette_Post(PlayersManager __instance, int playerNum)
        {
            if (!__instance.playerObjects[playerNum].gameObject.activeInHierarchy)
                return;

            _paletteSettingsByPlayerID[playerNum].Apply(__instance);
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
    }
}