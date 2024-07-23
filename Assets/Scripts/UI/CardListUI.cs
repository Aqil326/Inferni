using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardListUI : MonoBehaviour
{
    [SerializeField]
    private CardUI cardUIPrefab;

    [SerializeField]
    private CharmUI charmUIPrefab;

    [SerializeField]
    private Transform cardUIParent;

    [SerializeField]
    private ScrollRect scrollRect;

    private List<CardUI> cardUIs = new List<CardUI>();
    private List<CharmUI> charmUIs = new List<CharmUI>();
    private CardData[] cardDatas;
    private CharmData[] charmDatas;

    public void Init(CardData[] cardDatas, CharmData[] charmDatas)
    {
        this.cardDatas = cardDatas;
        this.charmDatas = charmDatas;
    }

    public void Show()
    {
        gameObject.SetActive(true);

        int i = 0;
        if (cardDatas != null)
        {
            CardUI cardUI = null;
            for (; i < cardDatas.Length; i++)
            {
                if (i < cardUIs.Count)
                {
                    cardUI = cardUIs[i];
                    cardUI.gameObject.SetActive(true);
                }
                else
                {
                    cardUI = Instantiate(cardUIPrefab, cardUIParent);
                    cardUIs.Add(cardUI);
                }
                cardUI.SetCard(new Card(cardDatas[i]));
            }

            for (; i < cardUIs.Count; i++)
            {
                cardUIs[i].gameObject.SetActive(false);
            }
        }

        if (charmDatas != null)
        {
            i = 0;
            CharmUI charmUI = null;
            for (; i < charmDatas.Length; i++)
            {
                if (i < charmUIs.Count)
                {
                    charmUI = charmUIs[i];
                    charmUI.gameObject.SetActive(true);
                }
                else
                {
                    charmUI = Instantiate(charmUIPrefab, cardUIParent);
                    charmUIs.Add(charmUI);
                }
                charmUI.SetCharm(new Charm(charmDatas[i]));
            }
        }
        scrollRect.verticalNormalizedPosition = 1;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
