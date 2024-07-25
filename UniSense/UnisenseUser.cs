using UnityEngine;
using UnityEngine.InputSystem;
using WrapperDS5W;
using UniSense.LowLevel;
using UniSense;
using UniSense.Utilities;
using DeviceType = UniSense.Utilities.DeviceType;

using System;
using System.Collections.Generic;
namespace UniSense.Users
{
	internal static class UserIndexFinder
	{
		static Dictionary<int, List<string>> valueKeyLookup = new Dictionary<int, List<string>>();
		static Dictionary<string, int> unisenseIdLookup = new Dictionary<string, int>();
		internal static void AddValue(string key, int value)
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
		internal static bool TryGetUnisenseId(string key, out int unisenseId)
		{
			return unisenseIdLookup.TryGetValue(key, out unisenseId);
		}

		internal static bool RemoveByValue(int unisenseId)
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

		internal static bool RemoveByKey(string key)
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
	public class UniSenseUser
	{
        #region Static Portion
        public static UniSenseUser[] Users  = new UniSenseUser[32];
		private static OS_Type _osType;
		private static Queue<int> _availableUnisenseId = new Queue<int>();

		static UniSenseUser()
		{
			for (int i = 0; i < 32; i++) _availableUnisenseId.Enqueue(i);
			_osType = (IntPtr.Size == 4) ? OS_Type._x86 : OS_Type._x64;

		}

		/// <summary>
		/// Use This method to find the unisense ID assocated with an input device given a device ID or a serial number.
		/// </summary>
		/// <param name="key">Serial Number for BT devices and device ID for USB devices</param> 
		/// <param name="unisenseId"> Unisense Id of found device if it exsists</param>
		/// <returns>True if an item if the key matched to a unisense ID</returns>
		public static bool Find(string key, out int unisenseId) => UserIndexFinder.TryGetUnisenseId(key, out unisenseId);
        #endregion

        public int UniSenseID { get; internal set; }

		public string SerialNumber { get; private set; }
		public bool ConnectionOpen { get; private set; }
		public bool ReadyToConnect { get; private set; }
		public bool Enabled { get; internal set; }
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

		public DeviceType ActiveDevice = DeviceType.None;
		
		private PlayerInput _playerInput;
		private bool _playerInputPaired;

		/// <summary>
		/// Is this device in need of a usb pair to go along with the connected bt dualsense
		/// </summary>
		/// <returns></returns>
		public bool NeedsPair
        {
			get { return (ActiveDevice == DeviceType.DualSenseBT) && !USBAttached; }
        }

		/// <summary>
		/// True if the BT DualSense reports an active USB counterpart
		/// </summary>
		public bool DontOpenWirelessConnection
		{
			get { return BTAttached && Devices.DualsenseBT.usbConnected.isPressed;  }
		} //Do I Even Need this?

		public UniSenseUser()
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

