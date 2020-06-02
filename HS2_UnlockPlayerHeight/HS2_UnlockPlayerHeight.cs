using BepInEx;
using BepInEx.Harmony;
using BepInEx.Logging;
using BepInEx.Configuration;

using AIChara;
using Manager;
using UnityEngine;

namespace HS2_UnlockPlayerHeight {
    [BepInPlugin(nameof(HS2_UnlockPlayerHeight), nameof(HS2_UnlockPlayerHeight), VERSION)]
    public class HS2_UnlockPlayerHeight : BaseUnityPlugin
    {
        public const string VERSION = "1.3.0";
        
        public new static ManualLogSource Logger;

        private static ConfigEntry<bool> cardHeight { get; set; }
        private static ConfigEntry<int> customHeight { get; set; }

        public static ChaControl chara;
        
        public static float cardHeightValue;

        private void Awake()
        {
            Logger = base.Logger;

            cardHeight = Config.Bind(new ConfigDefinition("H Scene", "Height from card"), true, new ConfigDescription("Set players height according to the value in the card"));
            customHeight = Config.Bind(new ConfigDefinition("H Scene", "Custom height"), 75, new ConfigDescription("If 'Height from card' is off, use this value instead'", new AcceptableValueRange<int>(-100, 200)));

            HarmonyWrapper.PatchAll(typeof(CoreHooks));

            if (Application.productName != "HoneySelect2") 
                return;
            
            cardHeight.SettingChanged += delegate { ApplySettings(); };
            customHeight.SettingChanged += delegate { ApplySettings(); };

            HarmonyWrapper.PatchAll(typeof(GameHooks));
        }
        
        public static void ApplySettings()
        {
            if (chara == null || !HSceneManager.isHScene)
                return;
            
            chara.SetShapeBodyValue(0, GetHeight());
        }
        
        private static float GetHeight() => cardHeight.Value ? cardHeightValue : customHeight.Value / 100f;
    }
}