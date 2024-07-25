using UniSense;
using UnityEngine;
using UnityEngine.InputSystem;
using UniSense.LowLevel;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.LowLevel;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
//using UniSense.NewConnections;
using UniSense.DevConnections;

#region Custom Data Structures
//TODO: Could potentially have a static system for the players so I would add a player like Player.Add(). Would update a static internal player list. This is definitely something to consider
//I Just need to plan it out and see if it would be easier to do it like this, probably but just have to try it out I guess.
//TODO: Add reconnection capability
//TODO: Add ways to listen to what this script is doing 
//TODO: Achieve feature parody with PlayerInputManager
//TODO: Find way to update the split screen 
//TODO: Add editor script and achieve feature parody with Player Input Manager
//TODO: Find new solution for _playersToReconnect queue since relaiontship can be broken;

public enum ControllerType
{
    DualSenseBT,
    DualSenseUSB,
    GenericGamepad
}
public class Player
{
    public string Name { get; private set; }
    private ref OldUniSenseUser user { get { return ref OldUniSenseConnectionHandler.UnisenseUsers[_unisenseId]; } }
    private Rect CameraRect { get { return playerInput.camera.rect; } set { playerInput.camera.rect = value; } }
    private GameObject gameObject;
    private PlayerInput playerInput { get { return gameObject.GetComponent<PlayerInput>(); } }
    private IHandleSingleplayer singlePlayerHandler;
    private int _unisenseId;
    private static List<Player> _players;
    private static Queue<int> _playersToReconnect;
    private static bool _initialized = false;

    private static int _nextUniqueId = 0;
    public int uniqueId { get; private set; }
    public bool UserConnected { get; private set; }
    //Maybe add a playerIndex int here don't know if needed
    public Player(int unisenseId, GameObject gameObject, IHandleSingleplayer singlePlayerHandeler, string name = null)
    {
        if (!_initialized) Debug.LogError("Not Initialized");
        this.Name = name;
        this.gameObject = gameObject;
        this._unisenseId = unisenseId;
        this.singlePlayerHandler = singlePlayerHandeler;
        singlePlayerHandeler.OnCurrentUserChanged(unisenseId);
        UserConnected = true;
        uniqueId = _nextUniqueId++; //Assign _nextUniqueId to _uniqueId then increment the value of _nextUniqueId
    }

    public static void Initialize(ref List<Player> players, ref Queue<int> playersToReconnect)
    {
        _players = players;
        _initialized = true;
        _playersToReconnect = playersToReconnect;
    }

    public void SetSplitScreen(int splitScreenIndex, int screenCount, SplitScreenBlueprint screenBlueprint, Rect screenRectangle)
    {
        if (!_initialized) Debug.LogError("Not Initialized");
        screenBlueprint.Recover();
        Rect rect = screenBlueprint.rects[screenCount -1][splitScreenIndex];
        rect = new Rect(
                                  x: ((rect.position.x + .5f - (0.5f * rect.width )) * screenRectangle.width ) + (screenRectangle.x),
                                  y: ((rect.position.y + .5f - (0.5f * rect.height)) * screenRectangle.height) + (screenRectangle.y),
                                  width : rect.size.x * screenRectangle.width,
                                  height: rect.size.y * screenRectangle.height
                                  );
        CameraRect = rect;
    }

    public static bool FindPlayerWithUnisenseId(int unisenseId,  out int playerIndex)
    {
        if (!_initialized) Debug.LogError("Not Initialized");
        for (playerIndex = 0; playerIndex < _players.Count; playerIndex++)
       {
            if(_players[playerIndex]._unisenseId == unisenseId) return true;
       }
       return false;
    }

    public static bool FindPlayerWithUniqueId(int uniqueId, out int playerIndex)
    {
        if (!_initialized) Debug.LogError("Not Initialized");
        for (playerIndex = 0; playerIndex < _players.Count; playerIndex++)
        {
            if (_players[playerIndex].uniqueId == uniqueId) return true; 
        }
        return false;
    }


