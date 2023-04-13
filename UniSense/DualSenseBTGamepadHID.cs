using UniSense.LowLevel;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.Scripting;
using System.Collections.Generic;
namespace UniSense
{

	[InputControlLayout(
		stateType = typeof(DualSenseBTHIDInputReport),
		displayName = "PS5 Controller")]
	[Preserve]
#if UNITY_EDITOR
	[InitializeOnLoad]
#endif
	//This Script is responsible for sending haptic signals to the connected DualSense controller.

	//It uses unity inbuilt input InputDevice.ExecuteCommand<TCommand> method in combination with 
	//the custom device command "language" DualSenseHIDOutputReport.

	public class DualSenseBTGamepadHID : DualShockGamepad
	{
		private DualSenseBTHIDOutputReport CurrentCommand = DualSenseBTHIDOutputReport.Create();
		public ButtonControl leftTriggerButton { get; protected set; }
		public ButtonControl rightTriggerButton { get; protected set; }
		public ButtonControl playStationButton { get; protected set; }
		public ButtonControl micMuteButton { get; protected set; }




#if UNITY_EDITOR
		static DualSenseBTGamepadHID()
		{
			Initialize();
		}
#endif

		/// <summary>
		/// Finds the first DualSense connected by the player or <c>null</c> if 
		/// there is no one connected to the system.
		/// </summary>
		/// <returns>A DualSenseGamepadHID instance or <c>null</c>.</returns>
		public static Gamepad[] FindAll()
		{

			List<Gamepad> dualSenseUSBGamepads = new List<Gamepad>();
			foreach (var gamepad in all)
			{
				var isDualSenseGamepad = gamepad is DualSenseBTGamepadHID;
				if (isDualSenseGamepad) dualSenseUSBGamepads.Add(gamepad);

			}

			return dualSenseUSBGamepads.ToArray();
		}

		/// <summary>
		/// Finds the DualSense last used/connected by the player or <c>null</c> if 
		/// there is no one connected to the system.
		/// </summary>
		/// <returns>A DualSenseGamepadHID instance or <c>null</c>.</returns>
		
	
		//public static DualSenseBTGamepadHID FindCurrent() => Gamepad.current as DualSenseBTGamepadHID;
		

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void Initialize()
		{
			InputSystem.RegisterLayout<DualSenseBTGamepadHID>(
				matches: new InputDeviceMatcher()
				.WithInterface("HID")
                .WithManufacturer("Sony.+Entertainment")
                .WithCapability("vendorId", 0x54C)
                .WithCapability("inputReportSize", 78)
                .WithCapability("productId", 0xCE6));

			


		}

		protected override void FinishSetup()
		{
			leftTriggerButton = GetChildControl<ButtonControl>("leftTriggerButton");
			rightTriggerButton = GetChildControl<ButtonControl>("rightTriggerButton");
			playStationButton = GetChildControl<ButtonControl>("systemButton");
			micMuteButton = GetChildControl<ButtonControl>("micMuteButton");

			base.FinishSetup();
		}

		private bool MotorHasValue => m_LowFrequencyMotorSpeed.HasValue || m_HighFrequenceyMotorSpeed.HasValue;
		private bool LeftTriggerHasValue => m_leftTriggerState.HasValue;
		private bool RightTriggerHasValue => m_rightTriggerState.HasValue;

		public override void PauseHaptics()
		{
			if (!MotorHasValue && !LeftTriggerHasValue && !RightTriggerHasValue)
				return;

			var command = DualSenseBTHIDOutputReport.Create();
			command.ResetMotorSpeeds();
			command.SetLeftTriggerState(new DualSenseTriggerState());
			command.SetRightTriggerState(new DualSenseTriggerState());

			ExecuteCommand(ref command);
		}

		public override void ResetHaptics()
		{
			if (!MotorHasValue && !LeftTriggerHasValue && !RightTriggerHasValue)
				return;

			var command = DualSenseBTHIDOutputReport.Create();
			command.ResetMotorSpeeds();
			command.SetLeftTriggerState(new DualSenseTriggerState());
			command.SetRightTriggerState(new DualSenseTriggerState());

			ExecuteCommand(ref command);

			m_HighFrequenceyMotorSpeed = null;
			m_LowFrequencyMotorSpeed = null;
		}

		public void ResetMotorSpeeds() => SetMotorSpeeds(0f, 0f);

		public void ResetLightBarColor() => SetLightBarColor(Color.black);

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

			ExecuteCommand(ref CurrentCommand);
		}

		public void Reset()
		{
			ResetHaptics();
			ResetMotorSpeeds();
			ResetLightBarColor();
			ResetTriggersState();
			SendCommand();
		}

		public void ResetCommand()
		{
			CurrentCommand = DualSenseBTHIDOutputReport.Create();
		}
		public void SendCommand()
		{
			ExecuteCommand(ref CurrentCommand);
		}

		public void SetGamepadState(DualSenseGamepadState state)
		{
			var command = DualSenseBTHIDOutputReport.Create();


			if (state.LightBarColor.HasValue)
			{
				var lightBarColor = state.LightBarColor.Value;
				command.SetLightBarColor(lightBarColor);
			}

			if (state.Motor.HasValue)
			{
				var motor = state.Motor.Value;
				command.SetMotorSpeeds(motor.LowFrequencyMotorSpeed, motor.HighFrequenceyMotorSpeed);
				m_LowFrequencyMotorSpeed = motor.LowFrequencyMotorSpeed;
				m_HighFrequenceyMotorSpeed = motor.HighFrequenceyMotorSpeed;
			}

			if (state.MicLed.HasValue)
			{
				var micLed = state.MicLed.Value;
				command.SetMicLedState(micLed);
			}

			if (state.RightTrigger.HasValue)
			{
				var rightTriggerState = state.RightTrigger.Value;
				command.SetRightTriggerState(rightTriggerState);
				m_rightTriggerState = rightTriggerState;
			}

			if (state.LeftTrigger.HasValue)
			{
				var leftTriggerState = state.LeftTrigger.Value;
				command.SetLeftTriggerState(leftTriggerState);
				m_leftTriggerState = leftTriggerState;
			}

			if (state.PlayerLedBrightness.HasValue)
			{
				var playerLedBrightness = state.PlayerLedBrightness.Value;
				command.SetPlayerLedBrightness(playerLedBrightness);
			}

			if (state.PlayerLed.HasValue)
			{
				var playerLed = state.PlayerLed.Value;
				command.SetPlayerLedState(playerLed);
			}

			ExecuteCommand(ref command);
		}

		internal void ExecuteThisCommand(DualSenseBTHIDOutputReport command)
		{
			ExecuteCommand(ref command);
		}


		private float? m_LowFrequencyMotorSpeed;
		private float? m_HighFrequenceyMotorSpeed;
		private DualSenseTriggerState? m_rightTriggerState;
		private DualSenseTriggerState? m_leftTriggerState;
	}
}