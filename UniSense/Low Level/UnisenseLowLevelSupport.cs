using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using UniSense.DS5WWrapper;
using UnityEngine.InputSystem.Haptics;

namespace UniSense.LowLevel
{
    #region Interfaces
    public interface IManageable
	{
		public void SetCurrentUser(int unisenseId);
		public void OnCurrentUserModified(UserChange change);
		public void OnNoCurrentUser();
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
    #endregion

	public static class OSType
    {
		public static readonly OS_Type Type;
		static OSType()
        {
			Type = (IntPtr.Size == 4) ? OS_Type._x86 : OS_Type._x64;
        }
    }

	#region CustomDataStructurs
	public enum OS_Type
	{
		_x64,
		_x86
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
	
	//Rules : This class should never automatically change it's active device
	//Rules : This class should never automatically open or close connections
	public class UnisenseUser
	{
		//TODO: See if adding an Enable/Disable feature would be beneficial
		
		#region Fields
		#region Instance Fields
		/// <summary>
		/// The UnisenseId associated with this user 
		/// </summary>
		public int UniSenseId { get; private set; }
		/// <summary>
		/// The Serial number of the attached bluetooth device (if attached)
		/// </summary>
		public string SerialNumber { get; private set; }
		/// <summary>
		/// Does this user have an active device
		/// </summary>
		public bool ConnectionOpen { get { return ActiveDeviceType != DeviceType.None; }}
		/// <summary>
		/// Is there a valid device to open a connection with
		/// </summary>
		public bool ReadyToConnect => HaveValidDevice();
		private bool HaveValidDevice()
		{
			if (USBAttached) return true;
			if (GenericAttached) return true;
			if (BTAttached && !_isBTPluggedIn) return true;
			return false;
		}
	
		/// <summary>
		/// Does the attached BT device have a USB connection
		/// </summary>
		private bool _isBTPluggedIn = false;

		/// <summary>
		/// All the devices that are attached to this user
		/// </summary>
		public UserDevice Devices { get; private set; }

		/// <summary>
		/// Is there a BT device attached
		/// </summary>
		/// <remarks>Note: Does not take into consideration if BT SHOULD be used</remarks>
		public bool BTAttached { get; private set; }
		
		/// <summary>
		/// Is there a USB device attached
		/// </summary>
		/// <remarks>Note: Does not take into consideration if USB SHOULD be used</remarks>
		public bool USBAttached { get; private set; }
		
		/// <summary>
		/// Is there a Generic device attached
		/// </summary>
		/// <remarks>Note: Does not take into consideration if Generic SHOULD be used</remarks>
		public bool GenericAttached { get; private set; }

		/// <summary>
		/// True if anything is attached to this user
		/// </summary>
		/// <remarks>Note: Does not take into consideration if a connection SHOULD be established</remarks>
		public bool IsSomthingAttached
		{
			get { return BTAttached || USBAttached || GenericAttached; }
		}
	
		/// <summary>
		/// True if the BT DualSense reports an active USB counterpart
		/// </summary>
		public bool DontOpenWirelessConnection { get { return BTAttached && _isBTPluggedIn; } }
		
		/// <summary>
		/// What device type is active
		/// </summary>
		public DeviceType ActiveDeviceType { get; private set; }
		
		/// <summary>
		/// The PlayerInput paired with this user
		/// </summary>
		private PlayerInput _playerInput;
		
		/// <summary>
		/// Is _playerInput not null
		/// </summary>
		private bool _playerInputPaired { get { return _playerInput != null; } }
        #endregion

        #region Static Fields
        
		private static OS_Type _osType;
		public static UserIndexFinder userLookup { get; private set; }

		#endregion
		#endregion

		#region Initialization and reseting
		/// <summary>
		/// Static Initializer 
		/// </summary>
		/// <remarks>Runs automatically the first time <see cref="UnisenseUser"/> is referenced</remarks>
		static UnisenseUser()
		{

			_osType = (IntPtr.Size == 4) ? OS_Type._x86 : OS_Type._x64;
			userLookup = new UserIndexFinder();
		}

