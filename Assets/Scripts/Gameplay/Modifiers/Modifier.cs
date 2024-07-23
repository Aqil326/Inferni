using System;
using System.Collections;
using UnityEngine;

public abstract class Modifier
{
    public string ID { get; private set; }

    public float Duration;
    public Sprite ModifierSprite => Data.modifierIcon;
    public string Description => Data.description;
    public string ModifierName => Data.modifierName;

    public ModifierData Data { get; private set; }
    public bool UseProjectileLifetimeMultiplier { get; private set; }
    public float Timer { get; private set; }
    public event Action TimerUpdatedEvent;
    public event Action<Modifier> ModifierRemoveEvent;

    protected Coroutine timerCoroutine;
    protected MonoBehaviour modifee;
    protected bool isPaused;

    public Modifier(string id, ModifierData modifierData, float duration, bool useProjectileLifetimeMultiplier)
    {
        ID = id;
        Data = modifierData;
        Duration = duration;
        UseProjectileLifetimeMultiplier = useProjectileLifetimeMultiplier;
    }

    protected void Init(MonoBehaviour modifee)
    {
        this.modifee = modifee;

        if (Duration > 0)
        {
            timerCoroutine = modifee.StartCoroutine(ModifierTimerCoroutine());
        }
    }

    private IEnumerator ModifierTimerCoroutine()
    {
        float timer = 0;

        while (timer < Duration)
        {
            yield return null;
            if (!isPaused)
            {
                Timer = timer;
                timer += Time.deltaTime;
                TimerUpdatedEvent.Invoke();
            }
        }
        timerCoroutine = null;
        RemoveModifier();
    }

    public void Pause()
    {
        isPaused = true;
    }

    public void Unpause()
    {
        isPaused = false;
    }

    public virtual void RemoveModifier()
    {
        if (timerCoroutine != null)
        {
            modifee.StopCoroutine(timerCoroutine);
        }
        ModifierRemoveEvent?.Invoke(this);
    }
}
