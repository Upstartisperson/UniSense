using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniSense.DevConnections;
using UnityEngine;
using UniSense.LowLevel;
using UnityEngine.InputSystem;
using DeviceType = UniSense.DevConnections.DeviceType;

namespace UniSense.pair
{

	public static class UnisensePair
	{
		private const int _persistentPerSecond = 875; //Approximate amount the persistent counter will count per second
		private const int _fastPerSecond = 3000000; //Approximate amount the fast counter will count per second
		private const int _framePerSecond = 250; //Approximate amount the frame counter will count per second
		private const int _frameEpsilon = 6;
		private const int _fastEpsilon = 60000;
		private const int _persistantEpsilon = 20;
		private const decimal _timeEpsilon = 0.03m;
		private static UniSenseUser[] _uniSenseUsers { get { return NewUniSenseConnectionHandler.UnisenseUsers; } }
		private static List<int> unpairedBtUsers = new();

		private static unsafe bool GetCounters(InputDevice device, out uint fastCounter, out uint persistentCounter, out uint frameCounter)
		{
			fastCounter = 0;
			persistentCounter = 0;
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
			frameCounter = reportBuffer[offset + 7];

			//BitConverter.ToUInt32(new Span<byte>(reportBuffer + offset + 7, 1));
			persistentCounter = BitConverter.ToUInt32(new Span<byte>(reportBuffer + offset + 12, 4));
			return true;
		}

		private static uint UintDelta(uint x, uint y, int byteLength)
		{

			uint max = (byteLength >= 4) ? uint.MaxValue : (uint)1 << (byteLength * 8);
			if (byteLength > 4)
			{
				Debug.LogError(byteLength + " Is outside of range");
				return 0;
			}
			uint diff = (x > y) ? x - y : y - x;
			uint altDiff = (byteLength == 4) ? (max - diff) + 1 : (max - diff); //adding one here is safe in the event diff is zero adding one to uint.MaxValue will wrap result back to zero.
			return (diff > altDiff) ? altDiff : diff; //altDiff and diff can both be zero, but in that case zero will still be returned so no issue there;
		}

		public static bool TryPair(DualSenseUSBGamepadHID device)
		{
			//List<DualSenseUSBGamepadHID> devicesToPair = _devicesToPair.ToList(); //Just use list from connection manage4r
			//Queue<int> stillLookingForPair = new Queue<int>();
			for (int i = 0; i < unpairedBtUsers.Count(); i++)
			{
				int unisenseID = unpairedBtUsers[i];
				bool pairingSuccsesful = false;
				//if (!_unisenseUsers[unisenseId].BTAttached) continue; redundant probably
				DualSenseBTGamepadHID btDevice = _uniSenseUsers[unisenseID].Devices.DualsenseBT;
				Debug.Log("Batt: " + btDevice.batteryLevel.ReadValue());
				if (btDevice.usbConnected.isPressed)
				{
					if (!GetCounters(btDevice, out uint inputFastCounter, out uint inputPersistentCounter, out uint inputFrameCounter))
					{
						Debug.LogError("Error retrieving counters");
						continue;
					}


					if (Math.Abs(btDevice.batteryLevel.ReadValue() - device.batteryLevel.ReadValue()) > 1) continue;

					if (!GetCounters(device, out uint fastCounter, out uint persistentCounter, out uint frameCounter))
					{
						Debug.LogError("Error retrieving counters");
						continue;
					}

					uint fastDelta = UintDelta(inputFastCounter, fastCounter, 4);
					uint persistentDelta = UintDelta(inputPersistentCounter, persistentCounter, 4);
					uint frameDelta = UintDelta(inputFrameCounter, frameCounter, 1);

					if (fastDelta <= _fastEpsilon && persistentDelta <= _persistantEpsilon && frameDelta <= _frameEpsilon)
					{
						//Could multiply by the inverse to speed up but not worth readability cost
						decimal fastTimepassed = (decimal)fastDelta / (decimal)_fastPerSecond;
						decimal persistantTimepassed = (decimal)persistentDelta / (decimal)_persistentPerSecond;

						decimal timeDelta = Math.Abs(fastTimepassed - persistantTimepassed);
						if (timeDelta > _timeEpsilon) continue;


						//Successfully matched the USB device to it's BT counterpart
						_uniSenseUsers[unisenseID].AddDevice(device, DeviceType.DualSenseUSB, unisenseID);

						//_handleMultiplayer.OnUserModified(unisenseID, UserChange.USBAdded); //Removed since all it does is change the active device to USB in this situation. Since the BT device will continue to work while USB is connected it only needs to be swapped when and if the BT device is disconnected which is handled elsewhere  

						pairingSuccsesful = true;
						unpairedBtUsers.RemoveAt(i);
						return true;
					}

				}

			}
			return false;


		}

		public static void ReadyForPair(int unisenseID) => unpairedBtUsers.Add(unisenseID); //TODO: Need to figure out when to actually call this method. should I do it on unisense user class? should I do it in the conection handeler instead?
		public static void NotLookingForPair(int unisenseID) => unpairedBtUsers.Remove(unisenseID);
	}
}