    /// <summary>
    /// Reconnect an existing player to a new user
    /// </summary>
    /// <returns></returns>
    public bool ReConnect(int unisenseId) //Can be called from on user added but also manually
    {
        if (!_initialized) Debug.LogError("Not Initialized");
        singlePlayerHandler.OnCurrentUserChanged(unisenseId); //This should compltly handle even choosing what device is acitve and pairing the player input
        UserConnected = true;
        _unisenseId = unisenseId;
        return false;
    }


    /// <summary>
    /// Empties the player for when a controller disconnects but doesn't delete it so it can be reconnected later
    /// </summary>
    /// <returns></returns>
    public bool OnDisconnect() //call it in OnUserRemoved just have to update the single player handler
    {
        if (!_initialized) Debug.LogError("Not Initialized");
        //TODO: Verify
        //In UnisenseUser.Clear() all values are reset to default meaning nothing should need to be done
        if (!UserConnected) return true;
        if (_unisenseId == -1) return false;
        singlePlayerHandler.OnNoCurrentUser();
        UserConnected = false;
        _unisenseId = -1;
        _playersToReconnect.Enqueue(this.uniqueId);
        return true;
    }

    /// <summary>
    /// Empties the player for when a controller disconnects but doesn't delete it so it can be reconnected later
    /// </summary>
    /// <returns></returns>
    public bool InitiateDisconnect() //call it to manually disconnect a user
    {
        if (!_initialized) Debug.LogError("Not Initialized");
        if (!UserConnected) return true;
        if (_unisenseId == -1) return false;
        user.SetActiveDevice(UniSense.DevConnections.DeviceType.None);
        user.UnPairPlayerInput();
        singlePlayerHandler.OnNoCurrentUser();
        UserConnected = false;
        _unisenseId = -1;
        _playersToReconnect.Enqueue(this.uniqueId);
        return true;
    }

    /// <summary>
    /// Will call the UserChanged part of IHandelSingleplayer and set the internal _unisenseId
    /// </summary>
    /// <param name="unisenseId"></param>
    /// <returns></returns>
    public bool ChangeUser(int unisenseId)
    {
        if (!_initialized) Debug.LogError("Not Initialized");
        if (_unisenseId == -1) return false;
        singlePlayerHandler.OnCurrentUserChanged(unisenseId);
        _unisenseId = unisenseId;
        return true;
    }


    /// <summary>
    /// Will Call the usermodifed part of IHandelSinglePlayer
    /// </summary>
    /// <returns></returns>
    public bool ModifyUser(UserChange change)
    {
        if (!_initialized) Debug.LogError("Not Initialized");
        if (_unisenseId == -1) return false;
        singlePlayerHandler.OnCurrentUserModified(change);
        return true;
    }
    /// <summary>
    /// Deletes the player
    /// </summary>
    /// <returns></returns>
    private void Delete()
    {
        if (!_initialized) Debug.LogError("Not Initialized");
        //needs to communicate with player input manager to set the correct splitscreen 
        //Just going to try deleting the whole prefab
        if (UserConnected)
        {
            user.SetActiveDevice(UniSense.DevConnections.DeviceType.None);
            user.UnPairPlayerInput();
        }
        GameObject.Destroy(gameObject); //Needs more then just this
    }


    public static void DestroyAt(int playerIdnex)
    {
        if (!_initialized) Debug.LogError("Not Initialized");
        _players[playerIdnex].Delete();
        _players.RemoveAt(playerIdnex);
    }
    public static void Destroy(int uniqueId)
    {
        if (!_initialized) Debug.LogError("Not Initialized");
        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].uniqueId == uniqueId) { DestroyAt(i); return; }
        }
    }

}

#endregion







[DisallowMultipleComponent]
public class DualSenseManager : MonoBehaviour, IHandleMultiplayer
{

