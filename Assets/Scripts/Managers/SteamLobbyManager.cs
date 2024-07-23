using System;
using System.Linq;
using System.Collections.Generic;
using Steamworks;
using HeathenEngineering.SteamworksIntegration;

using Unity.Services.Authentication;

using UnityEngine;

enum MessageKeyPrefixEnum
{
    ArenaGroup,
    Character,
    Ready,
    StartUgsMatchMaking,
    GuestUnityId,
    UgsLobbyIp,
    UgsLobbyPort,
    TeamIndex
}

public class LobbyMember
{  
    public UserData SteamUserData;
    public string Character;
    public bool IsSelf;
    public bool IsLobbyOwner;
    public bool IsReady;
    public string UnityAccountId;
    public string ArenaGroup;
    public int TeamIndex = -1;
}

public class SteamLobbyManager : BaseManager
{
    [SerializeField]
    private int maxLobbyMember;
    [SerializeField]
    private int[] validMemberAmount;
    
    // Logic       
    private CSteamID currentLobbyId;
    private CSteamID lobbyOwnerId;
    private CSteamID invitedFriendId;
    public List<LobbyMember> Members = new List<LobbyMember>();
   
    // Events
    public Action<CSteamID> OnLobbyIdChange;
    public Action<CSteamID> OnLobbyOwnerChanged;
    public Action OnMatchmakingCreated;
    public Action<List<LobbyMember>> OnMembersChanged;
    public Action<LobbyMember> OnMemberDataChanged;
    // Variables
    private bool isInitialized;
    private Callback<LobbyChatUpdate_t> lobbyChatUpdateCallback;
    private LocalPlayerManager localPlayerManager;
    
    public bool IsMatchmakingStarted = false; // Will be reset when enter another lobby
    private bool matchFound = false; // Will be reset when enter another lobby

    public int GetCurrentPlayerTeamIndex()
    {
#if !NOSTEAMWORKS
        var self = FindSelfInLobby();
        return self?.TeamIndex ?? -1;
#else
    return -1;
#endif
    }
    
 #if !NOSTEAMWORKS 
    private void Awake()
    {  
        if (!SteamManager.Initialized) return;
        
        Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        OnLobbyIdChange += UpdateCurrentLobbyOwner;
        EventBus.StartListening<string>(EventBusEnum.EventName.SERVER_FOUND_FOR_LOBBY, OnMatchmakingFound);
    }

    public override void OnDestroy()
    {
        OnLobbyIdChange -= UpdateCurrentLobbyOwner;
    }
    
    private void Update()
    {
        if (!isInitialized || Members == null || Members.Count == 0) return;
        
        foreach (var member in Members)
        {
            UpdateMemberLobbyData(member);
        }
        MatchmakingCreatedCheck();
        MatchmakingFoundCheck();
    }
    
    public void Init(CSteamID lobbyId, LocalPlayerManager localManager)
    {
        isInitialized = true;
        localPlayerManager = localManager;
        if (lobbyId.IsValid())
        {
            SteamMatchmaking.JoinLobby(lobbyId);    
        }
        else
        {
            CheckForExistingLobby();    
        }
    }

    private string GetCurrentPlayerArenaGroup()
    {
        return GameManager.GetManager<LocalRankingManager>().RankingData.ArenaGroup.ToString();
    }

    private LobbyMember FindMember(CSteamID memberId)
    {
        try
        {
            return Members.Find(member => member.SteamUserData.id.Equals(memberId));
        }
        catch (Exception)
        {
            Debug.Log($"Member {memberId} not found");
            return null;
        }
    }

    private void UpdateCurrentLobbyOwner(CSteamID lobbyId)
    {
        UpdateLobbyOwner();
    }

    private void UpdateLobbyOwner()
    {
        if (!currentLobbyId.IsValid()) return;
        
        var currentOwnerId = SteamMatchmaking.GetLobbyOwner(currentLobbyId);
        if (currentOwnerId.Equals(lobbyOwnerId)) return;
        
        lobbyOwnerId = currentOwnerId;
        OnLobbyOwnerChanged?.Invoke(lobbyOwnerId);
    }

    public bool IsLobbyOwner(CSteamID? playerId = null)
    {
        var targetPlayerId = playerId ?? UserData.Me.id; 
        return lobbyOwnerId.Equals(targetPlayerId);
    }

    public LobbyMember FindSelfInLobby()
    {
        var selfSteamId = UserData.Me.id;
        return !selfSteamId.IsValid() || Members.Count == 0? 
            null : 
            Members.Find(member => member.SteamUserData.id.Equals(selfSteamId));
    }

