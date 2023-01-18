using UnityEngine;
using System.Collections.Generic;
using Poly2Tri;

public class WallFunctions : MonoBehaviour
{

    public float Thickness = Globals.WallParams.Thickness;
    public float Height = Globals.WallParams.Height;
    public float Tilingfactor = 1;

    public List<Hole> HoleList = new();

    private int _holeStartIndex = 4;
    private int _holeEndIndex = 8;

    public float _holeLength = 1;
    public float _holeHeight = 1;
    public float _holeElevation = 1.5f;

    private Vector3 _startPoint;
    private Vector3 _endPoint;

    private float _angle = 0;
    private float _length = 0;

    private readonly Dictionary<Vector3, int> _vertexIndices = new();
    private Polygon _latestFace = null;

    private readonly List<int> _newTris = new();
    private readonly List<Vector3> _newVerts = new();
    private int _newVertCount = 0;

    private int ovl;

    public int Ovl
    {
        get
        {
            return this.ovl;
        }
        set
        {
            ovl = value;
        }
    }

    public float Angle
    {
        get
        {
            return this._angle;
        }
        set
        {
            _angle = value;
        }
    }

    public Vector3 StartPoint
    {
        get
        {
            return this._startPoint;
        }
        set
        {
            _startPoint = value;
        }
    }

    public Vector3 EndPoint
    {
        get
        {
            return this._endPoint;
        }
        set
        {
            _endPoint = value;
        }
    }
    public void GenerateWall(Vector3 start, Vector3 end)
    {

        StartPoint = start;
        EndPoint = end;
        // get _angle between direction vector and x axis
        Vector3 wallVector = end - start;

        float _angle = Vector3.Angle(wallVector, new Vector3(1, 0, 0));
        Vector3 _cross = Vector3.Cross(wallVector, new Vector3(1, 0, 0));
        if (_cross.y > 0) _angle = -_angle;

        // get wall _length
        _length = wallVector.magnitude;

        //create mesh
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();

        Mesh mesh = this.GetComponent<MeshFilter>().mesh;
        List<Vector3> verts = new()
        {
            new Vector3(-Thickness / 2, 0f, 0f),
            new Vector3(-Thickness / 2, Height, 0f),
            new Vector3(-Thickness / 2, Height, _length),
            new Vector3(-Thickness / 2, 0f, _length)
        };

        mesh.vertices = verts.ToArray(); ;
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };


        //extrude mesh
        mesh = this.ExtrudeWall(mesh);
        mesh.RecalculateNormals();

        //adjust wall parameters


        //assign material
        Material newMat = new(Shader.Find("Unlit/Texture"))
        {
            color = Color.white,

            //apply texture
            mainTexture = (Texture2D)Resources.Load("textures/probuilder")
        };//GetDefaultMaterial();
        newMat.mainTexture.wrapMode = TextureWrapMode.Repeat;

        //Resources.Load("meshgen material", typeof(Material)) as Material;
        //newMat.EnableKeyword("_EMISSION");
        //newMat.SetColor ("_EmissionColor", Color.white);
        MeshRenderer renderer = this.GetComponent<MeshRenderer>();
        renderer.material = newMat;

        //transform mesh to position and _angle
        this.transform.RotateAround(new Vector3(0, 0, 0), new Vector3(0, 1, 0), 90 + _angle);
        this.transform.position = start;

