using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameAnalyticsSDK;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

using TMPro;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Steamworks;
using HeathenEngineering.SteamworksIntegration;

using Npgsql;

[Serializable]
public class MatchmakingPlayerData
{
    public string LobbyId;
    public string AppVer;
    public int ArenaGroup;
    public int TeamIndex;
}

public class MatchmakingParam
{
    public string LobbyId;
    public bool TwoPlayerLobby;
    public bool MultiPlayerLobby;
    public int PlayerArenaGroup;
}

public class GameManager : NetworkBehaviour
{
    public bool DisableRP = false;
    public static GameManager Instance;

    ApplicationData m_AppData;
    
    [SerializeField]
    private int positionId = -1;
    [HideInInspector]
    public bool IsLocalDevelopment = false;
    [SerializeField]
    private World world;   

    public bool IsMatchActive { get; private set; }
    public bool IsOnline { get; private set; }
    public int ArenaGroup { get; private set; }
    public bool IsTrainingMode { get; private set; }
    public int PlayersToStartOfflineGame { get; set; }
    public bool IsGameEnd { get; private set; }
    public World World => world;

    public int ClientCardPlayed = 0;

    public event Action OnEscapeKeyPressed;
    public event Action GameStartedEvent;
    public event Action GameWonEvent;
    public event Action GameLostEvent;

    [SerializeField]
    private TextMeshProUGUI appVerText;
    [SerializeField]
    private TextMeshProUGUI waitingForPlayerText;
    
    private static Dictionary<Type, BaseManager> subManagersDictionary;
    

    public bool isServer;
    public bool isHost = false;
    // IsServer, etc. are only set once the server has started 
    // or the client has connected to the server.
#if UNITY_EDITOR
    private bool IsEditor = true;
#else
    private bool IsEditor = false;
#endif
    
    private bool IsServerGameStarted = false;
    public bool IsAllocated = false;
    public bool IsDeallocated = false;
    private bool AllocationDone = false;
    private bool DeallocationDone = false;

    private Example_ServerQueryHandler m_ServerQueryHandler;
    private Example_ServerEvents m_ServerEvents;
    
    private float pollTicketTimer;
    private float pollTicketTimerMax = 1.1f;
    private CreateTicketResponse createTicketResponse;

    private ushort portNumber = 7777; // Invalid port number
    private string address;
    private bool pollingMatchmakingTicket;
    private Coroutine autoDisconnectClientAfterMatchCoroutine;
    private float waitForAutoQuitWinnerMenuTimer = 0;


    private string steamLobbyId;
    private SteamLobbyManager steamLobbyManager;

    private PlayerData offlineGamePlayerData;

    // private Dictionary<string, string> pendingJoinersInfo = new Dictionary<string, string>();
    private List<Character>[] teamLosingOrder;
    private List<List<Character>> currentTeams;
    private int currentTeamCount = 0;

