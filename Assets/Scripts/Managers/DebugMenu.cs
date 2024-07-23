using System;
using System.Collections.Generic;
using UnityEngine;
using QFSW.QC;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Steamworks;

public class DebugMenu : MonoBehaviour
{
    [SerializeField]
    private Vector2 nativeSize = new Vector2(1920, 1080);
    [SerializeField]
    private Color backgroundColor;

    private bool isShowingMenu;
    private bool isShowingCardSelectionGrid;
    private bool isShowingPlayerSelectionGrid;
    private bool isShowingCharmSelectionGrid;

    private bool isCastTimeEnabled = true;
    private bool isAIEnabled = true;

    private CharacterManager characterManager;
    private RoundManager roundManager;
    private CardsEffectsManager cardsEffectsManager;

    private string selectedCardName = "None";
    private string selectedPlayerName = "None";
    private string selectedCharmName = "None";
    private int selectedPlayerIndex;
    private int selectedCardIndex;
    private int selectedCharmIndex;
    private string[] cardNameList;
    private List<CardData> cardsList;
    private string[] charmsNameList;
    private List<CharmData> charmsList;
    private Vector2 scrollPosition;
    private GUIStyle bgStyle;
    private string cardNameSearch;
    private string charmNameSearch;

    private string[] playersList;

    private string lobbyId = "";

