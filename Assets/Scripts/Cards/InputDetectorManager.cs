
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;
using DG.Tweening;
using static RoundManager;

public class InputDetectorManager : BaseManager
{
    [SerializeField]
    private float wordDetectorDelay = 1f;
    [SerializeField]
    private float baconAnimationDuration = 2f;
    [SerializeField]
    private NetworkObject baconEntityPrefab;
    [SerializeField]
    private Transform baconSpawn;
    [SerializeField]
    private Transform cardSpawn;
    [SerializeField]
    private CardData baconCard;

    private Boolean eggTriggered = false;
#if UNITY_SERVER
    private const string SECRET_WORD_1 = "rand";
    private const string SECRET_WORD_2 = "olph";
#else
    private const string SECRET_WORD_1 = "dumm";
    private const string SECRET_WORD_2 = "yyyy";
#endif

    private List<CodewordData> previousCodewords = new List<CodewordData>();
    private CharacterManager characterManager;
    private string currentTypedWord;
    private Coroutine typedWordCoroutine;

    public override void Init()
    {
        base.Init();

        characterManager = GameManager.GetManager<CharacterManager>();
    }

    private void Update()
    {
        if(IsServer || !GameManager.Instance.IsMatchActive || GameManager.GetManager<RoundManager>().CurrentRound.roundType == Round.RoundType.Draft)
        {
            return;
        }

        if (Input.anyKeyDown)
        {
            currentTypedWord += Input.inputString;

            if(!string.IsNullOrEmpty(currentTypedWord))
            {
                if (typedWordCoroutine != null)
                {
                    StopCoroutine(typedWordCoroutine);
                }

                typedWordCoroutine = StartCoroutine(CheckTypedWordCoroutine());
            }
        }
    }

    private IEnumerator CheckTypedWordCoroutine()
    {
        yield return new WaitForSeconds(wordDetectorDelay);

        Debug.Log(currentTypedWord);

        SendWordServerRpc(characterManager.PlayerCharacter.Index, currentTypedWord);

        currentTypedWord = string.Empty;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendWordServerRpc(int playerIndex, string word)
    {
        Debug.Log($"Received word {word} from Player {playerIndex}");

        if (!eggTriggered)
        {
            Debug.Log("eggTriggered = false");
            word = word.ToLower();
#if UNITY_SERVER
            if(word != SECRET_WORD_1 && word != SECRET_WORD_2)
            {
                return;
            }


            var player = characterManager.Characters[playerIndex];
            if (word == SECRET_WORD_2)
            {
                foreach (var cw in previousCodewords)
                {
                    if (player.Index == cw.character.Index)
                    {
                        //Player already sent a word
                        return;
                    }

                    if (player.TeamIndex == cw.character.TeamIndex)
                    {
                        eggTriggered = true;
                        TriggerBacon();
                        return;
                    }
                }
            }

            if (word == SECRET_WORD_1)
            {
                previousCodewords.Add(new CodewordData() { character = player, codeword = word });
            }
#endif
        }

    }

    private void TriggerBacon()
    {
        var bacon = Instantiate(baconEntityPrefab, baconSpawn.position, Quaternion.identity);
        bacon.Spawn();

        DOTween.Sequence().Append(bacon.transform.DOMove(cardSpawn.position, baconAnimationDuration).SetEase(Ease.InQuart))
            .AppendCallback(SendCards)
            .AppendInterval(baconAnimationDuration)
            .AppendCallback(() =>
            {
                bacon.Despawn(true);
            });
    }


    private void SendCards()
    { 
        var card = new Card(baconCard);

        foreach (var c in characterManager.Characters)
        {
            if (!c.IsDead)
            {
                if (c.HandSize < c.Hand.Length)
                {
                    c.AddCardToHandWithPosition(card, cardSpawn.position);
                }
                else
                {
                    c.AddCardToTopDeckWithPosition(card, cardSpawn.position);
                }
            }
        }
    }
}


public struct CodewordData
{
    public Character character;
    public string codeword;
}




