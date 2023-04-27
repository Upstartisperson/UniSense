using UniSense;
using UnityEngine;
using UnityEngine.InputSystem;
using UniSense.LowLevel;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.LowLevel;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using DS5W;

public class DualSenseManager : MonoBehaviour
{
	#region Variables
	private Dictionary<string, int> _controllerLookup = new Dictionary<string, int>();
	private DeviceEnumInfo[] infos;
	uint devicecount = 0;
	OS_Type oS;
	private DualSenseController[] _dualSenseControllers = new DualSenseController[16];
	/// <summary>
	/// how many controllers are stored in the controller array
	/// </summary>
	private int _controllerCount = 0;
	#endregion

	#region Custom Data Structures
	internal class DualSenseInfo
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
	internal enum DualSenseConnectionStatus
	{
		BT,
		USB,
		Disconected
	}

	internal enum DualSenseConnectionType
	{
		BT,
		USB
	}

	internal class DualSenseController
	{

		public DualSenseBTHIDOutputReport outputReport = DualSenseBTHIDOutputReport.Create();
		public string serialNumber = null;
		public DualSenseInfo devices = new DualSenseInfo();
		public bool BTConnected = false;
		public bool USBConnected = false;


		public class InternalLogic
		{
			public DualSenseConnectionStatus connectionStatus = DualSenseConnectionStatus.Disconected;
		}

		public InternalLogic _internalLogic;
		public void AddController(Gamepad gamepad, DualSenseConnectionType connectionType)
		{
			switch (connectionType)
			{
				case DualSenseConnectionType.BT:
					this.BTConnected = true;
					this.serialNumber = gamepad.description.serial.ToString();
					this.devices.DualsenseBT = gamepad as DualSenseBTGamepadHID;
					break;
				case DualSenseConnectionType.USB:
					this.USBConnected = true;
					this.serialNumber = gamepad.description.serial.ToString();
					this.devices.DualsenseUSB = gamepad as DualSenseUSBGamepadHID;
					break;
				default: Debug.LogError("No connection type defined"); break;
			}
		}
		

	}
	#endregion

	private void OnEnable() => InputSystem.onDeviceChange += OnDeviceChange;

	private void Start()

	{
		
	
		for(int i = 0; i < _dualSenseControllers.Length; i++)
        {
			_dualSenseControllers[i] = new DualSenseController();
        }
		oS = (IntPtr.Size == 4) ? OS_Type._x86 : OS_Type._x64;
		DualSenseInfo dualSenseInfo = new DualSenseInfo();
		Gamepad[] _USBGamepads = DualSenseUSBGamepadHID.FindAll();
		Gamepad[] _BTGamepads = DualSenseBTGamepadHID.FindAll();
		DS5WenumDevices(ref infos, 16);

		if (_BTGamepads != null && _BTGamepads.Length > 0)
		{
			foreach (Gamepad gamepad in _BTGamepads)
			{
				string _serialNumber = gamepad.description.serial.ToString();

				if (_controllerLookup.ContainsKey(_serialNumber))
				{
					_dualSenseControllers[_controllerLookup[_serialNumber]].AddController(gamepad, DualSenseConnectionType.BT);
				}
				else
				{
					_controllerLookup.Add(_serialNumber, _controllerCount);
					_dualSenseControllers[_controllerCount].AddController(gamepad, DualSenseConnectionType.BT);
					_controllerCount++;
				}

			}
		}

		if (_USBGamepads !=null && _USBGamepads.Length > 0)
		{
			foreach (Gamepad gamepad in _USBGamepads)
			{
				string _serialNumber = gamepad.description.serial.ToString();

				if (_controllerLookup.ContainsKey(_serialNumber))
				{
					_dualSenseControllers[_controllerLookup[_serialNumber]].AddController(gamepad, DualSenseConnectionType.USB);
				}
				else
				{
					_controllerLookup.Add(_serialNumber, _controllerCount);
					_dualSenseControllers[_controllerCount].AddController(gamepad, DualSenseConnectionType.USB);
					_controllerCount++;
				}

			}
		}
		DS5WProssesDeviceInfo(infos);
	}

