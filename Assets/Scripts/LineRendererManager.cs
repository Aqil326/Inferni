using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class LineRendererManager : MonoBehaviour
{
    public LineRenderer Line;
    public GameObject LineForInst;
    public int LineFrequency = 10;
    
    private int lineCount = 10;
    private List<LineRenderer> linesListLR = new List<LineRenderer>();
    private List<List<Vector3>> posLinesList = new List<List<Vector3>>(0);
    private List<Vector3> positions = new List<Vector3>(0);  
    private int index = 100;
    
    public float GetDist(Vector3 pos1, Vector3 pos2)
    {
        return Vector3.Distance(pos1, pos2);
    }

    public void Init(Vector3 startPosition, Vector3 endPosition, int intermediatePoints = 100)
    {
        
        List<Vector3> vector3List = new List<Vector3>();
        vector3List.Add(startPosition);
        for (int i = 1; i <= intermediatePoints; i++)
        {
            float t = (float)i / (float)(intermediatePoints + 1);
            Vector3 pointPosition = Vector3.Lerp(startPosition, endPosition, t);
            vector3List.Add(pointPosition);
        }

        vector3List.Add(endPosition);
        positions.AddRange(vector3List);
        float dist = GetDist(startPosition, endPosition);
        lineCount = (int)(dist * LineFrequency);
        InitList();
    }
 
    public void InitList()
    {
        posLinesList.Clear();

        int nLines = (lineCount * 2) - 1;
        int index = 0;
        for (int i = 0; i < nLines; i++)
        {
            List<Vector3> pos = new List<Vector3>();
            pos = positions.GetRange(index, (int)(positions.Count / nLines));
            index += (int)(positions.Count / nLines);
            posLinesList.Add(pos); 
        }
         
       
       for (int i = 0; i < lineCount; i++)
       {
           GameObject line = Instantiate(LineForInst,this.transform);
           linesListLR.Add(line.GetComponent<LineRenderer>());  
       }
  
       for (int i = 0, j = 0; i < linesListLR.Count; i++, j += 2)
       {
           List<Vector3> v3List = posLinesList[j];
           linesListLR[i].positionCount = v3List.Count;
           for(int i1 = 0; i1<v3List.Count; i1++) 
           {
               linesListLR[i].SetPosition(i1, v3List[i1]);
           } 
       } 

    } 
}
