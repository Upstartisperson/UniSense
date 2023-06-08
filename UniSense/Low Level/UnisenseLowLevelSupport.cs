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
using UnityEngine.InputSystem.DualSense;
using UnityEngine.Scripting;
using System.Text;
using UniSense.DS5WWrapper;

namespace UniSense.LowLevel
{
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
	public class UnisenseUser
	{

		public int UniSenseID { get; private set; }
		public string SerialNumber { get; private set; }
		public bool ConnectionOpen { get; private set; }
		public bool ReadyToConnect { get; private set; }
		public bool Enabled { get; private set; }
		public UniSenseDevice Devices { get; private set; }
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
		/// <summary>
		/// True if the BT DualSense reports an active USB counterpart
		/// </summary>
		public bool DontOpenWirelessConnection { get { return BTAttached && Devices.DualsenseBT.usbConnected.isPressed; } }
		public DeviceType ActiveDevice { get; private set; }
	
		private PlayerInput _playerInput;
		private bool _playerInputPaired;
		
		
		private static OS_Type _osType;
		public static UserIndexFinder userLookup { get; private set; }
		public static bool TryGetUnisenseId(string key, out int unisenseId) => userLookup.TryGetUnisenseId(key, out unisenseId);

		static UnisenseUser()
        {
			_osType = (IntPtr.Size == 4) ? OS_Type._x86 : OS_Type._x64;
			userLookup = new UserIndexFinder();
		}

		public UnisenseUser()
		{
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
			if (_playerInputPaired) return false;
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
			if (_playerInput.devices.Count > 0) _playerInput.user.UnpairDevices();

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
					if (BTAttached) return false;
					BTAttached = true;
					this.Devices.DualsenseBT = device as DualSenseBTGamepadHID;
					this.UniSenseID = unisenseID;
					this.SerialNumber = device.description.serial;
					userLookup.AddValue(this.SerialNumber, unisenseID);
					break;
				case DeviceType.DualSenseUSB:
					if (USBAttached) return false;
					USBAttached = true;
					this.Devices.DualsenseUSB = device as DualSenseUSBGamepadHID;
					this.UniSenseID = unisenseID;
					userLookup.AddValue(device.deviceId.ToString(), unisenseID);
					break;
				case DeviceType.GenericGamepad:
					if (GenericAttached) return false;
					GenericAttached = true;
					this.Devices.GenericGamepad = device as Gamepad;
					this.UniSenseID = unisenseID;
					userLookup.AddValue(device.deviceId.ToString(), unisenseID);
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
			if (deviceType == ActiveDevice) return true;
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
					if (!_playerInput.SwitchCurrentControlScheme(new InputDevice[] { Devices.ActiveDevice })) return false;
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
			if (ActiveDevice == deviceType) ConnectionOpen = false;
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
			if (deviceType == ActiveDevice && _playerInputPaired) _playerInput.user.UnpairDevices();

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

			if (!IsSomthingAttached)
			{
				ReadyToConnect = false;
				return true;
			}
			if (DontOpenWirelessConnection && !GenericAttached && !USBAttached)
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
			if (_playerInputPaired) _playerInput.user.UnpairDevices();
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
			userLookup.RemoveByValue(UniSenseID);
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




 



}
#endregion