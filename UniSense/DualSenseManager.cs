using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniSense.Utilities;
using UniSense.Users;
using UniSense.Connections;
using UniSense.Management;
using UnityEngine.InputSystem;

namespace UniSense.PlayerManager
{


    public enum JoinBehavoir : int
    {
        JoinPlayersAutomatically = 0,
        JoinPlayersWhenJoinActionIsTriggered = 1,
        JoinPlayersManually =2
    }
    public enum NotificationBehavoir : int
    {
        SendMessages = 0,
        BroadCastMessages = 1,
        None = 2
    }


    public class DualSenseManager : MonoBehaviour, IManage
    {

        #region Inspector Fields
        public bool _ManagerConfigured = false;

        public NotificationBehavoir notificationBehavoir;
        public  JoinBehavoir _JoinBehavoir;

        public InputActionProperty _JoinAction;
        public GameObject _PlayerPrefab;

        [Tooltip("Allow Mouse and Keyboard Player to join, Will be set to player 1")]
        public bool _AllowMouseKeyboard;
        [Tooltip("Allow Non-DualSense Controllers to Join")]
        public bool _AllowGeneric;

        [Range(1, 16)]
        public int _MaxPlayers;

        public bool _EnableSplitScreen;
        public bool _customSplitScreen;
        public SplitScreenBlueprint _customBlueprint;
        public bool _MaintianAscpectRatio;
        
        public bool _SetFixedNumber;
        [Range(1, 16)]
        public int _NumScreens;
        public Rect _ScreenSpace;

        private Camera _SSBackroundCam;

        private PlayerInputManager _playerInputManager;


        private bool _joinActionHooked = false;


        public void DisableCam(Camera cam)
        {
           if(cam == gameObject.GetComponent<Camera>())
            {
                gameObject.GetComponent<Camera>().enabled = false;
                Camera.onPostRender -= DisableCam;
            }  
        }

        private void AddCamera()
        {
            if (!TryGetComponent<Camera>(out _SSBackroundCam))
            {
                _SSBackroundCam = gameObject.AddComponent<Camera>();
            }
            else
            {
                DestroyImmediate(_SSBackroundCam);
                _SSBackroundCam = gameObject.AddComponent<Camera>();
            }
            _SSBackroundCam.clearFlags = CameraClearFlags.SolidColor;
            _SSBackroundCam.backgroundColor = Color.black;
            _SSBackroundCam.cullingMask = 0;
            _SSBackroundCam.farClipPlane = 0.02f;
            _SSBackroundCam.nearClipPlane = 0.01f;
            _SSBackroundCam.depth = -1;
            _SSBackroundCam.allowHDR = false;
            _SSBackroundCam.allowMSAA = false;
        }
        public void ConfigureManager()
        {
            AddCamera();
            AddPlayerInputManager();
        }

        public void AddPlayerInputManager()
        {
            if (!TryGetComponent<PlayerInputManager>(out _playerInputManager))
            {
                _playerInputManager = gameObject.AddComponent<PlayerInputManager>();
            }
            else
            {
                DestroyImmediate(_playerInputManager);
                _playerInputManager = gameObject.AddComponent<PlayerInputManager>();
            }
            _playerInputManager.notificationBehavior = PlayerNotifications.InvokeUnityEvents;
            _playerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
            _playerInputManager.splitScreen = false;
        }



        #endregion

        [HideInInspector]
        public static DualSenseManager Instance;
        private ref UniSenseUser[] _users { get { return ref UniSenseUser.Users; } }
        private int _nextPlayerId = 0;

        private List<UniSensePlayer> _players = new List<UniSensePlayer>();
        public List<UniSensePlayer> Players { get { return _players; } }

        private int _initTimer;
        #region Initialization
        public void Start()
        {
            if (!_ManagerConfigured)
            {
                Debug.LogError("Manager Not Configured. End Play Mode to Configure Manager");
                Debug.Break();
                return;
            }

            Instance = this;
            InputSystem.FlushDisconnectedDevices();
            if(!TryGetComponent<Camera>(out _SSBackroundCam))
            {
              AddCamera();
            }
            QueueInit();
        }

