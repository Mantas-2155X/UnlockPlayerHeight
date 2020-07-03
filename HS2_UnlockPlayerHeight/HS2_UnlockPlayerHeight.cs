using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;

using AIChara;
using Manager;
using UnityEngine;

namespace HS2_UnlockPlayerHeight {
    [BepInPlugin(nameof(HS2_UnlockPlayerHeight), nameof(HS2_UnlockPlayerHeight), VERSION)]
    public class HS2_UnlockPlayerHeight : BaseUnityPlugin
    {
        public const string VERSION = "1.4.2";
        
        public new static ManualLogSource Logger;

        private static ConfigEntry<bool> cardHeight { get; set; }
        private static ConfigEntry<int> customHeight { get; set; }
        private static ConfigEntry<bool> cardHeight2nd { get; set; }
        private static ConfigEntry<int> customHeight2nd { get; set; }
        
        public static ChaControl chara;
        public static ChaControl chara2nd;
        
        public static float cardHeightValue;
        public static float cardHeightValue2nd;
        
        private void Awake()
        {
            Logger = base.Logger;

            cardHeight = Config.Bind(new ConfigDefinition("H Scene", "Height from card"), true, new ConfigDescription("Set players height according to the value in the card"));
            customHeight = Config.Bind(new ConfigDefinition("H Scene", "Custom height"), 75, new ConfigDescription("If 'Height from card' is off, use this value instead'", new AcceptableValueRange<int>(-100, 200)));

            cardHeight2nd = Config.Bind(new ConfigDefinition("H Scene", "Height from card 2nd"), true, new ConfigDescription("Set players height according to the value in the card for 2nd male"));
            customHeight2nd = Config.Bind(new ConfigDefinition("H Scene", "Custom height 2nd"), 75, new ConfigDescription("If 'Height from card' is off, use this value instead for 2nd male", new AcceptableValueRange<int>(-100, 200)));

            var harmony = new HarmonyLib.Harmony("HS2_UnlockPlayerHeight");
            
            harmony.PatchAll(typeof(CoreHooks));

            if (Application.productName != "HoneySelect2") 
                return;
            
            cardHeight.SettingChanged += delegate { ApplySettings(false); };
            customHeight.SettingChanged += delegate { ApplySettings(false); };

            cardHeight2nd.SettingChanged += delegate { ApplySettings(true); };
            customHeight2nd.SettingChanged += delegate { ApplySettings(true); };
            
            harmony.PatchAll(typeof(GameHooks));
        }
        
        public static void ApplySettings(bool is2nd)
        {
            if (!HSceneManager.isHScene)
                return;

            if (is2nd)
            {
                if (chara2nd != null)
                    chara2nd.SetShapeBodyValue(0, cardHeight2nd.Value ? cardHeightValue2nd : customHeight2nd.Value / 100f);
            }
            else
            {
                if (chara != null)
                    chara.SetShapeBodyValue(0, cardHeight.Value ? cardHeightValue : customHeight.Value / 100f);
            }
        }
    }
}