	private void OnDeviceChange(InputDevice device, InputDeviceChange change)
	{


		bool isUSB = device is DualSenseUSBGamepadHID;
		bool isBT = device is DualSenseBTGamepadHID;
		string _serialNumber = device.description.serial.ToString();
		if (!isBT && !isUSB) return; //it is not a DualSense Controller so return

		switch (change)
		{
			case InputDeviceChange.Added:


				if (isBT)
				{
					if (_controllerLookup.ContainsKey(_serialNumber))
					{
						_dualSenseControllers[_controllerLookup[_serialNumber]].BTConnected = true;
						_dualSenseControllers[_controllerLookup[_serialNumber]].devices.DualsenseBT = device as DualSenseBTGamepadHID;
						SetupControllerConnection(ref _dualSenseControllers[_controllerLookup[_serialNumber]]);
						return;
					}
					else
					{
						_controllerLookup.Add(_serialNumber, _controllerCount);
						_dualSenseControllers[_controllerCount].BTConnected = true;
						_dualSenseControllers[_controllerCount].devices.DualsenseBT = device as DualSenseBTGamepadHID;
						DS5WenumDevices(ref infos, 16);
						DS5WProssesDeviceInfo(infos);
						SetupControllerConnection(ref _dualSenseControllers[_controllerCount]);
						_controllerCount++;
						return;
					}
				}

				if (isUSB)
				{
					if (_controllerLookup.ContainsKey(_serialNumber))
					{
						_dualSenseControllers[_controllerLookup[_serialNumber]].USBConnected = true;
						_dualSenseControllers[_controllerLookup[_serialNumber]].devices.DualsenseUSB = device as DualSenseUSBGamepadHID;
						SetupControllerConnection(ref _dualSenseControllers[_controllerLookup[_serialNumber]]);
						return;
					}
					else
					{
						_controllerLookup.Add(_serialNumber, _controllerCount);
						_dualSenseControllers[_controllerCount].USBConnected = true;
						_dualSenseControllers[_controllerCount].devices.DualsenseUSB = device as DualSenseUSBGamepadHID;
						DS5WenumDevices(ref infos, 16);
						DS5WProssesDeviceInfo(infos);
						SetupControllerConnection(ref _dualSenseControllers[_controllerCount]);
						_controllerCount++;
						return;
					}
				}
				break;

			case InputDeviceChange.Reconnected:
				if (isBT)
				{
					if (_controllerLookup.ContainsKey(_serialNumber))
					{
						_dualSenseControllers[_controllerLookup[_serialNumber]].BTConnected = true;
						_dualSenseControllers[_controllerLookup[_serialNumber]].devices.DualsenseBT = device as DualSenseBTGamepadHID;
						SetupControllerConnection(ref _dualSenseControllers[_controllerLookup[_serialNumber]]);
						return;
					}
					else
					{
						_controllerLookup.Add(_serialNumber, _controllerCount);
						_dualSenseControllers[_controllerCount].BTConnected = true;
						_dualSenseControllers[_controllerCount].devices.DualsenseBT = device as DualSenseBTGamepadHID;
						DS5WenumDevices(ref infos, 16);
						DS5WProssesDeviceInfo(infos);
						SetupControllerConnection(ref _dualSenseControllers[_controllerCount]);
						_controllerCount++;
						return;
					}
				}

				if (isUSB)
				{
					if (_controllerLookup.ContainsKey(_serialNumber))
					{
						_dualSenseControllers[_controllerLookup[_serialNumber]].USBConnected = true;
						_dualSenseControllers[_controllerLookup[_serialNumber]].devices.DualsenseUSB = device as DualSenseUSBGamepadHID;
						SetupControllerConnection(ref _dualSenseControllers[_controllerLookup[_serialNumber]]);
						return;
					}
					else
					{
						_controllerLookup.Add(_serialNumber, _controllerCount);
						_dualSenseControllers[_controllerCount].USBConnected = true;
						_dualSenseControllers[_controllerCount].devices.DualsenseUSB = device as DualSenseUSBGamepadHID;
						DS5WenumDevices(ref infos, 16);
						DS5WProssesDeviceInfo(infos);
						SetupControllerConnection(ref _dualSenseControllers[_controllerCount]);
						_controllerCount++;
						return;
					}
				}
				break;

			case InputDeviceChange.Removed:
				if (isBT)
				{
					if (_controllerLookup.ContainsKey(_serialNumber))
					{
						_dualSenseControllers[_controllerLookup[_serialNumber]].BTConnected = false;
						SetupControllerConnection(ref _dualSenseControllers[_controllerLookup[_serialNumber]]);
						return;
					}
					else Debug.LogError("Removed DualSense Device that was never previously recorded");

				}

				if (isUSB)
				{
					if (_controllerLookup.ContainsKey(_serialNumber))
					{
						_dualSenseControllers[_controllerLookup[_serialNumber]].USBConnected = false;
						SetupControllerConnection(ref _dualSenseControllers[_controllerLookup[_serialNumber]]);
						return;
					}
					else Debug.LogError("Removed DualSense Device that was never previously recorded");
				}
				break;


			case InputDeviceChange.Disconnected:
				if (isBT)
				{
					if (_controllerLookup.ContainsKey(_serialNumber))
					{
						_dualSenseControllers[_controllerLookup[_serialNumber]].BTConnected = false;
						SetupControllerConnection(ref _dualSenseControllers[_controllerLookup[_serialNumber]]);
						return;
					}
					else Debug.LogError("Removed DualSense Device that was never previously recorded");

				}

				if (isUSB)
				{
					if (_controllerLookup.ContainsKey(_serialNumber))
					{
						_dualSenseControllers[_controllerLookup[_serialNumber]].USBConnected = false;
						SetupControllerConnection(ref _dualSenseControllers[_controllerLookup[_serialNumber]]);
						return;
					}
					else Debug.LogError("Removed DualSense Device that was never previously recorded");
				}
				break;
		}

	}

