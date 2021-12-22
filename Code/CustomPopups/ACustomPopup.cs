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
        abstract protected GameObject FindPrefabs();
        virtual protected void Initialize()
        { }
        virtual protected string ButtonPrefabName
        => $"{typeof(T).Name}Button";

        // Initializers
        internal ACustomPopup()
        {
            Harmony.CreateAndPatchAll(typeof(ACustomPopup<T>));
            Harmony.CreateAndPatchAll(typeof(T));
            _this = this as T;
        }

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        [HarmonyPatch(typeof(TitleScreenScene), nameof(TitleScreenScene.Start)), HarmonyPostfix]
        static private void TitleScreenScene_Start_Post(TitleScreenScene __instance)
        {
            _buttonPrefab = _this.FindPrefabs();
            if (_buttonPrefab == null)
            {
                Log.Debug($"Failed to fully initialize {typeof(T).Name}!");
                return;
            }

            GameObject.DontDestroyOnLoad(_buttonPrefab.GetRootAncestor());

            _buttonPrefab = GameObject.Instantiate(_buttonPrefab);
            _buttonPrefab.name = _this.ButtonPrefabName;
            GameObject.DontDestroyOnLoad(_buttonPrefab);

            IsFullyInitialized = true;
            _this.Initialize();
        }
    }
}