using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using System;

public class FloorplanManager : MonoBehaviour
{
    public UIActionManager UIActionManager;

    [SerializeField]
    GameObject LineSprite;

    [SerializeField]
    GameObject NodeSprite;

    [SerializeField]
    GameObject PolygonSprite;

    [SerializeField]
    Transform LineContainer, NodeContainer,
    HouseObjectContainer, AttachableObjectContainer, PolygonContainer;

    public List<GameObject> NodeList = new();
    public List<GameObject> LineList = new();
    public List<GameObject> WindowList = new();
    public List<GameObject> HouseObjectList = new();
    List<GameObject> PolygonList = new();

    public bool DidDraw = false;
    public bool HasPolygons = false;

    bool _isDrawing = false;
    bool _objectIsDragged = false;
    bool _objectMoved = false;
    bool _objectIsSelected = false;

    GameObject _currentNode, _newLine, _draggingObject, _selectedObject;
    
    GridRenderer _gridRenderer;
    Vector3 _initialMousePosition, _currentMousePosition;

    readonly int _layerFloorplan = Globals.Layers.Floorplan;

    // Update is called once per frame
    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        
        _gridRenderer = GetComponent<GridRenderer>();
        _currentMousePosition = _gridRenderer.SnapToGridLines(Utils.GetCurrentMousePosition());

