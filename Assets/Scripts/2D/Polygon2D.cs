using System.Collections.Generic;
using UnityEngine;

public class Polygon2D : MonoBehaviour
{
    List<GameObject> NodeList;

    PolygonCollider2D polygonCollider2D;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    public static GameObject Instantiate(List<GameObject> nodeList, GameObject prefab, Transform parent)
    {
        GameObject go = Instantiate(prefab);
        go.transform.parent = parent;
        go.layer = Globals.Layers.Floorplan;
        var polygon = go.GetComponent<Polygon2D>();
        polygon.NodeList = nodeList;

        return go;
    }

    void Start()
    {
        polygonCollider2D = gameObject.GetComponent<PolygonCollider2D>();
        meshFilter = gameObject.GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        SetColliderPoints();
        CreateMesh();
    }
    private void SetColliderPoints()
    {
        Vector2[] points = new Vector2[NodeList.Count];

        for (int i = 0; i < points.Length; i++)
        {
            points[i] = NodeList[i].transform.position;
        }

        polygonCollider2D.points = points;
    }

    void CreateMesh()
    {
        int pointCount = polygonCollider2D.GetTotalPointCount();
        Mesh mesh = new Mesh();

        Vector2[] points = polygonCollider2D.points;
        Vector3[] vertices = new Vector3[pointCount];
        Vector2[] uv = new Vector2[pointCount];

        for (int j = 0; j < pointCount; j++)
        {
            Vector2 actual = points[j];
            vertices[j] = new Vector3(actual.x, actual.y, 0);
            uv[j] = actual;
        }

        Triangulator tr = new Triangulator(points);
        int[] triangles = tr.Triangulate();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        meshFilter.mesh = mesh;
    }
}