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

namespace UniSense.NewConnections
{
	#region Custom Data Structures
	public class ControllerInfo
	{
		public DualSenseUSBGamepadHID DualsenseUSB;
		public DualSenseBTGamepadHID DualsenseBT;
		public DeviceContext contextUSB;
		public DeviceContext contextBT;
		public DeviceEnumInfo enumInfoUSB;
		public DeviceEnumInfo enumInfoBT;
		public Gamepad GenericGamepad;
		public InputDevice InputDevice;
	}

	public class UniqueIdentifier
    {
		public string value = String.Empty;
		public UniqueIdentifier(GameObject gameObject, object Script)
        {
			value = gameObject.name + "_1159_" + Script.ToString();
        }
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
		NoControllerToRemove,
		AlreadyDisconnected,
		AccessDenied,
		NotInitilized,
		AlreadyConnected
	}

	public class Controller
	{
		public string key = string.Empty;
		public int unisenseID;
		public bool ReadyToConnect = false;
		public ControllerInfo devices = new ControllerInfo();
		public ControllerType ControllerType = ControllerType.None;
		public ControllerConnectionStatus connectionStatus = ControllerConnectionStatus.Disconected;

		public void AddController(Gamepad gamepad, ControllerType controllerType, int unisenseID)
		{
			switch (controllerType)
			{
				case ControllerType.DualSenseBT:
					this.key = gamepad.description.serial.ToString();
					this.devices.DualsenseBT = gamepad as DualSenseBTGamepadHID;
					this.ControllerType = ControllerType.DualSenseBT;
					this.ReadyToConnect = true;
					this.unisenseID = unisenseID;
					this.devices.InputDevice = gamepad;
					break;
				case ControllerType.DualSenseUSB:
					this.key = gamepad.deviceId.ToString();
					this.devices.DualsenseUSB = gamepad as DualSenseUSBGamepadHID;
					this.ControllerType = ControllerType.DualSenseUSB;
					this.ReadyToConnect = true;
					this.unisenseID = unisenseID;
					this.devices.InputDevice = gamepad;
					break;
				case ControllerType.GenericGamepad:
					this.devices.GenericGamepad = gamepad;
					this.key = gamepad.deviceId.ToString();
					this.ControllerType = ControllerType.GenericGamepad;
					this.ReadyToConnect = true;
					this.unisenseID = unisenseID;
					this.devices.InputDevice = gamepad;
					break;
				default: Debug.LogError("No connection type defined"); break;
			}

		}


	}
	public enum ControllerChange
    {
		Removed,
		Added
    }
	#endregion
	static class NewUniSenseConnectionHandler
	{
        #region Events
        public static event Action<Controller> OnCurrentControllerUpdated;
		public static event Action<Controller[]> OnControllerListUpdated;
		public static event Action<int, ControllerChange, string> OnControllerChange;
        #endregion
        #region Variables
        public static Mouse CurrentMouse;
		public static Keyboard CurrentKeyboard;
		public const int MaxControllerCount = 16;
		public static ref Controller CurrentController
		{ get { return ref _controllers[_currentControllerID]; } }
		private static int _currentControllerID;
		private static UniqueIdentifier _uniqueIdentifier;
		public static ref Controller[] Controllers {get { return ref _controllers; } }
		private static Controller[] _controllers = new Controller[MaxControllerCount];
		private static List<int> _controllersIndexList = new();
		public static Dictionary<string, int> ControllerLookup { get { return _controllerLookup; } }
		private static Dictionary<string, int> _controllerLookup = new Dictionary<string, int>();
		private static bool _initilized = false;
		private static DeviceEnumInfo[] _infos;
		private static uint _devicecount = 0;
		private static OS_Type _osType;
		private static bool _isMultiplayer;
		public static int ControllerCount { get { return _controllerCount; } }
		private static int _controllerCount = 0;
		#endregion


