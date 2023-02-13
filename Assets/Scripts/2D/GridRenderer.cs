
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

    readonly float thickLineMultiplier = 1.5f;

    List<GameObject> gridLineList = new();
    readonly List<GameObject> gridLineListX = new();
    readonly List<GameObject> gridLineListY = new();

    [SerializeField]
    bool SnapToGrid = true;

    LineRenderer _highlightedLineX, _highlightedLineY;
    Color _highlightedLineXColor, _highlightedLineYColor;

    // Use this for initialization
    void Start()
    {
        Camera.main.orthographicSize = Globals.Camera.MaxScale;
        GenerateGrid();
        Camera.main.orthographicSize = Globals.Camera.StartSize;
    }

    void GenerateGrid()
    {
        DestroyGrid();

        RectTransform appContainerRt = appContainer.transform as RectTransform;
        RectTransform gridAreaRt = gridArea.transform as RectTransform;

        float xRatio = gridAreaRt.rect.width * 1.0f / appContainerRt.rect.width;
        float yRatio = gridAreaRt.rect.height * 1.0f / appContainerRt.rect.height;

        Vector3 screenDimensions = CalculateScreenSizeInWorldCoords(xRatio, yRatio);
        float pxThickness = LineThickness();
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
        if (Input.GetAxis("Mouse ScrollWheel") > 0) 
        {
            ZoomIn(1);
        }

        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            ZoomOut(1);
        }

        UpdateLinesThickness();
    }
    
    public void ZoomOut(int step)
    {
        Camera.main.orthographicSize = Mathf.Min(Camera.main.orthographicSize + step, Globals.Camera.MaxScale);
    }

    public void ZoomIn(int step)
    {
        Camera.main.orthographicSize = Mathf.Max(Camera.main.orthographicSize - step, Globals.Camera.MinScale);
    }

    float LineThickness()
    {
        return 1f / (Screen.height / (Camera.main.orthographicSize * 2));
    }

    void UpdateLinesThickness()
    {
        foreach (GameObject lineObject in gridLineList)
        {
            var lineRenderer = lineObject.GetComponent<LineRenderer>();
            lineRenderer.startWidth = lineRenderer.endWidth = LineThickness();
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

    public Vector2 SnapToGridLines(Vector3 mousePosition)
    {
        if (!SnapToGrid) return mousePosition;

        Vector2 resultPosition = mousePosition;

        GameObject closestGridLineY = GetClosestLineY(mousePosition);
        GameObject closestGridLineX = GetClosestLineX(mousePosition);

        LineRenderer lineYRenderer = closestGridLineY.GetComponent<LineRenderer>();
        LineRenderer lineXRenderer = closestGridLineX.GetComponent<LineRenderer>();

        Vector2 crossPointY = new(lineYRenderer.GetPosition(0).x, mousePosition.y);
        Vector2 crossPointX = new(mousePosition.x, lineXRenderer.GetPosition(0).y);

        float snapProximityFactor = Globals.SnapProxmityFactor;
        float distanceToLineY = Vector2.Distance(mousePosition, crossPointY);
        float distanceToLineX = Vector2.Distance(mousePosition, crossPointX);

        ResetLineHighlight();

        if (distanceToLineY < snapProximityFactor && distanceToLineX >= snapProximityFactor)
        {
            HighlightGridLine(ref _highlightedLineY, ref _highlightedLineYColor, lineYRenderer);
            return crossPointY;
        }
        else if (distanceToLineX < snapProximityFactor && distanceToLineY >= snapProximityFactor)
        {
            HighlightGridLine(ref _highlightedLineX, ref _highlightedLineXColor, lineXRenderer);
            return crossPointX;
        }
        else if (distanceToLineY < snapProximityFactor && distanceToLineX < snapProximityFactor)
        {
            Vector p1 = new(lineYRenderer.GetPosition(0).x, lineYRenderer.GetPosition(0).y);
            Vector p2 = new(lineYRenderer.GetPosition(1).x, lineYRenderer.GetPosition(1).y);

            Vector q1 = new(lineXRenderer.GetPosition(0).x, lineXRenderer.GetPosition(0).y);
            Vector q2 = new(lineXRenderer.GetPosition(1).x, lineXRenderer.GetPosition(1).y);

            if (Utils.LineSegementsIntersect(p1, p2, q1, q2, out Vector intersectionPoint, true))
            {
                HighlightGridLine(ref _highlightedLineY, ref _highlightedLineYColor, lineYRenderer);
                HighlightGridLine(ref _highlightedLineX, ref _highlightedLineXColor, lineXRenderer);

                return new Vector2((float)intersectionPoint.X, (float)intersectionPoint.Y);
            }
        }

        return resultPosition;
    }

    void HighlightGridLine
        (ref LineRenderer highlightedLineRef, ref Color highlightedLineColorRef, LineRenderer line)
    {
        highlightedLineRef = line;
        highlightedLineColorRef = line.colorGradient.Evaluate(.5f);
        line.startColor = line.endColor = Globals.GridLine.HighlightColor;
    }

    void ResetLineHighlight()
    {
        if (_highlightedLineX != null)
        {
            _highlightedLineX.startColor = _highlightedLineX.endColor = _highlightedLineXColor;
        }

        if (_highlightedLineY != null)
        {
            _highlightedLineY.startColor = _highlightedLineY.endColor = _highlightedLineYColor;
        }
    }

}