    private void Start()
    {
        characterManager = GameManager.GetManager<CharacterManager>();
        roundManager = GameManager.GetManager<RoundManager>();
        cardsEffectsManager = GameManager.GetManager<CardsEffectsManager>();

        cardsList = GameDatabase.Cards;
        cardNameList = new string[cardsList.Count + 1];
        cardNameList[0] = "None";
        for (int i = 0; i < cardsList.Count; i++)
        {
            cardNameList[i + 1] = cardsList[i].Name;
        }

        charmsList = GameDatabase.Charms;
        charmsNameList = new string[charmsList.Count + 1];
        charmsNameList[0] = "None";
        for (int i = 0; i < charmsList.Count; i++)
        {
            charmsNameList[i + 1] = charmsList[i].charmName;
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void OnGUI()
    {
        if (!Debug.isDebugBuild)
        {
            return;
        }

        bgStyle = new GUIStyle(GUI.skin.box);
        bgStyle.normal.background = MakeTex(2, 2, backgroundColor);

        Vector3 scale = new Vector3(Screen.width / nativeSize.x, Screen.height / nativeSize.y, 1.0f);
        GUI.matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity, scale);
        if (!isShowingMenu)
        {
            if (GUILayout.Button("Show Debug Menu"))
            {
                isShowingMenu = true;
            }
            return;
        }

        GUILayout.BeginVertical(bgStyle);

        GUILayout.Label("Debug Menu");

        bool toggleAI = GUILayout.Toggle(isAIEnabled, "Are Bots On?");
        if (toggleAI != isAIEnabled)
        {
            isAIEnabled = toggleAI;
            characterManager.DebugToggleAI(isAIEnabled);
        }
#if !NOSTEAMWORKS
#if !IS_DEMO
        if (GUILayout.Button("Check Achievements"))
        {
            gameObject.GetComponent<SteamStatsAndAchievements>().DebugUpdate();
        }

        if (GUILayout.Button("Reset Achievements"))
        {
            Debug.Log("Resetting Achievements");
            SteamUserStats.ResetAllStats(true);
            SteamUserStats.RequestCurrentStats();
        }
        #endif
        lobbyId = GUILayout.TextField(lobbyId);
        if (GUILayout.Button("Join Lobby"))
        {
            EventBus.TriggerEvent(EventBusEnum.EventName.CLIENT_JOIN_LOBBY, new CSteamID(Convert.ToUInt64(lobbyId)));
            isShowingMenu = false;
        }
#endif        
        if(!GameManager.Instance.IsMatchActive)
        {
            bool isLocalDev = GUILayout.Toggle(GameManager.Instance.IsLocalDevelopment, "Is Local Development?");
            if (isLocalDev != GameManager.Instance.IsLocalDevelopment)
            {
                GameManager.Instance.IsLocalDevelopment = isLocalDev;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Offline Match Character Count");
            string characterCount = GUILayout.TextField(GameManager.Instance.PlayersToStartOfflineGame.ToString(), GUILayout.Width(150));
            int newCount;
            if (int.TryParse(characterCount, out newCount))
            {
                if (newCount != GameManager.Instance.PlayersToStartOfflineGame)
                {
                    GameManager.Instance.PlayersToStartOfflineGame = newCount;
                }
            }
            GUILayout.EndHorizontal();
#if IS_DEMO
            if (GUILayout.Button("Test Current Playtest Window"))
            {
                VersionChecker.DebugCurrentPlaytestWindow();
                SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
            }

            if(GUILayout.Button("Test Next Playtest Window"))
            {
                VersionChecker.DebugNextPlaytestWindow();
                SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
            }
#endif

            ShowCloseButton();
            GUILayout.EndVertical();
            return;
        }

        if (GUILayout.Button("Skip Round"))
        {
            roundManager.DebugSkipRound();
        }

        if (!GameManager.Instance.DisableRP)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Game Arena Group");
            var currentArenaGroup = GUILayout.TextField(GameManager.Instance.ArenaGroup.ToString(), GUILayout.Width(150));
            if (int.TryParse(currentArenaGroup, out var newArenaGroup))
            {
                if (newArenaGroup != GameManager.Instance.ArenaGroup && newArenaGroup >= 0 && newArenaGroup < 4)
                {
                    GameManager.Instance.DebugSetGameArenaGroup(newArenaGroup);
                }
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Projectile Speed Multiplier");
        string multiplier = GUILayout.TextField(cardsEffectsManager.DebugProjectileSpeed.ToString("0.0"));
        float newMultiplier;
        if (float.TryParse(multiplier, out newMultiplier))
        {
            if (newMultiplier != cardsEffectsManager.DebugProjectileSpeed)
            {
                cardsEffectsManager.DebugSetProjectileSpeedMultiplier(newMultiplier);
            }
        }
        GUILayout.EndHorizontal();

        string pauseTimerText = roundManager.IsTimerPaused.Value ? "Unpause Round Timer" : "Pause Round Timer";
        if (GUILayout.Button(pauseTimerText))
        {
            roundManager.DebugToggleRoundTimerPause();
        }

        bool toggleCast = GUILayout.Toggle(isCastTimeEnabled, "Is Cast Time On?");
        if (toggleCast != isCastTimeEnabled)
        {
            isCastTimeEnabled = toggleCast;
            characterManager.DebugToggleCasting(isCastTimeEnabled);
        }

        if (GUILayout.Button("Discard Hand"))
        {
            characterManager.PlayerCharacter.DebugDiscardHand();
        }

        GUILayout.BeginHorizontal();
        string buttonText = isShowingCardSelectionGrid ? "Close" : "Select Card";
        if (GUILayout.Button(buttonText))
        {
            isShowingCardSelectionGrid = !isShowingCardSelectionGrid;
        }

        GUILayout.Label($"Selected Card: {selectedCardName}");

        if (GUILayout.Button("Add Card to Hand"))
        {
            if (selectedCardName == "None")
            {
                return;
            }

            foreach (var card in cardsList)
            {
                if (card.Name == selectedCardName)
                {
                    string cardId = card.InternalID;
                    characterManager.PlayerCharacter.DebugAddCardToHand(cardId);
                }
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        buttonText = isShowingCharmSelectionGrid ? "Close" : "Select Charm";
        if (GUILayout.Button(buttonText))
        {
            isShowingCharmSelectionGrid = !isShowingCharmSelectionGrid;
        }

        GUILayout.Label($"Selected Charm: {selectedCharmName}");
        GUILayout.EndHorizontal();

        

        GUILayout.BeginHorizontal();

        if (playersList == null && characterManager.Characters.Count > 0)
        {
            playersList = new string[characterManager.Characters.Count + 1];
            playersList[0] = "None";
            for (int i = 0; i < characterManager.Characters.Count; i++)
            {
                playersList[i + 1] = characterManager.Characters[i].PlayerData.DisplayName;
            }
        }

        buttonText = isShowingPlayerSelectionGrid ? "Close" : "Select Player";
        if (GUILayout.Button(buttonText))
        {
            isShowingPlayerSelectionGrid = !isShowingPlayerSelectionGrid;
        }


        GUILayout.Label($"Selected Player: {selectedPlayerName}");
        GUILayout.EndHorizontal();

        if (isShowingCardSelectionGrid)
        {
            string[] cardNames = cardNameList;

            cardNameSearch = GUILayout.TextField(cardNameSearch);
            if (!string.IsNullOrEmpty(cardNameSearch))
            {
                var filteredNames = new List<string>();

                foreach (var name in cardNameList)
                {
                    if (name.ToUpper().Contains(cardNameSearch.ToUpper()))
                    {
                        filteredNames.Add(name);
                    }
                    cardNames = filteredNames.ToArray();

                    if (cardNames.Length > 0)
                    {
                        SelectCard(0, cardNames);
                    }
                }
            }
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(420), GUILayout.Height(300));
            int index = GUILayout.SelectionGrid(selectedCardIndex, cardNames, 3, GUILayout.Width(400));
            if (index != selectedCardIndex)
            {
                SelectCard(index, cardNames);
                isShowingCardSelectionGrid = false;
            }
            GUILayout.EndScrollView();
        }

        if (isShowingCharmSelectionGrid)
        {
            string[] charmNames = charmsNameList;

            charmNameSearch = GUILayout.TextField(charmNameSearch);
            if (!string.IsNullOrEmpty(charmNameSearch))
            {
                var filteredNames = new List<string>();

                foreach (var name in charmsNameList)
                {
                    if (name.ToUpper().Contains(charmNameSearch.ToUpper()))
                    {
                        filteredNames.Add(name);
                    }
                    charmNames = filteredNames.ToArray();

                    if (charmNames.Length > 0)
                    {
                        SelectCharm(0, charmNames);
                    }
                }
            }
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(420), GUILayout.Height(100));
            int index = GUILayout.SelectionGrid(selectedCharmIndex, charmNames, 3, GUILayout.Width(400));
            if (index != selectedCharmIndex)
            {
                SelectCharm(index, charmNames);
                isShowingCardSelectionGrid = false;
            }
            GUILayout.EndScrollView();
        }

        if (GUILayout.Button("Make Selected Player Play Selected Card"))
        {
            if (selectedPlayerName == "None" || selectedCardName == "None")
            {
                return;
            }

            foreach (var card in cardsList)
            {
                if (card.Name == selectedCardName)
                {
                    string cardId = card.InternalID;

                    foreach (var c in characterManager.Characters)
                    {
                        if (selectedPlayerName == c.PlayerData.DisplayName)
                        {
                            characterManager.DebugMakePlayerPlayCard(c.PlayerData.Id, cardId);
                        }
                    }
                }
            }
        }

        if (GUILayout.Button("Add Selected Charm To Selected Player"))
        {
            if (selectedPlayerName == "None" || selectedCharmName == "None")
            {
                return;
            }

            foreach (var charm in charmsList)
            {
                if (charm.charmName == selectedCharmName)
                {
                    string charmId = charm.InternalID;

                    foreach (var c in characterManager.Characters)
                    {
                        if (selectedPlayerName == c.PlayerData.DisplayName)
                        {
                            characterManager.DebugAddCharmToPlayer(c.PlayerData.Id, charmId);
                        }
                    }
                }
            }
        }

        if (GUILayout.Button("Down Selected Player"))
        {
            foreach (var c in characterManager.Characters)
            {
                if (selectedPlayerName == c.PlayerData.DisplayName)
                {
                    c.DealDamage(c.Health.Value, null);
                }
            }
        }

        if (GUILayout.Button("Rotate Camera To Character"))
        {
            foreach (var c in characterManager.Characters)
            {
                if (selectedPlayerName == c.PlayerData.DisplayName)
                {
                    characterManager.RotateCameraToCharacter(c);
                    EventBus.TriggerEvent(EventBusEnum.EventName.MAIN_CAMERA_CHANGED_CLIENT);
                }
            }
        }

        if (isShowingPlayerSelectionGrid)
        {
            int index = GUILayout.SelectionGrid(selectedPlayerIndex, playersList, 3, GUILayout.Width(400));
            if (index != selectedPlayerIndex)
            {
                selectedPlayerIndex = index;
                selectedPlayerName = playersList[index];
                isShowingPlayerSelectionGrid = false;
            }
        }
        ShowCloseButton();
       
        GUILayout.EndVertical();
    }

    private void ShowCloseButton()
    {
        if (GUILayout.Button("Close Menu"))
        {
            isShowingMenu = false;
        }
    }

    private void SelectCard(int index, string[] cardNames)
    {
        selectedCardIndex = index;
        selectedCardName = cardNames[index];
    }

    private void SelectCharm(int index, string[] charmNames)
    {
        selectedCharmIndex = index;
        selectedCharmName = charmNames[index];
    }
}
