using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Unity.Netcode;

public class CharacterManager : BaseManager
{
    [SerializeField]
    private PlayerHUD playerHUD;

    [SerializeField]
    private float spawnPointDistanceFromCenter;

    [SerializeField]
    private float spawnPointHeight;

    [SerializeField]
    private Transform spawnedCharactersParent;

    [SerializeField]
    private Character characterPrefab;

    [SerializeField]
    private Transform worldTransform;

    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private CameraManager cameraManager;

    [SerializeField]
    private bool usePositionIds;

    [SerializeField]
    private float teammateDistanceAngle = 45f;

    [SerializeField]
    private BotProfile botProfile;

    public List<Character> Characters { get; private set; } = new List<Character>();
    public List<List<Character>> Teams { get; private set; } = new List<List<Character>>();
    // Client only
    public Character PlayerCharacter { get; private set; }

    public Character HighestCharacter { get; private set; }
    public Character LowestCharacter { get; private set; }

    public int NumberOfCharactersAlive { get; private set; }
    public int NumberOfTeamsAlive { get; private set; }
    public int WinningTeamIndex { get; private set; }
    public bool AreBotsActive { get; private set; } = true;

    private List<PlayerData> players = new List<PlayerData>();
    private PlayersNetworkManager playersManager;
    private Vector3 originalCameraPosition;
    private Vector3 originalCameraRotation;

    public Action<Character> CharacterStateChangedEvent;
    public Action<Character, int, Card> CharacterHealthChangedByCardEvent;
    public Action<Character, Card, int> CharacterCardBatchDiscardedEvent;

    public delegate void OnPlayerPosition(Character character);
    public OnPlayerPosition OnHighestPlayer;
    public OnPlayerPosition OnLowestPlayer;

    private void Start()
    {
        playersManager = GameManager.GetManager<PlayersNetworkManager>();
        originalCameraPosition = mainCamera.transform.position;
        originalCameraRotation = mainCamera.transform.eulerAngles;
    }

    //Server-Only
    public void CreateOnlineCharacters()
    {
        playersManager.PlayerDisconnectedEvent += OnCharacterDisconnected;

        for (int i = 0; i < playersManager.Players.Count; i++)
        {
            PlayerData playerData = playersManager.Players[i];
            CreateCharacter(playerData, playerData.Id);
        }

        var networkIds = new ulong[Characters.Count];
        var teamIndexes = new ulong[Characters.Count];
        for (int i = 0; i < Characters.Count; i++)
        {
            var c = Characters[i];
            networkIds[i] = c.GetComponent<NetworkObject>().NetworkObjectId;
            teamIndexes[i] = (ulong)c.TeamIndex;
        }

        CreateTeams();
        AssignNemesisToAllCharacters();
        SendCreatedCharactersClientRpc(networkIds, teamIndexes);

        NumberOfCharactersAlive = Characters.Count;
        OnUpdateHealth(0, 0); 
    }

    [ClientRpc]
    private void SendCreatedCharactersClientRpc(ulong[] networkIds, ulong[] teamIndexes)
    {
        var index = 0;
        foreach (ulong n in networkIds)
        {
            NetworkObject foundObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[n];
            var character = foundObj.GetComponent<Character>();
            character.TeamIndex = (int)teamIndexes[index];
            index++;
            if (!IsHost)
            {
                Characters.Add(character);
                character.StateChangedEvent += OnCharacterStateChanged;
                character.Health.OnValueChanged += OnUpdateHealth;
            }

            if (character.IsPlayer())
            {
                PlayerCharacter = character;
            }
        }

        if (!IsHost)
        {
            CreateTeams();
        }

        playerHUD.Init(PlayerCharacter);
        RotateCameraToCharacter(PlayerCharacter);
        cameraManager.Init(PlayerCharacter);

        foreach (var c in Characters)
        {
            c.InitView();
        }

        NumberOfCharactersAlive = Characters.Count;
        OnUpdateHealth(0, 0);
        EventBus.TriggerEvent(EventBusEnum.EventName.CHARACTERS_INITIALIZED_CLIENT);
    }

