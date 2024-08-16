using System.Collections.Generic;
using UnityEngine.InputSystem;


namespace UniSense.pair
{
    /// <summary>
    /// This class is used to allow devices time to fully initialize. 
    /// USB DualSense gamepads can be placed in here for a set number of input system updates. 
    /// This class then automatically sends the fully initialized devices to the <see cref="UnisensePair"/> Class
    /// </summary>
    public class PairQueue
    {
        const char QueueTime = (char)10; //How many input-update cycles should all devices should be held in the queue
        private  List<DualSenseUSBGamepadHID> deviceQueue = new List<DualSenseUSBGamepadHID>();
        private  List<char> deviceTime = new List<char>();

        #region Initialization And Shutdown
        public PairQueue()
        {
            InputSystem.onAfterUpdate += Update;
        }

        /// <summary>
        /// <see cref="Exit"/> Needs to always be called before the application stops playing
        /// </summary>
        public void Exit()
        {
            InputSystem.onAfterUpdate -= Update;
        }

        #endregion

        /// <summary>
        /// Update the device queue
        /// </summary>
        private void Update()
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

        /// <summary>
        /// Place a new <see cref="DualSenseUSBGamepadHID"/> in the device queue
        /// </summary>
        /// <param name="device"></param>
        public void QueueDevice(DualSenseUSBGamepadHID device)
        {
            deviceQueue.Add(device);
            deviceTime.Add((char)0);

        }

        /// <summary>
        /// Attempt to remove a device in the queue. ONLY call when a device that is in the queue has been disconnected from the system
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public bool TryRemoveQueue(DualSenseUSBGamepadHID device)
        {
            for (int i = 0; i < deviceQueue.Count; i++)
            {
                if (deviceQueue[i] == device)
                {
                    deviceQueue.RemoveAt(i);
                    deviceTime.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

    }
}

