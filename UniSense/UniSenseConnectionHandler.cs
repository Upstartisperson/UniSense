using UniSense;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UniSense.LowLevel;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.LowLevel;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using DS5W;

namespace UniSense.Connections
{
	
	public static class UniSenseConnectionHandler
	{
		//TODO: Add helper classes to more streamlined add and remove controllers
		//TODO: Add support for generic gamepads
		//TODO: Decide whether I Initialize should trigger the OnCurrentControllerUpdated & OnControllerListUpdated events
		//TODO: Implement the CurrentControllerUpdated event
			//Done?
		//TODO: Make Initialize method prefer DualSense controllers for CurrentController
		//TODO: Update all methods to support the updated Controller class (formally DualSenseController)
		//TODO: Test
		//TODO: Let User decide what to do on the event controller is lost and different is connected
		//TODO: No need for mouse keyboard pair in Controller class just some global variables are good enough + events on change
		//TODO: Remove any unnecessary code
		//TODO: Implement any other methods that may be required for this re-factor to work with DualSense and DualSenseManager
		//TODO: Add way to require the destruction of this static class
		//TODO: Broadcast event that is called when a change occurs to the list of gamepads

		public static event Action<Controller> OnCurrentControllerUpdated;

		public static event Action<Controller[]> OnControllerListUpdated;

		private static string _uniqueScriptIdentifier;



		#region Variables
		/// <summary>
		/// DONT RELY ON THIS VALUE
		/// </summary>

		public static Mouse CurrentMouse;
		public static Keyboard CurrentKeyboard;
		public static Controller CurrentController;
		private static bool _initilized = false;
		private static Dictionary<string, int> _controllerLookup = new Dictionary<string, int>();
		private static DeviceEnumInfo[] _infos;
		private static uint _devicecount = 0;
		private static OS_Type _osType;
		private static Controller[] _controllers = new Controller[16];
		private static List<int> _controllersIndexList = new();
		/// <summary>
		/// how many controllers are stored in the controller array
		/// </summary>
		private static int _controllerCount = 0;
		#endregion

		#region Custom Data Structures
		public class DualSenseInfo
		{
			public DualSenseUSBGamepadHID DualsenseUSB;
			public DS5W.DeviceContext contextUSB;
			public DS5W.DeviceEnumInfo enumInfoUSB;
			public DualSenseBTGamepadHID DualsenseBT;
			public DS5W.DeviceContext contextBT;
			public DS5W.DeviceEnumInfo enumInfoBT;
		}

		internal enum OS_Type
		{
			_x64,
			_x86
		}
		public enum DualSenseConnectionStatus
		{
			BT,
			USB,
			Disconected
		}

		public enum DualSenseConnectionType
		{
			BT,
			USB
		}

		public enum ConnectionHandalerStatus
        {
			Ok,
			SourceIgnored,
			UnknownError,
			NoInputsDectected,
			AlreadyInitilized
        }
		
		public class MouseKeyboardPair
        {
			public Keyboard Keyboard;
			public Mouse Mouse;
			public string ID;
			public MouseKeyboardPair(Mouse mouse, Keyboard keyboard)
            {
				Keyboard = keyboard;
				Mouse = mouse;
				ID = keyboard.deviceId.ToString() + mouse.deviceId.ToString();
            }
			public MouseKeyboardPair() { }
        }

		public enum ControllerType
        {
			DualSense,
			GenericGamepad
        }

		public class Controller
		{
			public string key;
			public ControllerType ControllerType;
			public bool isDualSense = false;
			public string serialNumber = null;
			public DualSenseInfo devices = new DualSenseInfo();
			public Gamepad genericGamepad = null;
			public bool BTConnected = false;
			public bool USBConnected = false;
			public DualSenseConnectionStatus connectionStatus = DualSenseConnectionStatus.Disconected;
			public void AddGenericController(Gamepad gamepad)
			{
				genericGamepad = gamepad;
				ControllerType = ControllerType.GenericGamepad;
				this.key = gamepad.deviceId.ToString();
			}

