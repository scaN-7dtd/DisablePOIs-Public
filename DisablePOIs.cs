using HarmonyLib;
using System;
using System.IO;
using System.Xml.Linq;
using UnityEngine;

namespace DisablePOIs
{
    public class ModApi : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            ConfigLoader.Load(_modInstance.Path);

            var harmony = new Harmony("scan_disablepois");
            harmony.PatchAll();

            Debug.Log("[DisablePOIs] Loaded.");
        }
    }

    internal static class ConfigLoader
    {
        public static bool DebugLogs = false;
        public static bool DisableSleeperSpawn = true;

        public static void Load(string modPath)
        {
            string configPath = Path.Combine(modPath, "config.xml");

            if (!File.Exists(configPath))
            {
                Debug.LogWarning("[DisablePOIs] Config file not found at: " + configPath);
                return;
            }

            try
            {
                XDocument doc = XDocument.Load(configPath);

                if (doc.Root.Attribute("debugLogs") != null)
                    bool.TryParse(doc.Root.Attribute("debugLogs").Value, out DebugLogs);

                if (doc.Root.Attribute("disableSleeperSpawn") != null)
                    bool.TryParse(doc.Root.Attribute("disableSleeperSpawn").Value, out DisableSleeperSpawn);

                Debug.Log($"[DisablePOIs] Config loaded. Debug logs: {DebugLogs}. Disable sleeper spawn: {DisableSleeperSpawn}.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[DisablePOIs] Error reading config file: " + ex.Message);
            }
        }
    }

    [HarmonyPatch(typeof(BlockTrigger), nameof(BlockTrigger.OnTriggered))]
    public class BlockTrigger_OnTriggered_Patch
    {
        static bool Prefix(BlockTrigger __instance, int index)
        {
            if (ConfigLoader.DebugLogs)
            {
                Debug.Log($"[DisablePOIs] BlockTrigger.OnTriggered intercepted (index={index}, pos={__instance.ToWorldPos()}).");
            }

            __instance.SetTriggeredValueFlag((byte)index);
            if (__instance.CheckIsTriggered())
            {
                __instance.TriggeredValues.Clear();
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(SleeperVolume), nameof(SleeperVolume.OnTriggered))]
    public class SleeperVolume_OnTriggered_Patch
    {
        static bool Prefix(int _triggerIndex)
        {
            if (ConfigLoader.DebugLogs)
            {
                Debug.Log($"[DisablePOIs] SleeperVolume.OnTriggered intercepted (index={_triggerIndex}).");
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(SleeperVolume), nameof(SleeperVolume.UpdatePlayerTouched))]
    public class SleeperVolume_UpdatePlayerTouched_Patch
    {
        static bool loggedOnce = false;

        static bool Prefix()
        {
            if (!ConfigLoader.DisableSleeperSpawn)
            {
                return true;
            }

            if (ConfigLoader.DebugLogs && !loggedOnce)
            {
                Debug.Log("[DisablePOIs] SleeperVolume.UpdatePlayerTouched patch is active (further calls silenced).");
                loggedOnce = true;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(BlockHazard), nameof(BlockHazard.IsHazardOn))]
    public class BlockHazard_IsHazardOn_Patch
    {
        static void Postfix(ref bool __result)
        {
            __result = false;
        }
    }

    [HarmonyPatch(typeof(BlockHazard), nameof(BlockHazard.GetLightValue))]
    public class BlockHazard_GetLightValue_Patch
    {
        static void Postfix(ref byte __result)
        {
            __result = 0;
        }
    }
}