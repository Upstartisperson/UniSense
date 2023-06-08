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
using UniSense.NewConnections;


//TODO: FIXME usb controller not clearing output on game quit

[DisallowMultipleComponent]
public class DepreciatedDualSenseManager : MonoBehaviour
{
    public class Player
    {
        
        public string Name;
        public string Key;
        public Controller Controller;
        public GameObject GameObject;
        public PlayerInput PlayerInput;
        public DepreciatedDualSense DualSense;
        public ControllerType ControllerType
        {
            get { return Controller.ControllerType; }
        }
        public ControllerConnectionStatus ConnectionStatus
        {
            get { return Controller.connectionStatus; }
        }
        public int PlayerIndex;

        public Player(ref Controller controller, GameObject gameObject, int playerIndex, string name = null)
        {
            Key = controller.key;
            this.Controller = controller;
            this.GameObject = gameObject;
            PlayerInput = gameObject.GetComponent<PlayerInput>();
            DualSense = gameObject.GetComponent<DepreciatedDualSense>();
            this.DualSense.CurrentController = controller;
            PlayerIndex = playerIndex;
            Name = name;
        }

        public void UpdatePlayer(ref Controller controller)
        {
            Controller = null;
            Controller = controller;
            this.DualSense.CurrentController = controller;
            Key = controller.key;
        }
        public void DisconnectPlayer()
        {
            PlayerInput.user.UnpairDevices();
            Controller = null;
            Controller = new Controller();
            this.DualSense.CurrentController = Controller;
            Key = string.Empty;
            
        }
    }
    public enum PlayerNameType
    {
        GenerateNames,
        List,
    }
    public enum PlayerJoinBehavior
    {
        JoinPlayersWhenButtonIsPressed,
        JoinPlayersWhenJoinActionIsTriggered,
        JoinPlayersManually,
        JoinPlayersAutomatically
    }

    public static DepreciatedDualSenseManager instance { get; private set; }

    /// <summary>
    /// Min: 1, Max: 16
    /// </summary>
    [Range(1, 16)]
    public int MaxNumberOfPlayers = 1;
    public PlayerNameType NamingType;
    public string PlayerBaseName = "Player";
    public List<string> PlayerNames = new List<string>();
    public List<Player> PlayerList = new List<Player>();
    public PlayerJoinBehavior playerJoinBehavior;
    
    public GameObject PlayerPrefab;
    public Transform SpawnPoint;

    public bool AllowKeyBoardMouse;
    public bool AllowGenericGamepad;
    public bool AutoRemoveDisconnecected;
    
    public InputActionProperty JoinAction;
    private int _playerCount => PlayerList.Count;
    private int MaxPlayers = -1;
    
    private bool _splitScreen;
    
    
    private List<int> emptyPlayers = new();


    public event Action<int> PlayerJoined;
    public event Action<int> PlayerLeft;
    public event Action<int> PlayerRemoved;
    public event Action<int> PlayerReconnected;

    public void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        PlayerInputManager.instance.EnableJoining();
        PlayerInputManager.instance.playerPrefab = PlayerPrefab;
        MaxPlayers = PlayerInputManager.instance.maxPlayerCount;
        _splitScreen = PlayerInputManager.instance.splitScreen;
        ConnectionHandelerStatus status = UniSenseConnectionHandler.Initilize(new UniqueIdentifier(gameObject, this), true);
       
        UniSenseConnectionHandler.OnControllerChange += OnControllerChange;
        if(!(status == ConnectionHandelerStatus.Ok || status == ConnectionHandelerStatus.NoInputsDectected))
        {
            Debug.LogError(status.ToString());
            return;
        }

