using UnityEditor;
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
using UnityEngine.InputSystem.DualSense;
using UnityEngine.Scripting;
using System.Text;
//using System.Diagnostics; //Stopwatch if needed

//TODO: Set up queues for connection and disconnection might make it so two BT device can be detected at the same time without issue
//TODO: Needs testing
//TODO: Remove mouse and keyboard logic
//TODO: Make InitilizeUsers get called after the device matching has occurred
//TODO: Move all interfaces and data classes to separate script file for easy navigation
//TODO: Have a bool for UnisenseUsers that denotes if the can be used by a multiplayer manager
//TODO: 
namespace UniSense.DevConnections {

	

	public interface IManageable
    {
		public void SetCurrentUser(int unisenseId);
    }

	public interface IConnectionListener
    {
		public void OnUserAdded(int unisenseId);
		public void OnUserRemoved(int unisenseId);
		public void OnUserModified(int unisenseId, UserChange change);
		public void InitilizeUsers();

		public void OnCurrentUserModified();

    }

	public interface IHandleMultiplayer
    {
		public void OnUserAdded(int unisenseId);
		public void OnUserRemoved(int unisenseId);
		public void OnUserModified(int unisenseId, UserChange change);
		public void InitilizeUsers();
	}

    public interface IHandleSingleplayer 
	{
		public void InitilizeUsers(int unisenseId);
		public void OnCurrentUserModified(UserChange change);
		public void OnCurrentUserChanged(int uniSenseId);
		public void OnNoCurrentUser();

	}


    public enum UserChange
    {
		BTAdded,
		BTRemoved,
		BTDisabled,
		USBAdded,
		USBRemoved,
		GenericAdded,
		GenericRemoved,
    }

	#region CustomDataStructurs
	internal enum OS_Type
	{
		_x64,
		_x86
	}
	public class UniSenseUser 
	{
		
		public int UniSenseID { get; internal set;}

		public string SerialNumber { get; private set;}
		public bool ConnectionOpen { get; private set; }
		public bool ReadyToConnect { get; private set;}
		public bool Enabled { get; internal set; }
		public UniSenseDevice Devices { get; internal set; }
		public bool BTAttached { get; private set; }
		public bool USBAttached { get; private set; }
		public bool GenericAttached { get; private set; }
		public bool IsSomthingAttached
        {
			get { return BTAttached || USBAttached || GenericAttached; }
        }
		/// <summary>
		/// Is this user ready for a connection
		/// </summary>
		public bool IsReadyToConnect
        {
			get { return USBAttached || GenericAttached || (BTAttached && !Devices.DualsenseBT.usbConnected.isPressed); }
        }
		public DeviceType ActiveDevice = DeviceType.None;
		private OS_Type _osType;
		private PlayerInput _playerInput;
		private bool _playerInputPaired;
	
		/// <summary>
		/// True if the BT DualSense reports an active USB counterpart
		/// </summary>
		public bool DontOpenWirelessConnection
		{
			get
			{
				if (BTAttached && Devices.DualsenseBT.usbConnected.isPressed) return true;
				return false;
			}
		}

		public UniSenseUser()
		{
			_osType = (IntPtr.Size == 4) ? OS_Type._x86 : OS_Type._x64;
			SetDefualts();
		}

		private void SetDefualts()
        {
			UniSenseID = -1;
			ConnectionOpen = false;
			Enabled = false;
			Devices = new UniSenseDevice();
			BTAttached = false;
			USBAttached = false;
			GenericAttached = false;
			ActiveDevice = DeviceType.None;
			_playerInputPaired = false;
			_playerInput = null;
		}
		

		public bool PairWithPlayerInput(PlayerInput playerInput)
        {
			if(_playerInputPaired) return false;
			_playerInputPaired = true;
			_playerInput = playerInput;
            if (!_playerInput.neverAutoSwitchControlSchemes)
            {
				Debug.LogWarning("Turn auto switch control schemes off");
            }
			_playerInput.neverAutoSwitchControlSchemes = true;
			return true;
        }

		public bool UnPairPlayerInput()
		{
			if (!_playerInputPaired) return false;
			_playerInputPaired = false;
			if(_playerInput.devices.Count > 0) _playerInput.user.UnpairDevices();
			
			_playerInput = null;
			return true;
		}


		/// <summary>
		/// True if the BT DualSense reports an active USB counterpart
		/// </summary>
		/// <summary>
		/// Adds a device to this UnisenseUser
		/// </summary>
		/// <param name="device"></param>
		/// <param name="deviceType"></param>
		/// <param name="unisenseID"></param>
		/// <returns>True if successful </returns>
		internal bool AddDevice(InputDevice device, DeviceType deviceType, int unisenseID)
        {
			
			switch (deviceType)
            {
				case DeviceType.DualSenseBT:
					if(BTAttached) return false;
					BTAttached = true;
					this.Devices.DualsenseBT = device as DualSenseBTGamepadHID;
					this.UniSenseID = unisenseID;
					this.SerialNumber = device.description.serial;
					NewUniSenseConnectionHandler.userLookup.AddValue(this.SerialNumber, unisenseID);
					break;
				case DeviceType.DualSenseUSB:
					if (USBAttached) return false;
					USBAttached = true;
					this.Devices.DualsenseUSB = device as DualSenseUSBGamepadHID;
					this.UniSenseID = unisenseID;
					NewUniSenseConnectionHandler.userLookup.AddValue(device.deviceId.ToString(), unisenseID);
					break;
				case DeviceType.GenericGamepad:
					if (GenericAttached) return false;
					GenericAttached = true;
					this.Devices.GenericGamepad = device as Gamepad;
					this.UniSenseID = unisenseID;
					NewUniSenseConnectionHandler.userLookup.AddValue(device.deviceId.ToString(), unisenseID);
					break;
				default: 
					return false;
            }
			ReadyToConnect = true;
			return true;
        }

