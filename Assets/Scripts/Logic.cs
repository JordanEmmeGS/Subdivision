using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;

public class Logic : MonoBehaviour
{
    [SerializeField] private GameObject userPoint;
    [SerializeField] private GameObject linePrefab;
    
    private GameObject currentLine;
    private LineRenderer lineRenderer;

    private Subdivision.Calculator subdivisionCalculator = new Subdivision.Calculator();

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        Vector3 mousePos = ReturnMousePos();
        SpawnPointAt(mousePos);
        subdivisionCalculator.UpdateControlPointList(mousePos);
        subdivisionCalculator.UpdateSubdividedVertexList();
        Destroy(currentLine);
        RenderLine(subdivisionCalculator.data.subdividedVertexList);
    }

    private void SpawnPointAt(Vector3 mousePos)
    {
        Instantiate(userPoint, mousePos, Quaternion.identity);
    }

    private Vector3 ReturnMousePos()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition) + 10 * Vector3.forward;
        return mousePos;
    }
    

    private void RenderLine(List<Vector3> subdividedVertexList)
    {
        currentLine = Instantiate(linePrefab, Vector3.zero, quaternion.identity);

        lineRenderer = currentLine.GetComponent<LineRenderer>();
        lineRenderer.positionCount = subdividedVertexList.Count;

        for (int i = 0; i < subdividedVertexList.Count; i++)
        {
            lineRenderer.SetPosition(i, subdividedVertexList[i]);
        }
    }
}

namespace Subdivision
{
    public class Data
    {
        public List<Vector3> controlPointList = new List<Vector3>();
        public List<Vector3> subdividedVertexList = new List<Vector3>();
        
        public int numberOfSubdiv = 10;
    }
    
    public class Calculator
    {
        public Data data = new Data();

        public void UpdateControlPointList(Vector3 newVertex)
        {
            data.controlPointList.Add(newVertex);
        }
    
        public void UpdateSubdividedVertexList()
        {
            if (data.controlPointList.Count < 3)
            {
                return;
            }
            
            data.subdividedVertexList = new List<Vector3>();
            
            (NativeList<JobHandle> handleList, List<NativeList<Vector3>> tripletDivisionList) =
                ScheduleAllSubdivisionJobs();

            JobHandle.CompleteAll(handleList);
            handleList.Dispose();
            MergeAndDisposeTripletDivisionList(tripletDivisionList);
            AddFirstAndLastControlPoints();
        }

        private (NativeList<JobHandle>, List<NativeList<Vector3>>) ScheduleAllSubdivisionJobs()
        {
            NativeList<JobHandle> handleList = new NativeList<JobHandle>(Allocator.Temp);

            List<NativeList<Vector3>> tripletDivisionList = new List<NativeList<Vector3>>();

            for (int vertexIndex = 1; vertexIndex < data.controlPointList.Count - 1; vertexIndex++)
            {
                (JobHandle handle, NativeList<Vector3> tripletDivision) = ScheduleSingleSubdivisionJob(vertexIndex);
                handleList.Add(handle);
                tripletDivisionList.Add(tripletDivision);
            }

            return (handleList, tripletDivisionList);
        }

        private void MergeAndDisposeTripletDivisionList(List<NativeList<Vector3>> tripletDivisionList)
        {
            for (int index = 0; index < tripletDivisionList.Count; index++)
            {
                for (int i = 0; i < tripletDivisionList[index].Length; i++)
                {
                    Vector3 coord = tripletDivisionList[index][i];
                    data.subdividedVertexList.Add(coord);
                }

                tripletDivisionList[index].Dispose();

            }
        }

        private void AddFirstAndLastControlPoints()
        {
            data.subdividedVertexList.Insert(0,data.controlPointList[0]);
            data.subdividedVertexList.Add(data.controlPointList[data.controlPointList.Count - 1]);
        }

        private (JobHandle, NativeList<Vector3>) ScheduleSingleSubdivisionJob(int vertexIndex)
        {
            NativeList<Vector3> tripletDivision = new NativeList<Vector3>(Allocator.TempJob);

            Job job = new Job();
            
            job.v1 = data.controlPointList[vertexIndex - 1];
            job.v2 = data.controlPointList[vertexIndex];
            job.v3 = data.controlPointList[vertexIndex + 1];
            job.numberOfSubdiv = data.numberOfSubdiv;
            job.tripletDivision = tripletDivision;

            JobHandle handle = job.Schedule();

            return (handle, tripletDivision);
        }
    
    }

    public struct Job : IJob
    {
        public Vector3 v1, v2, v3;
        public int numberOfSubdiv;
        public NativeList<Vector3> tripletDivision;

        public void Execute()
        {
            NativeList<Vector3> tempToDivide = new NativeList<Vector3>(Allocator.Temp) {v1, v2, v3};
    
            for (int i = 0; i < numberOfSubdiv; i++)
            {           
                NativeList<Vector3> tempDivided = new NativeList<Vector3>(Allocator.Temp);

                for (int listIndex = 0; listIndex < tempToDivide.Length - 1; listIndex++)
                {
                    tempDivided.Add(Vector3.Lerp(tempToDivide[listIndex], tempToDivide[listIndex+1], 0.25f));
                    tempDivided.Add(Vector3.Lerp(tempToDivide[listIndex], tempToDivide[listIndex+1], 0.75f));
                }
                tempToDivide = tempDivided;
            }

            for (int i = 0; i < tempToDivide.Length; i++)
            {
                Vector3 point = tempToDivide[i];
                tripletDivision.Add(point);
            }
        }
    }
}