	private void OnDisable()
	{
		InputSystem.onDeviceChange -= OnDeviceChange;
		CloseAllConnections(ref _dualSenseControllers);
	}

	#region Helper Classes
	/// <summary>
	/// <para>Use this method to set the correct state of the DualSenseController _internalLogic </para>
	/// This method will manage the connections to your PS5 "Dualsense" controllers
	/// </summary>
	/// <param name="controller"></param>
	private void SetupControllerConnection(ref DualSenseController controller)
	{
		//Assume that controller._internalLogic.BTConnected & controller._internalLogic.USBConnected are both up to date
		//Assume that controller._internalLogic.connectionStatus has not been updated
		switch (controller._internalLogic.connectionStatus)
		{
			case DualSenseConnectionStatus.BT:

				if (controller.USBConnected) //If USB is connected, close BT connection and open USB connection
				{
					CloseBTConnection(ref controller);
					OpenUSBConnection(ref controller);
					controller._internalLogic.connectionStatus = DualSenseConnectionStatus.USB;
					return;
				}

				if (controller.BTConnected) return; //Nothing needs to change


				CloseBTConnection(ref controller); //No devices are connected so close connections and update connection status
				controller._internalLogic.connectionStatus = DualSenseConnectionStatus.Disconected;
				break;

			case DualSenseConnectionStatus.USB:
				if (controller.USBConnected) return; //Nothing needs to change

				if (controller.BTConnected) //if BT is connected close defunct USB connection and open BT connection
				{
					CloseUSBConnection(ref controller);
					OpenBTConnection(ref controller);
					controller._internalLogic.connectionStatus = DualSenseConnectionStatus.BT;
					return;
				}


				//No devices are connected so close connections and update connection status
				CloseUSBConnection(ref controller);
				controller._internalLogic.connectionStatus = DualSenseConnectionStatus.Disconected;
				break;

			case DualSenseConnectionStatus.Disconected:
				if (controller.USBConnected) //Connect to USB device
				{
					OpenUSBConnection(ref controller);
					controller._internalLogic.connectionStatus = DualSenseConnectionStatus.USB;
					return;
				}
				if (controller.BTConnected) //Connect to BT device
				{
					OpenBTConnection(ref controller);
					controller._internalLogic.connectionStatus = DualSenseConnectionStatus.BT;
					return;
				}
				break;
		}
	}

	private void OpenBTConnection(ref DualSenseController controller)
	{
		DS5W_RetrunValue status = (oS == OS_Type._x64) ? DS5W_x64.initDeviceContext(ref controller.devices.enumInfoBT, ref controller.devices.contextBT) :
														 DS5W_x86.initDeviceContext(ref controller.devices.enumInfoBT, ref controller.devices.contextBT);
		if (status != DS5W_RetrunValue.OK) Debug.LogError(status.ToString());
		if (!controller.devices.DualsenseBT.enabled) InputSystem.EnableDevice(controller.devices.DualsenseBT);
		return;
	}