    public bool IsGameReadyToStart()
    {
        return validMemberAmount.Contains(Members.Count) && Members.All(member => member.IsReady);
    }

    private void CheckForExistingLobby()
    {
        var lobbyId = SteamMatchmaking.GetLobbyByIndex(0);
        if (lobbyId.IsValid())
        {
            currentLobbyId = lobbyId;
            OnLobbyIdChange?.Invoke(currentLobbyId);
            UpdateLobbyMembers();
        }
        else
        {
            CreateLobby();
        }
    }

    private void UpdateLobbyMembers()
    {
        if (!currentLobbyId.IsValid() || !currentLobbyId.IsLobby()) return;
        
        var numMembers = SteamMatchmaking.GetNumLobbyMembers(currentLobbyId);
        var currentMembers = new List<LobbyMember>();
        var currentLocalCharacter = localPlayerManager ? localPlayerManager.SelectedCharacter.Name : "";
        LobbyMember selfMember = null;
        
        for (var i = 0; i < numMembers; i++)
        {
            var memberId = SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyId, i);
            var isSelf = memberId.Equals(UserData.Me.id);
            var member = new LobbyMember
            {
                SteamUserData = new UserData
                {
                    id = memberId,
                },
                IsSelf = isSelf,
                IsLobbyOwner = IsLobbyOwner(memberId),
                Character = isSelf ? currentLocalCharacter : string.Empty
            };
            if (member.IsSelf)
            {
                selfMember = member;
            }
            currentMembers.Add(member);
        }
        AssignTeams(currentMembers);
        Members = currentMembers;
        OnMembersChanged?.Invoke(Members);
        
