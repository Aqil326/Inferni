using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameQuitPrompt : MonoBehaviour
{
    [SerializeField]
    private string gameQuitText;

    [SerializeField]
    private string gameCloseText;

    [SerializeField]
    private TextMeshProUGUI mainText;

    [SerializeField]
    private Button yesButton;

    private void Start()
    {
        GameManager.Instance.OnEscapeKeyPressed += ShowQuitPrompt;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.OnEscapeKeyPressed -= ShowQuitPrompt;    
        }
    }

    private void ShowQuitPrompt()
    {
        if (gameObject.activeSelf)
        {
            CancelQuit();    
        }
        else
        {
            yesButton.onClick.RemoveAllListeners()
;            if(GameManager.Instance.IsMatchActive)
            {
                mainText.text = gameQuitText;
                yesButton.onClick.AddListener(ShowMainMenu);
            }
            else
            {
                mainText.text = gameCloseText;
                yesButton.onClick.AddListener(CloseGame);
            }
            gameObject.SetActive(true);
        }
    }

    public void ShowMainMenu()
    {
        // TODO: Update RP when player quit the game before game end
#if !NOSTEAMWORKS && !IS_DEMO      
        SteamStatsAndAchievements steamStatsAndAchievementsObj = GameObject.FindObjectOfType(typeof(SteamStatsAndAchievements)) as SteamStatsAndAchievements;
        steamStatsAndAchievementsObj.StatUpdate();
#endif
        GameManager.Instance.EndMatch();
    }

    public void CloseGame()
    {
        Application.Quit();
    }

    public void CancelQuit()
    {
        gameObject.SetActive(false);
    }
}
