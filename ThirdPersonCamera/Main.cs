﻿using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using System.Reflection;
using OWML.Utils;

namespace ThirdPersonCamera
{
    public class Main : ModBehaviour
    {
        public static bool IsLoaded { get; private set; } = false;
        private bool afterMemoryUplink = false;
        private bool _initNextTick = false;
        public bool IsUsingFreeLook;

        public static Main SharedInstance { get; private set; }
        public static ThirdPersonCamera ThirdPersonCamera { get; private set; }
        public static UIHandler UIHandler { get; private set; }
        public static PlayerMeshHandler PlayerMeshHandler { get; private set; }
        public static ToolMaterialHandler ToolMaterialHandler { get; private set; }
        public static HUDHandler HUDHandler { get; private set; }

        public static bool KeepFreeLookAngle { get; private set; }
        public static bool UseThirdPersonByDefault { get; private set; }
        public static bool UseCustomDreamerModel { get; private set; }

        public static float DefaultPlayerDistance { get; private set; }
        public static float DefaultPlayerSuitDistance { get; private set; }
        public static float DefaultShipDistance { get; private set; }

        private void Start()
        {
            SharedInstance = this;

            WriteSuccess($"ThirdPersonCamera is loaded!");

            // Helpers
            ThirdPersonCamera = new ThirdPersonCamera();
            UIHandler = new UIHandler();
            PlayerMeshHandler = new PlayerMeshHandler();
            ToolMaterialHandler = new ToolMaterialHandler();
            HUDHandler = new HUDHandler();

            // Patches
            ModHelper.HarmonyHelper.AddPostfix<StreamingGroup>("OnFinishOpenEyes", typeof(Patches), nameof(Patches.OnFinishOpenEyes));
            ModHelper.HarmonyHelper.AddPostfix<PlayerCameraEffectController>("CloseEyes", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<PlayerTool>("EquipTool", typeof(Patches), nameof(Patches.EquipTool));
            ModHelper.HarmonyHelper.AddPostfix<PlayerTool>("UnequipTool", typeof(Patches), nameof(Patches.UnequipTool));
            ModHelper.HarmonyHelper.AddPostfix<GhostGrabController>("OnStartLiftPlayer", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
            //ModHelper.HarmonyHelper.AddPostfix<DreamWorldController>("ExitLanternBounds", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
            //ModHelper.HarmonyHelper.AddPostfix<DreamWorldController>("EnterLanternBounds", typeof(Patches), nameof(Patches.EnableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<ProbeLauncher>("RetrieveProbe", typeof(Patches), nameof(Patches.OnRetrieveProbe));
            ModHelper.HarmonyHelper.AddPrefix<MindProjectorTrigger>("OnProjectionStart", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<MindProjectorTrigger>("OnProjectionComplete", typeof(Patches), nameof(Patches.EnableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<MindProjectorTrigger>("OnTriggerVolumeExit", typeof(Patches), nameof(Patches.EnableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<ShipDetachableModule>("Detach", typeof(Patches), nameof(Patches.OnDetach));
            ModHelper.HarmonyHelper.AddPostfix<LanternZoomPoint>("StartZoomIn", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<LanternZoomPoint>("FinishRetroZoom", typeof(Patches), nameof(Patches.EnableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<RoastingStickController>("OnEnterRoastingMode", typeof(Patches), nameof(Patches.OnRoastingStickActivate));
            ModHelper.HarmonyHelper.AddPrefix<QuantumObject>("OnSwitchActiveCamera", typeof(Patches), nameof(Patches.OnSwitchActiveCamera));
            ModHelper.HarmonyHelper.AddPostfix<TimelineObliterationController>("OnCrackEffectComplete", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<NomaiTranslatorProp>("Update", typeof(Patches), nameof(Patches.NomaiTranslaterPropUpdate));
            ModHelper.HarmonyHelper.AddPostfix<ShipNotificationDisplay>("Update", typeof(Patches), nameof(Patches.ShipNotificationDisplayUpdate));
            ModHelper.HarmonyHelper.AddPrefix<ReferenceFrameTracker>("GetPossibleReferenceFrame", typeof(Patches), nameof(Patches.GetPossibleReferenceFrame));
            ModHelper.HarmonyHelper.AddPrefix<ReferenceFrameTracker>("FindReferenceFrameInLineOfSight", typeof(Patches), nameof(Patches.PreFindReferenceFrameInLineOfSight));
            ModHelper.HarmonyHelper.AddPrefix<ReferenceFrameTracker>("FindReferenceFrameInLineOfSight", typeof(Patches), nameof(Patches.PostFindReferenceFrameInLineOfSight));
            ModHelper.HarmonyHelper.AddPrefix<PlayerCameraController>("UpdateRotation", typeof(Patches), nameof(Patches.UpdateRotation));
            ModHelper.HarmonyHelper.AddPrefix<PlayerCameraController>("Update", typeof(Patches), nameof(Patches.PlayerCameraControllerUpdate));
            ModHelper.HarmonyHelper.AddPrefix<PlayerCameraController>("UpdateInput", typeof(Patches), nameof(Patches.UpdateInput));
            ModHelper.HarmonyHelper.AddPrefix<PlayerCharacterController>("UpdateTurning", typeof(Patches), nameof(Patches.UpdateTurning));
            ModHelper.HarmonyHelper.AddPostfix<ProbeLauncherUI>("OnTakeSnapshot", typeof(Patches), nameof(Patches.OnTakeSnapshot));
            ModHelper.HarmonyHelper.AddPrefix<QuantumObject>("IsLockedByProbeSnapshot", typeof(Patches), nameof(Patches.IsLockedByProbeSnapshot));
            ModHelper.HarmonyHelper.AddPostfix<SignalscopeUI>("UpdateLabels", typeof(Patches), nameof(Patches.UpdateLabels));
            ModHelper.HarmonyHelper.AddPostfix<SignalscopeUI>("UpdateWaveform", typeof(Patches), nameof(Patches.UpdateWaveform));
            ModHelper.HarmonyHelper.AddPostfix<SignalscopeReticleController>("UpdateBrackets", typeof(Patches), nameof(Patches.UpdateBrackets));
            ModHelper.HarmonyHelper.AddPrefix<HUDCamera>("OnSwitchActiveCamera", typeof(Patches), nameof(Patches.HUDCameraOnSwitchActiveCamera));
            ModHelper.HarmonyHelper.AddPrefix<NomaiRemoteCamera>("LateUpdate", typeof(Patches), nameof(Patches.NomaiRemoteCameraLateUpdate));
            ModHelper.HarmonyHelper.AddPrefix<NomaiRemoteCameraPlatform>("Awake", typeof(Patches), nameof(Patches.NomaiRemoteCameraPlatformAwake));
            ModHelper.HarmonyHelper.AddPrefix<PlayerState>("OnInitPlayerForceAlignment", typeof(Patches), nameof(Patches.OnInitPlayerForceAlignment));
            ModHelper.HarmonyHelper.AddPrefix<PlayerState>("OnBreakPlayerForceAlignment", typeof(Patches), nameof(Patches.OnBreakPlayerForceAlignment));

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            ThirdPersonCamera.OnDestroy();
            UIHandler.OnDestroy();
            PlayerMeshHandler.OnDestroy();
            ToolMaterialHandler.OnDestroy();
            HUDHandler.OnDestroy();
        }

        public override void Configure(IModConfig config)
        {
            base.Configure(config);
            KeepFreeLookAngle = config.GetSettingsValue<bool>("Keep free look angle");
            UseThirdPersonByDefault = config.GetSettingsValue<bool>("Use 3rd person by default");
            UseCustomDreamerModel = config.GetSettingsValue<bool>("Use custom dreamer model");

            DefaultPlayerDistance = config.GetSettingsValue<float>("Default camera zoom (no suit)");
            DefaultPlayerSuitDistance = config.GetSettingsValue<float>("Default camera zoom (suit)");
            DefaultShipDistance = config.GetSettingsValue<float>("Default camera zoom (ship)");
            
            if(ThirdPersonCamera != null)
            {
                ThirdPersonCamera.SetDefaultDistanceSettings(PlayerState.AtFlightConsole());
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.name.Equals("SolarSystem"))
            {
                IsLoaded = false;
                afterMemoryUplink = false;
                return;
            }

            // Already loaded but we're being put into the SolarSystem scene again
            // We must have done the memory uplink (universe is reset after)
            if (IsLoaded) afterMemoryUplink = true;

            PreInit();
        }

        private void PreInit()
        {
            try
            {
                ThirdPersonCamera.PreInit();
                WriteSuccess("ThirdPersonCamera pre-initialization succeeded");
                _initNextTick = true;
            }
            catch(Exception e)
            {
                WriteError($"ThirdPersonCamera pre-initialization failed. {e.Message}. {e.StackTrace}");
            }
        }

        private void Init()
        {
            try
            {
                IsLoaded = true;

                ThirdPersonCamera.Init();
                UIHandler.Init();
                HUDHandler.Init();
                PlayerMeshHandler.Init();

                if (afterMemoryUplink) ThirdPersonCamera.CameraEnabled = true;

                try
                {
                    ModHelper.Interaction.GetMod("xen.DayDream").GetValue<List<OWCamera>>("Cameras").Add(ThirdPersonCamera.OWCamera);
                }
                catch (Exception) { }

                WriteSuccess("ThirdPersonCamera initialization succeeded");
            }
            catch (Exception e)
            {
                WriteError($"ThirdPersonCamera initialization failed. {e.Message}. {e.StackTrace}");
            }
        }

        private void Update()
        {
            if(_initNextTick)
            {
                Init();
                _initNextTick = false;
            }

            if (!IsLoaded) return;

            ThirdPersonCamera.Update();
            PlayerMeshHandler.Update();
            ToolMaterialHandler.Update();
            HUDHandler.Update();
        }

        public static bool IsThirdPerson()
        {
            // Also going to count if they're using static camera because why not
            return (ThirdPersonCamera.CameraEnabled && ThirdPersonCamera.CameraActive) || Locator.GetActiveCamera().name == "StaticCamera";
        }

        public static void OnFinishOpenEyes()
        {
            if (ThirdPersonCamera.JustStartedLoop)
            {
                Main.WriteInfo("Opening eyes for the first time");
                ThirdPersonCamera.JustStartedLoop = false;
                ThirdPersonCamera.EnableCamera();
                if (UseThirdPersonByDefault)
                {
                    ThirdPersonCamera.ActivateCamera();
                    Locator.GetPlayerCameraController().CenterCameraOverSeconds(1.0f, true);
                }
            }
            else
            {
                ThirdPersonCamera.EnableCamera();
            }
        }

        public static void OnInitPlayerForceAlignment()
        {
            // No longer zero G
            if(Main.SharedInstance.IsUsingFreeLook)
            {
                Locator.GetPlayerController().UnlockMovement();
            }
        }

        public static void OnBreakPlayerForceAlignment()
        {
            // In zero G
            if (Main.SharedInstance.IsUsingFreeLook)
            {
                Locator.GetPlayerController().LockMovement(true);
            }
        }

        public static void OnStartFreeLook()
        {
            if(PlayerState.InZeroG())
            {
                Locator.GetPlayerController().LockMovement(true);
            }
        }

        public static void OnStopFreeLook()
        {
            if (PlayerState.InZeroG())
            {
                Locator.GetPlayerController().UnlockMovement();
            }
        }

        public static void WriteError(string msg)
        {
            SharedInstance.ModHelper.Console.WriteLine(msg, MessageType.Error);
        }

        public static void WriteWarning(string msg)
        {
            SharedInstance.ModHelper.Console.WriteLine(msg, MessageType.Warning);
        }

        public static void WriteInfo(string msg)
        {
            SharedInstance.ModHelper.Console.WriteLine(msg, MessageType.Info);
        }

        public static void WriteSuccess(string msg)
        {
            SharedInstance.ModHelper.Console.WriteLine(msg, MessageType.Success);
        }
    }
}
