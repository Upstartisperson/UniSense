using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniSense.Users;
using UnityEngine.InputSystem;
using UniSense.Management;
public class UniSensePlayer 
{
    public static UniSensePlayer[] players;
    private static int _maxPlayers;
    public int PlayerId;
    public int UnisenseId;
    private static PlayerInputManager _manager;
    private IHandleSingleplayer _singleplayerHandler;
    public Camera PlayerCamera;
    public GameObject PlayerObject;
    public bool HasCam;
    public bool Active;
    public static void Initialize(int maxPlayers)
    {

        _maxPlayers = maxPlayers;
        players = new UniSensePlayer[_maxPlayers];
        if(PlayerInputManager.instance != null) _manager = PlayerInputManager.instance;
        else
        {
            Debug.LogError("Add a PlayerInputManager somewhere in the scene");
            Debug.Break();
        }
    }
    public UniSensePlayer(int playerId, int unisenseId, GameObject playerPrefab)
    {
       
        if(_manager == null)
        {
            Debug.LogError("Initialize UnisensePlayer First");
            Debug.Break();
        }
        _manager.playerPrefab = playerPrefab;
        PlayerObject = _manager.JoinPlayer().gameObject;
       
        if (!PlayerObject.TryGetComponent<IHandleSingleplayer>(out _singleplayerHandler))
        {
            Debug.LogError("Missing SinglePlayer Handler");
            Debug.Break();
        }
        Active = true;
        HasCam = PlayerObject.TryGetComponent<Camera>(out PlayerCamera);
        if(unisenseId != -1) _singleplayerHandler.SetCurrentUser(unisenseId);
        else Active = false;
        
        UnisenseId = unisenseId;
        PlayerId = playerId;
    }

    public void ModifyUser(UserChange change) => _singleplayerHandler.OnCurrentUserModified(change);

    public bool SetUser(int unisenseId)
    {
        if(!_singleplayerHandler.SetCurrentUser(unisenseId)) return false;
        UnisenseId = unisenseId;
        Active = true;
        return true;
    }

    public void SetRect(Rect rect)
    {
        if (HasCam) PlayerCamera.rect = rect;
    }

    public void RemoveUser()
    {
        _singleplayerHandler.SetNoCurrentUser();
        Active = false;
    }

    public void Destroy()
    {
        RemoveUser();
        GameObject.Destroy(PlayerObject);

    }

}
