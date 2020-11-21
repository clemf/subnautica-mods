using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System;

namespace ImmersiveVR
{
    enum Controller
    {
        Left,
        Right
    }

    internal class XRInputManager
    {
        private static readonly XRInputManager _instance = new XRInputManager();
        private readonly List<InputDevice> xrDevices = new List<InputDevice>();
        public InputDevice leftController;
        public InputDevice rightController;
        public GameObject gameLeftHand;
        public GameObject gameRightHand;
        public Player player;

        private XRInputManager()
        {
            GetDevices();
        }

        public static XRInputManager GetXRInputManager()
        {
            if (_instance.player == null)
            {
                _instance.TryGetPlayer();
            }
            return _instance;
        }

        void TryGetPlayer()
        {
            player = Utils.GetLocalPlayerComp();
            if (player != null)
            {
                gameRightHand = new GameObject("gameRightHand");
                gameRightHand.transform.parent = player.camRoot.transform;
                gameLeftHand = new GameObject("gameLeftHand");
                gameLeftHand.transform.parent = player.camRoot.transform;
            }
        }

        void GetDevices()
        {
            InputDevices.GetDevices(xrDevices);
            foreach (InputDevice device in xrDevices)
            {
                if (device.role == InputDeviceRole.LeftHanded)
                {
                    leftController = device;
                }
                if (device.role == InputDeviceRole.RightHanded)
                {
                    rightController = device;
                }
            }
        }

        InputDevice GetDevice(Controller name)
        {
            switch (name) {
                case Controller.Left:
                    return leftController;
                case Controller.Right:
                    return rightController;
                default: throw new Exception();
            }
        }

        public Transform GetGameTransform(Controller name)
        {
            switch (name)
            {
                case Controller.Left:
                    return gameLeftHand.transform;
                case Controller.Right:
                    return gameRightHand.transform;
                default: throw new Exception();
            }
        }

        public Vector2 Get(Controller controller, InputFeatureUsage<Vector2> usage)
        {
            InputDevice device = GetDevice(controller);
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

        public Vector3 Get(Controller controller, InputFeatureUsage<Vector3> usage)
        {
            InputDevice device = GetDevice(controller);
            Vector3 value = Vector3.zero;
            if (device != null && device.isValid)
            {
                device.TryGetFeatureValue(usage, out value);
            }
            else
            {
                GetDevices();
            }
            Transform parentTransform = GetGameTransform(controller);
            parentTransform.localPosition = value;
            return parentTransform.position;
        }

        public Quaternion Get(Controller controller, InputFeatureUsage<Quaternion> usage)
        {
            InputDevice device = GetDevice(controller);
            Quaternion value = Quaternion.identity;
            if (device != null && device.isValid)
            {
                device.TryGetFeatureValue(usage, out value);
            }
            else
            {
                GetDevices();
            }
            // Sets the rotation of hand input relative to orientation of in-game player
            Transform parentTransform = GetGameTransform(controller);
            parentTransform.localRotation = value;
            return parentTransform.rotation;
        }

        public float Get(Controller controller, InputFeatureUsage<float> usage)
        {
            InputDevice device = GetDevice(controller);
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

        public bool hasControllers()
        {
            bool hasController = false;
            if (leftController != null && leftController.isValid)
            {
                hasController = true;
            }
            if (rightController != null && rightController.isValid)
            {
                hasController = true;
            }
            return hasController;
        }

        [HarmonyPatch(typeof(VRUtil), nameof(VRUtil.GetLoadedSDK))]
        internal class VRUtilPatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                Console.WriteLine("XRSettings loadedDeviceName: " + XRSettings.loadedDeviceName);
                Console.WriteLine("XRSettings stereoRenderingMode: " + XRSettings.stereoRenderingMode);
                Console.WriteLine("XRSettings supportedDevices:");
                foreach (string device in XRSettings.supportedDevices) {
                    Console.WriteLine(device);
                }
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
                XRInputManager xrInput = GetXRInputManager();
                return xrInput.hasControllers();
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
                Vector2 vector = xrInput.Get(Controller.Left, CommonUsages.primary2DAxis);
                newValues[2] = vector.x;
                newValues[3] = -vector.y;
                Vector2 vector2 = xrInput.Get(Controller.Right, CommonUsages.primary2DAxis);
                newValues[0] = vector2.x;
                newValues[1] = -vector2.y;
                newValues[4] = xrInput.Get(Controller.Left, CommonUsages.trigger);
                newValues[5] = xrInput.Get(Controller.Right, CommonUsages.trigger); ;
                newValues[6] = 0f;
                newValues[7] = 0f;
                axisValues.SetValue(newValues);
            }
        }

        [HarmonyPatch(typeof(UnderwaterMotor))]
        [HarmonyPatch("UpdateMove")]
        public static class UnderwatermotorUpdateMove
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo getHandRotation = typeof(UnderwatermotorUpdateMove).GetMethod(nameof(UnderwatermotorUpdateMove.GetHandRotation));
                int startIndex = -1, endIndex = -1;
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldarg_0)
                    {
                        if (codes[i + 1].opcode == OpCodes.Ldfld && codes[i + 1].operand is FieldInfo fieldInfo && fieldInfo.Name == nameof(UnderwaterMotor.playerController))
                        {
                            if (codes[i + 2].opcode == OpCodes.Callvirt && codes[i + 2].operand is MethodInfo methodInfo1 && methodInfo1.Name == "get_forwardReference")
                            {
                                if (codes[i + 3].opcode == OpCodes.Callvirt && codes[i + 3].operand is MethodInfo methodInfo2 && methodInfo2.Name == "get_rotation")
                                {
                                    startIndex = i;
                                    endIndex = i + 3;
                                }
                            }
                        }
                    }
                }
                if (startIndex > -1 && endIndex > -1)
                {
                    codes[startIndex].opcode = OpCodes.Nop;
                    codes[startIndex + 1].opcode = OpCodes.Nop;
                    codes[startIndex + 2].opcode = OpCodes.Nop;
                    codes[endIndex].opcode = OpCodes.Call;
                    codes[endIndex].operand = getHandRotation;
                }
                return codes.AsEnumerable();
            }
            public static Quaternion GetHandRotation()
            {
                XRInputManager xrInput = GetXRInputManager();
                return xrInput.Get(Controller.Left, CommonUsages.deviceRotation);
            }
        }
    }
}
