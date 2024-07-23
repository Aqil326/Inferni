using UnityEngine;

public class Card3D : MonoBehaviour
{
    [SerializeField]
    private CardUI ui;

    private void Start()
    {
        ui.BlockPointerEvents();
    }

    public void SetCard(Card card)
    {
        ui.SetCard(card, is3D: true);
    }

    public void SetCardTarget(CharacterView view)
    {
        ui.SetXValue(CardXPreviewType.TrySetTarget, view);
    }
}