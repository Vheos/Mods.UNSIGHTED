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

    internal class CustomSaves : ACustomPopup<CustomSaves>
    {
        // Publics
        static internal void SetSaveSlotsCount(int count)
        {

        }

        // Privates
        override protected void Initialize()
        { }
        override protected bool TryFindPrefabs()
        {
            if (Resources.FindObjectsOfTypeAll<SaveSlotPopup>().TryGetAny(out var savesMenuPrefab)
            && savesMenuPrefab.saveSlotButtons.TryGetAny(out var saveSlotButtonPrefab))
            {
                _buttonPrefab = saveSlotButtonPrefab.gameObject;
                return true;
            }

            return false;
        }
        static private int GetGlobalSaveSlotID(GameType gameType, int localSaveSlotID)
        => localSaveSlotID < 3
        ? localSaveSlotID + 3 * (int)gameType
        : localSaveSlotID * 3 + (int)gameType;



        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

    }
}