        if (playerJoinBehavior == PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered)
        {
         
            JoinAction.action.Enable();
            JoinAction.action.performed += OnJoinAction;
            return;
        }
        for (int i = 0; i < _controllers.Length; i++ )
        {
            ref Controller controller = ref _controllers[i];
            if (controller.ReadyToConnect)
            {
                AddNewPlayer(ref controller, GetName(_playerCount));
            }
        }
       
    }
    
   
   
    public void OnJoinAction(InputAction.CallbackContext context)
    {
     
        InputDevice device = context.control.device;
        ControllerType controllerType = ControllerType.GenericGamepad;
        if (device is DualSenseUSBGamepadHID) controllerType = ControllerType.DualSenseUSB;
        else if (device is DualSenseBTGamepadHID) controllerType = ControllerType.DualSenseBT;
        string key;
        switch (controllerType)
        {
            case ControllerType.DualSenseBT:
                key = device.description.serial.ToString();
                if (UniSenseConnectionHandler.ControllerLookup.ContainsKey(key))
                {
                    int unisenseID = UniSenseConnectionHandler.ControllerLookup[key];
                    if (FindPlayer(key) != -1) return; 
                    OnControllerChangeimpl(unisenseID, ControllerChange.Added, key);
                    
                }
                break;
            case ControllerType.DualSenseUSB:
                key = device.deviceId.ToString();
                if (UniSenseConnectionHandler.ControllerLookup.ContainsKey(key))
                {
                    int unisenseID = UniSenseConnectionHandler.ControllerLookup[key];
                    if (FindPlayer(key) != -1) return;
                    OnControllerChangeimpl(unisenseID, ControllerChange.Added, key);

                }
                break;
            case ControllerType.GenericGamepad:
                key = device.deviceId.ToString();
                if (UniSenseConnectionHandler.ControllerLookup.ContainsKey(key))
                {
                    int unisenseID = UniSenseConnectionHandler.ControllerLookup[key];
                    if (FindPlayer(key) != -1) return;
                    OnControllerChangeimpl(unisenseID, ControllerChange.Added, key);

                }
                break;
        }
    }

    private int FindPlayer(string key)
    {
        for (int i = 0; i< PlayerList.Count; i++)
        {
            if (PlayerList[i].Key == key)
            {
                return i;
            }
        }
        return -1;
    }
    private void OnControllerChange(int unisenseID, ControllerChange change, string key)
    {
        if(playerJoinBehavior == PlayerJoinBehavior.JoinPlayersAutomatically || change == ControllerChange.Removed)
        {
            OnControllerChangeimpl(unisenseID, change, key);
        }
       
    }

    private void OnControllerChangeimpl(int UnisenseID, ControllerChange change, string key) 
    {
        switch (change)
        {
            case ControllerChange.Added:
                if (emptyPlayers.Count != 0)
                {
                    ReconnectPlayer(ref _controllers[UnisenseID], emptyPlayers[0]);
                    emptyPlayers.RemoveAt(0);
                    return;
                }
                AddNewPlayer(ref _controllers[UnisenseID], GetName(_playerCount));
                break;
            case ControllerChange.Removed:
                if (key == null) return;
                for (int i = 0; i < PlayerList.Count; i++)
                {
                    if (PlayerList[i].Key == key)
                    {
                        PlayerList[i].DisconnectPlayer();
                        emptyPlayers.Add(i);
                    }
                }
                break;
        }
    }
    private string GetName(int playerIndex)
    {
        switch (NamingType)
        {
            case PlayerNameType.GenerateNames:
                return PlayerBaseName + playerIndex.ToString();

            case PlayerNameType.List:
                if (playerIndex < PlayerNames.Count) return PlayerNames[playerIndex].ToString();
                return PlayerBaseName + playerIndex.ToString();
        }
        return null;
    }

    private ref Controller[] _controllers { get { return ref UniSenseConnectionHandler.Controllers; } }

    public void Disable()
    {

    }

    public void Enable()
    {

    }

    public void OnDisable()
    {
        UniSenseConnectionHandler.OnControllerChange -= OnControllerChange;
        UniSenseConnectionHandler.Destroy(new UniqueIdentifier(gameObject, this));
        JoinAction.action.performed -= OnJoinAction;
        JoinAction.action.Dispose();

    }



    



    [SerializeField]
    public bool Gamepad
    {
        get { return AllowGenericGamepad; }
        set { if (Application.isPlaying) return; AllowGenericGamepad = value; }
    }

    public void AddNewPlayer(ref Controller controller, string name)
    {
        int splitScreenIndex = -1;
        //if (_splitScreen) splitScreenIndex = _playerCount;
        InputDevice[] devices = new InputDevice[1] {controller.devices.InputDevice};



        GameObject _gameObject = PlayerInputManager.instance.JoinPlayer(playerIndex: _playerCount, splitScreenIndex: splitScreenIndex,  pairWithDevices: devices).gameObject;
        _gameObject.transform.SetPositionAndRotation(SpawnPoint.position, SpawnPoint.rotation);
        Player player = new Player(ref controller, _gameObject, _playerCount, name);
        player.GameObject.name = name;
        UniSenseConnectionHandler.ConnectController(ref controller, new UniqueIdentifier(gameObject, this));
        player.DualSense.CurrentController = controller;
        PlayerList.Add(player);
    }

    public void RemovePlayer(int playerIndex)
    {
       PlayerList[playerIndex].DisconnectPlayer();
    }

    public void ReconnectPlayer(ref Controller controller, int playerIndex)
    {
        InputDevice[] devices = new InputDevice[1] {controller.devices.InputDevice};
        PlayerList[playerIndex].PlayerInput.SwitchCurrentControlScheme(devices);
        UniSenseConnectionHandler.ConnectController(ref controller, new UniqueIdentifier(gameObject, this));
        PlayerList[playerIndex].UpdatePlayer(ref controller);
    }

    public void DissconnectPlayer(Player player)
    {
        
    }

    
	

}
