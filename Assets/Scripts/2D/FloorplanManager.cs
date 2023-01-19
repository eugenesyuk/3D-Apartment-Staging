using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class FloorplanManager : MonoBehaviour
{

    public GameObject LineSprite;
    public GameObject NodeSprite;
    public Transform LineContainer, NodeContainer, HouseObjectContainer, AttachableObjectContainer;
    public bool SnapToGrid = true;

    public List<GameObject> NodeList = new();
    public List<GameObject> LineList = new();
    public List<GameObject> WindowList = new();
    public List<GameObject> HouseObjectList = new();

    public bool IsDrawing = false;
    public bool DidDraw = false; 

    GameObject _newLine;
    GameObject _initialNode, _currentNode;

    private Vector3 _initialPos, _currentPos;
    readonly int _layerFloorplan = Globals.Layers.Floorplan;

    LineRenderer _highlightedLineX, _highlightedLineY;
    Color _highlightedLineXColor, _highlightedLineYColor;

    void Start()
    {
        OnTapGesture();
    }

    // Update is called once per frame
    void Update()
    {
        RenderDrawingLine();
        OnClickMouseRight();
    }

    void OnTapGesture()
    {
        TKTapRecognizer tapRecognizer = new();

        tapRecognizer.gestureRecognizedEvent += (r) =>
        {
            if (gameObject.activeInHierarchy)
            {
                if (gameObject.GetComponent<BoxCollider>().bounds.Contains(transform.TransformPoint(Utils.GetCurrentMousePosition(r.startTouchLocation()).GetValueOrDefault())))
                {
                    if (!IsDrawing)
                    {
                        DidDraw = false;
                        IsDrawing = true;
                        _initialPos = Utils.GetCurrentMousePosition(r.startTouchLocation()).GetValueOrDefault();
                        InstantiateNode(_initialPos);
                    }
                    else
                    {
                        DidDraw = true;
                        float newXpos = LineList.Last().transform.position.x + LineList.Last().transform.localScale.x * Mathf.Cos(Mathf.PI / 180f);
                        float newYpos = LineList.Last().transform.position.y + LineList.Last().transform.localScale.x * Mathf.Sin(Mathf.PI / 180f);

                        newXpos = Mathf.Round(newXpos * 100) / 100f;
                        newYpos = Mathf.Round(newYpos * 100) / 100f;

                        _initialPos = _currentPos;

                        SetPreviousLineEndNode();
                        HandleOverlap(LineList.Last());
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

    void RenderDrawingLine()
    {
        if (_newLine != null && IsDrawing)
        {
            _currentPos = OnSnapToGrid(Utils.GetCurrentMousePosition(Input.mousePosition).GetValueOrDefault());

            if (gameObject.GetComponent<BoxCollider>().bounds.Contains(transform.TransformPoint(_currentPos)))
            {
                Vector3 direction = _currentPos - _initialPos;

                // Need to give new value of rotation for the line script
                float angle = ((Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg) - 90) * -1;
                Quaternion newRotation = Quaternion.AngleAxis(angle, Vector3.forward);
                Vector3 newScale = new(direction.magnitude, _newLine.transform.localScale.y, _newLine.transform.localScale.z);

                _newLine.transform.rotation = newRotation;
                _newLine.transform.localScale = newScale;
                _newLine.GetComponent<Line>().RenderLineSizeLabel(_initialPos, _currentPos, LineContainer);
            }
        }
    }
    private Vector3 OnSnapToGrid(Vector3 mousePosition)
    {
        Vector3 resultPosition = mousePosition;

        if(SnapToGrid)
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
        int count = LineList.Count - 1;

        Dictionary<GameObject, Vector> linesToSplit = new();

        for (int i = 0; i < count; i++)
        {
            if (line.GetComponent<Line>().startNode != LineList[i].GetComponent<Line>().endNode)
            {
               
                Vector p1 = new(line.GetComponent<Line>().startNode.transform.position.x, line.GetComponent<Line>().startNode.transform.position.y);
                Vector p2 = new(line.GetComponent<Line>().endNode.transform.position.x, line.GetComponent<Line>().endNode.transform.position.y);

                Vector q1 = new(LineList[i].GetComponent<Line>().startNode.transform.position.x, LineList[i].GetComponent<Line>().startNode.transform.position.y);
                Vector q2 = new(LineList[i].GetComponent<Line>().endNode.transform.position.x, LineList[i].GetComponent<Line>().endNode.transform.position.y);

                if (Utils.LineSegementsIntersect(p1, p2, q1, q2, out Vector intersectionPoint, true))
                {
                    print("Line " + line.name + " Overlaps with " + LineList[i] + " at point " + intersectionPoint.X + " " + intersectionPoint.Y);
                    if (!double.IsNaN(intersectionPoint.X) || !double.IsNaN(intersectionPoint.Y))
                    {
                        linesToSplit.Add(LineList[i], intersectionPoint);
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
        _newLine = GameObject.Instantiate(LineSprite);
        _newLine.name = "Line" + LineList.Count();
        _newLine.transform.parent = LineContainer;
        _newLine.transform.position = position;
        _newLine.layer = _layerFloorplan;

        Line line = _newLine.GetComponent<Line>();
        line.name = _newLine.name;

        if (_currentNode == null)
        {
            line.startNode = _initialNode;
        }
        else
        {
            line.startNode = _currentNode;
        }

        LineList.Add(_newLine);
    }

    void InstantiateLine(GameObject startNode, GameObject endNode, Quaternion rotation, int multiplier)
    {
        _newLine = GameObject.Instantiate(LineSprite);
        _newLine.name = "Line" + LineList.Count();
        _newLine.transform.parent = LineContainer;
        _newLine.transform.position = startNode.transform.position;

        _newLine.transform.localScale = new Vector3(multiplier * Vector3.Distance(startNode.transform.position, endNode.transform.position), 0.2f, 1);
        _newLine.transform.rotation = rotation;
        _newLine.layer = _layerFloorplan;
        Line w = _newLine.GetComponent<Line>();
        w.startNode = startNode;
        w.endNode = endNode;
        LineList.Add(_newLine);
    }

    GameObject InstantiateNode(Vector3 position)
    {
        GameObject newNode = NormalizeNodeAtPoint(position);
        if (newNode == null)
        {
            newNode = GameObject.Instantiate(NodeSprite);
            newNode.transform.position = position;
            newNode.transform.parent = NodeContainer;
            newNode.name = "Node " + NodeList.Count();
            newNode.layer = _layerFloorplan;
            NodeList.Add(newNode);
        }
        if (!DidDraw)
        {
            _initialNode = newNode;
        }
        if (_currentNode != null)
        {
            if (newNode != _currentNode)
            {
                _currentNode.GetComponent<Node>().adjacentNodes.Add(newNode);
            }
        }
        _currentNode = newNode;
        return newNode;
    }

    GameObject InstantiateIntersectionNode(Vector3 position)
    {
        GameObject newNode = NormalizeNodeAtPoint(position);
        if (newNode == null)
        {
            newNode = GameObject.Instantiate(NodeSprite);
            newNode.transform.position = position;
            newNode.transform.parent = NodeContainer;
            newNode.name = "Node " + NodeList.Count();
            newNode.layer = _layerFloorplan;

            NodeList.Add(newNode);
        }
        print("Instantiating intersection node with name " + newNode.name);

        return newNode;
    }

    GameObject NormalizeNodeAtPoint(Vector3 position)
    {
        print("Normalizing");
        for (int i = 0; i < NodeList.Count; i++)
        {
            if (NodeList[i].GetComponent<Renderer>().bounds.Contains(position))
            {
                print("Overlap with node " + NodeList[i]);
                return NodeList[i];
            }
        }
        return null;
    }


    void SetPreviousLineEndNode()
    {
        LineList.Last().GetComponent<Line>().endNode = InstantiateNode(_initialPos);
        //LineList.Last().GetComponent<BoxCollider>().enabled = true;
    }
    private void OnClickMouseRight()
    {
        if (Input.GetMouseButtonDown(1))
        {
            RemoveDrawingLine();
            ResetLineHighlight();
        }
    }

    private void RemoveDrawingLine()
    {
        if (_currentNode != null)
        {
            LineList.Remove(_newLine);
            GameObject.DestroyImmediate(_newLine);

            if (_currentNode.GetComponent<Node>().adjacentNodes.Count == 0 && !DidDraw)
            {
                NodeList.Remove(_currentNode);
                GameObject.Destroy(_currentNode);
            }
            IsDrawing = false;
            _currentNode = null;
        }
    }
    public List<GameObject> ExportNodes()
    {
        return NodeList;
    }

    public List<GameObject> ExportObjects()
    {
        return HouseObjectList;
    }

    public List<GameObject> ExportWindows()
    {
        for (int i = 0; i < WindowList.Count; i++)
        {
            RaycastHit[] hitList = Physics.RaycastAll(transform.TransformPoint(WindowList[i].transform.position), Vector3.forward);

            int correctLineIndex = -1;

            for (int j = 0; j < hitList.Length; j++)
            {
                print("The ray hit" + hitList[j].transform.name);
                if (hitList[j].transform.name.Contains("Line"))
                {
                    if (Mathf.Approximately(hitList[j].transform.rotation.eulerAngles.z, WindowList[i].transform.rotation.eulerAngles.z))
                    {
                        correctLineIndex = j;
                    }
                    break;
                }
            }

            LineAttachableObject window = WindowList[i].GetComponent<LineAttachableObject>();
            if (correctLineIndex < hitList.Length && correctLineIndex != -1)
            {
                window.startNode = hitList[correctLineIndex].transform.GetComponent<Line>().startNode;
                window.endNode = hitList[correctLineIndex].transform.GetComponent<Line>().endNode;
            }
        }
        return WindowList;
    }

    public void Refresh()
    {
        NodeList.Clear();
        DestroyChildren(NodeContainer);

        LineList.Clear();
        DestroyChildren(LineContainer);

        WindowList.Clear();
        DestroyChildren(AttachableObjectContainer); 

        HouseObjectList.Clear();
        DestroyChildren(HouseObjectContainer);

        IsDrawing = false;
        DidDraw = false;
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
