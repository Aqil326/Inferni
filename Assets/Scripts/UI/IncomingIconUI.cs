using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IncomingIconUI : MonoBehaviour
{
    [SerializeField] 
    private IconWithMaskController icon;

    [SerializeField]
    private Image castingTimer;

    [SerializeField]
    private Image selectedHighlight;

    [SerializeField]
    private TextMeshProUGUI originPlayerText;

    public event Action<IncomingIconUI> IconDisabledEvent;

    public bool IsProjectile => ProjectileView != null;

    public float RemainingTime
    {
        get
        {
            if (ProjectileView == null)
            {
                if (Card == null)
                {
                    return 0;
                }
                return Card.Owner.RemainingCastingTime.Value;
            }
            return ProjectileView.Projectile.TimeToContact.Value;
        }
    }

    public Card Card { get; private set; }
    public ProjectileView ProjectileView { get; private set; }
    private bool isPointerOver;

    private void OnEnable()
    {
        selectedHighlight.enabled = false;
    }

    public void SetCastingCard(Card card)
    {
        if(Card != null)
        {
            Card.Owner.CardCastingEndEvent -= OnCastingEnded;

            if (isPointerOver)
            {
                TryClearPointerEvents();
            }
        }
        Card = card;
        icon.InitIcon(card.Data.CardSprite);   
        card.Owner.CardCastingEndEvent += OnCastingEnded;
        SetOriginPlayerName(card.Owner);
        originPlayerText.gameObject.SetActive(false);
        castingTimer.enabled = true;
    }

    public void SetProjectile(ProjectileView projectile)
    {
        if(ProjectileView != null)
        {
            projectile.Projectile.ProjectileDestroyedEvent -= OnProjectileDestroyed;
            projectile.Projectile.ProjectileTargetChangedEvent -= ProjectileTargetChanged;

            if (isPointerOver)
            {
                TryClearPointerEvents();
            }
        }
        ProjectileView = projectile;
        Card = projectile.Projectile.Card;
        icon.InitIcon(projectile.Projectile.Card.Data.CardSprite);
        projectile.Projectile.ProjectileDestroyedEvent += OnProjectileDestroyed;
        projectile.Projectile.ProjectileTargetChangedEvent += ProjectileTargetChanged;
        SetOriginPlayerName(projectile.Projectile.Owner);
        originPlayerText.gameObject.SetActive(false);
        castingTimer.enabled = false;

        
    }

    private void ProjectileTargetChanged(Projectile projectile, string oldProjectileId)
    {
        OnProjectileDestroyed(projectile);
    }

    private void SetOriginPlayerName(Character character)
    {
        originPlayerText.text = character.PlayerData.DisplayName;
    }

    private void OnCastingEnded()
    {
        Card.Owner.CardCastingEndEvent -= OnCastingEnded;
        gameObject.SetActive(false);
        IconDisabledEvent?.Invoke(this);
        Card = null;
    }

    private void OnProjectileTargetChanged(Projectile projectile)
    {
        if(projectile == ProjectileView)
        {
            OnProjectileDestroyed(projectile);
        }
    }

    private void OnProjectileDestroyed(Projectile projectile)
    {
        projectile.ProjectileDestroyedEvent -= OnProjectileDestroyed;
        projectile.ProjectileTargetChangedEvent -= ProjectileTargetChanged;
        gameObject.SetActive(false);
        IconDisabledEvent?.Invoke(this);
    }

    private void Update()
    {
        if(Card == null)
        {
            return;
        }

        castingTimer.fillAmount = Card.Owner.RemainingCastingTime.Value / Card.GetCardCastingTime();
    }

    public void OnPointerEnter()
    {
        isPointerOver = true;

        var cardInspectedData = new CardInspectedData()
        {
            card = Card,
            InFlight = true,
            inspectedRect = GetComponent<RectTransform>(),
            tooltipPivot = new Vector3(0.5f, 1)
        };
        EventBus.TriggerEvent<CardInspectedData>(EventBusEnum.EventName.CARD_INSPECTED_CLIENT, cardInspectedData);
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_SELECTED_CLIENT, GetTarget());
        originPlayerText.gameObject.SetActive(true);
        selectedHighlight.enabled = true;

        Debug.Log($"Pointer Enter {Card.Data.Name}");
    }

    public void OnPointerExit()
    {
        isPointerOver = false;
        EventBus.TriggerEvent<Card>(EventBusEnum.EventName.CLEAR_CARD_INSPECTED_CLIENT, Card);
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_DESELECTED_CLIENT, GetTarget());
        originPlayerText.gameObject.SetActive(false);
        selectedHighlight.enabled = false;
        Debug.Log($"Pointer Exit {Card.Data.Name}");
    }

    private TargetableView GetTarget()
    {
        TargetableView target = null;
        if (ProjectileView != null)
        {
            target = ProjectileView;
        }
        return target;
    }

    public void TryClearPointerEvents()
    {
        if (isPointerOver)
        {
            OnPointerExit();
        }
    }

    private void OnDisable()
    {
        TryClearPointerEvents();
    }
}
