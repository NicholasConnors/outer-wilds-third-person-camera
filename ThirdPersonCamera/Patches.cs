﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using OWML.ModHelper;
using OWML.Common;

namespace ThirdPersonCamera
{
    class Patches
    {
        public static void DisableThirdPersonCameraEvent()
        {
            GlobalMessenger.FireEvent("DisableThirdPersonCamera");
        }

        public static void EnableThirdPersonCameraEvent()
        {
            GlobalMessenger.FireEvent("EnableThirdPersonCamera");
        }

        public static void OnFinishOpenEyes()
        {
            Main.OnFinishOpenEyes();
        }

        public static void EquipTool(PlayerTool __instance)
        {
            GlobalMessenger<PlayerTool>.FireEvent("OnEquipTool", __instance);
        }

        public static void UnequipTool(PlayerTool __instance)
        {
            GlobalMessenger<PlayerTool>.FireEvent("OnUnequipTool", __instance);
        }

        public static void OnRetrieveProbe()
        {
            GlobalMessenger.FireEvent("OnRetrieveProbe");
        }

        public static void OnDetach(ShipDetachableModule __instance)
        {
            GlobalMessenger<ShipDetachableModule>.FireEvent("ShipModuleDetached", __instance);
        }

        public static void OnRoastingStickActivate()
        {
            GlobalMessenger.FireEvent("OnRoastingStickActivate");
        }

        public static bool OnSwitchActiveCamera(OWCamera activeCamera)
        {
            // Not sure who gets the event first
            bool flag = false;

            // If we switched from ThirdPersonCamera to PlayerCamera or vice versa we don't want to do anything
            string currentCamera = ThirdPersonCamera.CurrentCamera.name;
            string previousCamera = ThirdPersonCamera.PreviousCamera.name;

            // If we got the event first
            flag |= currentCamera == "PlayerCamera" && previousCamera == "ThirdPersonCamera";
            flag |= currentCamera == "ThirdPersonCamera" && previousCamera == "PlayerCamera";

            // If they got the event first
            previousCamera = currentCamera;
            currentCamera = activeCamera.name;

            flag |= currentCamera == "PlayerCamera" && previousCamera == "ThirdPersonCamera";
            flag |= currentCamera == "ThirdPersonCamera" && previousCamera == "PlayerCamera";

            // If flag we don't run the original method
            return !flag;
        }

        public static void NomaiTranslaterPropUpdate(NomaiTranslatorProp __instance, Text ____textField)
        {
            Text screenText = UIHandler.TranslatorText;
            if (screenText != null) screenText.text = ____textField.text;
        }

        public static void ShipNotificationDisplayUpdate(ShipNotificationDisplay __instance, Canvas ____displayCanvas)
        {
            if (__instance.name != "ConsoleDisplay") return;

            Text screenText = UIHandler.ShipText;

            string text = "";

            foreach(Text t in ____displayCanvas.GetComponentsInChildren<Text>())
            {
                if (t.name == "TestText") continue;
                text += t.text.Replace("\n", " ") + "\n";
            }

            screenText.text = __instance.HasText() ? text : "";
        }

        public static bool GetPossibleReferenceFrame(ReferenceFrameTracker __instance, ReferenceFrame ____possibleReferenceFrame)
        {
            // Stops you from targeting your ship while piloting it
            if (____possibleReferenceFrame.GetOWRigidBody().name == "Player_Body") return false;
            if (PlayerState.AtFlightConsole() && ____possibleReferenceFrame.GetOWRigidBody().name == "Ship_Body") return false;
            return true;
        }

        public static void PreFindReferenceFrameInLineOfSight()
        {
            // Don't let the raycast get the player or (sometimes) the ship
            Locator.GetPlayerBody().DisableCollisionDetection();
            if (PlayerState.AtFlightConsole()) Locator.GetShipBody().DisableCollisionDetection();
        }

        public static void PostFindReferenceFrameInLineOfSight()
        {
            Locator.GetPlayerBody().EnableCollisionDetection();
            Locator.GetShipBody().EnableCollisionDetection();
        }

        public static bool UpdateRotation(PlayerCameraController __instance, ref float ____degreesX, ref float ____degreesY, OWCamera ____playerCamera, bool ____isSnapping,
                ShipCockpitController ____shipController, Quaternion ____rotationX, Quaternion ____rotationY)
        {
            if (!Main.IsThirdPerson() || !OWInput.IsPressed(InputLibrary.freeLook, 0f)) return true;

            ____degreesX %= 360f;
            ____degreesY %= 360f;
            if (!____isSnapping)
            {
                if ((____shipController == null || (____shipController.AllowFreeLook() | !____shipController.IsPlayerAtFlightConsole())) && OWInput.IsPressed(InputLibrary.freeLook, 0f))
                {
                    ____degreesY = Mathf.Clamp(____degreesY, -80f, 80f);
                }
                else return true;
            }
            ____rotationX = Quaternion.AngleAxis(____degreesX, Vector3.up);
            ____rotationY = Quaternion.AngleAxis(____degreesY, -Vector3.right);
            Quaternion localRotation = ____rotationX * ____rotationY * Quaternion.identity;
            ____playerCamera.transform.localRotation = localRotation;

            return false;
        }

