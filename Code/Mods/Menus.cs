namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using HarmonyLib;
    using Tools.ModdingCore;
    using Tools.Extensions.UnityObjects;
    using Tools.Extensions.Math;
    using Vheos.Tools.Extensions.General;
    using Vheos.Tools.Extensions.Collections;
    using UnityEngine.UI;

    public class Menus : AMod, IDelayedInit
    {
        // Section
        override protected string SectionOverride
        => Sections.QOL;
        override protected string Description =>
            "Mods related to in-game menus" +
            "\n\nExamples:" +
            "\n• Set hotkeys for weapon sets" +
            "\n• Allow unbinding/duplicate controls";

        // Settings
        static private ModSetting<int> _saveSlots;
        static private Dictionary<int, LoadoutSettings> _loadoutSettingsByPlayerID;
        static private ModSetting<string> _undbindButton;
        static private ModSetting<BindingConflictResolution> _bindigsConflictResolution;
        override protected void Initialize()
        {
            _saveSlots = CreateSetting(nameof(_saveSlots), 3, IntRange(3, 10));       

            _loadoutSettingsByPlayerID = new Dictionary<int, LoadoutSettings>();
            for (int playerID = 0; playerID < 2; playerID++)
                _loadoutSettingsByPlayerID[playerID] = new LoadoutSettings(this, playerID);  

            _undbindButton = CreateSetting(nameof(_undbindButton), "Delete");    
            _bindigsConflictResolution = CreateSetting(nameof(_bindigsConflictResolution), BindingConflictResolution.Swap);

            // popup config
            _saveSlots.AddEvent(() => CustomSaves.SetSaveSlotsCount(_saveSlots));
            CustomControls.UpdateButtonsTable();
            CustomControls.UnbindButton.Set(() => _undbindButton.ToKeyCode());
            CustomControls.BindingsConflictResolution.Set(() => _bindigsConflictResolution);
        }
        override protected void SetFormatting()
        {
            _saveSlots.Format("Save slots");
            _saveSlots.Description =
                "How many save slots you'd like" +
                "\n(required game restart to take effect)";
            CreateHeader("Loadouts").Description =
                "Set hotkeys to quickly switch between user-defined sets of weapons" +
                "\nHotkeys can be configured in the in-game \"Controls\" menu:" +
                "\n• Switch Load. - switches to next loadout (loops back)" +
                "\n• Loadout 1~4 - switches to the exact loadout" +
                "\n(requires game restart to take effect)";
            using (Indent)
                foreach (var settings in _loadoutSettingsByPlayerID)
                    settings.Value.Format();
            _undbindButton.Format("\"Unbind\" button");
            _undbindButton.Description =
                "Press this when assigning a new button in the controls menu to unbind the button" +
                "\n\nvalue type: upper-case UnityEngine.KeyCode enum" +
                "\n(https://docs.unity3d.com/ScriptReference/KeyCode.html)";
            _bindigsConflictResolution.Format("Bindings conflict resolution");
            _bindigsConflictResolution.Description =
                "What should happen when you try to assign a button that's already bound:" +
                $"\n• {BindingConflictResolution.Swap} - the two conflicting buttons will swap places" +
                $"\n• {BindingConflictResolution.Unbind} - the other button binding will be removed" +
                $"\n• {BindingConflictResolution.Duplicate} - allow for one button to be bound to many actions";
        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(SettingsPreset.Vheos_HardMode):
                    ForceApply();
                    _loadoutSettingsByPlayerID[0]._count.Value = 2;
                    _loadoutSettingsByPlayerID[1]._count.Value = 2;
                    _undbindButton.Value = KeyCode.Delete.ToString();
                    _bindigsConflictResolution.Value = BindingConflictResolution.Duplicate;
                    break;
            }
        }
        public void OnUpdate()
        {

        }

        // Defines
        #region LoadoutSettings
        private class LoadoutSettings : PerPlayerSettings<Menus>
        {
            // Settings
            internal readonly ModSetting<int> _count;
            internal readonly ModSetting<string> _switch;
            internal readonly Loadout[] _loadouts;
            internal LoadoutSettings(Menus mod, int playerID) : base(mod, playerID)
            {
                _count = _mod.CreateSetting(PlayerPrefix + "Count", 1, _mod.IntRange(1, 4));
                _cachedCount = _count;
                if (!IsEnabled)
                    return;

                _switch = CustomControls.AddControlsButton(playerID, $"Switch Load.");
                _loadouts = new Loadout[_count];
                for (int i = 0; i < _loadouts.Length; i++)
                {
                    _loadouts[i] = new Loadout(i);
                    _loadouts[i].Button = CustomControls.AddControlsButton(playerID, $"Loadout {i + 1}");
                    _loadouts[i].Slots[0] = _mod.CreateSetting(PlayerPrefix + _loadouts[i].Prefix + "Slot1", NOTHING_WEAPON_NAME);
                    _loadouts[i].Slots[1] = _mod.CreateSetting(PlayerPrefix + _loadouts[i].Prefix + "Slot2", NOTHING_WEAPON_NAME);
                }
            }
            public override void Format()
            {
                _count.Format($"Player {_playerID + 1}");
                _count.Description =
                    "How many different weapon loadouts you'd like to use" +
                    "\nSet to 1 to disable this feature" +
                    "\n(requires game restart to take effect)";
            }

            // Publics
            internal void DetectInput()
            {
                if (!IsEnabled)
                    return;

                if (ButtonSystem.GetKeyDown(_switch.ToKeyCode()))
                    TrySwitchTo(+1);
                else
                    foreach (var loadout in _loadouts)
                        if (ButtonSystem.GetKeyDown(loadout.Button.ToKeyCode()))
                        {
                            SwitchTo(loadout);
                            return;
                        }
            }
            internal void UpdateCurrentSlot(int slotID, string weapon)
            {
                if (!IsEnabled
                || !VerifyCurrentLoadout())
                    return;

                _currentLoadout.Slots[slotID].Value = weapon;
            }
            internal bool IsEnabled
            => _cachedCount > 1;

            // Privates
            private const string NOTHING_WEAPON_NAME = "Null";
            private readonly int _cachedCount;
            private Loadout _currentLoadout;
            private void TrySwitchTo(int offset)
            {
                if (!VerifyCurrentLoadout())
                    return;

                int targetID = _currentLoadout.ID.Add(offset).PosMod(_count);
                SwitchTo(_loadouts[targetID]);
            }
            private void SwitchTo(Loadout loadout)
            {
                if (_currentLoadout == loadout)
                    return;

                _currentLoadout = loadout;
                for (int i = 1; i >= 0; i--)
                {
                    ModSetting<string> targetWeapon = _currentLoadout.Slots[i];
                    if (PseudoSingleton<Helpers>.instance.PlayerHaveItem(targetWeapon) <= 0)
                        targetWeapon.Value = NOTHING_WEAPON_NAME;
                    PseudoSingleton<Helpers>.instance.EquipWeapon(targetWeapon, _playerID, i);
                }

                if (PlayerEquipmentAndLevelScreen.instance != null)
                    PlayerEquipmentAndLevelScreen.instance.OnEnable();

                AudioController.Play(PseudoSingleton<GlobalGameManager>.instance.gameSounds.playerDeathSheen, 1f, 1f);
            }
            private bool VerifyCurrentLoadout()
            {
                // Current
                if (_currentLoadout != null)
                    return true;

                // Find matching
                var globals = PseudoSingleton<GlobalGameData>.instance;
                string[] playerSlots = globals.currentData.playerDataSlots[globals.loadedSlot].playersEquipData[_playerID].weapons;
                foreach (var loadout in _loadouts)
                    if (loadout.HasExactSlots(playerSlots))
                    {
                        _currentLoadout = loadout;
                        return true;
                    }

                // Get first
                if (_loadouts.IsNotEmpty())
                {
                    _currentLoadout = _loadouts[0];
                    return true;
                }

                // Fail
                return false;
            }

            // Defines
            internal class Loadout
            {
                // Publics
                public int ID;
                public ModSetting<string> Button;
                public ModSetting<string>[] Slots;
                public string Prefix
                => $"Loadout{ID + 1}_";
                public bool HasExactSlots(string[] slots)
                {
                    for (int i = 0; i < 2; i++)
                        if (Slots[i] != slots[i])
                            return false;
                    return true;
                }

                // Initializers
                public Loadout(int id)
                {
                    ID = id;
                    Slots = new ModSetting<string>[2];
                }
            }
        }
        #endregion

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Loadouts
        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.GetInputs)), HarmonyPostfix]
        static private void BasicCharacterController_GetInputs_Post(BasicCharacterController __instance)
        => _loadoutSettingsByPlayerID[__instance.myInfo.playerNum].DetectInput();

        [HarmonyPatch(typeof(PauseMenuPopup), nameof(PauseMenuPopup.Update)), HarmonyPostfix]
        static private void PauseMenuPopup_Update_Post(PauseMenuPopup __instance)
        => _loadoutSettingsByPlayerID[TButtonNavigation.myPlayer].DetectInput();

        [HarmonyPatch(typeof(Helpers), nameof(Helpers.EquipWeapon)), HarmonyPrefix]
        static private void Helpers_EquipWeapon_Pre(Helpers __instance, string weaponName, int playerNum, int weaponSlot)
        => _loadoutSettingsByPlayerID[playerNum].UpdateCurrentSlot(weaponSlot, weaponName);
    }
}