using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UniSense.LowLevel;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.LowLevel;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using WrapperDS5W;
using UnityEngine.InputSystem.DualSense;
using UnityEngine.Scripting;
using System.Text;
using UniSense.Management;
using UniSense.Users;
using UnityEditor;
using UniSense;
using UniSense.pair;
using UnityEngine;
using DeviceType = UniSense.Utilities.DeviceType;

namespace UniSense.Connections
{
	public static class UniSenseConnectionHandler
	{
		public static bool IsInitialized { get; private set; }
		private static bool _isMultiplayer;
		private static bool _allowGenericGamepad;
		private static bool _allowKeyboardMouse;
		private const int _maxPlayersDefualt = 16;
		private static int _currentUserIndex = -1;
		private static IManage _handleMultiplayer;
		private static IHandleSingleplayer _handleSingleplayer;
		private static PairQueue pairQueue = new PairQueue();
		private static ref UniSenseUser[] _users { get { return ref UniSenseUser.Users; } }

		public static ref UniSenseUser CurrentUser
		{
			get { return ref UniSenseUser.Users[_currentUserIndex]; }
		}

		//This will automatically be run immediately before this class is accessed for the first time.
		static UniSenseConnectionHandler()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
#if UNITY_EDITOR
				EditorApplication.Exit(0);
				throw new Exception("Current Platform Not Supported");
#else
			Application.Quit();
			throw new Exception("Current Platform Not Supported");
#endif
			}
			IsInitialized = false;