			public void AddDualSenseController(Gamepad gamepad, DualSenseConnectionType connectionType)
			{
				switch (connectionType)
				{
					case DualSenseConnectionType.BT:
						this.isDualSense = true;
						this.BTConnected = true;
						this.serialNumber = gamepad.description.serial.ToString();
						this.key = this.serialNumber;
						this.devices.DualsenseBT = gamepad as DualSenseBTGamepadHID;
						ControllerType = ControllerType.DualSense;
						break;
					case DualSenseConnectionType.USB:
						this.isDualSense = true;
						this.USBConnected = true;
						this.key = gamepad.deviceId.ToString();
						this.devices.DualsenseUSB = gamepad as DualSenseUSBGamepadHID;
						ControllerType = ControllerType.DualSense;
						break;
					default: Debug.LogError("No connection type defined"); break;
				}
			
			}


		}
		#endregion
		private static int GetControllerIndex()
        {
			int i = _controllersIndexList[0];
			_controllersIndexList.Remove(0);
			return i;
        }

		private static void RemoveController(int index)
        {
			_controllerLookup.Remove(_controllers[index].key);
			_controllersIndexList.Add(index);
			_controllers[index] = new Controller();
			_controllerCount--;
        }

		public static string GenerateUniqueIdentifier(GameObject gameObject, string classname)
        {
			return gameObject.GetInstanceID() + "_" + classname;
        }

		/// <summary>
		/// Call this method ONCE 
		/// </summary>
		/// <param name="uniqueIdentifier"> Use <see cref="UniSenseConnectionHandler.GenerateUniqueIdentifier(GameObject, string)"/> to generate this unique identifier </param>
		/// <param name="ignoreMouseKeyboard"></param>
		/// <param name="ignoreGenericGamepad"></param>
		/// <returns></returns>
		public static ConnectionHandalerStatus Initalize(string uniqueIdentifier, bool ignoreGenericGamepad = true)
		{
			//Only allow ONE script to control DualSenseConnectionHandler
			if (_initilized) return ConnectionHandalerStatus.AlreadyInitilized;
			if (string.IsNullOrEmpty(_uniqueScriptIdentifier)) _uniqueScriptIdentifier = uniqueIdentifier;
			if (_uniqueScriptIdentifier != uniqueIdentifier) return ConnectionHandalerStatus.SourceIgnored;
			InputSystem.onDeviceChange += OnDeviceChange;
			_osType = (IntPtr.Size == 4) ? OS_Type._x86 : OS_Type._x64;
			Gamepad[] usbGamepads = DualSenseUSBGamepadHID.FindAll();
			Gamepad[] btGamepads = DualSenseBTGamepadHID.FindAll();
			CurrentMouse = Mouse.current;
			CurrentKeyboard = Keyboard.current;
			Gamepad[] genericGamepads = Gamepad.all.ToArray();

			
			//Initialize the _controllers array with blank Controllers
			//Initialize the _controllerIndexList 
			for (int i = 0; i < _controllers.Length; i++)
            {
				_controllers[i] = new Controller();
				_controllersIndexList.Add(i);
			}

			//Add all BT DualSense controllers to _controllers
			if (btGamepads != null && btGamepads.Length > 0)
			{
				foreach (Gamepad gamepad in btGamepads)
				{
					string _serialNumber = gamepad.description.serial.ToString();
					int id = GetControllerIndex();
					_controllerLookup.Add(_serialNumber, id);
					_controllers[id].AddDualSenseController(gamepad, DualSenseConnectionType.BT);
					_controllerCount++;
				}
			}
			
			//Add all USB DualSense controllers to _controllers
			if (usbGamepads != null && usbGamepads.Length > 0)
			{
				foreach (Gamepad gamepad in usbGamepads)
				{
					string _deciceID = gamepad.deviceId.ToString(); //device id stand in for serial number
					int id = GetControllerIndex();
					_controllerLookup.Add(_deciceID, id);
					_controllers[id].AddDualSenseController(gamepad, DualSenseConnectionType.USB);
					_controllerCount++;
				}
			}
			
			//Add all generic gamepads to _controllers
			if(ignoreGenericGamepad && genericGamepads != null && genericGamepads.Length > 0)
            {
				
				foreach (Gamepad gamepad in genericGamepads)
                {
					if (gamepad is DualSenseBTGamepadHID) continue;
					if (gamepad is DualSenseUSBGamepadHID) continue;
					if (gamepad is Gamepad)
                    {
						string _deciceID = gamepad.deviceId.ToString(); //device id stand in for serial number
						int id = GetControllerIndex();
						_controllerLookup.Add(_deciceID, id);
						_controllers[id].AddGenericController(gamepad);
						_controllerCount++;
					}
				}
            }
			
			//If BT DualSense controllers exist then enumerate them with DS5W
			if (btGamepads != null && btGamepads.Count() > 0) DS5WenumDevices(ref _infos, 16);
			DS5WProssesDeviceInfo(_infos);
		
			CurrentController = _controllers[0]; //Set the current controller
			SetupControllerConnection(ref CurrentController);
			_initilized = true;
			return (_controllerCount > 0) ? ConnectionHandalerStatus.Ok : ConnectionHandalerStatus.NoInputsDectected;
		}

