using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UniSense.LowLevel;
using UniSense;
using UniSense.DS5WWrapper;
using UnityEditor;


//Need to be able to call a method to queue the connection of a device
//With BT I first need to set up on DS5W side then enable the unity input device finally set it up in UnisenseConnectionHandler
//With USB I guess just enable? not sure if I will disable it by default thou
//BT will be disabled by default because the multi-player join action (options button) will appear to be pressed (when not) if dual sense hasn't been set up in DS5W
                        //need to read feature report 0x05 in order to set up BT device's full input report
                        //I think it probably also enables output report but the output report is sent with DS5W so can't test that easily and also moot point
//The responsibility of this class will be to update every input update and wait for a set number of updates to allow for the device to be properly set up in windows and unity
//Needs to pause when no devices are queued for connection
//Needs(?) also an immediate way to open connection (for reopening connections when the device is already initialized)
//I could actually call this script from the Game pad script
    //Pros: simplifies UnisenseConnectionHandler
//If device is removed while connection is cued I could use hash map to locate the device in the list but, maintain and storing that hash map won't be worthwhile considering 
    //this edge case is very rare. So I will just iterate over the list and find the specific device
//TODO: add error handling to make BT go generic if DS5W fails

namespace UniSense.DeviceConnector
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class UnisenseDeviceConnector
    {
        #region Initialization

        //Ensure that this class is destroyed, and subscribe to on device change
        static UnisenseDeviceConnector()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChange;
            EditorApplication.wantsToQuit += Destroy;
            InputSystem.onDeviceChange += OnDeviceChanged;
           
        }
     
        /// <summary>
        /// Is initialized Automatically. 
        /// Waits for a predetermined delay then adds all detected Gamepads to a list of device that want to connect. 
        /// Finally enables update
        /// </summary>
        [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            if (_initializationDelayTimer == 0) InputSystem.onAfterUpdate += Initialize;
            if (_initializationDelayTimer++ < _initializationDelay) return;
            InputSystem.onAfterUpdate -= Initialize;
            _initialized = true;
            InputDevice[] devices = InputSystem.devices.ToArray();
            foreach (InputDevice device in devices)
            {
                if(device is Gamepad)
                {
                    _devices.Add(device);
                }
            }
            EnableUpdate();
        } 
        #endregion

        #region Destruction
        private static bool Destroy()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChange;
            EditorApplication.wantsToQuit -= Destroy;
            InputSystem.onDeviceChange -= OnDeviceChanged;
            return true;
        }
        private static void OnPlayModeStateChange(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.ExitingPlayMode) Destroy();
        }
        #endregion

       
        private static List<UnisenseDevice> _unsinseDevices { get { return UnisenseDevice.Devices; } }
        private static List<InputDevice> _devices = new();
        private static bool _updateEnabled = false;
        private static bool _initialized = false;
        private const int _initializationDelay = 50; //Verified worked in prototype
        private const int _deviceInitializationDelay = 5; //Pulled directly from ass
        private static int _deviceDelayTimer = 0;
        private static int _initializationDelayTimer = 0;
       



        #region EntryPoints
        private static void OnDeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (!(device is Gamepad)) return;
            switch (change)
            {
                case InputDeviceChange.Added:
                    _devices.Add(device);
                    EnableUpdate();
                    break;

                case InputDeviceChange.Removed:
                    //TODO: Add Removal Logic
                    
                    break;
            }
        }
        private static void InputUpdate()
        {
            if(_devices.Count < 0)
            {
                DisableUpdate();
                return;
            }

            if (_deviceDelayTimer++ < _deviceInitializationDelay) return;
            InputDevice device = _devices[0];

            switch (device)
            {
                case DualSenseBTGamepadHID:
                    if (_deviceDelayTimer - 1 == _deviceInitializationDelay)
                    {
                        DeviceEnumInfo enumInfo = new DeviceEnumInfo();
                        DS5W_ReturnValue status = (OSType.Type == OS_Type._x64) ? DS5W_x64.findDevice(ref enumInfo, device.description.serial) :
                                                                                  DS5W_x86.findDevice(ref enumInfo, device.description.serial);
                        if (status != DS5W_ReturnValue.OK)
                        {
                            //TODO : Handle Error and convert device to minimal report if can or maybey try somthing else?
                            Debug.LogError(status.ToString());
                            _devices.RemoveAt(0);
                            return;
                        }
                        return;
                    }
                    UnisenseDevice.CreateDevice(device);
                    _devices.RemoveAt(0);
                    Debug.Log("Device Created");
                    _deviceDelayTimer = 0;
                    break;
                case DualSenseUSBGamepadHID:
                    UnisenseDevice.CreateDevice(device);
                    _devices.RemoveAt(0);
                    Debug.Log("Device Created");
                    _deviceDelayTimer = 0;
                    break;
                case Gamepad:
                    UnisenseDevice.CreateDevice(device);
                    _devices.RemoveAt(0);
                    Debug.Log("Device Created");
                    _deviceDelayTimer = 0;
                    break;
            }
        }
        #endregion

        #region Helpers
        private static void EnableUpdate()
        {
            if(_updateEnabled) return;
            InputSystem.onAfterUpdate += InputUpdate;
            _updateEnabled = true;
        }
        private static void DisableUpdate()
        {
            if (!_updateEnabled) return;
            InputSystem.onAfterUpdate -= InputUpdate;
            _updateEnabled = false;
        }

        private static void QueueDeviceInitialization(InputDevice device)
        {
          

        }
        #endregion
    }

   
}