    private void Awake()
    {

#if UNITY_SERVER
        isServer = true;        
#else
        isServer = false;
#endif
#if IS_DEMO
        DisableRP = true;
#endif
        //init the command parser, get launch args
        m_AppData = new ApplicationData();
        IsLocalDevelopment = m_AppData.IsLocalDevelopment;
        if (m_AppData.PosID != -1)
        {
            positionId = m_AppData.PosID;
        }
        if(NetworkManager.Singleton == null)
        {
            //if there's no Network Manager, force load Init Secene
            SceneManager.LoadScene(0);
            return;
        }

        if(Instance != null)
        {
            Debug.LogError("There can only be one GameManager on the scene!");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        subManagersDictionary = new Dictionary<Type, BaseManager>();
        var subManagers = GetComponents<BaseManager>();

        foreach(var sm in subManagers)
        {
            if(subManagersDictionary.ContainsKey(sm.GetType()))
            {
                Debug.LogError($"Two components of type {sm.GetType()} were found. This is not allowed");
                continue;
            }

            subManagersDictionary.Add(sm.GetType(), sm);
            sm.Init();
        }

#if UNITY_EDITOR
        IsLocalDevelopment = true;
#endif

        if (isServer || Debug.isDebugBuild)
        {
            //Application.targetFrameRate = 30;    
        }

        var characterManager = GetManager<CharacterManager>();
        characterManager.CharacterStateChangedEvent += CheckForWinCondition;

        PlayersToStartOfflineGame = GlobalGameSettings.Settings.MaxPlayerAmount;
    }

    private async void Start()
    {
        Debug.Log("GameManager started");
        appVerText.text = EnvironmentSettings.Settings.GetVersionText(positionId.ToString());
        
        address = NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address;
        pollingMatchmakingTicket = false;
        if (isServer)
        {            
            if (!isHost)
            {
                m_ServerQueryHandler = NetworkManager.Singleton.GetComponent<Example_ServerQueryHandler>();
                m_ServerEvents = NetworkManager.Singleton.GetComponent<Example_ServerEvents>();
                IsAllocated = m_ServerEvents.isAllocated;
                LogServerConfig();
            }
            else
            {
                GetManager<LocalRankingManager>().Init(GetNetworkPlayerId());    
            }
        }
        else
        {
            await InitializeUnityAuthentication();
            EventBus.StartListening(EventBusEnum.EventName.SERVER_DISCONNECT_CLIENT, EndMatchAndLoadMainScene);
            portNumber = m_AppData.Port;
            address = m_AppData.IP;
            Debug.Log("Args to use = " + address + ":" + portNumber);
#if !NOSTEAMWORKS            
            // Redirect player to specific lobby room when player clicks "Join Game" from Steam Chat to open the game
            if (SteamManager.Initialized)
            {
                if (m_AppData.SteamLobby != null)
                {
                    Debug.Log("Try to redirect player to specific lobby room " + m_AppData.SteamLobby);
                    OnLobbyIdReceived(new CSteamID(m_AppData.SteamLobby));
                }
            }
#endif       
            GetManager<LocalRankingManager>().Init(GetNetworkPlayerId());
            GameAnalytics.Initialize();
        }
    }

#if !NOSTEAMWORKS    
    private void OnLobbyIdReceived(CSteamID lobbyId)
    {
        if (!lobbyId.IsValid() || !lobbyId.IsLobby()) return;
        
        Debug.Log($"Accepted join lobby {lobbyId} invitation from Steam Chat");
        var processedLobbyId = PlayerPrefs.GetString("ProcessedLobbyId", "");
        if (!lobbyId.IsValid() || lobbyId.ToString() == processedLobbyId) return;
        
        EventBus.TriggerEvent(EventBusEnum.EventName.CLIENT_JOIN_LOBBY, lobbyId);
        PlayerPrefs.SetString("ProcessedLobbyId", lobbyId.ToString());
        PlayerPrefs.Save();
    }
#endif

    public static void LogServerConfig()
    {
        var serverConfig = MultiplayService.Instance.ServerConfig;
        Debug.Log($"Server ID[{serverConfig.ServerId}]");
        Debug.Log($"AllocationID[{serverConfig.AllocationId}]");
        Debug.Log($"Port[{serverConfig.Port}]");
        Debug.Log($"QueryPort[{serverConfig.QueryPort}");
        Debug.Log($"LogDirectory[{serverConfig.ServerLogDirectory}]");
    }

    private void Update()
    {
        if (!IsGameEnd && Input.GetKeyDown(KeyCode.Escape))
        {
            OnEscapeKeyPressed?.Invoke();
        }

        if (isServer)
        {
            // Debug.Log("ConnectedlcientsIds.Count = " + NetworkManager.Singleton.ConnectedClientsIds.Count);
            if (!isHost)
            {
                m_ServerQueryHandler.currentPlayers = (ushort)NetworkManager.ConnectedClientsIds.Count;
                m_ServerQueryHandler.UpdatePublic();
            }
           
            var cm = GetManager<CharacterManager>();
            if (!IsServerGameStarted && cm.NumberOfCharactersAlive > 0)
            // TODO: May be there is a better way to accurately capture the event of a game starting
            {
                IsServerGameStarted = true;
            }
            else if (IsServerGameStarted && NetworkManager.Singleton.ConnectedClientsIds.Count == 0)  // && cm.NumberOfTeamsAlive == 0)
            {
                // Debug.Log("***no need to do anything here now***");
                // EndMatch();
                if (EnvironmentSettings.Settings.IsWASD)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();    
#endif
                }
            }
           
            if (IsAllocated && !AllocationDone)
            {
                Debug.Log("Server is calling Example_ReadyingServer");
                Example_ReadyingServer();
                AllocationDone = true;
            }
            if (IsDeallocated && !DeallocationDone)
            {
                Debug.Log("Server is calling Example_UnreadyingServer");
                Example_UnreadyingServer();
                DeallocationDone = true;
            }
        }
        if ((!IsServer) && (pollingMatchmakingTicket))
        {
            if (createTicketResponse != null) {
                // Debug.Log("Has ticket... ");
                // Has ticket
                pollTicketTimer -= Time.deltaTime;
                if (pollTicketTimer <= 0f) {
                    pollTicketTimer = pollTicketTimerMax;
                    Debug.Log("1s poll: " + Time.time);
                    PollMatchmakerTicket();
                }
            }
        }
    }

    public void StartOnlineServer()
    {
        IsOnline = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port = m_AppData.Port;
        Debug.Log("Port to use : " + NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port);
        NetworkManager.Singleton.StartServer();
    }

    private string GetNetworkPlayerId()
    {
        var networkPlayerId = string.Empty;
#if NOSTEAMWORKS
        var unityAccountId = AuthenticationService.Instance.PlayerId == null ? string.Empty : AuthenticationService.Instance.PlayerId;
        networkPlayerId = $"NonSteam_{Application.productName}_{unityAccountId}"; 
#else
        var steamAccountId = UserData.Me.id.ToString();
        networkPlayerId = steamAccountId;
#endif
        if (!IsLocalDevelopment) return networkPlayerId;
        
        return $"Local_{networkPlayerId}_{UnityEngine.Random.Range(0, 10000)}";

    }
    
