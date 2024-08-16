using UnityEngine.InputSystem;
using System;
using System.Runtime.InteropServices;
using UniSense.Management;
using UniSense.Users;
using UnityEditor;
using UniSense.pair;
using UnityEngine;
using DeviceType = UniSense.Utilities.DeviceType;

namespace UniSense.Connections
{
	public static class UniSenseConnectionHandler
	{
        #region Fields
        public static bool Initialized { get; private set; } //Has the UniSenseConnectionHandler been initialized
		private static bool _isMultiplayer; //Is the current game multiplayer
		private static bool _allowGenericGamepad; //Are non-DualSense controllers able to connect;
		private static bool _allowKeyboardMouse; //Are mouse and keyboard players allowed to connect
		private static int _currentUnsId = -1; //UniSense Id of the current user (for single-player mode only)
		private static IManageMultiPlayer _handleMultiplayer; //Reference to the multiplayer manager that initialized multiplayer
		private static IHandleSingleplayer _handleSingleplayer; //Reference to the single player handler that initialized single player
		private static PairQueue pairQueue = new PairQueue(); //Pairing queue used to queue DualSense USB devices for BT counterpart pairing
		private static UniSenseUser[] _users { get { return UniSenseUser.Users; } }
		public static UniSenseUser CurrentUser { get { return UniSenseUser.Users[_currentUnsId]; } }

        #endregion

        #region Class Initialization & Destruction
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
			Initialized = false;

			EnsureClosure();
			InputSystem.onDeviceChange += OnDeviceChange;
		}

		/// <summary>
		/// Ensures that this class shuts down properly
		/// </summary>
		private static void EnsureClosure()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			EditorApplication.wantsToQuit += Destroy;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
		{
			if (stateChange == PlayModeStateChange.ExitingPlayMode) Destroy();
		}

		/// <summary>
		/// Terminates all hooked events, Closes all open connections. This method is important since if connected bluetooth DualSense devices aren't shut down properly memory leaks and unpredictable system behavior is possible
		/// </summary>
		/// <returns></returns>
		private static bool Destroy()
		{
			UnisensePair.OnPairFailed -= UsbPairFailed;
			UnisensePair.OnUsbPaired -= OnUsbPaird;
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.wantsToQuit -= Destroy;
			InputSystem.onDeviceChange -= OnDeviceChange;
			pairQueue.Exit();

			for (int i = 0; i < _users.Length; i++)
			{
				_users[i].ClearUser(true);
			}
			return true; //when called by EditorApplication.wantsToQuit returning true allows the application to exit
		}

		#endregion


		/// <summary>
		/// Initialize UniSense in single-player mode. Example Call From  Script Inheriting <see cref="IHandleSingleplayer"/>:
		/// <code>
		/// bool initSucsesful = InitializeSingleplayer(this, true, false);
		/// </code>
		/// </summary>
		/// <param name="singleplayerHandler">Reference to calling class "this" is proper usage </param>
		/// <param name="allowKeyboardMouse">Should mouse and keyboard players be allowed to join</param>
		/// <param name="allowGenergicGamepad">Should players with non DualSense controllers be allowed to join</param>
		/// <returns>True if successful</returns>
		public static bool InitializeSingleplayer(IHandleSingleplayer singleplayerHandler, bool allowKeyboardMouse = false, bool allowGenergicGamepad = false)
		{
			if (Initialized || singleplayerHandler == null) return false;
			Initialized = true;
			_handleSingleplayer = singleplayerHandler;
			FinishInitialization(false, allowKeyboardMouse, allowGenergicGamepad);
			if (_currentUnsId != -1)
			{
				_handleSingleplayer.SetCurrentUser(_currentUnsId);
				return true;
			}
			return false;
		}


		/// <summary>
		/// Initialize UniSense in multi-player mode. Example Call From  Script Inheriting <see cref="IManageMultiPlayer"/>:
		/// <code>
		/// bool initSucsesful = InitializeMultiplayer(this, true, false);
		/// </code>
		/// </summary>
		/// <param name="multiplayerListener">Reference to calling class "this" is proper usage </param>
		/// <param name="allowKeyboardMouse">Should mouse and keyboard players be allowed to join</param>
		/// <param name="allowGenergicGamepad">Should players with non DualSense controllers be allowed to join</param>
		/// <returns>True if successful</returns>
		public static bool InitializeMultiplayer(IManageMultiPlayer multiplayerListener, bool allowKeyboardMouse = false, bool allowGenergicGamepad = false)
		{
			if (Initialized || multiplayerListener == null) return false;
			Initialized = true;
			_handleMultiplayer = multiplayerListener;
			FinishInitialization(true, allowKeyboardMouse, allowGenergicGamepad);
			multiplayerListener.InitilizeUsers();
			return false;
		}

