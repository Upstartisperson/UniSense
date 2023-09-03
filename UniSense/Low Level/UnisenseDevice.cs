using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using UniSense.DS5WWrapper;
using UnityEngine.InputSystem.Haptics;

namespace UniSense.LowLevel
{
	
    public class UnisenseDevice
	{
		public static List<UnisenseDevice> Devices { get; private set; }
		public readonly InputDevice InputDevice;
		private DeviceContext _deviceContext;
		private DeviceEnumInfo _deviceEnumInfo;
		public readonly DeviceType DeviceType;
		public readonly string DeviceKey;
		public readonly int UnisenseDeviceID;
		public readonly string SerialNumber;
		private static int _nextID = 0;
		public bool OutputEnabled { get; private set; }
		public bool Enabled { get; private set; }

		static UnisenseDevice()
		{
			Devices = new List<UnisenseDevice>();
		}
		/// <summary>
		/// Assumes that if it is BT DS5W has already been verified
		/// </summary>
		/// <param name="inputDevice"></param>
		/// <exception cref="ArgumentException">Not a supported input device</exception>
		private UnisenseDevice(InputDevice inputDevice)
		{
			switch (inputDevice)
			{
				case DualSenseBTGamepadHID:
					DeviceKey = inputDevice.description.serial;
					SerialNumber = inputDevice.description.serial;
					DeviceType = DeviceType.DualSenseBT;

					break;
				case DualSenseUSBGamepadHID:
					DeviceKey = inputDevice.deviceId.ToString();
					DeviceType = DeviceType.DualSenseUSB;
					break;
				case Gamepad:
					DeviceKey = inputDevice.deviceId.ToString();
					DeviceType = DeviceType.GenericGamepad;
					break;
				default: Debug.LogError("Not a supported input device"); throw new ArgumentException("Not a supported input device");
			}
			this.InputDevice = inputDevice;
			this.OutputEnabled = false;
			//this.EnableImpl(); 
			//TODO : fix
			this.UnisenseDeviceID = _nextID++;
			Devices.Add(this);
		}

		public static UnisenseDevice CreateDevice(InputDevice inputDevice)
		{
			Devices.Add(new UnisenseDevice(inputDevice));
			return Devices[Devices.Count - 1];
		}
		public static bool RemoveDevice(UnisenseDevice device)
		{
			if (!FindDevice(device, out int index)) return false;
			Devices.RemoveAt(index);
			return true;
		}
		public static bool FindDevice(UnisenseDevice device, out int index)
		{
			index = -1;
			if (device == null) return false;
			for (index = 0; index < Devices.Count; index++)
			{
				if (Devices[index].DeviceKey == device.DeviceKey) return true;
			}
			return false;
		}


		public bool EnableInput()
		{
			if (InputDevice == null) { Debug.LogError("InputDevice Null"); return false; }
			if (!FindDevice(this, out int index))
			{
				Debug.LogError("I really fucked up");
				return false;
			}
			InputSystem.EnableDevice(InputDevice);
			Enabled = true;
			Devices[index] = this;
			return true;
		}
		public bool DisableInput()
		{
			if (InputDevice == null) { Debug.LogError("InputDevice Null"); return false; }
			if (!FindDevice(this, out int index))
			{
				Debug.LogError("I really fucked up");
				return false;
			}
			InputSystem.DisableDevice(InputDevice);
			Enabled = false;
			Devices[index] = this;
			return true;
		}
		public bool EnableOutput()
		{
			if (OutputEnabled) { Debug.LogWarning("Redundant Enable"); return true; }
			if (!FindDevice(this, out int index))
			{
				Debug.LogError("I really fucked up");
				return false;
			}
			if (this.DeviceType == DeviceType.DualSenseBT)
			{
				DeviceEnumInfo enumInfo = new DeviceEnumInfo();
				DS5W_ReturnValue status = (OSType.Type == OS_Type._x64) ? DS5W_x64.findDevice(ref enumInfo, SerialNumber) :
																		  DS5W_x86.findDevice(ref enumInfo, SerialNumber);
				if (status != DS5W_ReturnValue.OK)
				{
					Debug.LogError(status.ToString());
					//TODO: Handle Error
					return false;
				}
				DeviceContext deviceContext = new DeviceContext();
				status = (OSType.Type == OS_Type._x64) ? DS5W_x64.initDeviceContext(ref enumInfo, ref deviceContext) :
														 DS5W_x86.initDeviceContext(ref enumInfo, ref deviceContext);
				if (status != DS5W_ReturnValue.OK)
				{
					Debug.LogError(status.ToString());
					//TODO: Handle Error
					return false;
				}
				this._deviceEnumInfo = enumInfo;
				this._deviceContext = deviceContext;
			}
			this.OutputEnabled = true;
			Devices[index] = this;
			return true;
		}
		public bool DisableOutput(bool ClearOutput)
		{
			if (!OutputEnabled) { Debug.LogWarning("Redundant Disable"); return true; }
			if (!FindDevice(this, out int index))
			{
				Debug.LogError("I really fucked up");
				return false;
			}
			if (this.DeviceType == DeviceType.DualSenseBT)
			{

				try
				{
					if (OSType.Type == OS_Type._x64) DS5W_x64.freeDeviceContext(ref this._deviceContext, ClearOutput);
					else DS5W_x86.freeDeviceContext(ref this._deviceContext, ClearOutput);
				}
				catch (Exception e)
				{
					Debug.LogError(e.ToString());
					//TODO: Handle Error
					return false;
				}

				this._deviceEnumInfo = new();
				this._deviceContext = new();
			}
			else
			{
				if (ClearOutput && this.InputDevice is IDualMotorRumble) ((IDualMotorRumble)this.InputDevice).ResetHaptics();
			}
			this.OutputEnabled = false;
			Devices[index] = this;
			return true;
		}
		public bool SendOutput(DualSenseHIDOutputReport command)
		{
			if (!this.OutputEnabled) { Debug.LogError("Output Not Enabled"); return false; }

			switch (DeviceType)
			{
				case DeviceType.DualSenseBT:
					byte[] rawCommand = GetRawCommand(command);
					DS5W_ReturnValue status = (OSType.Type == OS_Type._x64) ? DS5W_x64.setDeviceRawOutputState(ref this._deviceContext, rawCommand, rawCommand.Length) :
																			  DS5W_x86.setDeviceRawOutputState(ref this._deviceContext, rawCommand, rawCommand.Length);
					if (status != DS5W_ReturnValue.OK)
					{
						Debug.LogError(status.ToString());
						//TODO: Handle Error
						return false;
					}
					break;
				case DeviceType.DualSenseUSB:
					InputDevice?.ExecuteCommand(ref command);
					break;
				case DeviceType.GenericGamepad:
					if (InputDevice is IDualMotorRumble)
					{
						((IDualMotorRumble)InputDevice).SetMotorSpeeds(command.lowFrequencyMotorSpeed, command.highFrequencyMotorSpeed);
					}
					break;
				case DeviceType.None:
					Debug.LogError("No Devices Connected");
					return false;
				default:
					break;
			}
			return true;
		}
		private byte[] GetRawCommand(DualSenseHIDOutputReport command)
		{
			byte[] inbuffer = command.RetriveCommand();
			byte[] outbuffer = new byte[47];
			Array.Copy(inbuffer, 9, outbuffer, 0, 47);
			return outbuffer;
		}
	}

}


