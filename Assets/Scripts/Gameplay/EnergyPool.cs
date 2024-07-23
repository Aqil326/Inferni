using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class EnergyPool: NetworkBehaviour, ITargetable
{
    public NetworkVariable<int> Energy = new NetworkVariable<int>();
    public NetworkVariable<CardColors> Color = new NetworkVariable<CardColors>();
    public NetworkVariable<int> Level = new NetworkVariable<int>();
    public NetworkVariable<float> EnergyGenerationTimerRate = new NetworkVariable<float>();

    public int MaxEnergy => gameSettings.MaxEnergyAmounts[Level.Value - 1];
    public bool IsEmpty => Color.Value == CardColors.None;

    public int Index { get; private set; }
    public Character Owner { get; private set; }
    public int MaxLevel { get; private set; }

    private bool isPaused;

    public float GeneratingTime
    {
        get
        {
            float generatingTime = gameSettings.EnergyGenerationTimes[Level.Value - 1];

            if(Owner.Modifier is EnergyGeneratingRateModifier m)
            {
                generatingTime = m.ModifyGeneratingTime(generatingTime);
            }

            return generatingTime;
        }
    }

    string ITargetable.TargetId => Owner.PlayerData.Id + "#EnergyPool#" + Index;
    Vector3 ITargetable.Position => transform.position;

    private Coroutine energyGeneratingCoroutine;
    
    private GlobalGameSettings gameSettings;
    
    public void Init(Character character, int index)
    {
        Owner = character;
        Index = index;
        gameSettings = GlobalGameSettings.Settings;
        MaxLevel = gameSettings.MaxEnergyPoolLevel;
    }

    public bool CanAddCard(Card card)
    {
        if (card.Data.CardColor == Color.Value)
        {
            if (Level.Value == MaxLevel)
            {
                return false;
            }
        }
        return true;
    }

    public void AddCard(Card card)
    {
        if(!CanAddCard(card))
        {
            return;
        }

        if(card.Data.CardColor == Color.Value)
        {
            Level.Value++;

            if(energyGeneratingCoroutine == null)
            {
                energyGeneratingCoroutine = Owner.StartCoroutine(StartEnergyGeneration());
            }
        }
        else
        {
            Color.Value = card.Data.CardColor;
            Level.Value = 1;
            Energy.Value = 0;

            if(energyGeneratingCoroutine != null)
            {
                Owner.StopCoroutine(energyGeneratingCoroutine);
            }

            energyGeneratingCoroutine = Owner.StartCoroutine(StartEnergyGeneration());
        }
        AddEnergy(1);
    }

    public void IncreaseLevel(int amountOfLevels)
    {
        Level.Value += amountOfLevels;

        if(Level.Value > MaxLevel)
        {
            Level.Value = MaxLevel;
        }
    }

    public void DeductEnergy(int amount)
    {
        if(amount > Energy.Value)
        {
            Debug.LogError($"Cannot deduct {amount} energy from {Energy} Energy");
            return;
        }
        Energy.Value -= amount;

        if (energyGeneratingCoroutine == null)
        {
            energyGeneratingCoroutine = Owner.StartCoroutine(StartEnergyGeneration());
        }
    }

    public void AddEnergy(int amount)
    {
        Energy.Value += amount;

        if(Energy.Value >= MaxEnergy)
        {
            Energy.Value = MaxEnergy;

            if (energyGeneratingCoroutine != null)
            {
                Owner.StopCoroutine(energyGeneratingCoroutine);
                energyGeneratingCoroutine = null;
            }
        }
    }

    public void SetEnergyAndColor(CardColors color, int energyAmount)
    {
        Color.Value = color;
        Energy.Value = energyAmount;
    }

    private IEnumerator StartEnergyGeneration()
    {
        float timer = 0;
        while(timer < GeneratingTime)
        {
            yield return null;
            if (!isPaused)
            {
                EnergyGenerationTimerRate.Value = timer/GeneratingTime;
                timer += Time.deltaTime;
            }
        }

        energyGeneratingCoroutine = null;
        AddEnergy(1);
        if (Energy.Value < MaxEnergy)
        {
            energyGeneratingCoroutine = Owner.StartCoroutine(StartEnergyGeneration());
        }
    }

    public void Pause()
    {
        isPaused = true;
    }

    public void Unpause()
    {
        isPaused = false;
    }

#if UNITY_EDITOR
    public void DebugMaxMana()
    {
        if(Color.Value == CardColors.None)
        {
            return;
        }

        Level.Value = MaxLevel;
        AddEnergy(MaxEnergy);
    }
#endif

    bool ITargetable.ShouldOverrideTargetAvailability(ref bool canBeTargeted, Card card)
    {
        canBeTargeted = true;
        if (Level.Value == MaxLevel && card.Data.CardColor == Color.Value)
        {
            canBeTargeted = false;
        }
        return true;
    }
}