		/// <summary>
		/// Use this class to "start up" <see cref= "NewUniSenseConnectionHandler" />. 
		/// Without first calling this method all <see cref= "NewUniSenseConnectionHandler" /> methods will return an error
		/// </summary>
		/// <param name="uniqueIdentifier"></param>
		/// <param name="isMultiplayer">Set whether or not multilayer mode is enabled</param>
		/// <param name="ignoreGeneric">Should generic gamepads (non DualSense controllers) be ignored</param>
		/// <returns></returns>
		public static ConnectionHandelerStatus Initilize(UniqueIdentifier uniqueIdentifier, bool isMultiplayer = false, bool ignoreGeneric = false)
        {
			//Only allow ONE script to control DualSenseConnectionHandler
			if (_initilized) return ConnectionHandelerStatus.AlreadyInitilized;
			if (_uniqueIdentifier == null) _uniqueIdentifier = uniqueIdentifier;
			if (_uniqueIdentifier.value != uniqueIdentifier.value) return ConnectionHandelerStatus.SourceIgnored;
			_isMultiplayer = isMultiplayer;
			InputSystem.onDeviceChange += OnDeviceChange;
			_osType = (IntPtr.Size == 4) ? OS_Type._x86 : OS_Type._x64;
			Gamepad[] usbGamepads = DualSenseUSBGamepadHID.FindAll();
			Gamepad[] btGamepads = DualSenseBTGamepadHID.FindAll();
			Gamepad[] genericGamepads = Gamepad.all.ToArray();
			CurrentMouse = Mouse.current;
			CurrentKeyboard = Keyboard.current;
			
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
					AddControllerimpl(gamepad as InputDevice, ControllerType.DualSenseBT, false, false);
				}
			}

			//Add all USB DualSense controllers to _controllers
			if (usbGamepads != null && usbGamepads.Length > 0)
			{
				foreach (Gamepad gamepad in usbGamepads)
				{
					AddControllerimpl(gamepad as InputDevice, ControllerType.DualSenseUSB, false, false);
				}
			}

