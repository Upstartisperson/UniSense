using WrapperDS5W;
using UnityEngine.InputSystem;

namespace UniSense.Utilities
{
	/// <summary>
	/// Enum for differentiating between the 64-bit (_x64) and 32-bit (_x86) versions of windows
	/// </summary>
	internal enum OS_Type
	{
		_x64,
		_x86
	}

	/// <summary>
	/// Enum that stores all possible types of device connections that are possible
	/// </summary>
	public enum DeviceType
	{
		DualSenseBT,
		DualSenseUSB,
		GenericGamepad,
		MouseKeyboard,
		None
	}

	/// <summary>
	/// Class that holds all of the devices a <see cref="UniSense.Users.UniSenseUser"/> needs to store
	/// </summary>
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