		private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
		{

			bool isGamepad = device is Gamepad;
			bool isUSB = device is DualSenseUSBGamepadHID;
			bool isBT = device is DualSenseBTGamepadHID;

			//Is a generic gamepad so deal with it
			if (!isBT && !isUSB && isGamepad)
            {
				string deviceID;
				int id;
				switch (change)
				{
					case InputDeviceChange.Added:
						
						deviceID = device.deviceId.ToString();
						id = GetControllerIndex();
						_controllerLookup.Add(deviceID, id);
						_controllers[id].genericGamepad = device as Gamepad;
						SetupControllerConnection(ref _controllers[id]);
						SetCurrentGamepad((_controllers[id]));
						_controllerCount++;
						break;

					case InputDeviceChange.Reconnected:
						
						deviceID = device.deviceId.ToString();
						id = GetControllerIndex();
						_controllerLookup.Add(deviceID, id);
						_controllers[id].genericGamepad = device as Gamepad;
						SetupControllerConnection(ref _controllers[id]);
						SetCurrentGamepad((_controllers[id]));
						_controllerCount++;
						break;

					case InputDeviceChange.Removed:
						
						deviceID = device.deviceId.ToString();
						if (_controllerLookup.ContainsKey(deviceID))
						{
							_controllers[_controllerLookup[deviceID]] = new Controller();
							RemoveController(_controllerLookup[deviceID]);
							_controllerCount--;
							SetCurrentGamepad(GetCurrentGamepad());
						}
						else Debug.LogError("Removed DualSense Device that was never previously recorded");
						break;
						


					case InputDeviceChange.Disconnected:
						
						deviceID = device.deviceId.ToString();
						if (_controllerLookup.ContainsKey(deviceID))
						{
							_controllers[_controllerLookup[deviceID]] = new Controller();
							RemoveController(_controllerLookup[deviceID]);
							_controllerCount--;
							SetCurrentGamepad(GetCurrentGamepad());
						}
						else Debug.LogError("Removed DualSense Device that was never previously recorded");
						break;
				}
			} 

			switch (change)
			{
				case InputDeviceChange.Added:


					if (isBT)
					{
						string _serialNumber = device.description.serial.ToString();
						if (_controllerLookup.ContainsKey(_serialNumber))
						{
							int controllerindex = _controllerLookup[_serialNumber];
							_controllers[controllerindex].BTConnected = true;
							_controllers[controllerindex].devices.DualsenseBT = device as DualSenseBTGamepadHID;
							SetupControllerConnection(ref _controllers[controllerindex]);
							SetCurrentGamepad(_controllers[controllerindex]);
						}
						else
						{
							_controllerLookup.Add(_serialNumber, _controllerCount);
							_controllers[_controllerCount].BTConnected = true;
							_controllers[_controllerCount].devices.DualsenseBT = device as DualSenseBTGamepadHID;
							DS5WenumDevices(ref _infos, 16);
							DS5WProssesDeviceInfo(_infos);
							SetupControllerConnection(ref _controllers[_controllerCount]);
							SetCurrentGamepad((_controllers[_controllerCount]));
							_controllerCount++;
						}
					}

					if (isUSB)
					{
						string _deviceID = device.deviceId.ToString();
						if (_controllerLookup.ContainsKey(_deviceID))
						{
							int controllerindex = _controllerLookup[_deviceID];
							_controllers[controllerindex].USBConnected = true;
							_controllers[controllerindex].devices.DualsenseUSB = device as DualSenseUSBGamepadHID;
							SetupControllerConnection(ref _controllers[controllerindex]);
							SetCurrentGamepad((_controllers[controllerindex]));
						}
						else
						{
							_controllerLookup.Add(_deviceID, _controllerCount);
							_controllers[_controllerCount].USBConnected = true;
							_controllers[_controllerCount].devices.DualsenseUSB = device as DualSenseUSBGamepadHID;
							SetupControllerConnection(ref _controllers[_controllerCount]);
							SetCurrentGamepad(_controllers[_controllerCount]);
							_controllerCount++;
						}
					}
					break;

				case InputDeviceChange.Reconnected:
					if (isBT)
					{
						string _serialNumber = device.description.serial.ToString();
						if (_controllerLookup.ContainsKey(_serialNumber))
						{
							int controllerindex = _controllerLookup[_serialNumber];
							_controllers[controllerindex].BTConnected = true;
							_controllers[controllerindex].devices.DualsenseBT = device as DualSenseBTGamepadHID;
							SetupControllerConnection(ref _controllers[controllerindex]);
							SetCurrentGamepad(_controllers[controllerindex]);
						}
						else
						{
							_controllerLookup.Add(_serialNumber, _controllerCount);
							_controllers[_controllerCount].BTConnected = true;
							_controllers[_controllerCount].devices.DualsenseBT = device as DualSenseBTGamepadHID;
							DS5WenumDevices(ref _infos, 16);
							DS5WProssesDeviceInfo(_infos);
							SetupControllerConnection(ref _controllers[_controllerCount]);
							SetCurrentGamepad((_controllers[_controllerCount]));
							_controllerCount++;
						}
					}

					if (isUSB)
					{
						string _deviceID = device.deviceId.ToString();
						if (_controllerLookup.ContainsKey(_deviceID))
						{
							int controllerindex = _controllerLookup[_deviceID];
							_controllers[controllerindex].USBConnected = true;
							_controllers[controllerindex].devices.DualsenseUSB = device as DualSenseUSBGamepadHID;
							SetupControllerConnection(ref _controllers[controllerindex]);
							SetCurrentGamepad((_controllers[controllerindex]));
						}
						else
						{
							_controllerLookup.Add(_deviceID, _controllerCount);
							_controllers[_controllerCount].USBConnected = true;
							_controllers[_controllerCount].devices.DualsenseUSB = device as DualSenseUSBGamepadHID;
							SetupControllerConnection(ref _controllers[_controllerCount]);
							SetCurrentGamepad(_controllers[_controllerCount]);
							_controllerCount++;
						}
					}
					break;

				case InputDeviceChange.Removed:
					if (isBT)
					{
						string _serialNumber = device.description.serial.ToString();
						if (_controllerLookup.ContainsKey(_serialNumber))
						{
							_controllers[_controllerLookup[_serialNumber]].BTConnected = false;
							SetupControllerConnection(ref _controllers[_controllerLookup[_serialNumber]]);
							SetCurrentGamepad(GetCurrentGamepad());
						}
						else Debug.LogError("Removed DualSense Device that was never previously recorded");

					}

					if (isUSB)
					{
						string _deviceID = device.deviceId.ToString();
						if (_controllerLookup.ContainsKey(_deviceID))
						{
							_controllers[_controllerLookup[_deviceID]].USBConnected = false;
							SetupControllerConnection(ref _controllers[_controllerLookup[_deviceID]]);
							SetCurrentGamepad(GetCurrentGamepad());
						}
						else Debug.LogError("Removed DualSense Device that was never previously recorded");
					}
					break;


				case InputDeviceChange.Disconnected:
					if (isBT)
					{
						string _serialNumber = device.description.serial.ToString();
						if (_controllerLookup.ContainsKey(_serialNumber))
						{
							_controllers[_controllerLookup[_serialNumber]].BTConnected = false;
							SetupControllerConnection(ref _controllers[_controllerLookup[_serialNumber]]);
							SetCurrentGamepad(GetCurrentGamepad());
						}
						else Debug.LogError("Removed DualSense Device that was never previously recorded");

					}

					if (isUSB)
					{
						string _deviceID = device.deviceId.ToString();
						if (_controllerLookup.ContainsKey(_deviceID))
						{
							_controllers[_controllerLookup[_deviceID]].USBConnected = false;
							SetupControllerConnection(ref _controllers[_controllerLookup[_deviceID]]);
							SetCurrentGamepad(GetCurrentGamepad());
						}
						else Debug.LogError("Removed DualSense Device that was never previously recorded");
					}
					break;
			}
			OnControllerListUpdated(_controllers);
		}


