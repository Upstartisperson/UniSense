using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniSense.NewConnections;
using UnityEngine.InputSystem;
using UniSense.LowLevel;
using UniSense;
using System;
using System.Runtime.InteropServices;

public class PairingTest : MonoBehaviour
{
    List<Controller> _devices = new();


    int framecount = 0;
    float frameAverage = 0;
    int frameMin = 0;
    int frameMax = 0;
    float fastAverage = 0;
    int fastMin = 0;
    int fastMax = 0;
    float persistantAverage = 0;
    int persistantMin = 0;
    int persistantMax = 0;

    float timeDeltaAverage = 0;
    float timeDeltaMax = 0;
    //double _persistantPerSecond = 0;
    //double _Elapsedpersistant = 0;
    //double _lastPersistant = -1;
    //double _framePerSecond = 0;
    //double _ElapsedFrame = 0;
    //double _lastFrame = -1;
    //double _fastPerSecond = 0;
    //double _ElapsedFast = 0;
    //double _lastFast = -1;
    //double _lastUpdateTime = -1;
    //double _ElapsedUpdate = 0;

    private const int _persistentPerSecond = 875; //Approximate amount the persistent counter will count per second
    private const int _fastPerSecond = 3000000; //Approximate amount the fast counter will count per second
    private const int _framePerSecond = 250; //Approximate amount the frame counter will count per second


    public static unsafe bool GetCounters(InputDevice device, out uint fastCounter, out uint persistantTimer, out uint frameCounter)
    {
        fastCounter = 0;
        persistantTimer = 0;
        frameCounter = 0;
        int reportSize;
        int offset = 0;

        switch (device) //I'm surprised this worked
        {

            case DualSenseBTGamepadHID:
                reportSize = sizeof(DualSenseBTHIDInputReport);
                offset = 1;
                break;
            case DualSenseUSBGamepadHID:
                reportSize = sizeof(DualSenseUSBHIDInputReport);
                break;

            default: return false;



        }
        byte* reportBuffer = stackalloc byte[reportSize];
        device.CopyState(reportBuffer, reportSize);
        fastCounter = BitConverter.ToUInt32(new Span<byte>(reportBuffer + offset + 49, 4));
        byte[] framebytes = new byte[4];
        framebytes[0] = reportBuffer[offset + 7];
        frameCounter = BitConverter.ToUInt32(framebytes);
        persistantTimer = BitConverter.ToUInt32(new Span<byte>(reportBuffer + offset + 12, 4));
        return true;
    }

    ref Controller[] _connectionControllers
    {
        get { return ref UniSenseConnectionHandler.Controllers; }
    }
    public void Awake()
    {
       
    }
    public void Start()
    {
        InputSystem.onAfterUpdate += updatething;
        UniSenseConnectionHandler.Initilize(new UniqueIdentifier(gameObject.gameObject, this));
        UniSenseConnectionHandler.OnControllerListUpdated += ListUpdated;
        for (int i = 0; i < _connectionControllers.Length ; i++)
        {
            if (_connectionControllers[i].ReadyToConnect) _devices.Add(_connectionControllers[i]);
        }
    }


    /// <summary>
    /// Return the delta between two uint taking into account wrap around and the length of the bytes
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="byteLength"></param>
    /// <returns></returns>
    public uint Delta(uint x, uint y, int byteLength)
    {

        uint max = (byteLength >= 4) ? uint.MaxValue : (uint)1 << (byteLength * 8);
        if(byteLength > 4)
        {
            Debug.LogError(byteLength + " Is outside of range");
            return 0;
        }
        uint diff = ((x > y) ? x - y : y - x);
        uint altDiff = (byteLength == 4) ? (max - diff) + 1: (max - diff);
        return ((diff > altDiff) ? altDiff : diff);
    }