    public void RotateCameraToCharacter(Character character)
    {
        mainCamera.transform.position = originalCameraPosition;
        mainCamera.transform.eulerAngles = originalCameraRotation;
        int teamSize = GlobalGameSettings.Settings.TeamSize;
        bool lessThanTwoTeams = Characters.Count < teamSize * 2;
        if (lessThanTwoTeams)
        {
            teamSize = 1;
        }

        var teamCount = Teams.Count(team => team.Count > 0);
        float teamAngle = 360 / teamCount;
        
        var characterIndex = Teams[character.TeamIndex].IndexOf(character);
        float angle = teamAngle * character.TeamIndex + teammateDistanceAngle * characterIndex;
        mainCamera.transform.RotateAround(Vector3.zero, Vector3.up, angle);
    }

    private void OnCharacterDisconnected(PlayerData playerData)
    {
        Character character = null;
        foreach(var c in Characters)
        {
            if(c.PlayerData.Id == playerData.Id)
            {
                character = c;
            }
        }

        if (character == null)
        {
            Debug.LogError($"No character found with player ID {playerData.Id}");
        }
        else
        {
            Debug.Log($"Player {character.PlayerData.DisplayName} left the match");
            // TODO: Need further testing
            if (!GameManager.Instance.IsGameEnd)
            {
                AnalyticsManager.Instance.SendData(AnalyticsManager.EventType.Error, "Player disconnected before game end", AnalyticsManager.ErrorType.UnexpectedDisconnected);
            }
            character.Kill();
        }
    }

    public void CreateOfflineCharacters(PlayerData playerData, int totalCharacters)
    {
        playerData.TeamIndex = 0;
        var character = CreateCharacter(playerData, playerData.Id);
        
        PlayerCharacter = character;
        playerHUD.Init(PlayerCharacter);
        cameraManager.Init(PlayerCharacter);

        List<int> availableTeamIndexes = new List<int>();
        availableTeamIndexes.Add(0);
        for(int i = 1; i < totalCharacters/2; i++)
        {
            availableTeamIndexes.Add(i);
            availableTeamIndexes.Add(i);
        }

        var isTrainingMode = GameManager.Instance.IsTrainingMode;
        var characters = GameDatabase.GetAllCharacters();
        for(int i =  1; i < totalCharacters; i++)
        {
            var randomChar = characters[UnityEngine.Random.Range(0, characters.Count)];
            int random = UnityEngine.Random.Range(0, availableTeamIndexes.Count);
            var botData = new PlayerData()
            {
                Uid = i.ToString(),
                Id = (ulong)i,
                DisplayName = "Bot" + i,
                SelectedCharacterId = randomChar.InternalID,
                PositionId = 0,
                SteamId = "Bot" + i,
                TeamIndex = isTrainingMode ? -1 : availableTeamIndexes[random]
            };
            availableTeamIndexes.RemoveAt(random);
            var botChar = CreateCharacter(botData, playerData.Id);
            var bot = botChar.gameObject.AddComponent<CharacterAI>();
            bot.Init(botProfile, botChar);
        }

        NumberOfCharactersAlive = Characters.Count;
        OnUpdateHealth(0, 0);
        CreateTeams();
        AssignNemesisToAllCharacters();

        foreach (var c in Characters)
        {
            c.InitView();
        }

        EventBus.TriggerEvent(EventBusEnum.EventName.CHARACTERS_INITIALIZED_CLIENT);
    }

