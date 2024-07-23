using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Nakama;


public class PlayersNakamaNetworkManager : MonoBehaviour
{

    #region FIELDS

    //private bool blockJoinsAndLeaves = false;

    #endregion

    #region EVENTS

    public event Action<List<PlayerNetworkData>> onPlayersReceived;
    public event Action<PlayerNetworkData> onPlayerJoined;
    public event Action<PlayerNetworkData> onPlayerLeft;
    public event Action<PlayerNetworkData, int> onLocalPlayerObtained;

    #endregion

    #region PROPERTIES

    public static PlayersNakamaNetworkManager Instance { get; private set; }
    public List<PlayerNetworkData> Players { get; private set; } = new List<PlayerNetworkData>();
    public int PlayersCount { get => Players?.Count(player => player != null) ?? 0; }
    public PlayerNetworkData CurrentPlayer { get; private set; } = null;
    public int CurrentPlayerNumber { get; private set; } = -1;
    #endregion

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
    }

    private void Start()
    {
        // NetworkManager.Instance.onMatchJoin += MatchJoined;
        // NetworkManager.Instance.onMatchLeave += ResetLeaved;
        NakamaNetworkManager.Instance.Subscribe(NakamaNetworkManager.Code.Players, SetPlayers);
        NakamaNetworkManager.Instance.Subscribe(NakamaNetworkManager.Code.PlayerJoined, PlayerJoined);
        NakamaNetworkManager.Instance.Subscribe(NakamaNetworkManager.Code.StartMatch, MatchStarted);
    }

    private void OnDestroy()
    {
        // NetworkManager.Instance.onMatchJoin -= MatchJoined;
        //NetworkManager.Instance.onMatchLeave -= ResetLeaved;
        NakamaNetworkManager.Instance.Unsubscribe(NakamaNetworkManager.Code.Players, SetPlayers);
        NakamaNetworkManager.Instance.Unsubscribe(NakamaNetworkManager.Code.PlayerJoined, PlayerJoined);
        NakamaNetworkManager.Instance.Unsubscribe(NakamaNetworkManager.Code.StartMatch, MatchStarted);
    }

    private void SetPlayers(MultiplayerMessage message)
    {
        Players = message.GetData<List<PlayerNetworkData>>();
        onPlayersReceived?.Invoke(Players);
        GetCurrentPlayer();
    }

    private void PlayerJoined(MultiplayerMessage message)
    {
        PlayerNetworkData player = message.GetData<PlayerNetworkData>();

        int index = Players.IndexOf(null);
        if (index > -1)
            Players[index] = player;
        else
            Players.Add(player);

        onPlayerJoined?.Invoke(player);
    }

    public void MatchStarted(MultiplayerMessage message)
    { 
        //blockJoinsAndLeaves = true;
        //UIManager.Instance.DisableExitBtn();
        GetCurrentPlayer();
        //SceneLoader.Instance.LoadLevel();
    }

    private void PlayersChanged(IMatchPresenceEvent matchPresenceEvent)
    {
        /*if (blockJoinsAndLeaves)
            return;*/
        foreach (IUserPresence userPresence in matchPresenceEvent.Leaves)
        {
            var toRemovedPlayer = Players.Find(player => player.Presence.SessionId == userPresence.SessionId);
            if (toRemovedPlayer != null)
            {
                RemovePlayer(toRemovedPlayer);
                onPlayerLeft?.Invoke(toRemovedPlayer);
            }
        }
    }

    public void MatchJoined()
    {
        NakamaNetworkManager.Instance.Socket.ReceivedMatchPresence += PlayersChanged;
        GetCurrentPlayer();
    }

    public void ResetLeaved()
    {
        if (NakamaNetworkManager.Instance && NakamaNetworkManager.Instance.Socket != null)
        {
            NakamaNetworkManager.Instance.Socket.ReceivedMatchPresence -= PlayersChanged;    
        }
        //blockJoinsAndLeaves = false;
        Players = null;
        CurrentPlayer = null;
        CurrentPlayerNumber = -1;
    }

    public void RemovePlayer(PlayerNetworkData player)
    {
        Players.Remove(player);
        GetCurrentPlayer();
       /* if (GameManager.Instance.GetGameStatus() == GameStatus.loading)
        {
            UIManager.Instance.ResetUIMatchMaking();
        }
        else PlaykenManager.Instance.CheckIfPlayerWon();*/
    }

    private void GetCurrentPlayer()
    {
        if (Players == null)
            return;

        if (NakamaNetworkManager.Instance.Self == null)
            return;

        if (CurrentPlayer != null)
            return;

        CurrentPlayer = Players.Find(player => player.Presence.SessionId == NakamaNetworkManager.Instance.Self.SessionId);
        CurrentPlayerNumber = Players.IndexOf(CurrentPlayer);
        onLocalPlayerObtained?.Invoke(CurrentPlayer, CurrentPlayerNumber);
    }
}