		/// <summary>
		/// Instance Initializer
		/// </summary>
		/// <remarks>Run automatically when a new instance of <see cref="UnisenseUser"/> is created</remarks>
		public UnisenseUser()
		{
			SetDefualts();
		}
		private void SetDefualts()
		{
			UniSenseId = -1;
			Devices = new UserDevice();
			BTAttached = false;
			USBAttached = false;
			GenericAttached = false;
			ActiveDeviceType = DeviceType.None;
			_playerInput = null;
		}
		/// <summary>
		/// Closes all connections to active devices and resets this user to default;
		/// </summary>
		public void ClearUser(bool resetHaptics)
		{
			if (_playerInputPaired) _playerInput.user.UnpairDevices();
			switch (ActiveDeviceType)
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
			userLookup.RemoveByValue(UniSenseId);
			SetDefualts();
		}
		#endregion

		#region Helpers
		/// <summary>
		/// Used to get a unisenseId from the <see cref="UserIndexFinder"/> userLookup stored in <see cref="UnisenseUser"/>
		/// </summary>
		/// <param name="key">Either   a serial number (for BT) or deviceId (for generic and USB)</param>
		/// <param name="unisenseId"></param>
		/// <returns>True if the key has an associated unisenseId</returns>
		public static bool TryGetUnisenseId(string key, out int unisenseId) => userLookup.TryGetUnisenseId(key, out unisenseId);
        #endregion

        #region BT Specific
        //TODO: Test if this needs to be initialized or if OnActionmPerformed will be adequate when device is plugged in when game starts
        public void BTPluggedin()
        {
			_isBTPluggedIn = true;
			if(ActiveDeviceType == DeviceType.DualSenseBT) SetActiveDevice(DeviceType.None);
        }
		public void BTUnplugged()
		{
			_isBTPluggedIn = false;
			if (ActiveDeviceType == DeviceType.DualSenseBT) SetActiveDevice(DeviceType.None);
		}
        #endregion

        #region PlayerInput Handling
        public bool PairWithPlayerInput(PlayerInput playerInput)
		{
			if (_playerInputPaired) return false;
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
			if (_playerInput.devices.Count > 0) _playerInput.user.UnpairDevices();
			_playerInput = null;
			return true;
		}
        #endregion

        #region Device Handling

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
					if (BTAttached) return false;
					BTAttached = true;
					UserDevice userDevice = new();
					userDevice.ActiveDevice = device;
					userDevice.DualsenseBT = null;
					Devices = userDevice;
					this.Devices.DualsenseBT = device as DualSenseBTGamepadHID;
					this.UniSenseId = unisenseID;
					this.SerialNumber = device.description.serial;
					userLookup.AddValue(this.SerialNumber, unisenseID);
					break;
				case DeviceType.DualSenseUSB:
					if (USBAttached) return false;
					USBAttached = true;
					this.Devices.DualsenseUSB = device as DualSenseUSBGamepadHID;
					this.UniSenseId = unisenseID;
					userLookup.AddValue(device.deviceId.ToString(), unisenseID);
					break;
				case DeviceType.GenericGamepad:
					if (GenericAttached) return false;
					GenericAttached = true;
					this.Devices.GenericGamepad = device as Gamepad;
					this.UniSenseId = unisenseID;
					userLookup.AddValue(device.deviceId.ToString(), unisenseID);
					break;
				default:
					return false;
			}
			return true;
		}