		public static void Destroy()
		{

			InputSystem.onDeviceChange -= OnDeviceChange;
			CloseAllConnections(ref _controllers);
		}

		#region Helper Classes
		/// <summary>
		/// <para>Use this method to set the correct state of the DualSenseController _internalLogic </para>
		/// This method will manage the connections to your PS5 "Dualsense" controllers
		/// </summary>
		/// <param name="controller"></param>
		private static void SetupControllerConnection(ref Controller controller)
		{
			//if the controller is a generic gamepad nothing needs to happen 
			if (controller.ControllerType == ControllerType.GenericGamepad) return;

			//Assume that controller._internalLogic.BTConnected & controller._internalLogic.USBConnected are both up to date
			//Assume that controller.connectionStatus has not been updated
			switch (controller.connectionStatus)
			{
				case DualSenseConnectionStatus.BT:

					if (controller.USBConnected) //If USB is connected, something went wrong log error
					{
						Debug.LogError("USB connection should not exist");
						return;
					}

					if (controller.BTConnected) return; //Nothing needs to change


					CloseBTConnection(ref controller); //No devices are connected so close connections and update connection status
					controller.connectionStatus = DualSenseConnectionStatus.Disconected;
					break;

				case DualSenseConnectionStatus.USB:
					if (controller.USBConnected) return; //Nothing needs to change

					if (controller.BTConnected) //if BT is connected, something went wrong log error
					{
						Debug.LogError("BT connection should not exist");
						return;
					}



					//Unity handles the closure of USB dual sense device just update connection status
					controller.connectionStatus = DualSenseConnectionStatus.Disconected;
					break;

				case DualSenseConnectionStatus.Disconected:
					if (controller.USBConnected) //Connect to USB device
					{
						//No connection to open Unity handles the USB connection
						controller.connectionStatus = DualSenseConnectionStatus.USB;
						return;
					}
					if (controller.BTConnected) //Connect to BT device
					{
						OpenBTConnection(ref controller);
						controller.connectionStatus = DualSenseConnectionStatus.BT;
						return;
					}
					break;
			}
		}

