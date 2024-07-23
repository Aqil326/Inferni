using Nakama;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class NakamaNetworkManager : MonoBehaviour
{
    public enum Code
    {
        Players = 0,
        PlayerJoined = 1,
        StartMatch = 5,    //Used only on server
        PlayerDie = 6,
        StartCasting = 7,
        PlayCard = 9,
        DiscardCard = 10,
        ApplyCardEffect = 11,
        DrawInitialHand = 12,
        DrawCard = 13,
        GainEnergy = 14,
        StartCombatRound = 15,
        StartDraftRound = 16,
        AddCardToDeck = 17,
        DraftPackCreated = 18,
        HealthUpdated = 19,
        ReceiveDraftPack = 20,
        SendDraftPack = 21,
        AddCardToHand = 22
    }
        
    #region FIELDS

    private const string JoinOrCreateMatchRpc = "JoinOrCreateMatchRpc";
    private const string LogFormat = "{0} with code {1}:\n{2}";
    private const string SendingDataLog = "Sending data";
    private const string ReceivedDataLog = "Received data";
    private const string UdidKey = "udid";

    [SerializeField] private float retryTime = 5f;
    [SerializeField] private int minPlayers = 4;
    [SerializeField] private int maxPlayers = 8;


    [SerializeField] private ConnectionData connectionData = null;
    public int queueDuration = 59;
    public int endQueueDuration = 3;
    [SerializeField] private bool enableLog = false;
    [SerializeField] private bool useDeviceId = false;

    private Dictionary<Code, Action<MultiplayerMessage>> onReceiveData = new Dictionary<Code, Action<MultiplayerMessage>>();

    private IClient client = null;
    private ISession session = null;
    private ISocket socket = null;
    private IMatch match = null;

    public ConnectionData ConnectionFileData { get => connectionData; }

    #endregion

    #region EVENTS

    public event Action onConnecting = null;
    public event Action onConnected = null;
    public event Action onDisconnected = null;
    public event Action onLoginSuccess = null;
    public event Action onLoginFail = null;

    public event Action onMatchJoin = null;
    public event Action onMatchLeave = null;

    public event Action<List<PlayerNetworkData>> onPlayersReceived;
    public event Action onMatchStarted;
    // public event Action onLocalTick = null;

    #endregion

    #region PROPERTIES

    public static NakamaNetworkManager Instance { get; private set; }
    public string Username { get => session == null ? string.Empty : session.Username; }
    public bool IsLoggedIn { get => socket != null && socket.IsConnected; }
    public ISocket Socket { get => socket; }
    public ISession Session { get => session; }
    public IClient Client { get => client; }
    public IUserPresence Self { get => match == null ? null : match.Self; }
    public bool IsOnMatch { get => match != null; }
    public int MinPlayers { get => minPlayers; }
    public int MaxPlayers { get => maxPlayers; }

    #endregion

    public string MatchId { get { return match.Id; } }

    /// <summary>
    /// Singleton definiton
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        StartTryLogin();
    }

    public void StartTryLogin()
    {
        onLoginFail += LoginFailed;
        TryLogin();
    }

    private void TryLogin()
    {
        if (useDeviceId)
        {
            LoginWithDevice();
            return;
        }

        LoginWithUdid();
    }

    private void LoginFailed()
    {
        Invoke(nameof(TryLogin), retryTime);
    }

    public void LoginWithUdid()
    {
        string udid;

        Debug.Log("Sign in in Nakama with a Random id");
        udid = Guid.NewGuid().ToString();
        
        //PlayerPrefs.SetString(UdidKey, udid);
        client = new Nakama.Client(connectionData.Scheme, connectionData.HostNakama, connectionData.PortNakama, connectionData.ServerKey, UnityWebRequestAdapter.Instance);
        LoginAsync(connectionData, client.AuthenticateCustomAsync(udid));
    }

    public void LoginWithDevice()
    {
        print(SystemInfo.deviceUniqueIdentifier);
        Debug.Log(SystemInfo.deviceUniqueIdentifier);
        client = new Nakama.Client(connectionData.Scheme, connectionData.HostNakama, connectionData.PortNakama, connectionData.ServerKey, UnityWebRequestAdapter.Instance);
        LoginAsync(connectionData, client.AuthenticateDeviceAsync(SystemInfo.deviceUniqueIdentifier));
    }

    private async void LoginAsync(ConnectionData connectionData, Task<ISession> sessionTask)
    {
        onConnecting?.Invoke();
        try
        {
            session = await sessionTask;
            socket = client.NewSocket(true);
            socket.Connected += Connected;
            socket.Closed += Disconnected;
            await socket.ConnectAsync(session);
            onLoginSuccess?.Invoke();

            Subscribe(Code.StartMatch, ReceivedMatchStart);
        }
        catch (Exception exception)
        {
            Debug.Log(exception);
            onLoginFail?.Invoke();
        }
    }

    public void ForceNoNetworking()
    {
        if (socket != null)
            socket.CloseAsync();
        //GameManager.Instance.isLogged = false;
        if (UnityWebRequestAdapter.Instance) Destroy(UnityWebRequestAdapter.Instance.gameObject);
        gameObject.SetActive(false);

    }

    private void Connected()
    {
        onConnected?.Invoke();
    }

    private void Disconnected()
    {
        //GameManager.Instance.isLogged = false;
        Debug.LogError("You are disconnected");
        onDisconnected?.Invoke();
    }

    public void OnLoginComplete()
    {
        //GameManager.Instance.isLogged = true;
        onLoginFail -= LoginFailed;
        //  UIManager.Instance.ShowPlayCanvas();
        Debug.Log("Login ready");
        //UIManager.Instance.SetStartBtnStatus();
    }

    private void OnApplicationQuit()
    {
        if (socket != null)
            socket.CloseAsync();
    }

    public void FindMatch(string localPlayerName, string characterId)
    {
        // if (localPlayerName != NakamaUserManager.Instance.DisplayName)
        NakamaUserManager.Instance.UpdateDisplayNameAndFindMatch(localPlayerName, characterId);
    }


    public void OnDestroy()
    {
        PlayersNakamaNetworkManager.Instance.onPlayerJoined -= PlayerJoined;
        PlayersNakamaNetworkManager.Instance.onPlayerLeft -= PlayerLeft;
        PlayersNakamaNetworkManager.Instance.onPlayersReceived -= PlayersReceived;
        Unsubscribe(Code.StartMatch, ReceivedMatchStart);
    }

    public async void JoinMatchAsync()
    {
        Socket.ReceivedMatchState -= Receive;
        Socket.ReceivedMatchState += Receive;
        onDisconnected += Disconnected;
        IApiRpc rpcResult = await SendRPC(JoinOrCreateMatchRpc);
        string matchId = rpcResult.Payload;
        match = await Socket.JoinMatchAsync(matchId);
        onMatchJoin?.Invoke();
    }

    private void Receive(IMatchState newState)
    {
        if (enableLog)
        {
            var encoding = System.Text.Encoding.UTF8;
            var json = encoding.GetString(newState.State);
            LogData(ReceivedDataLog, newState.OpCode, json);
        }

        MultiplayerMessage multiplayerMessage = new MultiplayerMessage(newState);
        if (onReceiveData.ContainsKey(multiplayerMessage.DataCode))
            onReceiveData[multiplayerMessage.DataCode]?.Invoke(multiplayerMessage);
    }

    public void Subscribe(Code code, Action<MultiplayerMessage> action)
    {
        if (!onReceiveData.ContainsKey(code))
            onReceiveData.Add(code, null);

        onReceiveData[code] += action;
    }

    public void Unsubscribe(Code code, Action<MultiplayerMessage> action)
    {
        if (onReceiveData.ContainsKey(code))
            onReceiveData[code] -= action;
    }

    public async Task<IApiRpc> SendRPC(string rpc, string payload = "{}")
    {
        if (client == null || session == null)
            return null;

        return await client.RpcAsync(session, rpc, payload);
    }

    public void JoinedMatch()
    {
        onMatchLeave += LeftMatch;
        onMatchJoin -= JoinedMatch;
        onMatchLeave += PlayersNakamaNetworkManager.Instance.ResetLeaved;
        onMatchJoin -= PlayersNakamaNetworkManager.Instance.MatchJoined;
    }

    public void LeaveMatch()
    {
        LeaveMatchAsync();
    }

    private void LeftMatch()
    {
        onMatchLeave -= LeftMatch;
        onMatchLeave -= PlayersNakamaNetworkManager.Instance.ResetLeaved;
        //UIManager.Instance.ShowPlayCanvas()
    }

    public async void LeaveMatchAsync()
    {
        onDisconnected -= Disconnected;
        Socket.ReceivedMatchState -= Receive;
        await Socket.LeaveMatchAsync(match);
        match = null;
        onMatchLeave?.Invoke();
    }

    public void Send(Code code, object data = null)
    {
        if (match == null)
            return;

        string json = data != null ? data.Serialize() : string.Empty;
        if (enableLog)
            LogData(SendingDataLog, (long)code, json);

        Socket.SendMatchStateAsync(match.Id, (long)code, json);
    }

    public void PlayersReceived(List<PlayerNetworkData> players)
    {
        onPlayersReceived?.Invoke(players);
    }

    public void PlayerLeft(PlayerNetworkData player)
    {

        /*if (GameManager.Instance.GetGameStatus() < GameStatus.levelReady)
        {
            UpdateStatus();
        }
        else if (PlaykenManager.Instance)
        {
            PlaykenManager.Instance.RemovePlaykenById(player.Presence.SessionId);
        }*/
    }

    public void PlayerJoined(PlayerNetworkData player)
    {
        UpdateStatus();
    }

    public void UpdateStatus()
    {
        // Debug.Log("Update status: "+ PlayersNetworkManager.Instance.PlayersCount);
        //UIManager.Instance.UpdatePlayersMatchMaking();

    }


    private void ReceivedMatchStart(MultiplayerMessage message)
    {
        //GameManager.Instance.SetupOnlineGame();
        onMatchStarted?.Invoke();
    }

    private void LogData(string description, long dataCode, string json)
    {
        Debug.Log(string.Format(LogFormat, description, (Code)dataCode, json));
    }

}
