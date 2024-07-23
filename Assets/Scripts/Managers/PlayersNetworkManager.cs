using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

using Unity.Netcode.Transports.UTP;
using Unity.Services.Matchmaker.Models;

public class PlayersNetworkManager : BaseManager
{
    public NetworkVariable<int> NumberOfPlayers = new NetworkVariable<int>();
    public NetworkVariable<float> WaitForPlayersTimer = new NetworkVariable<float>();

    public List<PlayerData> Players { get; private set; } = new List<PlayerData>();
    public bool IsInitialized { get; private set; }
    public Action<PlayerData> PlayerDisconnectedEvent;

    private Coroutine waitForPlayersCoroutine;
    private Coroutine autoShutDownCoroutine;
    private Coroutine idleShutDownCoroutine;
    private PlayerData? localPlayerData;

    private float WaitForAutoShutDownTimer = 0;
    private float WaitForIdleShutDownTimer = 0;

    private ulong lastClientId;


    private void Start()
    {
        Debug.Log("PlayersNetworkManager starting");
        NetworkManager.Singleton.OnClientStarted += OnClientStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        if (IsClient)     
        {
            Debug.Log("Connecting to " + NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address + ":" + NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port);
        }
        // Debug.Log ("GameManager.Instance.isServer = " + GameManager.Instance.isServer + " " +  IsServer);
        if (GameManager.Instance.isServer)
        {
            if (idleShutDownCoroutine == null)
            {
                Debug.Log("Starting idleShutDownCoroutine");
                idleShutDownCoroutine = StartCoroutine(IdleShutDownCoroutine());
            }
        }
    }

