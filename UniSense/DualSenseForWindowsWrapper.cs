using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace DS5W
{
    /// <summary>
    /// Class that directly references the 64-bit version of the C++ DLL 
    /// </summary>
    public static class DS5W_x64
    {
#if UNITY_EDITOR
        public const string _DLLpath = "ds5w_x64.dll";
        #endif
        [DllImport(_DLLpath, CallingConvention = CallingConvention.Cdecl)]
        public static extern DS5W_ReturnValue enumDevices(ref IntPtr ptrBuffer, uint inArrayLength, ref uint ptrLegnth, bool pointerToArray = true);

        [DllImport(_DLLpath, CallingConvention = CallingConvention.Cdecl)]
        public static extern DS5W_ReturnValue initDeviceContext(ref DeviceEnumInfo ptrEnumInfo, ref DeviceContext ptrContext);

        [DllImport(_DLLpath, CallingConvention = CallingConvention.Cdecl)]
        public static extern void freeDeviceContext(ref DeviceContext ptrContext, bool clearOutput);

        [DllImport(_DLLpath, CallingConvention = CallingConvention.Cdecl)]
        public static extern DS5W_ReturnValue reconnectDevice(ref DeviceContext ptrContext);

        [DllImport(_DLLpath, CallingConvention = CallingConvention.Cdecl)]
        public static extern DS5W_ReturnValue setDeviceRawOutputState(ref DeviceContext ptrContext, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] ptrOutputByteMap, int size);

        [DllImport(_DLLpath, CallingConvention = CallingConvention.Cdecl,CharSet = CharSet.Unicode)]
        public static extern DS5W_ReturnValue findDevice(ref DeviceEnumInfo ptrEnumInfo, string serialNumber);

        [DllImport(_DLLpath, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)] //Try charset here I did it wron and never tested it
        public static extern DS5W_ReturnValue ReturnSerial(ref IntPtr ptrBuffer, string serialNumber);
         
    }

    /// <summary>
    /// Class that directly references the 32-bit version of the C++ DLL
    /// </summary>
    public static class DS5W_x86
    {
        public const string _DLLpath = "C:\\Users\\thom3\\source\\repos\\DualSense-WindowsDLLBuild\\VS19_Solution\\bin\\DualSenseWindows\\DebugDll-x86\\ds5w_x86.dll";
        [DllImport(_DLLpath, CallingConvention = CallingConvention.Cdecl)]
        public static extern DS5W_ReturnValue enumDevices(ref IntPtr ptrBuffer, uint inArrayLength, ref uint ptrLegnth, bool pointerToArray = true);

        [DllImport(_DLLpath, CallingConvention = CallingConvention.Cdecl)]
        public static extern DS5W_ReturnValue initDeviceContext(ref DeviceEnumInfo ptrEnumInfo, ref DeviceContext ptrContext);

        [DllImport(_DLLpath, CallingConvention = CallingConvention.Cdecl)]
        public static extern void freeDeviceContext(ref DeviceContext ptrContext, bool clearOutput);

        [DllImport(_DLLpath, CallingConvention = CallingConvention.Cdecl)]
        public static extern DS5W_ReturnValue reconnectDevice(ref DeviceContext ptrContext);

        [DllImport(_DLLpath, CallingConvention = CallingConvention.Cdecl)]
        public static extern DS5W_ReturnValue setDeviceRawOutputState(ref DeviceContext ptrContext, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] ptrOutputByteMap, int size);
        
        [DllImport(_DLLpath, CallingConvention = CallingConvention.Cdecl)]
        public static extern DS5W_ReturnValue findDevice(ref DeviceEnumInfo ptrEnumInfo, string serialNumber);

        [DllImport(_DLLpath, CallingConvention = CallingConvention.Cdecl)]
        public static extern DS5W_ReturnValue ReturnSerial(ref DeviceEnumInfo ptrEnumInfo, ref SerialNumber serialNumber);
    }
    public class DS5WHelpers
    {
        /// <summary>
        /// Constructs a pointer out of an array of DeviceEnumInfo
        /// </summary>
        /// <param name="ptrBuffer"></param>
        /// <param name="infos"></param>
         public static void BuildEnumDeviceBuffer(ref IntPtr ptrBuffer, DeviceEnumInfo[] infos)
        {
            ptrBuffer =  Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DeviceEnumInfo)) * infos.Length);
            for (int i = 0; i < infos.Length; i++)
            {
                Marshal.StructureToPtr(infos[i], ptrBuffer + (i * Marshal.SizeOf(typeof(DeviceEnumInfo))), false);  
            }
        }
        /// <summary>
        /// Converts a pointer into an array of DeviceEnumInfo; Method Frees Pointer Aromatically
        /// </summary>
        /// <param name="ptrBuffer"></param>
        /// <param name="infos"></param>
        public static void DeconstructEnumDeviceBuffer(ref IntPtr ptrBuffer, ref DeviceEnumInfo[] infos)
        {
            for (int i = 0; i < infos.Length; i++)
            {
                IntPtr infoPtr = ptrBuffer + i * Marshal.SizeOf<DeviceEnumInfo>();
                infos[i] = Marshal.PtrToStructure<DeviceEnumInfo>(infoPtr);
            }
            if (ptrBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptrBuffer);
            }
        }


    }




    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SerialNumber
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct InternalData
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string serialNumber;
        }
        public InternalData _internal;
    }

    /// <summary>
    /// Struct for the status of a device or related functions
    /// </summary>
    public enum DS5W_ReturnValue : uint
    {
        OK = 0,
        /// <summary>
        /// Operation encountered an unknown error
        /// </summary>
        E_UNKNOWN = 1,

        /// <summary>
        /// The user supplied buffer is to small
        /// </summary>
        E_INSUFFICIENT_BUFFER = 2,

        /// <summary>
        /// External unexpected winapi error (please report as issue if you get this error!)
        /// </summary>
        E_EXTERNAL_WINAPI = 3,

        /// <summary>
        /// Not enought memroy on the stack
        /// </summary>
        E_STACK_OVERFLOW = 4,

        /// <summary>
        /// Invalid arguments
        /// </summary>
        E_INVALID_ARGS = 5,

        /// <summary>
        /// This feature is currently not supported
        /// </summary>
        E_CURRENTLY_NOT_SUPPORTED = 6,

        /// <summary>
        /// Device was disconnected
        /// </summary>
        E_DEVICE_REMOVED = 7,

        /// <summary>
        /// Bluetooth communication error
        /// </summary>
        E_BT_COM = 8,


    }

    /// <summary>
    /// Struct for storing a devices connection type
    /// </summary>
    public enum DeviceConnection : byte
    {
        /// <summary>
        /// Controller is connected via USB.
        /// </summary>
        USB = 0,

        /// <summary>
        /// Controller is connected via Bluetooth.
        /// </summary>
        BT = 1,
    }

    /// <summary>
    /// Struct for storing device enum info during device discovery.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DeviceEnumInfo
    {
        /// <summary>
        /// Encapsulate data in struct to (at least try) prevent user from modifying the context.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct InternalData
        {
            /// <summary>
            /// Path to the discovered device.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string path;

            /// <summary>
            /// Serial number of the discovered device.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string serialNumber;


            /// <summary>
            /// Connection type of the discovered device.
            /// </summary>
            public DeviceConnection Connection;
        }

        public InternalData _internal;
    }
    /// <summary>
    /// Struct for storing device info
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DeviceContext
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct InternalData
        {
            
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string devicePath;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string SerialNumber;

            public IntPtr deviceHandle;


            public DeviceConnection connection;


            [MarshalAs(UnmanagedType.U1)]
            public bool connected;


            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 547)]
            public byte[] hidBuffer;

        }
        private InternalData _internal;

    }

}