    #region Fields
    public InputActionProperty JoinAction;
    public InputActionProperty LeaveAction;
    [Tooltip("Keep Players If Device Is Lost")]
    public bool PersistPlayers;
    public GameObject PlayerPrefab;
    public int MaxPlayers = 4;
    private bool _splitScreen;
    public List<Player> PlayerList = new();
    public Transform SpawnPoint;
    [Header("Split-Screen")]
    public bool EnableSplitScreen;
    public SplitScreenBlueprint screenBlueprint;
    public bool SetFixedNumber;
    public int NumberOfScreens; 
    public Rect ScreenRectangle = new Rect { x = 0, y = 0, height = 1, width = 1 };
    /// <summary>
    /// Stores the unique Id of the players that are awaiting a reconnection
    /// </summary>
    public Queue<int> PlayersToReconnect = new Queue<int>();
    
    public static DualSenseManager instance { get; private set; }
    #endregion

    #region Initialization
    public void Start()
    {
        if (instance == null)
        {
            instance = this;
            if(PlayerInputManager.instance == null)
            {
                Debug.LogError("No player input manager found");
                return;
            }

            OldUniSenseConnectionHandler.InitializeMultiplayer(this);

            PlayerInputManager.instance.EnableJoining();
            PlayerInputManager.instance.playerPrefab = PlayerPrefab;
            //MaxPlayers = PlayerInputManager.instance.maxPlayerCount;
            _splitScreen = PlayerInputManager.instance.splitScreen;
            JoinAction.action.Enable();
            JoinAction.action.performed += OnJoinAction;
            LeaveAction.action.Enable();
            LeaveAction.action.performed += OnLeaveAction;
            Player.Initialize(ref PlayerList, ref PlayersToReconnect);
            return;
           
        }
        Destroy(this);
        
    }



    #endregion

    #region Methods

    private void OnLeaveAction(InputAction.CallbackContext context)
    {
        InputDevice device = context.control.device;
        ControllerType controllerType = ControllerType.GenericGamepad;
        if (device is DualSenseUSBGamepadHID) controllerType = ControllerType.DualSenseUSB;
        else if (device is DualSenseBTGamepadHID) controllerType = ControllerType.DualSenseBT;
        string key;
        int unisenseId = -1;
        int playerindex = 0;
        switch (controllerType)
        {
            case ControllerType.DualSenseBT:
                key = device.description.serial.ToString();
                if (OldUniSenseConnectionHandler.userLookup.TryGetUnisenseId(key, out unisenseId))
                {
                    if (!Player.FindPlayerWithUnisenseId(unisenseId, out playerindex)) return;
                    RemovePlayer(playerindex);
                }
                break;
            case ControllerType.DualSenseUSB:
                key = device.deviceId.ToString();
                if (OldUniSenseConnectionHandler.userLookup.TryGetUnisenseId(key, out unisenseId))
                {
                    if (!Player.FindPlayerWithUnisenseId(unisenseId, out playerindex)) return;
                    RemovePlayer(playerindex);
                }
                break;
            case ControllerType.GenericGamepad:
                key = device.deviceId.ToString();
                if (OldUniSenseConnectionHandler.userLookup.TryGetUnisenseId(key, out unisenseId))
                {
                    if (!Player.FindPlayerWithUnisenseId(unisenseId, out playerindex)) return;
                    RemovePlayer(playerindex);
                }
                break;
        }
    }

    public void OnDestroy()
    {
        JoinAction.action.performed += OnJoinAction;
        LeaveAction.action.performed -= OnLeaveAction;
    }

