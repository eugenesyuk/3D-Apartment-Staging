using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Poly2Tri;

public class WallGenerator : MonoBehaviour
{
    public float Thickness = Globals.WallParams.Thickness;
    public LayerMask LayerMask3D;
    public GameObject Container3D;

    private readonly float _scaleFactor = Globals.ScaleFactor;
    private readonly Dictionary<Vector3, List<GameObject>> _nodes = new();
    private bool _alternator = false;
    private Vector3[][] _pointPairsArray = null;

    private readonly List<Vector3> _newVerts = new();
    private readonly List<int> _newTris = new();
    private int _newVertCount = 0;
    private readonly Dictionary<Vector3, int> _vertexIndices = new();
    private readonly Dictionary<GameObject, List<Hole>> _wallHoles = new();

    private GameObject[] _walls = null;
    private readonly int layer3D = Globals.Layers.Scene3D;
    public void Refresh()
    {
        _nodes.Clear();
        _pointPairsArray = null;
        _walls = null;
        _newVerts.Clear();
        _newTris.Clear();
        _newVertCount = 0;
        _vertexIndices.Clear();
        _wallHoles.Clear();
    }

    Vector3[][] GetPointPairsFromLines(List<GameObject> lineList)
    {
        List<Vector3[]> pointPairs = new();

        foreach (GameObject line in lineList)
        {
            Line lineComponent = line.GetComponent<Line>();

            Vector3 pointA = SwapVectorYZ(lineComponent.startNode.transform.position) / _scaleFactor;
            Vector3 pointB = SwapVectorYZ(lineComponent.endNode.transform.position) / _scaleFactor;

            pointPairs.Add(new Vector3[] { pointA, pointB });
        }

        return pointPairs.ToArray();
    }

    public void Generate3D(List<GameObject> lineList, List<GameObject> windowList, List<GameObject> houseObjectList)
    {
        _pointPairsArray = GetPointPairsFromLines(lineList);

        GenerateWalls();

        if (windowList.Count > 0)
        {
            AddWindows(windowList);
        }

        AdjustWalls();
        GenerateFloor();

        if (houseObjectList.Count > 0)
        {
            AddHouseObjects(houseObjectList);
        }
    }

