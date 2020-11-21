using UnityEngine;
using RootMotion.FinalIK;
using HarmonyLib;
using UnityEngine.XR;
using System;

namespace ImmersiveVR
{
    class XRArmsController
    {
        private static readonly XRArmsController _instance = new XRArmsController();
        public ArmsController armsController;
        public Player player;
        public FullBodyBipedIK ik;

        string editSide = "left";
        string editType = "position";
        string editCoord = "x";

        // 0f, -0.13f, -0.14f
        public float leftPosX = 0f;
        public float leftPosY = -0.13f;
        public float leftPosZ = -0.14f;

        // 270f, 90f, 0f
        public float leftRotX = 270f;
        public float leftRotY = 90f;
        public float leftRotZ = 0f;

        // 0f, -0.13f, -0.14f
        public float rightPosX = 0f;
        public float rightPosY = -0.13f;
        public float rightPosZ = -0.14f;

        // 35f, 190f, 270f
        public float rightRotX = 35f;
        public float rightRotY = 190f;
        public float rightRotZ = 270f;

        private XRArmsController()
        {
        }

        static XRArmsController GetXRArmsController()
        {
            return _instance;
        }

        public void Initialize(ArmsController controller)
        {
            armsController = controller;
            player = Utils.GetLocalPlayerComp();
            ik = controller.GetComponent<FullBodyBipedIK>();
        }

        public void setEdit()
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKey(KeyCode.Z))
                {
                    editCoord = "x";
                }
                if (Input.GetKey(KeyCode.X))
                {
                    editCoord = "y";
                }
                if (Input.GetKey(KeyCode.C))
                {
                    editCoord = "z";
                }
                if (Input.GetKey(KeyCode.A))
                {
                    editType = "position";
                }
                if (Input.GetKey(KeyCode.S))
                {
                    editType = "rotation";
                }
                if (Input.GetKey(KeyCode.Q))
                {
                    editSide = "left";
                }
                if (Input.GetKey(KeyCode.E))
                {
                    editSide = "right";
                }
            }
        }

        public void editHandCoord()
        {
            if (Input.GetKey(KeyCode.RightControl))
            {
                if(Input.GetKey(KeyCode.UpArrow))
                {
                    setEditCoord(0.1f);
                }
                if(Input.GetKey(KeyCode.DownArrow))
                {
                    setEditCoord(-0.1f);
                }
            }
        }

        public void printHandCoord()
        {
            Console.WriteLine("left position:");
            Console.WriteLine(leftPosX.ToString() + " " + leftPosY.ToString() + " " + leftPosZ.ToString());
            Console.WriteLine("left rotation:");
            Console.WriteLine(leftRotX.ToString() + " " + leftRotY.ToString() + " " + leftRotZ.ToString());
            Console.WriteLine("right position:");
            Console.WriteLine(rightPosX.ToString() + " " + rightPosY.ToString() + " " + rightPosZ.ToString());
            Console.WriteLine("right rotation:");
            Console.WriteLine(rightRotX.ToString() + " " + rightRotY.ToString() + " " + rightRotZ.ToString());
        }

        public void setEditCoord(float edit)
        {
            if (editSide == "left")
            {
                if (editType == "position")
                {
                    if ( editCoord == "x")
                    {
                        leftPosX += edit;
                    }
                    if (editCoord == "y")
                    {
                        leftPosY += edit;
                    }
                    if (editCoord == "z")
                    {
                        leftPosZ += edit;
                    }
                }
                if (editType == "rotation")
                {
                    if (editCoord == "x")
                    {
                        leftRotX += edit;
                    }
                    if (editCoord == "y")
                    {
                        leftRotY += edit;
                    }
                    if (editCoord == "z")
                    {
                        leftRotZ += edit;
                    }
                }
            }
            if (editSide == "right")
            {
                if (editType == "position")
                {
                    if (editCoord == "x")
                    {
                        rightPosX += edit;
                    }
                    if (editCoord == "y")
                    {
                        rightPosY += edit;
                    }
                    if (editCoord == "z")
                    {
                        rightPosZ += edit;
                    }
                }
                if (editType == "rotation")
                {
                    if (editCoord == "x")
                    {
                        rightRotX += edit;
                    }
                    if (editCoord == "y")
                    {
                        rightRotY += edit;
                    }
                    if (editCoord == "z")
                    {
                        rightRotZ += edit;
                    }
                }
            }
        }

        public void UpdateHandPositions()
        {
            setEdit();
            editHandCoord();
            printHandCoord();

            XRInputManager xrInput = XRInputManager.GetXRInputManager();
            Vector3 leftHandPos = xrInput.Get(Controller.Left, CommonUsages.devicePosition);
            Quaternion leftHandRot = xrInput.Get(Controller.Left, CommonUsages.deviceRotation);
            Vector3 rightHandPos = xrInput.Get(Controller.Right, CommonUsages.devicePosition);
            Quaternion rightHandRot = xrInput.Get(Controller.Right, CommonUsages.deviceRotation);
            
            Transform rightHand = xrInput.GetGameTransform(Controller.Right);
            rightHand.localPosition += new Vector3(rightPosX, rightPosY, rightPosZ);
            rightHand.localRotation *= Quaternion.Euler(rightRotX, rightRotY, rightRotZ);
            ik.solver.rightHandEffector.target = rightHand;
            Transform leftHand = xrInput.GetGameTransform(Controller.Left);
            leftHand.localPosition += new Vector3(leftPosX, leftPosY, leftPosZ);
            leftHand.localRotation *= Quaternion.Euler(leftRotX, leftRotY, leftRotZ);
            ik.solver.leftHandEffector.target = leftHand;
        }

        [HarmonyPatch(typeof(ArmsController))]
        [HarmonyPatch("Start")]
        class ArmsController_Start_Patch
        {
            [HarmonyPostfix]
            public static void PostFix(ArmsController __instance)
            {
                if (!XRSettings.enabled)
                {
                    return;
                }

                GetXRArmsController().Initialize(__instance);
            }
        }

        [HarmonyPatch(typeof(ArmsController))]
        [HarmonyPatch("Update")]
        class ArmsController_Update_Patch
        {

            [HarmonyPostfix]
            public static void Postfix()
            {
                if (!XRSettings.enabled)
                {
                    return;
                }
                XRArmsController xrArms = GetXRArmsController();

                Player player = xrArms.player;

                if ((Player.main.motorMode != Player.MotorMode.Vehicle && !player.cinematicModeActive))
                {
                    xrArms.UpdateHandPositions();
                }
            }
        }
    }
}