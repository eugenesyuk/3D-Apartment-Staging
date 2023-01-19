using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FloorplanManager : MonoBehaviour
{

    public GameObject lineSprite;
    public GameObject nodeSprite;
    public Transform lineContainer, nodeContainer, houseObjectcontainer, windowContainer;
    public bool SnapToGid = true;

    public List<GameObject> nodeList = new();
    public List<GameObject> lineList = new();
    public List<GameObject> windowList = new();
    public List<GameObject> houseObjectList = new();

    public bool isDrawing = false;
    public bool didDraw = false; 

    GameObject newLine;
    GameObject initialNode, currentNode;

    private Vector3 _initialPos, _currentPos;

    readonly float _xRotation;
    readonly int layerFloorplan = Globals.Layers.Floorplan;

    LineRenderer _highlightedLineX, _highlightedLineY;
    Color _highlightedLineXColor, _highlightedLineYColor;

    void Start()
    {
        AddTapGesture();
    }

    // Update is called once per frame
    void Update()
    {
        AdjustLineOnDraw();
        DetectRightClick();
    }

    void AddTapGesture()
    {
        TKTapRecognizer tapRecognizer = new();

        tapRecognizer.gestureRecognizedEvent += (r) =>
        {
            //Debug.Log ("tap recognizer fired: " + r);
            if (gameObject.activeInHierarchy)
            {
                if (gameObject.GetComponent<BoxCollider>().bounds.Contains(transform.TransformPoint(Utils.GetCurrentMousePosition(r.startTouchLocation()).GetValueOrDefault())))
                {
                    if (!isDrawing)
                    {
                        didDraw = false;
                        isDrawing = true;
                        _initialPos = Utils.GetCurrentMousePosition(r.startTouchLocation()).GetValueOrDefault();
                        InstantiateNode(_initialPos);
                    }
                    else
                    {
                        didDraw = true;
                        float newXpos = lineList.Last().transform.position.x + lineList.Last().transform.localScale.x * Mathf.Cos(_xRotation * Mathf.PI / 180f);
                        float newYpos = lineList.Last().transform.position.y + lineList.Last().transform.localScale.x * Mathf.Sin(_xRotation * Mathf.PI / 180f);

                        newXpos = Mathf.Round(newXpos * 100) / 100f;
                        newYpos = Mathf.Round(newYpos * 100) / 100f;

                        _initialPos = _currentPos;

                        SetPreviousLineEndNode();
                        HandleOverlap(lineList.Last());
                    }

                    InstantiateLine(_initialPos);
                }
                else
                {
                    RemoveDrawingLine();
                }
            }
        };


        TouchKit.addGestureRecognizer(tapRecognizer);
    }

    void AdjustLineOnDraw()
    {
        if (newLine != null && isDrawing)
        {
            _currentPos = SnapToGrid(Utils.GetCurrentMousePosition(Input.mousePosition).GetValueOrDefault());

            if (gameObject.GetComponent<BoxCollider>().bounds.Contains(transform.TransformPoint(_currentPos)))
            {
                Vector3 direction = _currentPos - _initialPos;

                // Need to give new value of rotation for the line script
                float angle = ((Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg) - 90) * -1;
                Quaternion newRotation = Quaternion.AngleAxis(angle, Vector3.forward);
                Vector3 newScale = new(direction.magnitude, newLine.transform.localScale.y, newLine.transform.localScale.z);

                newLine.transform.rotation = newRotation;
                newLine.transform.localScale = newScale;
                newLine.GetComponent<Line>().RenderLineSizeLabel(_initialPos, _currentPos, lineContainer);
            }
        }
    }
    private Vector3 SnapToGrid(Vector3 mousePosition)
    {
        Vector3 resultPosition = mousePosition;

        if(SnapToGid)
        {
            var gridRenderer = gameObject.GetComponent<GridRenderer>();

            GameObject closestGridLineY = gridRenderer.GetClosestLineX(mousePosition);
            GameObject closestGridLineX = gridRenderer.GetClosestLineY(mousePosition);

            LineRenderer lineYRenderer = closestGridLineY.GetComponent<LineRenderer>();
            LineRenderer lineXRenderer = closestGridLineX.GetComponent<LineRenderer>();

            Vector p1 = new(lineYRenderer.GetPosition(0).x, lineYRenderer.GetPosition(0).y);
            Vector p2 = new(lineYRenderer.GetPosition(1).x, lineYRenderer.GetPosition(1).y);

            Vector q1 = new(lineXRenderer.GetPosition(0).x, lineXRenderer.GetPosition(0).y);
            Vector q2 = new(lineXRenderer.GetPosition(1).x, lineXRenderer.GetPosition(1).y);

            if (Utils.LineSegementsIntersect(p1, p2, q1, q2, out Vector intersectionPoint, true))
            {
                ResetLineHighlight();

                _highlightedLineX = lineXRenderer;
                _highlightedLineY = lineYRenderer;

                _highlightedLineXColor = lineXRenderer.colorGradient.Evaluate(0);
                _highlightedLineYColor = lineYRenderer.colorGradient.Evaluate(0);

                lineYRenderer.startColor = lineYRenderer.endColor = lineXRenderer.startColor = lineXRenderer.endColor = Globals.Line.HighlightColor;

                resultPosition = new Vector3((float)intersectionPoint.X, (float)intersectionPoint.Y, 0);
            }

        }

        return resultPosition;
    }

    void ResetLineHighlight()
    {
        if (_highlightedLineX != null && _highlightedLineY != null)
        {
            _highlightedLineX.startColor = _highlightedLineX.endColor = _highlightedLineXColor;
            _highlightedLineY.startColor = _highlightedLineY.endColor = _highlightedLineYColor;
        }
    }

    void HandleOverlap(GameObject line)
    {
        int count = lineList.Count - 1;

        Dictionary<GameObject, Vector> linesToSplit = new();

        for (int i = 0; i < count; i++)
        {
            if (line.GetComponent<Line>().startNode != lineList[i].GetComponent<Line>().endNode)
            {
               
                Vector p1 = new(line.GetComponent<Line>().startNode.transform.position.x, line.GetComponent<Line>().startNode.transform.position.y);
                Vector p2 = new(line.GetComponent<Line>().endNode.transform.position.x, line.GetComponent<Line>().endNode.transform.position.y);

                Vector q1 = new(lineList[i].GetComponent<Line>().startNode.transform.position.x, lineList[i].GetComponent<Line>().startNode.transform.position.y);
                Vector q2 = new(lineList[i].GetComponent<Line>().endNode.transform.position.x, lineList[i].GetComponent<Line>().endNode.transform.position.y);

                if (Utils.LineSegementsIntersect(p1, p2, q1, q2, out Vector intersectionPoint, true))
                {
                    print("Line " + line.name + " Overlaps with " + lineList[i] + " at point " + intersectionPoint.X + " " + intersectionPoint.Y);
                    if (!double.IsNaN(intersectionPoint.X) || !double.IsNaN(intersectionPoint.Y))
                    {
                        linesToSplit.Add(lineList[i], intersectionPoint);
                    }
                }
            }
        }

        for (int index = 0; index < linesToSplit.Count; index++)
        {
            var item = linesToSplit.ElementAt(index);
            print("splitting line " + item.Key + " At position " + (float)item.Value.X + " " + (float)item.Value.Y);
            SplitLine(item.Key, new Vector3((float)item.Value.X, (float)item.Value.Y, 0));
        }
    }

    void SplitLine(GameObject line, Vector3 position)
    {
        GameObject newNode = InstantiateIntersectionNode(position);
        GameObject startNode = line.GetComponent<Line>().startNode;
        GameObject endNode = line.GetComponent<Line>().endNode;

        /*startNode.GetComponent<Node>().adjacentNodes.Remove(endNode);
        startNode.GetComponent<Node>().adjacentNodes.Add(newNode);
        newNode.GetComponent<Node>().adjacentNodes.Add(endNode);*/

        Vector3 scale = line.transform.localScale;
        int multiplier = 1;
        if (scale.x < 0)
        {
            multiplier = -1;
        }

        InstantiateLine(newNode, endNode, line.transform.rotation, multiplier);

        line.GetComponent<Line>().endNode = newNode;

        line.transform.localScale = new Vector3(multiplier * Vector3.Distance(startNode.transform.position, line.GetComponent<Line>().endNode.transform.position), scale.y, scale.z);

    }

    void InstantiateLine(Vector3 position)
    {
        newLine = GameObject.Instantiate(lineSprite);
        newLine.name = "Line" + lineList.Count();
        newLine.transform.parent = lineContainer;
        newLine.transform.position = position;
        newLine.layer = layerFloorplan;

        Line line = newLine.GetComponent<Line>();
        line.name = newLine.name;

        if (currentNode == null)
        {
            line.startNode = initialNode;
        }
        else
        {
            line.startNode = currentNode;
        }

        lineList.Add(newLine);
    }

    void InstantiateLine(GameObject startNode, GameObject endNode, Quaternion rotation, int multiplier)
    {
        newLine = GameObject.Instantiate(lineSprite);
        newLine.name = "Line" + lineList.Count();
        newLine.transform.parent = lineContainer;
        newLine.transform.position = startNode.transform.position;

        newLine.transform.localScale = new Vector3(multiplier * Vector3.Distance(startNode.transform.position, endNode.transform.position), 0.2f, 1);
        newLine.transform.rotation = rotation;
        newLine.layer = layerFloorplan;
        Line w = newLine.GetComponent<Line>();
        w.startNode = startNode;
        w.endNode = endNode;
        lineList.Add(newLine);
    }

    GameObject InstantiateNode(Vector3 position)
    {
        GameObject newNode = NormalizeNodeAtPoint(position);
        if (newNode == null)
        {
            newNode = GameObject.Instantiate(nodeSprite);
            newNode.transform.position = position;
            newNode.transform.parent = nodeContainer;
            newNode.name = "Node " + nodeList.Count();
            newNode.layer = layerFloorplan;
            nodeList.Add(newNode);
        }
        if (!didDraw)
        {
            initialNode = newNode;
        }
        if (currentNode != null)
        {
            if (newNode != currentNode)
            {
                currentNode.GetComponent<Node>().adjacentNodes.Add(newNode);
            }
        }
        currentNode = newNode;
        return newNode;
    }

    GameObject InstantiateIntersectionNode(Vector3 position)
    {
        GameObject newNode = NormalizeNodeAtPoint(position);
        if (newNode == null)
        {
            newNode = GameObject.Instantiate(nodeSprite);
            newNode.transform.position = position;
            newNode.transform.parent = nodeContainer;
            newNode.name = "Node " + nodeList.Count();
            newNode.layer = layerFloorplan;

            nodeList.Add(newNode);
        }
        print("Instantiating intersection node with name " + newNode.name);

        return newNode;
    }

    GameObject NormalizeNodeAtPoint(Vector3 position)
    {
        print("Normalizing");
        for (int i = 0; i < nodeList.Count; i++)
        {
            if (nodeList[i].GetComponent<Renderer>().bounds.Contains(position))
            {
                print("Overlap with node " + nodeList[i]);
                return nodeList[i];
            }
        }
        return null;
    }


    void SetPreviousLineEndNode()
    {
        lineList.Last().GetComponent<Line>().endNode = InstantiateNode(_initialPos);
        //lineList.Last().GetComponent<BoxCollider>().enabled = true;
    }
    private void DetectRightClick()
    {
        if (Input.GetMouseButtonDown(1))
        {
            RemoveDrawingLine();
            ResetLineHighlight();
        }
    }

    private void RemoveDrawingLine()
    {
        if (currentNode != null)
        {
            lineList.Remove(newLine);
            GameObject.DestroyImmediate(newLine);

            if (currentNode.GetComponent<Node>().adjacentNodes.Count == 0 && !didDraw)
            {
                nodeList.Remove(currentNode);
                GameObject.Destroy(currentNode);
            }
            isDrawing = false;
            currentNode = null;
        }
    }
    public List<GameObject> ExportNodes()
    {
        return nodeList;
    }

    public List<GameObject> ExportObjects()
    {
        return houseObjectList;
    }

    public List<GameObject> ExportWindows()
    {
        for (int i = 0; i < windowList.Count; i++)
        {
            RaycastHit[] hitList = Physics.RaycastAll(transform.TransformPoint(windowList[i].transform.position), Vector3.forward);

            int correctLineIndex = -1;

            for (int j = 0; j < hitList.Length; j++)
            {
                print("The ray hit" + hitList[j].transform.name);
                if (hitList[j].transform.name.Contains("Line"))
                {
                    if (Mathf.Approximately(hitList[j].transform.rotation.eulerAngles.z, windowList[i].transform.rotation.eulerAngles.z))
                    {
                        correctLineIndex = j;
                    }
                    break;
                }
            }

            LineAttachableObject window = windowList[i].GetComponent<LineAttachableObject>();
            if (correctLineIndex < hitList.Length && correctLineIndex != -1)
            {
                window.startNode = hitList[correctLineIndex].transform.GetComponent<Line>().startNode;
                window.endNode = hitList[correctLineIndex].transform.GetComponent<Line>().endNode;
            }
        }
        return windowList;
    }

    public void Refresh()
    {
        nodeList.Clear();
        DestroyChildren(nodeContainer);

        lineList.Clear();
        DestroyChildren(lineContainer);

        windowList.Clear();
        DestroyChildren(windowContainer); 

        houseObjectList.Clear();
        DestroyChildren(houseObjectcontainer);

        isDrawing = false;
        didDraw = false;
    }

    void DestroyChildren(Transform t)
    {
        bool isPlaying = Application.isPlaying;

        while (t.childCount != 0)
        {
            Transform child = t.GetChild(0);

            if (isPlaying)
            {
                child.parent = null;
                UnityEngine.Object.Destroy(child.gameObject);
            }
            else UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }
}
