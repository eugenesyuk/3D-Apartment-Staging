
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GridRenderer : MonoBehaviour
{

    public GameObject gridLine;
    public GameObject appContainer, gridContainer, gridArea;

    Vector3 bottomLeft;
    Vector3 screenLeft, screenTop;
    Vector3 bottomRight;
    Vector3 topRight;

    readonly int minScale = 1;
    readonly int maxScale = 40;
    readonly float thickLineMultiplier = 1.5f;

    List<GameObject> gridLineList = new();
    readonly List<GameObject> gridLineListX = new();
    readonly List<GameObject> gridLineListY = new();

    // Use this for initialization
    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        DestroyGrid();

        RectTransform appContainerRt = appContainer.transform as RectTransform;
        RectTransform gridAreaRt = gridArea.transform as RectTransform;

        float xRatio = gridAreaRt.rect.width * 1.0f / appContainerRt.rect.width;
        float yRatio = gridAreaRt.rect.height * 1.0f / appContainerRt.rect.height;

        Vector3 screenDimensions = CalculateScreenSizeInWorldCoords(xRatio, yRatio);
        float pxThickness = 1f / (Screen.height / (Camera.main.orthographicSize * 2));
        RenderLines(screenDimensions, pxThickness);
        GetComponent<BoxCollider>().size = screenDimensions;
        GetComponent<BoxCollider>().center = new Vector2(Mathf.Abs(screenLeft.x - bottomLeft.x) / 2, -(screenTop.y - topRight.y) / 2);
    }

    void DestroyGrid()
    {
        foreach (GameObject g in gridLineList)
        {
            Destroy(g);
        }

        gridLineList.Clear();
        gridLineListX.Clear();
        gridLineListY.Clear();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetAxis("Mouse ScrollWheel") > 0) // back
        {
            Camera.main.orthographicSize = Mathf.Max(Camera.main.orthographicSize - 1, minScale);
            GenerateGrid();
        }
        if (Input.GetAxis("Mouse ScrollWheel") < 0) // forward
        {
            Camera.main.orthographicSize = Mathf.Min(Camera.main.orthographicSize + 1, maxScale);
            GenerateGrid();
        }
    }

    void RenderLines(Vector2 dimensions, float thickness)
    {
        Vector2 numberOfLines = new(Mathf.CeilToInt(dimensions.x), Mathf.CeilToInt(dimensions.y));

        float adjustY = Mathf.Abs((int)dimensions.y - dimensions.y) / 2;
        float adjustX = Mathf.Abs((int)dimensions.x - dimensions.x) / 2;

        RenderLinesForAxis(gridLineListY, numberOfLines.x, adjustX, thickness);
        RenderLinesForAxis(gridLineListX, numberOfLines.y, adjustY, thickness);

        gridLineList = gridLineListY.Concat(gridLineListX).ToList();
    }

    void RenderLinesForAxis(List<GameObject> list, float count, float adjust, float thickness)
    {
        // Generate line from left to right
        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(gridLine);
            go.transform.parent = gridContainer.transform; //Make this the parents
            go.layer = Globals.Layers.Grid;

            Vector3 pos0, pos1;

            if (list == gridLineListY)
            {
                pos0 = new Vector3(bottomLeft.x + (i) + adjust, bottomLeft.y, 1);
                pos1 = new Vector3(bottomLeft.x + (i) + adjust, topRight.y, 1);
            }
            else
            {
                pos0 = new Vector3(bottomLeft.x, bottomLeft.y + adjust + (i), 1);
                pos1 = new Vector3(bottomRight.x, bottomLeft.y + adjust + (i), 1);
            }

            LineRenderer lineRenderer = go.GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = lineRenderer.endWidth = thickness;
            lineRenderer.startColor = lineRenderer.endColor = Globals.GridLine.Color;
            lineRenderer.SetPosition(0, pos0);
            lineRenderer.SetPosition(1, pos1);

            if (i % 5 == 0)
            {
                lineRenderer.startColor = lineRenderer.endColor = Globals.GridLine.StrongColor;
                lineRenderer.startWidth = lineRenderer.endWidth = thickness * thickLineMultiplier;
            }

            list.Add(go);
        }
    }

    Vector2 CalculateScreenSizeInWorldCoords(float xRatio, float yRatio)
    {
        Camera cam = Camera.main;

        screenLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane)); //Bottom Left Point (0,0)
        screenTop = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane)); //Top Right Point (1,1)
        bottomLeft = cam.ViewportToWorldPoint(new Vector3(1 - xRatio, 0, cam.nearClipPlane));
        bottomRight = cam.ViewportToWorldPoint(new Vector3(1, 0, cam.nearClipPlane)); //Bottom Right Point (0,1)
        topRight = cam.ViewportToWorldPoint(new Vector3(1, yRatio, cam.nearClipPlane));
        float width = (bottomRight - bottomLeft).magnitude;
        float height = (topRight - bottomRight).magnitude;

        Vector2 dimensions = new(width, height);

        return dimensions;
    }

    GameObject GetClosestLine(List<GameObject> lineList, Vector3 position)
    {
        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (GameObject obj in lineList)
        {
            LineRenderer lineRenderer = obj.GetComponent<LineRenderer>();
            float dist = Vector3.Distance(position, lineRenderer.GetPosition(0));

            if (dist < minDist)
            {
                closest = obj;
                minDist = dist;
            }
        }

        return closest;
    }

    public GameObject GetClosestLineY(Vector3 position)
    {
        return GetClosestLine(gridLineListY, position);
    }

    public GameObject GetClosestLineX(Vector3 position)
    {
        return GetClosestLine(gridLineListX, position);
    }
}
