using System;
using UnityEngine;

public interface ITargetable
{
    public string TargetId { get; }
    public Vector3 Position { get; }
    public bool ShouldOverrideTargetAvailability(ref bool canBeTargeted, Card card);
}

public abstract class TargetableView : MonoBehaviour
{
    public ITargetable Targetable { get; protected set; }

    protected virtual void Start()
    {
        EventBus.StartListening<Card>(EventBusEnum.EventName.CARD_DRAG_STARTED_CLIENT, OnCardDragStarted);
        EventBus.StartListening<Card>(EventBusEnum.EventName.CARD_DRAG_ENDED_CLIENT, OnCardDragEnded);
    }

    protected virtual void OnDestroy()
    {
        EventBus.StopListening<Card>(EventBusEnum.EventName.CARD_DRAG_STARTED_CLIENT, OnCardDragStarted);
        EventBus.StopListening<Card>(EventBusEnum.EventName.CARD_DRAG_ENDED_CLIENT, OnCardDragEnded);
    }

    public virtual void SetTargetable(ITargetable targetable)
    {
        Targetable = targetable;
    }

    public virtual bool ShouldOverrideTargetAvailability(ref bool canBeTargeted, Card card)
    {
        return Targetable.ShouldOverrideTargetAvailability(ref canBeTargeted, card);
    }

    public abstract void TargetSelected(bool isValidTarget);
    public abstract void TargetDeselected();

    protected abstract void OnCardDragStarted(Card card);
    protected abstract void OnCardDragEnded(Card card);
}
