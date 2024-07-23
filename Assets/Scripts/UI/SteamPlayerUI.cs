using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using HeathenEngineering.SteamworksIntegration;
using HeathenEngineering.SteamworksIntegration.UI;


public class SteamPlayerUI : MonoBehaviour
{

    [SerializeField] private SetUserAvatar avatar;
    [SerializeField] private SetUserName playerName;
    [SerializeField] private Image hostIndicator;
    [SerializeField] private Image readyIndicator;
    [SerializeField] private TextMeshProUGUI characterText;

#if !NOSTEAMWORKS 
    public void Init(UserData playerData, LocalPlayerManager localPlayerManager, ChooseCharacterUI chooseCharacterUI, GameObject lobbyContentUI)
    {
        ShowReadyIndicator(false);
        if (playerData.IsValid)
        {
            avatar.UserData = playerData;    
        }
        playerName.UserData = playerData;
        var isSetForSelf = IsSelf(playerData);
        characterText.text = isSetForSelf ? localPlayerManager.SelectedCharacter.Name : "";
    }

    private bool IsSelf(UserData playerData)
    {
        return playerData.id.Equals(UserData.Me.id);
    }
    
    public void SetCharacterUI(string characterName)
    {
        characterText.text = characterName;
    }
    
    public void SetHostUI(bool isVisible)
    {
        hostIndicator.gameObject.SetActive(isVisible);
    }

    public void ShowReadyIndicator(bool isVisible)
    {
        readyIndicator.gameObject.SetActive(isVisible);
    }
    
#endif    
}
