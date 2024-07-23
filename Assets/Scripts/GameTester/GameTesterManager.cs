using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameTesterManager : MonoBehaviour
{
    #region FIELDS

    [SerializeField] private Canvas startCanvas, endCanvas, goalsCanvas;
    [SerializeField] private TMP_InputField testPinInput;
    [SerializeField] private TMP_Text errorText, testerId;
    [SerializeField] private Button testPinBtn, closeEnd;
    [SerializeField] private GameTesterMode testerMode;
    [SerializeField] private string developerToken;
    [SerializeField] private bool debugLogging = true;
    [SerializeField] private bool resetTestProgress = false;
    [SerializeField] private Image gamesIcon;
    [SerializeField] private int requiredGames = 3;

    #endregion

    #region PROPERTIES

    private const string TEST_NO = "20240408_";
    private const string TEST_PIN = TEST_NO + "TEST_PIN";
    private const string GAME_PREFIX = TEST_NO + "GAMES_";
    private const string TEST_COMPLETED = TEST_NO + "TEST_COMPLETED";
    private const string PLAYER_NAME = TEST_NO + "PLAYER_NAME";
    private const string PLAYER_TOKEN = TEST_NO + "PLAYER_TOKEN";

    private int gamesPlayed = 0;
    private List<Image> icons = new();
    private static GameTesterManager Instance { get; set; }

    #endregion

    #region BEHAVIORS

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Init();
        AddEventListeners();
    }

    private void AddEventListeners()
    {
        testPinBtn.onClick.AddListener(SubmitTestPin);
        closeEnd.onClick.AddListener(CloseEndCanvas);
        GameManager.Instance.GameWonEvent += LevelCompleted;
        GameManager.Instance.GameLostEvent += LevelCompleted;    
    }

    private void RemoveEventListeners()
    {
        testPinBtn.onClick.RemoveListener(SubmitTestPin);
        closeEnd.onClick.RemoveListener(CloseEndCanvas);
        if (GameManager.Instance)
        {
            GameManager.Instance.GameWonEvent -= LevelCompleted;
            GameManager.Instance.GameLostEvent -= LevelCompleted;    
        }
    }

    private void LevelCompleted()
    {
        PlayerPrefs.SetInt(GAME_PREFIX + gamesPlayed, 1);
        LoadTestProgressInfo();
    }
    
    private void ResetDebugTestProgress()
    {
        if (!resetTestProgress) return;

        for (var i = 0; i < requiredGames; i++) PlayerPrefs.SetInt(GAME_PREFIX + i, 0);
        PlayerPrefs.SetInt(TEST_COMPLETED, 0);
        PlayerPrefs.SetString(TEST_PIN, "");
    }

    private bool IsTestUncompleted()
    {
        return PlayerPrefs.GetInt(TEST_COMPLETED, 0) == 0;
    }

    private string GetLocalTestPin()
    {
        return PlayerPrefs.GetString(TEST_PIN, "");
    }
    private string GetLocalTesterName()
    {
        return PlayerPrefs.GetString(PLAYER_NAME, "");
    }

    private void Init()
    {
#if UNITY_EDITOR
        ResetDebugTestProgress();
#endif
        var localTesterPin = GetLocalTestPin();
        if (string.IsNullOrEmpty(localTesterPin) && IsTestUncompleted())
        {
            startCanvas.gameObject.SetActive(true);
        }
        goalsCanvas.gameObject.SetActive(true);
        endCanvas.gameObject.SetActive(false);
        gamesIcon.gameObject.SetActive(false);
        testerId.text = "";
        testPinInput.text = localTesterPin;
        gamesPlayed = 0;

        if (!string.IsNullOrEmpty(localTesterPin))
        {
            LoadTestProgressInfo();
            SignInToGameTester(localTesterPin);
        }
    }

    private void LoadTestProgressInfo()
    {
        var localTesterName = GetLocalTesterName(); 
        if (!string.IsNullOrEmpty(localTesterName))
        {
            testerId.text = localTesterName;
        }
        
        foreach (var icon in icons)
        {
            Destroy(icon.gameObject);
        }

        icons = new List<Image>();
        gamesPlayed = 0;
        for (var i = 0; i < requiredGames; i++)
        {
            var img = Instantiate(gamesIcon, gamesIcon.transform.parent);
            var rect = img.rectTransform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x + 50 * i, rect.anchoredPosition.y);
            img.gameObject.SetActive(true);
            if (PlayerPrefs.GetInt(GAME_PREFIX + i, 0) > 0)
            {
                gamesPlayed++;
                img.GetComponent<Image>().color = new Color32(255, 255, 225, 255);
            }

            icons.Add(img);
        }

        if (gamesPlayed >= requiredGames && IsTestUncompleted())
        {
            StartCoroutine(GameTester.Api.UnlockTest(o => ShowTestCompletedUI()));
        }
    }

    private void ShowTestCompletedUI()
    {
        endCanvas.gameObject.SetActive(true);
    }

    private void CloseEndCanvas()
    {
        endCanvas.gameObject.SetActive(false);
        PlayerPrefs.SetInt(TEST_COMPLETED, 1);
    }

    private void SubmitTestPin()
    {
        var playerPin = testPinInput.text ?? string.Empty;
        SignInToGameTester(playerPin);
    }

    private void SignInToGameTester(string playerPin)
    {
        InitializeGameTester();

        if (playerPin.Length > 0) GameTester.SetPlayerPin(playerPin);

        StartCoroutine(GameTester.Api.Auth(o =>
        {
            if (o.Code == GameTesterResponseCode.Success)
            {
                errorText.text = "";

                ReadyToPlay(playerPin, o.PlayerName, o.PlayerToken);
            }
            else
            {
                errorText.text = o.Message;
                testPinInput.text = "";
            }
        }));
    }

    private void InitializeGameTester()
    {
        var mode = testerMode;

        GameTester.Initialize(mode, developerToken, debugLogging);
    }

    private void ReadyToPlay(string playerPin, string playerName, string playerToken)
    {
        if (playerPin != GetLocalTestPin())
        {
            PlayerPrefs.DeleteAll();
        }
        testerId.text = playerName;
        PlayerPrefs.SetString(TEST_PIN, playerPin);
        PlayerPrefs.SetString(PLAYER_NAME, playerName);
        PlayerPrefs.SetString(PLAYER_TOKEN, playerToken);
        startCanvas.gameObject.SetActive(false);

        LoadTestProgressInfo();
    }

    private void OnDestroy()
    {
        RemoveEventListeners();
    }
    
    #endregion
}