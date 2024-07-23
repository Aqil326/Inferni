
using System.Collections.Generic;
using UnityEngine;

public class DraftPackIconController : MonoBehaviour
{
    [SerializeField]
    private DraftPackIcon packIconPrefab;

    private RoundManager roundManager;

    private Queue<DraftPackIcon> iconPool = new Queue<DraftPackIcon>();
    private Dictionary<string, DraftPackIcon> assignedIcons = new Dictionary<string, DraftPackIcon>();

    private void Start()
    {
        EventBus.StartListening<DraftPackData>(EventBusEnum.EventName.DRAFT_PACK_PASS_CLIENT, OnDraftPackPassed);
        EventBus.StartListening<DraftPackData>(EventBusEnum.EventName.DRAFT_PACK_COMPLETE_CLIENT, OnDraftPackCompleted);
        roundManager = GameManager.GetManager<RoundManager>();
    }


    private DraftPackIcon GetIcon(DraftPackData draftPack)
    {
        DraftPackIcon icon = null;
        if (iconPool.Count > 0)
        {
            icon = iconPool.Dequeue();
            icon.gameObject.SetActive(true);
        }
        else
        {
            icon = Instantiate(packIconPrefab, Vector3.zero, Quaternion.identity, transform);
        }

        icon.Initialise(draftPack);
        assignedIcons.Add(draftPack.ID, icon);
        return icon;
    }

    private void OnDraftPackPassed(DraftPackData draftPackData)
    {
        if (!assignedIcons.TryGetValue(draftPackData.ID, out var icon))
        {
            icon = GetIcon(draftPackData);
        }
        icon.PassPack(draftPackData);
    }

    private void OnDraftPackCompleted(DraftPackData draftPackData)
    {
        if (assignedIcons.TryGetValue(draftPackData.ID, out var icon))
        {
            iconPool.Enqueue(icon);
            icon.gameObject.transform.position = Vector3.zero;
            icon.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        EventBus.StopListening<DraftPackData>(EventBusEnum.EventName.DRAFT_PACK_PASS_CLIENT, OnDraftPackPassed);
        EventBus.StopListening<DraftPackData>(EventBusEnum.EventName.DRAFT_PACK_COMPLETE_CLIENT, OnDraftPackCompleted);
    }
}