		private static void OpenBTConnection(ref Controller controller)
		{
			DS5W_RetrunValue status = (_osType == OS_Type._x64) ? DS5W_x64.initDeviceContext(ref controller.devices.enumInfoBT, ref controller.devices.contextBT) :
															 DS5W_x86.initDeviceContext(ref controller.devices.enumInfoBT, ref controller.devices.contextBT);
			if (status != DS5W_RetrunValue.OK) Debug.LogError(status.ToString());
			if (!controller.devices.DualsenseBT.enabled) InputSystem.EnableDevice(controller.devices.DualsenseBT);
			return;
		}

		private static void CloseBTConnection(ref Controller controller)
		{
			//if (controller.devices.DualsenseBT.enabled) InputSystem.DisableDevice(controller.devices.DualsenseBT); //Might delete this devices might only need to be enabled
			if (_osType == OS_Type._x64)
			{
				DS5W_x64.freeDeviceContext(ref controller.devices.contextBT);
			}
			else
			{
				DS5W_x86.freeDeviceContext(ref controller.devices.contextBT);
			}
		}


		private static byte[] GetRawCommand(DualSenseHIDOutputReport command)
		{
			byte[] inbuffer = command.RetriveCommand();
			byte[] outbuffer = new byte[47];
			Array.Copy(inbuffer, 9, outbuffer, 0, 47);
			return outbuffer;
		}

