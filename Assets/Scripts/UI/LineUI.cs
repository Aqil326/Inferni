using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
struct ScaleMetric
{
    public float From;
    public float To;
    public float Scale;
}

public class LineUI : MonoBehaviour
{
    [SerializeField]
    private LineRenderer lineRenderer;
    [SerializeField]
    private List<ScaleMetric> scales = new List<ScaleMetric>();
    
    private void Start()
    {
        if (lineRenderer == null) return;
        gameObject.SetActive(false);
    }

    private float GetScale(float length)
    {
        var metric = scales.Find(metric => length >= metric.From && length < metric.To);
        return metric.Equals(default(ScaleMetric)) ? scales[scales.Count - 1].Scale : metric.Scale;
    }
    
    public void SetLinePositions(Vector3 beginning, Vector3 end)
    {
        lineRenderer.SetPosition(0, beginning);
        lineRenderer.SetPosition(1, end);
        var length = Vector3.Distance(beginning, end);
        lineRenderer.textureScale = new Vector2(GetScale(length), 1f);
    }
    
    public void Show()
    {
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
