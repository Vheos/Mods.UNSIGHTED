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
    using UnityEngine.UI;

    internal class CustomSaves : ACustomPopup<CustomSaves>
    {
        // Publics
        static internal void SetSaveSlotsCount(int count)
        {


        }

        // Privates
        override protected GameObject FindPrefabs()
        => Resources.FindObjectsOfTypeAll<SaveSlotPopup>().TryGetAny(out _menuPrefab)
        && _menuPrefab.saveSlotButtons.TryGetAny(out var buttonPrefab)
         ? buttonPrefab.gameObject
         : null;
        override protected void DelayedInitialize()
        {
            base.DelayedInitialize();

            // set scale
            var buttonNavigation = _buttonPrefab.GetComponent<TButtonNavigation>();
            buttonNavigation.originalScale = buttonNavigation.transform.localScale = new Vector2(0.75f, 0.75f);

            // customize the button
            RectTransform buttonRect = _buttonPrefab.Rect();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0, 0);
            buttonRect.pivot = new Vector2(0, 0.5f);
            buttonRect.sizeDelta = new Vector2(230, 40);

            // initialize copy/erase buttons
            var slotButton = _buttonPrefab.GetComponent<SaveSlotButton>();
            GameObject InstantiateAndInitialize(GameObject prefab, string name)
            {
                var newButton = GameObject.Instantiate(prefab);
                newButton.BecomeChildOf(slotButton.haveSaveStuff);
                newButton.name = name;
                var ftext = newButton.GetComponentInChildren<FText>();
                ftext.text = ftext.originalText = name;
                ftext.ApplyText();

                return newButton;
            }
            slotButton.copyButton = InstantiateAndInitialize(slotButton.copyButton, COPY_BUTTON_NAME);
            slotButton.myEraseButton = InstantiateAndInitialize(slotButton.myEraseButton, ERASE_BUTTON_NAME);

            // remove old buttons
            foreach (var oldSlotButton in _menuPrefab.saveSlotButtons)
            {
                oldSlotButton.copyButton.Destroy();
                oldSlotButton.myEraseButton.Destroy();
            }
            _menuPrefab.saveSlotButtons.DestroyObject();

            // create new buttons
            _menuPrefab.saveSlotButtons = new SaveSlotButton[8];
            for (int i = 0; i < _menuPrefab.saveSlotButtons.Length; i++)
            {
                var newButton = GameObject.Instantiate(_buttonPrefab).GetComponent<SaveSlotButton>();
                newButton.BecomeChildOf(_menuPrefab);
                newButton.Rect().anchoredPosition = new Vector2(0, 177 - i.PosMod(4) * 42);
                if (i >= 4)
                {
                    newButton.Rect().anchoredPosition += new Vector2(400, 0);
                    newButton.transform.localScale *= new Vector2(-1, +1);
                }
                _menuPrefab.saveSlotButtons[i] = newButton;
            }

        }
        static private SaveSlotPopup _menuPrefab;
        static private int GetGlobalSaveSlotID(GameType gameType, int localSaveSlotID)
        => localSaveSlotID < 3
        ? localSaveSlotID + 3 * (int)gameType
        : localSaveSlotID * 3 + (int)gameType;
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
            ["FinishedGameIcon"] = (new Vector2(110f, 20f), new Vector2(15f, 15f)),
            [COPY_BUTTON_NAME] = (new Vector2(77.5f, 42f), new Vector2(45f, 12.5f)),
            [ERASE_BUTTON_NAME] = (new Vector2(122.5f, 42f), new Vector2(45f, 12.5f)),
            ["NewGame"] = (new Vector2(90f, 22.5f), new Vector2(0f, 0f)),
        };
        #endregion
        private const string COPY_BUTTON_NAME = "cpy";
        private const string ERASE_BUTTON_NAME = "del";
        private const string ALMA_SPRITE_NAME = "Alma";

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        [HarmonyPatch(typeof(SaveSlotPopup), nameof(SaveSlotPopup.ChangeGameType)), HarmonyPrefix]
        static private bool SaveSlotPopup_ChangeGameType_Pre(SaveSlotPopup __instance, int targetTypeId)
        {
            __instance.currentGameType = (GameType)targetTypeId;
            __instance.mainStoryButton.GetChildComponent<Image>().color = __instance.currentGameType == GameType.MainStory ? Color.white : Color.grey * 0.5f;
            __instance.dungeonButton.GetChildComponent<Image>().color = __instance.currentGameType == GameType.Dungeon ? Color.white : Color.grey * 0.5f;
            __instance.bossRushButton.GetChildComponent<Image>().color = __instance.currentGameType == GameType.BossRush ? Color.white : Color.grey * 0.5f;
            for (int i = 0; i < __instance.saveSlotButtons.Length; i++)
            {
                __instance.saveSlotButtons[i].saveSlot = GetGlobalSaveSlotID(__instance.currentGameType, i);
                __instance.saveSlotButtons[i].UpdateInfo();
            }
            return false;
        }

        [HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.UpdateInfo)), HarmonyPostfix]
        static private void SaveSlotButton_UpdateInfo_Post(SaveSlotButton __instance)
        {
            // customize each child
            foreach (var childGroup in new[] { __instance.haveSaveStuff, __instance.dontHaveSaveStuff })
                foreach (RectTransform child in childGroup.transform)
                {
                    child.anchorMin = child.anchorMax = new Vector2(0, 0);
                    child.pivot = 0.5f.ToVector2();
                    if (__instance.transform.localScale.x < 0)
                        child.localScale = new Vector2(-1, +1);
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
                    if (child.TryGetComponent(out FText ftext))
                        ftext.minTextPosition = null;
                }

            // remove life bar
            __instance.lifeBar.gameObject.SetActive(false);
        }

        [HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.AdjustSpritePivot)), HarmonyPrefix]
        static private bool SaveSlotButton_AdjustSpritePivot_Pre(SaveSlotButton __instance)
        => false;

    }
}