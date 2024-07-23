using UnityEngine;
using TMPro;
using Unity.Netcode;
using SimpleAudioManager;
using System;
// #if NOSTEAMWORKS
using Steamworks;
using HeathenEngineering.SteamworksIntegration;
// #endif
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class MainMenuUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI waitingForPlayersText;

    [SerializeField]
    private TextMeshProUGUI remainingTimeText;

    [SerializeField]
    private TMP_InputField inputField;
    
    [SerializeField]
    private ChooseCharacterUI chooseCharacterUI;

    [SerializeField]
    private LobbyScreenUI lobbyUI;
    
    [SerializeField]
    private LocalPlayerManager localPlayerManager;

    [SerializeField]
    private GameObject startGameButton;

    [SerializeField]
    private GameObject changeCharacterButton;

    [SerializeField]
    private GameObject cancelMatchmakingButton;

    [SerializeField]
    private GameObject startOnlineHostButton;

    [SerializeField]
    private GameObject startOfflineButton;

    [SerializeField]
    private TextMeshProUGUI startOnlineButton;

    [SerializeField]
    private GameObject[] uisToHideWhenMatchmaking;
    
    [SerializeField]
    private GameObject createLobbyButton;
    
    [SerializeField]
    private TMP_InputField lobbyIdText;

    [SerializeField]
    private GameObject mainMenuBackground;

    [SerializeField]
    private RawImage characterImage;

    [SerializeField]
    private CharacterModelDisplay characterDisplay;

    [SerializeField]
    private TextMeshProUGUI versionInvalidText;

    [SerializeField]
    private TextMeshProUGUI playtestWindowText;

    [SerializeField]
    private float menuCameraFarClip;

    [SerializeField]
    private GameObject playerNameInput;

    [SerializeField]
    private CardListUI cardList;

    // #if UNITY_SERVER
    private bool serverInitialized = false;
    // #endif

#if UNITY_SERVER
    private bool isServer = true;
#else
    private bool isServer = false;
#endif
#if UNITY_EDITOR
    private bool IsEditor = true;
#else
    private bool IsEditor = false;
#endif

    private bool isMatchmaking;

    // This will reset to false / default value set in GameManager in the first Update()
    // This reset is currently required to trigger menu hiding
    private bool isLocalDevelopment = true;
    
#if IS_DEMO
    private DateTime? currentPlaytestWindow;
    private DateTime? nextPlaytestWindow;
