using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CharacterModelListDisplay : MonoBehaviour
{
    [SerializeField]
    private CharacterDisplayParent[] characterParents;
    [SerializeField]
    private RenderTexture texture;
    [SerializeField]
    private Camera camera;
    [SerializeField]
    private Vector2 targetResolution;

    private Dictionary<LobbyMember, GameObject> playerModels = new();
    private int screenWidth;
    private int screenHeight;
    private float originalCameraSize;
    private bool isShowing;

    private void Start()
    {
        originalCameraSize = camera.orthographicSize;
    }

    public void Init(RawImage image)
    {
        image.texture = texture;
        CheckScreenRes();
    }

    private void CheckScreenRes()
    {
        if(Screen.width != screenWidth || Screen.height != screenHeight)
        {
            float widthRatio = Screen.width / targetResolution.x;
            float heightRatio = Screen.height / targetResolution.y;
            camera.orthographicSize = originalCameraSize * (widthRatio > heightRatio ? widthRatio : heightRatio);
            screenWidth = Screen.width;
            screenHeight = Screen.height;
        }
    }

    public void ShowCharacters(List<LobbyMember> lobbyMembers)
    {
        ResetUI();
        if (lobbyMembers.Count == 0) return;
        isShowing = true;
        MoveSelfToFirst(lobbyMembers);
        for (int index = 0; index < lobbyMembers.Count; index++)
        {
            var player = lobbyMembers[index];
#if !NOSTEAMWORKS 
            Debug.Log($"ShowCharacter for player: {player.SteamUserData.Nickname}");
#endif
            var characterParent = characterParents[index];          
            characterParent.ShowUI(player);
            UpdateCharacterModel(player, characterParent);
        }
    }

    private void MoveSelfToFirst(List<LobbyMember> members)
    {
        var targetIndex = members.FindIndex(member => member.IsSelf);
        if (targetIndex == -1) return;
        
        var myself = members[targetIndex];
        members.RemoveAt(targetIndex);
        members.Insert(0, myself);
    }
    public void UpdateCharacterUI(LobbyMember player)
    {
        var targetPlayerIndex = -1;
        LobbyMember existingPlayer = null;
        for (int index = 0; index < playerModels.Count; index++)
        {
            var playerKey = playerModels.ElementAt(index).Key;            
            if (playerKey.SteamUserData.id.Equals(player.SteamUserData.id))
            {
                targetPlayerIndex = index;
                existingPlayer = playerKey;
                break;
            }
        }

        if (targetPlayerIndex < 0)
        {
            Debug.Log("Player not found in PlayerModels, skip");
            return;
        }

        ChangeKey(playerModels, existingPlayer, player);
        var characterParent = characterParents[targetPlayerIndex];       
        characterParent.ShowUI(player);
        UpdateCharacterModel(player, characterParent);
    }

    private bool ChangeKey<TKey, TValue>(IDictionary<TKey, TValue> dict, TKey oldKey, TKey newKey)
    {
        if (!dict.TryGetValue(oldKey, out var value))
            return false;

        dict.Remove(oldKey);
        dict[newKey] = value;
        return true;
    }

    private void ResetUI()
    {
        isShowing = false;
        if (playerModels.Count > 0)
        {
            foreach (var model in playerModels.Values)
            {
                Destroy(model);
            }
        }
        playerModels.Clear();        
        foreach (var characterParent in characterParents)
        {
            characterParent.ResetUI();
        }
    }
    
    private void UpdateCharacterModel(LobbyMember player, CharacterDisplayParent characterParent)
    {
        if (player == null) return;
        var characterData = GameDatabase.GetCharacterDatabyName(player.Character);
        if (characterData == null)
        {
            Debug.Log("No available character, Skip");
            playerModels.TryAdd(player, null);
            return;
        }
        
        if (playerModels.TryGetValue(player, out GameObject model))
        {
            Destroy(model);
        }
        Debug.Log("created new character model");
        model = Instantiate(characterData.CharacterModel, characterParent.transform);
        SetGameLayerRecursive(model, LayerMask.NameToLayer("CharacterListDisplay"));
        playerModels[player] = model;
    }
    
    private void SetGameLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            child.gameObject.layer = layer;

            Transform _HasChildren = child.GetComponentInChildren<Transform>();
            if (_HasChildren != null)
            {
                SetGameLayerRecursive(child.gameObject, layer);
            }

        }
    }

    private void Update()
    {
        if(isShowing)
        {
            CheckScreenRes();
        }
    }
}



