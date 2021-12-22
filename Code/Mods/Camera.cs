namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using HarmonyLib;
    using Tools.ModdingCore;
    using Tools.Extensions.Math;

    public class Camera : AMod
    {
        // Section
        override protected string SectionOverride
        => Sections.QOL;
        override protected string Description =>
            "Mods related to the camera and screen" +
            "\n\nExamples:" +
            "\n• Change camera zoom to see more" +
            "\n• Enable co-op screen stretching" +
            "\n• Put an end to player 2's oppression";

        // Settings
        static private ModSetting<int> _zoom;
        static private ModSetting<bool> _controlZoomWithMouseScroll;
        static private ModSetting<int> _aimWeight;
        static private ModSetting<int> _shakeMultiplier;
        static private ModSetting<int> _maxCoopStretch;
        static private ModSetting<int> _coopStretchSpeed;
        static private ModSetting<bool> _prioritizePlayer1;
        static private ModSetting<bool> _teleportPlayer2;
        override protected void Initialize()
        {
            _zoom = CreateSetting(nameof(_zoom), 100, IntRange(50, 400));
            _controlZoomWithMouseScroll = CreateSetting(nameof(_controlZoomWithMouseScroll), false);
            _aimWeight = CreateSetting(nameof(_aimWeight), 100, IntRange(0, 200));
            _shakeMultiplier = CreateSetting(nameof(_shakeMultiplier), 100, IntRange(0, 200));

            _maxCoopStretch = CreateSetting(nameof(_maxCoopStretch), 100, IntRange(100, 200));
            _coopStretchSpeed = CreateSetting(nameof(_coopStretchSpeed), 50, IntRange(1, 100));
            _prioritizePlayer1 = CreateSetting(nameof(_prioritizePlayer1), true);
            _teleportPlayer2 = CreateSetting(nameof(_teleportPlayer2), true);
        }
        override protected void SetFormatting()
        {
            _zoom.Format("Zoom");
            _zoom.Description =
                "How close the camera is to the in-world sprites (doesn't affect UI)" +
                "\nLower values will help you see more of the area, but might trigger some visual glitches, especially in smaller areas" +
                "\n\nUnit: percent of original screen size";
            using (Indent)
                _controlZoomWithMouseScroll.Format("control with mouse scroll");
            _aimWeight.Format("Aim weight");
            _aimWeight.Description =
                "How closely the camera follows your mouse (or right thumbstick)" +
                "\nHigher values will make aiming more dynamic" +
                "\nSet to 0 to always center the camera on the character (or the characters' midpoint, if in co-op)" +
                "\n\nUnit: percent of original follow distance";
            _shakeMultiplier.Format("Shake multiplier");
            _shakeMultiplier.Description =
                "How violently the camera shakes when taking damage, performing heavy attacks, etc." +
                "\nLower values will make combat more readable, but might remove the OOMPH factor from some hefty actions" +
                "\n\nUnit: percent of original screen shake strength";

            _maxCoopStretch.Format("Max co-op stretch");
            _maxCoopStretch.Description =
                "How far will the screen stretch to keep both characters on-screen" +
                "\nStretching the screen too much will trigger visual glitches" +
                $"\n\nUnit: percent of total screen size, accounting for \"{_zoom.Name}\" setting";
            using (Indent)
            {
                _coopStretchSpeed.Format("speed", _maxCoopStretch, v => v > 100);
                _coopStretchSpeed.Description =
                    "How quickly the screen stretches catch up with characters" +
                    "\nLower values will make the stretching smoother, but possibly too slow to keep both characters on-screen" +
                    "\n\nUnit: arbitrary exponential-like scale";
            }
            _prioritizePlayer1.Format("Proritize player 1 on-screen");
            _prioritizePlayer1.Description =
                "Makes sure player 1's character is always on-screen and with a decent view range, even if it means pushing player 2 off-screen" +
                "\n\nThis is the default in-game behaviour, and it's pretty damn disgusting, so disable it if you value your co-opartner at all";
            _teleportPlayer2.Format("Teleport player 2 off-screen");
            _teleportPlayer2.Description =
                "When player 2's character goes off-screen, they get instantly teleported to player 1" +
                "\n\nYet another archaic mechanic to treat player 2 as a baby at best, and a vegetable at worst";
        }
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(SettingsPreset.Vheos_UI):
                    ForceApply();
                    _zoom.Value = 100;
                    _controlZoomWithMouseScroll.Value = false;
                    _aimWeight.Value = 50;
                    _shakeMultiplier.Value = 75;

                    _maxCoopStretch.Value = 200;
                    _coopStretchSpeed.Value = 50;
                    _prioritizePlayer1.Value = false;
                    _teleportPlayer2.Value = false;
                    break;
            }
        }

        // Privates
        private const float ORIGINAL_ORTOGRAPHIC_SIZE = 9f;
        static private float _currentCameraZoom = 1f;

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Zoom & stretch
        [HarmonyPatch(typeof(CameraSystem), nameof(CameraSystem.Update)), HarmonyPostfix]
        static private void CameraSystem_Update_Post(CameraSystem __instance)
        {
            if (PseudoSingleton<LevelController>.instance.inGameCutsceneScene)
                return;

            if (_controlZoomWithMouseScroll
            && Input.mouseScrollDelta.y != 0)
                _zoom.Value += (Input.mouseScrollDelta.y * 10 * _zoom / 100f).RoundTowardsZero();

            float totalZoom = _zoom / 100f;
            Vector2 cameraSize = 2 * ORIGINAL_ORTOGRAPHIC_SIZE / totalZoom * new Vector2(__instance.cameraView.aspect, 1);
            float maxStretch = _maxCoopStretch / 100f;
            if (maxStretch > 1f && PlayerInfo.NumberOfAlivePlayers() > 1)
            {
                List<PlayerInfo> players = PseudoSingleton<PlayersManager>.instance.players;
                Vector2 Player1Pos = players[0].myCharacter.myAnimations.myAnimator.myTransform.position;
                Vector2 Player2Pos = players[1].myCharacter.myAnimations.myAnimator.myTransform.position;

                Vector2 offset = Player1Pos.OffsetTo(Player2Pos).Abs();
                float unitDistance = offset.Div(cameraSize).MaxComp();
                float stretchZoom = unitDistance.MapClamped(1 / 3f, maxStretch, 1f, maxStretch).Inv();
                totalZoom *= stretchZoom;
            }

            _currentCameraZoom.SetLerp(totalZoom, _coopStretchSpeed.Value.Map(1, 100, 0.01f, 0.20f));
            __instance.cameraView.orthographicSize = ORIGINAL_ORTOGRAPHIC_SIZE / _currentCameraZoom;
            __instance.UICamera.orthographicSize = ORIGINAL_ORTOGRAPHIC_SIZE / _currentCameraZoom;
            __instance.cameraSizeX = cameraSize.x;
            __instance.cameraSizeY = cameraSize.y;
        }

        [HarmonyPatch(typeof(WorldLightController), nameof(WorldLightController.OnEnable)), HarmonyPostfix]
        static private void WorldLightController_OnEnable_Post(WorldLightController __instance)
        => __instance.transform.localScale *= 2f;

        // Aim weight
        [HarmonyPatch(typeof(CameraSystem), nameof(CameraSystem.CursorPositions)), HarmonyPostfix]
        static private void CameraSystem_CursorPositions_Post(CameraSystem __instance, ref Vector3 __result)
        => __result *= _aimWeight / 100f;

        // Shake
        [HarmonyPatch(typeof(CameraSystem), nameof(CameraSystem.cameraShake)), HarmonyPrefix]
        static private void CameraSystem_cameraShake_Post(CameraSystem __instance, ref float strenght)
        => strenght *= _shakeMultiplier / 100f;

        // Max distance from main player
        [HarmonyPatch(typeof(CameraSystem), nameof(CameraSystem.GetTargetsAveragePosition)), HarmonyPrefix]
        static private bool CameraSystem_GetTargetsAveragePosition_Pre(CameraSystem __instance, ref Vector3 __result)
        {
            if (_prioritizePlayer1)
                return true;

            if (__instance.targetsList.Count == 0)
                __result = __instance.myPosition;

            Vector2 r = Vector2.zero;
            foreach (var target in __instance.targetsList)
                if (target != null && target.gameObject.activeInHierarchy)
                    r += target.position.XY();
            r /= __instance.targetsList.Count;
            __result = r.Append(-15f);
            return false;
        }

        // Teleport player 2
        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.TeleportToPlayer1)), HarmonyPrefix]
        static private bool BasicCharacterController_TeleportToPlayer1_Pre(BasicCharacterController __instance)
        => _teleportPlayer2;
    }
}