        //PUNEET -> Added Meshcollider to wall
        gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        //gameObject.AddMissingComponent<MeshCollider>().sharedMesh = mesh;
    }
    public void AddHoles(List<Hole> holes)
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        List<Vector2> rectanglePoints = new()
        {
            new Vector2(0, 0),
            new Vector2(0, Height),
            new Vector2(_length, Height),
            new Vector2(_length, 0)
        };

        _latestFace = CreatePoly(rectanglePoints.ToArray());

        for (int l = 0; l < holes.Count; l++)
            Debug.Log(holes[l].Position);

        for (int k = 0; k < holes.Count; k++)
        {
            float distance = (holes[k].Position - this.StartPoint).magnitude;
            _holeLength = holes[k].HoleLength;
            _holeHeight = holes[k].HoleHeight;
            _holeElevation = holes[k].HoleElevation;
            //distance = 1;

            if (distance > (_length - _holeLength / 2) || distance < _holeLength / 2)
            {
                Debug.Log("Hole exceeds " + this.name + " by " + distance);
            }
            else
            {
                List<Vector2> holePoints = new()
                {
                    new Vector2(distance - _holeLength / 2, _holeElevation - _holeHeight / 2),
                    new Vector2(distance - _holeLength / 2, _holeElevation + _holeHeight / 2),
                    new Vector2(distance + _holeLength / 2, _holeElevation + _holeHeight / 2),
                    new Vector2(distance + _holeLength / 2, _holeElevation - _holeHeight / 2)
                };


                Polygon Hole = CreatePoly(holePoints.ToArray());
                Debug.Log("Created hole");


                _latestFace.AddHole(Hole);

                holes[k].HoleStartIndex = (k - 1) >= 0 ? holes[k - 1].HoleEndIndex : 4;
                //Debug.Log ("here");
                HoleList.Add(holes[k]);
            }


        } // holes loop end
        print(_latestFace);
        P2T.Triangulate(_latestFace);
        for (int i = 0; i < _latestFace.Triangles.Count; i++)
            for (int j = 0; j < 3; j++)
            {
                TriangulationPoint tpt = _latestFace.Triangles[i].Points[j];
                Vector3 pt = new(-Thickness / 2, (float)tpt.Y, (float)tpt.X);
                _newTris.Add(_vertexIndices[pt]);
            }
        mesh.Clear();
        mesh.vertices = _newVerts.ToArray();
        mesh.triangles = _newTris.ToArray();

        mesh = this.ExtrudeWall(mesh);
        mesh.RecalculateNormals();
        //PUNEET -> ReAdded Meshcollider to wall in the case of hole
        gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        //gameObject.AddMissingComponent<MeshCollider>().sharedMesh = mesh;
        //change uvs according to hole
    }

    private Polygon CreatePoly(Vector2[] points)
    {
        List<PolygonPoint> polyPoints = new();
        for (int i = 0; i < points.Length; i++)
        {
            polyPoints.Add(new PolygonPoint(points[i].x, points[i].y));
            Vector3 pt = new(-Thickness / 2, points[i].y, points[i].x);
            _newVerts.Add(pt);
            _vertexIndices.Add(pt, _newVertCount);
            _newVertCount++;
        }
        Polygon P = new(polyPoints);
        return P;
    }

    private Mesh ExtrudeWall(Mesh mesh)
    {
        //duplicate vertices
        Vector3[] orignalVertices = mesh.vertices;
        Vector3[] backVertices = new Vector3[orignalVertices.Length];
        Vector3[] midVertices = new Vector3[4];

        Vector3 thicknessVector = new(Thickness, 0, 0);

        for (int i = 0; i < orignalVertices.Length; i++)
            backVertices[i] = orignalVertices[i] + thicknessVector;

        Vector3 midVector = new(Thickness / 2, 0, 0);
        for (int i = 0; i < midVertices.Length; i++)
            midVertices[i] = orignalVertices[i] + midVector;

        //combine arrays 
        Vector3[] firstVertices = new Vector3[orignalVertices.Length + backVertices.Length];
        orignalVertices.CopyTo(firstVertices, 0);
        backVertices.CopyTo(firstVertices, orignalVertices.Length);

        int ovl = orignalVertices.Length;
        this.ovl = ovl;
        int last = firstVertices.Length;

        //generate inverted back triangles
        int[] orignalTriangles 
            = mesh.triangles;

        int[] backTriangles = new int[orignalTriangles.Length];

        for (int i = 0; i < orignalTriangles.Length; i += 3)
        {
            backTriangles[i] = orignalTriangles[i] + ovl;
            backTriangles[i + 1] = orignalTriangles[i + 2] + ovl;
            backTriangles[i + 2] = orignalTriangles[i + 1] + ovl;
        }
        //hole triangles

        // triangles and vertices for hex faces
        int[] hexTriangles = new int[8 * 2 * 3]; // 8 quads with 2 triangles with 3 values each
        int count = 0;

        Vector3[] hexVertices = new Vector3[6 * 4];

        for (int i = 0; i < 4; i++)
        {

            int first = i;
            int second = (i + 1) % 4;

            Vector3[] verticesNew = new Vector3[6];

            verticesNew[0] = orignalVertices[first];
            verticesNew[1] = orignalVertices[second];
            verticesNew[2] = midVertices[first];
            verticesNew[3] = midVertices[second];
            verticesNew[4] = backVertices[first];
            verticesNew[5] = backVertices[second];
            verticesNew.CopyTo(hexVertices, count);

            int[] quadTri = QuadTriangles(last + count + 1, last + count + 0, last + count + 2, last + count + 3);
            quadTri.CopyTo(hexTriangles, 2 * 2 * 3 * i);

            quadTri = QuadTriangles(last + count + 4, last + count + 5, last + count + 3, last + count + 2);
            quadTri.CopyTo(hexTriangles, 2 * 2 * 3 * i + 2 * 3);
            count += 6;
        }

        last = firstVertices.Length + hexVertices.Length;

        int[] holeTriangles = new int[HoleList.Count * 4 * 2 * 3]; // two triangles for each edge with 3 values each
        Vector3[] holeVertices = new Vector3[HoleList.Count * 4 * 4]; // 4 new points for each edge

        // triangles and vertices for Hole faces
        for (int k = 0; k < HoleList.Count; k++)
        {
            Debug.Log("Hole " + k);
            count = 0;

            _holeStartIndex = HoleList[k].HoleStartIndex;
            _holeEndIndex = HoleList[k].HoleEndIndex;

            Debug.Log(_holeStartIndex);

            for (int i = _holeStartIndex; i < _holeEndIndex; i++)
            {
                int first = i;
                int second = _holeStartIndex + (((i - _holeStartIndex) + 1) % (_holeEndIndex - _holeStartIndex));

                Vector3[] verticesHoleNew = new Vector3[4];
                verticesHoleNew[0] = orignalVertices[first];
                verticesHoleNew[1] = orignalVertices[second];
                verticesHoleNew[2] = backVertices[first];
                verticesHoleNew[3] = backVertices[second];

                Debug.Log("Wound " + verticesHoleNew[0]);

                verticesHoleNew.CopyTo(holeVertices, k * 16 + count);

                int[] quadTri = QuadTriangles(last + count + 0, last + count + 1, last + count + 3, last + count + 2);
                quadTri.CopyTo(holeTriangles, (6 * 4 * k) + (6 * (i - _holeStartIndex)));

                for (int m = 0; m < quadTri.Length; m++)
                {
                    Debug.Log(quadTri[m]);
                }

                count += 4;
            }

            last += 16;
        }

        for (int m = 0; m < holeVertices.Length; m++)
        {
            Debug.Log(holeVertices[m]);
        }

        Vector3[] vertices = new Vector3[firstVertices.Length + hexVertices.Length + holeVertices.Length];
        firstVertices.CopyTo(vertices, 0);

        hexVertices.CopyTo(vertices, firstVertices.Length);
        holeVertices.CopyTo(vertices, firstVertices.Length + hexVertices.Length);


        //combine triangles
        int[] triangles = new int[orignalTriangles.Length + backTriangles.Length + hexTriangles.Length + holeTriangles.Length];
        orignalTriangles.CopyTo(triangles, 0);

        backTriangles.CopyTo(triangles, orignalTriangles.Length);
        hexTriangles.CopyTo(triangles, orignalTriangles.Length + backTriangles.Length);
        holeTriangles.CopyTo(triangles, orignalTriangles.Length + backTriangles.Length + hexTriangles.Length);

        //add to mesh and return
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        //assign UVs
        AssignUV();

        return mesh;
    }

    int[] QuadTriangles(int a, int b, int c, int d)
    {
        int[] triangles = { a, c, b, a, d, c };
        return triangles;
    }

    private void AssignUV()
    {
        Mesh m = GetComponent<MeshFilter>().mesh;
        Vector3[] verts = m.vertices;

        int ovl = GetComponent<WallFunctions>().Ovl;
        Vector2[] uvs = new Vector2[verts.Length];

        //faces ---- 0 to 2 * OVL
        for (int i = 0; i < 2 * ovl; i++)
        {
            uvs[i] = new Vector2(verts[i].z / Tilingfactor, verts[i].y / Tilingfactor);
        }

        // loop for hexes ---- 2 * OVL to hole start 

        for (int i = 2 * ovl; i < (2 * ovl + 6 * 4); i += 6)
        {
            //base points 
            uvs[i] = new Vector2(verts[i].z / Tilingfactor, verts[i].y / Tilingfactor);
            uvs[i + 1] = new Vector2(verts[i + 1].z / Tilingfactor, verts[i + 1].y / Tilingfactor);

            //mid and end
            if (verts[i].y == verts[i + 1].y)
                for (int k = 2; k < 6; k++)
                    uvs[i + k] = new Vector2(verts[i + k].z / Tilingfactor, (verts[i + k].y + verts[i + k].x - verts[i + k % 2].x) / Tilingfactor);
            else
                for (int k = 2; k < 6; k++)
                    uvs[i + k] = new Vector2((verts[i + k].z + verts[i + k].x - verts[i + k % 2].x) / Tilingfactor, verts[i + k].y / Tilingfactor);
        }

        // Loop for holes --- hole start to end

        for (int i = (2 * ovl + 6 * 4); i < verts.Length; i += 4)
        {
            //base points 
            uvs[i] = new Vector2(verts[i].z / Tilingfactor, verts[i].y / Tilingfactor);
            uvs[i + 1] = new Vector2(verts[i + 1].z / Tilingfactor, verts[i + 1].y / Tilingfactor);

            //mid and end
            if (verts[i].y == verts[i + 1].y)
                for (int k = 2; k < 4; k++)
                    uvs[i + k] = new Vector2(verts[i + k].z / Tilingfactor, (verts[i + k].y + verts[i + k].x - verts[i + k % 2].x) / Tilingfactor);
            else
                for (int k = 2; k < 4; k++)
                    uvs[i + k] = new Vector2((verts[i + k].z + verts[i + k].x - verts[i + k % 2].x) / Tilingfactor, verts[i + k].y / Tilingfactor);
        }

        m.uv = uvs;
    }
}