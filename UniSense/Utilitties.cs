using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WrapperDS5W;
using UnityEngine.InputSystem;

namespace UniSense.Utilities
{
	internal enum OS_Type
	{
		_x64,
		_x86
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
}

