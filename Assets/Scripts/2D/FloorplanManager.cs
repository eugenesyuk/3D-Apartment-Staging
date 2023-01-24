using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FloorplanManager : MonoBehaviour
{

    public GameObject LineSprite;
    public GameObject NodeSprite;

    public Transform LineContainer, NodeContainer,
    HouseObjectContainer, AttachableObjectContainer;

    public List<GameObject> NodeList = new();
    public List<GameObject> LineList = new();
    public List<GameObject> WindowList = new();
    public List<GameObject> HouseObjectList = new();

    public bool SnapToGrid = true;
    public bool DidDraw = false;

    bool _isDrawing = false;
    bool _objectIsSelected = false;
    bool _objectMoved = false;

    GameObject _initialNode, _currentNode, _newLine, _selectedObject;

    LineRenderer _highlightedLineX, _highlightedLineY;
    Color _highlightedLineXColor, _highlightedLineYColor;

    Vector3 _initialMousePosition, _currentMousePosition, _offset;

    readonly int _layerFloorplan = Globals.Layers.Floorplan;

    void Start()
    {
        OnTapGesture();
    }

    // Update is called once per frame
    void Update()
    {
        _currentMousePosition = SnapToGridLines(Utils.GetCurrentMousePosition());
        OnObjectMove();
        OnObjectMoved();
        OnUpdateDrawingLine();
        OnClickMouseRight();
    }

    void OnTapGesture()
    {
        TKTapRecognizer tapRecognizer = new();

        tapRecognizer.gestureRecognizedEvent += (r) =>
        {
            if (!gameObject.activeInHierarchy) return;

            Vector2 tapPosition = Utils.GetMousePosition(r.startTouchLocation()).GetValueOrDefault();

            if (gameObject.GetComponent<BoxCollider>().bounds.Contains(transform.TransformPoint(tapPosition)))
            {
                if (!_isDrawing && !_objectIsSelected)
                {
                    _isDrawing = true;
                    _initialMousePosition = SnapToGridLines(tapPosition);
                    InstantiateNode(_initialMousePosition);
                }
                else
                {
                    DidDraw = true;
                    _initialMousePosition = _currentMousePosition;

                    SetPreviousLineEndNode();
                    HandleOverlap(LineList.Last());
                }

                InstantiateDrawingLine(_initialMousePosition);
            }
            else
            {
                RemoveDrawingLine();
            }
        };

        TouchKit.addGestureRecognizer(tapRecognizer);
    }

    void OnUpdateDrawingLine()
    {
        if (_newLine != null && _isDrawing)
        {
            if (gameObject.GetComponent<BoxCollider>().bounds.Contains(transform.TransformPoint(_currentMousePosition)))
            {
                _newLine.GetComponent<Line>().AdjustLine(_currentMousePosition);
            }
        }
    }

    void OnObjectMove()
    {
        if (Input.GetMouseButtonDown(0) && !_isDrawing)
        {
            Collider2D targetObject = Physics2D.OverlapPoint(_currentMousePosition);

            if (targetObject)
            {
                _objectIsSelected = true;
                _selectedObject = targetObject.transform.gameObject;
                _offset = _selectedObject.transform.position - _currentMousePosition;
            }
        }

        if (_selectedObject)
        {
            _selectedObject.transform.position = _currentMousePosition + _offset;
            _objectMoved = true;
        }

        if (Input.GetMouseButtonUp(0) && _selectedObject)
        {
            _objectIsSelected = false;
            _selectedObject = null;
        }
    }

    void OnObjectMoved()
    {
        if(_objectMoved)
        {
            AdjustAllLines();
            _objectMoved = false;
        }
    }

    void AdjustAllLines()
    {
        foreach(GameObject line in LineList)
        {
            line.GetComponent<Line>().AdjustLine();
        }
    }

    Vector2 SnapToGridLines(Vector3 mousePosition)
    {
        if (!SnapToGrid) return mousePosition;

        Vector2 resultPosition = mousePosition;
        GridRenderer gridRenderer = gameObject.GetComponent<GridRenderer>();

        GameObject closestGridLineY = gridRenderer.GetClosestLineY(mousePosition);
        GameObject closestGridLineX = gridRenderer.GetClosestLineX(mousePosition);

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
        highlightedLineColorRef = line.colorGradient.Evaluate(0);
        line.startColor = line.endColor = Globals.Line.HighlightColor;
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
            SplitLine(item.Key, new Vector3((float)item.Value.X, (float)item.Value.Y, 0));
        }
    }

    void SplitLine(GameObject line, Vector3 position)
    {
        GameObject newNode = InstantiateIntersectionNode(position);
        GameObject startNode = line.GetComponent<Line>().startNode;
        GameObject endNode = line.GetComponent<Line>().endNode;

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

    void InstantiateDrawingLine(Vector3 position)
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
            newNode.AddComponent<BoxCollider2D>();
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
                newNode.GetComponent<Node>().adjacentNodes.Add(_currentNode);
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

        return newNode;
    }

    GameObject NormalizeNodeAtPoint(Vector3 position)
    {
        for (int i = 0; i < NodeList.Count; i++)
        {
            if (NodeList[i].GetComponent<Renderer>().bounds.Contains(position))
            {
                return NodeList[i];
            }
        }
        return null;
    }


    void SetPreviousLineEndNode()
    {
        LineList.Last().GetComponent<Line>().endNode = InstantiateNode(_initialMousePosition);
        LineList.Last().GetComponent<BoxCollider>().enabled = true;
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

            if (_currentNode.GetComponent<Node>().adjacentNodes.Count == 0)
            {
                NodeList.Remove(_currentNode);
                GameObject.Destroy(_currentNode);
            }
            _isDrawing = false;
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

        _isDrawing = false;
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
                Destroy(child.gameObject);
            }
            else DestroyImmediate(child.gameObject);
        }
    }
}
