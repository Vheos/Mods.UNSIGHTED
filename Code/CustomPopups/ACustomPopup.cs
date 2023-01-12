
using HarmonyLib;
using UnityEngine;
using Vheos.Mods.Core;

namespace Vheos.Mods.UNSIGHTED;
internal abstract class ACustomPopup<T> where T : ACustomPopup<T>
{
    // Publics
    internal static bool IsFullyInitialized
    { get; private set; }

    // Privates
    protected static T _this;
    protected static GameObject _buttonPrefab;
    protected abstract GameObject FindPrefabs();
    protected virtual void Initialize()
    { }
    protected virtual string ButtonPrefabName
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
    private static void TitleScreenScene_Start_Post(TitleScreenScene __instance)
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