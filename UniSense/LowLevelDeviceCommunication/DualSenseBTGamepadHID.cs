using UniSense.LowLevel;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.Scripting;
using System.Collections.Generic;
using UnityEngine.InputSystem.DualSense;
using WrapperDS5W;
namespace UniSense
{

	[InputControlLayout(
		stateType = typeof(DualSenseBTHIDInputReport),
		displayName = "Blue Tooth PS5 Controller")]
	[Preserve]
	//This Script is responsible for sendinSg haptic signals to the connected DualSense controller.

	//It uses unity inbuilt input InputDevice.ExecuteCommand<TCommand> method in combination with 
	//the custom device command "language" DualSenseHIDOutputReport.

	public class DualSenseBTGamepadHID : UnisenseDualSenseGamepad
	{


		/// <summary>
		/// Finds all the connected DualSense controllers or <c>null</c> if 
		/// there is no one connected to the system.
		/// </summary>
		/// <returns>An array of gamepads or <c>null</c>.</returns>
		public static Gamepad[] FindAll()
		{

			List<Gamepad> dualSenseUSBGamepads = new List<Gamepad>();
			foreach (var gamepad in all)
			{
				var isDualSenseGamepad = gamepad is DualSenseBTGamepadHID;
				if (isDualSenseGamepad) dualSenseUSBGamepads.Add(gamepad);

			}

			return (dualSenseUSBGamepads.Count > 0) ? dualSenseUSBGamepads.ToArray() : null;
		}

		/// <summary>
		/// Finds the first DualSense connected by the player or <c>null</c> if 
		/// there is no one connected to the system.
		/// </summary>
		/// <returns>A gamepad instance or <c>null</c>.</returns>
		public static Gamepad FindFirst()
		{
			foreach (var gamepad in all)
			{
				var isDualSenseGamepad = gamepad is DualSenseBTGamepadHID;
				if (isDualSenseGamepad) return gamepad;
			}
			return null;
		}

		/// <summary>
		/// Finds the DualSense last used/connected by the player or <c>null</c> if 
		/// there is no one connected to the system.
		/// </summary>
		/// <returns>A DualSenseGamepadHID instance or <c>null</c>.</returns>
		public static DualSenseBTGamepadHID FindCurrent() => Gamepad.current as DualSenseBTGamepadHID;



        protected override void OnAdded() 
        {	
			base.OnAdded();
			InputSystem.DisableDevice(this); //Disable device to stop 

		}

        protected override void FinishSetup()
		{
            base.FinishSetup();
        }

	}
}