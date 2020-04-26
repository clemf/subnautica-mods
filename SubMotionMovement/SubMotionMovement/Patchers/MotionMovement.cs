using UnityEngine;
using Harmony;
using UnityEngine.XR;
using System.Collections.Generic;
using System;

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

        void getHandRotation ()
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
            getHandRotation();
        }

        [HarmonyPatch(typeof(UnderwaterMotor), "UpdateMove")]
        class UpdateMovePrefix
        {
            static void Prefix(UnderwaterMotor __instance)
            {
                if (!XRSettings.enabled)
                {
                    return;
                }
                Instance.backupCameraTransform();
                Instance.setCameraToHand();
            }
        }

        [HarmonyPatch(typeof(UnderwaterMotor), "UpdateMove")]
        class UpdateMovePostfix
        {
            static void PostFix(UnderwaterMotor __instance)
            {
                if (!XRSettings.enabled)
                {
                    return;
                }
                Instance.restoreCamera();
            }
        }
    }
}
