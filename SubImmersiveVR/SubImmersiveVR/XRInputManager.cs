using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Harmony;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

namespace ImmersiveVR.Patchers
{
    internal class XRInputManager
    {
        private static readonly XRInputManager _instance = new XRInputManager();
        private List<InputDevice> xrDevices = new List<InputDevice>();
        private InputDeviceRole roles = InputDeviceRole.LeftHanded | InputDeviceRole.RightHanded;
        public InputDevice leftHand;
        public InputDevice rightHand;

        private XRInputManager()
        {
            GetDevices();
        }

        static XRInputManager GetXRInputManager()
        {
            return _instance;
        }

        void GetDevices()
        {
            InputDevices.GetDevices(xrDevices);
            foreach (InputDevice device in xrDevices)
            {
                if (device.role == InputDeviceRole.LeftHanded)
                {
                    leftHand = device;
                }
                if (device.role == InputDeviceRole.RightHanded)
                {
                    rightHand = device;
                }
            }
        }

        Vector2 Get(InputDevice device, InputFeatureUsage<Vector2> usage)
        {
            Vector2 value = Vector2.zero;
            if (device != null && device.isValid)
            {
                device.TryGetFeatureValue(usage, out value);
            } else
            {
                GetDevices();
            }
            return value;
        }

        float Get(InputDevice device, InputFeatureUsage<float> usage)
        {
            float value = 0f;
            if (device != null && device.isValid)
            {
                device.TryGetFeatureValue(usage, out value);
            }
            else
            {
                GetDevices();
            }
            return value;
        }

        [HarmonyPatch(typeof(GameInput), nameof(GameInput.GetControllerEnabled))]
        internal class GetControllerEnabledPatch
        {
            [HarmonyPostfix]
            public static void PostFix(ref bool __result)
            {
                __result = true;
            }
        }

        [HarmonyPatch(typeof(GameInput), "UpdateAxisValues")]
        internal class UpdateAxisValuesPatch
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                short step = 0;
                MethodInfo checkXR = typeof(UpdateAxisValuesPatch).GetMethod(nameof(UpdateAxisValuesPatch.CheckUseXRInput));
                MethodInfo getXRValues = typeof(UpdateAxisValuesPatch).GetMethod(nameof(UpdateAxisValuesPatch.GetXRAxisValues));
                MethodInfo ovrInputCheck = typeof(GameInput).GetMethod("GetUseOculusInputManager", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                FieldInfo axisValues = typeof(GameInput).GetField("axisValues", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                foreach (CodeInstruction instruction in instructions)
                {
                    if (step == 0)
                    {
                        // Replace OVRInputManager check with our own
                        if (instruction.opcode == OpCodes.Call && instruction.operand.Equals(ovrInputCheck))
                        {
                            step++;
                            yield return new CodeInstruction(OpCodes.Call, checkXR);
                        } else
                        {
                            yield return instruction;
                        }
                    }
                    else if (step == 1)
                    {
                        // Handle result on stack
                        step++;
                        yield return instruction;
                    } else if (step == 2)
                    {
                        // Load instance onto the stack
                        step++;
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                    }
                    else if (step == 3)
                    {
                        // Set axis values from XRInput
                        step++;
                        yield return new CodeInstruction(OpCodes.Call, getXRValues);
                    }
                    else if (step == 4)
                    {
                        // No-op existing axisValue assignments until end of conditional block
                        if (instruction.opcode == OpCodes.Br)
                        {
                            step++;
                            yield return instruction;
                        }
                        else
                        {
                            instruction.opcode = OpCodes.Nop;
                            yield return instruction;
                        }
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }

            public static bool CheckUseXRInput()
            {
                Console.WriteLine("CheckXRInput");
                return true;
            }

            private static Traverse axisValues;
            public static void GetXRAxisValues(GameInput instance)
            {
                if (axisValues == null)
                {
                    axisValues = Traverse.Create(instance).Field("axisValues");
                }
                XRInputManager xrInput = GetXRInputManager();
                float[] newValues = axisValues.GetValue<float[]>();
                Vector2 vector = xrInput.Get(xrInput.leftHand, CommonUsages.primary2DAxis);
                newValues[2] = vector.x;
                newValues[3] = -vector.y;
                Vector2 vector2 = xrInput.Get(xrInput.rightHand, CommonUsages.primary2DAxis);
                newValues[0] = vector2.x;
                newValues[1] = -vector2.y;
                newValues[4] = xrInput.Get(xrInput.leftHand, CommonUsages.trigger);
                newValues[5] = xrInput.Get(xrInput.rightHand, CommonUsages.trigger); ;
                newValues[6] = 0f;
                newValues[7] = 0f;
                axisValues.SetValue(newValues);
            }
        }
    }
}