    private void CreateTeams()
    {
        int teamSize = GlobalGameSettings.Settings.TeamSize;
        bool lessThanTwoTeams = Characters.Count < teamSize * 2;

        if(lessThanTwoTeams)
        {
            teamSize = 1;
        }

        var isPreAssignedTeam = Characters.All(character => character.TeamIndex > -1);
        
        // TODO: refactor the code below
        if (isPreAssignedTeam)
        {
            Characters = Characters.OrderBy(c => c.PlayerData.TeamIndex).ToList();
            var maxTeamIndex = Characters.Max(c => c.TeamIndex);
            for (int i = 0; i <= maxTeamIndex; i++)
            {
                Teams.Add(new List<Character>());
            }

            NumberOfTeamsAlive = Characters.Count / teamSize;
            SetPreAssignedTeam(teamSize);
            return;
        }
        
        for (int i = 0; i < Characters.Count / teamSize; i++)
        {
            Teams.Add(new List<Character>());
            NumberOfTeamsAlive++;
        }

        AssignTeam(teamSize);
        
    }

    private void SetPreAssignedTeam(int teamSize)
    {
        float teamAngle = 360 / NumberOfTeamsAlive;
        int characterIndex = 0;

        for (int i = 0; i < Characters.Count; i++)
        {
            var teamIndex = Characters[i].TeamIndex;
            Character c = Characters[i];
            
            float angle = teamAngle * teamIndex + teammateDistanceAngle * characterIndex;
            var spawnPoint = Quaternion.AngleAxis(angle, Vector3.up) * new Vector3(0, spawnPointHeight, -spawnPointDistanceFromCenter);
            
            c.Index = i;
            c.CharacterView.SetTeamInfo();
            c.transform.position = spawnPoint;
            Teams[c.TeamIndex].Add(c);
            characterIndex++;
            if (characterIndex == teamSize)
            {
                characterIndex = 0;
            }
        }
    }

    private void AssignTeam(int teamSize)
    {
        float teamAngle = 360 / NumberOfTeamsAlive;
        int teamIndex = 0;
        int characterIndex = 0;

        if (usePositionIds)
        {
            Characters = Characters.OrderBy(c => c.PlayerData.PositionId).ToList();
        }

        for (int i = 0; i < Characters.Count; i++)
        {
            float angle = teamAngle * teamIndex + teammateDistanceAngle * characterIndex;
            Vector3 spawnPoint = Quaternion.AngleAxis(angle, Vector3.up) * new Vector3(0, spawnPointHeight, -spawnPointDistanceFromCenter);

            Character c = Characters[i];
            c.TeamIndex = teamIndex;
            c.Index = i;
            c.CharacterView.SetTeamInfo();
            c.transform.position = spawnPoint;
            Teams[c.TeamIndex].Add(c);

            characterIndex++;
            if(characterIndex == teamSize)
            {
                characterIndex = 0;
                teamIndex++;
            }
        }
    }
    
    private void AssignNemesisToAllCharacters()
    {
        if(GameManager.Instance.DisableRP)
        {
            return;
        }

        foreach (var c in Characters)
        {
            if(c.Nemesis == null)
            {
                AssignNemesis(c);
            }
        }
    }

    private void AssignNemesis(Character character)
    {
        var availableNemesis = Characters.Where(c => c.TeamIndex != character.TeamIndex && c.Nemesis == null).ToList();
        if(availableNemesis.Count > 0)
        {
            var randomCharacter = availableNemesis[UnityEngine.Random.Range(0, availableNemesis.Count)];
            character.AssignNemesis(randomCharacter);
            randomCharacter.AssignNemesis(character);
        }
    }

    public Character CreateCharacter(PlayerData playerData, ulong ownerId)
    {
        var character = Instantiate(characterPrefab);
        character.GetComponent<NetworkObject>().SpawnWithOwnership(ownerId);
        Characters.Add(character);
        character.Init(playerData);
        character.StateChangedEvent += OnCharacterStateChanged;
        character.Health.OnValueChanged += OnUpdateHealth;
        character.HealthChangedByCardEvent += OnHealthChangedByCard;
        character.CardBatchDiscardedEvent += (sourceCard, discardAmount) => OnCharacterCardBatchDiscarded(character, sourceCard, discardAmount);
        return character;
    }

