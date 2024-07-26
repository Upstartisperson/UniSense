using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniSense.Utilities;
using UniSense.Users;
using UniSense.Connections;
using UniSense.Management;
using UnityEngine.InputSystem;

public class DualSenseManager : MonoBehaviour, IManage
{
    #region Inspector Fields
    public bool AllowMouseKeyboard;
    public bool AllowGeneric;


    public GameObject PlayerPrefab;
    public bool AutoJoin;
    [HideInInspector]
    public int MaxPlayers;
    public bool EnableSplitScreen;
    
    public bool _fixAscpect;
    public Rect _aspect;
    public bool _fixedNumber;
    public int _numScreens;
    public Rect _screenSpace;

    public bool _customSplitScreen;
    #endregion

    [HideInInspector]
    public static DualSenseManager Instance;
    private ref UniSenseUser[] _users { get { return ref UniSenseUser.Users; } }
    private int _nextPlayerId = 0;
    
    private List<UniSensePlayer> _players = new List<UniSensePlayer>();

    private int _initTimer;
    #region Initialization
    public void Start()
    {
        
        Instance = this;
        InputSystem.FlushDisconnectedDevices();
        QueueInit();
    }

    private void OnDestroy()
    {
        if (_initTimer < 50 && _initTimer > 0) InputSystem.onAfterUpdate -= QueueInit;
    }

    private void QueueInit()
    {
        if (_initTimer++ == 0) InputSystem.onAfterUpdate += QueueInit;
        if (_initTimer > 50)
        {
            UniSensePlayer.Initialize(MaxPlayers);
            _players.Add(new UniSensePlayer(_nextPlayerId++, -1, PlayerPrefab));
            UpdateSplitScreen();
            InputSystem.onAfterUpdate -= QueueInit;
            UniSenseConnectionHandler.InitializeMultiplayer(this, AllowMouseKeyboard, AllowGeneric);
            Debug.Log("Initialization successful");
        }

    }
    #endregion

    #region IManage
    public void InitilizeUsers()
    {
        if (!AutoJoin) return;
        for (int i = 0; i < _users.Length; i++)
        {
            if (_users[i].IsReadyToConnect)
            {
                JoinPlayer(i);
            }
        }
    }

    private int FindPlayerWithUser(int unisenseId)
    {
        for(int i = 0; i < _players.Count; i++)
        {
            if (_players[i].UnisenseId == unisenseId) return i;
        }
        return -1;
    }

    public void OnUserAdded(int unisenseId)
    {
        if (!AutoJoin) return;
        JoinPlayer(unisenseId);
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

    public UniSensePlayer JoinPlayer(int unisenseId)
    {
        for(int i = 0; i < _players.Count; i++)
        {
            if (_players[i].Active) continue;
            _players[i].SetUser(unisenseId);
            
            UpdateSplitScreen();
            return _players[i];
        }
        _players.Add(new UniSensePlayer(_nextPlayerId++, unisenseId, PlayerPrefab));
        UpdateSplitScreen();
        return _players[_players.Count - 1];
    }

    public bool RemovePlayer(int playerId)
    {
       if (_players.Count == 1)
       {
            _players[0].RemoveUser();
            return true;
       }

       for (int i = 0; i < _players.Count; i++)
       {
            if (_players[i].PlayerId == playerId)
            {
                _players[i].Destroy();
                _players.RemoveAt(i);
                UpdateSplitScreen();
                return true;
            } 
       }
       return false;
    }
    public void UpdateSplitScreen()
    {
       
        if (!EnableSplitScreen) return;

        if(_customSplitScreen)
        {
            throw new System.Exception("Not Implemented Exception");
        }
        else
        { 
            int numScreens = (_fixedNumber) ? _numScreens : _players.Count;
            int countX = Mathf.RoundToInt(Mathf.Sqrt(numScreens)); 
            int countY = Mathf.CeilToInt(Mathf.Sqrt(numScreens));
            float sizeX = _screenSpace.width / countX;
            float sizeY = _screenSpace.height / countY;

            float rectHeight = sizeY;
            float rectWidth = sizeX;

            

            if (_fixAscpect)
            {
                float aspectRatio = _screenSpace.width / _screenSpace.height;
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
                float x = _screenSpace.xMin + ((i % countX) * sizeX + sizeX / 2);

                float y = _screenSpace.yMax - ((int)(i / countX) * sizeY + (sizeY / 2));

                Rect rect = new Rect(0, 0, rectWidth, rectHeight);
                rect.center = new Vector2(x, y);
                _players[i].SetRect(rect);
            }

        }

    }

    
}
