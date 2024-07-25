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
using UniSense.DevConnections;

//TODO: Finish this
//TODO: There Was a bug that I can't replicate anymore. when bt dissconeted and usb was attached when two players were instatnatied with the manager,
//for whatever reason user.connection open would be set to false and no output commands would be sent
public class DualSense : MonoBehaviour, IHandleSingleplayer, IManageable
{
    private int _currentUserIndex = -1;
    public bool AllowKeyboardMouse;
    public bool AllowGenericController;
	private bool _isManaged;
	private OS_Type _osType;
    private ref OldUniSenseUser _currentuser
    {
        get { return ref OldUniSenseConnectionHandler.UnisenseUsers[_currentUserIndex]; }
    }

	public DualSense()
    {
		_osType = (IntPtr.Size == 4) ? OS_Type._x86 : OS_Type._x64;
	}


    #region SinglePlayer

    public void Start()
    {
		
		if (DualSenseManager.instance != null) //If DualSenseManager exists in the scene then let DualSenseManager handle initialization
        {
			_isManaged = true;
			return;
        }
        if (OldUniSenseConnectionHandler.IsInitialized) return;
        if (OldUniSenseConnectionHandler.InitializeSingleplayer(this, AllowKeyboardMouse, AllowGenericController))
        {
            Debug.Log("Initialization successful");
        }
        else
        {
            Debug.LogError("Initialization failed");
            return;
        }
    }
    public void InitilizeUsers(int unisenseId)
    {
		
        Debug.Log("Initialization started");
		if (unisenseId == -1) return;
		_currentUserIndex = unisenseId;
		_currentuser.PairWithPlayerInput(GetComponent<PlayerInput>());
		if(_currentuser.BTAttached) _currentuser.SetActiveDevice(UniSense.DevConnections.DeviceType.DualSenseBT);
		if (_currentuser.USBAttached) _currentuser.SetActiveDevice(UniSense.DevConnections.DeviceType.GenericGamepad);
    }

    public void OnCurrentUserChanged(int unisenseId)
    {
		Debug.Log("Current User Changed From: " + _currentUserIndex + "To: " + unisenseId);
		if(_currentUserIndex != -1)
        {
			_currentuser.SetActiveDevice(UniSense.DevConnections.DeviceType.None);
			_currentuser.UnPairPlayerInput();

		}
		_currentUserIndex = unisenseId;
		_currentuser.PairWithPlayerInput(GetComponent<PlayerInput>());
		if (_currentuser.USBAttached && _currentuser.SetActiveDevice(UniSense.DevConnections.DeviceType.DualSenseUSB)) return;

		//if (!_currentuser.DontOpenWirelessConnection && _currentuser.SetActiveDevice(UniSense.DevConnections.DeviceType.DualSenseBT)) return; //Updated version below DontOpenWirelessConnection deemed obsolete. If the BT device reports an active USB device but no device is found just continue as normal with BT.
		if (_currentuser.BTAttached && _currentuser.SetActiveDevice(UniSense.DevConnections.DeviceType.DualSenseBT)) return;

		if (_currentuser.GenericAttached && _currentuser.SetActiveDevice(UniSense.DevConnections.DeviceType.GenericGamepad)) return;

		//if(!NewUniSenseConnectionHandler.RemoveCurrentUser()) Debug.LogError("Attempt to connect failed. Failed to remove current user");

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
				if (!_currentuser.SetActiveDevice(UniSense.DevConnections.DeviceType.DualSenseUSB)) Debug.LogError("Can't set USB active");
                break;
            case UserChange.BTDisabled:
                _currentuser.SetActiveDevice(UniSense.DevConnections.DeviceType.None);
				Invoke("QueueBTRemoval", 2);
                break;
            case UserChange.USBAdded:
				if(IsInvoking("QueueBTRemoval")) CancelInvoke("QueueBTRemoval");
				if (!_currentuser.SetActiveDevice(UniSense.DevConnections.DeviceType.DualSenseUSB)) Debug.LogError("Can't set USB active");
				break;
            case UserChange.USBRemoved:
				if (!_currentuser.SetActiveDevice(UniSense.DevConnections.DeviceType.DualSenseBT)) Debug.LogError("Can't set BT active");
				break;
            case UserChange.GenericAdded:
				Debug.LogError("Generic should never be added in OnCurrentUserModified"); 
				break;
            case UserChange.GenericRemoved:
				Debug.LogError("Generic should never be removed in OnCurrentUserModified");
				break;
            default:
                break;
        }
		
    }

	public bool QueueBTRemoval() 
    {
		return OldUniSenseConnectionHandler.RemoveCurrentUser();
    }

    public void OnNoCurrentUser()
    {
       _currentUserIndex = -1;
    }

    #endregion

    #region MultiPlayer
    public void SetCurrentUser(int unisenseId)
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



	private bool _userConnected => _currentuser.ConnectionOpen;



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

		switch (_currentuser.ActiveDevice)
		{
			case UniSense.DevConnections.DeviceType.DualSenseBT:
				byte[] rawDeviceCommand = GetRawCommand(_currentCommand);
				DS5W_ReturnValue status = (_osType == OS_Type._x64) ? DS5W_x64.setDeviceRawOutputState(ref _currentuser.Devices.contextBT, rawDeviceCommand, rawDeviceCommand.Length) :
																      DS5W_x86.setDeviceRawOutputState(ref _currentuser.Devices.contextBT, rawDeviceCommand, rawDeviceCommand.Length);
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

			case UniSense.DevConnections.DeviceType.DualSenseUSB:
				_currentuser.Devices.DualsenseUSB?.ExecuteCommand(ref _currentCommand);
				break;
			case UniSense.DevConnections.DeviceType.GenericGamepad:
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
