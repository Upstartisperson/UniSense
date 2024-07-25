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
		public void SetCurrentUser(int unisenseId);
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
		public void InitilizeUsers();
	}
	
	public interface IHandleSingleplayer
	{
		public void InitilizeUsers(int unisenseId);
		public void OnCurrentUserModified(UserChange change);
		public void OnCurrentUserChanged(int uniSenseId);
		public void OnNoCurrentUser();
	
	}

}
    