using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.DualShock.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem;
using UnityEngine.Scripting;
using UniSense.LowLevel;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.Scripting;
using System.Collections.Generic;
using UniSense.LowLevel;
using UniSense;
namespace UnityEngine.InputSystem.DualSense
{

#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class DualSenseSupport
    {

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            InputSystem.RegisterLayout<UnisenseDualSenseGamepad>();

            InputSystem.RegisterLayout<DualSenseUSBGamepadHID>(
                matches: new InputDeviceMatcher()
                .WithInterface("HID")
                .WithManufacturer("Sony.+Entertainment")
                .WithCapability("vendorId", 0x54C)
                .WithCapability("inputReportSize", 64)
                .WithCapability("productId", 0xCE6));
            InputSystem.RegisterLayout<DualSenseBTGamepadHID>(
                matches: new InputDeviceMatcher()
                .WithInterface("HID")
                .WithManufacturer("Sony.+Entertainment")
                .WithCapability("vendorId", 0x54C)
                .WithCapability("inputReportSize", 78)
                .WithCapability("productId", 0xCE6));
        }
    }
}
