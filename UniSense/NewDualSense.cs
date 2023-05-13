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
using UniSense.NewConnections;

public class NewDualSense : MonoBehaviour
{

	

    private void Start()
    {
		if (!GetComponent<PlayerInput>().neverAutoSwitchControlSchemes) Debug.LogError("Disable 'Auto-Switch' on Player Input");
		if (DualSenseManager.instance != null) return;
		NewUniSenseConnectionHandler.Initilize(uniqueIdentifier: new UniqueIdentifier(gameObject, this));
		NewUniSenseConnectionHandler.OnCurrentControllerUpdated += UpadateCurrentController;
		_currentController = NewUniSenseConnectionHandler.CurrentController;
		_isManaged = false;
	}

	private bool _isManaged = true;

    private void OnDisable()
    {
		if (_isManaged) return;
 		NewUniSenseConnectionHandler.Destroy(new UniqueIdentifier(gameObject, this));
    }

    private void UpadateCurrentController(Controller controller)
    {
		_currentController = controller;
    }



    #region Variables
  
	private OS_Type oS;
	private Controller _currentController = new Controller();
	public Controller CurrentController { get { return _currentController; } set { _currentController = value; }  }
	#endregion



	private byte[] GetRawCommand(DualSenseHIDOutputReport command)
	{
		byte[] inbuffer = command.RetriveCommand();
		byte[] outbuffer = new byte[47];
		Array.Copy(inbuffer, 9, outbuffer, 0, 47);
		return outbuffer;
	}


	//This region of the script functions as an easy way of communicating with the DualSense controller's haptic and LED systems
	#region Commands
	private DualSenseHIDOutputReport CurrentCommand = DualSenseHIDOutputReport.Create();

	

	private bool controllerConnected => _currentController.connectionStatus != ControllerConnectionStatus.Disconected;



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



	public void SendCommand() //intended to be called by the local component attached to the player
	{
	

		if (_currentController == null) return;
		if (_currentController.connectionStatus == ControllerConnectionStatus.Disconected) return;
		switch (_currentController.ControllerType)
		{
			case ControllerType.DualSenseBT:

				byte[] rawDeviceCommand = GetRawCommand(CurrentCommand);
				DS5W_RetrunValue status = (oS == OS_Type._x64) ? DS5W_x64.setDeviceRawOutputState(ref _currentController.devices.contextBT, rawDeviceCommand, rawDeviceCommand.Length) :
																 DS5W_x86.setDeviceRawOutputState(ref _currentController.devices.contextBT, rawDeviceCommand, rawDeviceCommand.Length);
				if (status != DS5W_RetrunValue.OK) Debug.LogError(status.ToString());
				break;

			case ControllerType.DualSenseUSB:

				_currentController.devices.DualsenseUSB?.ExecuteCommand(ref CurrentCommand);
				break;
			case ControllerType.GenericGamepad:
				Debug.LogError("Not DualSense Controller");
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



    #endregion



} 

