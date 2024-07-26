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

	
	
	public interface IConnectionListener
	{
		public void OnUserAdded(int unisenseId);
		public void OnUserRemoved(int unisenseId);
		public void OnUserModified(int unisenseId, UserChange change);
		
		public void InitilizeUsers();
	
		public void OnCurrentUserModified();
	
	}
	
	public interface IManage
	{
		public void OnUserAdded(int unisenseId);
		public void OnUserRemoved(int unisenseId);
		public void OnUserModified(int unisenseId, UserChange change);
		public void InitilizeUsers();
	}
	
	public interface IHandleSingleplayer
	{
		
		public void OnCurrentUserModified(UserChange change);
		public bool SetCurrentUser(int uniSenseId);
		public void SetNoCurrentUser();
	
	}

}
    