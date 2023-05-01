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
using UniSense.Connections;

namespace UniSense
{
	public interface IDualSenseManager
	{



	}
	//Confirmed no memory leaks
	public class DualSense : MonoBehaviour, IDualSenseManager
	{

		/// <summary>
		/// Use this class to form readonly bool
		/// </summary>
		public class IsManaged
		{
			public readonly bool value;
			public IsManaged(bool value)
			{
				this.value = value;
			}

		}


		#region Variables
		/// <summary>
		/// DONT RELY ON THIS VALUE
		/// </summary>
		[SerializeField]
		private bool isBeingManaged = false;

		private IsManaged _isManaged;
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

		public class DualSenseController
		{


			public string serialNumber = null;
			public DualSenseInfo devices = new DualSenseInfo();
			public bool BTConnected = false;
			public bool USBConnected = false;
			public DualSenseConnectionStatus connectionStatus = DualSenseConnectionStatus.Disconected;

			

			
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
		private void Awake()
		{
			_isManaged = new IsManaged(isBeingManaged);
		}
		private void OnEnable()
		{
			if (_isManaged.value == false) InputSystem.onDeviceChange += OnDeviceChange;
		}

		private void Start()
		{
			PlayerInput playerInput = GetComponent<PlayerInput>();
			PlayerInputManager playerInputManager = GetComponent<PlayerInputManager>();
			 

			//var somthing = InputSystem.devices;

			//Debug.Log("hu");
			//Initialize the _dualSenseControllers array with blank Dualsensecontroller
			for (int i = 0; i < _dualSenseControllers.Length; i++)
			{
				_dualSenseControllers[i] = new DualSenseController();
			}
			oS = (IntPtr.Size == 4) ? OS_Type._x86 : OS_Type._x64;
			if (_isManaged.value == true) return; //If DualSense is being controlled by DualSenseManager return;
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

			if (_USBGamepads != null && _USBGamepads.Length > 0)
			{
				foreach (Gamepad gamepad in _USBGamepads)
				{
					string _deciceID = gamepad.deviceId.ToString(); //device id stand in for serial number

					if (_controllerLookup.ContainsKey(_deciceID))
					{
						_dualSenseControllers[_controllerLookup[_deciceID]].AddController(gamepad, DualSenseConnectionType.USB);
					}
					else
					{
						_controllerLookup.Add(_deciceID, _controllerCount);
						_dualSenseControllers[_controllerCount].AddController(gamepad, DualSenseConnectionType.USB);
						_controllerCount++;
					}

				}
			}
			DS5WProssesDeviceInfo(infos);
			senseController = _dualSenseControllers[0];
			SetupControllerConnection(ref senseController);
		}

