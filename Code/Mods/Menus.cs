namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using HarmonyLib;
    using Tools.ModdingCore;
    using Tools.Extensions.Math;
    using Tools.Extensions.Collections;
    using System.Collections;

    public class Menus : AMod, IDelayedInit
    {
        // Section
        override protected string SectionOverride
        => Sections.QOL;
        override protected string Description =>
            "Mods related to in-game menus" +
            "\n\nExamples:" +
            "\n• Display all save slots on one page" +
            "\n• Allow unbinding/duplicate controls";

        // Settings
        static private ModSetting<bool> _alternateLoadMenu;
        static private ModSetting<string> _undbindButton;
        static private ModSetting<BindingConflictResolution> _bindigsConflictResolution;
        override protected void Initialize()
        {
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
        override protected void SetFormatting()
        {
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
        override protected void LoadPreset(string presetName)
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
    }
}