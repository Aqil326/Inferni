using System.Collections.Generic;
using UnityEngine;

public static class ListExtensions
{
    //Fisher-Yates shuffle
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(1, n);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}


