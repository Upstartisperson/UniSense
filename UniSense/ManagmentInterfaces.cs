using System;
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

	public interface IManageable
	{
		public void SetCurrentUser_M(int unisenseId);
	}
	
	public interface IConnectionListener
	{
		public void OnUserAdded(int unisenseId);
		public void OnUserRemoved(int unisenseId);
		public void OnUserModified(int unisenseId, UserChange change);
		
		public void InitilizeUsers();
	
		public void OnCurrentUserModified();
	
	}
	
	public interface IHandleMultiplayer
	{
		public void OnUserAdded(int unisenseId);
		public void OnUserRemoved(int unisenseId);
		public void OnUserModified(int unisenseId, UserChange change);
		[Obsolete("Don't have a need for it")]
		public void InitilizeUsers();
	}
	
	public interface IHandleSingleplayer
	{
		
		public void OnCurrentUserModified(UserChange change);
		public void SetCurrentUser_S(int uniSenseId);
		public void SetNoCurrentUser();
	
	}

}
    