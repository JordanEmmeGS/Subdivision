using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;

public struct SubdivisionJob : IJob
{
    public Vector3 v1, v2, v3;
    public int numberOfSubdiv;
    public NativeList<Vector3> result;

    public void Execute()
    {
        NativeList<Vector3> tempList = new NativeList<Vector3>(Allocator.Temp) {v1, v2, v3};
        
        for (int i = 0; i < numberOfSubdiv; i++)
        {
            for (int listIndex = 0; listIndex < tempList.Length - 1; listIndex++)
            {
                result.Add(Vector3.Lerp(tempList[listIndex], tempList[listIndex+1], 0.25f));
                result.Add(Vector3.Lerp(tempList[listIndex], tempList[listIndex+1], 0.75f));
            }
            tempList = result;
            result = new NativeList<Vector3>();
        }

        result = tempList;
    }
}

public class Logic : MonoBehaviour
{
    [SerializeField] private GameObject userPoint;
    private List<Vector3> userPointsCoords = new List<Vector3>();
    private List<Vector3> divisionPointsCoords = new List<Vector3>();

    [SerializeField] private GameObject linePrefab;
    private GameObject currentLine;
    private LineRenderer lineRenderer;

    private int numberOfSubdiv = 8;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        SpawnPoint();
        Subdivision();
        Destroy(currentLine);
        RenderLine();
    }

    private void SpawnPoint()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition) + 10*Vector3.forward;
        Instantiate(userPoint, mousePos, Quaternion.identity);
        userPointsCoords.Add(mousePos);
    }


    private void Subdivision()
    {
        if (userPointsCoords.Count < 3)
        {
            return;
        }
        divisionPointsCoords = userPointsCoords;
        
        NativeList<JobHandle> handles = new NativeList<JobHandle>(Allocator.Temp);

        List<NativeList<Vector3>> results = new List<NativeList<Vector3>>();

        for (int index = 1; index < userPointsCoords.Count - 1; index++)
        {
            NativeList<Vector3> result = new NativeList<Vector3>(Allocator.TempJob);
            
            SubdivisionJob job = new SubdivisionJob();
            job.v1 = userPointsCoords[index-1];
            job.v2 = userPointsCoords[index];
            job.v3 = userPointsCoords[index+1];
            job.numberOfSubdiv = numberOfSubdiv;
            
            job.result = result;
            
            JobHandle handle = job.Schedule();
            handles.Add(handle);
            results.Add(result);
        }
        JobHandle.CompleteAll(handles);
        handles.Dispose();
        
        for (int index = 0; index < userPointsCoords.Count - 2; index++)
        {
            for (int i = 0; i < results[index].Length; i++)
            {
                Vector3 coord = results[index][i];
                divisionPointsCoords.Add(coord);
            }

            results[index].Dispose();
        }
    }
    
    /*private void Subdivision()
    {
        divisionPointsCoords = userPointsCoords;
        
        

        for (int i = 0; i < numberOfSubdiv; i++)
        {
            SubdivisionStep();
        }
    }*/
    
    private void RenderLine()
    {
        currentLine = Instantiate(linePrefab, Vector3.zero, quaternion.identity);
        
        lineRenderer = currentLine.GetComponent<LineRenderer>();
        lineRenderer.positionCount = divisionPointsCoords.Count + 1;

        for (int i = 1; i < divisionPointsCoords.Count; i++)
        {
            lineRenderer.SetPosition(i, divisionPointsCoords[i]);
        }
        
        lineRenderer.SetPosition(0, userPointsCoords[0]);
        lineRenderer.SetPosition(divisionPointsCoords.Count, userPointsCoords[userPointsCoords.Count-1]);
    }
    
    /*private void SubdivisionStep()
    {
        int vertexNumber = divisionPointsCoords.Count;
        List<Vector3> tempList = new List<Vector3>();
        
        for (int i = 0; i < vertexNumber - 1; i++)
        {
            (Vector3 newVertex1, Vector3 newVertex2) = NewVertices(divisionPointsCoords[i], divisionPointsCoords[i + 1]);
            {
                tempList.Add(newVertex1);
                tempList.Add(newVertex2);
            }
        }

        divisionPointsCoords = tempList;
    }

    private (Vector3 newVertex1, Vector3 newVertex2) NewVertices(Vector3 v1, Vector3 v2)
    {
        Vector3 newVertex1 = Vector3.Lerp(v1, v2, 0.25f);
        Vector3 newVertex2 = Vector3.Lerp(v1, v2, 0.75f);
        
        return (newVertex1, newVertex2);
    }*/
    
}
