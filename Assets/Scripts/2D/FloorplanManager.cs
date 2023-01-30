using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class FloorplanManager : MonoBehaviour
{
    public UIActionManager UIActionManager;
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
    bool _objectIsDragged = false;
    bool _objectMoved = false;

    GameObject _currentNode, _newLine, _draggingObject, _selectedNode;
    Collider2D _mouseOverObject;

    LineRenderer _highlightedLineX, _highlightedLineY;
    Color _highlightedLineXColor, _highlightedLineYColor;

    Vector3 _initialMousePosition, _currentMousePosition;

    readonly int _layerFloorplan = Globals.Layers.Floorplan;

    // Update is called once per frame
    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        _currentMousePosition = SnapToGridLines(Utils.GetCurrentMousePosition());
        _mouseOverObject = Physics2D.OverlapPoint(_currentMousePosition);

        OnMouseLeftDown();
        OnObjectDrag();
        OnMouseLeftUp();
        OnUpdateDrawingLine();
        OnMouseRightDown();
    }

    void OnObjectDrag()
    {
        float positionDifference = Vector3.Distance(_initialMousePosition, _currentMousePosition);
    
        if (_objectIsDragged &&  positionDifference > Globals.Node.Size || _objectMoved)
        {
            _draggingObject.transform.position = _currentMousePosition;
            _objectMoved = true;
            AdjustAllLines();
        }
    }
    void OnMouseLeftDown()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        _initialMousePosition = SnapToGridLines(Utils.GetCurrentMousePosition());

        if (!gameObject.GetComponent<BoxCollider>().bounds.Contains(_currentMousePosition))
        {
            RemoveDrawingLine();
            return;
        }

        if (_isDrawing)
        {
            DidDraw = true;
            SetPreviousLineEndNode();
            HandleOverlap(LineList.Last());
        } else
        {
            if (IsMouseOverNode())
            {
                _objectIsDragged = true;
                _draggingObject = _mouseOverObject.transform.gameObject;
            }
            else
            {
                _isDrawing = true;
                InstantiateNode(_currentMousePosition);
            }
        }

        if(!_objectIsDragged) InstantiateDrawingLine(_currentMousePosition);
        DeselectNode();
    }

    bool IsMouseOverNode()
    {
        return _mouseOverObject != null && _mouseOverObject.GetComponent<Node>() != null ? true : false;
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

    void OnMouseLeftUp()
    {
        if (!Input.GetMouseButtonUp(0)) return;

        if (IsMouseOverNode() && !_isDrawing && !_objectMoved)
        {
            SelectNode(_mouseOverObject);
        }

        _objectIsDragged = false;
        _objectMoved = false;
        _draggingObject = null;
    }

    public void RemoveSelectedNode()
    {
        if (_selectedNode == null) return;

        LineList.RemoveAll(line =>
        {
            Line lineComponent = line.GetComponent<Line>();
            if (lineComponent.startNode == _selectedNode || lineComponent.endNode == _selectedNode)
            {
                Destroy(line); 
                return true;
            };
            return false;
        });

        List<GameObject> unlinkedNodes = new();

        foreach (GameObject node in NodeList)
        {
            Node nodeComponent = node.GetComponent<Node>();

            if (nodeComponent.AdjacentNodes.Contains(_selectedNode))
            {
                nodeComponent.AdjacentNodes.Remove(_selectedNode);
            }

            if (nodeComponent.AdjacentNodes.Count == 0) unlinkedNodes.Add(node);
        }

        NodeList.Remove(_selectedNode);
        Destroy(_selectedNode);

        for (int i = 0; i < unlinkedNodes.Count - 1; i++)
        {
                InstantiateLine(unlinkedNodes[i], unlinkedNodes[i + 1]);   
        }

        AdjustAllLines();
        DeselectNode();
    }

    private void SelectNode(Collider2D targetObject)
    {
        _selectedNode = targetObject.transform.gameObject;
        UIActionManager.ShowNodePanel(targetObject.transform.position);
    }
    private void DeselectNode()
    {
        _selectedNode = null;
        UIActionManager.HideNodePanel();
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
        highlightedLineColorRef = line.colorGradient.Evaluate(.5f);
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
        Line lineComponent = line.GetComponent<Line>();

        for (int i = 0; i < count; i++)
        {
            Line currentLine = LineList[i].GetComponent<Line>();

            if (lineComponent.startNode == currentLine.endNode || lineComponent.startNode == currentLine.startNode ||
            lineComponent.endNode == currentLine.startNode || lineComponent.endNode == currentLine.endNode) continue;

            Vector p1 = new(lineComponent.startNode.transform.position.x, lineComponent.startNode.transform.position.y);
            Vector p2 = new(lineComponent.endNode.transform.position.x, lineComponent.endNode.transform.position.y);

            Vector q1 = new(currentLine.startNode.transform.position.x, currentLine.startNode.transform.position.y);
            Vector q2 = new(currentLine.endNode.transform.position.x, currentLine.endNode.transform.position.y);

            if (Utils.LineSegementsIntersect(p1, p2, q1, q2, out Vector intersectionPoint, true))
            {
                if (!double.IsNaN(intersectionPoint.X) || !double.IsNaN(intersectionPoint.Y))
                {
                    linesToSplit.Add(LineList[i], intersectionPoint);
                    linesToSplit.Add(line, intersectionPoint);
                    break;
                }
            }
        }

        for (int index = 0; index < linesToSplit.Count; index++)
        {
            var item = linesToSplit.ElementAt(index);
            SplitLine(item.Key, new Vector3((float)item.Value.X, (float)item.Value.Y, 0));
        }

        if(linesToSplit.Count > 0)
        {
            HandleOverlap(LineList.Last());
        }
    }

    void SplitLine(GameObject lineObject, Vector3 position)
    {
        Line line = lineObject.GetComponent<Line>();

        GameObject startNodeObject = line.startNode;
        GameObject intersectionNodeObject = InstantiateNode(position);
        GameObject endNodeObject = line.endNode;

        Node startNode = startNodeObject.GetComponent<Node>();
        Node intersectionNode = intersectionNodeObject.GetComponent<Node>();
        Node endNode = endNodeObject.GetComponent<Node>();

        Vector3 scale = lineObject.transform.localScale;
        int multiplier = scale.x < 0 ? -1 : 1;

        
        line.endNode = intersectionNodeObject;
        InstantiateLine(intersectionNodeObject, endNodeObject, lineObject.transform.rotation, multiplier);

        line.AdjustLine();
        _newLine.GetComponent<Line>().AdjustLine();

        startNode.AdjacentNodes.Remove(endNodeObject);
        endNode.AdjacentNodes.Remove(startNodeObject);

        startNode.AdjacentNodes.AddIfNotThere(intersectionNodeObject);
        intersectionNode.AdjacentNodes.AddIfNotThere(startNodeObject);
        intersectionNode.AdjacentNodes.AddIfNotThere(endNodeObject);
        endNode.AdjacentNodes.AddIfNotThere(intersectionNodeObject);
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
        line.startNode = _currentNode;
 
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
        Line line = _newLine.GetComponent<Line>();
        line.startNode = startNode;
        line.endNode = endNode;
        LineList.Add(_newLine);
    }

    void InstantiateLine(GameObject startNode, GameObject endNode)
    {
        _newLine = GameObject.Instantiate(LineSprite);
        _newLine.name = "Line" + LineList.Count();
        _newLine.transform.parent = LineContainer;
        _newLine.transform.position = startNode.transform.position;
        _newLine.layer = _layerFloorplan;
        Line line = _newLine.GetComponent<Line>();
        line.startNode = startNode;
        line.endNode = endNode;
        line.AdjustLine();
        LineList.Add(_newLine);
    }

    GameObject InstantiateNode(Vector3 position)
    {
        GameObject newNode = NormalizeNodeAtPoint(position);
        if (newNode == null)
        {
            newNode = GameObject.Instantiate(NodeSprite);
            newNode.transform.localScale = new Vector3(Globals.Node.Size, Globals.Node.Size);
            newNode.transform.position = position;
            newNode.transform.parent = NodeContainer;
            newNode.name = "Node " + NodeList.Count();
            newNode.layer = _layerFloorplan;
            newNode.AddComponent<BoxCollider2D>();
            NodeList.Add(newNode);
        }

        if (_currentNode != null && newNode != _currentNode)
        {
            _currentNode.GetComponent<Node>().AdjacentNodes.Add(newNode);
            newNode.GetComponent<Node>().AdjacentNodes.Add(_currentNode);
        }

        _currentNode = newNode;

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
        LineList.Last().GetComponent<Line>().endNode = InstantiateNode(_currentMousePosition);
        LineList.Last().GetComponent<BoxCollider>().enabled = true;
    }
    private void OnMouseRightDown()
    {
        if (!Input.GetMouseButtonDown(1)) return;

        RemoveDrawingLine();
        ResetLineHighlight();
        DeselectNode();
    }

    private void RemoveDrawingLine()
    {
        if (_currentNode != null)
        {
            LineList.Remove(_newLine);
            DestroyImmediate(_newLine);
            Node node = _currentNode.GetComponent<Node>();

            if (node.AdjacentNodes.Count == 0)
            {
               NodeList.Remove(_currentNode);
               Destroy(_currentNode);
            }
            _isDrawing = false;
            _currentNode = null;
        }
    }
    public List<GameObject> ExportNodes()
    {
        return NodeList;
    }
    public List<GameObject> ExportLines()
    {
        return LineList;
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
                child.SetParent(null);
                Destroy(child.gameObject);
            }
            else DestroyImmediate(child.gameObject);
        }
    }
}
