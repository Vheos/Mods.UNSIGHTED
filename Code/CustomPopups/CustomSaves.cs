namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using HarmonyLib;
    using Mods.Core;
    using Tools.Extensions.UnityObjects;
    using Tools.Extensions.Math;
    using Tools.Extensions.Collections;
    using Tools.UtilityN;

    internal class CustomSaves : ACustomPopup<CustomSaves>
    {
        // Publics
        static internal void SetSaveSlotsCount(int count)
        {
            // remove old buttons
            foreach (var oldSlotButton in _menuPrefab.saveSlotButtons)
            {
                oldSlotButton.gameObject.SetActive(false);
                oldSlotButton.copyButton.SetActive(false);
                oldSlotButton.myEraseButton.SetActive(false);
            }

            // create and assign new buttons
            _menuPrefab.saveSlotButtons = new SaveSlotButton[count];
            var buttonsTable = new[] { new List<RectTransform>(), new List<RectTransform>() };
            var globalGameData = PseudoSingleton<GlobalGameData>.instance;
            for (int i = 0; i < _menuPrefab.saveSlotButtons.Length; i++)
            {
                var newButton = GameObject.Instantiate(_buttonPrefab).GetComponent<SaveSlotButton>();
                newButton.BecomeChildOf(_menuPrefab);
                _menuPrefab.saveSlotButtons[i] = newButton;
                buttonsTable[i % 2].Add(newButton.Rect());

                // try initialize save slot
                foreach (var gameType in Utility.GetEnumValues<GameType>())
                {
                    int slotID = GetSaveSlotID(gameType, i);
                    if (globalGameData.currentData.playerDataSlots.TryGet(slotID, out var playerData)
                    && playerData.dataStrings == null)
                        globalGameData.CreateDefaultDataSlot(slotID);
                }
            }

            // format buttons
            for (int ix = 0; ix < buttonsTable.Length; ix++)
            {
                float posX = ix.IsEven() ? SCREEN_RECT.xMin : SCREEN_RECT.xMax;
                int countY = buttonsTable[ix].Count;
                var totalButtonSizeY = BUTTON_SIZE.y * BUTTON_SCALE.y;
                float totalGapSizeY = SCREEN_RECT.height - countY * totalButtonSizeY;
                float gapSizeY = totalGapSizeY.Div(countY - 1);

                for (int iy = 0; iy < countY; iy++)
                {
                    var slotButton = buttonsTable[ix][iy].GetComponent<SaveSlotButton>();
                    float posY = SCREEN_RECT.yMax - iy * (totalButtonSizeY + gapSizeY);
                    buttonsTable[ix][iy].anchoredPosition = new Vector2(posX, posY);
                    if (ix.IsOdd())
                        buttonsTable[ix][iy].localScale *= new Vector2(-1, +1);

                    var newGameText = slotButton.dontHaveSaveStuff.GetChildComponent<FText>();
                    newGameText.originalText = newGameText.text = "New Game";

                    // customize children                   
                    foreach (var childGroup in new[] { slotButton.haveSaveStuff, slotButton.dontHaveSaveStuff })
                        foreach (RectTransform child in childGroup.transform)
                        {
                            child.anchorMin = child.anchorMax = new Vector2(0, 0);
                            child.pivot = 0.5f.ToVector2();

                            if (slotButton.transform.localScale.x < 0)
                                child.localScale *= new Vector2(-1, +1);

                            if (child.TryGetComponent(out FText ftext))
                            {
                                ftext.minTextPosition = ftext.maxTextPosition = null;
                                if (slotButton.transform.localScale.x < 0)
                                {
                                    ftext.lineModel.GetComponent<HorizontalLayoutGroup>().FlipAlignmentHorizontally();
                                    ftext.linesParent.GetComponent<GridLayoutGroup>().FlipAlignmentHorizontally();
                                }
                            }
                        }
                }
            }

            // disable page button
            _menuPrefab.pageText.transform.parent.gameObject.SetActive(false);

            // update navigation
            var returnButton = _menuPrefab.GetComponentInChildren<ClosePopupButton>().gameObject;
            buttonsTable[0].Insert(0, returnButton.Rect());
            buttonsTable[1].Insert(0, null);
            InternalUtility.CreateMutualLinks(buttonsTable);

            // game mode buttons
            _menuPrefab.mainStoryButton.CreateMutualLinkWith(buttonsTable.First().Last().gameObject, AxisDirections.UP);
            _menuPrefab.dungeonButton.GetComponent<TButtonNavigation>().onUp = buttonsTable.First().Last().gameObject;
            _menuPrefab.bossRushButton.CreateMutualLinkWith(buttonsTable.Last().Last().gameObject, AxisDirections.UP);
        }

        // Privetes
        override protected GameObject FindPrefabs()
        => Resources.FindObjectsOfTypeAll<SaveSlotPopup>().TryGetAny(out _menuPrefab)
        && _menuPrefab.saveSlotButtons.TryGetAny(out var buttonPrefab)
         ? buttonPrefab.gameObject
         : null;
        override protected void Initialize()
        {
            // set scale
            var buttonNavigation = _buttonPrefab.GetComponent<TButtonNavigation>();
            buttonNavigation.originalScale = buttonNavigation.transform.localScale = BUTTON_SCALE;

            // customize the button
            RectTransform buttonRect = _buttonPrefab.Rect();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0, 0);
            buttonRect.pivot = new Vector2(0, 0);
            buttonRect.sizeDelta = BUTTON_SIZE;

            // initialize copy/erase buttons  
            var slotButton = _buttonPrefab.GetComponent<SaveSlotButton>();
            GameObject InstantiateAndInitialize(GameObject prefab, string name)
            {
                var newButton = GameObject.Instantiate(prefab);
                newButton.BecomeChildOf(slotButton.haveSaveStuff);
                newButton.name = name;
                
                var ftext = newButton.GetChildComponent<FText>();
                ftext.text = ftext.originalText = name;
                ftext.GetComponent<UITranslateText>().enabled = false;
                ftext.Rect().anchoredPosition = new Vector2(5, 2);

                var buttonNav = newButton.GetComponent<TButtonNavigation>();
                buttonNav.normalColor = buttonNav.normalColor.NewA(1);
                buttonNav.highlightColor = buttonNav.highlightColor.NewA(1);

                var onClick = newButton.GetComponent<SendMessageOnClick>();
                onClick.onClick = null;
                onClick.target = _buttonPrefab.gameObject;
                onClick.message = name == COPY_BUTTON_NAME ? nameof(SaveSlotButton.CopyClicked) : nameof(SaveSlotButton.EraseClicked);

                var onSelect = newButton.GetComponent<SendMessageOnSelect>();
                onSelect.onSelectEvent = null;

                return newButton;
            }
            slotButton.copyButton = InstantiateAndInitialize(slotButton.copyButton, COPY_BUTTON_NAME);
            slotButton.myEraseButton = InstantiateAndInitialize(slotButton.myEraseButton, ERASE_BUTTON_NAME);
        }
        static private SaveSlotPopup _menuPrefab;
        static private int GetSaveSlotID(GameType gameType, int buttonID)
        => (int)gameType * 3
        + buttonID / 3 * 9
        + buttonID % 3;
        static private int GetButtonID(int saveSlotID)
        => saveSlotID / 9 * 3
        + saveSlotID % 9;

        // Settings
        static private readonly Rect SCREEN_RECT = Rect.MinMaxRect(0, 2, 400, 162);
        static private readonly Vector2 BUTTON_SIZE = new Vector2(230, 40);
        static private readonly Vector2 BUTTON_SCALE = new Vector2(0.75f, 0.75f);
        private const int ORIGINAL_BUTTONS_COUNT = 3;
        private const string COPY_BUTTON_NAME = "cpy";
        private const string ERASE_BUTTON_NAME = "del";
        private const string ALMA_SPRITE_NAME = "Alma";
        #region RectTransformOverrides
        static private readonly Dictionary<string, (Vector2 Position, Vector2 Size)> RECT_OVERRIDES_BY_CHILD_NAME = new Dictionary<string, (Vector2, Vector2)>
        {
            [ALMA_SPRITE_NAME] = (new Vector2(25f, 6f), new Vector2(float.NaN, float.NaN)),
            //["Lifebar"] = (new Vector2(92.5f, 36.5f), new Vector2(float.NaN, float.NaN)),
            ["Money"] = (new Vector2(55f, 22.5f), new Vector2(0f, 0f)),
            ["HoursRemain"] = (new Vector2(55f, 11f), new Vector2(0f, 0f)),
            ["Area"] = (new Vector2(210f, 22.5f), new Vector2(0f, 0f)),
            ["PlayTime"] = (new Vector2(210f, 11f), new Vector2(0f, 0f)),
            ["Garden"] = (new Vector2(155f, 40f), new Vector2(float.NaN, float.NaN)),
            ["Museum"] = (new Vector2(170f, 40f), new Vector2(float.NaN, float.NaN)),
            ["Aquarium"] = (new Vector2(185f, 40f), new Vector2(float.NaN, float.NaN)),
            ["Highway"] = (new Vector2(200f, 40f), new Vector2(float.NaN, float.NaN)),
            ["Factory"] = (new Vector2(215f, 40f), new Vector2(float.NaN, float.NaN)),
            ["FairySprite"] = (new Vector2(40f, 40f), new Vector2(float.NaN, float.NaN)),
            ["FinishedGameIcon"] = (new Vector2(110f, 22.5f), new Vector2(15f, 15f)),
            [COPY_BUTTON_NAME] = (new Vector2(100f, 40f), new Vector2(25, 12.5f)),
            [ERASE_BUTTON_NAME] = (new Vector2(130f, 40f), new Vector2(25, 12.5f)),
            ["NewGame"] = (new Vector2(90f, 22.5f), new Vector2(0f, 0f)),
        };
        #endregion

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        [HarmonyPatch(typeof(SaveSlotPopup), nameof(SaveSlotPopup.ChangeGameType), new[] { typeof(int), typeof(bool) }), HarmonyPrefix]
        static private bool SaveSlotPopup_ChangeGameType_Pre(SaveSlotPopup __instance, int targetTypeId, bool resetPage)
        {
            if (__instance.saveSlotButtons.Length <= ORIGINAL_BUTTONS_COUNT)
                return true;

            if (resetPage)
                __instance.currentPage = 0;
            __instance.UpdatePageLabel();

            __instance.currentGameType = (GameType)targetTypeId;
            __instance.mainStoryButton.GetChildComponent<Image>().color = __instance.currentGameType == GameType.MainStory ? Color.white : Color.grey * 0.5f;
            __instance.dungeonButton.GetChildComponent<Image>().color = __instance.currentGameType == GameType.Dungeon ? Color.white : Color.grey * 0.5f;
            __instance.bossRushButton.GetChildComponent<Image>().color = __instance.currentGameType == GameType.BossRush ? Color.white : Color.grey * 0.5f;
            for (int i = 0; i < __instance.saveSlotButtons.Length; i++)
            {
                __instance.saveSlotButtons[i].saveSlot = GetSaveSlotID(__instance.currentGameType, i);
                __instance.saveSlotButtons[i].UpdateInfo();
            }

            return false;
        }

        [HarmonyPatch(typeof(SaveSlotPopup), nameof(SaveSlotPopup.Start)), HarmonyPostfix]
        static private void SaveSlotPopup_Start_Post(SaveSlotPopup __instance)
        {
            if (__instance.saveSlotButtons.Length <= ORIGINAL_BUTTONS_COUNT)
                return;

            for (int i = 0; i < __instance.saveSlotButtons.Length; i++)
            {
                var onSelect = __instance.saveSlotButtons[i].GetComponent<SendMessageOnSelect>();
                int localSlotID = i;
                onSelect.onSelectEvent.RemoveAllListeners();
                onSelect.onSelectEvent.AddListener(() => __instance.ShowDescription(localSlotID));
            }
        }

        [HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.UpdateInfo)), HarmonyPostfix]
        static private void SaveSlotButton_UpdateInfo_Post(SaveSlotButton __instance)
        {
            if (__instance.mySaveSlotPopup.saveSlotButtons.Length <= ORIGINAL_BUTTONS_COUNT)
                return;

            // remove life bar
            __instance.lifeBar.gameObject.SetActive(false);

            // update children
            foreach (var childGroup in new[] { __instance.haveSaveStuff, __instance.dontHaveSaveStuff })
                foreach (RectTransform child in childGroup.transform)
                {
                    if (RECT_OVERRIDES_BY_CHILD_NAME.TryGetValue(child.name, out var overrides))
                    {
                        if (!overrides.Size.AnyNaN())
                            child.sizeDelta = overrides.Size;
                        if (!overrides.Position.AnyNaN())
                            child.anchoredPosition = overrides.Position;
                    }
                    if (child.name == ALMA_SPRITE_NAME)
                    {
                        Vector2 position = child.anchoredPosition;
                        position.y += child.sizeDelta.y / 2f;
                        child.anchoredPosition = position;
                    }
                }
        }

        [HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.AdjustSpritePivot)), HarmonyPrefix]
        static private bool SaveSlotButton_AdjustSpritePivot_Pre(SaveSlotButton __instance)
        => __instance.mySaveSlotPopup.saveSlotButtons.Length <= ORIGINAL_BUTTONS_COUNT;
    }
}