		/// <summary>
		/// Sets the device this user is using
		/// </summary>
		/// <param name="controllerType"></param>
		/// <returns>True if successful</returns>
		public bool SetActiveDevice(DeviceType deviceType) //Will automatically open and close connections to controllers
        {
			if (!_playerInputPaired) return false;
			if(deviceType == ActiveDevice) return true;
			_playerInput.user.UnpairDevices();
			if (ConnectionOpen)
			{
				switch (ActiveDevice)
				{
					case DeviceType.DualSenseBT:
						CloseConnection(DeviceType.DualSenseBT, true);
						break;
					case DeviceType.DualSenseUSB:
						CloseConnection(DeviceType.DualSenseUSB, true);
						break;
					case DeviceType.GenericGamepad:
						CloseConnection(DeviceType.GenericGamepad, true);
						break;
				}
			}
            switch (deviceType)
            {
                case DeviceType.DualSenseBT:
					ConnectionOpen = OpenConnection(DeviceType.DualSenseBT);
					if (!ConnectionOpen)
					{
						ActiveDevice = DeviceType.None;
						return false;
					}
                        ActiveDevice = DeviceType.DualSenseBT;
					this.Devices.ActiveDevice = this.Devices.DualsenseBT;
					if(!_playerInput.SwitchCurrentControlScheme(new InputDevice[] { Devices.ActiveDevice })) return false;
                    break;
                case DeviceType.DualSenseUSB:
					ConnectionOpen = OpenConnection(DeviceType.DualSenseUSB);
					if (!ConnectionOpen)
					{
						ActiveDevice = DeviceType.None;
						return false;
					}
					ActiveDevice = DeviceType.DualSenseUSB;
					this.Devices.ActiveDevice = this.Devices.DualsenseUSB;
					Debug.Log(this.UniSenseID);
					if (!_playerInput.SwitchCurrentControlScheme(new InputDevice[] { Devices.ActiveDevice })) return false;
					break;
                case DeviceType.GenericGamepad:
					ConnectionOpen = OpenConnection(DeviceType.GenericGamepad);
					if (!ConnectionOpen)
					{
						ActiveDevice = DeviceType.None;
						return false;
					}
					ActiveDevice = DeviceType.GenericGamepad;
					this.Devices.ActiveDevice = this.Devices.GenericGamepad;
					if (!_playerInput.SwitchCurrentControlScheme(new InputDevice[] { Devices.ActiveDevice })) return false;
					break;
                case DeviceType.None:
					ActiveDevice = DeviceType.None;
					break;
                default:
                    break;
            }
			return true;
        }
		/// <summary>
		/// Returns true if successful
		/// </summary>
		/// <param name="controllerType"></param>
		/// <returns></returns>
		private bool OpenConnection(DeviceType deviceType)
		{
			if (deviceType == DeviceType.DualSenseBT) 
			{



				DS5W_ReturnValue status = (_osType == OS_Type._x64) ? DS5W_x64.findDevice(ref Devices.enumInfoBT, SerialNumber):
																	  DS5W_x86.findDevice(ref Devices.enumInfoBT, SerialNumber);
				if (status != DS5W_ReturnValue.OK)
				{
					Debug.Log(status.ToString());
					return false;
				}
				status = (_osType == OS_Type._x64) ? DS5W_x64.initDeviceContext(ref Devices.enumInfoBT, ref Devices.contextBT):
													 DS5W_x86.initDeviceContext(ref Devices.enumInfoBT, ref Devices.contextBT);

				if (status == DS5W_ReturnValue.OK) return true;
				else Debug.LogError(status.ToString());

				return false;
			}
			return true;
        }
		/// <summary>
		/// Closes the connection of a specific device type
		/// </summary>
		/// <param name="deviceType"></param>
		/// <returns>True if successful </returns>
		private bool CloseConnection(DeviceType deviceType, bool clearOutput)
        {
			switch (deviceType)
			{
				case DeviceType.DualSenseBT:
					try
					{
						if (_osType == OS_Type._x64)
						{
							DS5W_x64.freeDeviceContext(ref Devices.contextBT, clearOutput);
						}
						else
						{
							DS5W_x86.freeDeviceContext(ref Devices.contextBT, clearOutput);
						}
						Devices.enumInfoBT._internal.path = string.Empty;
					}
					catch (Exception ex)
					{
						Debug.LogError(ex.ToString());
						return false;
					}
					break;
				case DeviceType.DualSenseUSB:
					if (!clearOutput) return true;
					DualSenseHIDOutputReport report = DualSenseHIDOutputReport.Create();
					Devices.DualsenseUSB?.ExecuteCommand(ref report);
					break;
				case DeviceType.GenericGamepad:
					if (!clearOutput) return true;
					Devices.GenericGamepad?.ResetHaptics();
					break;
			}
			if(ActiveDevice == deviceType) ConnectionOpen = false;
			return true;
		}
		//TODO: Test if InputUser.UnpairDevice works
		/// <summary>
		/// Method to remove a disconnected device
		/// </summary>
		/// <param name="deviceType"></param>
		/// <returns>True if successful </returns>
		internal bool RemoveDevice(DeviceType deviceType)
        {
			if(deviceType == ActiveDevice && _playerInputPaired) _playerInput.user.UnpairDevices();
			
            switch (deviceType)
            {
                case DeviceType.DualSenseBT:
					if(!BTAttached) return false;
					if(!NewUniSenseConnectionHandler.userLookup.RemoveByKey(Devices.DualsenseBT.description.serial))
					{
						Debug.LogError("Failed to remove key");
						return false;
                    }
					CloseConnection(deviceType, false);
					this.BTAttached = false;
					this.SerialNumber = string.Empty;
                    break;
                case DeviceType.DualSenseUSB:
					if(!USBAttached) return false;
					if (!NewUniSenseConnectionHandler.userLookup.RemoveByKey(Devices.DualsenseUSB.deviceId.ToString()))
					{
						Debug.LogError("Failed to remove key");
						return false;
					}
					CloseConnection(deviceType, false);
					USBAttached = false;
					break;
                case DeviceType.GenericGamepad:
					if (!GenericAttached) return false;
					if (!NewUniSenseConnectionHandler.userLookup.RemoveByKey(Devices.GenericGamepad.deviceId.ToString()))
					{
						Debug.LogError("Failed to remove key");
						return false;
					}
					CloseConnection(deviceType, false);
					GenericAttached = false;
					break;
            }
			
			if(!IsSomthingAttached)
            {
				ReadyToConnect = false;
				return true;
            }
			if(DontOpenWirelessConnection && !GenericAttached && !USBAttached)
            {
				ReadyToConnect = false;
				return true;
            }
           return true;
		}
		/// <summary>
		/// Closes all connections to active devices and resets this user to default;
		/// </summary>
		public void ClearUser(bool resetHaptics)
		{
			if(_playerInputPaired) _playerInput.user.UnpairDevices();
            switch (ActiveDevice)
            {
                case DeviceType.DualSenseBT:
					CloseConnection(DeviceType.DualSenseBT, resetHaptics);
                    break;
                case DeviceType.DualSenseUSB:
					CloseConnection(DeviceType.DualSenseUSB, resetHaptics);
					break;
                case DeviceType.GenericGamepad:
					CloseConnection(DeviceType.GenericGamepad, resetHaptics);
					break;
                case DeviceType.None:
                    break;
                default:
					break;
            }
			NewUniSenseConnectionHandler.userLookup.RemoveByValue(UniSenseID);
			SetDefualts();
        }
	}
	public enum DeviceType
	{
		DualSenseBT,
		DualSenseUSB,
		GenericGamepad,
		None
	}
	public class UniSenseDevice
	{
		public DualSenseUSBGamepadHID DualsenseUSB;
		public DualSenseBTGamepadHID DualsenseBT;
		public Gamepad GenericGamepad;
		public DeviceContext contextBT;
		public DeviceEnumInfo enumInfoBT;
		public InputDevice ActiveDevice;
	}
	public class UserIndexFinder
	{
		Dictionary<int, List<string>> valueKeyLookup = new Dictionary<int, List<string>>();
		Dictionary<string, int> unisenseIdLookup = new Dictionary<string, int>();
		public void AddValue(string key, int value)
		{
			if (unisenseIdLookup.ContainsKey(key)) return;
			if (valueKeyLookup.TryGetValue(value, out List<string> list))
			{
				valueKeyLookup[value].Add(key);
				unisenseIdLookup.Add(key, value);
			}
			else
			{
				valueKeyLookup.Add(value, new List<string> { key });
				unisenseIdLookup.Add(key, value);
			}
		}
		public bool TryGetUnisenseId(string key, out int unisenseId)
		{
			return unisenseIdLookup.TryGetValue(key, out unisenseId);
		}