		private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {


            bool isUSB = device is DualSenseUSBGamepadHID;
			

			bool isBT = device is DualSenseBTGamepadHID;

			if (!isBT && !isUSB) return; //it is not a DualSense Controller so return

			switch (change)
			{
				case InputDeviceChange.Added:


					if (isBT)
					{
						string _serialNumber = device.description.serial.ToString();
						if (_controllerLookup.ContainsKey(_serialNumber))
						{
							int controllerindex = _controllerLookup[_serialNumber];
							_dualSenseControllers[controllerindex].BTConnected = true;
							_dualSenseControllers[controllerindex].devices.DualsenseBT = device as DualSenseBTGamepadHID;
							SetupControllerConnection(ref _dualSenseControllers[controllerindex]);
							SetCurrentGamepad(_dualSenseControllers[controllerindex]);
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
							SetCurrentGamepad((_dualSenseControllers[_controllerCount]));
							_controllerCount++;
							return;
						}
					}

					if (isUSB)
					{
						string _deviceID = device.deviceId.ToString();
						if (_controllerLookup.ContainsKey(_deviceID))
						{
							int controllerindex = _controllerLookup[_deviceID];
							_dualSenseControllers[controllerindex].USBConnected = true;
							_dualSenseControllers[controllerindex].devices.DualsenseUSB = device as DualSenseUSBGamepadHID;
							SetupControllerConnection(ref _dualSenseControllers[controllerindex]);
							SetCurrentGamepad((_dualSenseControllers[controllerindex]));
							return;
						}
						else
						{
							_controllerLookup.Add(_deviceID, _controllerCount);
							_dualSenseControllers[_controllerCount].USBConnected = true;
							_dualSenseControllers[_controllerCount].devices.DualsenseUSB = device as DualSenseUSBGamepadHID;
							SetupControllerConnection(ref _dualSenseControllers[_controllerCount]);
							SetCurrentGamepad(_dualSenseControllers[_controllerCount]);
							_controllerCount++;
							return;
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
							_dualSenseControllers[controllerindex].BTConnected = true;
							_dualSenseControllers[controllerindex].devices.DualsenseBT = device as DualSenseBTGamepadHID;
							SetupControllerConnection(ref _dualSenseControllers[controllerindex]);
							SetCurrentGamepad(_dualSenseControllers[controllerindex]);
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
							SetCurrentGamepad((_dualSenseControllers[_controllerCount]));
							_controllerCount++;
							return;
						}
					}

					if (isUSB)
					{
						string _deviceID = device.deviceId.ToString();
						if (_controllerLookup.ContainsKey(_deviceID))
						{
							int controllerindex = _controllerLookup[_deviceID];
							_dualSenseControllers[controllerindex].USBConnected = true;
							_dualSenseControllers[controllerindex].devices.DualsenseUSB = device as DualSenseUSBGamepadHID;
							SetupControllerConnection(ref _dualSenseControllers[controllerindex]);
							SetCurrentGamepad((_dualSenseControllers[controllerindex]));
							return;
						}
						else
						{
							_controllerLookup.Add(_deviceID, _controllerCount);
							_dualSenseControllers[_controllerCount].USBConnected = true;
							_dualSenseControllers[_controllerCount].devices.DualsenseUSB = device as DualSenseUSBGamepadHID;
							SetupControllerConnection(ref _dualSenseControllers[_controllerCount]);
							SetCurrentGamepad(_dualSenseControllers[_controllerCount]);
							_controllerCount++;
							return;
						}
					}
					break;

				case InputDeviceChange.Removed:
					if (isBT)
					{
						string _serialNumber = device.description.serial.ToString();
						if (_controllerLookup.ContainsKey(_serialNumber))
						{
							_dualSenseControllers[_controllerLookup[_serialNumber]].BTConnected = false;
							SetupControllerConnection(ref _dualSenseControllers[_controllerLookup[_serialNumber]]);
							SetCurrentGamepad(GetCurrentGamepad());
							return;
						}
						else Debug.LogError("Removed DualSense Device that was never previously recorded");

					}

					if (isUSB)
					{
						string _deviceID = device.deviceId.ToString();
						if (_controllerLookup.ContainsKey(_deviceID))
						{
							_dualSenseControllers[_controllerLookup[_deviceID]].USBConnected = false;
							SetupControllerConnection(ref _dualSenseControllers[_controllerLookup[_deviceID]]);
							SetCurrentGamepad(GetCurrentGamepad());
							return;
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
							_dualSenseControllers[_controllerLookup[_serialNumber]].BTConnected = false;
							SetupControllerConnection(ref _dualSenseControllers[_controllerLookup[_serialNumber]]);
							SetCurrentGamepad(GetCurrentGamepad());
							return;
						}
						else Debug.LogError("Removed DualSense Device that was never previously recorded");

					}

					if (isUSB)
					{
						string _deviceID = device.deviceId.ToString();
						if (_controllerLookup.ContainsKey(_deviceID))
						{
							_dualSenseControllers[_controllerLookup[_deviceID]].USBConnected = false;
							SetupControllerConnection(ref _dualSenseControllers[_controllerLookup[_deviceID]]);
							SetCurrentGamepad(GetCurrentGamepad());
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
			//if (controller.devices.DualsenseBT.enabled) InputSystem.DisableDevice(controller.devices.DualsenseBT); //Might delete this devices might only need to be enabled
			if (oS == OS_Type._x64)
			{
				DS5W_x64.freeDeviceContext(ref controller.devices.contextBT);
			}
			else
			{
				DS5W_x86.freeDeviceContext(ref controller.devices.contextBT);
			}
		}
		

		private byte[] GetRawCommand(DualSenseHIDOutputReport command)
		{
			byte[] inbuffer = command.RetriveCommand();
			byte[] outbuffer = new byte[47];
			Array.Copy(inbuffer, 9, outbuffer, 0, 47);
			return outbuffer;
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
				switch (controllers[i].connectionStatus)
				{
					case DualSenseConnectionStatus.BT:
						CloseBTConnection(ref controllers[i]);
						break;
					default: break;
				}
			}
		}

		/// <summary>
		/// Adds Information from the DeviceEnumInfo array to the DualSenseController array
		/// </summary>
		/// <param name="DeviceEnum"></param>
		private void DS5WProssesDeviceInfo(DeviceEnumInfo[] DeviceEnum)
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

							_dualSenseControllers[_controllerLookup[_serialNumber]].devices.enumInfoBT = info;
						}
						else
						{
							_controllerLookup.Add(_serialNumber, _controllerCount);
							_dualSenseControllers[_controllerCount].devices.enumInfoBT = info;
							_controllerCount++;
						}

						break;
					default: break;
				}
			}
		}
		#endregion
		public bool memtest = false;
        private void Update()
        {
			if (!memtest) return;
			for(int i = 0; i < 100; i++)
            {
				DS5WenumDevices(ref infos, 16);
			}
			
        }


