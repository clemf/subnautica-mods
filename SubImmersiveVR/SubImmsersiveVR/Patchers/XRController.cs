using UnityEngine;
using Harmony;
using UnityEngine.XR;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;

namespace ImmersiveVR.Patchers
{    public class XRController : MonoBehaviour
    {
        private static XRController _instance;
        private readonly List<XRNodeState> nodeStatesCache = new List<XRNodeState>();
        private GameObject leftHand;
        private LineRenderer leftHandLine;

        public static XRController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject().AddComponent<XRController>();
                    _instance.Instantiate();
                }
                return _instance;
            }
        }

        public void Instantiate ()
        {
            leftHand = new GameObject();
            leftHandLine = new GameObject().AddComponent<LineRenderer>();
            DrawHandLine();
        }

        void UpdateHandRotation ()
        {
            Quaternion leftHandRotation;
            Vector3 leftHandPosition;
            InputTracking.GetNodeStates(nodeStatesCache);
            foreach (XRNodeState nodeState in nodeStatesCache)
            {
                if (nodeState.nodeType == XRNode.LeftHand)
                {
                    if (nodeState.TryGetRotation(out leftHandRotation)) {
                        leftHand.transform.rotation = leftHandRotation;
                    }
                    if (nodeState.TryGetPosition(out leftHandPosition))
                    {
                        leftHand.transform.position = leftHandPosition;
                    }
                    leftHandLine.transform.rotation = leftHandRotation;
                    leftHandLine.transform.position = leftHandPosition;
                    leftHandLine.SetPosition(0, leftHandPosition);
                }
            }
        }

        void DrawHandLine ()
        {
            leftHandLine.startColor = Color.red;
            leftHandLine.endColor = Color.red;
            leftHandLine.transform.localScale = new Vector3(1f, 1f, 100f);
            leftHandLine.startWidth = 0.1f;
            leftHandLine.endWidth = 0.1f;
        }

        void Update()
        {
            UpdateHandRotation();
        }

        [HarmonyPatch(typeof(UnderwaterMotor))]
        [HarmonyPatch("UpdateMove")]
        public static class UnderwatermotorUpdateMove
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo getHandRotation = typeof(UnderwatermotorUpdateMove).GetMethod(nameof(UnderwatermotorUpdateMove.GetHandRotation));
                int startIndex = -1;
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
                                }
                            }
                        }
                    }
                }
                if (startIndex > -1)
                {
                    codes[startIndex].opcode = OpCodes.Nop;
                    codes[startIndex + 1].opcode = OpCodes.Nop;
                    codes[startIndex + 2].opcode = OpCodes.Nop;
                    codes[startIndex + 3].opcode = OpCodes.Call;
                    codes[startIndex + 3].operand = getHandRotation;
                }
                return codes.AsEnumerable();
            }
            public static Quaternion GetHandRotation()
            {
                return XRController.Instance.leftHand.transform.rotation;
            }
        }
    }
}
