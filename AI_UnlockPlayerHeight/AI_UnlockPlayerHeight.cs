﻿using System.Collections;

using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;

using AIChara;
using AIProject;
using Manager;
using UnityEngine;

namespace AI_UnlockPlayerHeight 
{
    [BepInPlugin(nameof(AI_UnlockPlayerHeight), nameof(AI_UnlockPlayerHeight), VERSION)]
    public class AI_UnlockPlayerHeight : BaseUnityPlugin
    {
        public const string VERSION = "1.4.4";
        
        public new static ManualLogSource Logger;

        private static ConfigEntry<bool> alignCamera { get; set; }
        private static ConfigEntry<float> lookAtOffset { get; set; }
        private static ConfigEntry<float> lookAtPOVOffset { get; set; }

        private static ConfigEntry<bool> cardHeight { get; set; }
        private static ConfigEntry<int> customHeight { get; set; }
        
        private static ConfigEntry<bool> cardHeightDuringH { get; set; }
        private static ConfigEntry<int> customHeightDuringH { get; set; }

        public static PlayerActor actor;

        public static float cardHeightValue;
        
        private static readonly float[] defaultY =
        {
            0f, 
            0f, 
            10f, 
            15f, 
            15f, 
            16.25f, 
            15f, 
            16f, 
            20.11f
        };

        private static AI_UnlockPlayerHeight instance;
        
        private void Awake()
        {
            instance = this;
            Logger = base.Logger;
            
            alignCamera = Config.Bind(new ConfigDefinition("Camera", "Align camera to player height"), true, new ConfigDescription("Aligns camera position according to player height"));
            lookAtOffset = Config.Bind(new ConfigDefinition("Camera", "Camera y offset"), 0f, new ConfigDescription("Camera lookAt y offset", new AcceptableValueRange<float>(-10f, 10f)));
            lookAtPOVOffset = Config.Bind(new ConfigDefinition("Camera", "Camera POV y offset"), 0f, new ConfigDescription("Camera lookAtPOV y offset", new AcceptableValueRange<float>(-10f, 10f)));

            cardHeight = Config.Bind(new ConfigDefinition("Free Roam & Events", "Height from card"), true, new ConfigDescription("Set players height according to the value in the card"));
            customHeight = Config.Bind(new ConfigDefinition("Free Roam & Events", "Custom height"), 75, new ConfigDescription("If 'Height from card' is off, use this value instead", new AcceptableValueRange<int>(-100, 200)));

            cardHeightDuringH = Config.Bind(new ConfigDefinition("H Scene", "Height from card (H)"), false, new ConfigDescription("Set players height according to the value in the card"));
            customHeightDuringH = Config.Bind(new ConfigDefinition("H Scene", "Custom height (H)"), 75, new ConfigDescription("If 'Height from card' is off, use this value instead", new AcceptableValueRange<int>(-100, 200)));

            var harmony = new HarmonyLib.Harmony("HS2_UnlockPlayerHeight");
            
            harmony.PatchAll(typeof(CoreHooks));

            if (Application.productName != "AI-Syoujyo" && Application.productName != "AI-Shoujo") 
                return;
            
            alignCamera.SettingChanged += delegate { ApplySettings(actor); };
            lookAtOffset.SettingChanged += delegate { ApplySettings(actor); };

            cardHeight.SettingChanged += delegate { ApplySettings(actor); };
            customHeight.SettingChanged += delegate { ApplySettings(actor); };

            cardHeightDuringH.SettingChanged += delegate { ApplySettings(actor); };
            customHeightDuringH.SettingChanged += delegate { ApplySettings(actor); };
                
            harmony.PatchAll(typeof(GameHooks));
        }

        private static float GetHeight()
        {
            if(HSceneManager.isHScene)
                return cardHeightDuringH.Value ? cardHeightValue : customHeightDuringH.Value / 100f;
            
            return cardHeight.Value ? cardHeightValue : customHeight.Value / 100f;
        }
        
        public static void ApplySettings(PlayerActor __instance)
        {
            actor = __instance;
            if (actor == null) 
                return;

            var chaControl = actor.ChaControl;
            if (chaControl == null) 
                return;

            var height = GetHeight();
            chaControl.SetShapeBodyValue(0, height);
            
            var controller = actor.PlayerController;
            if (controller == null) 
                return;

            instance.StartCoroutine(ApplySettings_Coroutine(controller, chaControl));
        }

        // Need to recode this someday to something more efficient and not "hardcoded". head transform, eyes transform for pov
        private static IEnumerator ApplySettings_Coroutine(PlayerController controller, ChaControl chaControl)
        {
            yield return null;
            
            var eyeObjs = chaControl.eyeLookCtrl.eyeLookScript.eyeObjs;
            
            var newEyePos = Vector3.Lerp(eyeObjs[0].eyeTransform.position, eyeObjs[1].eyeTransform.position, 0.5f);
            var newHeadPos = chaControl.objHead.transform.position;
            
            for (var i = 0; i < controller.transform.childCount; i++)
            {
                var child = controller.transform.GetChild(i);
                var position = child.position;
                var localPosition = child.localPosition;

                if (!alignCamera.Value)
                {
                    child.localPosition = new Vector3(localPosition.x, defaultY[i], localPosition.z);
                    continue;
                }

                if (child.name.Contains("Head"))
                {
                    child.position = new Vector3(position.x, newHeadPos.y, position.z);
                    continue;
                }

                if (child.name.Contains("Action"))
                {
                    child.localPosition = new Vector3(localPosition.x, defaultY[i] + (-0.75f + Mathf.Clamp01(GetHeight())) * 2, localPosition.z);
                    continue;
                }
                
                if (child.name.Contains("Lookat"))
                {
                    var offset = lookAtOffset.Value;

                    if (child.name.Contains("POV"))
                        offset = lookAtPOVOffset.Value;

                    child.position = new Vector3(position.x, newEyePos.y + offset, position.z);
                    continue;
                }
                
                child.localPosition = new Vector3(localPosition.x, defaultY[i] + (-0.75f + GetHeight()) * 2, localPosition.z);
            }
        }
    }
}