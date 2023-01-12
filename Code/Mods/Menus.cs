
using HarmonyLib;
using UnityEngine;
using Vheos.Mods.Core;

namespace Vheos.Mods.UNSIGHTED;
public class Menus : AMod, IDelayedInit
{
    // Section
    protected override string SectionOverride
    => Sections.QOL;
    protected override string Description =>
        "Mods related to in-game menus" +
        "\n\nExamples:" +
        "\n• Display all save slots on one page" +
        "\n• Allow unbinding/duplicate controls";

    // Settings
    private static ModSetting<bool> _developerCheats;
    private static ModSetting<bool> _alternateLoadMenu;
    private static ModSetting<string> _undbindButton;
    private static ModSetting<BindingConflictResolution> _bindigsConflictResolution;
    protected override void Initialize()
    {
        _developerCheats = CreateSetting(nameof(_developerCheats), false);
        _alternateLoadMenu = CreateSetting(nameof(_alternateLoadMenu), false);
        _undbindButton = CreateSetting(nameof(_undbindButton), "Delete");
        _bindigsConflictResolution = CreateSetting(nameof(_bindigsConflictResolution), BindingConflictResolution.Swap);

        // popup config
        if (_alternateLoadMenu)
            CustomSaves.SetSaveSlotsCount(8);
        CustomControls.UpdateButtonsTable();
        CustomControls.UnbindButton.Set(() => _undbindButton.ToKeyCode());
        CustomControls.BindingsConflictResolution.Set(() => _bindigsConflictResolution);
    }
    protected override void SetFormatting()
    {
        _developerCheats.Format("Developer cheats");
        _developerCheats.Description =
            "Allows you to access the developer cheats menu" +
            "\nAfter you enable this setting, open your inventory, navigate to the settings menu (the one with \"Controls\" and \"Options\") " +
            "then click the new \"Cheats\" button at the top";
        _alternateLoadMenu.Format("Alternate load menu");
        _alternateLoadMenu.Description =
            "Adds 2 extra save slots and displays all save slots on one page" +
            "\n(requires game restart to take effect)";

        _undbindButton.Format("\"Unbind\" button");
        _undbindButton.Description =
            "Press this when assigning a new button in the controls menu to unbind the button" +
            "\n\nvalue type: case-sensitive UnityEngine.KeyCode enum" +
            "\n(https://docs.unity3d.com/ScriptReference/KeyCode.html)";
        _bindigsConflictResolution.Format("Bindings conflict resolution");
        _bindigsConflictResolution.Description =
            "What happens when you try to assign a button that's already been assigned" +
            $"\n• {BindingConflictResolution.Swap} - the two conflicting buttons will swap places" +
            $"\n• {BindingConflictResolution.Unbind} - the other button binding will be removed" +
            $"\n• {BindingConflictResolution.Duplicate} - allow for one button to be bound to many actions";
    }
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(SettingsPreset.Vheos_UI):
                ForceApply();
                _alternateLoadMenu.Value = true;
                _undbindButton.Value = KeyCode.Delete.ToString();
                _bindigsConflictResolution.Value = BindingConflictResolution.Duplicate;
                break;
        }
    }
    public void OnUpdate()
    {

    }

    // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

    // Cheats menu
    [HarmonyPatch(typeof(EnableOnlyInDebugMode), nameof(EnableOnlyInDebugMode.Start)), HarmonyPrefix]
    private static bool EnableOnlyInDebugMode_Start_Pre(EnableOnlyInDebugMode __instance)
    => !_developerCheats;
}