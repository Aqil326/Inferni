using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LineManager : MonoBehaviour
{
    private class LineInfo
    {
        public ITargetable Beginning;
        public ITargetable End;
        public readonly LineUI LineUI;
        public bool IsVisible;
        public readonly string SourceId;

        public LineInfo(ITargetable beginning, ITargetable end, LineUI lineUI, string sourceId)
        {
            Beginning = beginning;
            End = end;
            LineUI = lineUI;
            IsVisible = false;
            SourceId = sourceId;
        }
    }
    
    [SerializeField]
    private LineUI lineUIPrefab;
    
    public static LineManager Instance;
    private List<LineInfo> lines = new List<LineInfo>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            DestroyImmediate(this);
        }
    }

    private Predicate<LineInfo> GetPredicate(ITargetable beginning, ITargetable end)
    {
        if (beginning == null)
        {
            return line => line.End == end || line.Beginning == end;
        }

        if (end == null)
        {
            return line => line.End == beginning || line.Beginning == beginning;
        }
        
        return line =>
            line.Beginning == beginning && line.End == end ||
            line.Beginning == end && line.End == beginning;
    }

    private List<LineInfo> GetExistLines(ITargetable beginning, ITargetable end)
    {
        return lines.FindAll(GetPredicate(beginning, end));
    }
    
    public void ShowLines(ITargetable position)
    {
        var existLines = GetExistLines(position, null);
        foreach (var lineInfo in existLines.Where(lineInfo => lineInfo.LineUI))
        {
            lineInfo.IsVisible = true;
        }
    }

    public void CreateLine(ITargetable beginning, ITargetable end, string sourceId)
    {
        var lineUI = Instantiate(lineUIPrefab);
        lineUI.SetLinePositions(beginning.Position, end.Position);
        var newLine = new LineInfo(beginning, end, lineUI, sourceId);
        lines.Add(newLine);
    }

    public void RemoveLine(string sourceId)
    {
        var existLine = lines.Find(line => line.SourceId == sourceId);
        if (existLine?.LineUI == null) return;
        
        Destroy(existLine.LineUI.gameObject);
        lines.Remove(existLine);
    }
    
    public void ShowLine(string sourceId, ITargetable beginning, ITargetable end)
    {
        var existedLine = lines.Find(line => line.SourceId == sourceId);
        if (existedLine?.LineUI == null) return;
        
        existedLine.Beginning = beginning;
        existedLine.End = end;
        existedLine.IsVisible = true;
    }

    private void Update()
    {
        if (lines.Count <= 0) return;

        var visibleLines = new List<LineInfo>();
        foreach (var line in lines.Where(line => line.LineUI))
        {
            if(line.Beginning != null && line.End !=null)
            {
                line.LineUI.SetLinePositions(line.Beginning.Position, line.End.Position);
            }
            if (line.IsVisible)
            {
                if (!visibleLines.Any(visibleLine => visibleLine.Beginning == line.Beginning && visibleLine.End == line.End))
                {
                    line.LineUI.Show();
                    visibleLines.Add(line);    
                }
            }
            else
            {
                line.LineUI.Hide();
            }
        }
    }

    public void HideLines(ITargetable beginning, ITargetable end)
    {
        var existLines = GetExistLines(beginning, end);
        if (existLines.Count <= 0) return;
        
        foreach (var lineInfo in existLines.Where(lineInfo => lineInfo.LineUI))
        {
            lineInfo.IsVisible = false;
        }
    }

    public void SwitchLineDirection(string oldSourceId, string newSourceId, ITargetable beginning, ITargetable end)
    {
        RemoveLine(oldSourceId);
        CreateLine(beginning, end, newSourceId);
        ShowLine(newSourceId, beginning, end);
    }
}
