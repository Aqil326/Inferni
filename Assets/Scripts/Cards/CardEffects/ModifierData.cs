using UnityEngine;

[CreateAssetMenu(fileName = "ModifierData", menuName = "Data/Modifier")]
public class ModifierData: ScriptableObjectWithId
{
    public string modifierName;
    public Sprite modifierIcon;
    public GameObject modifierEffectPrefab;
    public string description;
    public bool pauseAnimation;
    public int modifierScore;
}




