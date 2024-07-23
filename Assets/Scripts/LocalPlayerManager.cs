using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalPlayerManager : MonoBehaviour
{
    private const string PREV_SELECTED_CHARACTER_KEY = "CurrentCharacter";

    public CharacterData SelectedCharacter { get; private set; }

    private void Awake()
    {
        if(PlayerPrefs.HasKey(PREV_SELECTED_CHARACTER_KEY))
        {
            string selectedCharacter = PlayerPrefs.GetString(PREV_SELECTED_CHARACTER_KEY);
            if (!GameDatabase.CharacterExists(selectedCharacter))
            {
                SelectedCharacter = GameDatabase.InitialCharacter;
                return;
            }
            SelectedCharacter = GameDatabase.GetCharacterData(selectedCharacter);
        }

        if (SelectedCharacter == null)
        {
            SelectedCharacter = GameDatabase.InitialCharacter;
        }
    }

    public void ChangeSelectedCharacter(CharacterData data)
    {
        SelectedCharacter = data;
        PlayerPrefs.SetString(PREV_SELECTED_CHARACTER_KEY, data.InternalID);
    }
}
