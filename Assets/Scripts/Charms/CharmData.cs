using UnityEngine;

public abstract class CharmData : ScriptableObjectWithId
{
    public string charmName;
    public Sprite charmIcon;
    public string description;
    public int score;

    public abstract Charm GetCharm();
}




