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
	#region Custom Data Structures
	public class ControllerInfo
	{
		public DualSenseUSBGamepadHID DualsenseUSB;
		public DS5W.DeviceContext contextUSB;
		public DS5W.DeviceEnumInfo enumInfoUSB;
		public DualSenseBTGamepadHID DualsenseBT;
		public DS5W.DeviceContext contextBT;
		public DS5W.DeviceEnumInfo enumInfoBT;
		public Gamepad GenericGamepad;
	}

	internal enum OS_Type
	{
		_x64,
		_x86
	}
	/// <summary>
	/// Whether or not the connection is open to the device
	/// </summary>
	public enum ControllerConnectionStatus
	{
		ConnectionOpen,
		Disconected
	}
	public enum ControllerType
	{
		DualSenseBT,
		DualSenseUSB,
		GenericGamepad,
		None
	}

	public enum ConnectionHandelerStatus
	{
		Ok,
		SourceIgnored,
		UnknownError,
		NoInputsDectected,
		AlreadyInitilized,
		NoControllerToRemove
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

	

	public class Controller
	{
		public string key = string.Empty;
		public string serialNumber = null;
		public bool BTConnected = false;
		public bool USBConnected = false;
		public bool ReadyToConnect = false;
		public ControllerInfo devices = new ControllerInfo();
		public ControllerType ControllerType = ControllerType.None;
		public ControllerConnectionStatus connectionStatus = ControllerConnectionStatus.Disconected;

		public void AddController(Gamepad gamepad, ControllerType controllerType)
		{
			switch (controllerType)
			{
				case ControllerType.DualSenseBT:
					
					this.BTConnected = true;
					this.serialNumber = gamepad.description.serial.ToString();
					this.key = this.serialNumber;
					this.devices.DualsenseBT = gamepad as DualSenseBTGamepadHID;
					this.ControllerType = ControllerType.DualSenseBT;
					this.ReadyToConnect = true;
					break;
				case ControllerType.DualSenseUSB:
					
					this.USBConnected = true;
					this.key = gamepad.deviceId.ToString();
					this.devices.DualsenseUSB = gamepad as DualSenseUSBGamepadHID;
					this.ControllerType = ControllerType.DualSenseUSB;
					this.ReadyToConnect = true;
					break;
				case ControllerType.GenericGamepad:
					this.devices.GenericGamepad = gamepad;
					this.key = gamepad.deviceId.ToString();
					this.ControllerType = ControllerType.GenericGamepad;
					this.ReadyToConnect = true;
					break;
				default: Debug.LogError("No connection type defined"); break;
			}

		}


	}
	#endregion
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




		#region Variables
		/// <summary>
		/// DONT RELY ON THIS VALUE
		/// </summary>

		private static string _uniqueScriptIdentifier;
		private static int _currentControllerID;
		public static Mouse CurrentMouse;
		public static Keyboard CurrentKeyboard;
		public static ref Controller CurrentController
        { get { return ref _controllers[_currentControllerID]; } }
        private static bool _initilized = false;
		private static Dictionary<string, int> _controllerLookup = new Dictionary<string, int>();
		private static DeviceEnumInfo[] _infos;
		private static uint _devicecount = 0;
		private static OS_Type _osType;
		private static Controller[] _controllers = new Controller[16];
		private static List<int> _controllersIndexList = new();
		public static bool _isMultiplayer;
		/// <summary>
		/// how many controllers are stored in the controller array
		/// </summary>
		private static int _controllerCount = 0;
		#endregion

		
		private static int GetControllerIndex()
        {
			int i = _controllersIndexList[0];
			_controllersIndexList.RemoveAt(0);
			return i;
        }
		public static ConnectionHandelerStatus DisconnectController(ref Controller controller)
        {
			if (controller.connectionStatus == ControllerConnectionStatus.Disconected) return ConnectionHandelerStatus.Ok; //maybey add a new return type
			switch (controller.ControllerType)
			{
				case ControllerType.DualSenseBT:
					CloseBTConnection(ref controller);
					controller.connectionStatus = ControllerConnectionStatus.Disconected;
					if (controller.devices.DualsenseBT.enabled)
					{
						InputSystem.DisableDevice(controller.devices.DualsenseBT);
					}
					
					break;
				case ControllerType.DualSenseUSB:
					if (controller.devices.DualsenseUSB.enabled)
					{
						DualSenseHIDOutputReport report = DualSenseHIDOutputReport.Create();
						controller.devices.DualsenseUSB?.ExecuteCommand(ref report);
						InputSystem.DisableDevice(controller.devices.DualsenseUSB);
					}
					break;
				case ControllerType.GenericGamepad:
					if (controller.devices.GenericGamepad.enabled)
					{
						controller.devices.GenericGamepad?.PauseHaptics();
						InputSystem.DisableDevice(controller.devices.GenericGamepad);
					}
					
					break;
			}
			return ConnectionHandelerStatus.Ok;
        }


		public static ConnectionHandelerStatus ConnectController(ref Controller controller)
        {
			if (controller.connectionStatus == ControllerConnectionStatus.ConnectionOpen) return ConnectionHandelerStatus.Ok;
			switch (controller.ControllerType)
            {
				case ControllerType.DualSenseBT:
					OpenBTConnection(ref controller);
					controller.connectionStatus = ControllerConnectionStatus.ConnectionOpen;
					if (!controller.devices.DualsenseBT.enabled)
					{
						InputSystem.EnableDevice(controller.devices.DualsenseBT);
					}
					
					break;
				case ControllerType.DualSenseUSB:
					if (!controller.devices.DualsenseUSB.enabled)
                    {
						InputSystem.EnableDevice(controller.devices.DualsenseUSB);
                    }
					controller.connectionStatus = ControllerConnectionStatus.ConnectionOpen;
					
					break;
				case ControllerType.GenericGamepad:
					if (!controller.devices.GenericGamepad.enabled)
					{
						InputSystem.EnableDevice(controller.devices.GenericGamepad);
					}
					controller.connectionStatus = ControllerConnectionStatus.ConnectionOpen;
					break;
            }

			return ConnectionHandelerStatus.Ok;
        }
		
		private static ConnectionHandelerStatus RemoveController(InputDevice device, ControllerType controllerType)
        {
			int id = 0;
			string deviceID;
			switch (controllerType)
            {
				case ControllerType.GenericGamepad:
					deviceID = device.deviceId.ToString();
                    if (_controllerLookup.ContainsKey(deviceID))
                    {
						id = _controllerLookup[deviceID];
						_controllers[id] = new Controller();
						_controllers[id].ReadyToConnect = false;
						_controllers[id].connectionStatus = ControllerConnectionStatus.Disconected;
						//If device is the current controller assign a new current controller
						if(CurrentController.key == device.deviceId.ToString())
                        {
							SetCurrentGamepad(GetCurrentGamepad(), false);
						}
						_controllerCount--;
					}
                    else return ConnectionHandelerStatus.NoControllerToRemove;
                    break;

				case ControllerType.DualSenseBT:
						deviceID = device.description.serial.ToString();
						if (_controllerLookup.ContainsKey(deviceID))
						{
						id = _controllerLookup[deviceID];
						_controllers[id].connectionStatus = ControllerConnectionStatus.Disconected;
						_controllers[id].BTConnected = false;
						_controllers[id].ReadyToConnect = false;
						SetupControllerConnection(ref _controllers[id]);
						//If device is the current controller assign a new current controller
						if (CurrentController.key == device.description.serial.ToString())
						{
							SetCurrentGamepad(GetCurrentGamepad(), false);
						}
							_controllerCount--;
						}
                    else return ConnectionHandelerStatus.NoControllerToRemove;
					break;

				case ControllerType.DualSenseUSB:
						deviceID = device.deviceId.ToString();
						if (_controllerLookup.ContainsKey(deviceID))
						{
							id = _controllerLookup[deviceID];
							_controllers[id].connectionStatus = ControllerConnectionStatus.Disconected;
							_controllers[id].USBConnected = false;
							_controllers[id].ReadyToConnect = false;
							SetupControllerConnection(ref _controllers[id]);
							//If device is the current controller assign a new current controller
							if (CurrentController.key == device.deviceId.ToString())
							{
								SetCurrentGamepad(GetCurrentGamepad(), false);
							}
							_controllerCount--;
						}
					else return ConnectionHandelerStatus.NoControllerToRemove;
					break;
			}
			_controllerLookup.Remove(_controllers[id].key);
			_controllersIndexList.Add(id);
			_controllers[id] = new Controller();
			_controllerCount--;
			return ConnectionHandelerStatus.Ok;
        }

		public static string GenerateUniqueIdentifier(GameObject gameObject, string classname)
        {
			return gameObject.GetInstanceID() + "_" + classname;
        }

		/// <summary>
		/// Call this method ONCE 
		/// </summary>
		/// <param name="uniqueIdentifier"> Use <see cref="UniSenseConnectionHandler.GenerateUniqueIdentifier(GameObject, string)"/> to generate this unique identifier </param>
		/// <param name="ignoreGenericGamepad"></param>
		/// <returns></returns>
		public static ConnectionHandelerStatus Initalize(string uniqueIdentifier, bool ignoreGenericGamepad = true, bool isMultiplayer = false)
		{
			
			//Only allow ONE script to control DualSenseConnectionHandler
			if (_initilized) return ConnectionHandelerStatus.AlreadyInitilized;
			if (string.IsNullOrEmpty(_uniqueScriptIdentifier)) _uniqueScriptIdentifier = uniqueIdentifier;
			if (_uniqueScriptIdentifier != uniqueIdentifier) return ConnectionHandelerStatus.SourceIgnored;
			_isMultiplayer = isMultiplayer;
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
					AddController(gamepad as InputDevice, ControllerType.DualSenseBT);
				}
			}
			
			//Add all USB DualSense controllers to _controllers
			if (usbGamepads != null && usbGamepads.Length > 0)
			{
				foreach (Gamepad gamepad in usbGamepads)
				{
					AddController(gamepad as InputDevice , ControllerType.DualSenseUSB);
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
						AddController(gamepad as InputDevice, ControllerType.GenericGamepad);
					}
				}
            }
			//Remove Already Added the enum in AddController
			//If BT DualSense controllers exist then enumerate them with DS5W
			if (btGamepads != null && btGamepads.Count() > 0) DS5WenumDevices(ref _infos, 16);
			DS5WProssesDeviceInfo(_infos);
		
			_currentControllerID = 0; //Set the current controller
			if (!_isMultiplayer)
            {
				ConnectController(ref CurrentController);
            }
			
			//SetupControllerConnection(ref CurrentController);
			_initilized = true;
			return (_controllerCount > 0) ? ConnectionHandelerStatus.Ok : ConnectionHandelerStatus.NoInputsDectected;
		}

		
		public static ConnectionHandelerStatus AddController(InputDevice device, ControllerType controllerType)
        {
			string deviceID;
			int id;
			switch (controllerType) 
			{
			case ControllerType.GenericGamepad:
					deviceID = device.deviceId.ToString();
					if (_controllerLookup.ContainsKey(deviceID)) return ConnectionHandelerStatus.AlreadyInitilized;
					
					id = GetControllerIndex();
                    _controllerLookup.Add(deviceID, id);
					_controllers[id].AddController(device as Gamepad, ControllerType.GenericGamepad);
					SetupControllerConnection(ref _controllers[id]);
					_controllerCount++;
					if (_isMultiplayer) return ConnectionHandelerStatus.Ok;
					SetCurrentGamepad(id, true);
					ConnectController(ref CurrentController);
					break;
				case ControllerType.DualSenseUSB:

					deviceID = device.deviceId.ToString();
					if (_controllerLookup.ContainsKey(deviceID)) return ConnectionHandelerStatus.AlreadyInitilized;
					id = GetControllerIndex();
					_controllerLookup.Add(deviceID, id);
					_controllers[id].AddController(device as Gamepad, controllerType);
					SetupControllerConnection(ref _controllers[id]);
					_controllerCount++;
					if (_isMultiplayer) return ConnectionHandelerStatus.Ok;
					SetCurrentGamepad(id, true);
					ConnectController(ref CurrentController);
					break;
				case ControllerType.DualSenseBT:
					deviceID = device.description.serial.ToString();
					if (_controllerLookup.ContainsKey(deviceID)) return ConnectionHandelerStatus.AlreadyInitilized;
					id = GetControllerIndex();
					_controllerLookup.Add(deviceID, id);
					_controllers[id].AddController(device as Gamepad, controllerType);
					DS5WenumDevices(ref _infos, 16);
					DS5WProssesDeviceInfo(_infos); //Adds device at an uncontrolled rate could make controllercount wrong
					SetupControllerConnection(ref _controllers[id]);
					_controllerCount++;
					if (_isMultiplayer) return ConnectionHandelerStatus.Ok;
					SetCurrentGamepad(id, true);
					ConnectController(ref CurrentController);
					break;
            }
			return ConnectionHandelerStatus.Ok;
        }


	        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
		{
			Debug.Log("it happend again" + device.name.ToString() + ", " + change);
			ControllerType controllerType = ControllerType.GenericGamepad;
			if (device is DualSenseUSBGamepadHID) controllerType = ControllerType.DualSenseUSB;
			else if (device is DualSenseBTGamepadHID) controllerType = ControllerType.DualSenseBT;
			bool isGamepad = device is Gamepad;
			bool isUSB = device is DualSenseUSBGamepadHID;
			bool isBT = device is DualSenseBTGamepadHID;
			ConnectionHandelerStatus status;
			if (!isGamepad && !isUSB && !isBT) return;
			switch (change)
			{
				case InputDeviceChange.Added:
					status = AddController(device, controllerType);
					if (status != ConnectionHandelerStatus.Ok) Debug.LogError(status.ToString());
					if (OnControllerListUpdated != null) OnControllerListUpdated(_controllers);
					break;

				

				case InputDeviceChange.Removed:
					status = RemoveController(device, controllerType);
					if (!_isMultiplayer && CurrentController.ReadyToConnect) ConnectController(ref CurrentController);
					if (status != ConnectionHandelerStatus.Ok) Debug.LogError(status.ToString());
					if (OnControllerListUpdated != null) OnControllerListUpdated(_controllers);
					break;


				default: break;
			}
		

			////Is a generic gamepad so deal with it
			//if (!isBT && !isUSB && isGamepad)
			//         {
			//	string deviceID;
			//	int id;
			//	switch (change)
			//	{
			//		case InputDeviceChange.Added:

			//			deviceID = device.deviceId.ToString();
			//			id = GetControllerIndex();
			//			_controllerLookup.Add(deviceID, id);
			//			_controllers[id].genericGamepad = device as Gamepad;
			//			SetupControllerConnection(ref _controllers[id]);
			//			SetCurrentGamepad((_controllers[id]));
			//			_controllerCount++;
			//			break;

			//		case InputDeviceChange.Reconnected:

			//			deviceID = device.deviceId.ToString();
			//			id = GetControllerIndex();
			//			_controllerLookup.Add(deviceID, id);
			//			_controllers[id].genericGamepad = device as Gamepad;
			//			SetupControllerConnection(ref _controllers[id]);
			//			SetCurrentGamepad((_controllers[id]));
			//			_controllerCount++;
			//			break;

			//		case InputDeviceChange.Removed:

			//			deviceID = device.deviceId.ToString();
			//			if (_controllerLookup.ContainsKey(deviceID))
			//			{
			//				_controllers[_controllerLookup[deviceID]] = new Controller();
			//				RemoveController(_controllerLookup[deviceID]);
			//				_controllerCount--;
			//				SetCurrentGamepad(GetCurrentGamepad());
			//			}
			//			else Debug.LogError("Removed DualSense Device that was never previously recorded");
			//			break;



			//		case InputDeviceChange.Disconnected:

			//			deviceID = device.deviceId.ToString();
			//			if (_controllerLookup.ContainsKey(deviceID))
			//			{
			//				_controllers[_controllerLookup[deviceID]] = new Controller();
			//				RemoveController(_controllerLookup[deviceID]);
			//				_controllerCount--;
			//				SetCurrentGamepad(GetCurrentGamepad());
			//			}
			//			else Debug.LogError("Removed DualSense Device that was never previously recorded");
			//			break;
			//	}
			//} 
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
		
		[Obsolete("Use ''ConnectController'' and ''DisconnectController'' instead")]
		private static void SetupControllerConnection(ref Controller controller)
		{
			////if the controller is a generic gamepad nothing needs to happen 
			//if (controller.ControllerType == ControllerType.GenericGamepad) return;

			////Assume that controller._internalLogic.BTConnected & controller._internalLogic.USBConnected are both up to date
			////Assume that controller.connectionStatus has not been updated
			//switch (controller.connectionStatus)
			//{
			//	case ControllerConnectionStatus.DualSenseBT:

			//		if (controller.USBConnected) //If USB is connected, something went wrong log error
			//		{
			//			Debug.LogError("USB connection should not exist");
			//			return;
			//		}

			//		if (controller.BTConnected) return; //Nothing needs to change


			//		CloseBTConnection(ref controller); //No devices are connected so close connections and update connection status
			//		controller.connectionStatus = ControllerConnectionStatus.Disconected;
			//		break;

			//	case ControllerConnectionStatus.DualSenseUSB:
			//		if (controller.USBConnected) return; //Nothing needs to change

			//		if (controller.BTConnected) //if BT is connected, something went wrong log error
			//		{
			//			Debug.LogError("BT connection should not exist");
			//			return;
			//		}



			//		//Unity handles the closure of USB dual sense device just update connection status
			//		controller.connectionStatus = ControllerConnectionStatus.Disconected;
			//		break;

			//	case ControllerConnectionStatus.Disconected:
			//		if (controller.USBConnected) //Connect to USB device
			//		{
			//			//No connection to open Unity handles the USB connection
			//			controller.connectionStatus = ControllerConnectionStatus.DualSenseUSB;
			//			return;
			//		}
			//		if (controller.BTConnected) //Connect to BT device
			//		{
			//			OpenBTConnection(ref controller);
			//			controller.connectionStatus = ControllerConnectionStatus.DualSenseBT;
			//			return;
			//		}
			//		break;
			//}
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
				switch (controllers[i].ControllerType)
				{
					case ControllerType.DualSenseBT:
						CloseBTConnection(ref controllers[i]);
						break;
					default: break;
				}
			}
		}

		/// <summary>
		/// Method to retreive the index of the currentController 
		/// If the current currentController is disconceted it will choose the first controller that is connected as currentController
		/// </summary>
		/// <returns></returns>
		public static int GetCurrentGamepad()
		{

			if (CurrentController == null || CurrentController.connectionStatus == ControllerConnectionStatus.Disconected)
			{
				for (int i = 0; i < _controllers.Length; i++)
				{
					if (_controllers[i].ReadyToConnect)
					{
						return i;
					}
				}
				Debug.Log("No controllers are connected");
				return 0;
			}
			return _currentControllerID;
		}


		public static void SetCurrentGamepad(int controllerIndex, bool puaseHaptics )
		{
			if (!puaseHaptics || CurrentController.connectionStatus != ControllerConnectionStatus.ConnectionOpen)
            {
				_currentControllerID = controllerIndex;
				if (OnCurrentControllerUpdated != null) OnCurrentControllerUpdated(CurrentController);
				return;
			}
			switch (CurrentController.ControllerType)
			{
				case ControllerType.None: break;
				case ControllerType.DualSenseBT:
					DisconnectController(ref CurrentController);
					break;
				case ControllerType.DualSenseUSB:
					DisconnectController(ref CurrentController);
					break;
				default: break;
			}
			_currentControllerID = controllerIndex;
			if (OnCurrentControllerUpdated != null) OnCurrentControllerUpdated(CurrentController);
		}

		/// <summary>
		/// Adds Information from the DeviceEnumInfo array to the DualSenseController array
		/// </summary>
		/// <param name="DeviceEnum"></param>
		private static void DS5WProssesDeviceInfo(DeviceEnumInfo[] DeviceEnum) //Maybey make so can't add a new device
		{
			if (DeviceEnum == null) return;	
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