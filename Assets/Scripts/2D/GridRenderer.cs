
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class GridRenderer : MonoBehaviour
{

    public GameObject gridLine;
    public GameObject appContainer, gridContainer;

    Vector3 bottomLeft;
    Vector3 screenLeft, screenTop;
    Vector3 bottomRight;
    Vector3 topRight;

    int minScale = 1;
    int maxScale = 40;

    float thickLineMultiplier = 1.5f;

    List<GameObject> gridLineList = new List<GameObject>();

    // Use this for initialization
    void Start()
    {
        generateGrid();
    }

    void generateGrid()
    {
        destroyGrid();

        RectTransform appContainerRt = appContainer.transform as RectTransform;
        RectTransform gridContainerRt = gridContainer.transform as RectTransform;

        float xRatio = gridContainerRt.rect.width * 1.0f / appContainerRt.rect.width;
        float yRatio = gridContainerRt.rect.height * 1.0f / appContainerRt.rect.height;

        Vector3 screenDimensions = CalculateScreenSizeInWorldCoords(xRatio, yRatio);
        float pxThickness = 1f / (Screen.height / (Camera.main.orthographicSize * 2));
        RenderLines(screenDimensions, pxThickness);
        GetComponent<BoxCollider>().size = screenDimensions;
        GetComponent<BoxCollider>().center = new Vector2(Mathf.Abs(screenLeft.x - bottomLeft.x) / 2, -(screenTop.y - topRight.y) / 2);
    }

    void destroyGrid()
    {
        foreach (GameObject g in gridLineList)
        {
            Destroy(g);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetAxis("Mouse ScrollWheel") > 0) // back
        {
            Camera.main.orthographicSize = Mathf.Max(Camera.main.orthographicSize - 1, minScale);
            generateGrid();
        }
        if (Input.GetAxis("Mouse ScrollWheel") < 0) // forward
        {
            Camera.main.orthographicSize = Mathf.Min(Camera.main.orthographicSize + 1, maxScale);
            generateGrid();
        }
    }

    void RenderLines(Vector2 dimensions, float thickness)
    {
        Vector2 numberOfLines = new Vector2(Mathf.CeilToInt(dimensions.x), Mathf.CeilToInt(dimensions.y));
        float adjustY = Mathf.Abs((int)dimensions.y - dimensions.y) / 2 + 0.5f;
        float adjustX = Mathf.Abs((int)dimensions.x - dimensions.x) / 2;

        // Generate lines vertically
        for (int i = 0; i < numberOfLines.x; i++)
        {
            GameObject go = GameObject.Instantiate(gridLine);
            go.transform.parent = transform; //Make this the parent
            LineRenderer lineRenderer = go.GetComponent<LineRenderer>();
            lineRenderer.SetVertexCount(2);
            lineRenderer.SetWidth(thickness, thickness);
            lineRenderer.SetPosition(0, new Vector3(bottomLeft.x + (i) + adjustX, bottomLeft.y, 1));
            lineRenderer.SetPosition(1, new Vector3(bottomLeft.x + (i) + adjustX, topRight.y, 1));

            if (i % 5 == 0)
            {
                lineRenderer.SetColors(new Color(0.75f, 0.75f, 0.75f, 1f), new Color(0.75f, 0.75f, 0.75f, 1f));
                lineRenderer.SetWidth(thickness * thickLineMultiplier, thickness * thickLineMultiplier);
            }
            gridLineList.Add(go);
        }

        // Generate line from left to right
        for (int j = 0; j < numberOfLines.y; j++)
        {
            GameObject go = GameObject.Instantiate(gridLine);
            go.transform.parent = transform; //Make this the parents
            LineRenderer lineRenderer = go.GetComponent<LineRenderer>();
            lineRenderer.SetVertexCount(2);
            lineRenderer.SetWidth(thickness, thickness);
            lineRenderer.SetPosition(0, new Vector3(bottomLeft.x, bottomLeft.y + adjustY + (j), 1));
            lineRenderer.SetPosition(1, new Vector3(bottomRight.x, bottomLeft.y + adjustY + (j), 1));

            if (j % 5 == 0)
            {
                lineRenderer.SetColors(new Color(0.75f, 0.75f, 0.75f, 1f), new Color(0.75f, 0.75f, 0.75f, 1f));
                lineRenderer.SetWidth(thickness * thickLineMultiplier, thickness * thickLineMultiplier);
            }
            gridLineList.Add(go);
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

        Vector2 dimensions = new Vector2(width, height);

        return dimensions;
    }
}