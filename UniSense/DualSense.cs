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
using WrapperDS5W;
using UniSense.Management;
using UniSense.Users;
using UniSense.Utilities;
using DeviceType = UniSense.Utilities.DeviceType;

//TODO: Finish this
//TODO: There Was a bug that I can't replicate anymore. when bt dissconeted and usb was attached when two players were instatnatied with the manager,
//for whatever reason user.connection open would be set to false and no output commands would be sent
public class DualSense : MonoBehaviour, IHandleSingleplayer, IManageable
{
    private int _currentUserIndex = -1;
    public bool AllowKeyboardMouse;
    public bool AllowGenericController;
	private bool _isManaged;
	private int _initTimer = 0;
	
    private ref UniSenseUser _currentUser
    {
        get { return ref UniSenseUser.Users[_currentUserIndex]; }
    }




	#region SinglePlayer

	public void Start()
    {
		
		if (DualSenseManager.instance != null) //If DualSenseManager exists in the scene then let DualSenseManager handle initialization
        {
			_isManaged = true;
			return;
        }
        if (UniSenseConnectionHandler.IsInitialized) return;
		QueueInit();
    }

    private void OnDestroy()
    {
		if (_initTimer < 50 && _initTimer > 0) InputSystem.onAfterUpdate -= QueueInit;
    }

    private void QueueInit()
    {
		if (_initTimer++ == 0) InputSystem.onAfterUpdate += QueueInit;
		if(_initTimer > 50)
        {
			InputSystem.onAfterUpdate -= QueueInit;
			if (UniSenseConnectionHandler.InitializeSingleplayer(this, AllowKeyboardMouse, AllowGenericController))
			{
				Debug.Log("Initialization successful");
			}
			else
			{
				Debug.LogError("Initialization failed");
				return;
			}
		}
		
    }
  //  public void InitilizeUsers(int unisenseId) //Deemed obsolete
  //  {
		
  //      Debug.Log("Initialization started");
		//if (unisenseId == -1) return;
		//_currentUserIndex = unisenseId;
		//_currentUser.PairWithPlayerInput(GetComponent<PlayerInput>());
		//if(_currentUser.BTAttached) _currentUser.SetActiveDevice(UniSense.DevConnections.DeviceType.DualSenseBT);
		//if (_currentUser.USBAttached) _currentUser.SetActiveDevice(UniSense.DevConnections.DeviceType.GenericGamepad);
  //  }

    public void SetCurrentUser_S(int unisenseId)
    {
		Debug.Log("Current User Changed From: " + _currentUserIndex + "To: " + unisenseId); //TODO: Remove Debug Log
		if(_currentUserIndex != -1)
        {
			_currentUser.SetActiveDevice(DeviceType.None);
			_currentUser.UnPairPlayerInput();
		}
		_currentUserIndex = unisenseId;
		_currentUser.PairWithPlayerInput(GetComponent<PlayerInput>());
		
		if (_currentUser.USBAttached && _currentUser.SetActiveDevice(DeviceType.DualSenseUSB)) return;
		else Debug.LogError("USB Failed To Connect");

		if (_currentUser.BTAttached && _currentUser.SetActiveDevice(DeviceType.DualSenseBT)) return;
		else Debug.LogError("BT Failed To Connect");

		if (_currentUser.GenericAttached && _currentUser.SetActiveDevice(DeviceType.GenericGamepad)) return;
		else Debug.LogError("Generic Failed To Connect");

	}



    public void OnCurrentUserModified(UserChange change)
    {
        Debug.Log("Current user modified: " + change.ToString());
        switch (change)
        {
            case UserChange.BTAdded:
				Debug.LogError("BT should never be added in OnCurrentUserModified"); //Because a BT can't be paired to USB, USB has to be paired to a BT
                break;
            case UserChange.BTRemoved: //Should only be called when there is an active USB connection
				if (!_currentUser.SetActiveDevice(DeviceType.DualSenseUSB)) Debug.LogError("Can't set USB active");
                break;
            case UserChange.USBAdded:
				if (!_currentUser.SetActiveDevice(DeviceType.DualSenseUSB)) Debug.LogError("Can't set USB active");
				break;
            case UserChange.USBRemoved:
				if (!_currentUser.SetActiveDevice(DeviceType.DualSenseBT)) Debug.LogError("Can't set BT active");
				break;
          
            default:
				Debug.LogError("Error: " + change.ToString() + "Should not be called in OnCurrentUserModified"); //TODO: Remove
                break;
        }
		
    }

