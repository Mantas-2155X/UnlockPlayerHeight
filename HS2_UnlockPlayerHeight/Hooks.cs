using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;

using HarmonyLib;

using AIChara;
using Manager;
using CharaCustom;

namespace HS2_UnlockPlayerHeight
{
    public static class CoreHooks
    {
        private static IEnumerable<CodeInstruction> RemoveLock(IEnumerable<CodeInstruction> instructions, int min, int max, string name)
        {
            var il = instructions.ToList();
            
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f);
            if (index <= 0)
            {
                HS2_UnlockPlayerHeight.Logger.LogMessage("Failed transpiling '" + name + "' 0.75f index not found!");
                HS2_UnlockPlayerHeight.Logger.LogWarning("Failed transpiling '" + name + "' 0.75f index not found!");
                return il;
            }
            
            for(int i = min; i < max; i++)
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
        // Apply duringH height settings when starting H //
        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartAnimationInfo")]
        public static void HScene_SetStartAnimationInfo_HeightPostfix(HScene __instance)
        {
            HS2_UnlockPlayerHeight.chara = Traverse.Create(__instance).Field("hSceneManager").Field("player").GetValue<ChaControl>();
            
            HS2_UnlockPlayerHeight.ApplySettings();
        }
 
        // Save players height from card into cardHeightValue //
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "InitShapeBody")]
        public static void ChaControl_InitShapeBody_HeightPostfix(ChaControl __instance)
        {
            if (__instance != null && __instance.isPlayer) 
                HS2_UnlockPlayerHeight.cardHeightValue = __instance.chaFile.custom.body.shapeValueBody[0];
        }

        // Ignore setting male height to 0.75f when changing H position //
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "SetShapeBodyValue")]
        public static bool ChaControl_SetShapeBodyValue_HeightPrefix(ChaControl __instance, ref bool __result, int index, float value)
        {
            if (!HSceneManager.isHScene || __instance == null || __instance.sex != 0)
                return true;

            if (index != 0 || value != 0.75f || __instance.objHitBody != null)
                return true;
            
            __result = true;
            
            return false;
        }

        // Enable male height slider in charamaker //
        [HarmonyPostfix, HarmonyPatch(typeof(CustomControl), "Initialize")]
        public static void CustomControl_Initialize_HeightPrefix(CustomControl __instance, byte _sex)
        {
            if (_sex != 0)
                return;

            var comp = __instance.GetComponentInChildren<CvsB_ShapeWhole>();
            if (comp == null)
                return;

            var set = Traverse.Create(comp).Field("ssHeight").GetValue<CustomSliderSet>();
            if (set == null)
                return;

            set.gameObject.SetActive(true);
        }

        //--Hard height lock of 75 for the player removal--//
        private static IEnumerable<CodeInstruction> HScene_ChangeAnimation_RemoveHeightLock(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && (instruction.operand as MethodInfo)?.Name == "SetShapeBodyValue");
            if (index <= 0)
            {
                HS2_UnlockPlayerHeight.Logger.LogMessage("Failed transpiling 'HScene_ChangeAnimation_RemoveHeightLock' SetShapeBodyValue index not found!");
                HS2_UnlockPlayerHeight.Logger.LogWarning("Failed transpiling 'HScene_ChangeAnimation_RemoveHeightLock' SetShapeBodyValue index not found!");
                return il;
            }

            for (int i = -7; i < 2; i++)
                il[index + i].opcode = OpCodes.Nop;
            
            return il;
        }
    }
}