
using System;
using UnityEngine;

public class Charm
{
    public CharmData CharmData { get; private set; }
    public CharmSlot CharmSlot;

    protected Character character;
    protected CharacterManager characterManager;

    public event Action<Charm> CharmRemovedEvent;

    public Charm(CharmData charmData)
    {
        this.CharmData = charmData;
        characterManager = GameManager.GetManager<CharacterManager>();
    }

    public virtual void AttachToCharacter(Character character)
    {
        this.character = character;
    }

    public virtual void RemoveCharm()
    {
        CharmRemovedEvent?.Invoke(this);
    }

    public Charm Clone()
    {
        var typeName = GetType().Name;
        var charmType = Type.GetType(typeName);
        
        if (charmType == null)
        {
            Debug.LogError($"Charm type '{typeName}' not found.");
            return null;
        }

        if (!typeof(Charm).IsAssignableFrom(charmType))
        {
            Debug.LogError($"Type '{typeName}' is not a Charm.");
            return null;
        }

        var charmInstance = (Charm)Activator.CreateInstance(charmType, new object[] { CharmData });
        
        return charmInstance;
    }
    protected T GetData<T>() where T:CharmData
    {
        return CharmData as T;
    }
}