    public void SetupOnlineGame(string playerName, CharacterData selectedCharacter, bool isServerHost, string lobbyId = "", bool isMultiplayerLobby = false, bool isTwoPlayerLobby = false)
    {
        IsOnline = true;
        Debug.Log($"Starting Client SetupOnlineGame at {address}:{portNumber}");
        // Temp fix for serializer thrown due to uid is null  
        var uid = AuthenticationService.Instance.PlayerId == null ? "" : AuthenticationService.Instance.PlayerId;
        Debug.Log("Unity PlayerID = " + uid);
        var rankingData = GetManager<LocalRankingManager>().RankingData;
        var networkPlayerId = GetNetworkPlayerId();
        var teamIndex = GetManager<SteamLobbyManager>().GetCurrentPlayerTeamIndex();
        GetManager<PlayersNetworkManager>().SetLocalPlayerData(uid, playerName, selectedCharacter, positionId, rankingData, networkPlayerId, teamIndex);
        if (IsLocalDevelopment)
        {   
            if (isServerHost)
            {
                NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
                NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port = portNumber;
                Debug.Log("payloadUserID = " + networkPlayerId);
                string payload = JsonUtility.ToJson(new ConnectionPayload(networkPlayerId, Application.version, lobbyId, 0));
                byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
                Debug.Log("payload = " + System.Text.Encoding.UTF8.GetString(payloadBytes));
                NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes; //would make sense to add password to this
                NetworkManager.Singleton.StartHost();
            }
            else
            {
                // For Code Jam, to get some sort of ID for that player,
                // so he won't be able to call the Easter Egg twice
                /* InitializationOptions initializationOptions = new InitializationOptions();
                initializationOptions.SetProfile(UnityEngine.Random.Range(0, 10000).ToString());
                await UnityServices.InitializeAsync(initializationOptions);
                Debug.Log("AuthenticationService.Instance.IsSignedIn: " + AuthenticationService.Instance.IsSignedIn);
                await SignInAnonymouslyAsync(); 
                */
                // end of Code Jam
                EventBus.TriggerEvent(EventBusEnum.EventName.SERVER_FOUND_FOR_LOBBY, address + ":" + portNumber);
                ConnectToHost(portNumber, address, lobbyId);
            }
        }
        else
        {
            if (isMultiplayerLobby)
            {
                steamLobbyId = lobbyId;
                FindMatch(new MatchmakingParam{
                    LobbyId = lobbyId, 
                    TwoPlayerLobby = false, 
                    MultiPlayerLobby = true,
                    PlayerArenaGroup = rankingData.ArenaGroup
                });
            }
            else if (isTwoPlayerLobby)
            {
                steamLobbyId = lobbyId;
                FindMatch(new MatchmakingParam{
                    LobbyId = lobbyId, 
                    TwoPlayerLobby = true, 
                    MultiPlayerLobby = false,
                    PlayerArenaGroup = rankingData.ArenaGroup
                });
            }
            else
            {
                FindMatch(new MatchmakingParam{
                    LobbyId = lobbyId, 
                    PlayerArenaGroup = rankingData.ArenaGroup
                });
            }
        }
        
    }

    public void LobbyGuestSetupOnlineGame(string playerName, CharacterData selectedCharacter, string lobbyId, string ipAddress, ushort portNumber)
    {
        Debug.Log("starting Client LobbyGuestSetupOnlineGame");
        // Temp fix for serializer thrown due to uid is null  
        var uid = AuthenticationService.Instance.PlayerId == null ? "" : AuthenticationService.Instance.PlayerId;
        Debug.Log("Unity PlayerID = " + uid);
        var rankingData = GetManager<LocalRankingManager>().RankingData;
        var teamIndex = GetManager<SteamLobbyManager>().GetCurrentPlayerTeamIndex();
        GetManager<PlayersNetworkManager>().SetLocalPlayerData(uid, playerName, selectedCharacter, -1, rankingData, GetNetworkPlayerId(), teamIndex);
        ConnectToHost(portNumber, ipAddress, lobbyId);
    }

