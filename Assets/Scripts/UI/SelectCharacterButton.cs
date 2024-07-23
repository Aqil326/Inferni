using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectCharacterButton : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI buttonText;

    [SerializeField]
    private Image characterImage;

    [SerializeField]
    private Button button;

    [SerializeField]
    private Image buttonHighlight;

    public CharacterData CharacterData { get; private set; }

    public Action<CharacterData> ButtonClickedEvent;

    public void SetCharacter(CharacterData characterData)
    {
        CharacterData = characterData;
        buttonText.text = characterData.Name;
        characterImage.sprite = characterData.Sprite;
        SetDeselected();
    }

    public void SetSelected()
    {
        buttonHighlight.enabled = true;
        button.interactable = false;
    }

    public void SetDeselected()
    {
        buttonHighlight.enabled = false;
        button.interactable = true;
    }

    public void OnClick()
    {
        ButtonClickedEvent?.Invoke(CharacterData);
    }
}