    private void OnCharacterCardBatchDiscarded(Character character, Card sourceCard, int discardAmount)
    {
        CharacterCardBatchDiscardedEvent?.Invoke(character, sourceCard, discardAmount);
    }

    private void OnHealthChangedByCard(Character character, int healthChange, Card card)
    {
        CharacterHealthChangedByCardEvent?.Invoke(character, healthChange, card);
    }

    public ITargetable GetTargetable(string targetId)
    {
        if(string.IsNullOrEmpty(targetId))
        {
            return null;
        }

        if(targetId == World.WORLD_TARGET_ID)
        {
            return GameManager.Instance.World;
        }

        string characterSlotId = targetId;
        string suffix = null;

        if(targetId.Contains("#"))
        {
            var splitString = targetId.Split("#");
            characterSlotId = splitString[0];
            suffix = splitString[1];
        }

        if(suffix != null && suffix.Contains("Projectile"))
        {
            var projectiles = GameManager.GetManager<CardsEffectsManager>().Projectiles;

            foreach(var p in projectiles)
            {
                if(((ITargetable)p).TargetId == targetId)
                {
                    return p;
                }
            }

            Debug.Log($"Couldn't find projectile with Projectile Id {targetId}");
            return null;
        }

        Character character = null;

        foreach (var c in Characters)
        {
            if (((ITargetable)c).TargetId == targetId)
            {
                return c;
            }

            if (c.PlayerData.Id.ToString() == characterSlotId)
            {
                character = c;
            }
        }

        if(suffix == null)
        {
            return character;
        }

        var subTargetable = character.TryGetSubtargetable(targetId);

        if(subTargetable != null)
        {
            return subTargetable;
        }

        //TODO: Process prefix to get different targetables
        Debug.Log($"Couldn't find targetable {targetId}");
        return null;
    }

    public Character GetCharacterById(string playerId)
    {
        foreach(var c in Characters)
        {
            if(c.PlayerData.Id.ToString() == playerId)
            {
                return c;
            }
        }
        Debug.LogError($"Couldn't find a Character with Player Id {playerId}");
        return null;
    }

    public List<Character> GetAllOpponents(Character sourceCharacter)
    {
        return Characters
            .Where(c => c.TeamIndex != sourceCharacter.TeamIndex)
            .ToList();
    }
    public List<Character> GetAllAllies(Character sourceCharacter)
    {
        return Characters
            .Where(c => c.TeamIndex == sourceCharacter.TeamIndex && c != sourceCharacter)
            .ToList();
    }

    public Character GetRandomOpponent(Character sourceCharacter)
    {
        var list = GetAllOpponents(sourceCharacter);

        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    private void OnUpdateHealth(int oldHealth, int newHealth)
    {
        List<Character> orderedChars =
            Characters.OrderBy(oc => oc.Health.Value).ToList<Character>();

        LowestCharacter = orderedChars[0];
        OnLowestPlayer?.Invoke(LowestCharacter);
        HighestCharacter = orderedChars[orderedChars.Count - 1];
        OnHighestPlayer?.Invoke(HighestCharacter);
    }

    private void OnCharacterStateChanged(Character character)
    {
        NumberOfCharactersAlive = 0;

        foreach(var c in Characters)
        {
            if(c.State.Value == CharacterState.Alive)
            {
                NumberOfCharactersAlive++;
            }
        }

        NumberOfTeamsAlive = 0;
        WinningTeamIndex = -1;
        for (int i = 0; i < Teams.Count; i++)
        {
            foreach (var c in Teams[i])
            {
                if (c.State.Value == CharacterState.Alive)
                {
                    WinningTeamIndex = i;
                    NumberOfTeamsAlive++;
                    break;
                }
            }
        }

        if(NumberOfTeamsAlive > 1)
        {
            WinningTeamIndex = -1;
        }

        CharacterStateChangedEvent?.Invoke(character);
    }

#if UNITY_EDITOR
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.M))
        {
            PlayerCharacter.DebugMaxMana();
        }
    }