        public static bool UpdateInput(PlayerCameraController __instance, float deltaTime, ShipCockpitController ____shipController, bool ____zoomed, OWCamera ____playerCamera,
                float ____initFOV, ref float ____degreesX, ref float ____degreesY)
        {
            if ((____shipController != null && ____shipController.IsPlayerAtFlightConsole()) || !Main.IsThirdPerson() || !OWInput.IsPressed(InputLibrary.freeLook, 0f)) return true;

            bool flag3 = Locator.GetAlarmSequenceController() != null && Locator.GetAlarmSequenceController().IsAlarmWakingPlayer();
            Vector2 vector = Vector2.one;
            vector *= ((____zoomed || flag3) ? PlayerCameraController.ZOOM_SCALAR : 1f);
            vector *= ____playerCamera.fieldOfView / ____initFOV;

            if (Time.timeScale > 1f)
            {
                vector /= Time.timeScale;
            }

            Vector2 axisValue = OWInput.GetAxisValue(InputLibrary.look, InputMode.All);
            ____degreesX += axisValue.x * 180f * vector.x * deltaTime;
            ____degreesY += axisValue.y * 180f * vector.y * deltaTime;

            return false;
        }

        public static bool PlayerCameraControllerUpdate(PlayerCameraController __instance, ref float ____degreesX)
        {
            // When ending freelook makes you take the shortest path back, also enables it for when you did it out of the ship
            if (OWInput.IsNewlyReleased(InputLibrary.freeLook, InputMode.All))
            {
                if (____degreesX > 180f) ____degreesX -= 360f;
                if (____degreesX < -180f) ____degreesX += 360f;
            }

            if (!Main.IsThirdPerson() || PlayerState.AtFlightConsole()) return true;

            if (OWInput.IsNewlyReleased(InputLibrary.freeLook, InputMode.All))
            {
                __instance.CenterCameraOverSeconds(0.33f, true);
            }
            if (OWTime.IsPaused(OWTime.PauseType.Reading))
            {
                // To call a private method
                MethodInfo methodInfo = __instance.GetType().GetMethod("UpdateCamera", BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo.Invoke(__instance, new object[] { Time.unscaledDeltaTime });
            }

            return false;
        }

        public static bool UpdateTurning()
        {
            // Return false if turning is locked (we're using freelook)
            return !(Main.IsThirdPerson() && OWInput.IsPressed(InputLibrary.freeLook));
        }

        public static void OnTakeSnapshot(ProbeLauncherUI __instance, ProbeCamera camera, RenderTexture snapshot, Texture2D ____rearSnapshotOverlay, Texture2D ____frontSnapshotOverlay)
        {
            if (camera.GetID() == ProbeCamera.ID.Reverse)
            {
                UIHandler.SetProbeLauncherTexture(____rearSnapshotOverlay);
            }
            else
            {
                UIHandler.SetProbeLauncherTexture(____frontSnapshotOverlay);
            }
            UIHandler.SetProbeLauncherTexture(snapshot);
        }

        public static bool IsLockedByProbeSnapshot(QuantumObject __instance, bool ____visibleInProbeSnapshot, ref bool __result)
        {
            if (Main.IsThirdPerson())
            {
                __result = ____visibleInProbeSnapshot && Locator.GetToolModeSwapper().GetToolMode() == ToolMode.Probe;
                return false;
            }

            return true;
        }

        public static void UpdateLabels(SignalscopeUI __instance, Text ____distanceLabel, Text ____signalscopeLabel)
        {
            UIHandler.SetSignalScopeLabel(____signalscopeLabel.text, ____distanceLabel.text);
        }

        public static void UpdateWaveform(SignalscopeUI __instance, Vector3[] ____linePoints)
        {
            UIHandler.SetSignalScopeWaveform(____linePoints);
        }

        public static void UpdateBrackets(SignalscopeReticleController __instance, Transform ____reticuleBracketsTransform, List<SkinnedMeshRenderer> ____clonedLeftBrackets,
            List<SkinnedMeshRenderer> ____clonedRightBrackets)
        {
            Transform parent = PlayerState.AtFlightConsole() && Main.IsThirdPerson() ? UIHandler.SigScopeReticuleParent.transform : ____reticuleBracketsTransform;
            for (int i = 0; i < ____clonedLeftBrackets.Count; i++)
            {
                ____clonedLeftBrackets[i].transform.parent = parent;
                ____clonedRightBrackets[i].transform.parent = parent;
            }
        }

        public static bool HUDCameraOnSwitchActiveCamera(HUDCamera __instance, OWCamera __0) 
        {
            if(__0.name.Equals("ThirdPersonCamera"))
            {
                MethodInfo methodInfo = typeof(HUDCamera).GetMethod("ResumeHUDRendering", BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo.Invoke(__instance, new object[] { });
                return false;
            }
            return true;
        }
    }
}