    public override void OnDestroy()
    {
        Debug.Log("PlayersNetworkManager destroy");
        base.OnDestroy();
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    public override void OnNetworkSpawn()
    {
        IsInitialized = true;
        Debug.Log("PlayersNetworkManager Initialized");
        if (IsServer)
        {
            NumberOfPlayers.Value = 0;
            WaitForPlayersTimer.Value = 0;
        }

    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"PlayersNetworkManager Client Connected {clientId}");
        if (IsClient && localPlayerData.HasValue)
        {
            Debug.Log("localPlayerData.WASDPositionId = " + localPlayerData?.PositionId);
            AddPlayerDataServerRpc(localPlayerData.Value, new ServerRpcParams());
            localPlayerData = null;
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log("Running OnClientDisconnected clientId = " + clientId);
        if (GameManager.Instance.isServer)
        {
            bool isDisconnectedPlayerSet = false;
            PlayerData disconnectedPlayer = default;
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i].Id == clientId)
                {
                    disconnectedPlayer = Players[i];
                    Players.RemoveAt(i);
                    isDisconnectedPlayerSet = true;
                    break;
                }
            }
            NumberOfPlayers.Value = Players.Count;
            if (isDisconnectedPlayerSet)
            {
                PlayerDisconnectedEvent?.Invoke(disconnectedPlayer);
            }
            if (Players.Count < GlobalGameSettings.Settings.MinPlayerAmount)
            {
                WaitForPlayersTimer.Value = 0;
                if (waitForPlayersCoroutine != null)
                {
                    StopCoroutine(waitForPlayersCoroutine);
                    waitForPlayersCoroutine = null;
                }
            }
        }

        if(!GameManager.Instance.isServer) //IsClient
        {
            bool triggerDisconnect = true;
            Debug.Log("Client Disconnected");
            if (NetworkManager.Singleton.DisconnectReason != string.Empty)
            {
                Debug.Log($"Approval Declined Reason: {NetworkManager.Singleton.DisconnectReason}");
                if ( NetworkManager.Singleton.DisconnectReason == "Incorrect Client Version")
                {
                    EventBus.TriggerEvent(EventBusEnum.EventName.SERVER_REJECTED_CLIENT);
                    triggerDisconnect = false;
                }
            }
            if(GameManager.Instance.IsMatchActive)
            {
                GameManager.Instance.EndMatch();
            }
            if (triggerDisconnect)
            {
                EventBus.TriggerEvent(EventBusEnum.EventName.SERVER_DISCONNECTED_CLIENT);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if(IsServer)
        {
            Players.Clear();

            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    public void SetLocalPlayerData(string uID, string playerName, CharacterData selectedCharacter, int positionId, RankingData rankingData, string steamId, int teamIndex = -1)
    {
        Debug.Log("Set Local Player Data");
        localPlayerData = new PlayerData()
        {
            Uid = uID, 
            DisplayName = playerName, 
            SelectedCharacterId = selectedCharacter.InternalID, 
            PositionId = positionId,
            RP = rankingData.RP,
            ArenaGroup = rankingData.ArenaGroup,
            SteamId = steamId,
            TeamIndex = teamIndex
        };
    }

    private void OnClientStarted()
    {
        Debug.Log("PlayersNetworkManager Client Started");
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddPlayerDataServerRpc(PlayerData playerData, ServerRpcParams rpcParams)
    {
        playerData.Id = rpcParams.Receive.SenderClientId;
        lastClientId = playerData.Id;
        Debug.Log("Client Connected to PlayersNetworkManager, PlayerID:" + playerData.Id + " Player UID: " + playerData.Uid);
        Debug.Log("Client WASDPositionId = " + playerData.PositionId);

        foreach(var p in Players)
        {
            if(p.Id == playerData.Id)
            {
                return;
            }
        }

        Players.Add(playerData);
        NumberOfPlayers.Value = Players.Count;

        if (IsServer)
        {
            if (autoShutDownCoroutine == null)
            {
                Debug.Log("Starting autoShutDownCoroutine");
                autoShutDownCoroutine = StartCoroutine(AutoShutDownCoroutine());
            }
        }


        if ((Players.Count == GlobalGameSettings.Settings.MaxPlayerAmount) ||
            (EnvironmentSettings.Settings.IsWASD && (Players.Count == GlobalGameSettings.Settings.MinPlayerAmount) ))
        {
            Debug.Log("Stopping timer coroutines");
            if (autoShutDownCoroutine != null)
            {
                StopCoroutine(autoShutDownCoroutine);
                autoShutDownCoroutine = null;
            }
            if (waitForPlayersCoroutine != null)
            {
                StopCoroutine(waitForPlayersCoroutine);
                waitForPlayersCoroutine = null;
            }
            if (idleShutDownCoroutine != null)
            {
                StopCoroutine(idleShutDownCoroutine);
                idleShutDownCoroutine = null;
            }
            GameManager.Instance.StartOnlineGame(Players[0].ArenaGroup);
        }
        else if (Players.Count >= GlobalGameSettings.Settings.MinPlayerAmount)
        {
            WaitForPlayersTimer.Value = 0;
            if (waitForPlayersCoroutine == null)
            {
                waitForPlayersCoroutine = StartCoroutine(WaitForPlayersCoroutine());
            }
        }
    }

    private IEnumerator WaitForPlayersCoroutine()
    {
        float timer = GlobalGameSettings.Settings.WaitForPlayersTimer;

        if(Debug.isDebugBuild)
        {
            timer = GlobalGameSettings.Settings.DebugWaitForPlayersTimer;
        }

        while (WaitForPlayersTimer.Value < timer)
        {
            yield return null;
            WaitForPlayersTimer.Value += Time.deltaTime;
        }
        // need to kick odd player out before starting online game
        if ((Players.Count / GlobalGameSettings.Settings.TeamSize) * GlobalGameSettings.Settings.TeamSize != Players.Count )
        {
            Debug.Log("Need to kick some players out");
            Debug.Log("Players.Count = " + Players.Count);
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[]{lastClientId}
                }
            };
            ForceDisconnectClientRpc(clientRpcParams);
            Debug.Log("Players.Count = " + Players.Count);
            // don't want to wait for this to be called by event trigger
            // make sure the player is removed before StartOnlineGame()
            OnClientDisconnected(lastClientId);
            Debug.Log("Players.Count = " + Players.Count);
        }
        waitForPlayersCoroutine = null;
        StopCoroutine(autoShutDownCoroutine);
        autoShutDownCoroutine = null;
        if (idleShutDownCoroutine != null)
            {
                StopCoroutine(idleShutDownCoroutine);
                idleShutDownCoroutine = null;
            }
        // May be it is necessary to wait for the client actually disconnected?
        //while ((Players.Count / GlobalGameSettings.Settings.TeamSize) * GlobalGameSettings.Settings.TeamSize != Players.Count )
        //{
        //    Debug.Log("Players.Count = " + Players.Count);
        //    yield return null;
        //}
        GameManager.Instance.StartOnlineGame(Players[0].ArenaGroup);
    }

    [ClientRpc]
    private void ForceDisconnectClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("ForceDisconnectClientRpc is running");
        if (!GameManager.Instance.isServer)
        {
            EventBus.TriggerEvent(EventBusEnum.EventName.SERVER_DISCONNECT_CLIENT);
        }
    }

    private IEnumerator AutoShutDownCoroutine()
    {
        float timer = 20; //15
        while (WaitForAutoShutDownTimer < timer)
        {
            yield return null;
            WaitForAutoShutDownTimer += Time.deltaTime;
        }
        Debug.Log("Times up, Auto Shut Down...");
        if (IsServer)
        {
            //#if UNITY_EDITOR
            // UnityEditor.EditorApplication.isPlaying = false;
            //#else
            if (!GameManager.Instance.IsLocalDevelopment)
            {    
                Application.Quit();
            }
           //#endif
        }
    }

    private IEnumerator IdleShutDownCoroutine()
    {
        float timer = 30;
        while (WaitForIdleShutDownTimer < timer)
        {
            yield return null;
            WaitForIdleShutDownTimer += Time.deltaTime;
        }
        Debug.Log("Times up, Idle Shut Down...");
        if (IsServer)
        {
            //#if UNITY_EDITOR
            // UnityEditor.EditorApplication.isPlaying = false;
            //#else
            if (!GameManager.Instance.IsLocalDevelopment)
            {    
                Application.Quit();
            }
           //#endif
        }
    }
   
}