        private void OnDestroy()
        {
            if (_initTimer < 50 && _initTimer > 0) InputSystem.onAfterUpdate -= QueueInit;
            if (_joinActionHooked)
            {
                _JoinAction.action.performed -= OnJoinActionTriggered;
                _JoinAction.action.Disable();
            }
           
        }

        private void QueueInit()
        {
            if (_initTimer++ == 0) InputSystem.onAfterUpdate += QueueInit;
            if (_initTimer > 50)
            {
                UniSensePlayer.Initialize(_MaxPlayers);

                InputSystem.onAfterUpdate -= QueueInit;
                UniSenseConnectionHandler.InitializeMultiplayer(this, _AllowMouseKeyboard, _AllowGeneric);
                Debug.Log("Initialization successful");
                if(_JoinBehavoir == JoinBehavoir.JoinPlayersWhenJoinActionIsTriggered)
                {
                    _JoinAction.action.Enable();   
                    _JoinAction.action.performed += OnJoinActionTriggered;
                    
                    _joinActionHooked = true;
                }
            }

        }
        #endregion

        private void OnJoinActionTriggered(InputAction.CallbackContext context)
        {
            InputDevice device = context.control.device;
            string deviceKey;
            switch (device)
            {
                case DualSenseBTGamepadHID:
                    deviceKey = device.description.serial;
                    break;

                case DualSenseUSBGamepadHID:
                    deviceKey = device.deviceId.ToString();
                    break;

                case Gamepad:
                    if (!_AllowGeneric) return;
                    deviceKey = device.deviceId.ToString();
                    break;

                case Mouse:
                    JoinMouseKeyboardPlayer(_PlayerPrefab);
                    return;
                   
                case Keyboard:
                    JoinMouseKeyboardPlayer(_PlayerPrefab);
                    return;

               default : return;
            }
            if(!UniSenseUser.Find(deviceKey, out int unisenseId))
            {
                Debug.LogError("No UniSense Id found For Device");
                return;
            }
            JoinPlayer(unisenseId, _PlayerPrefab);

        }

        #region IManage
        public void InitilizeUsers()
        {
            if (_JoinBehavoir != JoinBehavoir.JoinPlayersAutomatically) return;
            if(_AllowMouseKeyboard) JoinMouseKeyboardPlayer(_PlayerPrefab);
            
            for (int i = 0; i < _users.Length; i++)
            {
                
                if (_users[i].IsReadyToConnect)
                {
                    JoinPlayer(i, _PlayerPrefab);
                }
            }
        }

        private int FindPlayerWithUser(int unisenseId)
        {
            for (int i = 0; i < _players.Count; i++)
            {
                if (_players[i].UnisenseId == unisenseId) return i;
            }
            return -1;
        }

        private void NotifyPlayerJoined(int playerId)
        {
            switch (notificationBehavoir)
            {
                case NotificationBehavoir.SendMessages:
                    SendMessage("OnPlayerJoind", playerId, SendMessageOptions.DontRequireReceiver);
                    break;
                case NotificationBehavoir.BroadCastMessages:
                    BroadcastMessage("OnPlayerJoind", playerId, SendMessageOptions.DontRequireReceiver);
                    break;
                default:
                    break;
            }
        }

        private void NotifyPlayerLeft(int playerId)
        {
            switch (notificationBehavoir)
            {
                case NotificationBehavoir.SendMessages:
                    SendMessage("OnPlayerLeft", playerId, SendMessageOptions.DontRequireReceiver);
                    break;
                case NotificationBehavoir.BroadCastMessages:
                    BroadcastMessage("OnPlayerLeft", playerId, SendMessageOptions.DontRequireReceiver);
                    break;
                default:
                    break;
            }
        }

        public void OnUserAdded(int unisenseId)
        {
            if (_JoinBehavoir != JoinBehavoir.JoinPlayersAutomatically) return;
            JoinPlayer(unisenseId, _PlayerPrefab);
        }

        public void OnUserModified(int unisenseId, UserChange change)
        {
            int playerIndex = FindPlayerWithUser(unisenseId);
            if (playerIndex != -1)
            {
                _players[playerIndex].ModifyUser(change);
                return;
            }
            Debug.LogError("No PlayerFound With User Id (unisenseId)");
        }

        public void OnUserRemoved(int unisenseId)
        {
            int playerIndex = FindPlayerWithUser(unisenseId);
            if (playerIndex != -1)
            {
                _players[playerIndex].RemoveUser();
                
                return;
            }
            Debug.LogError("No PlayerFound With User Id (unisenseId)");
        }
        #endregion

