namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections.Generic;
    using HarmonyLib;
    using UnityEngine;
    using UnityEngine.PostProcessing;
    using Tools.ModdingCore;
    using Tools.Extensions.Math;

    public class Camera : AMod
    {
        // Settings
        static private ModSetting<float> _zoom;
        static private ModSetting<float> _aimWeight;

        static private ModSetting<float> _stretchMax;
        static private ModSetting<float> _stretchUpdateSpeed;
        static private ModSetting<Vector2> _maxDistanceFromPlayer1;
        static private ModSetting<bool> _dontTeleportPlayer2;

        static private ModSetting<float> _shakeMultiplier;
        static private ModSetting<float> _exposureLerpTarget;
        static private ModSetting<float> _exposureLerpAlpha;

        override protected void Initialize()
        {
            _zoom = CreateSetting(nameof(_zoom), 1f, FloatRange(0.5f, 2f));
            _aimWeight = CreateSetting(nameof(_aimWeight), 1f, FloatRange(0f, 2f));

            _stretchMax = CreateSetting(nameof(_stretchMax), 1f, FloatRange(1f, 2f));
            _stretchUpdateSpeed = CreateSetting(nameof(_stretchUpdateSpeed), 0.1f, FloatRange(0f, 1f));
            _maxDistanceFromPlayer1 = CreateSetting(nameof(_maxDistanceFromPlayer1), MAIN_PLAYER_MAX_CAMERA_DISTANCE);
            _dontTeleportPlayer2 = CreateSetting(nameof(_dontTeleportPlayer2), false);

            _exposureLerpTarget = CreateSetting(nameof(_exposureLerpTarget), 1f, FloatRange(0f, 2f));
            _exposureLerpAlpha = CreateSetting(nameof(_exposureLerpAlpha), 0f, FloatRange(0f, 1f));
            _shakeMultiplier = CreateSetting(nameof(_shakeMultiplier), 1f, FloatRange(0f, 2f));
        }
        override protected void SetFormatting()
        {
            _zoom.Format("Zoom");
            _aimWeight.Format("Aim weight");

            _stretchMax.Format("Max multiplayer stretch");
            _stretchUpdateSpeed.Format("Update speed");
            _maxDistanceFromPlayer1.Format("Max camera distance from player 1");
            _dontTeleportPlayer2.Format("Don't teleport player 2");

            _shakeMultiplier.Format("Shake multiplier");
            _exposureLerpTarget.Format("Exposure lerp target");
            _exposureLerpAlpha.Format("Exposure lerp alpha");

        }

        // Privates
        private const float ORIGINAL_ORTOGRAPHIC_SIZE = 9f;
        static private readonly Vector2 MAIN_PLAYER_MAX_CAMERA_DISTANCE = new Vector2(7f, 3f);
        static private float _currentCameraZoom = 1f;


        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Zoom & stretch
        [HarmonyPatch(typeof(CameraSystem), nameof(CameraSystem.Update)), HarmonyPostfix]
        static private void CameraSystem_Update_Post(CameraSystem __instance)
        {
            if (PseudoSingleton<LevelController>.instance.inGameCutsceneScene)
                return;

            float totalZoom = _zoom;
            Vector2 cameraSize = 2 * ORIGINAL_ORTOGRAPHIC_SIZE / _zoom * new Vector2(__instance.cameraView.aspect, 1);

            if (_stretchMax > 1f && PlayerInfo.NumberOfAlivePlayers() > 1)
            {
                List<PlayerInfo> players = PseudoSingleton<PlayersManager>.instance.players;
                Vector2 Player1Pos = players[0].myCharacter.myAnimations.myAnimator.myTransform.position;
                Vector2 Player2Pos = players[1].myCharacter.myAnimations.myAnimator.myTransform.position;

                Vector2 offset = Player1Pos.OffsetTo(Player2Pos).Abs();
                float unitDistance = offset.Div(cameraSize).MaxComp();
                float stretchZoom = unitDistance.MapClamped(1/3f, _stretchMax, 1f, _stretchMax).Inv();
                totalZoom *= stretchZoom;
            }

            _currentCameraZoom.SetLerp(totalZoom, _stretchUpdateSpeed);
            __instance.cameraView.orthographicSize = ORIGINAL_ORTOGRAPHIC_SIZE / _currentCameraZoom;
            __instance.UICamera.orthographicSize = ORIGINAL_ORTOGRAPHIC_SIZE / _currentCameraZoom;
            __instance.cameraSizeX = cameraSize.x / _currentCameraZoom;
            __instance.cameraSizeY = cameraSize.y / _currentCameraZoom;
        }

        [HarmonyPatch(typeof(WorldLightController), nameof(WorldLightController.OnEnable)), HarmonyPostfix]
        static private void WorldLightController_OnEnable_Post(WorldLightController __instance)
        => __instance.transform.localScale *= 2f;

        // Aim weight
        [HarmonyPatch(typeof(CameraSystem), nameof(CameraSystem.CursorPositions)), HarmonyPostfix]
        static private void CameraSystem_CursorPositions_Post(CameraSystem __instance, ref Vector3 __result)
        => __result *= _aimWeight;

        // Max distance from main player
        [HarmonyPatch(typeof(CameraSystem), nameof(CameraSystem.GetTargetsAveragePosition)), HarmonyPrefix]
        static private bool CameraSystem_GetTargetsAveragePosition_Pre(CameraSystem __instance, ref Vector3 __result)
        {
            if (__instance.targetsList.Count == 0)
                __result = __instance.myPosition;

            Vector2 r = Vector2.zero;
            foreach (var target in __instance.targetsList)
                if (target != null && target.gameObject.activeInHierarchy)
                    r += target.position.XY();
            r /= __instance.targetsList.Count;

            Vector2 mainPlayerPos = PseudoSingleton<PlayersManager>.instance.GetFirstAlivePlayer().myCharacter.myAnimations.myAnimator.transform.position;
            r = r.Clamp(mainPlayerPos - _maxDistanceFromPlayer1, mainPlayerPos + _maxDistanceFromPlayer1);
            __result = r.Append(-15f);
            return false;
        }

        // Teleport when outside camera
        [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.TeleportToPlayer1)), HarmonyPrefix]
        static private bool BasicCharacterController_TeleportToPlayer1_Pre(BasicCharacterController __instance)
        => !_dontTeleportPlayer2;

        // Shake
        [HarmonyPatch(typeof(CameraSystem), nameof(CameraSystem.cameraShake)), HarmonyPrefix]
        static private void CameraSystem_cameraShake_Post(CameraSystem __instance, ref float strenght)
        => strenght *= _shakeMultiplier;

        // Exposure
        [HarmonyPatch(typeof(Lists), nameof(Lists.Start)), HarmonyPostfix]
        static private void Lists_Start_Post(Lists __instance)
        {
            PostProcessingProfile[] GetCameraProfiles(AreaDescription areaDescription)
            => new[]
            {
                areaDescription.morningCameraProfile,
                areaDescription.dayCameraProfile,
                areaDescription.eveningCameraProfile,
                areaDescription.nightCameraProfile
            };

            var processedProfiles = new HashSet<PostProcessingProfile>();
            foreach (var areaDescription in __instance.areaDatabase.areas)
                foreach (var profile in GetCameraProfiles(areaDescription))
                    if (!processedProfiles.Contains(profile))
                    {
                        var settings = profile.colorGrading.settings;
                        settings.basic.postExposure.SetLerp(_exposureLerpTarget, _exposureLerpAlpha);
                        profile.colorGrading.settings = settings;
                        processedProfiles.Add(profile);
                    }
        }
    }
}