			EnsureClosure();
			InputSystem.onDeviceChange += OnDeviceChange;
		}

		private static void EnsureClosure()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			EditorApplication.wantsToQuit += Destroy;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
		{
			if (stateChange == PlayModeStateChange.ExitingPlayMode) Destroy();
		}
		private static bool Destroy()
		{
			UnisensePair.OnPairFailed -= UsbPairFailed;
			UnisensePair.OnUsbPaired -= OnUsbPaird;
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.wantsToQuit -= Destroy;
			InputSystem.onDeviceChange -= OnDeviceChange;
			pairQueue.Exit();

			//InputSystem.onAfterUpdate -= InputSystemUpdate;

			//if (_deviceMatchQueued) InputSystem.onAfterUpdate -= MatchDevicesOnNextUpdate;
			for (int i = 0; i < _users.Length; i++)
			{
				_users[i].ClearUser(true);
			}
			return true; //when called by EditorApplication.wantsToQuit returning true allows the application to exit
		}

		/// <summary>
		/// Initialize unisense in single player mode
		/// </summary>
		/// <param name="singleplayerListener"></param>
		/// <param name="allowKeyboardMouse"></param>
		/// <param name="allowGenergicGamepad"></param>
		/// <returns>True if successful</returns>
		public static bool InitializeSingleplayer(IHandleSingleplayer singleplayerListener, bool allowKeyboardMouse = false, bool allowGenergicGamepad = false)
		{
			if (IsInitialized || singleplayerListener == null) return false;
			IsInitialized = true;
			_handleSingleplayer = singleplayerListener;
			FinishInitialization(false, allowKeyboardMouse, allowGenergicGamepad, _maxPlayersDefualt);
			if (_currentUserIndex != -1)
			{
				_handleSingleplayer.SetCurrentUser(_currentUserIndex);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Initialize unisense in multi player mode
		/// </summary>
		/// <param name="maxPlayers"></param>
		/// <param name="multiplayerListener"></param>
		/// <param name="allowKeyboardMouse"></param>
		/// <param name="allowGenergicGamepad"></param>
		/// <returns>True if successful</returns>
		public static bool InitializeMultiplayer(IManage multiplayerListener, bool allowKeyboardMouse = false, bool allowGenergicGamepad = false, int maxPlayers = _maxPlayersDefualt)
		{
			if (IsInitialized || multiplayerListener == null) return false;
			IsInitialized = true;
			_handleMultiplayer = multiplayerListener;
			FinishInitialization(true, allowKeyboardMouse, allowGenergicGamepad, maxPlayers);
			multiplayerListener.InitilizeUsers();
			return false;
		}

		private static void FinishInitialization(bool isMultiplayer, bool allowKeyboardMouse, bool allowGenergicGamepad, int maxPlayers)
		{
			UnisensePair.OnPairFailed += UsbPairFailed;
			UnisensePair.OnUsbPaired += OnUsbPaird;
			UniSenseUser.init();
			_isMultiplayer = isMultiplayer;
			_allowGenericGamepad = allowGenergicGamepad;
			_allowKeyboardMouse = allowKeyboardMouse;

			//TODO: Could Combine all three foreach loops into one would be easy because Gamepad.all contains usb and bt dualsense gamepads
			//now a generic gamepad has the potentail to become the current user just need to modify that

			Gamepad[] btGamepads = DualSenseBTGamepadHID.FindAll();
			Gamepad[] usbGamepads = DualSenseUSBGamepadHID.FindAll();
			Gamepad[] genericGamepads = Gamepad.all.ToArray();

			if (btGamepads != null && btGamepads.Length > 0)
			{
				foreach (Gamepad gamepad in btGamepads)
				{
					UniSenseUser.InitUser(gamepad, DeviceType.DualSenseBT);
				}
			}

			if (usbGamepads != null && usbGamepads.Length > 0)
			{
				foreach (Gamepad gamepad in usbGamepads)
				{
					pairQueue.QueueDevice(gamepad as DualSenseUSBGamepadHID);
				}
			}

			if (allowGenergicGamepad && genericGamepads != null && genericGamepads.Length > 0)
			{
				foreach (Gamepad gamepad in genericGamepads)
				{
					if (gamepad is DualSenseUSBGamepadHID) continue;
					if (gamepad is DualSenseBTGamepadHID) continue;
					UniSenseUser.InitUser(gamepad, DeviceType.GenericGamepad);
				}
			}


			if (btGamepads != null && btGamepads.Length > 0)
			{
				foreach (Gamepad gamepad in btGamepads)
				{
					InputSystem.EnableDevice(gamepad);
				}
			}

			if (FindNewCurrentUser(out int Id)) _currentUserIndex = Id;

		}

		private static bool FindNewCurrentUser(out int unisenseId)
		{
			for (unisenseId = 0; unisenseId < _users.Length; unisenseId++)
			{
				if (_users[unisenseId].IsReadyToConnect) return true;
			}
			return false;
		}

		private static void OnUsbPaird(int unisenseId) //TODO : Debug
		{
			if (_isMultiplayer)
			{
				_handleMultiplayer.OnUserModified(unisenseId, UserChange.USBAdded);
			}
			else
			{
				if (_currentUserIndex == unisenseId) _handleSingleplayer.OnCurrentUserModified(UserChange.USBAdded);
				else
				{
					_currentUserIndex = unisenseId;
					_handleSingleplayer.SetCurrentUser(unisenseId);
				}

			}
		}
		private static void UsbPairFailed(DualSenseUSBGamepadHID device) //TODO : Debug
		{
			if (!UniSenseUser.InitUser(device, DeviceType.DualSenseUSB, out int id))
			{
				Debug.LogError("failed to initialize USB user");
				return;
			}
			if (_isMultiplayer)
			{
				_handleMultiplayer.OnUserAdded(id);
			}
			else
			{
				_currentUserIndex = id;
				_handleSingleplayer.SetCurrentUser(id);
			}


		}

		private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
		{
			int unisenseId = -1;
			string key = string.Empty;
			DeviceType deviceType = DeviceType.GenericGamepad;
			UserChange userChange;

			switch (device)
			{
				case DualSenseBTGamepadHID:
					deviceType = DeviceType.DualSenseBT;
					break;
				case DualSenseUSBGamepadHID:
					deviceType = DeviceType.DualSenseUSB;
					break;
			}

			switch (change)
			{
				#region Added
				case InputDeviceChange.Added:
					switch (device)
					{

						case DualSenseBTGamepadHID:
							InputSystem.EnableDevice(device);
							if (!UniSenseUser.InitUser(device, DeviceType.DualSenseBT, out unisenseId))
							{
								Debug.LogError("failed to initialize Bt user");
								return;
							}
							if (_isMultiplayer)
							{
								_handleMultiplayer.OnUserAdded(unisenseId);
							}
							else
							{
								_currentUserIndex = unisenseId;
								_handleSingleplayer.SetCurrentUser(unisenseId);
							}
							break;

						case DualSenseUSBGamepadHID:
							pairQueue.QueueDevice(device as DualSenseUSBGamepadHID);
							break;

						case Gamepad:
							if (!UniSenseUser.InitUser(device, DeviceType.GenericGamepad, out unisenseId))
							{
								Debug.LogError("failed to initialize Generic user");
								return;
							}
							if (_isMultiplayer)
							{
								_handleMultiplayer.OnUserAdded(unisenseId);
							}
							else
							{
								_currentUserIndex = unisenseId;
								_handleSingleplayer.SetCurrentUser(unisenseId);
							}
							break;

					}
					break;
				#endregion

				#region Removed
				case InputDeviceChange.Removed:
					switch (device)
					{
						case DualSenseBTGamepadHID:
							key = device.description.serial;
							userChange = UserChange.BTRemoved;
							break;
						case DualSenseUSBGamepadHID:
							key = device.deviceId.ToString();
							userChange = UserChange.USBRemoved;
							break;
						default:
							key = device.deviceId.ToString();
							userChange = UserChange.GenericRemoved;
							break;
					}

					if (!UniSenseUser.Find(key, out unisenseId))
					{
						if (deviceType == DeviceType.DualSenseUSB)
						{
							if (pairQueue.TryRemoveQueue(device as DualSenseUSBGamepadHID))
							{
								return;
							}
						}
						Debug.LogError("No UnisenseId found");
						return;
					}
					_users[unisenseId].RemoveDevice(deviceType);

					if (!_users[unisenseId].IsReadyToConnect)
					{
						if (_isMultiplayer)
						{
							_handleMultiplayer.OnUserRemoved(unisenseId);
						}
						else
						{
							if (unisenseId == _currentUserIndex)
							{
								if (!FindNewCurrentUser(out int newId))
								{
									_handleSingleplayer.SetNoCurrentUser();
								}
								else
								{
									_currentUserIndex = newId;
									_handleSingleplayer.SetCurrentUser(newId);
								}
							}
						}
						_users[unisenseId].ClearUser(false);
					}
					else
					{
						if (_isMultiplayer)
						{
							_handleMultiplayer.OnUserModified(unisenseId, userChange);
						}
						else
						{
							_handleSingleplayer.OnCurrentUserModified(userChange);
						}
					}

					break;
					#endregion
			}
		}
	}
}