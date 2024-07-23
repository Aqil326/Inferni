using System;
using System.Collections.Generic;
using UnityEngine;

public class ChangeTeamWindow : MonoBehaviour
{
    [SerializeField]
    private TeamChangeButton button;

    private List<TeamChangeButton> buttons = new List<TeamChangeButton>();

    private Action<int, int> teamChangedCallback;
    private RectTransform rectTransform;
    private Transform originalParent;
    private int characterIndex;

    public event Action<int> WindowHiddenEvent;

    public void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalParent = rectTransform.parent;
        var settings = GlobalGameSettings.Settings;
        int maxTeams = settings.MaxPlayerAmount / settings.TeamSize;
        for(int i = 0; i < maxTeams; i++)
        {
            int teamIndex = i;
            TeamChangeButton currentButton = button;
            if(i > 0)
            {
                currentButton = Instantiate(button, button.transform.parent);
            }
            currentButton.button.onClick.AddListener(()=>ChangeTeam(teamIndex));
            currentButton.buttonImage.color = settings.GetTeamColor(teamIndex);
            currentButton.buttonText.text = settings.GetTeamName(teamIndex);
            buttons.Add(currentButton);
        }
    }

    private void ChangeTeam(int teamIndex)
    {
        teamChangedCallback?.Invoke(characterIndex, teamIndex);
        Hide();
    }

    public void Show(RectTransform parentRect, int characterIndex, int currentTeamIndex, Action<int, int> teamChangedCallback)
    {
        WindowHiddenEvent?.Invoke(this.characterIndex);
        this.characterIndex = characterIndex;
        this.teamChangedCallback = teamChangedCallback;
        rectTransform.SetParent(parentRect);
        rectTransform.anchoredPosition = new Vector2(parentRect.rect.width / 2 + rectTransform.rect.width / 2, 0);
        rectTransform.SetParent(originalParent);
        for(int i = 0; i < buttons.Count; i++)
        {
            buttons[i].button.interactable = i != currentTeamIndex ;
        }
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        WindowHiddenEvent?.Invoke(characterIndex);
        gameObject.SetActive(false);
    }
}