    public void updatething()
    {
        if (_devices.Count < 2) return;
        framecount++;
        //  _connectionControllers[0].devices.InputDevice.
        double timeDelta = _connectionControllers[0].devices.InputDevice.lastUpdateTime - _connectionControllers[1].devices.InputDevice.lastUpdateTime;
        GetCounters(_connectionControllers[0].devices.InputDevice, out uint inputFastCounter, out uint inputPersistemtCounter, out uint inputFrameCounter);
        GetCounters(_connectionControllers[1].devices.InputDevice, out uint FastCounter, out uint PersistentCounter, out uint FrameCounter);
        //PersistentCounter = (uint)((double)PersistentCounter + (_persistentPerSecond * timeDelta));
        //FastCounter = (uint)((double)FastCounter + (_fastPerSecond * timeDelta));
        //FrameCounter = (uint)((double)FrameCounter + (_framePerSecond * timeDelta));
        int frameDelta = (int)Delta(inputFrameCounter, FrameCounter, 1);
        frameMin = (frameMin < frameDelta) ? frameMin : frameDelta;
        frameMax = (frameMax > frameDelta) ? frameMax : frameDelta;
        frameAverage = ((frameAverage * (framecount - 1)) + frameDelta) / framecount;
        Debug.Log("Frame Average: " + frameAverage.ToString());
        Debug.Log("Frame Min: " + frameMin.ToString());
        Debug.Log("Frame Max: " + frameMax.ToString());

        int fastDelta = (int)Delta(inputFastCounter, FastCounter, 4);
        fastMin = (fastMin < fastDelta) ? fastMin : fastDelta;
        fastMax = (fastMax > fastDelta) ? fastMax : fastDelta;
        fastAverage = ((fastAverage * (framecount - 1)) + fastDelta) / framecount;
        Debug.Log("Fast Average: " + fastAverage.ToString());
        Debug.Log("Fast Min: " + fastMin.ToString());
        Debug.Log("Fast Max: " + fastMax.ToString());

        int persistantDelta = (int)Delta(inputPersistemtCounter, PersistentCounter, 4);
        persistantMin = (persistantMin < persistantDelta) ? persistantMin : persistantDelta;
        persistantMax = (persistantMax > persistantDelta) ? persistantMax : persistantDelta;
        persistantAverage = ((persistantAverage * (framecount - 1)) + persistantDelta) / framecount;
        Debug.Log("Persistant Average: " + persistantAverage.ToString());
        Debug.Log("Persistant Min: " + persistantMin.ToString());
        Debug.Log("Persistant Max: " + persistantMax.ToString());

        float fastTimepassed = (float)fastDelta / (float)_fastPerSecond;
        float persistantTimepassed = (float)persistantDelta / (float)_persistentPerSecond;
        float timeDelta1 = Mathf.Abs(fastTimepassed - persistantTimepassed);

        timeDeltaAverage = ((timeDeltaAverage * (framecount - 1)) + (float)timeDelta1) / framecount;
        timeDeltaMax = (timeDeltaMax > (float)timeDelta1) ? timeDeltaMax : (float)timeDelta1;
        Debug.Log("TimeDelta Average: " + timeDeltaAverage.ToString());
        Debug.Log("TimeDelta Max: " + timeDeltaMax.ToString());
    }

    public void Update()
    {

        //if (_devices.Count > 0)
        //{
        //    var device = _devices[0].devices.InputDevice;
        //    if (!device.wasUpdatedThisFrame) return;
        //    GetCounters(device, out uint checkFastCounter, out uint checkPersistantCounter, out uint checkFrameCounter);


        //    double updatediff = (_lastUpdateTime == -1) ? 0 : device.lastUpdateTime - _lastUpdateTime;
        //    _ElapsedUpdate += updatediff;
        //    _lastUpdateTime = device.lastUpdateTime;
        //    double persistantDiff =(_lastPersistant == -1) ? 0 : Delta((uint)_lastPersistant, checkPersistantCounter, 4);
        //    _Elapsedpersistant += persistantDiff;
        //    _lastPersistant = checkPersistantCounter;
        //    _persistantPerSecond = persistantDiff / updatediff;
        //    double fastDiff = (_lastFast == -1) ? 0 : Delta(checkFastCounter, (uint)_lastFast, 4);
        //    _ElapsedFast += fastDiff;
        //    _lastFast = checkFastCounter;
        //    _fastPerSecond = fastDiff / updatediff;
        //    double frameDiff = (_lastFrame == -1) ? 0 : Delta(checkFrameCounter, (uint)_lastFrame, 1);
        //    _ElapsedFrame += frameDiff;
        //    _lastFrame = checkFrameCounter;
        //    _framePerSecond = frameDiff / updatediff;

        //    Debug.Log("Persistant per second: " + _persistantPerSecond);
        //    Debug.Log("Persistant Per second Average: " + _Elapsedpersistant / _ElapsedUpdate);
        //    Debug.Log("Fast per second: " + _fastPerSecond);
        //    Debug.Log("Fast Per second Average: " + _ElapsedFast / _ElapsedUpdate);
        //    Debug.Log("Frame per second: " + _framePerSecond);
        //    Debug.Log("Frame Per second Average: " + _ElapsedFrame / _ElapsedUpdate);

        //}

        
        
        //fastAverage = ((fastAverage * (framecount - 1)) + Delta(inputFastCounter, FastCounter)) / framecount;
        //persistantAverage = ((persistantAverage * (framecount - 1)) + Delta(inputPersistantCounter, PersistantCounter)) / framecount;

        //Debug.Log("Fast Average: " + fastAverage.ToString());
        //Debug.Log("Persistent Average: " + persistantAverage.ToString());

    }


    public void ListUpdated(Controller[] controllers)
    {
        _devices.Clear();
        for (int i = 0; i < _connectionControllers.Length; i++)
        {
            if (_connectionControllers[i].ReadyToConnect) _devices.Add(_connectionControllers[i]);
        }
    }
    public void OnDisable()
    {
        InputSystem.onAfterUpdate -= updatething;
        UniSenseConnectionHandler.Destroy(new UniqueIdentifier(gameObject, this));
        UniSenseConnectionHandler.OnControllerListUpdated -= ListUpdated;
    }
}
