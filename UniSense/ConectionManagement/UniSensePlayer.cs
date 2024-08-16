using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniSense.Users;
using UnityEngine.InputSystem;
using UniSense.Management;
using DeviceType = UniSense.Utilities.DeviceType;
public class UniSensePlayer 
{
    public int PlayerId; //Id unique to player
    public int UnisenseId; //UnisenseId of the connected UniSenseUser user
    
    public GameObject PlayerObject;
   
    public bool Active;
    public bool HasMouseKeyboard;

    private static PlayerInputManager _manager;
    private IHandleSingleplayer _singleplayerHandler;
    private bool _hasCam;
    private PlayerInput _playerInput;

    /// <summary>
    /// Used for debug display on <see cref="UniSense.PlayerManager.DualSenseManager"/>
    /// </summary>
    public DeviceType DeviceType 
    { 
        get 
        { 
            return UnisenseId == -1 ? HasMouseKeyboard ? DeviceType.MouseKeyboard 
                                                       : DeviceType.None 
                                    : UniSenseUser.Users[UnisenseId].ActiveDevice;
        } 
    }

    public static void Initialize()
    {       
        if(PlayerInputManager.instance != null) _manager = PlayerInputManager.instance;
        else
        {
            Debug.LogError("Add a PlayerInputManager somewhere in the scene");
            Debug.Break();
        }
    }
    
    public static UniSensePlayer ConnectPlayer(GameObject playerObject, int playerId)
    {
        return new UniSensePlayer(playerObject, playerId);
    }

    private UniSensePlayer(GameObject playerObject, int playerId)
    {
        //if (_manager == null)
        //{
        //    Debug.LogError("Initialize UnisensePlayer First");
        //    Debug.Break();
        //}

        if (!playerObject.TryGetComponent<IHandleSingleplayer>(out _singleplayerHandler))
        {
            Debug.LogError("Missing SinglePlayer Handler");
            Debug.Break(); 
        }

        Active = false;
        _playerInput = playerObject.GetComponent<PlayerInput>();
        _hasCam = _playerInput.camera != null;
        PlayerId = playerId;
        PlayerObject = playerObject;
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
        _playerInput = PlayerObject.GetComponent<PlayerInput>();
        _hasCam = _playerInput.camera != null;
        if (unisenseId != -1) _singleplayerHandler.SetCurrentUser(unisenseId);
        
        
        else
        {
            Active = false;
            PlayerObject.GetComponent<PlayerInput>().user.UnpairDevices();
        }
        UnisenseId = unisenseId;
        PlayerId = playerId;
    }

    public void SetPlayerNum(int num) => _singleplayerHandler.SetPlayerNumber(num);

    public void ModifyUser(UserChange change) => _singleplayerHandler.OnCurrentUserModified(change);

    public bool SetUser(int unisenseId)
    {
        HasMouseKeyboard = false;
        if (!_singleplayerHandler.SetCurrentUser(unisenseId)) return false;
        UnisenseId = unisenseId;
        Active = true;
        return true;
    }

    public void SetMouseKeyboard()
    {
        RemoveUser();
        _singleplayerHandler.SetMouseKeyboard();
        HasMouseKeyboard = true;
        Active = true;
    }

    public void SetRect(Rect rect)
    {
        if (_hasCam) _playerInput.camera.rect = rect;
        
    }

    public void RemoveUser()
    {
        _singleplayerHandler.SetNoCurrentUser();
        if(!HasMouseKeyboard) Active = false;
    }

    public void Destroy()
    {
        RemoveUser();
        GameObject.Destroy(PlayerObject);

    }

}