		/// <summary>
		/// Does all initialization steps common to multiplayer and single player modes
		/// </summary>
		/// <param name="isMultiplayer"></param>
		/// <param name="allowKeyboardMouse"></param>
		/// <param name="allowGenergicGamepad"></param>
		private static void FinishInitialization(bool isMultiplayer, bool allowKeyboardMouse, bool allowGenergicGamepad)
		{
			UnisensePair.OnPairFailed += UsbPairFailed;
			UnisensePair.OnUsbPaired += OnUsbPaird;
		
			_isMultiplayer = isMultiplayer;
			_allowGenericGamepad = allowGenergicGamepad;
			_allowKeyboardMouse = allowKeyboardMouse;

			Gamepad[] gamepads = Gamepad.all.ToArray(); //Find all gamepads connected to Unity's input system

			if (gamepads == null || gamepads.Length == 0) return; //Check if at least one gamepad is connected

		    //Set up connected gamepads
			foreach (Gamepad gamepad in gamepads)
            {
                switch (gamepad)
                {
					case DualSenseBTGamepadHID:
						UniSenseUser.InitUser(gamepad, DeviceType.DualSenseBT);
						InputSystem.EnableDevice(gamepad);
						break;
					case DualSenseUSBGamepadHID:
						pairQueue.QueueDevice(gamepad as DualSenseUSBGamepadHID);
						break;
					default:
						if (allowGenergicGamepad) UniSenseUser.InitUser(gamepad, DeviceType.GenericGamepad);
						break;
                }
            }

			if (!_isMultiplayer && FindNewCurrentUser(out int Id)) _currentUnsId = Id; //Set current user for single player mode
		}

		/// <summary>
		/// Selects a current user out of all active UniSense users
		/// </summary>
		/// <param name="unisenseId">The UniSenseId of the new current user, -1 if no user is found</param>
		/// <returns>True if a new current user was selected</returns>
		private static bool FindNewCurrentUser(out int unisenseId)
		{
			for (unisenseId = 0; unisenseId < _users.Length; unisenseId++)
			{
				if (_users[unisenseId].IsReadyToConnect) return true;
			}
			unisenseId = -1;
			return false;
		}

		/// <summary>
		/// Method called when a USB DualSense was successfully paired to its BT counterpart
		/// </summary>
		/// <param name="unisenseId">UniSense Id of the USB DualSense that got paired</param>
		private static void OnUsbPaird(int unisenseId) 
		{
			if (_isMultiplayer)
			{
				_handleMultiplayer.OnUserModified(unisenseId, UserChange.USBAdded);
			}
			else
			{
				if (_currentUnsId == unisenseId) _handleSingleplayer.OnCurrentUserModified(UserChange.USBAdded);
				else
				{
					_currentUnsId = unisenseId;
					_handleSingleplayer.SetCurrentUser(unisenseId);
				}

			}
		}

		/// <summary>
		/// Method called when USB DaulSense failed to find a BT counterpart
		/// </summary>
		/// <param name="device">USB DualSense that failed to find BT counterpart</param>
		private static void UsbPairFailed(DualSenseUSBGamepadHID device) 
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
				_currentUnsId = id;
				_handleSingleplayer.SetCurrentUser(id);
			}
		}

		/// <summary>
		/// Method that deals with the connection and disconnection of all unity input devices
		/// </summary>
		/// <param name="device"></param>
		/// <param name="change"></param>
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
								_currentUnsId = unisenseId;
								_handleSingleplayer.SetCurrentUser(unisenseId);
							}
							break;

						case DualSenseUSBGamepadHID:
							pairQueue.QueueDevice(device as DualSenseUSBGamepadHID);
							break;

						case Gamepad:
							if (!_allowGenericGamepad) return;
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
								_currentUnsId = unisenseId;
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
							if (unisenseId == _currentUnsId)
							{
								if (!FindNewCurrentUser(out int newId))
								{
									_handleSingleplayer.SetNoCurrentUser();
								}
								else
								{
									_currentUnsId = newId;
									_handleSingleplayer.SetCurrentUser(newId);
								}
							}
						}
						 _users[unisenseId].ClearUser(false);
						if (_allowKeyboardMouse && !_isMultiplayer) _handleSingleplayer.SetMouseKeyboard();
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