		public bool RemoveByValue(int unisenseId)
		{
			if (valueKeyLookup.TryGetValue(unisenseId, out List<string> list))
			{
				foreach (string key in list)
				{
					if (!unisenseIdLookup.Remove(key)) return false;
				}
				return valueKeyLookup.Remove(unisenseId);
			}
			return false;
		}

		public bool RemoveByKey(string key)
		{
			if (!unisenseIdLookup.TryGetValue(key, out int unisenseId)) return false;
			if ((valueKeyLookup.TryGetValue(unisenseId, out List<string> list)))
			{
				if (!valueKeyLookup[unisenseId].Remove(key)) return false;
				return unisenseIdLookup.Remove(key);
			}
			return false;
		}
	}
	#endregion
	//TODO: Add a input action that will allow the change of current user
	public static class NewUniSenseConnectionHandler
    {
		#region Variables
		private static Queue<DualSenseUSBGamepadHID> _devicesToPair = new Queue<DualSenseUSBGamepadHID>();
		private static Queue<int> _lookingForPair = new Queue<int>();
		private static Queue<int> _availableUnisenseId = new Queue<int>();
        private static OS_Type _osType;
		internal static UserIndexFinder userLookup = new UserIndexFinder();
		private static UniSenseUser[] _unisenseUsers;
		