        //This region of the script functions as an easy way of communicating with the DualSense controller's haptic and LED systems
        #region Commands
        private DualSenseHIDOutputReport CurrentCommand = DualSenseHIDOutputReport.Create();

		private DualSenseController senseController = new DualSenseController();

		private bool controllerConnected => senseController.connectionStatus != DualSenseConnectionStatus.Disconected;



		public void ResetHaptics()
		{
			//TODO : Implement a check to see if necessary
			CurrentCommand = DualSenseHIDOutputReport.Create();
			CurrentCommand.ResetMotorSpeeds();
			CurrentCommand.SetLeftTriggerState(new DualSenseTriggerState());
			CurrentCommand.SetRightTriggerState(new DualSenseTriggerState());
			SendCommand();
		}
		public void SetLightBarColor(Color color) => CurrentCommand.SetLightBarColor(color);
		public void SetMotorSpeeds(float lowFrequency, float highFrequency) => CurrentCommand.SetMotorSpeeds(lowFrequency, highFrequency);
		public void ResetMotorSpeeds() => SetMotorSpeeds(0f, 0f);

		public void ResetLightBarColor() => SetLightBarColor(Color.black);

		public void ResetLEDs() => CurrentCommand.DisableLightBarAndPlayerLed();

		public void SetPlayerLED(PlayerLedBrightness brightness, PlayerLED playerLED, bool fade = false) => CurrentCommand.SetPlayerLedState(new PlayerLedState((byte)playerLED, fade), brightness);



		public void Reset()
		{
			if (controllerConnected)
			{
				Debug.LogError("No Controller Connected");
				return;
			}
			ResetHaptics();
			ResetMotorSpeeds();
			ResetLEDs();
			ResetTriggersState();
			SendCommand();

		}
		public void ResetTriggersState()
		{
			var EmptyState = new DualSenseTriggerState
			{
				EffectType = DualSenseTriggerEffectType.NoResistance,
				EffectEx = new DualSenseEffectExProperties(),
				Section = new DualSenseSectionResistanceProperties(),
				Continuous = new DualSenseContinuousResistanceProperties()
			};

			CurrentCommand.SetRightTriggerState(EmptyState);
			CurrentCommand.SetLeftTriggerState(EmptyState);


		}

		public void ResetCommand()
		{
			CurrentCommand = DualSenseHIDOutputReport.Create();
		}


		/// <summary>
		/// This method returns the controller that this component controls
		/// </summary>
		/// <param name="dualSense"></param>
		/// <returns></returns>
		public DualSenseController GetCurrentGamepad()
		{
			
			if (senseController == null || senseController.connectionStatus == DualSenseConnectionStatus.Disconected)
			{
				for (int i = 0; i < _dualSenseControllers.Length; i++)
				{
					if (_dualSenseControllers[i].connectionStatus != DualSenseConnectionStatus.Disconected)
					{
						return _dualSenseControllers[i];
					}
				}
				Debug.Log("No controllers are connected");
				return null;
			}
			return senseController;
		}


