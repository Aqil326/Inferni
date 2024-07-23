using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class CardProjectile : NetworkBehaviour
{
    [SerializeField]
    private Card3D cardUI;

    private Coroutine projectileMoveCoroutine;
    private bool isPaused;
    private CardAddedData cardData;
    private float timer;
    private float totalTime;
    private float speed;
    private Action<CardProjectile> movementFinishedCallback;
    private bool addToDeck;

    public void SetCard(Card card)
    {
        cardUI.SetCard(card);
    }

    public void MoveProjectile(CardAddedData cardData, bool addToDeck, Action<CardProjectile> movementFinishedCallback)
    {
        transform.position = cardData.currentPosition;
        speed = GlobalGameSettings.Settings.GetCardProjectileSpeed(GameManager.Instance.ArenaGroup);

        this.cardData = cardData;
        this.addToDeck = addToDeck;
        this.movementFinishedCallback = movementFinishedCallback;
        totalTime = Vector3.Distance(cardData.currentPosition, cardData.targetCharacter.transform.position) / speed;

        if (projectileMoveCoroutine != null)
        {
            StopCoroutine(projectileMoveCoroutine);
        }
        projectileMoveCoroutine = StartCoroutine(ProjectileMoveCoroutine());
    }

    private IEnumerator ProjectileMoveCoroutine()
    {
        timer = 0;

        while(timer < totalTime)
        {
            yield return null;

            if(!isPaused)
            {
                transform.position = Vector3.Lerp(cardData.currentPosition, cardData.targetCharacter.transform.position, timer / totalTime);
                timer += speed * Time.deltaTime;
            }
        }
        if (addToDeck)
        {
            cardData.targetCharacter.AddCardToTopDeck(cardData.card, cardData.targetCharacter.transform.position);
        }
        else
        {
            cardData.targetCharacter.AddCardToHand(cardData.card, cardData.targetCharacter.transform.position);
        }
        movementFinishedCallback?.Invoke(this);
    }

    public void Pause()
    {
        isPaused = true;
    }

    public void Unpause()
    {
        isPaused = false;
    }
}


