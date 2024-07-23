using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyIdUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI lobbyIdText;
    [SerializeField]
    private TextMeshProUGUI lobbyIdMaskText;
    [SerializeField]
    private GameObject copyLobbyIdButton;
    [SerializeField]
    private ButtonUI visibilityToggleButton;
    [SerializeField]
    private Sprite showIcon;
    [SerializeField]
    private Sprite hideIcon;
    
    private CSteamID lobbyId;
    private bool isLobbyIdVisible;

    private void Start()
    {
        SetUIsVisibility();
    }

    public void SetLobbyId(CSteamID lobbyId)
    {
        if (!lobbyId.IsValid() || !lobbyId.IsLobby()) return;
        
        this.lobbyId = lobbyId;
        lobbyIdText.text = lobbyId.ToString();
        lobbyIdMaskText.text = MaskLobbyIdWithDots(lobbyId.ToString());
        SetUIsVisibility();
    }
    
    public void CopyLobbyId()
    {
        if (!lobbyId.IsValid()) return;
        
        GUIUtility.systemCopyBuffer = lobbyId.ToString();
        GameManager.GetManager<ToastManager>().ShowToast("Copy Success", "Lobby ID is copied to your clipboard", 2.0f);
    }

    public void ShowOrHideLobbyId()
    {
        isLobbyIdVisible = !isLobbyIdVisible;
        UpdateToggleButtonIcon();
        SetUIsVisibility();
    }

    private void UpdateToggleButtonIcon()
    {
        Image buttonImage = visibilityToggleButton.GetComponent<Image>();
        if (buttonImage == null) return;
        buttonImage.sprite = isLobbyIdVisible ? showIcon : hideIcon;
    }

    private void SetUIsVisibility()
    {
        var isValidLobbyId = lobbyId.IsLobby() && lobbyId.IsValid();
        copyLobbyIdButton.SetActive(isValidLobbyId);
        visibilityToggleButton.gameObject.SetActive(isValidLobbyId);
        
        lobbyIdText.gameObject.SetActive(isValidLobbyId && isLobbyIdVisible);
        lobbyIdMaskText.gameObject.SetActive(isValidLobbyId && !isLobbyIdVisible);
    }
    
    private string MaskLobbyIdWithDots(string input)
    {
        return new string('.', input.Length);
    }
}
