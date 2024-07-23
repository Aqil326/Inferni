using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IncomingUI : MonoBehaviour
{
    private const float REORDER_TIMER = 1f;

    [SerializeField]
    private IncomingIconUI iconPrefab;

    [SerializeField]
    private Transform iconsParent;

    private Stack<IncomingIconUI> incomingIconsPool = new Stack<IncomingIconUI>();
    private List<IncomingIconUI> icons = new List<IncomingIconUI>();
    private CharacterManager characterManager;
    private float reorderTimer;

    private void Start()
    {
        EventBus.StartListening<CastEventParams>(EventBusEnum.EventName.START_CASTING_CLIENT, OnCastingStarted);
        EventBus.StartListening<ProjectileView>(EventBusEnum.EventName.PROJECTILE_SHOT_CLIENT, TryAddIncomingIcon);
        EventBus.StartListening<ProjectileView>(EventBusEnum.EventName.PROJECTILE_TARGET_CHANGED_CLIENT, TryAddIncomingIcon);
        gameObject.SetActive(false);
        characterManager = GameManager.GetManager<CharacterManager>();
    }

    private void OnCastingStarted(CastEventParams castParameters)
    {
        ITargetable playerTarget = characterManager.PlayerCharacter;
        if (castParameters.target == playerTarget)
        {
            var icon = AddIcon();
            icon.SetCastingCard(castParameters.card);
            ReorderIcons();
            if (icons.Count == 1)
            {
                gameObject.SetActive(true);
            }
        }
    }

    private void TryAddIncomingIcon(ProjectileView projectileView)
    {
        ITargetable playerTarget = characterManager.PlayerCharacter;
        if (projectileView.Projectile.Target == playerTarget ||
            (projectileView.Projectile.Target is CharmSlot slot && slot.Owner == characterManager.PlayerCharacter) ||
            (projectileView.Projectile.Target is CastingIcon cast && cast.Owner == characterManager.PlayerCharacter))
        {
            var icon = AddIcon();
            icon.SetProjectile(projectileView);
            ReorderIcons();
            if (icons.Count == 1)
            {
                gameObject.SetActive(true);
            }
        }
    }

    private IncomingIconUI AddIcon()
    {
        var icon = GetIconInstance();
        icon.transform.SetParent(iconsParent);
        icon.transform.localScale = Vector3.one;
        icon.IconDisabledEvent += OnIconDisabled;
        icons.Add(icon);
        gameObject.SetActive(true);
        return icon;
    }

    private void Update()
    {
        if(icons.Count >= 2)
        {
            reorderTimer += Time.deltaTime;
            if (reorderTimer >= REORDER_TIMER)
            {
                ReorderIcons();
            }
        }
    }

    private void ReorderIcons()
    {
        if (icons.Count < 2)
        {
            return;
        }

        var reorderedIcons = icons.OrderByDescending(i => i.RemainingTime).ThenBy(i => i.IsProjectile).ToList();

        if (reorderedIcons.SequenceEqual(icons))
        {
            return;
        }
        icons = reorderedIcons;

        for (int i = 0; i < icons.Count; i++)
        {
            icons[i].TryClearPointerEvents();
            icons[i].transform.SetSiblingIndex(i);
        }
        reorderTimer = 0;
    }

    private void OnIconDisabled(IncomingIconUI icon)
    {
        icon.IconDisabledEvent -= OnIconDisabled;
        icon.transform.SetParent(null);
        incomingIconsPool.Push(icon);
        icons.Remove(icon);
        ReorderIcons();

        if(icons.Count == 0)
        {
            gameObject.SetActive(false);
        }
    }

    private IncomingIconUI GetIconInstance()
    {
        if(incomingIconsPool.Count > 0)
        {
            var i = incomingIconsPool.Pop();
            i.gameObject.SetActive(true);
            return i;
        }

        return Instantiate(iconPrefab);
    }

    private void OnDestroy()
    {
        EventBus.StopListening<CastEventParams>(EventBusEnum.EventName.START_CASTING_CLIENT, OnCastingStarted);
        EventBus.StopListening<ProjectileView>(EventBusEnum.EventName.PROJECTILE_SHOT_CLIENT, TryAddIncomingIcon);
        EventBus.StopListening<ProjectileView>(EventBusEnum.EventName.PROJECTILE_TARGET_CHANGED_CLIENT, TryAddIncomingIcon);
    }
}
