using UnityEngine;
using System.Collections.Generic;

public class Node : MonoBehaviour
{

    public List<GameObject> AdjacentNodes = new();

    FloorplanManager Floorplan;
    Renderer Renderer;

    public bool isMouseOver = false;
    public bool isSelected = false;

    void Start () {
        Renderer = gameObject.GetComponent<Renderer>();
        Floorplan = GameObject.Find("Floorplan Container").GetComponent<FloorplanManager>();
    }
 
    void OnMouseDown()
    {
        Floorplan.StartDrag(gameObject);
    }

    private void OnMouseUp()
    {
        Select();
        Floorplan.ResetDrag();
    }

    private void OnMouseEnter()
    {
        if (!Floorplan.CanSelect()) return;
        isMouseOver = true;
        Highlight();
    }

    private void OnMouseExit()
    {
        isMouseOver = false;
        if (isSelected) return;
        ResetHightlight();
    }

    void Select()
    {
        if (!Floorplan.CanSelect()) return;
        Floorplan.SelectNode(gameObject);
        Highlight();
        isSelected = true;
    }

    public void Deselect()
    {
        isSelected = false;
        ResetHightlight();
    }

    void Highlight()
    {
        Renderer.material.color = Globals.Node.HighlightColor;
    }

    void ResetHightlight()
    {
        Renderer.material.color = Globals.Node.Color;
    }
}
