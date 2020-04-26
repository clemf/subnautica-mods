using System.Reflection;
using Harmony;

namespace MotionMovement
{
    public static class MainPatcher
    {
        public static void Patch()
        {
            var harmony = HarmonyInstance.Create("com.test.subnautica.vrmotion");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}