    public void ConnectToHost(ushort portNumber, string ipAddress, string lobbyId)
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port = portNumber;
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = ipAddress;
        Debug.Log("Connecting to " + NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address + ":" + NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port);
        var payloadUserID = GetNetworkPlayerId();
        Debug.Log("payloadUserID = " + payloadUserID); 
        string payload = JsonUtility.ToJson(new ConnectionPayload(payloadUserID, Application.version, lobbyId, -1));
        byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
        Debug.Log("payload = " + System.Text.Encoding.UTF8.GetString(payloadBytes));
        // System.Text.Encoding.ASCII.GetBytes("room password");
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes; //would make sense to add password to this
        var result = NetworkManager.Singleton.StartClient();
        // TODO:  need to parse the result variable, probably easier to identify the correct / positive case
        if (!result)
        {
            AnalyticsManager.Instance.SendData(AnalyticsManager.EventType.Error, "NetworkManager.Singleton.StartClient() failed", AnalyticsManager.ErrorType.JoinGameServerFailed);
        }
        else
        {
            StartCoroutine(WaitAndCheckIfMatchStarted());
        }
    }
    
    // TODO: To test
    private IEnumerator WaitAndCheckIfMatchStarted()
    {
        yield return new WaitForSeconds(60);
        if (IsMatchActive)
        {
            Debug.Log("Client match has been started");            
        }
        else
        {
            var errorMsg = "Client match not started after connected to game server";
            Debug.LogError(errorMsg);
            AnalyticsManager.Instance.SendData(AnalyticsManager.EventType.Error, errorMsg, AnalyticsManager.ErrorType.StartMatchFailed);
        }
    }

    private int SetGameArenaGroup(List<int> playerArenaGroups, bool isPrivateLobby)
    {
        var targetArenaGroup = isPrivateLobby ? playerArenaGroups.Max() : (int)Math.Ceiling(playerArenaGroups.Average());
        SetGameArenaGroup(targetArenaGroup);
        return targetArenaGroup;
    }

    private void SetGameArenaGroup(int arenaGroup)
    {
        ArenaGroup = arenaGroup;
        if (isServer)
        {
            SetGameArenaGroupClientRpc(ArenaGroup);
        }
    }
    private async void FindMatch(MatchmakingParam param)
    {
        var lobbyId = param.LobbyId;
        var twoPlayerLobby = param.TwoPlayerLobby;
        var multiPlayerLobby = param.MultiPlayerLobby;
        
        waitingForPlayerText.text = "Matchmaking...";
        var matchmakingType = multiPlayerLobby
            ? EnvironmentSettings.MatchmakingType.Private
            : EnvironmentSettings.MatchmakingType.Public;
        var queueName = EnvironmentSettings.Settings.GetMatchmakingQueue(matchmakingType);
        Debug.Log("FindMatch with lobbyId: " + lobbyId + " queue: " + queueName);
        if (multiPlayerLobby || twoPlayerLobby)
        {
#if !NOSTEAMWORKS 
            var steamLobbyMembers = GetManager<SteamLobbyManager>().Members;
            List<Unity.Services.Matchmaker.Models.Player> matchmakingPlayerDataForMembers = new List<Unity.Services.Matchmaker.Models.Player>();
            var arenaGroupArray = steamLobbyMembers.Select(member =>
            {
                try
                {
                    return Int32.Parse(member.ArenaGroup);
                }
                catch (Exception e)
                {
                    Debug.Log($"Invalid arena group {member.ArenaGroup} for lobby member {member.SteamUserData.Nickname}");
                    return 0;
                }
            }).ToList();
            var gameArenaGroup = SetGameArenaGroup(arenaGroupArray, multiPlayerLobby);
            Debug.Log("Matchmaking gameArenaGroup = " + gameArenaGroup);
            foreach (var member in steamLobbyMembers)
            {
                matchmakingPlayerDataForMembers.Add(
                    new Unity.Services.Matchmaker.Models.Player(member.UnityAccountId, 
                        new MatchmakingPlayerData {
                            LobbyId = lobbyId,
                            AppVer = Application.version,
                            ArenaGroup = gameArenaGroup,
                            TeamIndex = member.TeamIndex,
                        }
                        )
                    );
            }

            try
            {
                createTicketResponse = await MatchmakerService.Instance.CreateTicketAsync(
                    matchmakingPlayerDataForMembers, 
                    new CreateTicketOptions { QueueName = queueName });
                EventBus.TriggerEvent(EventBusEnum.EventName.CLIENT_CREATE_MATCHMAKER_TICKET_SUCCESS);
            }
            catch (Exception e)
            {
                // TODO: Test
                var message = e.Message;
                var errorCode = AnalyticsManager.ErrorType.CreateMatchmakerTicketFailed;
                Debug.LogError($"[{errorCode}][CreateMatchmakerTicketFromLobby] - {message}");
                AnalyticsManager.Instance.SendData(AnalyticsManager.EventType.Error, message, errorCode);          
            }
#endif
        } 
        else
        {
            try
            {
                createTicketResponse = await MatchmakerService.Instance.CreateTicketAsync(new List<Unity.Services.Matchmaker.Models.Player> {
                    new Unity.Services.Matchmaker.Models.Player(AuthenticationService.Instance.PlayerId, 
                        new MatchmakingPlayerData {
                            LobbyId = lobbyId,
                            AppVer = Application.version,
                            ArenaGroup = param.PlayerArenaGroup
                        })
                }, new CreateTicketOptions { QueueName = queueName });
                EventBus.TriggerEvent(EventBusEnum.EventName.CLIENT_CREATE_MATCHMAKER_TICKET_SUCCESS);
            }
            catch (Exception e)
            {
                // TODO: Test
                var message = e.Message;
                var errorCode = AnalyticsManager.ErrorType.CreateMatchmakerTicketFailed;
                Debug.LogError($"[{errorCode}][CreateMatchmakerTicketFromPublicMatch] - {message}");
                AnalyticsManager.Instance.SendData(AnalyticsManager.EventType.Error, message, errorCode);
            }
        }
        // Wait a bit, don't poll right away
        pollTicketTimer = pollTicketTimerMax;
        pollingMatchmakingTicket = true;
    }

    public void StopMatchmaking()
    {
        if (pollingMatchmakingTicket)
        {
            Debug.Log("Stopping Matchmaking Tickets");
            Task.Run(async () =>
            {
                try
                {
                    await MatchmakerService.Instance.DeleteTicketAsync(createTicketResponse.Id);
                    Debug.Log("Stopped Matchmaking Tickets");
                }
                catch (MatchmakerServiceException ex)
                {
                    Debug.LogException(ex);
                    AnalyticsManager.Instance.SendData(AnalyticsManager.EventType.Error, ex.Message, AnalyticsManager.ErrorType.CancelMatchmakerTicketFailed);
                }
            });
        }
        pollingMatchmakingTicket = false;
    }
    
    private async Task InitializeUnityAuthentication() 
    {
        Debug.Log("UnityServices.State: " + UnityServices.State);
        if (UnityServices.State != ServicesInitializationState.Initialized) {
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}"); 
            InitializationOptions initializationOptions = new InitializationOptions();
            initializationOptions.SetProfile(UnityEngine.Random.Range(0, 10000).ToString());
            await UnityServices.InitializeAsync(initializationOptions);
            Debug.Log("UnityServices.InitializeAsync done");
        }
        Debug.Log("AuthenticationService.Instance.IsSignedIn Before: " + AuthenticationService.Instance.IsSignedIn);
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await SignInAnonymouslyAsync();
        }
        Debug.Log("AuthenticationService.Instance.IsSignedIn After: " + AuthenticationService.Instance.IsSignedIn);
    }

    private async Task SignInAnonymouslyAsync()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in anonymously succeeded!");
            Debug.Log("here4.2 AuthenticationService.Instance.IsSignedIn: " + AuthenticationService.Instance.IsSignedIn);
            
            // Shows how to get the playerID
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}"); 

        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
         }
    }

    // Method used by Client only
    private async void PollMatchmakerTicket()
    {
        if (!pollingMatchmakingTicket)
        {
            Debug.Log("Matchmaker Ticket Polling has been cancelled");
            return;   
        }
        
        Debug.Log("PollMatchmakerTicket");
        TicketStatusResponse ticketStatusResponse = null;
        try
        {
            ticketStatusResponse = await MatchmakerService.Instance.GetTicketAsync(createTicketResponse.Id);
        }
        catch (Exception e)
        {
            var errorMessage = "Polling matchmaking ticket failed, error = " + e.Message;
            Debug.LogError(errorMessage);
            pollingMatchmakingTicket = false; // Stop polling when failed
            AnalyticsManager.Instance.SendData(AnalyticsManager.EventType.Error, errorMessage, AnalyticsManager.ErrorType.PollingMatchmakerTicketFailed);
            return;
        }
        

        if (ticketStatusResponse == null) {
            Debug.Log("Null means no updates to this ticket, keep waiting");
            return;
        }

        // Not null means there is an update to the ticket
        if (ticketStatusResponse.Type != typeof(MultiplayAssignment))
        {
            var errorMessage = "Polling matchmaking ticket failed, error = invalid ticket assignment type " +
                               ticketStatusResponse.Type;
            Debug.LogError(errorMessage);
            pollingMatchmakingTicket = false; // Stop polling due to invalid ticket assignment
            AnalyticsManager.Instance.SendData(AnalyticsManager.EventType.Error, errorMessage, AnalyticsManager.ErrorType.PollingMatchmakerTicketError);
            return;
        }
        
        MultiplayAssignment multiplayAssignment = ticketStatusResponse.Value as MultiplayAssignment;

        Debug.Log("multiplayAssignment.Status " + multiplayAssignment.Status);
        waitingForPlayerText.text = "Match " + multiplayAssignment.Status;
        switch (multiplayAssignment.Status) {
            case MultiplayAssignment.StatusOptions.Found:
                createTicketResponse = null;
                pollingMatchmakingTicket = false;

                Debug.Log(multiplayAssignment.Ip + " " + multiplayAssignment.Port);

                string ipAddress = multiplayAssignment.Ip;
                ushort port = (ushort)multiplayAssignment.Port;

                EventBus.TriggerEvent(EventBusEnum.EventName.SERVER_FOUND_FOR_LOBBY, ipAddress + ":" + port.ToString());

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipAddress, port);
                // TODO: use a JSON string for a shared ConnectionPayLoad object
                var processedLobbyId = PlayerPrefs.GetString("ProcessedLobbyId", "");
                Debug.Log("ProcessedLobbyId: " + processedLobbyId);
                Debug.Log("steamLobbyId: " + steamLobbyId);
                ConnectToHost(port, ipAddress, steamLobbyId);
                //  string payload = JsonUtility.ToJson(new ConnectionPayload(UserData.Me.id.ToString(), Application.version, steamLobbyId, positionId));
                // byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
                // Debug.Log("payload = " + System.Text.Encoding.UTF8.GetString(payloadBytes));
                // System.Text.Encoding.ASCII.GetBytes("room password");
                // NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes; //would make sense to add password to this
                // NetworkManager.Singleton.StartClient();

                break;
            case MultiplayAssignment.StatusOptions.InProgress:
                pollingMatchmakingTicket = true;
                if (EnvironmentSettings.Settings.IsWASD)
                {
                    waitingForPlayerText.text = "Waiting for 4 WASD players to start the game, please stand by";

                } else
                {
                    // GameTester.gg
                    waitingForPlayerText.text = "Waiting for players...";
                }
                // Still waiting...
                break;
            case MultiplayAssignment.StatusOptions.Failed:
                createTicketResponse = null;
                pollingMatchmakingTicket = false;
                var msg = "Failed to create Multi-play server!";
                Debug.LogError(msg);
                waitingForPlayerText.text = "Matchmaking failed, please try again";
                // TODO
                // EventBus.TriggerEvent(EventBusEnum.EventName.RESET_MAIN_MENU);
                // lookingForMatchTransform.gameObject.SetActive(false);
                AnalyticsManager.Instance.SendData(AnalyticsManager.EventType.Error, msg, AnalyticsManager.ErrorType.AllocateGameServerFailed);
                break;
            case MultiplayAssignment.StatusOptions.Timeout:
                createTicketResponse = null;
                pollingMatchmakingTicket = false;
                var errorMsg = "Multi-play Timeout!";
                Debug.LogError(errorMsg);
                waitingForPlayerText.text = "Matchmaking timeout, please try again";
                AnalyticsManager.Instance.SendData(AnalyticsManager.EventType.Error, errorMsg, AnalyticsManager.ErrorType.AllocateGameServerTimeout);
                // TODO
                // EventBus.TriggerEvent(EventBusEnum.EventName.RESET_MAIN_MENU);
                // lookingForMatchTransform.gameObject.SetActive(false);
                break;
        }

    }
    
    // Method used by Server only
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        Debug.Log("ApprovalCheck running");
        if (IsServerGameStarted)
        {
            response.Approved = false;
            var msg = "Game already in progress";
            response.Reason = msg;
            AnalyticsManager.Instance.SendData(AnalyticsManager.EventType.Error, msg, AnalyticsManager.ErrorType.ApprovalCheckFailedServerHasStarted);
            return;
        }        
        if (GetManager<PlayersNetworkManager>().NumberOfPlayers.Value == GlobalGameSettings.Settings.MaxPlayerAmount)
        {
            response.Approved = false;
            var msg = "Server Full";
            response.Reason = msg;
            AnalyticsManager.Instance.SendData(AnalyticsManager.EventType.Error, msg, AnalyticsManager.ErrorType.ApprovalCheckFailedServerFull);
            return;
        }
        // string clientAppVersion =  System.Text.Encoding.UTF8.GetString(request.Payload);
        string payloadString = System.Text.Encoding.UTF8.GetString(request.Payload);
        Debug.Log( "Payload receiverd: " + payloadString);
        ConnectionPayload payload = JsonUtility.FromJson<ConnectionPayload>(payloadString);
        Debug.Log("received clientAppVersion: " + payload.ToString());
        if  (payload.AppVersion != Application.version)
        {
            response.Approved = false;
            var msg = "Incorrect Client Version";
            response.Reason = msg;
            AnalyticsManager.Instance.SendData(AnalyticsManager.EventType.Error, $"{msg}: {payload.AppVersion}", AnalyticsManager.ErrorType.ApprovalCheckFailedInvalidClientVersion);
            return;
        }

        // TODO: Should really be done at StartOnlineGame, bnecause some players may have dropped out while waiting
        // pendingJoinersInfo.Add(payload.SteamID, payload.LobbyID);
        response.Approved = true;
    }

    // Server only 
    public void StartOnlineGame(int gameArenaGroup)
    {
        IsOnline = true;
        SetGameArenaGroup(gameArenaGroup);
        GetManager<CharacterManager>().CreateOnlineCharacters();
        GetManager<RoundManager>().StartGame();
        if (!DisableRP)
        {
            currentTeams = new List<List<Character>>();
            foreach ( List<Character> characterList in GetManager<CharacterManager>().Teams) 
            {
                List<Character> newCharacterList = new List<Character>();
                foreach ( Character jtem in characterList )
                {
                    newCharacterList.Add(jtem);
                }
                currentTeams.Add(newCharacterList);
            }
            currentTeamCount = GetManager<CharacterManager>().NumberOfTeamsAlive;
            teamLosingOrder = new List<Character>[currentTeamCount];
        }
        IsMatchActive = true;
        StartMatchClientRpc();
        Debug.Log("Online game started");
    }

    [ClientRpc]
    private void SetGameArenaGroupClientRpc(int arenaGroup)
    {
        ArenaGroup = arenaGroup;
    }

    [ClientRpc]
    private void StartMatchClientRpc()
    {
        IsMatchActive = true;
        EventBus.TriggerEvent(EventBusEnum.EventName.GAME_STARTED_CLIENT);
    }

    public void StartOfflineGame(string playerName, CharacterData selectedCharacter, bool isTrainingMode = false)
    {


        IsOnline = false;
        IsMatchActive = true;
        IsTrainingMode = isTrainingMode;
        offlineGamePlayerData = new PlayerData()
        {
            Uid = "Local", 
            DisplayName = playerName, 
            SelectedCharacterId = selectedCharacter.InternalID, 
            PositionId = 0, 
            SteamId = "Offline_Host",
            TeamIndex = -1,
        };
        NetworkManager.Singleton.OnClientConnectedCallback += OnLocalHostConnected;
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 9000);
        NetworkManager.Singleton.StartHost();
        if(isTrainingMode)
        {
            Debug.Log("Training game started");
        }
    }

    private void OnLocalHostConnected(ulong id)
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnLocalHostConnected;
        offlineGamePlayerData.Id = NetworkManager.Singleton.LocalClientId;
        int offlineGamePlayers = IsTrainingMode ? GlobalGameSettings.Settings.TrainingModePlayersCount : PlayersToStartOfflineGame;
        GetManager<CharacterManager>().CreateOfflineCharacters(offlineGamePlayerData, offlineGamePlayers);
        GetManager<RoundManager>().StartGame();
        EventBus.TriggerEvent(EventBusEnum.EventName.GAME_STARTED_CLIENT);
    }


    private void EndMatchAndLoadMainScene()
    {
        EndMatch();
    }

    public void EndMatch(string sceneName = "")
    {
        Debug.Log("EndMatch shutting down Network");
        // Server running this would not cut off the client connection properly

        NetworkManager.Singleton.Shutdown();
        
        IsMatchActive = false;
        if (autoDisconnectClientAfterMatchCoroutine != null)
        {
            StopCoroutine(autoDisconnectClientAfterMatchCoroutine);
            autoDisconnectClientAfterMatchCoroutine = null;
            waitForAutoQuitWinnerMenuTimer = 0;
        }

        var targetScene = string.IsNullOrEmpty(sceneName) ? SceneManager.GetActiveScene().name : sceneName; 
        SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
    }

    public static T GetManager<T>() where T : BaseManager
    {
        if(subManagersDictionary.TryGetValue(typeof(T), out var manager))
        {
            return manager as T;
        }

        Debug.LogError($"No submanager of type {typeof(T)} was found");
        return null;
    }

    private async void CheckForWinCondition(Character character)
    {
        var cm = GetManager<CharacterManager>();
        //If server needs to check anything for Game End, put it here
        if (isServer)
        {
            if (!DisableRP)
            {
                while (cm.NumberOfTeamsAlive < currentTeamCount)
                {
                    foreach ( List<Character> characterList in currentTeams )
                    {
                        bool newlyDeadTeam = true;
                        List<Character> tempCharacterList = new List<Character>();
                        if (characterList.Count > 0)
                        {
                            foreach ( Character characterVar in characterList)
                            {
                                if (!characterVar.IsDead)
                                {
                                    newlyDeadTeam = false;
                                }
                                else if (newlyDeadTeam)
                                {
                                    tempCharacterList.Add(characterVar);
                                }
                            }
                            if (newlyDeadTeam)
                            {
                                teamLosingOrder[currentTeamCount--] = tempCharacterList;
                                characterList.RemoveAll(character => character.IsDead);
                            }
                        }
                    }
                }
            }
            if (cm.NumberOfTeamsAlive <= 1 && !IsGameEnd)
            {
                if (!DisableRP)
                {
                    UpdatePlayersRPAfterGameEnd();
                }
                // pendingJoinersInfo = new Dictionary<string, string>();
                // Restart the game at the server to accept player connections for a new game
                if (!isHost)
                {
                    // if isHost, IsGameEnd will be set in the IsClient part below 
                    TriggerGameEnd();
                    
                }
                // The Boolean is needed for game server logic if the server does not auto shutdown after each game
                // IsServerGameStarted = false;
                
                // await m_Backfiller.StopBackfill();
                // Dispose();
                if (!EnvironmentSettings.Settings.IsWASD)
                {
                    if (autoDisconnectClientAfterMatchCoroutine == null)
                    {
                        autoDisconnectClientAfterMatchCoroutine = StartCoroutine(AutoDisconnectClientAfterMatchCoroutine());
                    }   
                }         
            }
        }
        if (IsClient)
        {
            Debug.Log("CheckForWinCondition IsClient path reached");
            if (cm.PlayerCharacter.TeamIndex == character.TeamIndex)
            {
                //check if everyone in own Team is on Dead state
                bool isTeamDead = true;
                foreach (var c in cm.Teams[character.TeamIndex])
                {
                    if (c.State.Value != CharacterState.Dead)
                    {
                        isTeamDead = false;
                    }
                }
                if (isTeamDead)
                {
                    GameLostEvent?.Invoke();
                    cm.PlayerCharacter.IsSpectating = true;
                    return;
                }
            }
            if (cm.NumberOfTeamsAlive == 1 && !IsGameEnd)
            {
                TriggerGameEnd();
                if (cm.PlayerCharacter.TeamIndex == cm.WinningTeamIndex)
                {
                    GameWonEvent?.Invoke();
                }
                else if(!cm.PlayerCharacter.IsSpectating)
                {
                    GameLostEvent?.Invoke();
                }
                else
                {
                    EndMatch();
                }
            }
        }
    }

    private void UpdatePlayersRPAfterGameEnd()
    {
        var hackyString = "[";
        for (int i = 0;  i < teamLosingOrder.Count(); i++)
        {
            // TODO: Handle Nemesis points correctly
            foreach ( Character teamMember in teamLosingOrder[i])
            {
                hackyString = hackyString + "{\"steam_id\" : \"" + teamMember.PlayerData.SteamId + "\", \"match_ranking\" : " + i + ", \"nemesis_points\" : " + "3" + " },";
            }
        }
        hackyString = hackyString[..^1];
        hackyString = hackyString + "]";
        // {"match_result" : [{"steam_id" : "ABC123", "match_ranking" : 1, "nemesis_points" : -3}, 
        //                    {"steam_id" : "ABC123D", "match_ranking" : 2, "nemesis_points" : 0}]}'
        Debug.Log("hackyString = " + hackyString);
        StartCoroutine(PostData("profiles/submitMatchResults", "{\"match_result\": " + hackyString + "}", result =>
        {
            if (result == 0)
            {
                Debug.Log("Data posted successfully!");
                // Handle success (e.g., load a new scene)
            }
            else
            {
                Debug.LogError("Error posting data.");
            }
        }));
    }

    private void TriggerGameEnd()
    {
        IsGameEnd = true;

        if (IsServer)
        {
            EventBus.TriggerEvent(EventBusEnum.EventName.GAME_END_SERVER);
        }
    }

    public override void OnDestroy()
    {
        if(Instance == this)
        {
            GetManager<CharacterManager>().CharacterStateChangedEvent -= CheckForWinCondition;

            Instance = null;
            subManagersDictionary = null;
        }
    }

    void OnApplicationQuit()
    {
        StopMatchmaking();
    }
   


    // #if UNITY_SERVER
    /// <summary>
    /// Ready the server. This is to indicate that the server is ready to accept players.
    /// Readiness is the server's way of saying it's ready for players to join the server.
    /// You must wait until you have been Allocated before you can call ReadyServerForPlayersAsync.
    /// </summary>
    private async void Example_ReadyingServer()
    {
        // After the server is back to a blank slate and ready to accept new players
        await MultiplayService.Instance.ReadyServerForPlayersAsync();
    }

    /// <summary>
    /// Unready the server. This is to indicate that the server is in some condition which means it can't accept players.
    /// For example, after a game has ended and you need to reset the server to prepare for a new match.
    /// </summary>
    private async void Example_UnreadyingServer()
    {
        // The match has ended and players are disconnected from the server
        await MultiplayService.Instance.UnreadyServerAsync();
    }
    //#endif


    // Used by server
    private IEnumerator AutoDisconnectClientAfterMatchCoroutine()
    {
        float timer = 15;
        while (waitForAutoQuitWinnerMenuTimer < timer)
        {
            yield return null;
            waitForAutoQuitWinnerMenuTimer += Time.deltaTime;
        }
        Debug.Log("Times up, Auto Disconnecting All clients...");
        if (isHost) // || (isServer && IsLocalDevelopment))
        {
            Debug.Log("EndMatch");
            EndMatch();
        }
        else // if (isServer)
        {
            // The ready Server step may be redundant now that we proceed to quit the application
            Example_ReadyingServer();
            // Debug.Log("Server Calling ForceDisconnectClientRpc");
            // Fail to invoke the RPC on client side this way
            // GetManager<PlayersNetworkManager>().ForceDisconnectClientRpc();
            Debug.Log("Closing Server");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();    
            #endif
            
        }
    }

    private IEnumerator PostData(string url, string json, System.Action<int> callback)
    {
        var uri = $"{EnvironmentSettings.Settings.GetAPIServerHost()}/{url}";
        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(uri, json))
        {
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError(www.error);
                callback.Invoke(1); // Error occurred
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
                callback.Invoke(0); // Success
            }
        }
    }
    
    public void DebugSetGameArenaGroup(int arenaGroup)
    {
        if (Debug.isDebugBuild)
        {
            DebugSetGameArenaGroupServerRPC(arenaGroup);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DebugSetGameArenaGroupServerRPC(int arenaGroup)
    {
        SetGameArenaGroup(arenaGroup);        
    }
}


