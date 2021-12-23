namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using UnityEngine;
    using HarmonyLib;
    using Tools.ModdingCore;
    using Tools.Extensions.General;
    using Tools.Extensions.UnityObjects;
    using Tools.Extensions.Math;
    using Tools.Extensions.Reflection;
    using Tools.Extensions.Collections;
    using Vheos.Tools.Extensions.DumpN;

    internal class CustomControls : ACustomPopup<CustomControls>
    {
        // Publics
        static internal ModSetting<string> AddControlsButton(int playerID, string name)
        {
            // setting
            string settingGUID = GetSettingGUID(playerID, name);
            var setting = new ModSetting<string>("", settingGUID, KeyCode.None.ToString());
            setting.IsVisible = false;

            string buttonGUID = GetButtonGUID(name);
            if (!_settingsByButtonGUID.ContainsKey(buttonGUID))
            {
                _settingsByButtonGUID.Add(buttonGUID, new ModSetting<string>[2]);

                // initialize button
                var newButton = GameObject.Instantiate(_buttonPrefab).GetComponent<ChangeInputButton>();
                newButton.BecomeChildOf(_buttonsHolder);
                newButton.inputName = buttonGUID;
                newButton.currentKey = setting.ToKeyCode();
                newButton.buttonName.originalText = setting.Value.ToUpper();
                newButton.buttonName.linesParent.GetChildGameObjects().Destroy();

                var actionName = newButton.GetComponentInChildren<FText>();
                actionName.originalText = actionName.text = name;
                actionName.GetComponent<UITranslateText>().enabled = false;
            }

            _settingsByButtonGUID[buttonGUID][playerID] = setting;
            return setting;
        }
        static internal void UpdateButtonsTable()
        {
            // table size and position
            RectTransform tableRect = _buttonsHolder.GetComponent<RectTransform>();
            tableRect.sizeDelta = TABLE_SIZE;
            tableRect.anchoredPosition = new Vector2(0, -32);

            // buttons size and position
            Vector2Int tableCounts = GetTableCounts(tableRect.childCount);
            var buttonsTable = tableCounts.ToArray2D<RectTransform>();

            Vector3 textScale = Vector3.one;
            if (tableCounts.x >= COMPACT_TEXT_SCALE_THRESHOLD.x)
                textScale.x = COMPACT_TEXT_SCALE.x;
            if (tableCounts.y >= COMPACT_TEXT_SCALE_THRESHOLD.y)
                textScale.y = COMPACT_TEXT_SCALE.y;

            for (int i = 0; i < tableRect.childCount; i++)
            {
                var buttonRect = tableRect.GetChild(i) as RectTransform;
                int ix = i / tableCounts.y;
                int iy = i % tableCounts.y;
                buttonsTable[ix][iy] = buttonRect;

                buttonRect.pivot = new Vector2(0.5f, 0.5f);
                buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0, 1);
                Vector2 buttonTotalSize = tableRect.sizeDelta / tableCounts;
                buttonRect.anchoredPosition = buttonTotalSize * new Vector2(ix + 0.5f, -(iy + 0.5f));
                buttonRect.sizeDelta = buttonTotalSize - 2 * BUTTON_PADDING;

                foreach (var text in buttonRect.GetComponentsInChildren<FText>())
                    text.transform.localScale = text.originalScale = textScale;
            }

            // navigation
            InternalUtility.CreateMutualLinks(buttonsTable);

            // controls manager
            _controlsManager.inputButtons = _controlsManager.GetComponentsInHierarchy<ChangeInputButton>(3, 3).GetGameObjects().ToArray();
        }
        static internal Getter<KeyCode> UnbindButton
        { get; } = new Getter<KeyCode>();
        static internal Getter<BindingConflictResolution> BindingsConflictResolution
        { get; } = new Getter<BindingConflictResolution>();

        // Privates
        override protected GameObject FindPrefabs()
        => Resources.FindObjectsOfTypeAll<PlayerInputWindowsManager>().TryGetAny(out _controlsManager)
        && _controlsManager.inputButtons.TryGetAny(out var buttonPrefab)
        && buttonPrefab.GetParent().TryNonNull(out _buttonsHolder)
         ? buttonPrefab
         : null;
        override protected void Initialize()
        {
            _settingsByButtonGUID = new Dictionary<string, ModSetting<string>[]>();

            var buttonNav = _buttonPrefab.GetComponent<TButtonNavigation>();
            buttonNav.normalColor = buttonNav.targetImage.color = CUSTOM_BUTTON_COLOR;
            buttonNav.useDefaultNormalColor = false;
            buttonNav.reafirmNeighboors = false;
        }
        static private GameObject _buttonsHolder;
        static private PlayerInputWindowsManager _controlsManager;
        static private Dictionary<string, ModSetting<string>[]> _settingsByButtonGUID;
        static private Vector2Int GetTableCounts(int buttonsCount)
        {
            int sizeX = buttonsCount.Div(MIN_ROWS).RoundUp().ClampMax(MAX_COLUMNS);
            int sizeY = buttonsCount.Div(MAX_COLUMNS).RoundUp().ClampMin(MIN_ROWS);
            return new Vector2Int(sizeX, sizeY);
        }
        static private string GetSettingGUID(int playerID, string name)
        => $"{typeof(CustomControls).Name}_Player{playerID + 1}_{name}";
        static private string GetButtonGUID(string name)
        => $"{typeof(CustomControls).Name}_{name}";

        // Settings
        private const int ORIGINAL_BUTTONS_COUNT = 14;
        private const int MAX_COLUMNS = 3;
        private const int MIN_ROWS = 7;
        static private readonly Vector2 COMPACT_TEXT_SCALE = new Vector2(0.75f, 0.75f);
        static private readonly Vector2Int COMPACT_TEXT_SCALE_THRESHOLD = new Vector2Int(3, 10);
        static private readonly Color CUSTOM_BUTTON_COLOR = new Color(1 / 2f, 1 / 5f, 1 / 10, 1 / 3f);
        static private readonly Vector2 BUTTON_PADDING = new Vector2(2, 1);
        static private readonly Vector2Int TABLE_SIZE = new Vector2Int(296, 126);
        #region VANILLA_BUTTON_NAMES
        static private readonly (string RefName, string InputName)[] VANILLA_BUTTON_NAMES = new[]
        {
            ("interact", "interact"),
            ("aimLock", "aimlock"),
            ("guard", "guard"),
            ("weapon0Input", "sword"),
            ("dash", "dash"),
            ("heal", "heal"),
            ("weapon1Input", "gun"),
            ("pause", "pause"),
            ("reload", "reload"),
            ("map", "map"),
            ("run", "run"),
            ("up", "up"),
            ("left", "left"),
            ("right", "right"),
            ("down", "down"),
        };
        #endregion

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        [HarmonyPatch(typeof(ChangeInputButton), nameof(ChangeInputButton.GetCurrentKey)), HarmonyPostfix]
        static private void ChangeInputButton_GetCurrentKey_Pre(ChangeInputButton __instance, ref KeyCode __result)
        {
            if (_settingsByButtonGUID.IsNullOrEmpty()
            || !_settingsByButtonGUID.TryGetValue(__instance.inputName, out var settings)
            || !settings.TryGetNonNull(__instance.playerNum, out var playerSetting))
                return;

            __result = playerSetting.ToKeyCode();
        }

        [HarmonyPatch(typeof(ChangeInputButton), nameof(ChangeInputButton.InputDetected)), HarmonyPrefix]
        static private bool ChangeInputButton_InputDetected_Pre(ChangeInputButton __instance, KeyCode targetKey)
        {
            // cache
            var inputManager = PseudoSingleton<GlobalInputManager>.instance;
            var playerInput = inputManager.inputData.playersInputList[__instance.playerNum];
            var inputReceiver = playerInput.inputType[(int)playerInput.currentInputType];

            // update
            __instance.currentKey = __instance.GetCurrentKey();
            KeyCode swapKey = BindingsConflictResolution == BindingConflictResolution.Swap
                ? __instance.currentKey : KeyCode.None;
            if (targetKey == UnbindButton)
                targetKey = KeyCode.None;

            __instance.buttonName.text = targetKey.ToString();
            __instance.buttonName.color = Color.grey;
            __instance.buttonName.ApplyText(false, true, "", true);
            inputManager.SetControllerId(__instance.playerNum, playerInput.joystickNumber);

            // swap
            if (targetKey != KeyCode.None
            && BindingsConflictResolution != BindingConflictResolution.Duplicate)
            {
                // vanilla
                foreach (var (RefName, _) in VANILLA_BUTTON_NAMES)
                    if (inputReceiver.TryGetField<KeyCode>(RefName, out var previousKeyCode)
                    && previousKeyCode == targetKey)
                    {
                        inputReceiver.SetField(RefName, swapKey);
                        break;
                    }
                // custom
                foreach (var settings in _settingsByButtonGUID)
                    if (settings.Value.TryGetNonNull(__instance.playerNum, out var playerSetting)
                    && playerSetting.ToKeyCode() == targetKey)
                    {
                        _settingsByButtonGUID[settings.Key][__instance.playerNum].Value = swapKey.ToString();
                        break;
                    }
            }

            // assign
            // vanilla
            foreach (var (RefName, InputName) in VANILLA_BUTTON_NAMES)
                if (InputName == __instance.inputName.ToLower())
                {
                    inputReceiver.SetField(RefName, targetKey);
                    break;
                }
            // custom
            foreach (var setting in _settingsByButtonGUID)
                if (setting.Key == __instance.inputName)
                {
                    _settingsByButtonGUID[setting.Key][__instance.playerNum].Value = targetKey.ToString();
                    break;
                }

            PseudoSingleton<TEventSystem>.instance.SelectObject(__instance.gameObject);
            __instance.myDeviceButton.updateButtonNames();
            return false;
        }

        [HarmonyPatch(typeof(TranslationSystem), nameof(TranslationSystem.FindTerm)), HarmonyPostfix]
        static private void TranslationSystem_FindTerm_Post(TranslationSystem __instance, ref string __result, string element)
        {
            if (_settingsByButtonGUID.IsNullOrEmpty())
                return;

            string typePrefix = typeof(CustomControls).Name + "_";
            if (element.Contains(typePrefix))
                __result = element.Replace(typePrefix, null);
        }

        [HarmonyPatch(typeof(GlobalInputManager), nameof(GlobalInputManager.GetKeyCodeName),
            new[] { typeof(int), typeof(string), typeof(string), typeof(Sprite) },
            new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref }),
            HarmonyPrefix]

        static private void GlobalInputManager_GetKeyCodeName_Pre(GlobalInputManager __instance, int playerNum, string inputName, ref string buttonName, ref Sprite buttonIcon)
        {
            if (_settingsByButtonGUID.IsNullOrEmpty()
            || !_settingsByButtonGUID.TryGetValue(inputName, out var settings)
            || !settings.TryGetNonNull(playerNum, out var playerSetting))
                return;

            buttonName = playerSetting;
        }

        [HarmonyPatch(typeof(DetectDeviceWindow), nameof(DetectDeviceWindow.PressedKey)), HarmonyPrefix]
        static private void DetectDeviceWindow_PressedKey_Pre(DetectDeviceWindow __instance, ref KeyCode targetKey)
        {
            if (targetKey == UnbindButton)
                DetectDeviceWindow.useInputFilter = false;
        }

        [HarmonyPatch(typeof(ChangeInputButton), nameof(ChangeInputButton.OnEnable)), HarmonyPostfix]
        static private void ChangeInputButton_OnEnable_Post(ChangeInputButton __instance)
        {
            if (_settingsByButtonGUID.IsNullOrEmpty()
            || !_settingsByButtonGUID.TryGetValue(__instance.inputName, out var settings))
                return;

            __instance.gameObject.SetActive(settings[__instance.playerNum] != null);
        }

        [HarmonyPatch(typeof(PlayerInputWindowsManager), nameof(PlayerInputWindowsManager.AdjustToDevice)), HarmonyPrefix]
        static private bool PlayerInputWindowsManager_AdjustToDevice_Post(PlayerInputWindowsManager __instance, int playerNum)
        {
            if (_settingsByButtonGUID.IsNullOrEmpty())
                return true;

            var inputData = PseudoSingleton<GlobalInputManager>.instance.inputData;
            bool isUsingGamepad = inputData.GetInputProfile(playerNum).myInputType != InputType.Keyboard;

            _buttonsHolder.GetChildComponent<AutoAimButton>().UpdateLabel();
            _buttonsHolder.GetChildComponent<AnalogMovementButton>().UpdateLabel();
            _buttonsHolder.GetChildComponent<RunToggleButton>().UpdateLabel();

            __instance.inputButtons[10].gameObject.SetActive(isUsingGamepad);
            __instance.autoAimButton.gameObject.SetActive(isUsingGamepad);
            __instance.analogMovementButton.gameObject.SetActive(isUsingGamepad);

            __instance.deviceButton.GetComponent<TButtonNavigation>().onRight = inputData.numberOfPlayers == 2 ? __instance.player1Button.gameObject : null;
            return false;
        }
    }
}