			//Add all generic gamepads to _controllers
			if (ignoreGeneric && genericGamepads != null && genericGamepads.Length > 0)
			{

				foreach (Gamepad gamepad in genericGamepads)
				{
					if (gamepad is DualSenseBTGamepadHID) continue;
					if (gamepad is DualSenseUSBGamepadHID) continue;
					if (gamepad is Gamepad)
					{
						AddControllerimpl(gamepad as InputDevice, ControllerType.GenericGamepad, false, false);
					}
				}
			}
			//Remove Already Added the enum in AddController
			//If BT DualSense controllers exist then enumerate them with DS5W
			if (btGamepads != null && btGamepads.Count() > 0)
			{
				DS5WenumDevices(ref _infos, 16);
				DS5WProssesDeviceInfo(_infos);
			}
			_currentControllerID = 0; //Set the current controller
			if (!_isMultiplayer)
			{
				ConnectControllerimpl(ref CurrentController);
			}
			_initilized = true;
			return (_controllerCount > 0) ? ConnectionHandelerStatus.Ok : ConnectionHandelerStatus.NoInputsDectected;

		}		

		private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
		{
			if (change == InputDeviceChange.Enabled) return;
			if (change == InputDeviceChange.Disabled) return;
			if (change == InputDeviceChange.SoftReset) return;
			if (change == InputDeviceChange.HardReset) return;
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
					status = AddControllerimpl(device, controllerType, true, updateCurrentController: !_isMultiplayer);
					if (status != ConnectionHandelerStatus.Ok) Debug.LogError(status.ToString());
					if (!_isMultiplayer) SetCurrentGamepad(device, controllerType);
					if (OnControllerListUpdated != null) OnControllerListUpdated(_controllers);
					break;



				case InputDeviceChange.Removed:
					status = RemoveControllerimpl(device, controllerType, updateCurrentController: !_isMultiplayer);
					if (!_isMultiplayer && CurrentController.ReadyToConnect) ConnectControllerimpl(ref CurrentController);
					if (status != ConnectionHandelerStatus.Ok) Debug.LogError(status.ToString());
					if (OnControllerListUpdated != null) OnControllerListUpdated(_controllers);
					break;


				default: break;
			}
		}

		public static void Destroy(UniqueIdentifier uniqueIdentifier)
		{
			if (uniqueIdentifier == null) return;
			if (uniqueIdentifier.value != _uniqueIdentifier.value) return;
			CloseAllConnections(ref _controllers);
			InputSystem.onDeviceChange -= OnDeviceChange;
		}

		#region HelperClasses
		private static int AllocateUniSenseID()
        {
			int i = _controllersIndexList[0];
			_controllersIndexList.RemoveAt(0);
			return i;
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

		#region DS5W Helpers
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
		/// <summary>
		/// Adds Information from the DeviceEnumInfo array to the DualSenseController array
		/// </summary>
		/// <param name="DeviceEnum"></param>
		private static void DS5WProssesDeviceInfo(DeviceEnumInfo[] DeviceEnum) //Maybey make so can't add a new device. DONE
		{
			if (DeviceEnum == null) return;
			foreach (DeviceEnumInfo info in DeviceEnum)
			{
				if (info._internal.path == null || info._internal.path == "") continue;
				switch (info._internal.Connection)
				{
					case DeviceConnection.BT:
						string _serialNumber = info._internal.serialNumber;
						if (_controllerLookup.ContainsKey(_serialNumber)) //Check if the devices unity counterpart already exists
						{
							_controllers[_controllerLookup[_serialNumber]].devices.enumInfoBT = info;
						}
						break;
					default: break;
				}
			}
		}
		#endregion
		#region Current controller handling
		private static int GetFirstValidControllerIndex()
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

		/// <summary>
		/// Connects to and sets the value of <see cref="CurrentController"/> and dissconnects the current <see cref="CurrentController"/>
		/// </summary>
		/// <param name="controllerIndex"></param>
		
		private static void SetCurrentGamepad(int controllerIndex)
		{
			switch (CurrentController.ControllerType)
			{
				case ControllerType.None: break;
				case ControllerType.DualSenseBT:
					DisconnectControllerimpl(ref CurrentController);
					break;
				case ControllerType.DualSenseUSB:
					DisconnectControllerimpl(ref CurrentController);
					break;
				default: break;
			}
			_currentControllerID = controllerIndex;
			ConnectControllerimpl(ref CurrentController);
			if (OnCurrentControllerUpdated != null) OnCurrentControllerUpdated(CurrentController);
		}

		/// <summary>
		///  Connects to and sets the value of <see cref="CurrentController"/> and dissconnects the current <see cref="CurrentController"/>
		/// </summary>
		/// <param name="controllerIndex"></param>
		private static void SetCurrentGamepad(InputDevice inputDevice, ControllerType controllerType)
		{
			string key;
			int id = -1;
			switch (CurrentController.ControllerType)
			{
				case ControllerType.None: break;
				case ControllerType.DualSenseBT:
					DisconnectControllerimpl(ref CurrentController);
					break;
				case ControllerType.DualSenseUSB:
					DisconnectControllerimpl(ref CurrentController);
					break;
				default: break;
			}
			switch (controllerType)
            {
				case ControllerType.DualSenseBT:
                    key = inputDevice.description.serial;
					id = _controllerLookup[key];
					break;

				case ControllerType.DualSenseUSB:
					key = inputDevice.deviceId.ToString();
					id = _controllerLookup[key];
					break;

				case ControllerType.GenericGamepad:
					key = inputDevice.deviceId.ToString();
					id = _controllerLookup[key];
					break;
            }
			if (id == -1) return;
			_currentControllerID = id;
			ConnectControllerimpl(ref CurrentController);
			if (OnCurrentControllerUpdated != null) OnCurrentControllerUpdated(CurrentController);
		}
		#endregion
		#region Controller Handling
		public static ConnectionHandelerStatus AddController(InputDevice device, ControllerType controllerType,UniqueIdentifier uniqueIdentifier, bool enumDS5W = true, bool updateCurrentController = true)
        {
			if (!_initilized) return ConnectionHandelerStatus.NotInitilized;
			if (_uniqueIdentifier.value != uniqueIdentifier.value) return ConnectionHandelerStatus.AccessDenied;
			return AddControllerimpl(device, controllerType, enumDS5W, updateCurrentController);
        }
		private static ConnectionHandelerStatus AddControllerimpl(InputDevice device, ControllerType controllerType, bool enumDS5W = true, bool updateCurrentController = true)
 		{
			string deviceID;
			int id = -1;
			switch (controllerType)
			{
				case ControllerType.GenericGamepad:
					deviceID = device.deviceId.ToString();
					if (_controllerLookup.ContainsKey(deviceID)) return ConnectionHandelerStatus.AlreadyInitilized;
					id = AllocateUniSenseID();
					_controllerLookup.Add(deviceID, id);
					_controllers[id].AddController(device as Gamepad, ControllerType.GenericGamepad, id);
					_controllerCount++;
					break;
				case ControllerType.DualSenseUSB:
					deviceID = device.deviceId.ToString();
					if (_controllerLookup.ContainsKey(deviceID)) return ConnectionHandelerStatus.AlreadyInitilized;
					id = AllocateUniSenseID();
					_controllerLookup.Add(deviceID, id);
					_controllers[id].AddController(device as Gamepad, ControllerType.DualSenseUSB, id);
					_controllerCount++;
                    break;
				case ControllerType.DualSenseBT:
					deviceID = device.description.serial.ToString();
					if (_controllerLookup.ContainsKey(deviceID)) return ConnectionHandelerStatus.AlreadyInitilized;
					id = AllocateUniSenseID();
					_controllerLookup.Add(deviceID, id);
					_controllers[id].AddController(device as Gamepad, ControllerType.DualSenseBT, id);

					//_controllers[0].devices.DualsenseBT.variants;
					//ConnectControllerimpl(ref _controllers[id]);
					//DisconnectControllerimpl(ref _controllers[id]);
					_controllerCount++;
					if (enumDS5W)
					{
						DS5WenumDevices(ref _infos, MaxControllerCount);
						DS5WProssesDeviceInfo(_infos); //Adds device at an uncontrolled rate could make controllercount wrong. Fixed?
					}
                    break;
			}
			if (id == -1) return ConnectionHandelerStatus.UnknownError; //TODO: Maybey add better error handeling
			if(OnControllerChange != null) OnControllerChange(id, ControllerChange.Added, _controllers[id].key);
			return ConnectionHandelerStatus.Ok;
		}
		public static ConnectionHandelerStatus RemoveController(InputDevice device, ControllerType controllerType, UniqueIdentifier uniqueIdentifier, bool updateCurrentController = true)
        {
			if (!_initilized) return ConnectionHandelerStatus.NotInitilized;
			if (_uniqueIdentifier.value != uniqueIdentifier.value) return ConnectionHandelerStatus.AccessDenied;
			return RemoveControllerimpl(device, controllerType, updateCurrentController);
		}
		private static ConnectionHandelerStatus RemoveControllerimpl(InputDevice device, ControllerType controllerType, bool updateCurrentController = true) //TODO add current controller logic
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
						//If device is the current controller assign a new current controller
						if (CurrentController.key == device.deviceId.ToString() && updateCurrentController)
						{
							SetCurrentGamepad(GetFirstValidControllerIndex());
						}
					}
					else return ConnectionHandelerStatus.NoControllerToRemove;
					break;

				case ControllerType.DualSenseBT:
					deviceID = device.description.serial.ToString();
					if (_controllerLookup.ContainsKey(deviceID))
					{
						id = _controllerLookup[deviceID];
						CloseBTConnection(ref _controllers[id]);
						_controllers[id].connectionStatus = ControllerConnectionStatus.Disconected;
						_controllers[id].ReadyToConnect = false;
						if (CurrentController.key == device.description.serial.ToString() && updateCurrentController)
						{
							SetCurrentGamepad(GetFirstValidControllerIndex());
						}
					}
					else return ConnectionHandelerStatus.NoControllerToRemove;
					break;

				case ControllerType.DualSenseUSB:
					deviceID = device.deviceId.ToString();
					if (_controllerLookup.ContainsKey(deviceID))
					{
						id = _controllerLookup[deviceID];
						_controllers[id].connectionStatus = ControllerConnectionStatus.Disconected;
						DualSenseHIDOutputReport report = DualSenseHIDOutputReport.Create();
						device?.ExecuteCommand(ref report);
						//If device is the current controller assign a new current controller
						if (CurrentController.key == device.deviceId.ToString() && updateCurrentController)
                        {
                            SetCurrentGamepad(GetFirstValidControllerIndex());
                        }
					}
					else return ConnectionHandelerStatus.NoControllerToRemove;
					break;
			}
			InputSystem.RemoveDevice(device);
			string key = _controllers[id].key;
			_controllerLookup.Remove(key);
			_controllersIndexList.Add(id);
			
			_controllers[id] = new Controller();
			_controllerCount--;
			if(OnControllerChange != null) OnControllerChange(id, ControllerChange.Removed, key);
			return ConnectionHandelerStatus.Ok;
		}
		#endregion
		#region Connection Handling
		public static ConnectionHandelerStatus ConnectController(ref Controller controller, UniqueIdentifier uniqueIdentifier)
        {
			if (!_initilized) return ConnectionHandelerStatus.NotInitilized;
			if (_uniqueIdentifier.value != uniqueIdentifier.value) return ConnectionHandelerStatus.AccessDenied;
			return ConnectControllerimpl(ref controller);
		}
		private static ConnectionHandelerStatus ConnectControllerimpl(ref Controller controller)
		{
			if (controller.connectionStatus == ControllerConnectionStatus.ConnectionOpen) return ConnectionHandelerStatus.AlreadyConnected;

			switch (controller.ControllerType)
            {
				case ControllerType.DualSenseBT:
					InputSystem.EnableDevice(controller.devices.DualsenseBT);
					OpenBTConnection(ref controller);
					break;
				case ControllerType.DualSenseUSB:
					InputSystem.EnableDevice(controller.devices.DualsenseUSB);
					break;
				case ControllerType.GenericGamepad:
					InputSystem.EnableDevice(controller.devices.GenericGamepad);
					break;
            }
			controller.connectionStatus = ControllerConnectionStatus.ConnectionOpen;
			return ConnectionHandelerStatus.Ok;
		}
		public static ConnectionHandelerStatus DisconnectController(ref Controller controller, UniqueIdentifier uniqueIdentifier)
        {
			if (!_initilized) return ConnectionHandelerStatus.NotInitilized;
			if (_uniqueIdentifier.value != uniqueIdentifier.value) return ConnectionHandelerStatus.AccessDenied;
			return DisconnectControllerimpl(ref controller);
		}
		private static ConnectionHandelerStatus	DisconnectControllerimpl(ref Controller controller)
		{
			if (controller.connectionStatus == ControllerConnectionStatus.Disconected) return ConnectionHandelerStatus.AlreadyDisconnected;
			switch (controller.ControllerType)
			{
				case ControllerType.DualSenseBT:
					CloseBTConnection(ref controller);
					InputSystem.DisableDevice(controller.devices.DualsenseBT);
					break;
				case ControllerType.DualSenseUSB:
					DualSenseHIDOutputReport report = DualSenseHIDOutputReport.Create();
					controller.devices.DualsenseUSB?.ExecuteCommand(ref report);
					InputSystem.DisableDevice(controller.devices.DualsenseUSB);
					break;
				case ControllerType.GenericGamepad:
					controller.devices.GenericGamepad.ResetHaptics();
					InputSystem.DisableDevice(controller.devices.GenericGamepad);
					break;
			}
			controller.connectionStatus = ControllerConnectionStatus.Disconected;
			return ConnectionHandelerStatus.Ok;
		}
		private static void OpenBTConnection(ref Controller controller)
		{
			DS5W_RetrunValue status = (_osType == OS_Type._x64) ? DS5W_x64.initDeviceContext(ref controller.devices.enumInfoBT, ref controller.devices.contextBT) :
																  DS5W_x86.initDeviceContext(ref controller.devices.enumInfoBT, ref controller.devices.contextBT);
			if (status != DS5W_RetrunValue.OK) Debug.LogError(status.ToString());
			return;
		}
		private static void CloseBTConnection(ref Controller controller)
		{
			
			if (_osType == OS_Type._x64)
			{
				DS5W_x64.freeDeviceContext(ref controller.devices.contextBT);
			}
			else
			{
				DS5W_x86.freeDeviceContext(ref controller.devices.contextBT);
			}
		}
		#endregion
		#endregion

	}
}
