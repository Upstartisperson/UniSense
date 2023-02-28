using UniSense;
using UnityEngine;
using UnityEngine.InputSystem;
using UniSense.LowLevel;





/// <summary>
/// Functions to expose methods responsible for controlling the haptic's and LED of a DualSense Controller.
/// <example>
/// For Example in a separate script:
/// <code>
/// private void Start()
/// {
/// DualSenseManager dualSense = gameobject.GetComponent[DualSenseManager](); 
/// dualSense.SetTriggerContinuousResistance(DualSenseTrigger.Left, 1f, 0.5f);
/// }
/// </code>
/// </example>
/// </summary>
[DisallowMultipleComponent]
public class DualSenseManager : MonoBehaviour
{

	#region DualSense Monitoring
	//This Region of the script is responsible for finding a DualSense controller and giving the second half of the script a reference of that controller

	private DualSenseGamepadHID Dualsense;
	private void Start()
	{

		var dualSense = DualSenseGamepadHID.FindCurrent();
		var isDualSenseConected = dualSense != null;
		if (isDualSenseConected) NotifyConnection(dualSense);
		else NotifyDisconnection();

	}

	private void OnEnable() => InputSystem.onDeviceChange += OnDeviceChange;

	private void OnDisable()
	{
		InputSystem.onDeviceChange -= OnDeviceChange;
		var dualSense = DualSenseGamepadHID.FindCurrent();
		dualSense?.Reset();
	}

	private void OnDeviceChange(InputDevice device, InputDeviceChange change)
	{
		var isNotDualSense = !(device is DualSenseGamepadHID);
		if (isNotDualSense) return;

		switch (change)
		{
			case InputDeviceChange.Added:
				NotifyConnection(device as DualSenseGamepadHID);
				break;
			case InputDeviceChange.Reconnected:
				NotifyConnection(device as DualSenseGamepadHID);
				break;
			case InputDeviceChange.Disconnected:
				NotifyDisconnection();
				break;
		}
	}

	private void NotifyConnection(DualSenseGamepadHID dualSense)
	{
		Dualsense = dualSense;
	}

	private void NotifyDisconnection()
	{
		Dualsense = null;
	}

	#endregion


	#region DualSense Communication

	//This region of the script functions as an easy way of communicating with the DualSense controller's haptic and LED systems

	private DualSenseHIDOutputReport CurrentCommand = DualSenseHIDOutputReport.Create();
	private bool MotorHasValue => m_LowFrequencyMotorSpeed.HasValue || m_HighFrequenceyMotorSpeed.HasValue;
	private bool LeftTriggerHasValue => m_leftTriggerState.HasValue;
	private bool RightTriggerHasValue => m_rightTriggerState.HasValue;

	public void PauseHaptics()
	{
		if (Dualsense == null)
		{
			Debug.LogError("No Controller Connected");
			return;
		}
		Dualsense?.PauseHaptics();
	}

	public void ResetHaptics()
	{
		if (Dualsense == null)
		{
			Debug.LogError("No Controller Connected");
			return;
		}
		Dualsense?.ResetHaptics();
	}

	public void ResetMotorSpeeds() => SetMotorSpeeds(0f, 0f);

	public void ResetLightBarColor() => SetLightBarColor(Color.black);

	public void ResetTriggersState() => Dualsense?.ResetTriggersState();

	public void Reset()
	{
		if (Dualsense == null)
		{
			Debug.LogError("No Controller Connected");
			return;
		}
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
		if (Dualsense == null)
		{
			Debug.LogError("No Controller Connected");
			return;
		}
		Dualsense?.ExecuteThisCommand(CurrentCommand);
	}

	/// <summary>
	/// Use this method to set the continuous resistance state of either the left or the right trigger 
	/// </summary>
	/// <param name="trigger">From the UniSense namespace must be either DualSenseTrigger.Left, or DualSenseTrigger.Right</param>
	/// <param name="force">How well will the trigger resist pressure. (Range from 0 to 1)</param>
	/// <param name="position">How much pre-travel before resistance begins. (Range from 0 to 1)</param>
	public void SetTriggerContinuousResistance(DualSenseTrigger trigger, float force, float position)
	{
		if (Dualsense == null)
		{
			Debug.LogError("No Controller Connected");
			return;
		}
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
				m_leftTriggerState = leftTriggerState;
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
				m_rightTriggerState = rightTriggerState;
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
		if (Dualsense == null)
		{
			Debug.LogError("No Controller Connected");
			return;
		}
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
				m_leftTriggerState = leftTriggerState;
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
				m_rightTriggerState = rightTriggerState;
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
		if (Dualsense == null)
		{
			Debug.LogError("No Controller Connected");
			return;
		}
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
				m_leftTriggerState = leftTriggerState;
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
				m_rightTriggerState = rightTriggerState;
				CurrentCommand.SetRightTriggerState(rightTriggerState);
				break;

			default: return;
		}
	}

	public void SetLightBarColor(Color color)
	{
		if (Dualsense == null)
		{
			Debug.LogError("No Controller Connected");
			return;
		}

		CurrentCommand.SetLightBarColor(color);
	}

	public void SetMotorSpeeds(float lowFrequency, float highFrequency)
	{
		if (Dualsense == null)
		{
			Debug.LogError("No Controller Connected");
			return;
		}
		CurrentCommand.SetMotorSpeeds(lowFrequency, highFrequency);
		m_LowFrequencyMotorSpeed = lowFrequency;
		m_HighFrequenceyMotorSpeed = highFrequency;
	}

	private float? m_LowFrequencyMotorSpeed;
	private float? m_HighFrequenceyMotorSpeed;
	private DualSenseTriggerState? m_rightTriggerState;
	private DualSenseTriggerState? m_leftTriggerState;
	#endregion
}




