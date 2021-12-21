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

    abstract internal class ACustomPopup<T> where T : ACustomPopup<T>
    {
        // Publics
        static internal bool IsFullyInitialized
        { get; private set; }

        // Privates
        static protected T _this;
        static protected GameObject _buttonPrefab;
        abstract protected bool TryFindPrefabs();
        abstract protected void Initialize();

        // Initializers
        internal ACustomPopup()
        {
            Harmony.CreateAndPatchAll(typeof(ACustomPopup<T>));
            Harmony.CreateAndPatchAll(typeof(T));
            _this = this as T;
            _this.Initialize();
        }

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        [HarmonyPatch(typeof(TitleScreenScene), nameof(TitleScreenScene.Start)), HarmonyPostfix]
        static private void TitleScreenScene_Start_Post(TitleScreenScene __instance)
        {
            if (!_this.TryFindPrefabs())
            {
                Log.Debug($"Failed to fully initialize {typeof(T).Name}!");
                return;
            }

            GameObject.DontDestroyOnLoad(_buttonPrefab.GetRootAncestor());
            IsFullyInitialized = true;
        }
    }
}