		/// <summary>
		/// NEVER set values like UnisenseUsers[0] = new UniSenseUser();
		/// </summary>
		public static UniSenseUser[] UnisenseUsers { get { return _unisenseUsers; } }

     
		private const int _persistentPerSecond = 875; //Approximate amount the persistent counter will count per second
		private const int _fastPerSecond = 3000000; //Approximate amount the fast counter will count per second
		private const int _framePerSecond = 250; //Approximate amount the frame counter will count per second
		private const int _frameEpsilon = 6;
		private const int _fastEpsilon = 60000;
		private const int _persistantEpsilon = 20;
		private const decimal _timeEpsilon = 0.03m;
		private static int _monoHash = 0;
		public static bool IsInitialized { get; private set;}
		private static bool _isMultiplayer;
		private static bool _allowGenericGamepad;
		private static bool _allowKeyboardMouse;
		private const int _maxPlayersDefualt = 16;
		private static DeviceEnumInfo[] _enumInfos;
		private static bool _deviceMatchQueued = false;
		private static bool _initializationQueued = false;
		private static InputAction _usbConnectedAction = new InputAction();
		private static int _currentUserIndex = -1;
		private static IHandleMultiplayer _handleMultiplayer;
		private static IHandleSingleplayer _handleSingleplayer;
		public static ref UniSenseUser CurrentUser
        {
			get { return ref _unisenseUsers[_currentUserIndex]; }
        }
		public static int MaxPlayers { get { return _unisenseUsers.Length; } }
		#endregion

		#region Events & Hooks

		#endregion

