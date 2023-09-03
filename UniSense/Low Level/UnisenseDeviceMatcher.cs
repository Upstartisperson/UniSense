using System.Collections;
using System.Collections.Generic;
//using UnityEngine
using UniSense.LowLevel;
using UnityEngine.InputSystem;

//TODO: make sure BT or USB can be connected first
namespace UniSense.LowLevel.Matching
{


    public static class UnisenseDeviceMatcher
    {
        private static List<DeviceMatcher> _devicesToMatch = new List<DeviceMatcher>();

        public static void Initailize()
        {
            Enable();
        }



        /// <summary>
        /// Starts the class auto update
        /// </summary>
        private static void Enable()
        {
            InputSystem.onAfterUpdate += InputUpdate;
        }

        private static void InputUpdate()
        {
           if(_devicesToMatch.Count < 0) { Disable(); return; }
           if (_devicesToMatch[0].UpdatesElapsed++ < _devicesToMatch[0].UpdateDelay) return;
           DeviceMatcher deviceToMatch = _devicesToMatch[0];
            int matchIndex = 0;
            switch (_devicesToMatch[0].DeviceType)
            {
                case DeviceType.DualSenseBT:
                    if (!LookForUSBPair(deviceToMatch, out matchIndex))
                    {
                        
                    }
                        
                    break;
                case DeviceType.DualSenseUSB:
                    if(!LookForBTPair(deviceToMatch, out matchIndex))  return;


                    break;
                default:
                    break;
            }
        }

        private static bool LookForUSBPair(DeviceMatcher deviceMatcher, out int indexResult)
        {
            indexResult = 0;
            return false;
        }
        private static bool LookForBTPair(DeviceMatcher deviceMatcher, out int indexResult)
        {
            indexResult = 0;
            return false;
        }

        /// <summary>
        /// Ends the class auto update
        /// </summary>
        private static void Disable()
        {
            InputSystem.onAfterUpdate -= InputUpdate;
        }


    }

    public class DeviceMatcher
    {
        public DeviceType DeviceType { get; private set; }

        public string Key { get; private set; }

        public InputDevice InputDevice { get; private set; }

        public int UpdateDelay { get; private set; }

        public int UpdatesElapsed;
    }

}