#endif

    public override void OnDestroy()
    {
        if(playersManager != null)
        {
            playersManager.PlayerDisconnectedEvent -= OnCharacterDisconnected;
        }
    }

    public void DebugMakePlayerPlayCard(ulong playerId, string cardId)
    {
        if(Debug.isDebugBuild)
        {
            DebugMakePlayerPlayCardServerRPC(playerId, cardId);
        }
    }

    [ServerRpc]
    private void DebugMakePlayerPlayCardServerRPC(ulong playerId, string cardId)
    {
        var cardData = GameDatabase.GetCardData(cardId);

        Character randomBot = null;
        foreach(var c in Characters)
        {
            if (c.PlayerData.Id == playerId)
            {
                randomBot = c;
                break;
            }
        }

        ITargetable chosenTarget = null;
        switch (cardData.Target)
        {
            case CardTarget.Any:
            case CardTarget.AnyOpponent:
            case CardTarget.AnyPlayer:
            case CardTarget.AllOpponents:
                chosenTarget = PlayerCharacter;
                break;

            case CardTarget.Self:
                chosenTarget = randomBot;
                break;

            case CardTarget.Teammate:
                foreach (var c in GetAllAllies(randomBot))
                {
                    chosenTarget = c;
                    break;
                }
                break;
            case CardTarget.Spell:
            case CardTarget.OpponentsSpell:
                var projectileList = GameManager.GetManager<CardsEffectsManager>().Projectiles;
                if (projectileList.Count > 0)
                {
                    chosenTarget = projectileList[0];
                }
                break;
            case CardTarget.World:
                chosenTarget = GameManager.Instance.World;
                break;
            case CardTarget.SpellInCast:
                if (PlayerCharacter.IsCasting.Value)
                {
                    chosenTarget = PlayerCharacter.CastingTargetable;
                }
                break;
            case CardTarget.DownedPlayer:
                foreach (var c in Characters)
                {
                    if (c.State.Value == CharacterState.Downed)
                    {
                        chosenTarget = c;
                        break;
                    }
                }
                break;
            case CardTarget.Charm:
                foreach(var c in PlayerCharacter.CharmSlots)
                {
                    if(!c.IsEmpty)
                    {
                        chosenTarget = c;
                        break;
                    }
                }
                break;
            case CardTarget.EmptyCharmSlot:
                foreach (var c in PlayerCharacter.CharmSlots)
                {
                    if (c.IsEmpty)
                    {
                        chosenTarget = c;
                        break;
                    }
                }
                break;
            case CardTarget.CardInHand:
                foreach (var c in randomBot.Hand)
                {
                    if (c != null)
                    {
                        chosenTarget = c;
                        break;
                    }
                }
                break;
            default:
                Debug.LogError($"Couldn't find available Targetable for Card Target {cardData.Target}");
                return;
        }

        var card = new Card(cardData);
        randomBot.AddCardToHand(card, 0);

        randomBot.StartCasting(card, chosenTarget, true);
    }

    public void DebugToggleCasting(bool isCastTimeEnabled)
    {
        if (!Debug.isDebugBuild)
        {
            return;
        }

        foreach(var c in Characters)
        {
            c.SetIgnoreCasting(!isCastTimeEnabled);
        }
    }

    public void DebugToggleAI(bool isAIEnabled)
    {
        if (!Debug.isDebugBuild)
        {
            return;
        }

        AreBotsActive = isAIEnabled;
    }

    public void DebugAddCharmToPlayer(ulong playerId, string charmId)
    {
        if (!Debug.isDebugBuild)
        {
            return;
        }
        DebugAddCharmToPlayerServerRPC(playerId, charmId);
    }

    [ServerRpc]
    private void DebugAddCharmToPlayerServerRPC(ulong playerId, string charmId)
    {
        var charmData = GameDatabase.GetCharmData(charmId);

        foreach(var c in Characters)
        {
            if (c.PlayerData.Id == playerId)
            {
                c.AddCharm(charmData);
            }
        }
    }
}
