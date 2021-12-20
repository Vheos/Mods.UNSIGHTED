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
    using Tools.UtilityN;
    using Tools.Extensions.Reflection;
    using Vheos.Tools.Extensions.General;

    public class SFXPlayer : AMod
    {
        // Section
        override protected string SectionOverride
        => Sections.VARIOUS;
        override protected string Description =>
            "Allows you to sound-test all in-game sound effects";
        override protected bool IsAdvanced
        => true;

        // Settings
        static private ModSetting<bool> _sfxPlayerToggle;
        static private Dictionary<SFX, ModSetting<bool>> _togglesBySFX;
        override protected void Initialize()
        {
            _sfxPlayerToggle = CreateSetting(nameof(_sfxPlayerToggle), true);
            _togglesBySFX = new Dictionary<SFX, ModSetting<bool>>();
            foreach (var sfx in Utility.GetEnumValues<SFX>())
            {
                _togglesBySFX[sfx] = CreateSetting("SFXPlayer_" + sfx, false);
                _togglesBySFX[sfx].AddEventSilently(() => PlaySFXFromConfig(sfx));
            }
        }
        override protected void SetFormatting()
        {
            foreach (var toggle in _togglesBySFX)
            {
                toggle.Value.IsAdvanced = true;
                toggle.Value.Format(toggle.Key.ToString().FirstLetterCapitalized().SplitCamelCase());
            }
        }

        // Privates
        static private void PlaySFXFromConfig(SFX sfx)
        {
            _togglesBySFX[sfx].SetSilently(false);
            if (!PseudoSingleton<GlobalGameManager>.instance.TryNonNull(out var gameManager))
                return;

            string internalSFXName = gameManager.gameSounds.GetField<string>(sfx.ToString());
            AudioController.Play(internalSFXName, 1f, 1f);
        }

        // Defines
        private enum SFX
        {
            aquariumBossHeadButt,
            axeSpinAttackBeginSound,
            axeSpinAttackSound,
            bambooCutSound,
            barrierDoorLowering,
            barrierDoorRising,
            bikeSound,
            buddhaMinionAttack,
            buddhaMinionDeathVoice,
            buddhaMinionHitVoice,
            buddhaMinionLand,
            buddhaMinionMagicVoice,
            buddhaMinionPrepareAttack,
            bulletHitWall,
            catAttack,
            catBark,
            catEat,
            catPurr,
            chestClose,
            chestOpen,
            climbingNonVine,
            climbingVine,
            craterTowerCrack,
            craterTowerExplosion,
            craterTowerExplosionAmbiance1,
            craterTowerExplosionAmbiance2,
            craterTowerLaser,
            cuteAnimalDead,
            darkElemental,
            darkElemental2,
            dashSound,
            deerCry,
            deerRunningAway,
            defaultBlasterSound,
            defendedBulletSound,
            defendedSwordSound,
            dobermanBark,
            doctorsGun,
            dogCage,
            dogCryLoop,
            dogEat,
            dogGrowl,
            dogHowling,
            dogJump,
            dogLanding,
            dogSad,
            doorRumbleSound,
            doorRumbleSoundFinish,
            doveFlying,
            droneSound,
            dungeonEntranceSoundEffect,
            dungeonRaidTeleport,
            dustPing,
            eagleCrash,
            eagleFall,
            eagleRide,
            elevatorBell,
            elevatorDoorClose,
            elevatorDoorOpen,
            elevatorMovement,
            elevatorMovementEnd,
            enemyAppearSound,
            enemyBurn,
            enemyDeathHit,
            enemyElectrified,
            enemyFrozen,
            enemyFrozenBreak,
            enemyHitByBullet,
            enemyHitBySword,
            enemyMassiveHit,
            energySound,
            escapeSequenceAmbiance,
            escapeSequenceRocks,
            fallingSound,
            fb1BallsAppear,
            fb1BallsMovement,
            fb1Darkness,
            fb1PhaseSwap,
            fb1RaquelFlash,
            fb1SecondPhaseSwap,
            fb1TeleportIn,
            fb2BallShot,
            fb2BallShotLoop,
            fb2BallWarning,
            fb2TeleportOut,
            fb2TileShock,
            fb2TileWarning,
            fbArmAttackCutscene,
            fbTeleportIn,
            fbTeleportOut,
            flameElemental,
            flameThrower,
            flashbackTeleportOutLight,
            flashbackTransition,
            grenadeSound,
            hookshotClick,
            hookshotImpact,
            hookshotNothing,
            hookshotShot,
            hookshotZipIn,
            hookshotZipOut,
            iceElemental,
            iceGrenadeSound,
            icePlatformAppear,
            icePlatformCrack1,
            icePlatformCrack2,
            icePlatformDestroy,
            iceThrower,
            itemCollected,
            itemCollectedJingle,
            ladder,
            lavaFillSound,
            liftingPot,
            magazineEmptySound,
            mechaButtonFailSound,
            mechaButtonPressSound,
            mechaButtonUpFailSound,
            mechaChargeDash,
            mechaDash1,
            mechaDash2,
            menuClick,
            menuNegative,
            menuSelect,
            miniFinalBossMusic,
            moneyLost,
            negativeSound,
            npcNotification,
            parriedSound,
            platformLowering,
            platformRising,
            playerDeathExplosion,
            playerDeathHit,
            playerDeathSheen,
            playerDeathVoice,
            playerGuardingSound,
            playerHealSound,
            playerHitVoices,
            playerJumpSound,
            playerLandingSound,
            playerMassiveHit,
            playerNearDeathSound,
            playerOutOfAmmoSound,
            playerReloadSound,
            playerRollSound,
            playerWallClimbSound,
            playerWallGrabSound,
            playerWallJumpSound,
            poodleBark,
            punchingBagHit,
            puzzleSolvedSound,
            raquelTeleportIn,
            raquelTeleportOut,
            reloadFailSound,
            rockBlockDestroy,
            rodCast,
            rodCaught,
            rodMovement,
            rodRemove,
            rollingBallDeflected,
            samuraiCloneAppear,
            samuraiCloneDisappear,
            samuraiJump,
            sfxVolumeTest,
            shardBallAppear,
            shardBounce,
            shardBreak,
            shardLightAppear,
            shardLightLoop,
            shardShudder,
            shibaBark,
            shotgunFire,
            shovelBegin,
            shovelEnd,
            shovelFail,
            shovelSuccess,
            shurikenLightUpSound,
            spacePlatform,
            spinAttackBoomSound,
            spinAttackChargingSound,
            spinAttackFlashingSound,
            spinnerJumpSound,
            spinnerLandSound,
            spinnerRidingSound,
            spinnerWallHitSound,
            staminaBoom,
            stBernardBark,
            swordHitWall,
            swordsClashedSound,
            swordSpinAttackBeginSound,
            swordSpinAttackSound,
            terminalActivated,
            throwingObject,
            thunderElemental,
            turnUnsighted,
            turnUnsightedAntecipation,
        }

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006
    }
}