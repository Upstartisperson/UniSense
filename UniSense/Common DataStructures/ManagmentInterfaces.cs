using System;
using UnityEngine;

/// <summary>
/// File containing data structures and interfaces necessary for the management of UniSense users
/// </summary>
namespace UniSense.Management
{
	public enum UserChange
	{
		BTAdded,
		BTRemoved,
		BTDisabled,
		USBAdded,
		USBRemoved,
		GenericAdded,
		GenericRemoved,
	}
	
	/// <summary>
	/// Interface requiring certain methods to be present if a script wishes to manage single player handlers
	/// </summary>
	public interface IManageMultiPlayer
	{
		public void OnUserAdded(int unisenseId);
		public void OnUserRemoved(int unisenseId);
		public void OnUserModified(int unisenseId, UserChange change);
		public void InitilizeUsers();
	}
	
	/// <summary>
	/// Interface requiring certain methods to be present if a script wishes to be a single player DualSense handler.
	/// </summary>
	public interface IHandleSingleplayer
	{
		public void OnCurrentUserModified(UserChange change);
		public bool SetCurrentUser(int uniSenseId);
		public void SetPlayerNumber(int num);
		public void SetNoCurrentUser();
		public void SetMouseKeyboard();

		public Camera PlayerCamera;
	
	}

}
    