#endif

    private void OnEnable()
    {
        mainMenuBackground.SetActive(true);
        EventBus.StartListening(EventBusEnum.EventName.GAME_STARTED_CLIENT, OnMatchStarted);
        EventBus.StartListening(EventBusEnum.EventName.RESET_MAIN_MENU, RestoreCancelMatchmakingUI);
        EventBus.StartListening(EventBusEnum.EventName.REJECT_MAIN_MENU_CLIENT, RejectCancelMatchmakingUI);
        EventBus.StartListening(EventBusEnum.EventName.SERVER_DISCONNECTED_CLIENT, OnServerDisconnected);
        EventBus.StartListening(EventBusEnum.EventName.SERVER_REJECTED_CLIENT, OnServerRejecteded);
        EventBus.StartListening<string>(EventBusEnum.EventName.SERVER_FOUND_FOR_LOBBY, OnMatchmakingFound);
        EventBus.StartListening(EventBusEnum.EventName.CLIENT_CREATE_MATCHMAKER_TICKET_SUCCESS, OnMatchmakingCreated);
       
        waitingForPlayersText.text = "";
        remainingTimeText.text = "";
        OnChosenCharacterChanged(localPlayerManager.SelectedCharacter);
        chooseCharacterUI.Close();
#if !NOSTEAMWORKS        
        lobbyUI.ResetUI();
#endif
        lobbyUI.Hide(false);
#if !NOSTEAMWORKS
        lobbyUI.OnLobbyUIHide += HideMenuButtons;
#endif
        cancelMatchmakingButton.SetActive(false);
        createLobbyButton.SetActive(true); //isLocalDevelopment);

        if (PlayerPrefs.HasKey("PlayerName"))
        {
            inputField.text = PlayerPrefs.GetString("PlayerName");
        }

        if (VersionChecker.VersionCheckFinished)
        {
            HandleVersionCheck();
        }
        else
        {
            foreach (var ui in uisToHideWhenMatchmaking)
            {
                ui.SetActive(false);
            }
            VersionChecker.VersionCheckFinishedEvent += HandleVersionCheck;
        }

        Camera.main.farClipPlane = menuCameraFarClip;
    }

    private void OnMatchmakingFound(string lobbyId)
    {
        cancelMatchmakingButton.SetActive(false);
    }
    
    private void OnMatchmakingCreated()
    {
        cancelMatchmakingButton.SetActive(true);
    }
    
    private void HandleVersionCheck()
    {
        VersionChecker.VersionCheckFinishedEvent -= HandleVersionCheck;
        versionInvalidText.gameObject.SetActive(!VersionChecker.IsVersionValid);
        foreach (var ui in uisToHideWhenMatchmaking)
        {
            ui.SetActive(VersionChecker.IsVersionValid);
        }

        if (!VersionChecker.IsVersionValid)
        {
            versionInvalidText.text = $"Current game version {Application.version} is outdated. Please update to the newest version";
        }

        playtestWindowText.gameObject.SetActive(false);

#if STEAMWORKS_NET
        playerNameInput.SetActive(false);
#else
        playerNameInput.SetActive(true);
#endif

#if IS_DEMO
        if(!VersionChecker.ShouldCheckPlaytestWindows)
        {
            return;
        }

        startGameButton.SetActive(false);
        playtestWindowText.gameObject.SetActive(true);

        var windowStart = VersionChecker.GetPlaytestWindowStart();
        if (windowStart.HasValue)
        {
            var startDate = windowStart;
            var endDate = VersionChecker.GetPlaytestWindowEnd();
            if (startDate < DateTime.UtcNow && endDate > DateTime.UtcNow)
            {
                startGameButton.SetActive(true);
                currentPlaytestWindow = endDate;
                nextPlaytestWindow = null;
            }
            else if(startDate > DateTime.UtcNow)
            {
                nextPlaytestWindow = startDate;
                currentPlaytestWindow = null;
            }
        }
        else
        {
            playtestWindowText.text = "No public online game now";
        }
#endif
    }

    private void Start()
    {
        characterDisplay.Init(characterImage);
        // isAllocated = false;
        GameManager.GetManager<PlayersNetworkManager>().NumberOfPlayers.OnValueChanged += UpdatePlayerAmount;
        GameManager.GetManager<PlayersNetworkManager>().WaitForPlayersTimer.OnValueChanged += UpdateTimer;

        cardList.Init(GameDatabase.Cards.ToArray(), GameDatabase.Charms.ToArray());
    }

    
    private void OnDisable()
    {
        if (mainMenuBackground)
        {
            mainMenuBackground.SetActive(false);    
        }
        EventBus.StopListening(EventBusEnum.EventName.SERVER_DISCONNECTED_CLIENT, OnServerDisconnected);
        VersionChecker.VersionCheckFinishedEvent -= HandleVersionCheck;
    }

    private void OnDestroy()
    {
        EventBus.StopListening(EventBusEnum.EventName.GAME_STARTED_CLIENT, OnMatchStarted);
        EventBus.StopListening(EventBusEnum.EventName.RESET_MAIN_MENU, RestoreCancelMatchmakingUI);
        EventBus.StopListening(EventBusEnum.EventName.REJECT_MAIN_MENU_CLIENT, RejectCancelMatchmakingUI);
        EventBus.StopListening<string>(EventBusEnum.EventName.SERVER_FOUND_FOR_LOBBY, OnMatchmakingFound);
        EventBus.StopListening(EventBusEnum.EventName.CLIENT_CREATE_MATCHMAKER_TICKET_SUCCESS, OnMatchmakingCreated);

        if (GameManager.Instance != null)
        {
            GameManager.GetManager<PlayersNetworkManager>().NumberOfPlayers.OnValueChanged -= UpdatePlayerAmount;
            GameManager.GetManager<PlayersNetworkManager>().WaitForPlayersTimer.OnValueChanged -= UpdateTimer;
        }

        if (lobbyUI)
        {
            lobbyUI.OnLobbyUIHide -= HideMenuButtons;
        }
    }

    private void StartOnlineServer()
    {
        gameObject.SetActive(false);
        GameManager.Instance.StartOnlineServer();
    }

    public void OnStartGame(bool isServerHost)
    {
        StartOnlineMatch(isServerHost);
    }

    private async void StartOnlineMatch(bool isServerHost, string lobbyId = "")
    {
        Debug.Log($"StartOnlineMatch isServer: {isServerHost}, lobbyId: {lobbyId}, character: {localPlayerManager.SelectedCharacter.Name}");
        GameManager.Instance.isServer = isServerHost;
        GameManager.Instance.isHost = isServerHost;
        isMatchmaking = true;

        GameManager.Instance.SetupOnlineGame(GetPlayerName(), localPlayerManager.SelectedCharacter, isServerHost, lobbyId);
        HideMenuButtons();
    }

    private void HideMenuButtons()
    {
        foreach(var toHideUI in uisToHideWhenMatchmaking)
        {
            toHideUI.SetActive(false);
        }
        cancelMatchmakingButton.SetActive(false);
    }

    public void StartOfflineMatch()
    {
        gameObject.SetActive(false);
        GameManager.Instance.StartOfflineGame(GetPlayerName(), localPlayerManager.SelectedCharacter);
    }

    public void StartTrainingMode()
    {
        gameObject.SetActive(false);
        GameManager.Instance.StartOfflineGame(GetPlayerName(), localPlayerManager.SelectedCharacter, true);
    }

    public void CancelMatchmaking()
    {   
        if (!isLocalDevelopment)
        {
            GameManager.Instance.StopMatchmaking();
        }
        //Disconnect player
        NetworkManager.Singleton.Shutdown();        
        RestoreCancelMatchmakingUI();
        var targetScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
    }

    private void RestoreCancelMatchmakingUI()
    {
        foreach(var go in uisToHideWhenMatchmaking)
        {
            go.SetActive(true);
        }
        startOnlineHostButton.SetActive(isLocalDevelopment);
        cancelMatchmakingButton.SetActive(false);
        waitingForPlayersText.text = "";
        remainingTimeText.text = "";

        isMatchmaking = false;
    }

    private void RejectCancelMatchmaking()
    {   
        if (!isLocalDevelopment)
        {
            GameManager.Instance.StopMatchmaking();
        }
        //Disconnect player
        NetworkManager.Singleton.Shutdown();

        RejectCancelMatchmakingUI();

    }

    private void RejectCancelMatchmakingUI()
    {
        foreach(var go in uisToHideWhenMatchmaking)
        {
            go.SetActive(true);
        }
        startOnlineButton.text = "Incorrect Client Version";
        // TODO: startOnlineButton.SetActive(false);
        startOnlineHostButton.SetActive(isLocalDevelopment);
        cancelMatchmakingButton.SetActive(false);
        waitingForPlayersText.text = "";
        remainingTimeText.text = "";

        isMatchmaking = false;
    }

    private void OnServerDisconnected()
    {
        if(isMatchmaking)
        {
            CancelMatchmaking();
        }
    }

    private void OnServerRejecteded()
    {
        if(isMatchmaking)
        {
            RejectCancelMatchmaking();
        }
    }
    

    private string GetPlayerName()
    {
        string name = "Player" + UnityEngine.Random.Range(0, 100);
#if STEAMWORKS_NET
        if (SteamManager.Initialized)
        {
            name = UserData.Me.Nickname;
        }
#else
        if (!string.IsNullOrEmpty(inputField.text))
        {
            name = inputField.text;
            SavePlayerName();
;        }
#endif
        return name;
    }

    private void SavePlayerName()
    {
        if(string.IsNullOrEmpty(inputField.text))
        {
            return;
        }
        PlayerPrefs.SetString("PlayerName", inputField.text);
    }
    
    public void ShowCharacterSelection()
    {
        chooseCharacterUI.Show(localPlayerManager, OnChosenCharacterChanged);
    }

    public void ShowLobbyUI()
    {
#if !NOSTEAMWORKS
        lobbyUI.Show(CSteamID.Nil);        
#endif
    }

    public void JoinLobby()
    {     
        var lobbyId = lobbyIdText.text.Trim();
        if (string.IsNullOrEmpty(lobbyId)) return;
#if !NOSTEAMWORKS        
        EventBus.TriggerEvent(EventBusEnum.EventName.CLIENT_JOIN_LOBBY, new CSteamID(Convert.ToUInt64(lobbyId)));        
#endif
    }
    
    private void OnChosenCharacterChanged(CharacterData character)
    {
        characterDisplay.ShowCharacter(character);
    }

    private void UpdatePlayerAmount(int previous, int numberOfPlayers)
    {
        bool gameStarting = numberOfPlayers >= GlobalGameSettings.Settings.MinPlayerAmount;
        remainingTimeText.gameObject.SetActive(gameStarting);
        if (EnvironmentSettings.Settings.IsWASD)
        {
            waitingForPlayersText.text = $"Waiting for players... {numberOfPlayers} of {GlobalGameSettings.Settings.MinPlayerAmount}.";
        } else
        {
            waitingForPlayersText.text = $"Waiting for players... {numberOfPlayers} of {GlobalGameSettings.Settings.MaxPlayerAmount}.";
        }
    }

    private void UpdateTimer(float previous, float current)
    {
        remainingTimeText.text = (GlobalGameSettings.Settings.WaitForPlayersTimer - ((int)current)) + " seconds left";
    }

    private void OnMatchStarted()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {  
        if (isServer)
        {
            Debug.Log("Server is calling MainMenuUI Update");
            // May be it is better to start online server from Game Manager?
            if (!serverInitialized)
            {
                Debug.Log("Server initialising! Flag A");
                serverInitialized = true;
                StartOnlineServer();
            }
        }
        else
        {
            if(GameManager.Instance == null)
            {
                return;
            }
            if (isLocalDevelopment != GameManager.Instance.IsLocalDevelopment)
            {
                isLocalDevelopment = GameManager.Instance.IsLocalDevelopment;

                startOnlineHostButton.SetActive(isLocalDevelopment);
                startOfflineButton.SetActive(isLocalDevelopment);

                if (isLocalDevelopment)
                {
                    startOnlineButton.text = "Start Local Game";
                }
                else
                {
                    startOnlineButton.text = "Join Public Match";
                }

            }
            if (SongManager.instance != null && !SongManager.instance.isSongPlaying)
            {
                // SongManager.instance.PlaySong(0);
                // SongManager.instance.SetIntensity(0);
            }
        }
#if IS_DEMO
        if(currentPlaytestWindow.HasValue)
        {
            var timeSpan = currentPlaytestWindow.Value - DateTime.UtcNow;
            playtestWindowText.text = $"Public game online {((int)(timeSpan.TotalHours)).ToString("00")}:{timeSpan.Minutes.ToString("00")}:{timeSpan.Seconds.ToString("00")}";

            if (timeSpan < TimeSpan.Zero)
            {
                HandleVersionCheck();
            }
        }
        else if(nextPlaytestWindow.HasValue)
        {
            var timeSpan = nextPlaytestWindow.Value - DateTime.UtcNow;
            playtestWindowText.text = $"Next public online game will happen in {((int)timeSpan.TotalHours).ToString("00")}:{timeSpan.Minutes.ToString("00")}:{timeSpan.Seconds.ToString("00")}";

            if(timeSpan < TimeSpan.Zero)
            {
                HandleVersionCheck();
            }
        }
#endif
    }

    public void ShowCodex()
    {
        cardList.Show();
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}



