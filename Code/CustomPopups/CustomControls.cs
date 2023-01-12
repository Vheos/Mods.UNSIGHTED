
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vheos.Helpers.Reflection;
using Vheos.Mods.Core;

namespace Vheos.Mods.UNSIGHTED;
internal class CustomControls : ACustomPopup<CustomControls>
{
    // Publics
    internal static ModSetting<string> AddControlsButton(int playerID, string name)
    {
        // setting
        string settingGUID = GetSettingGUID(playerID, name);
        var setting = new ModSetting<string>("", settingGUID, KeyCode.None.ToString())
        {
            IsVisible = false
        };

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
    internal static void UpdateButtonsTable()
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
    internal static Getter<KeyCode> UnbindButton
    { get; } = new Getter<KeyCode>();
    internal static Getter<BindingConflictResolution> BindingsConflictResolution
    { get; } = new Getter<BindingConflictResolution>();

    // Privates
    protected override GameObject FindPrefabs()
    => Resources.FindObjectsOfTypeAll<PlayerInputWindowsManager>().TryGetAny(out _controlsManager)
    && _controlsManager.inputButtons.TryGetAny(out var buttonPrefab)
    && buttonPrefab.GetParent().TryNonNull(out _buttonsHolder)
     ? buttonPrefab
     : null;
    protected override void Initialize()
    {
        _settingsByButtonGUID = new Dictionary<string, ModSetting<string>[]>();

        var buttonNav = _buttonPrefab.GetComponent<TButtonNavigation>();
        buttonNav.normalColor = buttonNav.targetImage.color = CUSTOM_BUTTON_COLOR;
        buttonNav.useDefaultNormalColor = false;
        buttonNav.reafirmNeighboors = false;
    }
    private static GameObject _buttonsHolder;
    private static PlayerInputWindowsManager _controlsManager;
    private static Dictionary<string, ModSetting<string>[]> _settingsByButtonGUID;
    private static Vector2Int GetTableCounts(int buttonsCount)
    {
        int sizeX = buttonsCount.ToFloat().Div(MIN_ROWS).RoundUp().ClampMax(MAX_COLUMNS);
        int sizeY = buttonsCount.ToFloat().Div(MAX_COLUMNS).RoundUp().ClampMin(MIN_ROWS);
        return new Vector2Int(sizeX, sizeY);
    }
    private static string GetSettingGUID(int playerID, string name)
    => $"{typeof(CustomControls).Name}_Player{playerID + 1}_{name}";
    private static string GetButtonGUID(string name)
    => $"{typeof(CustomControls).Name}_{name}";

    // Settings
    private const int ORIGINAL_BUTTONS_COUNT = 14;
    private const int MAX_COLUMNS = 3;
    private const int MIN_ROWS = 7;
    private static readonly Vector2 COMPACT_TEXT_SCALE = new(0.75f, 0.75f);
    private static readonly Vector2Int COMPACT_TEXT_SCALE_THRESHOLD = new(3, 10);
    private static readonly Color CUSTOM_BUTTON_COLOR = new(1 / 2f, 1 / 5f, 1 / 10, 1 / 3f);
    private static readonly Vector2 BUTTON_PADDING = new(2, 1);
    private static readonly Vector2Int TABLE_SIZE = new(296, 126);
    #region VANILLA_BUTTON_NAMES
    private static readonly (string RefName, string InputName)[] VANILLA_BUTTON_NAMES = new[]
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
        ("loadout", "loadout"),
    };
    #endregion

    // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

    [HarmonyPatch(typeof(ChangeInputButton), nameof(ChangeInputButton.GetCurrentKey)), HarmonyPostfix]
    private static void ChangeInputButton_GetCurrentKey_Pre(ChangeInputButton __instance, ref KeyCode __result)
    {
        if (_settingsByButtonGUID.IsNullOrEmpty()
        || !_settingsByButtonGUID.TryGetValue(__instance.inputName, out var settings)
        || !settings.TryGetNonNull(__instance.playerNum, out var playerSetting))
            return;

        __result = playerSetting.ToKeyCode();
    }

    [HarmonyPatch(typeof(ChangeInputButton), nameof(ChangeInputButton.InputDetected)), HarmonyPrefix]
    private static bool ChangeInputButton_InputDetected_Pre(ChangeInputButton __instance, KeyCode targetKey)
    {
        // cache
        var inputManager = GlobalInputManager.instance;
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

        TEventSystem.instance.SelectObject(__instance.gameObject);
        __instance.myDeviceButton.updateButtonNames();
        return false;
    }

    [HarmonyPatch(typeof(TranslationSystem), nameof(TranslationSystem.FindTerm)), HarmonyPostfix]
    private static void TranslationSystem_FindTerm_Post(TranslationSystem __instance, ref string __result, string element)
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

    private static void GlobalInputManager_GetKeyCodeName_Pre(GlobalInputManager __instance, int playerNum, string inputName, ref string buttonName, ref Sprite buttonIcon)
    {
        if (_settingsByButtonGUID.IsNullOrEmpty()
        || !_settingsByButtonGUID.TryGetValue(inputName, out var settings)
        || !settings.TryGetNonNull(playerNum, out var playerSetting))
            return;

        buttonName = playerSetting;
    }

    [HarmonyPatch(typeof(DetectDeviceWindow), nameof(DetectDeviceWindow.PressedKey)), HarmonyPrefix]
    private static void DetectDeviceWindow_PressedKey_Pre(DetectDeviceWindow __instance, ref KeyCode targetKey)
    {
        if (targetKey == UnbindButton)
            DetectDeviceWindow.useInputFilter = false;
    }

    [HarmonyPatch(typeof(ChangeInputButton), nameof(ChangeInputButton.OnEnable)), HarmonyPostfix]
    private static void ChangeInputButton_OnEnable_Post(ChangeInputButton __instance)
    {
        if (_settingsByButtonGUID.IsNullOrEmpty()
        || !_settingsByButtonGUID.TryGetValue(__instance.inputName, out var settings))
            return;

        __instance.gameObject.SetActive(settings[__instance.playerNum] != null);
    }

    [HarmonyPatch(typeof(PlayerInputWindowsManager), nameof(PlayerInputWindowsManager.AdjustToDevice)), HarmonyPrefix]
    private static bool PlayerInputWindowsManager_AdjustToDevice_Post(PlayerInputWindowsManager __instance, int playerNum)
    {
        if (_settingsByButtonGUID.IsNullOrEmpty())
            return true;

        var inputData = GlobalInputManager.instance.inputData;
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