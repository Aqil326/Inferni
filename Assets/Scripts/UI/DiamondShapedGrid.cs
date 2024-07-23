using System;
using System.Collections.Generic;
using UnityEngine;

public class DiamondShapedGrid : MonoBehaviour
{
    [SerializeField]
    private int topRowLength;
    [SerializeField]
    private int bottomRowLength;
    [SerializeField]
    private Transform rowPrefab;
    [SerializeField]
    private GameObject contentPrefab;

    public void Init<T>(int totalItemsCount, Action<int, T> contentInitialized)
    {
        bool isTop = true;
        int rowIndex = 0;
        Transform currentRow = null;

        for(int i = 0; i < totalItemsCount; i++)
        {
            if(rowIndex == 0)
            {
                currentRow = Instantiate(rowPrefab, transform);
            }
            var content = Instantiate(contentPrefab, currentRow);
            contentInitialized?.Invoke(i, content.GetComponent<T>());
            rowIndex++;

            if(rowIndex == (isTop? topRowLength : bottomRowLength))
            {
                rowIndex = 0;
                isTop = !isTop;
            }
        }

    }
}
