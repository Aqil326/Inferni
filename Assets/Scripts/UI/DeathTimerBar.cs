using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeathTimerBar : MonoBehaviour
{
    [SerializeField]
    private Image dealDeathTimerBar;

    [SerializeField]
    private TextMeshProUGUI deathTimer;

    private Character character;

    public void Init(Character character)
    {
        gameObject.SetActive(false);
        this.character = character;
        character.State.OnValueChanged += OnUpdateState;
        character.DeathTimer.OnValueChanged += OnUpdateDeathTimer;
        OnUpdateDeathTimer(character.DeathTimer.Value, character.DeathTimer.Value);
    }

    private void OnUpdateState(CharacterState oldState, CharacterState newState)
    {
        gameObject.SetActive(newState == CharacterState.Downed && character.State.Value == CharacterState.Downed);
    }
    
    private void OnUpdateDeathTimer(int oldTimer, int newTimer)
    {
        var maxTimer = character.MaxDeathTimer;
        dealDeathTimerBar.fillAmount = ((float) newTimer) / ((float) maxTimer);

        if(deathTimer != null)
        {
            deathTimer.text = newTimer + "/" + maxTimer;
        }
    }

    private void OnDestroy()
    {
        if(character != null)
        {
            character.State.OnValueChanged -= OnUpdateState;
            character.DeathTimer.OnValueChanged -= OnUpdateDeathTimer;
        }
    }
}
