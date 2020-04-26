using UnityEngine;
using Harmony;
using UnityEngine.XR;
using System.Collections.Generic;
using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;

namespace SubMotionMovement.Patchers
{
    [RequireComponent(typeof(Camera))]
    public class MotionMovement : MonoBehaviour
    {
        private static MotionMovement _instance;
        private readonly List<XRNodeState> nodeStatesCache = new List<XRNodeState>();
        private GameObject _leftHand;
        private LineRenderer _leftHandLine;
        private Quaternion _cameraBackup = Quaternion.identity;
        public Camera mainCamera;

        public static MotionMovement Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject().AddComponent<MotionMovement>();
                    _instance.instantiate();
                }
                return _instance;
            }
        }

        public static Quaternion getHandRotation()
        {
            return Instance._leftHand.transform.rotation;
        }

        public void instantiate ()
        {
            _leftHand = new GameObject();
            _leftHandLine = new GameObject().AddComponent<LineRenderer>();
            mainCamera = Camera.main;
            Console.WriteLine("mainCamera: " + Convert.ToString(mainCamera));
            if (mainCamera)
            {
                Console.WriteLine("mainCamera name: " + mainCamera.name);
                Console.WriteLine("mainCamera id: " + mainCamera.GetInstanceID());
            }
            drawHandLine();
        }

        public void backupCameraTransform ()
        {
            Console.WriteLine("*******************************************");
            if (_cameraBackup == null)
            {
                Console.WriteLine("cameraBackup is null");
            }
            if (mainCamera == null)
            {
                Console.WriteLine("mainCamera is null");

            } else if (mainCamera.transform == null)
            {
                Console.WriteLine("camera transform is null");
            }
            Console.WriteLine("*******************************************");

            _cameraBackup = mainCamera.transform.rotation;
        }

        public void setCameraToHand ()
        {
            mainCamera.transform.rotation = _leftHand.transform.rotation;
        }

        public void restoreCamera ()
        {
            mainCamera.transform.rotation = _cameraBackup;
        }

        void updateHandRotation ()
        {
            Quaternion leftHandRotation;
            Vector3 leftHandPosition;
            InputTracking.GetNodeStates(nodeStatesCache);
            foreach (XRNodeState nodeState in nodeStatesCache)
            {
                if (nodeState.nodeType == XRNode.LeftHand)
                {
                    if (nodeState.TryGetRotation(out leftHandRotation)) {
                        _leftHand.transform.rotation = leftHandRotation;
                    }
                    if (nodeState.TryGetPosition(out leftHandPosition))
                    {
                        _leftHand.transform.position = leftHandPosition;
                    }
                    _leftHandLine.transform.rotation = leftHandRotation;
                    _leftHandLine.transform.position = leftHandPosition;

                }
            }
        }

        void drawHandLine ()
        {
            _leftHandLine.startColor = Color.red;
            _leftHandLine.endColor = Color.red;
            _leftHandLine.transform.localScale = new Vector3(1f, 1f, 100f);
            _leftHandLine.startWidth = 0.1f;
            _leftHandLine.endWidth = 0.1f;
        }

        void Update()
        {
            updateHandRotation();
        }

        [HarmonyPatch(typeof(UnderwaterMotor))]
        [HarmonyPatch("UpdateMove")]
        public static class UnderwatermotorUpdateMove
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo getHandRotation = typeof(UnderwatermotorUpdateMove).GetMethod(nameof(UnderwatermotorUpdateMove.TryThis));
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
                    // we cannot remove the first code of our range since some jump actually jumps to
                    // it, so we replace it with a no-op instead of fixing that jump (easier).
                    codes[startIndex].opcode = OpCodes.Nop;
                    codes[startIndex + 1].opcode = OpCodes.Nop;
                    codes[startIndex + 2].opcode = OpCodes.Nop;
                    codes[endIndex].opcode = OpCodes.Call;
                    codes[endIndex].operand = getHandRotation;
                }
                return codes.AsEnumerable();
            }
            public static Quaternion TryThis()
            {
                return MotionMovement.getHandRotation();
            }
        }

        //[HarmonyPatch(typeof(UnderwaterMotor), "UpdateMove")]
        //class UpdateMovePrefix
        //{
        //    static void Prefix(UnderwaterMotor __instance)
        //    {
        //        if (!XRSettings.enabled)
        //        {
        //            return;
        //        }
        //        Instance.backupCameraTransform();
        //        Instance.setCameraToHand();
        //    }
        //}

        //[HarmonyPatch(typeof(UnderwaterMotor), "UpdateMove")]
        //class UpdateMovePostfix
        //{
        //    static void PostFix(UnderwaterMotor __instance)
        //    {
        //        if (!XRSettings.enabled)
        //        {
        //            return;
        //        }
        //        Instance.restoreCamera();
        //    }
        //}
    }
}