    private void OnJoinAction(InputAction.CallbackContext context)
    {
        InputDevice device = context.control.device;
        ControllerType controllerType = ControllerType.GenericGamepad;
        if (device is DualSenseUSBGamepadHID) controllerType = ControllerType.DualSenseUSB;
        else if (device is DualSenseBTGamepadHID) controllerType = ControllerType.DualSenseBT;
        string key;
        int unisenseId = -1;
        int playerindex = 0;
        switch (controllerType)
        {
            case ControllerType.DualSenseBT:
                key = device.description.serial.ToString();
                if (OldUniSenseConnectionHandler.userLookup.TryGetUnisenseId(key, out unisenseId))
                {
                    if (Player.FindPlayerWithUnisenseId(unisenseId, out playerindex)) return;
                    if (PersistPlayers && AttemptReconnect(unisenseId)) return;
                    AddPlayer(unisenseId, "nameeeee");
                }
                break;
            case ControllerType.DualSenseUSB:
                key = device.deviceId.ToString();
                if (OldUniSenseConnectionHandler.userLookup.TryGetUnisenseId(key, out unisenseId))
                {
                    if (Player.FindPlayerWithUnisenseId(unisenseId, out playerindex)) return;
                    if (PersistPlayers && AttemptReconnect(unisenseId)) return;
                    AddPlayer(unisenseId, "nameeeee");
                }
                break;
            case ControllerType.GenericGamepad:
                key = device.deviceId.ToString();
                if (OldUniSenseConnectionHandler.userLookup.TryGetUnisenseId(key, out unisenseId))
                {
                    if (Player.FindPlayerWithUnisenseId(unisenseId, out playerindex)) return;
                    if (PersistPlayers && AttemptReconnect(unisenseId)) return;
                    AddPlayer(unisenseId, "nameeeee");
                }
                break;
        }
    }

    public void InitilizeUsers() //Don't need it right now will for auto but needs to call it after devivce are matched in NewUnisenseConnectionHandler
    {
        Debug.Log("Initialize users");
    }

    public void OnUserAdded(int uniSenseId)
    {
        Debug.Log("user added");
    }

    public void OnUserModified(int uniSenseId, UserChange change)
    {
        if (Player.FindPlayerWithUnisenseId(uniSenseId, out int playerIndex)) 
        { 
          PlayerList[playerIndex].ModifyUser(change);
            
        }
    }

    public void OnUserRemoved(int uniSenseId)
    {
        if (Player.FindPlayerWithUnisenseId(uniSenseId, out int playerIndex))
        {
            if (PersistPlayers)
            {
                PlayerList[playerIndex].OnDisconnect();
            }
            else RemovePlayer(uniSenseId);
            //if (EnableSplitScreen) UpdateSplitScreen();
        }
    }


    #endregion

    #region Helper Methods
    public void AddPlayer(int uniSenseId, string name)
    {
        //TODO: currently how I find IHandleSinglePlayer leaves something to be desired

        GameObject _gameObject = PlayerInputManager.instance.JoinPlayer().gameObject;
        _gameObject.transform.SetPositionAndRotation(SpawnPoint.position, SpawnPoint.rotation);
        IHandleSingleplayer singlePlayerHandler = _gameObject.GetComponent<IHandleSingleplayer>();
        if (singlePlayerHandler == null) { Debug.LogError("Failed to add player"); return; } 
        PlayerList.Add(new Player(uniSenseId, _gameObject, singlePlayerHandler, name));
        if(EnableSplitScreen) UpdateSplitScreen();
    }

    public void RemovePlayer(int playerIndex) 
    {
        Player.DestroyAt(playerIndex);
        UpdateSplitScreen();
    }

    public bool AttemptReconnect(int unisenseId)
    {
        if (PlayersToReconnect.Count == 0) return false;
        if (Player.FindPlayerWithUniqueId(PlayersToReconnect.Dequeue(), out int playerIndex))
        {
            if (!PlayerList[playerIndex].ReConnect(unisenseId)) return true;
        }
        Debug.LogError("Failed To Reconnect");
        return false;
    }   

    public void UpdateSplitScreen()
    {
        for (int i = 0; i < PlayerList.Count; i++)
        {
            PlayerList[i].SetSplitScreen(i, (SetFixedNumber) ? NumberOfScreens : PlayerList.Count, screenBlueprint, ScreenRectangle);
        }
    }

    public void UpdatePlayerNumbers() //Might just outright remove
    {
        throw new NotImplementedException();
    }

    
    #endregion



}
