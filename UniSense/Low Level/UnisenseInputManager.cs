using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UniSense.LowLevel;
using UnityEngine.InputSystem;

//TODO : Don't think this class has any place in my codes architecture


public static class UnisenseInputManager 
{
    private static List<UnisenseDevice> _devices = new();
 
    public static List<UnisenseDevice> Devices { get { return _devices; } }
    
    //public static event Action

    //TODO: Could use dictionary if I need more performance
    
    public static bool TryFindDeviceByKey(string key, out int index)
    {
        for (index = 0; index < _devices.Count; index++)
        {
            if (_devices[index].DeviceKey == key) return true;
        }
		
        return false;
    }
	//#region Protected Accesses
	//public static bool EnableInput(UnisenseDevice unisenseDevice)
	//{
	//	if (!FindDevice(unisenseDevice, out int index))
	//	{
	//		Debug.LogError("Device Not Found");
	//		return false;
	//	}
	//	return _devices[index].EnableImpl();
	//}
	//public static bool DisableInput(UnisenseDevice unisenseDevice)
	//{
	//	if (!FindDevice(unisenseDevice, out int index))
	//	{
	//		Debug.LogError("Device Not Found");
	//		return false;
	//	}
	//	return _devices[index].DisableImpl();
	//}
	//public static bool EnableOutput(UnisenseDevice unisenseDevice)
	//{
	//	if (!FindDevice(unisenseDevice, out int index))
	//	{
	//		Debug.LogError("Device Not Found");
	//		return false;
	//	}
	//	return _devices[index].EnableOutputImpl();
	//}
	//public static bool DisableOutput(UnisenseDevice unisenseDevice, bool clearOutput)
	//{
	//	if (!FindDevice(unisenseDevice, out int index))
	//	{
	//		Debug.LogError("Device Not Found");
	//		return false;
	//	}
	//	return _devices[index].DisableOutputImpl(clearOutput);
	//}
	//#endregion
	//private static bool EnableImpl(ref UnisenseDevice unisenseDevice)
	//{
	//	if (unisenseDevice.InputDevice == null) { Debug.LogError("InputDevice Null"); return false; }
	//	InputSystem.EnableDevice(unisenseDevice.InputDevice);
	//	unisenseDevice.Enabled = true;
	//	InputDevice fd;
	//	fd.enabled
	//	return true;
	//}
	//public static bool DisableImpl()
	//{
	//	if (InputDevice == null) { Debug.LogError("InputDevice Null"); return false; }
	//	InputSystem.DisableDevice(InputDevice);
	//	Enabled = false;
	//	return true;
	//}
	//private static bool EnableOutputImpl()
	//{
	//	if (OutputEnabled) { Debug.LogWarning("Redundant Enable"); return true; }
	//	if (!FindDevice(this, out int index))
	//	{
	//		Debug.LogError("I really fucked up");
	//		return false;
	//	}
	//	if (this.DeviceType == DeviceType.DualSenseBT)
	//	{
	//		DeviceEnumInfo enumInfo = new DeviceEnumInfo();
	//		DS5W_ReturnValue status = (OSType.Type == OS_Type._x64) ? DS5W_x64.findDevice(ref enumInfo, SerialNumber) :
	//																  DS5W_x86.findDevice(ref enumInfo, SerialNumber);
	//		if (status != DS5W_ReturnValue.OK)
	//		{
	//			Debug.LogError(status.ToString());
	//			//TODO: Handle Error
	//			return false;
	//		}
	//		DeviceContext deviceContext = new DeviceContext();
	//		status = (OSType.Type == OS_Type._x64) ? DS5W_x64.initDeviceContext(ref enumInfo, ref deviceContext) :
	//												 DS5W_x86.initDeviceContext(ref enumInfo, ref deviceContext);
	//		if (status != DS5W_ReturnValue.OK)
	//		{
	//			Debug.LogError(status.ToString());
	//			//TODO: Handle Error
	//			return false;
	//		}
	//		this._deviceEnumInfo = enumInfo;
	//		this._deviceContext = deviceContext;
	//	}
	//	this.OutputEnabled = true;
	//	Devices[index] = this;
	//	return true;
	//}
	//private static bool DisableOutputImpl(bool ClearOutput)
	//{
	//	if (!OutputEnabled) { Debug.LogWarning("Redundant Disable"); return true; }
	//	if (!FindDevice(this, out int index))
	//	{
	//		Debug.LogError("I really fucked up");
	//		return false;
	//	}
	//	if (this.DeviceType == DeviceType.DualSenseBT)
	//	{

	//		try
	//		{
	//			if (OSType.Type == OS_Type._x64) DS5W_x64.freeDeviceContext(ref this._deviceContext, ClearOutput);
	//			else DS5W_x86.freeDeviceContext(ref this._deviceContext, ClearOutput);
	//		}
	//		catch (Exception e)
	//		{
	//			Debug.LogError(e.ToString());
	//			//TODO: Handle Error
	//			return false;
	//		}

	//		this._deviceEnumInfo = new();
	//		this._deviceContext = new();
	//	}
	//	else
	//	{
	//		if (ClearOutput && this.InputDevice is IDualMotorRumble) ((IDualMotorRumble)this.InputDevice).ResetHaptics();
	//	}
	//	this.OutputEnabled = false;
	//	Devices[index] = this;
	//	return true;
	//}






}