		private static void DS5WenumDevices(ref DeviceEnumInfo[] infos, int Arraysize)
		{
			IntPtr ptrDeviceEnum = IntPtr.Zero;
			infos = new DeviceEnumInfo[Arraysize];
			DS5WHelpers.BuildEnumDeviceBuffer(ref ptrDeviceEnum, infos);

			DS5W_RetrunValue status = (_osType == OS_Type._x64) ? DS5W_x64.enumDevices(ref ptrDeviceEnum, (uint)Arraysize, ref _devicecount, false) :
															 DS5W_x86.enumDevices(ref ptrDeviceEnum, (uint)Arraysize, ref _devicecount, false);
			if (status != DS5W_RetrunValue.OK) Debug.LogError(status.ToString());
			DS5WHelpers.DeconstructEnumDeviceBuffer(ref ptrDeviceEnum, ref infos);
		}

		private static void CloseAllConnections(ref Controller[] controllers)
		{
			for (int i = 0; i < controllers.Length; i++)
			{
				switch (controllers[i].connectionStatus)
				{
					case DualSenseConnectionStatus.BT:
						CloseBTConnection(ref controllers[i]);
						break;
					default: break;
				}
			}
		}


		public static Controller GetCurrentGamepad()
		{

			if (CurrentController == null || CurrentController.connectionStatus == DualSenseConnectionStatus.Disconected)
			{
				for (int i = 0; i < _controllers.Length; i++)
				{
					if (_controllers[i].connectionStatus != DualSenseConnectionStatus.Disconected)
					{
						return _controllers[i];
					}
				}
				Debug.Log("No controllers are connected");
				return null;
			}
			return CurrentController;
		}


		public static void SetCurrentGamepad(Controller gamepad)
		{
			switch (CurrentController.connectionStatus)
			{
				case DualSenseConnectionStatus.Disconected: break;
				case DualSenseConnectionStatus.BT:
					byte[] rawDeviceCommand = GetRawCommand(DualSenseHIDOutputReport.Create());
					DS5W_RetrunValue status = (_osType == OS_Type._x64) ? DS5W_x64.setDeviceRawOutputState(ref CurrentController.devices.contextBT, rawDeviceCommand, rawDeviceCommand.Length) :
																	 DS5W_x86.setDeviceRawOutputState(ref CurrentController.devices.contextBT, rawDeviceCommand, rawDeviceCommand.Length);
					if (status != DS5W_RetrunValue.OK) Debug.LogError(status.ToString());
					break;
				case DualSenseConnectionStatus.USB:
					DualSenseHIDOutputReport report = DualSenseHIDOutputReport.Create();
					CurrentController.devices.DualsenseUSB?.ExecuteCommand(ref report);
					break;
				default: break;
			}
			CurrentController = gamepad;
			OnCurrentControllerUpdated(gamepad);
		}

		/// <summary>
		/// Adds Information from the DeviceEnumInfo array to the DualSenseController array
		/// </summary>
		/// <param name="DeviceEnum"></param>
		private static void DS5WProssesDeviceInfo(DeviceEnumInfo[] DeviceEnum)
		{
			foreach (DeviceEnumInfo info in DeviceEnum)
			{
				if (info._internal.path == null || info._internal.path == "") continue;


				switch (info._internal.Connection)
				{
					case DeviceConnection.BT:
						string _serialNumber = info._internal.serialNumber;
						
						if (_controllerLookup.ContainsKey(_serialNumber))
						{
							_controllers[_controllerLookup[_serialNumber]].devices.enumInfoBT = info;
						}
						else
						{
							int id = GetControllerIndex();
							_controllerLookup.Add(_serialNumber, id);
							_controllers[id].devices.enumInfoBT = info;
							_controllerCount++;
						}

						break;
					default: break;
				}
			}
		}
		#endregion






	}
}