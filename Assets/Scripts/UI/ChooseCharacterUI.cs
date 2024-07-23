using TMPro;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChooseCharacterUI : MonoBehaviour
{
    [SerializeField]
    private CharacterModelDisplay characterDisplay;
    [SerializeField]
    private DiamondShapedGrid selectCharacterGrid;
    [SerializeField]
    private RawImage characterImage;
    [SerializeField]
    private TextMeshProUGUI characterName;
    [SerializeField]
    private TextMeshProUGUI characterDescription;
    [SerializeField]
    private CharmSlotUI[] charmSlots;
    [SerializeField]
    private CardListUI cardList;

    private LocalPlayerManager localPlayerManager;
    private Action<CharacterData> callback;
    private SelectCharacterButton currentSelectedButton;
    private List<SelectCharacterButton> selectCharacterButtons = new List<SelectCharacterButton>();
    private bool isInit;
    
    public Action OnClose;

    public void Show(LocalPlayerManager localPlayerManager, Action<CharacterData> chosenCharacterChangedCallback)
    {
        characterDisplay.Init(characterImage);

        gameObject.SetActive(true);
        this.localPlayerManager = localPlayerManager;
        callback = chosenCharacterChangedCallback;
        cardList.Hide();

        if (!isInit)
        {
            var characters = GameDatabase.GetAllCharacters();
            selectCharacterGrid.Init<SelectCharacterButton>(characters.Count, (int index, SelectCharacterButton button) =>
            {
                button.SetCharacter(characters[index]);
                button.ButtonClickedEvent += OnCharacterSelected;
                selectCharacterButtons.Add(button);
            });
            isInit = true;
        }
        
        OnCharacterSelected(localPlayerManager.SelectedCharacter);
    }

    private void OnCharacterSelected(CharacterData character)
    {
        if(currentSelectedButton != null)
        {
            currentSelectedButton.SetDeselected();
        }

        foreach(var button in selectCharacterButtons)
        {
            if(button.CharacterData == character)
            {
                currentSelectedButton = button;
                button.SetSelected();
                break;
            }
        }

        characterDisplay.ShowCharacter(character);
        characterName.text = character.Name;
        characterDescription.text = character.Description;

        foreach(var slot in charmSlots)
        {
            slot.ShowEmpty();
        }

        foreach(var charm in character.StartingCharms)
        {
            charmSlots[charm.Index].SetCharmIcon(charm.CharmData);
        }


        if (character != localPlayerManager.SelectedCharacter)
        {
            localPlayerManager.ChangeSelectedCharacter(character);
            callback?.Invoke(character);
        }
        
    }

    public void Close()
    {
        gameObject.SetActive(false);
        OnClose?.Invoke();
    }

    public void ShowDeck()
    {
        cardList.Init(currentSelectedButton.CharacterData.StartingCards, null);
        cardList.Show();
    }
}
