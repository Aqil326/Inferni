using System;
using System.Collections.Generic;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreenUI : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup canvasGroup;
    [SerializeField]
    private LobbyIdUI lobbyIdUI;
    [SerializeField]
    private CharacterModelListDisplay characterModelListDisplay;
    [SerializeField]
    private RawImage charactersImage;
    [SerializeField]
    private Button changeCharacterButton;
    [SerializeField]
    private Button startGameButton;
    [SerializeField]
    private Button readyButton;
    [SerializeField]
    private Button leaveButton;
    [SerializeField]
    private ChooseCharacterUI chooseCharacterUI;
    [SerializeField]
    private GameObject lobbyContentUI;
    [SerializeField]
    private LocalPlayerManager localPlayerManager;
    [SerializeField]
    private TextMeshProUGUI matchmakingText;
    [SerializeField]
    private LobbyCharacterUI[] lobbyCharacterUIs;
    [SerializeField]
    private ChangeTeamWindow changeTeamWindow;

    public Action OnLobbyUIHide;

    private SteamLobbyManager steamLobbyManager;

    private CSteamID lobbyId;
#if !NOSTEAMWORKS
    private void Start()
    {
        EventBus.StartListening<CSteamID>(EventBusEnum.EventName.CLIENT_JOIN_LOBBY, OnJoinLobby);
        EventBus.StartListening<string>(EventBusEnum.EventName.SERVER_FOUND_FOR_LOBBY, OnMatchmakingFound);
        
        steamLobbyManager = GameManager.GetManager<SteamLobbyManager>();
        steamLobbyManager.OnLobbyIdChange += OnLobbyIdChange;
        steamLobbyManager.OnLobbyOwnerChanged += OnLobbyOwnerChanged;
        steamLobbyManager.OnMatchmakingCreated += OnMatchmakingCreated;
        steamLobbyManager.OnMembersChanged += OnMembersChanged;
        steamLobbyManager.OnMemberDataChanged += OnMemberDataChanged;
        chooseCharacterUI.OnClose += OnChosenCharacterUIClosed;

        changeTeamWindow.WindowHiddenEvent += OnWindowHidden;

        for (int i = 0; i < lobbyCharacterUIs.Length; i++)
        {
            int index = i;
            lobbyCharacterUIs[i].canvasGroup.alpha = 0;
            lobbyCharacterUIs[i].changeTeamButton.onClick.AddListener(() => ShowChangeTeamWindow(index));
            lobbyCharacterUIs[i].changeTeamButton.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        EventBus.StopListening<CSteamID>(EventBusEnum.EventName.CLIENT_JOIN_LOBBY, OnJoinLobby);
        EventBus.StopListening<string>(EventBusEnum.EventName.SERVER_FOUND_FOR_LOBBY, OnMatchmakingFound);

        if (steamLobbyManager)
        {
            steamLobbyManager.OnLobbyIdChange -= OnLobbyIdChange;
            steamLobbyManager.OnLobbyOwnerChanged -= OnLobbyOwnerChanged;
            steamLobbyManager.OnMembersChanged -= OnMembersChanged;
            steamLobbyManager.OnMemberDataChanged -= OnMemberDataChanged;
        }

        if (chooseCharacterUI != null && chooseCharacterUI.OnClose != null)
        {
            chooseCharacterUI.OnClose -= OnChosenCharacterUIClosed;
        }

        for (int i = 0; i < lobbyCharacterUIs.Length; i++)
        {
            lobbyCharacterUIs[i].changeTeamButton.onClick.RemoveAllListeners();
        }
        changeTeamWindow.WindowHiddenEvent -= OnWindowHidden;
    }

    public void Show(CSteamID currentLobbyId)
    {
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        ResetUI();
        
        if (steamLobbyManager)
        {
            steamLobbyManager.Init(currentLobbyId, localPlayerManager);
        }
    }

    public void ResetUI()
    {
        if (steamLobbyManager == null) return;
        
        characterModelListDisplay.Init(charactersImage);
        matchmakingText.gameObject.SetActive(false);
        startGameButton.gameObject.SetActive(false);
        readyButton.gameObject.SetActive(false);
        leaveButton.gameObject.SetActive(true);
        changeTeamWindow.Hide();
    }
#endif    
    public void Hide(bool joinGame)
    {
        if (canvasGroup)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (joinGame)
        {
            OnLobbyUIHide?.Invoke();    
        }
    }

#if !NOSTEAMWORKS    
    #region Lobby Events
    private void OnMemberDataChanged(LobbyMember updatedMemberInfo)
    {
        for (int i = 0; i < steamLobbyManager.Members.Count; i++)
        {
            LobbyMember member = steamLobbyManager.Members[i];
            if (member.SteamUserData.id == updatedMemberInfo.SteamUserData.id && member.TeamIndex >= 0)
            {
                lobbyCharacterUIs[i].teamImage.color = GlobalGameSettings.Settings.GetTeamColor(member.TeamIndex);
            }
        }
        characterModelListDisplay.UpdateCharacterUI(updatedMemberInfo);
        UpdateButtonUIs();
    }

    private void OnMatchmakingFound(string foundLobbyId)
    {
        Hide(true);    
    }

    private void OnMembersChanged(List<LobbyMember> updatedMembers)
    {
        ResetUI();

        for(int i = 0; i < lobbyCharacterUIs.Length; i++)
        {
            lobbyCharacterUIs[i].canvasGroup.alpha = i < updatedMembers.Count ? 1 : 0;
        }
        characterModelListDisplay.ShowCharacters(updatedMembers);
        UpdateButtonUIs();
    }

    private void OnLobbyOwnerChanged(CSteamID currentLobbyOwnerId)
    {
        if (steamLobbyManager == null) return;
        
        var isSelfOwner = steamLobbyManager.IsLobbyOwner();
        if (startGameButton)
        {
            startGameButton.gameObject.SetActive(isSelfOwner);    
        }
        if (readyButton)
        {
            readyButton.gameObject.SetActive(!isSelfOwner);
        }
        if (leaveButton)
        {
            leaveButton.gameObject.SetActive(true);
        }
    }

    private void OnLobbyIdChange(CSteamID changedLobbyId)
    {
        var isValidLobby = changedLobbyId.IsValid();
        lobbyId = changedLobbyId;
        lobbyIdUI.SetLobbyId(lobbyId);

        if (!isValidLobby) return;

        UpdateButtonUIs();
    }

    private void OnJoinLobby(CSteamID joinedLobbyId)
    {
        Show(joinedLobbyId);
    }

    private void OnMatchmakingCreated()
    {
        matchmakingText.gameObject.SetActive(true);
        UpdateButtonUIs();
    }
    #endregion
    
    #region Button Controls
    private void UpdateButtonUIs()
    {
        var selfInLobby = steamLobbyManager.FindSelfInLobby();
        var isMatchmakingStarted = steamLobbyManager.IsMatchmakingStarted;
        TryUpdateStartButtonUI(selfInLobby, isMatchmakingStarted);
        TryUpdateChangeCharacterButtonUI(selfInLobby, isMatchmakingStarted);
        TryUpdateReadyButtonUI(selfInLobby, isMatchmakingStarted);
        TryUpdateLeaveButtonUI(selfInLobby, isMatchmakingStarted);
        TryUpdateChangeTeamButtonUI();
    }

    private void TryUpdateChangeTeamButtonUI()
    {
        var isLobbyOwner = steamLobbyManager.IsLobbyOwner();
        foreach (var ui in lobbyCharacterUIs)
        {
            ui.changeTeamButton.gameObject.SetActive(isLobbyOwner);
        }
    }

    private void TryUpdateStartButtonUI(LobbyMember playerInfo, bool isMatchmaking)
    {
        if (startGameButton == null) return;
        
        if (playerInfo is not { IsLobbyOwner: true })
        {
            startGameButton.gameObject.SetActive(false);
            return;
        }
        
        startGameButton.gameObject.SetActive(true);
        startGameButton.interactable = !isMatchmaking && steamLobbyManager.IsGameReadyToStart();
    }

    private void TryUpdateChangeCharacterButtonUI(LobbyMember playerInfo, bool isMatchmaking)
    {
        changeCharacterButton.interactable = !isMatchmaking;
    }

    private void TryUpdateReadyButtonUI(LobbyMember playerInfo, bool isMatchmaking)
    {
        if (readyButton == null) return;
        
        if (playerInfo is { IsLobbyOwner: true })
        {
            readyButton.gameObject.SetActive(false);
        }
        else
        {
            readyButton.gameObject.SetActive(true);
            readyButton.interactable = !isMatchmaking && playerInfo is { IsReady: false };
        }
    }

    private void TryUpdateLeaveButtonUI(LobbyMember playerInfo, bool isMatchmaking)
    {
        leaveButton.interactable = !isMatchmaking;
    }

    public void CopyLobbyIdToClipboard()
    {
        if (!lobbyId.IsValid()) return;
        
        GUIUtility.systemCopyBuffer = lobbyId.ToString();
    }

    public void LeaveLobby()
    {
        if (steamLobbyManager)
        {
            steamLobbyManager.LeaveLobby();
        }

        Hide(false);
    }

    public void SendReadyMessage()
    {
        steamLobbyManager.SendReadyStatusMessage(true);
    }

    public void StartMatchmaking()
    {
        steamLobbyManager.SendStartMatchmakingMessage(localPlayerManager.SelectedCharacter);
    }
    #endregion
    
    #region Change Character
    public void ShowCharacterSelection()
    {
        lobbyContentUI.gameObject.SetActive(false);
        chooseCharacterUI.Show(localPlayerManager, OnChosenCharacterChanged);
    }

    private void OnMyCharacterChange(string newCharacter)
    {
        steamLobbyManager.SendCharacterMessage(newCharacter);
    }

    private void OnChosenCharacterChanged(CharacterData character)
    {
        OnMyCharacterChange(character.Name);
        lobbyContentUI.gameObject.SetActive(true);
    }

    private void OnChosenCharacterUIClosed()
    {
        if (lobbyContentUI)
        {
            lobbyContentUI.gameObject.SetActive(true);
        }
    }
    #endregion

    private void ShowChangeTeamWindow(int index)
    {
        var characterUI = lobbyCharacterUIs[index];
        changeTeamWindow.Show(characterUI.rectTransform, index, steamLobbyManager.Members[index].TeamIndex, OnCharacterTeamChanged);
        characterUI.changeTeamButton.interactable = false;
    }

    private void OnWindowHidden(int index)
    {
        if (index >= 0)
        {
            lobbyCharacterUIs[index].changeTeamButton.interactable = true;
        }
    }

    private void OnCharacterTeamChanged(int characterIndex, int newTeamIndex)
    {
        var changedMember = steamLobbyManager.Members[characterIndex];
        int currentTeam = changedMember.TeamIndex;
        int teamMemberCount = 1;

        int teamSize = GlobalGameSettings.Settings.TeamSize;
        if(steamLobbyManager.Members.Count/ teamSize < 2)
        {
            teamSize = 1;
        }
        foreach (var member in steamLobbyManager.Members)
        {
            if(member != changedMember && member.TeamIndex == newTeamIndex)
            {
                teamMemberCount++;
                if (teamMemberCount > teamSize)
                {
                    steamLobbyManager.TrySendTeamIndexMessage(member.SteamUserData.id, currentTeam);
                }
            }
        }
        steamLobbyManager.TrySendTeamIndexMessage(changedMember.SteamUserData.id, newTeamIndex);
    }

#endif
}
