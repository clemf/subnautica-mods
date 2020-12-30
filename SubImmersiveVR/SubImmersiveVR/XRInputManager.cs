using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using HarmonyLib;
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

        private XRInputManager()
        {
            GetDevices();
        }

        public static XRInputManager GetXRInputManager()
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
            switch (name)
            {
                case Controller.Left:
                    return leftController;
                case Controller.Right:
                    return rightController;
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
            }
            else
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
            return value;
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
            return value;
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

        public bool Get(Controller controller, InputFeatureUsage<bool> usage)
        {
            InputDevice device = GetDevice(controller);
            bool value = false;
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

        public bool GetXRInput(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.JoystickButton0:
                    // ControllerButtonA
                    return Get(Controller.Right, CommonUsages.primaryButton);
                case KeyCode.JoystickButton1:
                    // ControllerButtonB
                    return Get(Controller.Right, CommonUsages.secondaryButton);
                case KeyCode.JoystickButton2:
                    // ControllerButtonX
                    return Get(Controller.Left, CommonUsages.primaryButton);
                case KeyCode.JoystickButton3:
                    // ControllerButtonY
                    return Get(Controller.Left, CommonUsages.secondaryButton);
                case KeyCode.JoystickButton4:
                    // ControllerButtonLeftBumper
                    return Get(Controller.Left, CommonUsages.gripButton);
                case KeyCode.JoystickButton5:
                    // ControllerButtonRightBumper
                    return Get(Controller.Right, CommonUsages.gripButton);
                case KeyCode.JoystickButton6:
                    // ControllerButtonBack - reservered by "oculus" button
                    return false;
                case KeyCode.JoystickButton7:
                    // ControllerButtonHome
                    return Get(Controller.Left, CommonUsages.menuButton);
                case KeyCode.JoystickButton8:
                    // ControllerButtonLeftStick
                    return Get(Controller.Left, CommonUsages.primary2DAxisClick);
                case KeyCode.JoystickButton9:
                    // ControllerButtonRightStick
                    return Get(Controller.Right, CommonUsages.primary2DAxisClick);
                default:
                    return false;
            }
        }

        [HarmonyPatch(typeof(GameInput), "UpdateAxisValues")]
        internal class UpdateAxisValuesPatch
        {
            public static bool Prefix(bool useKeyboard, bool useController, GameInput ___instance)
            {
                XRInputManager xrInput = GetXRInputManager();
                if (!xrInput.hasControllers())
                {
                    return true;
                }

                for (int i = 0; i < GameInput.axisValues.Length; i++)
                {
                    GameInput.axisValues[i] = 0f;
                }
                if (useController)
                {
                    Vector2 vector = xrInput.Get(Controller.Left, CommonUsages.primary2DAxis);
                    GameInput.axisValues[2] = vector.x;
                    GameInput.axisValues[3] = -vector.y;
                    Vector2 vector2 = xrInput.Get(Controller.Right, CommonUsages.primary2DAxis);
                    GameInput.axisValues[0] = vector2.x;
                    GameInput.axisValues[1] = -vector2.y;
                    // TODO: Use deadzone?
                    GameInput.axisValues[4] = xrInput.Get(Controller.Left, CommonUsages.trigger).CompareTo(0.3f);
                    GameInput.axisValues[5] = xrInput.Get(Controller.Right, CommonUsages.trigger).CompareTo(0.3f);
                }
                if (useKeyboard)
                {
                    GameInput.axisValues[10] = Input.GetAxis("Mouse ScrollWheel");
                    GameInput.axisValues[8] = Input.GetAxisRaw("Mouse X");
                    GameInput.axisValues[9] = Input.GetAxisRaw("Mouse Y");
                }
                for (int j = 0; j < GameInput.axisValues.Length; j++)
                {
                    GameInput.AnalogAxis axis = (GameInput.AnalogAxis)j;
                    GameInput.Device deviceForAxis = ___instance.GetDeviceForAxis(axis);
                    float f = GameInput.lastAxisValues[j] - GameInput.axisValues[j];
                    GameInput.lastAxisValues[j] = GameInput.axisValues[j];
                    if (deviceForAxis != GameInput.lastDevice)
                    {
                        float num = 0.1f;
                        if (Mathf.Abs(f) > num)
                        {
                            if (!PlatformUtils.isConsolePlatform)
                            {
                                GameInput.lastDevice = deviceForAxis;
                            }
                        }
                        else
                        {
                            GameInput.axisValues[j] = 0f;
                        }
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(GameInput), "UpdateKeyInputs")]
        internal class UpdateKeyInputsPatch {
            public static bool Prefix(bool useKeyboard, bool useController)
            {
                XRInputManager xrInput = GetXRInputManager();
                if (!xrInput.hasControllers())
                {
                    return true;
                }

                float unscaledTime = Time.unscaledTime;
                for (int i = 0; i < GameInput.inputs.Count; i++)
                {
                    GameInput.InputState inputState = default;
                    GameInput.InputState prevInputState = GameInput.inputStates[i];
                    inputState.timeDown = prevInputState.timeDown;
                    bool wasHeld = (prevInputState.flags & GameInput.InputStateFlags.Held) > 0U;

                    GameInput.Input currentInput = GameInput.inputs[i];
                    GameInput.Device device = currentInput.device;
                    KeyCode key = currentInput.keyCode;

                    if (key != KeyCode.None)
                    {
                        bool pressed = xrInput.GetXRInput(key);
                        GameInput.InputStateFlags prevState = GameInput.inputStates[i].flags;
                        if (pressed && (prevState == GameInput.InputStateFlags.Held && prevState == GameInput.InputStateFlags.Down))
                        {
                            inputState.flags |= GameInput.InputStateFlags.Held;
                        }
                        if (pressed && prevState == GameInput.InputStateFlags.Up)
                        {
                            inputState.flags |= GameInput.InputStateFlags.Down;
                        }
                        if (!pressed)
                        {
                            inputState.flags |= GameInput.InputStateFlags.Up;
                        }
                        if (inputState.flags != 0U && !PlatformUtils.isConsolePlatform && (GameInput.controllerEnabled || device != GameInput.Device.Controller))
                        {
                            GameInput.lastDevice = device;
                        }
                    }
                    else
                    {
                        float axisValue = GameInput.axisValues[(int)currentInput.axis];
                        bool isPressed;
                        if (GameInput.inputs[i].axisPositive)
                        {
                            isPressed = (axisValue > currentInput.axisDeadZone);
                        }
                        else
                        {
                            isPressed = (axisValue < -currentInput.axisDeadZone);
                        }
                        if (isPressed)
                        {
                            inputState.flags |= GameInput.InputStateFlags.Held;
                        }
                        if (isPressed && !wasHeld)
                        {
                            inputState.flags |= GameInput.InputStateFlags.Down;
                        }
                        if (!isPressed && wasHeld)
                        {
                            inputState.flags |= GameInput.InputStateFlags.Up;
                        }
                    }

                    if ((inputState.flags & GameInput.InputStateFlags.Down) != 0U)
                    {
                        int lastIndex = GameInput.lastInputPressed[(int)device];
                        int newIndex = i;
                        inputState.timeDown = unscaledTime;
                        if (lastIndex > -1)
                        {
                            GameInput.Input lastInput = GameInput.inputs[lastIndex];
                            bool isSameTime = inputState.timeDown == GameInput.inputStates[lastIndex].timeDown;
                            bool lastAxisIsGreater = Mathf.Abs(GameInput.axisValues[(int)lastInput.axis]) > Mathf.Abs(GameInput.axisValues[(int)currentInput.axis]);
                            if (isSameTime && lastAxisIsGreater)
                            {
                                newIndex = lastIndex;
                            }
                        }
                        GameInput.lastInputPressed[(int)device] = newIndex;
                    }

                    if ((device == GameInput.Device.Controller && !useController) || (device == GameInput.Device.Keyboard && !useKeyboard))
                    {
                        inputState.flags = 0U;
                        if (wasHeld)
                        {
                            inputState.flags |= GameInput.InputStateFlags.Up;
                        }  
                    }
                    GameInput.inputStates[i] = inputState;
                }

                return false;
            }
        }
    }
}