		public void SetCurrentGamepad(DualSenseController gamepad)
        {
			if (senseController == null)
            {
				senseController = gamepad;
				return;
            }
            switch (senseController.connectionStatus)
            {
				case DualSenseConnectionStatus.Disconected: break;
				case DualSenseConnectionStatus.BT:
					byte[] rawDeviceCommand = GetRawCommand(DualSenseHIDOutputReport.Create());
					DS5W_RetrunValue status = (oS == OS_Type._x64) ? DS5W_x64.setDeviceRawOutputState(ref senseController.devices.contextBT, rawDeviceCommand, rawDeviceCommand.Length) :
																	 DS5W_x86.setDeviceRawOutputState(ref senseController.devices.contextBT, rawDeviceCommand, rawDeviceCommand.Length);
					if (status != DS5W_RetrunValue.OK) Debug.LogError(status.ToString());
					break;
				case DualSenseConnectionStatus.USB:
					DualSenseHIDOutputReport report = DualSenseHIDOutputReport.Create();
					senseController.devices.DualsenseUSB?.ExecuteCommand(ref report);
					break;
				default: break;
            }
			senseController = gamepad;
        }


		public void SendCommand() //intended to be called by the local component attached to the player
		{
			if (_isManaged.value == true)
			{
				//TODO : Add a function call that uses a player ID to send a command to a DualSenseManager component
				return;
			}

			senseController = GetCurrentGamepad();
			if (senseController == null) return;
			switch (senseController.connectionStatus)
			{
				case DualSenseConnectionStatus.BT:
					
					byte[] rawDeviceCommand = GetRawCommand(CurrentCommand);
					DS5W_RetrunValue status = (oS == OS_Type._x64) ? DS5W_x64.setDeviceRawOutputState(ref senseController.devices.contextBT, rawDeviceCommand, rawDeviceCommand.Length) :
																	 DS5W_x86.setDeviceRawOutputState(ref senseController.devices.contextBT, rawDeviceCommand, rawDeviceCommand.Length);
					if (status != DS5W_RetrunValue.OK) Debug.LogError(status.ToString());
					break;

				case DualSenseConnectionStatus.USB:
				
					senseController.devices.DualsenseUSB?.ExecuteCommand(ref CurrentCommand);
					break;
				case DualSenseConnectionStatus.Disconected:
					Debug.LogError("No controller connected");
					break;
				default:
					Debug.LogError("DualSenseConnectionStatusNUll");
					break;
			}
		}

		/// <summary>
		/// Use this method to set the continuous resistance state of either the left or the right trigger 
		/// </summary>
		/// <param name="trigger">From the UniSense namespace must be either DualSenseTrigger.Left, or DualSenseTrigger.Right</param>
		/// <param name="force">How well will the trigger resist pressure. (Range from 0 to 1)</param>
		/// <param name="position">How much pre-travel before resistance begins. (Range from 0 to 1)</param>
		
			public void SetTriggerContinuousResistance(DualSenseTrigger trigger, float force, float position)
			{
				
				switch (trigger)
				{
					case DualSenseTrigger.Left:
						var leftTriggerState = new DualSenseTriggerState
						{
							EffectType = DualSenseTriggerEffectType.ContinuousResistance,
							EffectEx = new DualSenseEffectExProperties(),
							Section = new DualSenseSectionResistanceProperties(),
							Continuous = new DualSenseContinuousResistanceProperties()
						};

						leftTriggerState.Continuous.Force = (byte)(force * 255);
						leftTriggerState.Continuous.StartPosition = (byte)(position * 255);
						CurrentCommand.SetLeftTriggerState(leftTriggerState);
						break;

					case DualSenseTrigger.Right:
						var rightTriggerState = new DualSenseTriggerState
						{
							EffectType = DualSenseTriggerEffectType.ContinuousResistance,
							EffectEx = new DualSenseEffectExProperties(),
							Section = new DualSenseSectionResistanceProperties(),
							Continuous = new DualSenseContinuousResistanceProperties()
						};
						rightTriggerState.Continuous.Force = (byte)(force * 255);
						rightTriggerState.Continuous.StartPosition = (byte)(position * 255);
						CurrentCommand.SetRightTriggerState(rightTriggerState);
						break;

					default: return;
				}
			}