    public void Destroy3D()
    {
        foreach (Transform child in Container3D.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        Refresh();
    }

    private void GenerateWalls()
    {
        GameObject[] walls = new GameObject[_pointPairsArray.Length];

        for (int i = 0; i < _pointPairsArray.Length; i++)
        {
            GameObject wallObject = new()
            {
                layer = layer3D,
                name = "Wall " + i
            };

            wallObject.transform.parent = Container3D.transform;
            walls[i] = wallObject;
            WallFunctions wallScript = wallObject.AddComponent<WallFunctions>();
            wallScript.GenerateWall(_pointPairsArray[i][0], _pointPairsArray[i][1]);

        }

        _walls = walls;

        //Create a dictionary of all _nodes and coincident _walls 
        //To be used for adjusting _walls and floor generation
        foreach (GameObject wallObject in walls)
        {
            NodeAddOrUpdate(wallObject.GetComponent<WallFunctions>().StartPoint, wallObject);
            NodeAddOrUpdate(wallObject.GetComponent<WallFunctions>().EndPoint, wallObject);
        }

    }
    private void AdjustWalls()
    {
        List<GameObject>[] nodeValues = _nodes.Values.ToArray();
        Vector3[] nodePoints = _nodes.Keys.ToArray();

        for (int i = 0; i < nodeValues.Count(); i++)
        {
            GameObject[] coincidentWalls = nodeValues[i].ToArray();
            for (int j = 0; j < coincidentWalls.Length; j++)
            {
                Vector3 start = GetStart(coincidentWalls[j]);
                Vector3 end = GetEnd(coincidentWalls[j]);
                Vector3 otherPoint = start == nodePoints[i] ? end : start;

                float angle = Vector3.Angle(otherPoint - nodePoints[i], new Vector3(1, 0, 0));
                Vector3 cross = Vector3.Cross(otherPoint - nodePoints[i], new Vector3(1, 0, 0));
                if (cross.y < 0) angle = -angle;

                coincidentWalls[j].GetComponent<WallFunctions>().Angle = angle;
            }

            if (coincidentWalls.Length < 3)
                coincidentWalls = coincidentWalls.OrderBy(w => w.GetComponent<WallFunctions>().Angle).ToArray();
            else
                coincidentWalls = coincidentWalls.OrderByDescending(w => w.GetComponent<WallFunctions>().Angle).ToArray();

            _alternator = false;
    
            if (coincidentWalls.Length > 1)
            {
                for (int j = 0; j < coincidentWalls.Length; j++)
                {
                    //Debug.Log(coincidentWalls[j].name + " : " + coincidentWalls[j].GetComponent<WallFunctions>().Angle + ", " + coincidentWalls[(j + 1) % coincidentWalls.Width].name + " : " + coincidentWalls[(j + 1) % coincidentWalls.Width].GetComponent<WallFunctions>().Angle);
                    _alternator = !_alternator;
                    AdjustShape(coincidentWalls[j], coincidentWalls[(j + 1) % coincidentWalls.Length], nodePoints[i]);
                }
            }
        }
    }
    private void GenerateFloor()
    {
        Vector3[] nodePoints = _nodes.Keys.ToArray();

        if (_pointPairsArray.Length > 2)
        {
            List<Point> p = new();
            for (int i = 0; i < nodePoints.Length; i++)
            {
                Point s = new()
                {
                    X = nodePoints[i].x,
                    Y = nodePoints[i].z
                };
                p.Add(s);
            }

            Point[] ch = ConvexHull.CH2(p).ToArray();
            Vector2[] floorVertices = new Vector2[ch.Length - 1];

            for (int i = 0; i < ch.Length - 1; i++)
            {
                floorVertices[i] = new Vector2(ch[i].X, ch[i].Y);
            }

            GameObject floor = new()
            {
                name = "Floor"
            };
            floor.transform.parent = Container3D.transform;
            floor.AddComponent<MeshFilter>();
            floor.AddComponent<MeshRenderer>();

            Mesh floorMesh = floor.GetComponent<MeshFilter>().mesh;

            Polygon floorPoly = CreatePoly(floorVertices);
            P2T.Triangulate(floorPoly);

            for (int i = 0; i < floorPoly.Triangles.Count; i++)
                for (int j = 0; j < 3; j++)
                {
                    TriangulationPoint tpt = floorPoly.Triangles[i].Points[j];
                    Vector3 pt = new((float)tpt.X, 0, (float)tpt.Y);
                    _newTris.Add(_vertexIndices[pt]);
                }

            floorMesh.vertices = _newVerts.ToArray();
            int[] tris = _newTris.ToArray();

            for (int i = 0; i < tris.Length; i += 3)
            {
                (tris[i + 2], tris[i + 1]) = (tris[i + 1], tris[i + 2]);
            }

            floorMesh.triangles = tris;
            floorMesh.RecalculateNormals();

            //assign material
            Material newMat = new(Shader.Find("Unlit/Color"))
            {
                color = Color.gray
            };//GetDefaultMaterial();
            //newMat.mainTexture.wrapMode = TextureWrapMode.Repeat;
            floor.GetComponent<MeshRenderer>().material = newMat;

            //PUNEET -> Added Mesh Collider to floor
            floor.AddComponent<MeshCollider>();
            floor.GetComponent<MeshCollider>().sharedMesh = floorMesh;
            floor.layer = layer3D;
        }
    }
    private Polygon CreatePoly(Vector2[] points)
    {
        List<PolygonPoint> polyPoints = new();

        for (int i = 0; i < points.Length; i++)
        {
            polyPoints.Add(new PolygonPoint(points[i].x, points[i].y));
            Vector3 pt = new(points[i].x, 0, points[i].y);
            _newVerts.Add(pt);
            _vertexIndices.Add(pt, _newVertCount);
            _newVertCount++;
        }

        Polygon P = new(polyPoints);

        return P;
    }

    public void AddHouseObjects(List<GameObject> houseObjects)
    {
        foreach (GameObject houseObject in houseObjects)
        {
            string category = houseObject.GetComponent<HouseObject>().category;
            GameObject container = Instantiate(Resources.Load("furniture/3D_Models/" + category + "Container")) as GameObject;
            container.transform.position = SwapVectorYZ(houseObject.transform.position);
            container.transform.parent = Container3D.transform;
            Vector3 newYVector = container.transform.position;
            newYVector.y += 1;
            container.transform.position = newYVector;
            container.GetComponent<HouseObject3D>().setModel(houseObject.name);
        }
    }

    public void AddWindows(List<GameObject> windows)
    {
        foreach (GameObject window in windows)
        {

            Hole hole = new()
            {
                Position = window.transform.position / _scaleFactor,
                HoleLength = window.GetComponent<LineAttachableObject>().Width,
                HoleHeight = window.GetComponent<LineAttachableObject>().Height,
                HoleElevation = window.GetComponent<LineAttachableObject>().Elevation
            };
            //Vector3 startNode = SwapVectorYZ(window.GetComponent<LineAttachableObject>().startNode.transform.position);
            //Vector3 endNode = SwapVectorYZ(window.GetComponent<LineAttachableObject>().endNode.transform.position);
            GameObject wall = LiesOn(hole);
            hole.Position = SwapVectorYZ(window.transform.position) / _scaleFactor;

            if (wall != null)
            {
                HoleAddOrUpdate(wall, hole);
            }
            else
                Debug.Log("Not Found");
        }
        if (_wallHoles.Count > 0)
        {
            foreach (KeyValuePair<GameObject, List<Hole>> entry in _wallHoles)
            {
                Debug.Log("Sent holes : " + entry.Value.Count);
                entry.Key.GetComponent<WallFunctions>().AddHoles(entry.Value);
            }
        }
    }
    private GameObject LiesOn(Hole hole)
    {
        Vector3 relativePos = new(hole.Position.x, hole.Position.z, hole.Position.y);

        print("Box pos is  " + relativePos + " " + new Vector3(Thickness, hole.HoleHeight / 2, Thickness));

        Collider[] colliderList = Physics.OverlapBox(relativePos, new Vector3(Thickness, hole.HoleHeight / 2, Thickness), Quaternion.identity, LayerMask3D);
        print("Size of collider list " + colliderList.Length);

        foreach (Collider hit in colliderList)
        {
            print(hit.name.ToLower());

            if (hit.name.ToLower().Contains("wall"))
            {
                print("Hit with Line " + hit.gameObject);
                return hit.gameObject;
            }
        }

        return null;
    }

    private void HoleAddOrUpdate(GameObject wall, Hole hole)
    {
        print("Inside add or update and wall is " + wall + " hole is " + hole);
        print("Line holes dictionairy is size" + _wallHoles.Count);
        if (_wallHoles.ContainsKey(wall))
        {
            List<Hole> l = _wallHoles[wall];
            l.Add(hole);
            _wallHoles[wall] = l;
        }
        else
        {
            List<Hole> l = new()
            {
                hole
            };
            _wallHoles.Add(wall, l);
        }
    }

    private void NodeAddOrUpdate(Vector3 corner, GameObject wall)
    {
        if (_nodes.ContainsKey(corner))
        {
            List<GameObject> l = _nodes[corner];
            l.Add(wall);
            _nodes[corner] = l;
        }
        else
        {
            List<GameObject> l = new()
            {
                wall
            };
            _nodes.Add(corner, l);
        }
    }

    private void AdjustShape(GameObject a, GameObject b, Vector3 point)
    {
        float angle = a.GetComponent<WallFunctions>().Angle - b.GetComponent<WallFunctions>().Angle;
        int baseA, baseB, dupliBaseA, dupliBaseB;

        //_angle adjustments
        if (angle > 180)
        {
            angle = -(angle - 180);
        }

        if (angle < -180)
        {
            angle += 360;
        }
        //Debug.Log (_angle);

        Mesh meshA = a.GetComponent<MeshFilter>().mesh;
        Mesh meshB = b.GetComponent<MeshFilter>().mesh;

        Vector3[] vertsA = meshA.vertices;
        int ovlA = a.GetComponent<WallFunctions>().Ovl;
        Vector3[] vertsB = meshB.vertices;
        int ovlB = b.GetComponent<WallFunctions>().Ovl;

        float ext = (Thickness / 2) / Mathf.Tan(angle * Mathf.Deg2Rad / 2);

        //Debug.Log (ext);
        //if (Mathf.Abs (_angle) > 90)
        //  ext = -ext;

        //Debug.Log (a.name + " " + b.name + " : " + ext);
        bool isStartA = IsStart(a, point);
        bool isStartB = IsStart(b, point);

        if (isStartA)
        {
            baseA = ovlA;
            dupliBaseA = 2 * ovlA + 4;
        }
        else
        {
            baseA = 0;
            dupliBaseA = 2 * ovlA;
        }

        if (!isStartB)
        {
            baseB = ovlB;
            dupliBaseB = 2 * ovlB + 4;
        }
        else
        {
            baseB = 0;
            dupliBaseB = 2 * ovlB;
        }

        //subtract positive z direction vector from close-to-_angle edge of A
        Vector3 extVector = new(0, 0, ext);

        if (isStartA)
        {
            vertsA[baseA + 0] += extVector;
            vertsA[dupliBaseA + 0] += extVector;
            vertsA[dupliBaseA + 19] += extVector;

            vertsA[baseA + 1] += extVector;
            vertsA[dupliBaseA + 1] += extVector;
            vertsA[dupliBaseA + 6] += extVector;
        }
        else
        {
            vertsA[baseA + 2] -= extVector;
            vertsA[dupliBaseA + 7] -= extVector;
            vertsA[dupliBaseA + 12] -= extVector;

            vertsA[baseA + 3] -= extVector;
            vertsA[dupliBaseA + 13] -= extVector;
            vertsA[dupliBaseA + 18] -= extVector;

        }

        if (isStartB)
        {
            vertsB[baseB + 0] += extVector;
            vertsB[dupliBaseB + 0] += extVector;
            vertsB[dupliBaseB + 19] += extVector;

            vertsB[baseB + 1] += extVector;
            vertsB[dupliBaseB + 1] += extVector;
            vertsB[dupliBaseB + 6] += extVector;
        }
        else
        {
            vertsB[baseB + 2] -= extVector;
            vertsB[dupliBaseB + 7] -= extVector;
            vertsB[dupliBaseB + 12] -= extVector;

            vertsB[baseB + 3] -= extVector;
            vertsB[dupliBaseB + 13] -= extVector;
            vertsB[dupliBaseB + 18] -= extVector;
        }

        meshA.vertices = vertsA;
        meshB.vertices = vertsB;
    }
    private bool IsStart(GameObject a, Vector3 point)
    {
        //when coincident edges arent all start edges
        return (a.GetComponent<WallFunctions>().StartPoint == point);
    }
    private Vector3 GetStart(GameObject a)
    {
        return a.GetComponent<WallFunctions>().StartPoint;
    }

    private Vector3 GetEnd(GameObject a)
    {
        return a.GetComponent<WallFunctions>().EndPoint;
    }

    Vector3 SwapVectorYZ(Vector3 vectorToSwap)
    {
        (vectorToSwap.y, vectorToSwap.z) = (vectorToSwap.z, vectorToSwap.y);
        return vectorToSwap;
    }
}