		//Ensures that this class is initialized only in windows, otherwise the application is quit
		//Also ensures that when _osType is accessed it will be set correctly
		static NewUniSenseConnectionHandler()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
				#if UNITY_EDITOR
				EditorApplication.Exit(0);
				throw new Exception("Current Platform Not Supported");
				#else
				Application.Quit();
				throw new Exception("Current Platform Not Supported");
				#endif
			}
			IsInitialized = false;
			_osType = (IntPtr.Size == 4) ? OS_Type._x86 : OS_Type._x64;
			EnsureClosure();
		}

		


		private static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
          if(stateChange == PlayModeStateChange.ExitingPlayMode) Destroy();
        }

		/// <summary>
		/// Initialize unisense in single player mode
		/// </summary>
		/// <param name="singleplayerListener"></param>
		/// <param name="allowKeyboardMouse"></param>
		/// <param name="allowGenergicGamepad"></param>
		/// <returns>True if successful</returns>
		public static bool InitializeSingleplayer(IHandleSingleplayer singleplayerListener, bool allowKeyboardMouse = false, bool allowGenergicGamepad = false)
        {
            if (IsInitialized || singleplayerListener == null) return false;
			IsInitialized = true;
			_handleSingleplayer = singleplayerListener;
			return FinishInitialization(false, allowKeyboardMouse, allowGenergicGamepad, _maxPlayersDefualt);
		}

		/// <summary>
		/// Initialize unisense in multi player mode
		/// </summary>
		/// <param name="maxPlayers"></param>
		/// <param name="multiplayerListener"></param>
		/// <param name="allowKeyboardMouse"></param>
		/// <param name="allowGenergicGamepad"></param>
		/// <returns>True if successful</returns>
		public static bool InitializeMultiplayer(IHandleMultiplayer multiplayerListener, bool allowKeyboardMouse = false, bool allowGenergicGamepad = false, int maxPlayers = _maxPlayersDefualt)
		{
			if (IsInitialized || multiplayerListener == null) return false;
			IsInitialized = true;
			_handleMultiplayer = multiplayerListener;
			return FinishInitialization(true, allowKeyboardMouse, allowGenergicGamepad, maxPlayers);
		}



		private static bool FinishInitialization(bool isMultiplayer, bool allowKeyboardMouse, bool allowGenergicGamepad, int maxPlayers)
        {
			_isMultiplayer = isMultiplayer;
			_allowGenericGamepad = allowGenergicGamepad;
			_allowKeyboardMouse = allowKeyboardMouse;
			_usbConnectedAction.Enable();
			_usbConnectedAction.AddBinding("<DualSenseBTGamepadHID>/usbConnected");
			_usbConnectedAction.performed += OnUSBConnected;
			_usbConnectedAction.canceled += OnUSBRemoved;
            InputSystem.onAfterUpdate += InputSystemUpdate;

			_unisenseUsers = new UniSenseUser[maxPlayers];
			for (int i = 0; i < maxPlayers; i++)
			{
				_availableUnisenseId.Enqueue(i);
				_unisenseUsers[i] = new UniSenseUser();
			}

			//TODO: Could Combine all three foreach loops into one would be easy because Gamepad.all contains usb and bt dualsense gamepads
			//now a generic gamepad has the potentail to become the current user just need to modify that

			Gamepad[] btGamepads = DualSenseBTGamepadHID.FindAll();
			Gamepad[] usbGamepads = DualSenseUSBGamepadHID.FindAll();
			Gamepad[] genericGamepads = Gamepad.all.ToArray();
			if (btGamepads != null && btGamepads.Length > 0)
			{
				foreach (Gamepad gamepad in btGamepads)
				{
					int unisenseId = _availableUnisenseId.Dequeue();
					_unisenseUsers[unisenseId].AddDevice(gamepad, DeviceType.DualSenseBT, unisenseId);
					_lookingForPair.Enqueue(unisenseId);
				}
			}


			if (usbGamepads != null && usbGamepads.Length > 0)
			{
				foreach (Gamepad gamepad in usbGamepads)
				{
					_devicesToPair.Enqueue(gamepad as DualSenseUSBGamepadHID);
				}
			}


			if (allowGenergicGamepad && genericGamepads != null && genericGamepads.Length > 0)
			{
				foreach (Gamepad gamepad in genericGamepads)
				{
					if (gamepad is DualSenseUSBGamepadHID) continue;
					if(gamepad is DualSenseBTGamepadHID) continue;
					int unisenseId = _availableUnisenseId.Dequeue();
					_unisenseUsers[unisenseId].AddDevice(gamepad, DeviceType.GenericGamepad, unisenseId);
				}
			}
			DS5WEnumDevices(ref _enumInfos, MaxPlayers);
			DS5WMatchEnumInfoToUnisenseUser(_enumInfos);

			if (btGamepads != null && btGamepads.Length > 0)
			{
				foreach (Gamepad gamepad in btGamepads)
				{
					InputSystem.EnableDevice(gamepad);
				}
			}

			if (FindNewCurrentUser(out int Id)) _currentUserIndex = Id;
			QueueDeviceInitialization();
			return true;
		}

        private static void InputSystemUpdate()
        {
			if(DevicesToEnable.TryDequeue(out InputDevice device)) InputSystem.EnableDevice(device);
        }

        private static void OnUSBRemoved(InputAction.CallbackContext obj)
        {
			
			InputDevice device = obj.control.device;
			if (!(device is DualSenseBTGamepadHID))
			{
				//TODO: Add real error message
				Debug.LogError("How?");
				return;
			}
			int unisenseId;
			if (!userLookup.TryGetUnisenseId(device.description.serial, out unisenseId))
			{
				Debug.LogError("Controller not registered");
				return;
			}
			Debug.Log("USB Removed : " + unisenseId);

			if (_lookingForPair.Contains(unisenseId))
            {
				UpdateLookingForPair();
            }
		}

		private static void UpdateLookingForPair()
        {
			Queue<int> lookingForPair = new Queue<int>();
			foreach(int unisenseId in _lookingForPair)
            {
				if (_unisenseUsers[unisenseId].Devices.DualsenseBT.usbConnected.isPressed) lookingForPair.Enqueue(unisenseId);
            }
			_lookingForPair = lookingForPair;
        }

        private static void OnUSBConnected(InputAction.CallbackContext obj)
        {
			InputDevice device = obj.control.device;
			if (!(device is DualSenseBTGamepadHID))
            {
				//TODO: Add real error message
				Debug.LogError("How?");
				return;
            }
			int unisenseId;
            if(!userLookup.TryGetUnisenseId(device.description.serial, out unisenseId))
            {
				Debug.LogError("Controller not registered");
				return;
            }
			if(_lookingForPair.Contains(unisenseId)) return;
			_unisenseUsers[unisenseId].SetActiveDevice(DeviceType.None);
			_lookingForPair.Enqueue(unisenseId);
			if(_isMultiplayer)
            {
				_handleMultiplayer.OnUserModified(unisenseId, UserChange.BTDisabled);
			}
            else
            {
				if(unisenseId == _currentUserIndex)
                {
					_handleSingleplayer.OnCurrentUserModified(UserChange.BTDisabled);
                }
            }
        }


        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
			int unisenseId = -1;
			string key = string.Empty;
			switch (change)
            {
                case InputDeviceChange.Added:
					switch (device)
					{
						case DualSenseBTGamepadHID:
							if (!_availableUnisenseId.TryDequeue(out unisenseId))
							{
								Debug.LogError("Max user count reached");
								return;
							}
							DeviceEnumInfo[] deviceEnumInfo = new DeviceEnumInfo[MaxPlayers];
							DS5WEnumDevices(ref deviceEnumInfo, MaxPlayers);
							DS5WMatchEnumInfoToUnisenseUser(deviceEnumInfo);
							DevicesToEnable.Enqueue(device);
							if(!_unisenseUsers[unisenseId].AddDevice(device, DeviceType.DualSenseBT, unisenseId))
                            {
								Debug.LogError("failed to add bt device");
                            }
                            if (_isMultiplayer)
                            {
								_handleMultiplayer.OnUserAdded(unisenseId);
                            }
                            else
                            {
								_currentUserIndex = unisenseId;
								_handleSingleplayer.OnCurrentUserChanged(unisenseId);
                            }
							//TODO: allow for BT gamepads that are connected after to match with usb
							//Don't need to check if usb connected becuase OnUSBConnected should be called after the first inputupdate
							//QueueDeviceMatch();
							
							
							break;
						case DualSenseUSBGamepadHID:
							_devicesToPair.Enqueue(device as DualSenseUSBGamepadHID); 
							QueueDeviceMatch();
							break;
						case Gamepad:
							if (!_availableUnisenseId.TryDequeue(out unisenseId))
							{
								Debug.LogError("Max user count reached");
								return;
							}
							if (!_unisenseUsers[unisenseId].AddDevice(device, DeviceType.GenericGamepad, unisenseId))
							{
								Debug.LogError("failed to add Generic device");
							}
							if (_isMultiplayer)
							{
								_handleMultiplayer.OnUserAdded(unisenseId);
							}
							else
							{
								_currentUserIndex = unisenseId;
								_handleSingleplayer.OnCurrentUserChanged(unisenseId);
							}
							break;

					}
					break;
                case InputDeviceChange.Removed:
					switch (device)
					{
						case DualSenseBTGamepadHID:
							key = device.description.serial;
							if(!userLookup.TryGetUnisenseId(key, out unisenseId))
                            {
								Debug.LogError("No UnisenseId found");
								return;
                            }
							_unisenseUsers[unisenseId].RemoveDevice(DeviceType.DualSenseBT);
							if (_isMultiplayer)
							{
								if (_unisenseUsers[unisenseId].IsReadyToConnect)
								{
									_handleMultiplayer.OnUserModified(unisenseId, UserChange.BTRemoved);
								}
								else
								{
									_unisenseUsers[unisenseId].ClearUser(false);
									_availableUnisenseId.Enqueue(unisenseId);
									_handleMultiplayer.OnUserRemoved(unisenseId);
								}
							}
							else
                            {
								if (unisenseId != _currentUserIndex) return;
								if (_unisenseUsers[unisenseId].IsReadyToConnect)
								{
									_handleSingleplayer.OnCurrentUserModified(UserChange.BTRemoved);
								}
								else
								{
									_unisenseUsers[unisenseId].ClearUser(false);
									_availableUnisenseId.Enqueue(unisenseId);
									if (!FindNewCurrentUser(out int newId))
									{
										_handleSingleplayer.OnNoCurrentUser();
										return;
									}
									_currentUserIndex = newId;
									_handleSingleplayer.OnCurrentUserChanged(newId);
								}
							}
							break;
						case DualSenseUSBGamepadHID:
							key = device.deviceId.ToString();
							if (!userLookup.TryGetUnisenseId(key, out unisenseId))
							{
								Debug.LogError("No UnisenseId found");
								return;
							}
							_unisenseUsers[unisenseId].RemoveDevice(DeviceType.DualSenseUSB);

							if (_isMultiplayer)
							{
								if (_unisenseUsers[unisenseId].IsSomthingAttached)
								{
									_handleMultiplayer.OnUserModified(unisenseId, UserChange.USBRemoved);
								}
								else
								{
									_unisenseUsers[unisenseId].ClearUser(false);
									_availableUnisenseId.Enqueue(unisenseId);
									_handleMultiplayer.OnUserRemoved(unisenseId);
								}
							}
							else
							{
								if (unisenseId != _currentUserIndex) return;
								if (_unisenseUsers[unisenseId].IsSomthingAttached)
								{
									_handleSingleplayer.OnCurrentUserModified(UserChange.USBRemoved);
								}
								else
								{
									_unisenseUsers[unisenseId].ClearUser(false);
									_availableUnisenseId.Enqueue(unisenseId);
									if (!FindNewCurrentUser(out int newId))
									{
										_handleSingleplayer.OnNoCurrentUser();
										return;
									}
									_currentUserIndex = newId;
									_handleSingleplayer.OnCurrentUserChanged(newId);
								}
							}
							break;
						case Gamepad:
							key = device.deviceId.ToString();
							if (!userLookup.TryGetUnisenseId(key, out unisenseId))
							{
								Debug.LogError("No UnisenseId found");
								return;
							}
                            _unisenseUsers[unisenseId].RemoveDevice(DeviceType.GenericGamepad);
							_unisenseUsers[unisenseId].ClearUser(false);
							_availableUnisenseId.Enqueue(unisenseId);
							if (_isMultiplayer)
                            {
								_handleMultiplayer.OnUserRemoved(unisenseId);
                            }
                            else
                            {
								if (unisenseId != _currentUserIndex) return;
								if (!FindNewCurrentUser(out int newId))
								{
									_handleSingleplayer.OnNoCurrentUser();
									return;
								}
								_currentUserIndex = newId;
								_handleSingleplayer.OnCurrentUserChanged(newId);
							}
							break;
					}
					break;
            }
        }

		private static bool Destroy()
        {
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.wantsToQuit -= Destroy;
			InputSystem.onDeviceChange -= OnDeviceChange;
			_usbConnectedAction.performed -= OnUSBConnected;
			_usbConnectedAction.canceled -= OnUSBRemoved;
			InputSystem.onAfterUpdate -= InputSystemUpdate;
			if(_deviceMatchQueued) InputSystem.onAfterUpdate -= MatchDevicesOnNextUpdate;
			for(int i = 0; i < _unisenseUsers.Length; i++)
            {
				_unisenseUsers[i].ClearUser(true);
            }
			return true; //when called by EditorApplication.wantsToQuit returning true allows the application to exit
		}

		#region Helper Methods
	
		/// <summary>
		/// Enumerate dualSense devices on the DS5W side
		/// </summary>
		/// <param name="infos"></param>
		/// <param name="Arraysize"></param>
		private static void DS5WEnumDevices(ref DeviceEnumInfo[] infos, int Arraysize)
		{
			uint discoveredDeviceCount = 0;
			IntPtr ptrDeviceEnum = IntPtr.Zero;
			infos = new DeviceEnumInfo[Arraysize];
			DS5WHelpers.BuildEnumDeviceBuffer(ref ptrDeviceEnum, infos);
			DS5W_ReturnValue status = (_osType == OS_Type._x64) ? DS5W_x64.enumDevices(ref ptrDeviceEnum, (uint)Arraysize, ref discoveredDeviceCount, false) :
																  DS5W_x86.enumDevices(ref ptrDeviceEnum, (uint)Arraysize, ref discoveredDeviceCount, false);
			if (status != DS5W_ReturnValue.OK) UnityEngine.Debug.LogError(status.ToString());
			DS5WHelpers.DeconstructEnumDeviceBuffer(ref ptrDeviceEnum, ref infos);
		}

		/// <summary>
		/// Adds Information from a DeviceEnumInfo array to <see cref="_unisenseUsers"/>
		/// </summary>
		/// <param name="DeviceEnum"></param>
		private static void DS5WMatchEnumInfoToUnisenseUser(DeviceEnumInfo[] DeviceEnum) //Maybey make so can't add a new device. DONE
		{
			if (DeviceEnum == null) return;
			foreach (DeviceEnumInfo info in DeviceEnum)
			{
				if (string.IsNullOrEmpty(info._internal.path)) continue;
				if (info._internal.Connection != DeviceConnection.BT) continue;
				string _serialNumber = info._internal.serialNumber;
				if (userLookup.TryGetUnisenseId(_serialNumber, out int unisenseId)) //Check if the devices unity counterpart already exists
				{
					_unisenseUsers[unisenseId].Devices.enumInfoBT = info;
				}
			}
		}

		public static bool RemoveCurrentUser()
        {
			if(_currentUserIndex != -1) CurrentUser.ClearUser(false);
			if (!FindNewCurrentUser(out int unisenseId)) return false;
			_currentUserIndex = unisenseId;
			_handleSingleplayer.OnCurrentUserChanged(unisenseId);
			return true;
        }

		private static bool FindNewCurrentUser(out int unisenseId)
        {
			for(unisenseId = 0; unisenseId < _unisenseUsers.Length; unisenseId++)
            {
				if(_unisenseUsers[unisenseId].IsReadyToConnect) return true;
            }
			return false;
        }

		//Note: if at runtime the same DualSense is connected via USB THEN BT it won't pair correctly.
		//Not a huge problem, just disable the BT counterpart if it shows a USB controller is detected
		//Dynamic switching between BT and USB won't work but it's such a rare edge case that this solution is good enough.
		//Keep in mind this isn't a problem in single player, since any controller will control the same player.
		//TODO: need to make sure that it isn't possible to add a device after the input system update and still be considered here;
		private static void MatchDevices()
		{
			List<DualSenseUSBGamepadHID> devicesToPair = _devicesToPair.ToList();
			Queue<int> stillLookingForPair = new Queue<int>();
			foreach (int unisenseID in _lookingForPair)
			{

				bool pairingSuccsesful = false;
				if (!_unisenseUsers[unisenseID].BTAttached) continue;
				DualSenseBTGamepadHID lookingForPair = _unisenseUsers[unisenseID].Devices.DualsenseBT;
				Debug.Log("Batt: " + lookingForPair.batteryLevel.ReadValue());
				if (lookingForPair.usbConnected.isPressed)
				{
					if (!GetCounters(_unisenseUsers[unisenseID].Devices.DualsenseBT, out uint inputFastCounter, out uint inputPersistentCounter, out uint inputFrameCounter))
					{
						Debug.LogError("Error retrieving counters");
						continue;
					}
					foreach (InputDevice device in devicesToPair)
					{
						if (!(device is DualSenseUSBGamepadHID))
						{
							Debug.LogError("Not DualSense usb device");
							continue;
						}
						if (Math.Abs(lookingForPair.batteryLevel.ReadValue() - (device as DualSenseUSBGamepadHID).batteryLevel.ReadValue()) > 1) continue;
						if (!GetCounters(device, out uint fastCounter, out uint persistentCounter, out uint frameCounter))
						{
							Debug.LogError("Error retrieving counters");
							continue;
						}
						uint fastDelta = UintDelta(inputFastCounter, fastCounter, 4);
						uint persistentDelta = UintDelta(inputPersistentCounter, persistentCounter, 4);
						uint frameDelta = UintDelta(inputFrameCounter, frameCounter, 1);
						if (fastDelta <= _fastEpsilon && persistentDelta <= _persistantEpsilon && frameDelta <= _frameEpsilon)
						{
							//Could multiply by the inverse to speed up but not worth readability cost
							decimal fastTimepassed = (decimal)fastDelta / (decimal)_fastPerSecond;
							decimal persistantTimepassed = (decimal)persistentDelta / (decimal)_persistentPerSecond;

							decimal timeDelta = Math.Abs(fastTimepassed - persistantTimepassed);
							if (timeDelta > _timeEpsilon) continue;
							if (!devicesToPair.Remove(device as DualSenseUSBGamepadHID)) Debug.LogError("can't remove device");
							
							//Successfully matched the USB device to it's BT counterpart
							_unisenseUsers[unisenseID].AddDevice(device, DeviceType.DualSenseUSB, unisenseID);
                            if (_isMultiplayer)
                            {
								_handleMultiplayer.OnUserModified(unisenseID, UserChange.USBAdded);
							}
                            else
                            {
								if(unisenseID == _currentUserIndex) _handleSingleplayer.OnCurrentUserModified(UserChange.USBAdded);
								else
                                {
									_currentUserIndex = unisenseID;
									_handleSingleplayer.OnCurrentUserChanged(unisenseID);

								}
                                
                            }
                            
							pairingSuccsesful = true;
							break;
						}
					}
					if (pairingSuccsesful) continue;
					stillLookingForPair.Enqueue(unisenseID);
					
					continue;
				}
				
			}
			//Just finished tyring to match controllers deal with the remainder
			//Just add USB devices as new users
			//Keep BT devices that are plugged in the _lookingForPair list to try again
			_lookingForPair = stillLookingForPair;
			foreach (InputDevice device in devicesToPair)
			{
				if (!_availableUnisenseId.TryDequeue(out int unisenseId) || !_unisenseUsers[unisenseId].AddDevice(device, DeviceType.DualSenseUSB, unisenseId))
				{
					Debug.LogError("can't add device");
					continue;
				}
                if (_isMultiplayer)
                {
					_handleMultiplayer.OnUserAdded(unisenseId);
				}
                else
                {
					_currentUserIndex = unisenseId;
					_handleSingleplayer.OnCurrentUserChanged(unisenseId);
                }
				
			}
			_devicesToPair.Clear();
			_lookingForPair = stillLookingForPair;
		}

		private static void QueueDeviceInitialization()
        {
			if (_initializationQueued) return;
			InputSystem.Update();
			InputSystem.onAfterUpdate += InitializeOnNextInputUpdate;
			_initializationQueued = true;
		}
		static int time = 0;

		private static Queue<InputDevice> DevicesToEnable = new Queue<InputDevice>();
		

	 

		

		//TODO: Fix this so users are initialized after the USB is matched
		//Necessitates a change to MatchDevices
		private static void InitializeOnNextInputUpdate()
        {
			if (!_initializationQueued) return;
			time++;
			if (time < 50) return;
			time = 0;
			InputSystem.onAfterUpdate -= InitializeOnNextInputUpdate;
            if (_isMultiplayer)
            {
				_handleMultiplayer.InitilizeUsers();
            }
			else
            {
				_handleSingleplayer.InitilizeUsers(_currentUserIndex);
			}
			MatchDevices();
			_initializationQueued = false;

        }
		private static void QueueDeviceMatch()
		{
			if (_deviceMatchQueued) return;
			InputSystem.onAfterUpdate += MatchDevicesOnNextUpdate;
			_deviceMatchQueued = true;
		}

		private static void MatchDevicesOnNextUpdate()
		{
			if (!_deviceMatchQueued) return;
			InputSystem.onAfterUpdate -= MatchDevicesOnNextUpdate;
			MatchDevices();
			_deviceMatchQueued = false;
		}

		/// <summary>
		/// Gets the counters used for device matching from a DualSense device
		/// </summary>
		/// <param name="device"></param>
		/// <param name="fastCounter"></param>
		/// <param name="persistentCounter"></param>
		/// <param name="frameCounter"></param>
		/// <returns>True if successful</returns>
		private static unsafe bool GetCounters(InputDevice device, out uint fastCounter, out uint persistentCounter, out uint frameCounter)
        {	
			fastCounter = 0;
			persistentCounter = 0;
			frameCounter = 0;
			int reportSize;
			int offset = 0;

            switch (device) //I'm surprised this worked
			{

				case DualSenseBTGamepadHID:
					reportSize = sizeof(DualSenseBTHIDInputReport);
					offset = 1;
					break;
				case DualSenseUSBGamepadHID:
					reportSize= sizeof(DualSenseUSBHIDInputReport);
					break;

				default: return false; 



			}
			byte* reportBuffer = stackalloc byte[reportSize];
			device.CopyState(reportBuffer, reportSize);
			fastCounter = BitConverter.ToUInt32(new Span<byte>(reportBuffer + offset + 49, 4));
			frameCounter = reportBuffer[offset + 7];

				//BitConverter.ToUInt32(new Span<byte>(reportBuffer + offset + 7, 1));
			persistentCounter = BitConverter.ToUInt32(new Span<byte>(reportBuffer + offset + 12, 4));
            return true;
        }


		/// <summary>
		/// Return the delta between two uint taking into account wrap around and the length of the bytes
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="byteLength"></param>
		/// <returns></returns>
		private static uint UintDelta(uint x, uint y, int byteLength)
		{

			uint max = (byteLength >= 4) ? uint.MaxValue : (uint)1 << (byteLength * 8);
			if (byteLength > 4)
			{
				Debug.LogError(byteLength + " Is outside of range");
				return 0;
			}
			uint diff = (x > y) ? x - y : y - x;
			uint altDiff = (byteLength == 4) ? (max - diff) + 1 : (max - diff); //adding one here is safe in the event diff is zero adding one to uint.MaxValue will wrap result back to zero.
			return (diff > altDiff) ? altDiff : diff; //altDiff and diff can both be zero, but in that case zero will still be returned so no issue there;
		}


		/// <summary>
		/// Get a devices input report as raw array of bytes
		/// </summary>
		/// <returns>
		/// True if successful, false otherwise
		/// </returns>
		/// <remarks>
		/// By default this method will add one to the start index of a Bluetooth DualSense controller to match the USB style input report
		/// </remarks>
		/// <param name="device"></param>
		/// <param name="controllerType"></param>
		/// <param name="bytes"></param>
		private static unsafe bool GetRawInputReport(InputDevice device, DeviceType controllerType, out byte[] bytes, bool autoOffsetBT = true)
		{
			bytes = null;
			if (controllerType == DeviceType.GenericGamepad) return false;
			int reportSize;
			int startIndex = 0;
			switch (controllerType)
			{
				case DeviceType.None:
					return false;
				case DeviceType.DualSenseBT:
					startIndex += 1;
					reportSize = sizeof(DualSenseBTHIDInputReport);
					break;
				case DeviceType.DualSenseUSB:
					reportSize = sizeof(DualSenseUSBHIDInputReport);
					break;
				default: return false;
			}
			byte* stackBuffer = stackalloc byte[reportSize];
			device.CopyState(stackBuffer, reportSize);
			byte[] buffer = new Span<byte>(stackBuffer + startIndex, reportSize - startIndex).ToArray();
			bytes = buffer;
			return true;
		}

		private static void EnsureClosure()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			EditorApplication.wantsToQuit += Destroy;
			InputSystem.onDeviceChange += OnDeviceChange;
		}
		#endregion

	}

}
