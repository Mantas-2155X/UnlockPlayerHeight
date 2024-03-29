using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;

using HarmonyLib;

using AIChara;
using AIProject;
using CharaCustom;
using Manager;

namespace AI_UnlockPlayerHeight
{
    public static class CoreHooks
    {
        private static IEnumerable<CodeInstruction> RemoveLock(IEnumerable<CodeInstruction> instructions, int min, int max, string name)
        {
            var il = instructions.ToList();
            
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f);
            if (index <= 0)
            {
                AI_UnlockPlayerHeight.Logger.LogMessage("Failed transpiling '" + name + "' 0.75f index not found!");
                AI_UnlockPlayerHeight.Logger.LogWarning("Failed transpiling '" + name + "' 0.75f index not found!");
                return il;
            }
            
            for(var i = min; i < max; i++)
                il[index + i].opcode = OpCodes.Nop;

            return il;
        }
        
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "Initialize")]
        public static IEnumerable<CodeInstruction> ChaControl_Initialize_RemoveHeightLock(IEnumerable<CodeInstruction> instructions) => RemoveLock(instructions, -6, 2, "ChaControl_Initialize_RemoveHeightLock");

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "InitShapeBody")]
        public static IEnumerable<CodeInstruction> ChaControl_InitShapeBody_RemoveHeightLock(IEnumerable<CodeInstruction> instructions) => RemoveLock(instructions, -2, 2, "ChaControl_InitShapeBody_RemoveHeightLock");

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "SetShapeBodyValue")]
        public static IEnumerable<CodeInstruction> ChaControl_SetShapeBodyValue_RemoveHeightLock(IEnumerable<CodeInstruction> instructions) => RemoveLock(instructions, 0, 2, "ChaControl_SetShapeBodyValue_RemoveHeightLock");

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "UpdateShapeBodyValueFromCustomInfo")]
        public static IEnumerable<CodeInstruction> ChaControl_UpdateShapeBodyValueFromCustomInfo_RemoveHeightLock(IEnumerable<CodeInstruction> instructions) => RemoveLock(instructions, -2, 2, "ChaControl_UpdateShapeBodyValueFromCustomInfo_RemoveHeightLock");
    }
    
    public static class GameHooks
    {
        // Apply height, camera settings for free roam & events //
        [HarmonyPostfix, HarmonyPatch(typeof(PlayerActor), "InitializeIK")]
        public static void PlayerActor_InitializeIK_HeightPostfix(PlayerActor __instance) => AI_UnlockPlayerHeight.ApplySettings(__instance);
        
        // Apply roam height settings when ending H //
        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "OnDisable")]
        public static void HScene_OnDisable_HeightPostfix() => AI_UnlockPlayerHeight.ApplySettings(AI_UnlockPlayerHeight.actor);
        
        // Read card height and apply starting H //
        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartVoice")]
        public static void HScene_SetStartVoice_HeightPostfix()
        {
            if (AI_UnlockPlayerHeight.actor != null)
                AI_UnlockPlayerHeight.ApplySettings(AI_UnlockPlayerHeight.actor);
        }
        
        // Save players height from card into cardHeightValue //
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "InitShapeBody")]
        public static void ChaControl_InitShapeBody_HeightPostfix(ChaControl __instance)
        {
            if (__instance != null && __instance.isPlayer) 
                AI_UnlockPlayerHeight.cardHeightValue = __instance.chaFile.custom.body.shapeValueBody[0];
        }
        
        // Ignore setting male height to 0.75f when changing H position //
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "SetShapeBodyValue")]
        public static bool ChaControl_SetShapeBodyValue_HeightPrefix(ChaControl __instance, ref bool __result, int index, float value)
        {
            if (!HSceneManager.isHScene || __instance == null || __instance.sex != 0)
                return true;

            if (index != 0 || value != 0.75f || __instance.objHitBody != null)
                return true;
            
            var frame = new StackFrame(2);
            if (frame.GetMethod().Name != "MoveNext")
                return true;
            
            frame = new StackFrame(3);
            if (!frame.GetMethod().Name.Contains("ChangeAnimation"))
                return true;
            
            __result = true;
            
            return false;
        }

        // Enable male height slider in charamaker //
        [HarmonyPostfix, HarmonyPatch(typeof(CustomControl), "Initialize")]
        public static void CustomControl_Initialize_HeightPrefix(CustomControl __instance, byte _sex, UnityEngine.GameObject ___objSubCanvas)
        {
            if (_sex != 0)
                return;

            var comp = ___objSubCanvas?.transform.Find("SettingWindow/WinBody").GetComponentInChildren<CvsB_ShapeWhole>();
            if (comp == null)
                return;

            var set = Traverse.Create(comp).Field("ssHeight").GetValue<CustomSliderSet>();
            if (set == null)
                return;

            set.gameObject.SetActive(true);
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(Les), "setAnimationParamater")]
        public static IEnumerable<CodeInstruction> Les_setAnimationParamater_RemoveHeightLock(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f);
            if (index <= 0)
            {
                AI_UnlockPlayerHeight.Logger.LogMessage("Failed transpiling 'Les_setAnimationParamater_RemoveHeightLock' 0.75f index not found!");
                AI_UnlockPlayerHeight.Logger.LogWarning("Failed transpiling 'Les_setAnimationParamater_RemoveHeightLock' 0.75f index not found!");
                return il;
            }

            for (var i = -4; i < 2; i++)
                il[index + i].opcode = OpCodes.Nop;

            index = il.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && (instruction.operand as MethodInfo)?.Name == "get_isPlayer");
            if (index <= 0)
            {
                AI_UnlockPlayerHeight.Logger.LogMessage("Failed transpiling 'Les_setAnimationParamater_RemoveHeightLock' get_isPlayer index not found!");
                AI_UnlockPlayerHeight.Logger.LogWarning("Failed transpiling 'Les_setAnimationParamater_RemoveHeightLock' get_isPlayer index not found!");
                return il;
            }
            
            for (var i = -4; i < 2; i++)
                il[index + i].opcode = OpCodes.Nop;

            return il;
        }
    }
}