using UniSense.LowLevel;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.Scripting;

namespace UniSense
{

	[InputControlLayout(
		stateType = typeof(DualSenseHIDInputReport),
		displayName = "PS5 Controller")]
	[Preserve]
#if UNITY_EDITOR
	[InitializeOnLoad]
#endif
	//This Script is responsible for sending haptic signals to the connected DualSense controller.

	//It uses unity inbuilt input InputDevice.ExecuteCommand<TCommand> method in combination with 
	//the custom device command "language" DualSenseHIDOutputReport.

	public class DualSenseGamepadHID : DualShockGamepad
	{
		private DualSenseHIDOutputReport CurrentCommand = DualSenseHIDOutputReport.Create();
		public ButtonControl leftTriggerButton { get; protected set; }
		public ButtonControl rightTriggerButton { get; protected set; }
		public ButtonControl playStationButton { get; protected set; }
		public ButtonControl micMuteButton { get; protected set; }




#if UNITY_EDITOR
		static DualSenseGamepadHID()
		{
			Initialize();
		}
#endif

		/// <summary>
		/// Finds the first DualSense connected by the player or <c>null</c> if 
		/// there is no one connected to the system.
		/// </summary>
		/// <returns>A DualSenseGamepadHID instance or <c>null</c>.</returns>
		public static DualSenseGamepadHID FindFirst()
		{
			foreach (var gamepad in all)
			{
				var isDualSenseGamepad = gamepad is DualSenseGamepadHID;
				if (isDualSenseGamepad) return gamepad as DualSenseGamepadHID;
			}

			return null;
		}

		/// <summary>
		/// Finds the DualSense last used/connected by the player or <c>null</c> if 
		/// there is no one connected to the system.
		/// </summary>
		/// <returns>A DualSenseGamepadHID instance or <c>null</c>.</returns>
		public static DualSenseGamepadHID FindCurrent() => Gamepad.current as DualSenseGamepadHID;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void Initialize()
		{
			InputSystem.RegisterLayout<DualSenseGamepadHID>(
				matches: new InputDeviceMatcher()
					.WithInterface("HID")
					.WithManufacturer("Sony.+Entertainment")
					.WithCapability("vendorId", 0x54C)
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

			var command = DualSenseHIDOutputReport.Create();
			command.ResetMotorSpeeds();
			command.SetLeftTriggerState(new DualSenseTriggerState());
			command.SetRightTriggerState(new DualSenseTriggerState());

			ExecuteCommand(ref command);
		}

		public override void ResetHaptics()
		{
			if (!MotorHasValue && !LeftTriggerHasValue && !RightTriggerHasValue)
				return;

			var command = DualSenseHIDOutputReport.Create();
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
			CurrentCommand = DualSenseHIDOutputReport.Create();
		}
		public void SendCommand()
		{
			ExecuteCommand(ref CurrentCommand);
		}

		public void SetGamepadState(DualSenseGamepadState state)
		{
			var command = DualSenseHIDOutputReport.Create();


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

		internal void ExecuteThisCommand(DualSenseHIDOutputReport command)
		{
			ExecuteCommand(ref command);
		}


		private float? m_LowFrequencyMotorSpeed;
		private float? m_HighFrequenceyMotorSpeed;
		private DualSenseTriggerState? m_rightTriggerState;
		private DualSenseTriggerState? m_leftTriggerState;
	}
}