	private void CloseBTConnection(ref DualSenseController controller)
	{
		if (controller.devices.DualsenseBT.enabled) InputSystem.DisableDevice(controller.devices.DualsenseBT);
		if (oS == OS_Type._x64)
		{
			DS5W_x64.freeDeviceContext(ref controller.devices.contextBT);
		}
		else
		{
			DS5W_x86.freeDeviceContext(ref controller.devices.contextBT);
		}
	}

	private void OpenUSBConnection(ref DualSenseController controller)
	{
		DS5W_RetrunValue status = (oS == OS_Type._x64) ? DS5W_x64.initDeviceContext(ref controller.devices.enumInfoUSB, ref controller.devices.contextUSB) :
														 DS5W_x86.initDeviceContext(ref controller.devices.enumInfoUSB, ref controller.devices.contextUSB);
		if (status != DS5W_RetrunValue.OK) Debug.LogError(status.ToString());
		if (!controller.devices.DualsenseUSB.enabled) InputSystem.EnableDevice(controller.devices.DualsenseUSB);
		return;
	}

	private void CloseUSBConnection(ref DualSenseController controller)
	{
		if (controller.devices.DualsenseUSB.enabled) InputSystem.DisableDevice(controller.devices.DualsenseUSB);
		if (oS == OS_Type._x64)
		{
			DS5W_x64.freeDeviceContext(ref controller.devices.contextUSB);
		}
		else
		{
			DS5W_x86.freeDeviceContext(ref controller.devices.contextUSB);
		}
	}

	private void DS5WenumDevices(ref DeviceEnumInfo[] infos, int Arraysize)
	{
		IntPtr ptrDeviceEnum = IntPtr.Zero;
		infos = new DeviceEnumInfo[Arraysize];
		DS5WHelpers.BuildEnumDeviceBuffer(ref ptrDeviceEnum, infos);
		DS5W_RetrunValue status = (oS == OS_Type._x64) ? DS5W_x64.enumDevices(ref ptrDeviceEnum, (uint)Arraysize, ref devicecount, false) :
														 DS5W_x86.enumDevices(ref ptrDeviceEnum, (uint)Arraysize, ref devicecount, false);
		if (status != DS5W_RetrunValue.OK) Debug.LogError(status.ToString());
		DS5WHelpers.DeconstructEnumDeviceBuffer(ref ptrDeviceEnum, ref infos);
	}

	private void CloseAllConnections(ref DualSenseController[] controllers)
	{
		for (int i = 0; i < controllers.Length; i++)
		{
			switch (controllers[i]._internalLogic.connectionStatus)
			{
				case DualSenseConnectionStatus.BT:
					CloseBTConnection(ref controllers[i]);
					break;
				case DualSenseConnectionStatus.USB:
					CloseUSBConnection(ref controllers[i]);
					break;
				default: break;
			}
		}
	}

	/// <summary>
	/// Adds Information from the DeviceEnumInfo array to the DualSenseController array
	/// </summary>
	/// <param name="DeviceEnum"></param>
	public void DS5WProssesDeviceInfo(DeviceEnumInfo[] DeviceEnum)
	{
		foreach (DeviceEnumInfo info in DeviceEnum)
		{
			if (info._internal.path == null || info._internal.path == "") continue;

			string _serialNumber = info._internal.serialNumber;
			switch (info._internal.Connection)
			{
				case DeviceConnection.USB:

					if (_controllerLookup.ContainsKey(_serialNumber))
					{

						_dualSenseControllers[_controllerLookup[_serialNumber]].devices.enumInfoUSB = info;
					}
					else
					{
						_controllerLookup.Add(_serialNumber, _controllerCount);
						_dualSenseControllers[_controllerCount].devices.enumInfoUSB = info;
						_controllerCount++;
					}

					break;

				case DeviceConnection.BT:

					if (_controllerLookup.ContainsKey(_serialNumber))
					{

						_dualSenseControllers[_controllerLookup[_serialNumber]].devices.enumInfoBT = info;
					}
					else
					{
						_controllerLookup.Add(_serialNumber, _controllerCount);
						_dualSenseControllers[_controllerCount].devices.enumInfoBT = info;
						_controllerCount++;
					}

					break;
			}
		}
	}
	#endregion
}