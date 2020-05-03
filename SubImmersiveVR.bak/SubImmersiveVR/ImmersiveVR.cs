using System.Reflection;
using Harmony;

namespace ImmersiveVR
{
    public static class MainPatcher
    {
        public static void Patch()
        {
            var harmony = HarmonyInstance.Create("com.datoo.subnautica.vrmotion");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}