    public void SetNoCurrentUser()
    {
       _currentUserIndex = -1;
    }

    #endregion

    #region MultiPlayer
    public void SetCurrentUser_M(int unisenseId) //TODO: Is this useless?
    {
        _currentUserIndex = unisenseId;
    }
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
	private DualSenseHIDOutputReport _currentCommand = DualSenseHIDOutputReport.Create();



	private bool _userConnected => _currentUser.ConnectionOpen;



	public void ResetHaptics()
	{
		//TODO : Implement a check to see if necessary
		_currentCommand = DualSenseHIDOutputReport.Create();
		_currentCommand.ResetMotorSpeeds();
		_currentCommand.SetLeftTriggerState(new DualSenseTriggerState());
		_currentCommand.SetRightTriggerState(new DualSenseTriggerState());
		//InputSystem.onEvent
		SendCommand();
	}
	public void SetLightBarColor(Color color) => _currentCommand.SetLightBarColor(color);
	public void SetMotorSpeeds(float lowFrequency, float highFrequency) => _currentCommand.SetMotorSpeeds(lowFrequency, highFrequency);
	public void ResetMotorSpeeds() => SetMotorSpeeds(0f, 0f);

	public void ResetLightBarColor() => SetLightBarColor(Color.black);

	public void SetMicLEDState(DualSenseMicLedState micLedState) => _currentCommand.SetMicLedState(micLedState);
	public void ResetLEDs() => _currentCommand.DisableLightBarAndPlayerLed();

	public void SetPlayerLED(PlayerLedBrightness brightness, PlayerLED playerLED, bool fade = false) => _currentCommand.SetPlayerLedState(new PlayerLedState((byte)playerLED, fade), brightness);


	public void ResetTriggersState()
	{
		var EmptyState = new DualSenseTriggerState
		{
			EffectType = DualSenseTriggerEffectType.NoResistance,
			EffectEx = new DualSenseEffectExProperties(),
			Section = new DualSenseSectionResistanceProperties(),
			Continuous = new DualSenseContinuousResistanceProperties()
		};

		_currentCommand.SetRightTriggerState(EmptyState);
		_currentCommand.SetLeftTriggerState(EmptyState);


	}

	public void ResetCommand()
	{
		_currentCommand = DualSenseHIDOutputReport.Create();
	}




	public void SendCommand() //intended to be called by the local component attached to the player
	{


		if (_currentUserIndex == -1) return;
		if (!_userConnected) return;

		switch (_currentUser.ActiveDevice)
		{
			case DeviceType.DualSenseBT:
				byte[] rawDeviceCommand = GetRawCommand(_currentCommand);
				DS5W_ReturnValue status = DS5W.setDeviceRawOutputState(ref _currentUser.Devices.contextBT, rawDeviceCommand, rawDeviceCommand.Length);
				if (status != DS5W_ReturnValue.OK)
				{
					//if(_currentuser.USBAttached && _currentuser.SetActiveDevice(UniSense.DevConnections.DeviceType.DualSenseUSB))
     //               {
					//	Debug.Log("Failed to send output report succsesfully switched active device");
					//	SendCommand();
					//	return;
     //               }
					//if(NewUniSenseConnectionHandler.RemoveCurrentUser())
     //               {
					//	Debug.Log("Failed to send output report attempting to change active user");
					//	SendCommand();
					//	return;
     //               }
					Debug.LogError(status.ToString());
				}
                    break;

			case DeviceType.DualSenseUSB:
				_currentUser.Devices.DualsenseUSB?.ExecuteCommand(ref _currentCommand);
				break;
			case DeviceType.GenericGamepad: //TODO add haptic feedback for generic controllers
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
				_currentCommand.SetLeftTriggerState(leftTriggerState);
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
				_currentCommand.SetRightTriggerState(rightTriggerState);
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
				_currentCommand.SetLeftTriggerState(leftTriggerState);
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
				_currentCommand.SetRightTriggerState(rightTriggerState);
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
				_currentCommand.SetLeftTriggerState(leftTriggerState);
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
				_currentCommand.SetRightTriggerState(rightTriggerState);
				break;

			default: return;
		}
	}



	#endregion
}