		/// <summary>
		/// Method to remove a disconnected device
		/// </summary>
		/// <param name="deviceType"></param>
		/// <returns>True if successful </returns>
		private bool RemoveDevice(DeviceType deviceType)
		{
			if (deviceType == ActiveDeviceType && _playerInputPaired) _playerInput.user.UnpairDevices();

			switch (deviceType)
			{
				case DeviceType.DualSenseBT:
					if (!BTAttached) return false;
					if (!userLookup.RemoveByKey(Devices.DualsenseBT.description.serial))
					{
						Debug.LogError("Failed to remove key");
						return false;
					}
					CloseConnection(deviceType, false);
					this.BTAttached = false;
					this.SerialNumber = string.Empty;
					break;
				case DeviceType.DualSenseUSB:
					if (!USBAttached) return false;
					if (!userLookup.RemoveByKey(Devices.DualsenseUSB.deviceId.ToString()))
					{
						Debug.LogError("Failed to remove key");
						return false;
					}
					CloseConnection(deviceType, false);
					USBAttached = false;
					break;
				case DeviceType.GenericGamepad:
					if (!GenericAttached) return false;
					if (!userLookup.RemoveByKey(Devices.GenericGamepad.deviceId.ToString()))
					{
						Debug.LogError("Failed to remove key");
						return false;
					}
					CloseConnection(deviceType, false);
					GenericAttached = false;
					break;
			}
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
			if (deviceType == ActiveDeviceType) return true;
			_playerInput.user.UnpairDevices();
			if (ConnectionOpen)
			{
				switch (ActiveDeviceType)
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
				Devices.ActiveDevice = null;
			}
			switch (deviceType)
			{
				case DeviceType.DualSenseBT:
					if (!OpenConnection(DeviceType.DualSenseBT))
					{
						ActiveDeviceType = DeviceType.None;
						return false;
					}
					if (!_playerInput.SwitchCurrentControlScheme(new InputDevice[] { Devices.ActiveDevice })) return false;
					this.Devices.ActiveDevice = this.Devices.DualsenseBT;
					ActiveDeviceType = DeviceType.DualSenseBT;
					break;
				case DeviceType.DualSenseUSB:
					
					if (!OpenConnection(DeviceType.DualSenseUSB))
					{
						ActiveDeviceType = DeviceType.None;
						return false;
					}
					if (!_playerInput.SwitchCurrentControlScheme(new InputDevice[] { Devices.ActiveDevice })) return false;
					this.Devices.ActiveDevice = this.Devices.DualsenseUSB;
					ActiveDeviceType = DeviceType.DualSenseUSB;
					break;
				case DeviceType.GenericGamepad:
					if (!OpenConnection(DeviceType.GenericGamepad))
					{
						ActiveDeviceType = DeviceType.None;
						return false;
					}
					if (!_playerInput.SwitchCurrentControlScheme(new InputDevice[] { Devices.ActiveDevice })) return false;
					this.Devices.ActiveDevice = this.Devices.GenericGamepad;
					ActiveDeviceType = DeviceType.GenericGamepad;
					break;
				case DeviceType.None:
					ActiveDeviceType = DeviceType.None;
					break;
				default:
					break;
			}
			return true;
		}
        #endregion

        #region Connection Handling
        /// <summary>
        /// Returns true if successful
        /// </summary>
        /// <param name="controllerType"></param>
        /// <returns></returns>
        private bool OpenConnection(DeviceType deviceType)
		{
			if (deviceType == DeviceType.DualSenseBT)
			{

				//TODO: Maybe just have DS5W method for connecting to device with serial number

				DS5W_ReturnValue status = (_osType == OS_Type._x64) ? DS5W_x64.findDevice(ref Devices.enumInfoBT, SerialNumber) :
																	  DS5W_x86.findDevice(ref Devices.enumInfoBT, SerialNumber);
				if (status != DS5W_ReturnValue.OK)
				{
					Debug.Log(status.ToString());
					return false;
				}
				status = (_osType == OS_Type._x64) ? DS5W_x64.initDeviceContext(ref Devices.enumInfoBT, ref Devices.contextBT) :
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
			return true;
		}

        #endregion

	}


	public enum DeviceType
	{
		DualSenseBT,
		DualSenseUSB,
		GenericGamepad,
		None
	}
	public class UserDevice
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
		private Dictionary<int, List<string>> valueKeyLookup = new Dictionary<int, List<string>>();
		private Dictionary<string, int> unisenseIdLookup = new Dictionary<string, int>();
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
}

