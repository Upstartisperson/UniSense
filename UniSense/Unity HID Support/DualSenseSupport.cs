using UnityEngine.InputSystem.Layouts;
using UnityEditor;
using System;
using UnityEngine.Scripting;
using UnityEngine.InputSystem;





using UniSense;
namespace UnityEngine.InputSystem.DualSense
{
    [Preserve]
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    [ExecuteInEditMode]
    public static class DualSenseSupport
    {
        [SerializeReference] internal static bool _initiliezed = false;
        #if UNITY_EDITOR
        static DualSenseSupport()
        {
            if (_initiliezed) return;
            Initialize();
        }
        #endif
        
        [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Initialize()
        { 
            if(_initiliezed) return ;
            _initiliezed=true;
            InputSystem.RegisterProcessor<InvertBytesKeepBits>();


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

            //InputSystem.RegisterLayoutOverride(@"
            //{
            //    ""name"" : ""GamepadPlayerUsageTags"",
            //    ""extend"" : ""UnisenseDualSenseGamepad"",
            //    ""commonUsages"" : [
            //        ""Player1"", ""Player2""
            //    ]
            //}
            //");
        }
        
    }

    
    public class InvertBytesKeepBits : InputProcessor<uint>
    {
        public override uint Process(uint value, InputControl control)
        {
            byte[] bytes =  BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }
    }
}