        public UniSensePlayer JoinPlayer(int unisenseId, GameObject playerPrefab)
        {
            for (int i = 0; i < _players.Count; i++)
            {
                if (_players[i].Active) continue;
                _players[i].SetUser(unisenseId);

                UpdateSplitScreen();
                return _players[i];
            }
            _players.Add(new UniSensePlayer(_nextPlayerId++, unisenseId, playerPrefab));
            UpdateSplitScreen();
            NotifyPlayerJoined(_nextPlayerId - 1);
            return _players[_players.Count - 1];
        }

        public UniSensePlayer JoinMouseKeyboardPlayer(GameObject playerPrefab)
        {
            for (int i = 0; i < _players.Count; i++)
            {
                if (_players[i].Active) continue;
                _players[i].SetMouseKeyboard();
                UpdateSplitScreen();
                return _players[i];
            }
            _players.Add(new UniSensePlayer(_nextPlayerId++, -1, playerPrefab));
            _players[_players.Count - 1].SetMouseKeyboard();
            UpdateSplitScreen();
            return _players[_players.Count - 1];
        }

        public bool RemovePlayer(int playerId)
        {

            for (int i = 0; i < _players.Count; i++)
            {
                if (_players[i].PlayerId == playerId)
                {
                    NotifyPlayerLeft(playerId);
                    _players[i].Destroy();
                    _players.RemoveAt(i);
                    UpdateSplitScreen();
                    
                    return true;
                }
            }
            return false;
        }

        public Rect TranslateToUV(Rect rect, Vector2 screenSize)
        {
            rect.position = new Vector2((screenSize.x / 2) + rect.position.x - (rect.size.x / 2), (screenSize.y / 2) + rect.position.y - (rect.size.y / 2));
            return rect;
        }

        public void UpdateSplitScreen()
        {

            if (!_EnableSplitScreen) return;

            if (_customSplitScreen)
            {
               if (_customBlueprint != null)
               {
                   // Rect[][] rects =_customBlueprint.RetriveDefualt();
                    _customBlueprint.Recover();
                    


                    int screens = (_SetFixedNumber) ? _NumScreens : _players.Count;
                    
                    Rect[] rects = _customBlueprint.rects[screens -1];

                    for (int i = 0; i < _players.Count; i++)
                    {
                        _players[i].SetRect(TranslateToUV(rects[i], _ScreenSpace.size));
                    }
                    gameObject.GetComponent<Camera>().enabled = true;
                    Camera.onPostRender += DisableCam;
                    return;
                }
                Debug.LogError("Custom Blueprint Not Found, Reverting To Default");
                _customSplitScreen = false;
            }
           
            int numScreens = (_SetFixedNumber) ? _NumScreens : _players.Count;
            int countX = Mathf.RoundToInt(Mathf.Sqrt(numScreens));
            int countY = Mathf.CeilToInt(Mathf.Sqrt(numScreens));
            float sizeX = _ScreenSpace.width / countX;
            float sizeY = _ScreenSpace.height / countY;

            float rectHeight = sizeY;
            float rectWidth = sizeX;

            if (_MaintianAscpectRatio)
            {
                float aspectRatio = _ScreenSpace.width / _ScreenSpace.height;
                if (Mathf.Abs(aspectRatio - (rectWidth / rectHeight)) > 0.00001f)
                {
                    if (aspectRatio < rectWidth / rectHeight)
                    {
                        rectWidth = rectHeight * aspectRatio;
                    }
                    else
                    {
                        rectHeight = rectWidth / aspectRatio;
                    }
                }

            }

            for (int i = 0; i < _players.Count; i++)
            {
                float x = _ScreenSpace.xMin + ((i % countX) * sizeX + sizeX / 2);

                float y = _ScreenSpace.yMax - ((int)(i / countX) * sizeY + (sizeY / 2));

                Rect rect = new Rect(0, 0, rectWidth, rectHeight);
                rect.center = new Vector2(x, y);
                _players[i].SetRect(rect);
            }
            gameObject.GetComponent<Camera>().enabled = true;
            Camera.onPostRender += DisableCam;
            
        }


    }
}