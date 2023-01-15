using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DrawingManager : MonoBehaviour
{

    public GameObject lineSprite;
    public GameObject nodeSprite;
    public Transform lineContainer, nodeContainer, houseObjectcontainer, windowContainer;

    GameObject newLine;
    GameObject initialNode, currentNode;

    public List<GameObject> nodeList = new();
    public List<GameObject> lineList = new();
    public List<GameObject> windowList = new();
    public List<GameObject> houseObjectList = new();

    private Vector3 _initialPos, _currentPos;
    private float _xRotation;
    public bool isDrawing = false; //This is used to determine whether the user has stopped drawing (right click) and perform necessary action
                                   // Use this for initialization
    public bool didDraw = false;

    void Start()
    {
        addTapGesture();
    }

    void addTapGesture()
    {
        TKTapRecognizer tapRecognizer = new();

        tapRecognizer.gestureRecognizedEvent += (r) =>
        {
            //Debug.Log ("tap recognizer fired: " + r);
            if (gameObject.activeInHierarchy)
            {
                if (gameObject.GetComponent<BoxCollider>().bounds.Contains(transform.TransformPoint(GetCurrentMousePosition(r.startTouchLocation()).GetValueOrDefault())))
                {
                    if (!isDrawing)
                    {
                        didDraw = false;
                        isDrawing = true;
                        _initialPos = GetCurrentMousePosition(r.startTouchLocation()).GetValueOrDefault();
                        instantiateNode(_initialPos);
                    }
                    else
                    {
                        didDraw = true;
                        float newXpos = lineList.Last().transform.position.x + lineList.Last().transform.localScale.x * Mathf.Cos(_xRotation * Mathf.PI / 180f);
                        float newYpos = lineList.Last().transform.position.y + lineList.Last().transform.localScale.x * Mathf.Sin(_xRotation * Mathf.PI / 180f);

                        newXpos = Mathf.Round(newXpos * 100) / 100f;
                        newYpos = Mathf.Round(newYpos * 100) / 100f;

                        _initialPos = _currentPos;

                        setPreviousLineEndNode();
                        handleOverlap(lineList.Last());
                    }

                    instantiateWall(_initialPos);

                }
                else
                {
                    removeDrawingLine();
                }
            }
        };


        TouchKit.addGestureRecognizer(tapRecognizer);
    }

    void handleOverlap(GameObject line)
    {
        int count = lineList.Count - 1;

        Dictionary<GameObject, Vector> linesToSplit = new();

        for (int i = 0; i < count; i++)
        {
            if (line.GetComponent<Line>().startNode != lineList[i].GetComponent<Line>().endNode)
            {
                Vector intersectionPoint = new();
                Vector p1 = new(line.GetComponent<Line>().startNode.transform.position.x, line.GetComponent<Line>().startNode.transform.position.y);
                Vector p2 = new(line.GetComponent<Line>().endNode.transform.position.x, line.GetComponent<Line>().endNode.transform.position.y);

                Vector q1 = new(lineList[i].GetComponent<Line>().startNode.transform.position.x, lineList[i].GetComponent<Line>().startNode.transform.position.y);
                Vector q2 = new(lineList[i].GetComponent<Line>().endNode.transform.position.x, lineList[i].GetComponent<Line>().endNode.transform.position.y);
                if (LineSegementsIntersect(p1, p2, q1, q2, out intersectionPoint, true))
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
            splitLine(item.Key, new Vector3((float)item.Value.X, (float)item.Value.Y, 0));
        }
    }

    void splitLine(GameObject line, Vector3 position)
    {
        GameObject newNode = instantiateIntersectionNode(position);
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

        instantiateLine(newNode, endNode, line.transform.rotation, multiplier);

        line.GetComponent<Line>().endNode = newNode;

        line.transform.localScale = new Vector3(multiplier * Vector3.Distance(startNode.transform.position, line.GetComponent<Line>().endNode.transform.position), scale.y, scale.z);

    }



    void instantiateWall(Vector3 position)
    {
        newLine = GameObject.Instantiate(lineSprite);
        newLine.name = "Line" + lineList.Count();
        newLine.transform.parent = lineContainer;
        newLine.transform.position = _initialPos;
        //newLine.GetComponent<BoxCollider>().enabled = false;
        Line w = newLine.GetComponent<Line>();
        w.name = newLine.name;

        if (currentNode == null)
        {
            w.startNode = initialNode;
        }
        else
        {
            w.startNode = currentNode;
        }

        lineList.Add(newLine);
    }

    void instantiateLine(GameObject startNode, GameObject endNode, Quaternion rotation, int multiplier)
    {
        newLine = GameObject.Instantiate(lineSprite);
        newLine.name = "Line" + lineList.Count();
        newLine.transform.parent = lineContainer;
        newLine.transform.position = startNode.transform.position;

        newLine.transform.localScale = new Vector3(multiplier * Vector3.Distance(startNode.transform.position, endNode.transform.position), 0.2f, 1);
        newLine.transform.rotation = rotation;
        Line w = newLine.GetComponent<Line>();
        w.startNode = startNode;
        w.endNode = endNode;
        lineList.Add(newLine);
    }

    GameObject instantiateNode(Vector3 position)
    {
        GameObject newNode = NormalizeNodeAtPoint(position);
        if (newNode == null)
        {
            newNode = GameObject.Instantiate(nodeSprite);
            newNode.transform.position = position;
            newNode.transform.parent = nodeContainer;
            newNode.name = "Node " + nodeList.Count();
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

    GameObject instantiateIntersectionNode(Vector3 position)
    {
        GameObject newNode = NormalizeNodeAtPoint(position);
        if (newNode == null)
        {
            newNode = GameObject.Instantiate(nodeSprite);
            newNode.transform.position = position;
            newNode.transform.parent = nodeContainer;
            newNode.name = "Node " + nodeList.Count();

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


    void setPreviousLineEndNode()
    {
        lineList.Last().GetComponent<Line>().endNode = instantiateNode(_initialPos);
        //lineList.Last().GetComponent<BoxCollider>().enabled = true;
    }

    // Update is called once per frame
    void Update()
    {

        detectRightClick();

        if (newLine != null && isDrawing)
        {
            _currentPos = GetCurrentMousePosition(Input.mousePosition).GetValueOrDefault();

            if (gameObject.GetComponent<BoxCollider>().bounds.Contains(transform.TransformPoint(_currentPos)))
            {
                Vector3 direction = _currentPos - _initialPos;

                float newX = direction.magnitude; //The new X scale for the 

                // Need to give new value of rotation for the line script
                float angle = ((Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg) - 90) * -1;
                Quaternion newRotation = Quaternion.AngleAxis(angle, Vector3.forward);

                //_xRotation = Mathf.Round(newRotation.eulerAngles.z / 5) * 5;
                //newRotation = Quaternion.Euler(newRotation.x, newRotation.y, _xRotation);
                newLine.transform.rotation = newRotation;

                Vector3 newScale = new(newX, newLine.transform.localScale.y, newLine.transform.localScale.z);
                newLine.transform.localScale = newScale;

                newLine.GetComponent<Line>().RenderLineSizeLabel(_initialPos, _currentPos, lineContainer);
            }
        }
    }

    private void detectRightClick()
    {
        if (Input.GetMouseButtonDown(1))
        {
            removeDrawingLine();
        }
    }

    private void removeDrawingLine()
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

    private Vector3? GetCurrentMousePosition(Vector3 screenPosition)
    {
        if (Camera.main != null)
        {
            var ray = Camera.main.ScreenPointToRay(screenPosition);
            var plane = new Plane(Vector3.forward, Vector3.zero);

            float rayDistance;
            if (plane.Raycast(ray, out rayDistance))
            {
                return ray.GetPoint(rayDistance);
            }
        }
        return null;
    }

    public List<GameObject> exportNodes()
    {
        return nodeList;
    }

    public List<GameObject> exportObjects()
    {
        return houseObjectList;
    }

    public List<GameObject> exportWindows()
    {

        //(GetComponent<Renderer>().bounds.center, GetComponent<Renderer>().bounds.extents * 1.1f, Vector3.forward, transform.rotation, float.PositiveInfinity, layerMask);

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

            WallAttachableObject w = windowList[i].GetComponent<WallAttachableObject>();
            if (correctLineIndex < hitList.Length && correctLineIndex != -1)
            {
                w.startNode = hitList[correctLineIndex].transform.GetComponent<Line>().startNode;
                w.endNode = hitList[correctLineIndex].transform.GetComponent<Line>().endNode;
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

    /// <summary>
    /// Test whether two line segments intersect. If so, calculate the intersection point.
    /// <see cref="http://stackoverflow.com/a/14143738/292237"/>
    /// </summary>
    /// <param name="p">Vector to the start point of p.</param>
    /// <param name="p2">Vector to the end point of p.</param>
    /// <param name="q">Vector to the start point of q.</param>
    /// <param name="q2">Vector to the end point of q.</param>
    /// <param name="intersection">The point of intersection, if any.</param>
    /// <param name="considerOverlapAsIntersect">Do we consider overlapping lines as intersecting?
    /// </param>
    /// <returns>True if an intersection point was found.</returns>
    public static bool LineSegementsIntersect(Vector p, Vector p2, Vector q, Vector q2,
        out Vector intersection, bool considerCollinearOverlapAsIntersect = false)
    {
        intersection = new Vector();

        var r = p2 - p;
        var s = q2 - q;
        var rxs = r.Cross(s);
        var qpxr = (q - p).Cross(r);

        // If r x s = 0 and (q - p) x r = 0, then the two lines are collinear.
        if (rxs.IsZero() && qpxr.IsZero())
        {
            // 1. If either  0 <= (q - p) * r <= r * r or 0 <= (p - q) * s <= * s
            // then the two lines are overlapping,
            if (considerCollinearOverlapAsIntersect)
                if ((0 <= (q - p) * r && (q - p) * r <= r * r) || (0 <= (p - q) * s && (p - q) * s <= s * s))
                    return true;

            // 2. If neither 0 <= (q - p) * r = r * r nor 0 <= (p - q) * s <= s * s
            // then the two lines are collinear but disjoint.
            // No need to implement this expression, as it follows from the expression above.
            return false;
        }

        // 3. If r x s = 0 and (q - p) x r != 0, then the two lines are parallel and non-intersecting.
        if (rxs.IsZero() && !qpxr.IsZero())
            return false;

        // t = (q - p) x s / (r x s)
        var t = (q - p).Cross(s) / rxs;

        // u = (q - p) x r / (r x s)

        var u = (q - p).Cross(r) / rxs;

        // 4. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
        // the two line segments meet at the point p + t r = q + u s.
        if (!rxs.IsZero() && (0 <= t && t <= 1) && (0 <= u && u <= 1))
        {
            // We can calculate the intersection point using either t or u.
            intersection = p + t * r;

            // An intersection was found.
            return true;
        }

        // 5. Otherwise, the two line segments are not parallel but do not intersect.
        return false;
    }
}
