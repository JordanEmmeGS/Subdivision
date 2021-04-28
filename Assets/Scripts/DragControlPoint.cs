using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragControlPoint : MonoBehaviour
{
    private void OnMouseDrag()
    {
        Vector3 mousePosDrag = Camera.main.ScreenToWorldPoint(Input.mousePosition) + 10*Vector3.forward;
        int index = transform.GetSiblingIndex();
        (int index, Vector3 mousePosDrag) message = (index, mousePosDrag);

        gameObject.transform.position = mousePosDrag;
        
        GameObject.Find("Logic").SendMessage("UpdateDraggedControlPoint", message);
    }
}
