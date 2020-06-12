using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
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
        private static HSceneManager manager;
        private static ChaControl[] males;
        
        // Apply duringH height settings when starting H //
        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartAnimationInfo")]
        public static void HScene_SetStartAnimationInfo_HeightPostfix(HScene __instance)
        {
            manager = Traverse.Create(__instance).Field("hSceneManager").GetValue<HSceneManager>();
            males = __instance.GetMales();
            
            HS2_UnlockPlayerHeight.chara = males[0];
            HS2_UnlockPlayerHeight.chara2nd = males[1];

            HS2_UnlockPlayerHeight.cardHeightValue = 0.75f;
            HS2_UnlockPlayerHeight.cardHeightValue2nd = 0.75f;
            
            if (manager.pngMale != null)
            {
                var card = new ChaFileControl();
                
                if (card.LoadCharaFile(manager.pngMale) || card.LoadCharaFile(manager.pngMale, 1))
                    HS2_UnlockPlayerHeight.cardHeightValue = card.custom.body.shapeValueBody[0];
            }
            
            if (manager.pngMaleSecond != null)
            {
                var card = new ChaFileControl();
                
                if (card.LoadCharaFile(manager.pngMaleSecond) || card.LoadCharaFile(manager.pngMaleSecond, 1))
                    HS2_UnlockPlayerHeight.cardHeightValue2nd = card.custom.body.shapeValueBody[0];
            }

            HS2_UnlockPlayerHeight.ApplySettings(false);
            HS2_UnlockPlayerHeight.ApplySettings(true);
        }

        // Ignore setting male height to 0.75f when changing H position //
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "SetShapeBodyValue")]
        public static bool ChaControl_SetShapeBodyValue_HeightPrefix(ChaControl __instance, ref bool __result, int index, float value)
        {
            if (!HSceneManager.isHScene || males == null)
                return true;

            if (males[0] != null && males[0] != __instance && males[1] != null && males[1] != __instance)
                return true;
            
            if (index != 0 || value != 0.75f)
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
    }
}