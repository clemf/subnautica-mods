using QModManager.API.ModLoading;
using HarmonyLib;

namespace ImmersiveVR
{
    // Your main patching class must have the QModCore attribute (and must be public)
    [QModCore]
    public static class MainPatcher
    {
        // Your patching method must have the QModPatch attribute (and must be public)
        [QModPatch]
        public static void Patch()
        {
            // Add your patching code here
            Harmony harmony = new Harmony("com.datoo.subnautica.vrmotion");
            harmony.PatchAll();
        }
    }
}