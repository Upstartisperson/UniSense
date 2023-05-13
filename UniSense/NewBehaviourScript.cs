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
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.Layouts;
using UniSense.NewConnections;
public class NewBehaviourScript : MonoBehaviour
{
    NewDualSense dualSense;
  //  DualSenseHIDOutputReport CurrentCommand = new DualSenseHIDOutputReport();
    public DualSenseTrigger trigger;
    [Range(0f, 1f)]
    public float force;
    [Range(0f, 1f)]
    public float posistion;
    [Range(0f, 1f)]
    public float H_Rumble;
    [Range(0f, 1f)]
    public float L_Rumble;


    public bool connected = true;
    //bool thing = true;
    DeviceContext deviceContext;
    DS5W_RetrunValue DevcieStatus;
    public void Start()
    {

        NewUniSenseConnectionHandler.Initilize(new UniqueIdentifier(gameObject, this));
        dualSense = GetComponent<NewDualSense>();

        // int deviceid = DualSenseUSBGamepadHID.FindFirst().deviceId;
        // static extern HID.HIDDeviceDescriptor ReadHIDDeviceDescriptor(deviceid, ref InputDeviceDescription deviceDescription, IInputRuntime runtime)



        //DualSense sense = GetComponent<DualSense>();
        //      IDualSenseManager dualSenseManager = GetComponent<DualSense>();

        //      dualSenseManager.TestClass();

        //  uint deviceCount = 0;
        //      deviceContext = new DeviceContext();
        //      DeviceEnumInfo[] infos = new DeviceEnumInfo[16];
        //      IntPtr ptrBuffer = IntPtr.Zero;
        //      DS5WHelpers.BuildEnumDeviceBuffer(ref ptrBuffer, infos);
        //      DS5W_RetrunValue status = DS5W_x64.enumDevices(ref ptrBuffer, (uint)16, ref deviceCount, false);
        //      if (status != DS5W_RetrunValue.OK) Debug.LogError(status.ToString());
        //      DS5WHelpers.DeconstructEnumDeviceBuffer(ref ptrBuffer, ref infos);
        //      //status = DS5W_x64.SetSerialNumber(ref infos[0], "boobieshehe1");
        //      status = DS5W_x64.initDeviceContext(ref infos[0], ref deviceContext);
        //      if (status != DS5W_RetrunValue.OK) Debug.LogError(status.ToString());
    }

    // Update is called once per frame
    void Update()
    {


        if (connected)
        {
            dualSense.SetTriggerContinuousResistance(trigger, force, posistion);
            dualSense.SetMotorSpeeds(L_Rumble, H_Rumble);
            dualSense.SendCommand();












            // dualSense.SendCommand();
        }
    }
}
  