        OnMouseLeftDown();
        OnMouseRightDown();
        OnObjectDrag();
        OnUpdateDrawingLine();
    }

    private void UpdatePolygon()
    {
        GetPolygonsNodes();
        SetHasPolygons();
    }

    private void SetHasPolygons()
    {
        HasPolygons = PolygonList.Count > 0 ? true : false;
    }

    private void GetPolygonsNodes()
    {
        if (NodeList.Count < 3) return;

        DestroyChildren(PolygonContainer);
        PolygonList.Clear();

        List<GameObject> polygonNodes = new();
        GameObject firstAdjacent = null;

        for(var i = 0; i < NodeList.Count; i++)
        {
            var currentElement = NodeList[i];
            var nextElement = i + 1 < NodeList.Count ? NodeList[i + 1] : NodeList[0];
            var prevElement = i - 1 >= 0 && i - 1 < NodeList.Count ? NodeList[i - 1] : null;
            var currentNode = currentElement.GetComponent<Node>();

            if (currentNode.AdjacentNodes.Count < 2)
            {
                firstAdjacent = null;
                polygonNodes.Clear();
                continue;
            }

            if (currentNode.AdjacentNodes.Contains(nextElement))
            {
                if (firstAdjacent == null) firstAdjacent = currentElement;
                if (!polygonNodes.Contains(currentElement)) polygonNodes.Add(currentElement);
            }

            if (prevElement != null && prevElement == firstAdjacent) continue;

            if(firstAdjacent != null && currentNode.AdjacentNodes.Contains(firstAdjacent))
            {
                if (!polygonNodes.Contains(currentElement)) polygonNodes.Add(currentElement);
                PolygonList.Add(Polygon2D.Instantiate(new List<GameObject>(polygonNodes), PolygonSprite, PolygonContainer));
                polygonNodes.Clear();
                firstAdjacent = null;
            }
        }
    }

    bool IsNode(GameObject _object)
    {
        return _object.name.Contains("Node");
    }
    bool IsLine(GameObject _object)
    {
        return _object.name.Contains("Line");
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

        _initialMousePosition = _gridRenderer.SnapToGridLines(Utils.GetCurrentMousePosition());

        if (!gameObject.GetComponent<BoxCollider>().bounds.Contains(_currentMousePosition))
        {
            RemoveDrawingLine();
            return;
        }

        if (_isDrawing)
        {
            DidDraw = true;
            SetPreviousLineEndNode();
            AdjustAllLines();
            HandleOverlap(LineList.Last());
            InstantiateDrawingLine(_currentMousePosition);
            UpdatePolygon();
            return;
        }

        if (!_objectIsDragged && !IsMouseOverLine())
        {
            _isDrawing = true;
            InstantiateNode(_currentMousePosition);
            InstantiateDrawingLine(_currentMousePosition);
            DeselectLine();
        }

        DeselectNode();
    }
    private void OnMouseRightDown()
    {
        if (!Input.GetMouseButtonDown(1)) return;

        RemoveDrawingLine();
        DeselectAll();
    }

    public void StartDrag(GameObject dragObject)
    {
        _objectIsDragged = true;
        _draggingObject = dragObject;
    }

    bool IsMouseOverNode()
    {
        return NodeList.Exists(item => item.GetComponent<Node>().isMouseOver == true);
    }
    bool IsMouseOverLine()
    {
        return LineList.Exists(item => item.GetComponent<Line>().isMouseOver == true);
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

    public void ResetDrag()
    {
        var draggingLine = LineList.Find(item => item.GetComponent<Line>().startNode == _draggingObject || item.GetComponent<Line>().endNode == _draggingObject);
        HandleOverlap(draggingLine);
        _objectIsDragged = false;
        _objectMoved = false;
        _draggingObject = null;
    }

    public void RemoveSelectedNode()
    {
        if (_selectedObject == null) return;

        LineList.RemoveAll(line =>
        {
            Line lineComponent = line.GetComponent<Line>();
            if (lineComponent.startNode == _selectedObject || lineComponent.endNode == _selectedObject)
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

            if (nodeComponent.AdjacentNodes.Contains(_selectedObject))
            {
                nodeComponent.AdjacentNodes.Remove(_selectedObject);
            }

            if (nodeComponent.AdjacentNodes.Count == 0) unlinkedNodes.Add(node);
        }

        NodeList.Remove(_selectedObject);
        Destroy(_selectedObject);

        for (int i = 0; i < unlinkedNodes.Count - 1; i++)
        {
                InstantiateLine(unlinkedNodes[i], unlinkedNodes[i + 1]);   
        }

        AdjustAllLines();
        UpdatePolygon();
        DeselectNode();
    }

    public void DrawLineFromNode()
    {
        if (_selectedObject == null) return;
        _isDrawing = true;
        _currentNode = _selectedObject;
        InstantiateDrawingLine(_selectedObject.transform.position);
        DeselectNode();
    }

    public void SelectNode(GameObject node)
    {
        DeselectAll();
        _objectIsSelected = true;
        _selectedObject = node;
        UIActionManager.ShowNodePanel(node.transform.position);
    }

    public void SelectLine(GameObject line)
    {
        DeselectAll();
        _objectIsSelected = true;
        _selectedObject = line;
        UIActionManager.ShowLinePanel();
    }

    private void DeselectNode()
    {
        if (_selectedObject != null && IsNode(_selectedObject))
        {
            _selectedObject.GetComponent<Node>().Deselect();
            _objectIsSelected = false;
            _selectedObject = null;
            UIActionManager.HideNodePanel();
        }
    }

    private void DeselectLine()
    {
        if (_selectedObject != null && IsLine(_selectedObject))
        {
            _selectedObject.GetComponent<Line>().Deselect();
            _objectIsSelected = false;
            _selectedObject = null;
            UIActionManager.HideLinePanel();
            UIActionManager.HideResizePanel();
        }
    }

    private void DeselectAll()
    {
        DeselectNode();
        DeselectLine();
    }

    void AdjustAllLines()
    {
        foreach(GameObject line in LineList)
        {
            line.GetComponent<Line>().AdjustLine();
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
            newNode.AddComponent<BoxCollider>();
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

        PolygonList.Clear();
        DestroyChildren(PolygonContainer);

        DeselectAll();

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

    public void AddNode()
    {
        if (!_selectedObject) return;

        var line = _selectedObject.GetComponent<Line>();
        var startNode = line.startNode;
        var endNode = line.endNode;

        Vector3 middlePoint = (startNode.transform.position + endNode.transform.position) / 2f;
        var middleNode = InstantiateNode(middlePoint);

        middleNode.GetComponent<Node>().AdjacentNodes.Add(startNode);
        middleNode.GetComponent<Node>().AdjacentNodes.Add(endNode);

        line.endNode = middleNode;

        startNode.GetComponent<Node>().AdjacentNodes.Add(middleNode);
        endNode.GetComponent<Node>().AdjacentNodes.Add(middleNode);

        InstantiateLine(middleNode, endNode);
        AdjustAllLines();

        _currentNode = null;
        DeselectLine();
    }

    public void ResizeSelectedLine(float length)
    {   
       _selectedObject.GetComponent<Line>().Resize(length);
        AdjustAllLines();
        DeselectLine();
    }

    public void RemoveSelectedLine()
    {
        if (_selectedObject == null) return;
        LineList.Remove(_selectedObject);
        Destroy(_selectedObject);
        DeselectLine();
        UpdatePolygon();
    }

    public float GetSelectedLineLength()
    {
        if(!_selectedObject) return 0;

        return _selectedObject.GetComponent<Line>().length;
    }

    public bool CanSelect()
    {
        return _isDrawing || _objectMoved ? false : true;
    }
}