        if (selfMember != null)
        {
            SendCharacterMessage(currentLocalCharacter);
            if (selfMember.IsLobbyOwner)
            {
                // Automatically set host status to be ready
                SendReadyStatusMessage(true);    
            }    
        }
    }

    private void AssignTeams(List<LobbyMember> lobbyMembers)
    {
        int teamSize = GlobalGameSettings.Settings.TeamSize;
        List<int> availableTeamIndexes = new List<int>();
        if (lobbyMembers.Count < teamSize * 2)
        {
            teamSize = 1;
        }

        for (int i = 0; i < lobbyMembers.Count; i++)
        {
            availableTeamIndexes.Add(i / teamSize);
        }
        
        foreach (var member in lobbyMembers)
        {
            int currentTeam = GetMemberTeamIndex(member.SteamUserData.id);
            if(currentTeam == -1 || !availableTeamIndexes.Contains(currentTeam))
            {
                member.TeamIndex = availableTeamIndexes[0];
                availableTeamIndexes.RemoveAt(0);
                TrySendTeamIndexMessage(member.SteamUserData.id, member.TeamIndex);
            }
            else
            {
                member.TeamIndex = currentTeam;
                availableTeamIndexes.Remove(currentTeam);
            }
        }
    }

    private void UpdateMemberLobbyData(LobbyMember targetMember)
    {
        var memberId = targetMember.SteamUserData.id;
        var isMemberUpdated = false;
        
        var readyStatus = GetMemberReadyStatus(memberId);
        if (targetMember.IsReady != readyStatus)
        {
            targetMember.IsReady = readyStatus;
            isMemberUpdated = true;
        }
        
        var character = GetMemberCharacter(memberId);
        if (targetMember.Character != character)
        {
            targetMember.Character = character;
            isMemberUpdated = true;
        }
        
        var unityAccountId = GetMemberUnityAccountId(memberId);
        if (targetMember.UnityAccountId != unityAccountId)
        {
            targetMember.UnityAccountId = unityAccountId;
            isMemberUpdated = true;
        }
        
        var arenaGroup = GetMemberArenaGroup(memberId);
        if (targetMember.ArenaGroup != arenaGroup)
        {
            targetMember.ArenaGroup = arenaGroup;
            isMemberUpdated = true;
        }

        var teamIndex = GetMemberTeamIndex(memberId);
        if(targetMember.TeamIndex != teamIndex)
        {
            targetMember.TeamIndex = teamIndex;
            isMemberUpdated = true;
        }

        if (isMemberUpdated)
        {
            OnMemberDataChanged?.Invoke(targetMember);    
        }
    }

    #region Matchmaking
    private void OnMatchmakingFound(string socket)
    {
        if (IsLobbyOwner()) 
        {
            Debug.Log("Setting Lobby's Inferni Server address to: " + socket);
            string[] socketSplit = socket.Split(':');
            string ip = socketSplit[0];
            ushort port;
            ushort.TryParse(socketSplit[1], out port);
            var key = MessageKeyPrefixEnum.UgsLobbyIp.ToString();
            SteamMatchmaking.SetLobbyData(currentLobbyId, key, ip);
            key = MessageKeyPrefixEnum.UgsLobbyPort.ToString();
            SteamMatchmaking.SetLobbyData(currentLobbyId, key, port.ToString());
        }
    }
    private void MatchmakingCreatedCheck()
    {
        if (matchFound) return;
        
        var startMatchmaking = SteamMatchmaking.GetLobbyData(currentLobbyId, MessageKeyPrefixEnum.StartUgsMatchMaking.ToString());
        if (startMatchmaking == "true" && !IsMatchmakingStarted)
        {
            IsMatchmakingStarted = true;
            OnMatchmakingCreated?.Invoke();
        }
    }
    private void MatchmakingFoundCheck()
    {
        if (!IsLobbyOwner() && !matchFound)
        {
            string ip = SteamMatchmaking.GetLobbyData(currentLobbyId, MessageKeyPrefixEnum.UgsLobbyIp.ToString());
            if (!string.IsNullOrEmpty(ip))
            {
                Debug.Log("Inferni Lobby Server Address: " + ip);
                ushort port;
                ushort.TryParse(SteamMatchmaking.GetLobbyData(currentLobbyId, MessageKeyPrefixEnum.UgsLobbyPort.ToString()), out port);
                Debug.Log("Inferni Lobby Server Port: " + port);
                matchFound = true;
                GameManager.Instance.LobbyGuestSetupOnlineGame(UserData.Me.Name, localPlayerManager.SelectedCharacter, currentLobbyId.ToString(), ip, port);
                EventBus.TriggerEvent(EventBusEnum.EventName.SERVER_FOUND_FOR_LOBBY, ip + ":" + port.ToString());
            }
        }
    }
    #endregion
    
    #region Steam Methods
    private void CreateLobby()
    {
        var steamAPICall = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, maxLobbyMember);
        var lobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyCreatedCallResult.Set(steamAPICall);
    }
    public void LeaveLobby()
    {
        SteamMatchmaking.LeaveLobby(currentLobbyId);
        currentLobbyId.Clear();
        OnLobbyIdChange?.Invoke(CSteamID.Nil);
    }
    
    public void InviteFriendToLobby(CSteamID friendSteamId)
    {
        if (!currentLobbyId.IsValid())
        {
            invitedFriendId = friendSteamId;
            CreateLobby();
        }
        else
        {
            SteamMatchmaking.InviteUserToLobby(currentLobbyId, friendSteamId);
        }
    }
    
    public void ChatWithFriend(CSteamID friendSteamId)
    {
        SteamFriends.ActivateGameOverlayToUser("chat", friendSteamId);
    }
    #endregion
    
    #region Steam Events
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        EventBus.TriggerEvent(EventBusEnum.EventName.CLIENT_JOIN_LOBBY, callback.m_steamIDLobby);
    }
    private void OnLobbyCreated(LobbyCreated_t callback, bool ioFailure)
    {
        if (callback.m_eResult == EResult.k_EResultOK)
        {
            currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            OnLobbyIdChange?.Invoke(currentLobbyId);
            if (invitedFriendId.IsValid())
            {
                InviteFriendToLobby(invitedFriendId);       
            }
            else
            {
                UpdateLobbyMembers();
            }
            RegisterChatUpdateCallback();
        }
        else
        {
            Debug.LogError("Failed to create lobby.");
        }
    }

    private void RegisterChatUpdateCallback()
    {
        if (lobbyChatUpdateCallback != null)
        {
            lobbyChatUpdateCallback.Dispose();
            lobbyChatUpdateCallback = null;
        }
        lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
    }
    
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (callback.m_EChatRoomEnterResponse == (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            OnLobbyIdChange?.Invoke(currentLobbyId);
            UpdateLobbyMembers();
            RegisterChatUpdateCallback();
        }
        else
        {
            Debug.LogError("Failed to join lobby. Error: " + callback.m_EChatRoomEnterResponse);
        }
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t updateParams)
    {
        if (updateParams.m_ulSteamIDLobby != currentLobbyId.m_SteamID)
        {
            Debug.Log($"OnLobbyChatUpdate callback {updateParams.m_ulSteamIDLobby} is not current lobby {currentLobbyId.m_SteamID}, Skip");
            return;
        }
        var stateChange = (EChatMemberStateChange)updateParams.m_rgfChatMemberStateChange;

        if (stateChange.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeEntered))
        {
            Debug.Log($"Player Joined Lobby: {updateParams.m_ulSteamIDUserChanged}");
            UpdateLobbyMembers();
        }

        if (stateChange.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeLeft) ||
            stateChange.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeDisconnected))
        {
            Debug.Log($"Player Left Lobby: {updateParams.m_ulSteamIDUserChanged}");
            UpdateLobbyOwner();
            UpdateLobbyMembers();
        }
    }
   
    #endregion
    
    #region Steam Lobby Messages
    public void SendReadyStatusMessage(bool isReady)
    {
        var steamId = UserData.Me.id;
        var key = $"{MessageKeyPrefixEnum.Ready}_{steamId}";
        var value = isReady ? "true" : "false";
    
        SteamMatchmaking.SetLobbyMemberData(currentLobbyId, key, value);
        TrySendGuestUnityIdMessage();
        TrySendArenaGroupMessage();
    }

    public void SendCharacterMessage(string character)
    {
        if (string.IsNullOrEmpty(character)) return;
        
        var steamId = UserData.Me.id;
        var key = $"{MessageKeyPrefixEnum.Character}_{steamId}";
    
        SteamMatchmaking.SetLobbyMemberData(currentLobbyId, key, character);
        TrySendGuestUnityIdMessage();
        TrySendArenaGroupMessage();
    }

    public void SendStartMatchmakingMessage(CharacterData selectedCharacter)
    {
        var key = MessageKeyPrefixEnum.StartUgsMatchMaking.ToString();
        SteamMatchmaking.SetLobbyData(currentLobbyId, key, "true");
        GameManager.Instance.SetupOnlineGame(UserData.Me.Name, selectedCharacter, false, currentLobbyId.ToString(), true, false);
        // TODO: testing for 2 player lobby
        // GameManager.Instance.SetupOnlineGame(UserData.Me.Name, selectedCharacter, false, currentLobbyId.ToString(), false, true);
    }

    private void TrySendGuestUnityIdMessage()
    {
        var steamId = UserData.Me.id;
        var self = FindMember(steamId);
        if (!string.IsNullOrEmpty(self?.UnityAccountId)) return;

        var guestUnityId = AuthenticationService.Instance.PlayerId;
        var key = $"{MessageKeyPrefixEnum.GuestUnityId}_{steamId}";
        SteamMatchmaking.SetLobbyMemberData(currentLobbyId, key, guestUnityId);
    }

    private void TrySendArenaGroupMessage()
    {
        var steamId = UserData.Me.id;
        var self = FindMember(steamId);
        if (!string.IsNullOrEmpty(self?.ArenaGroup)) return;

        var key = $"{MessageKeyPrefixEnum.ArenaGroup}_{steamId}";
        var arenaGroup = GetCurrentPlayerArenaGroup();
        SteamMatchmaking.SetLobbyMemberData(currentLobbyId, key, arenaGroup.ToString());
    }

    public void TrySendTeamIndexMessage(CSteamID memberId, int teamIndex)
    {
        var key = $"{MessageKeyPrefixEnum.TeamIndex}_{memberId}";
        SteamMatchmaking.SetLobbyData(currentLobbyId, key, teamIndex.ToString());
    }

    private bool GetMemberReadyStatus(CSteamID memberId)
    {
        if (!memberId.IsValid()) return false;
        
        var key = $"{MessageKeyPrefixEnum.Ready}_{memberId}";
        var value = SteamMatchmaking.GetLobbyMemberData(currentLobbyId, memberId, key);
        return value == "true";
    }

    private string GetMemberUnityAccountId(CSteamID memberId)
    {
        if (!memberId.IsValid()) return string.Empty;
        
        var key = $"{MessageKeyPrefixEnum.GuestUnityId}_{memberId}";
        return SteamMatchmaking.GetLobbyMemberData(currentLobbyId, memberId, key);
    }

    private string GetMemberArenaGroup(CSteamID memberId)
    {
        if (!memberId.IsValid()) return string.Empty;
        
        var key = $"{MessageKeyPrefixEnum.ArenaGroup}_{memberId}";
        return SteamMatchmaking.GetLobbyMemberData(currentLobbyId, memberId, key);
    }

    private string GetMemberCharacter(CSteamID memberId)
    {
        if (!memberId.IsValid()) return string.Empty;

        var key = $"{MessageKeyPrefixEnum.Character}_{memberId}";
        return SteamMatchmaking.GetLobbyMemberData(currentLobbyId, memberId, key);
    }

    private int GetMemberTeamIndex(CSteamID memberId)
    {
        if (!memberId.IsValid()) return -1;

        var key = $"{MessageKeyPrefixEnum.TeamIndex}_{memberId}";
        string teamIndex = SteamMatchmaking.GetLobbyData(currentLobbyId, key);
        if(string.IsNullOrEmpty(teamIndex))
        {
            return -1;
        }
        return Int32.Parse(teamIndex);
    }
    #endregion
#endif
}