			/// <summary>
			/// Use this method to set the section state of either the left or the right trigger
			/// </summary>
			/// <param name="trigger">From the UniSense namespace must be either DualSenseTrigger.Left, or DualSenseTrigger.Right</param>
			/// <param name="force">How well will the trigger resist pressure. (Range from 0 to 1)</param>
			/// <param name="startPosition">How much pre-travel before resistance begins. (Range from 0 to 1)</param>
			/// <param name="endPosition">Where will the trigger resistance end. (Range from 0 to 1)</param>
			public void SetTriggerSectionResistance(DualSenseTrigger trigger, float force, float startPosition, float endPosition)
			{
				switch (trigger)
				{
					case DualSenseTrigger.Left:
						var leftTriggerState = new DualSenseTriggerState
						{
							EffectType = DualSenseTriggerEffectType.SectionResistance,
							EffectEx = new DualSenseEffectExProperties(),
							Section = new DualSenseSectionResistanceProperties(),
							Continuous = new DualSenseContinuousResistanceProperties()
						};
						leftTriggerState.Section.Force = (byte)(force * 255);
						leftTriggerState.Section.StartPosition = (byte)(startPosition * 255);
						leftTriggerState.Section.EndPosition = (byte)(endPosition * 255);
						CurrentCommand.SetLeftTriggerState(leftTriggerState);
						break;

					case DualSenseTrigger.Right:
						var rightTriggerState = new DualSenseTriggerState
						{
							EffectType = DualSenseTriggerEffectType.SectionResistance,
							EffectEx = new DualSenseEffectExProperties(),
							Section = new DualSenseSectionResistanceProperties(),
							Continuous = new DualSenseContinuousResistanceProperties()
						};
						rightTriggerState.Section.Force = (byte)(force * 255);
						rightTriggerState.Section.StartPosition = (byte)(startPosition * 255);
						rightTriggerState.Section.EndPosition = (byte)(endPosition * 255);
						CurrentCommand.SetRightTriggerState(rightTriggerState);
						break;

					default: return;
				}
			}


			/// <summary>
			/// Use this method to set a trigger vibration effect of either the left or the right trigger
			/// </summary>
			/// <param name="trigger">From the UniSense namespace must be either DualSenseTrigger.Left, or DualSenseTrigger.Right</param>
			/// <param name="startPosision">How much pre-travel before the effect begins. (Range from 0 to 1)</param>
			/// <param name="beginForce">How pronounced will the effect start. (Range from 0 to 1)</param>
			/// <param name="middleForce">How pronounced will the effect be in the middle. (Range from 0 to 1)</param>
			/// <param name="endForce">How pronounced will the effect be at the end. (Range from 0 to 1)</param>
			/// <param name="frequency">What will the frequency of the effect be</param>
			/// <param name="keepEffect">Fill this one out because I have no clue</param>
			public void SetTriggerEffectEXResistance(DualSenseTrigger trigger, float startPosision, float beginForce, float middleForce, float endForce, float frequency, bool keepEffect)
			{
				switch (trigger)
				{
					case DualSenseTrigger.Left:
						var leftTriggerState = new DualSenseTriggerState
						{
							EffectType = DualSenseTriggerEffectType.EffectEx,
							EffectEx = new DualSenseEffectExProperties(),
							Section = new DualSenseSectionResistanceProperties(),
							Continuous = new DualSenseContinuousResistanceProperties()
						};
						leftTriggerState.EffectEx.StartPosition = (byte)(startPosision * 255);
						leftTriggerState.EffectEx.BeginForce = (byte)(beginForce * 255);
						leftTriggerState.EffectEx.MiddleForce = (byte)(middleForce * 255);
						leftTriggerState.EffectEx.EndForce = (byte)(endForce * 255);
						leftTriggerState.EffectEx.Frequency = (byte)(frequency * 255);
						leftTriggerState.EffectEx.KeepEffect = keepEffect;
						CurrentCommand.SetLeftTriggerState(leftTriggerState);
						break;

					case DualSenseTrigger.Right:
						var rightTriggerState = new DualSenseTriggerState
						{
							EffectType = DualSenseTriggerEffectType.EffectEx,
							EffectEx = new DualSenseEffectExProperties(),
							Section = new DualSenseSectionResistanceProperties(),
							Continuous = new DualSenseContinuousResistanceProperties()
						};
						rightTriggerState.EffectEx.StartPosition = (byte)(startPosision * 255);
						rightTriggerState.EffectEx.BeginForce = (byte)(beginForce * 255);
						rightTriggerState.EffectEx.MiddleForce = (byte)(middleForce * 255);
						rightTriggerState.EffectEx.EndForce = (byte)(endForce * 255);
						rightTriggerState.EffectEx.Frequency = (byte)(frequency * 255);
						rightTriggerState.EffectEx.KeepEffect = keepEffect;
						CurrentCommand.SetRightTriggerState(rightTriggerState);
						break;

					default: return;
				}
			}

		

			


		
		} 
			
		#endregion
	}