		/// <summary>
		/// Use To initlize a unisense user. Do this before adding a device
		/// </summary>
		/// <returns></returns>
		public int Initialize()
        {
			if(UniSenseID != -1)
            {
				Debug.LogWarning("User Already Initialized");
				return UniSenseID;
            }
			if (!_availableUnisenseId.TryDequeue(out int id))
			{
				throw new Exception("No UniSense ID's available"); //TODO: Could handle more gracefully, just stop the process of connecting the device that causes this error
			}
			UniSenseID = id;
			return id;
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
		/// Adds a device to this UnisenseUser
		/// </summary>
		/// <param name="device"></param>
		/// <param name="deviceType"></param>
		/// <param name="unisenseID"></param>
		/// <returns>True if successful </returns>
		internal bool AddDevice(InputDevice device, DeviceType deviceType)
		{
			if(this.UniSenseID == -1)
            {
				Initialize();
				Debug.LogWarning("User Not Initialized!! Always Initialize First");
            }

			switch (deviceType)
			{
				case DeviceType.DualSenseBT:
					if (BTAttached) return false;
					BTAttached = true;
					this.Devices.DualsenseBT = device as DualSenseBTGamepadHID;
					this.SerialNumber = device.description.serial;
					UserIndexFinder.AddValue(this.SerialNumber, UniSenseID);
					break;
				case DeviceType.DualSenseUSB:
					if (USBAttached) return false;
					USBAttached = true;
					this.Devices.DualsenseUSB = device as DualSenseUSBGamepadHID;
					UserIndexFinder.AddValue(device.deviceId.ToString(), UniSenseID);
					break;
				case DeviceType.GenericGamepad:
					if (GenericAttached) return false;
					GenericAttached = true;
					this.Devices.GenericGamepad = device as Gamepad;
					UserIndexFinder.AddValue(device.deviceId.ToString(), UniSenseID);
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
				CloseConnection(ActiveDevice, true);
				//switch (ActiveDevice)
				//{
				//	case DeviceType.DualSenseBT:
				//		CloseConnection(DeviceType.DualSenseBT, true);
				//		break;
				//	case DeviceType.DualSenseUSB:
				//		CloseConnection(DeviceType.DualSenseUSB, true);
				//		break;
				//	case DeviceType.GenericGamepad:
				//		CloseConnection(DeviceType.GenericGamepad, true);
				//		break;
				//}
			}
			
			ConnectionOpen = OpenConnection(deviceType);
			ActiveDevice = deviceType;
			if (!ConnectionOpen)
			{
				ActiveDevice = DeviceType.None;
				return false;
			}
			
			switch (deviceType)
			{
				case DeviceType.DualSenseBT:
					Devices.ActiveDevice = Devices.DualsenseBT;
					break;
				case DeviceType.DualSenseUSB:
					Devices.ActiveDevice = Devices.DualsenseUSB;
					break;
				case DeviceType.GenericGamepad:
					Devices.ActiveDevice = Devices.GenericGamepad;
					break;
			}

			return _playerInput.SwitchCurrentControlScheme(new InputDevice[] { Devices.ActiveDevice });

			//switch (deviceType)
			//{
			//	case DeviceType.DualSenseBT:
			//		ConnectionOpen = OpenConnection(DeviceType.DualSenseBT);
			//		if (!ConnectionOpen)
			//		{
			//			ActiveDevice = DeviceType.None;
			//			return false;
			//		}
			//		ActiveDevice = DeviceType.DualSenseBT;
			//		this.Devices.ActiveDevice = this.Devices.DualsenseBT;
			//		if (!_playerInput.SwitchCurrentControlScheme(new InputDevice[] { Devices.ActiveDevice })) return false;
			//		break;
			//	case DeviceType.DualSenseUSB:
			//		ConnectionOpen = OpenConnection(DeviceType.DualSenseUSB);
			//		if (!ConnectionOpen)
			//		{
			//			ActiveDevice = DeviceType.None;
			//			return false;
			//		}
			//		ActiveDevice = DeviceType.DualSenseUSB;
			//		this.Devices.ActiveDevice = this.Devices.DualsenseUSB;
			//		Debug.Log(this.UniSenseID);
			//		if (!_playerInput.SwitchCurrentControlScheme(new InputDevice[] { Devices.ActiveDevice })) return false;
			//		break;
			//	case DeviceType.GenericGamepad:
			//		ConnectionOpen = OpenConnection(DeviceType.GenericGamepad);
			//		if (!ConnectionOpen)
			//		{
			//			ActiveDevice = DeviceType.None;
			//			return false;
			//		}
			//		ActiveDevice = DeviceType.GenericGamepad;
			//		this.Devices.ActiveDevice = this.Devices.GenericGamepad;
			//		if (!_playerInput.SwitchCurrentControlScheme(new InputDevice[] { Devices.ActiveDevice })) return false;
			//		break;
			//	case DeviceType.None:
			//		ActiveDevice = DeviceType.None;
			//		break;
			//	default:
			//		break;
			//}
			//return true;
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
				DS5W_ReturnValue status = DS5W.findDevice(ref Devices.enumInfoBT, SerialNumber);
				
				if (status != DS5W_ReturnValue.OK)
				{
					Debug.Log(status.ToString());
					return false;
				}
				status = DS5W.initDeviceContext(ref Devices.enumInfoBT, ref Devices.contextBT);

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
						DS5W.freeDeviceContext(ref Devices.contextBT, clearOutput);
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
					if (!UserIndexFinder.RemoveByKey(Devices.DualsenseBT.description.serial))
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
					if (!UserIndexFinder.RemoveByKey(Devices.DualsenseUSB.deviceId.ToString()))
					{
						Debug.LogError("Failed to remove key");
						return false;
					}
					CloseConnection(deviceType, false);
					USBAttached = false;
					break;
				case DeviceType.GenericGamepad:
					if (!GenericAttached) return false;
					if (!UserIndexFinder.RemoveByKey(Devices.GenericGamepad.deviceId.ToString()))
					{
						Debug.LogError("Failed to remove key");
						return false;
					}
					CloseConnection(deviceType, false);
					GenericAttached = false;
					break;
			}

			ReadyToConnect = IsSomthingAttached;
			return true;
		}
		/// <summary>
		/// Closes all connections to active devices and resets this user to default;
		/// </summary>
		public void ClearUser(bool resetHaptics)
		{
			if (_playerInputPaired) _playerInput.user.UnpairDevices();
			CloseConnection(ActiveDevice, resetHaptics);
			UserIndexFinder.RemoveByValue(UniSenseID);
			_availableUnisenseId.Enqueue(UniSenseID);
			SetDefualts();
		}
	}
}
