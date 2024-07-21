using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UniSense.pair
{


  
    /// <summary>
    /// This class is used to allow devices time to fully initialize. 
    /// USB DualSense gamepads can be placed in here for a set number of input system updates. 
    /// This class then automatically sends the fully initialized devices to the <see cref="UnisensePair"/> Class
    /// </summary>
    public  class PairQueue
    {
        const char QueueTime = (char)10;
        private  List<DualSenseUSBGamepadHID> deviceQueue = new List<DualSenseUSBGamepadHID>();
        private  List<char> deviceTime = new List<char>();

        public void Update()
        {
            if (deviceTime.Count == 0) return;
            if(deviceTime[0] >= QueueTime)
            {
                UnisensePair.TryPair(deviceQueue[0]);
                deviceQueue.RemoveAt(0);
                deviceTime.RemoveAt(0);
                Update();
                return;
            }

            for (int i = 0; i < deviceTime.Count; i++)
            {
                deviceTime[i]++;
            }
            
        }

        public void BeginPairQueue(DualSenseUSBGamepadHID device)
        {
            deviceQueue.Add(device);
            deviceTime.Add((char)0);
        }

    }
}

