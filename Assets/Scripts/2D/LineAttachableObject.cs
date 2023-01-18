using UnityEngine;
using System.Collections;

public class LineAttachableObject : HouseObject
{
    public GameObject startNode, endNode;
    public float length;
    public float height;
    public float elevation;
    private LayerMask layerMask;

    // Use this for initialization
    protected override void Start()
    {
        layerMask = LayerMask.GetMask("Floorplan");
        isWallAttachable = true;
        base.Start();
    }

    public override void init(string name, bool isWallAttachable)
    {
        base.init(name, isWallAttachable);

        if (name.Contains("window"))
        {
            length = 1f;
            height = 1f;
            elevation = 1.15f;
        }
        else if (name.Contains("door"))
        {
            length = 1f;
            height = 1.8f;
            elevation = height * 0.5f + 0.001f;
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    protected override void MakePlacable()
    {
        base.MakePlacable();
        if (isWallAttachable)
        {
            RaycastHit[] hitList = Physics.BoxCastAll(GetComponent<Renderer>().bounds.center, GetComponent<Renderer>().bounds.extents * 1.1f, Vector3.forward, transform.rotation, float.PositiveInfinity, layerMask);
          
            if (hitList.Length > 0)
            {
                for (int i = 0; i < hitList.Length; i++)
                {
                    print("Hit with object " + hitList[i].transform.name);

                    if (hitList[i].transform.name.Contains("Line"))
                    {
                        print(hitList);
                        adjustPosition(hitList[i].transform);
                        break;
                    }
                    else
                    {
                        MakeNotPlacable();
                    }
                }
            }
        }
    }

    protected override void PlaceObject()
    {

        RaycastHit[] hitList = Physics.BoxCastAll(GetComponent<Renderer>().bounds.center, GetComponent<Renderer>().bounds.extents * 1.1f, Vector3.forward, transform.rotation, float.PositiveInfinity, layerMask);
        int firstWallPos = hitList.Length;
        if (hitList.Length > 0)
        {
            for (int i = 0; i < hitList.Length; i++)
            {
                if (!hitList[i].transform.name.Contains("Line"))
                {
                    Destroy(gameObject);
                }
                else if (i < firstWallPos)
                {
                    firstWallPos = i;
                }
            }
            adjustPosition(hitList[firstWallPos].transform);
        }
        print(wallManager + " is wall manager");
        wallManager.windowList.Add(gameObject);
        gameObject.name += (wallManager.windowList.Count - 1);
        base.PlaceObject();
    }

    public void adjustPosition(Transform overlap)
    {
        Vector p1 = new Vector(overlap.GetComponent<Line>().startNode.transform.position.x, overlap.GetComponent<Line>().startNode.transform.position.y);
        Vector p2 = new Vector(overlap.GetComponent<Line>().endNode.transform.position.x, overlap.GetComponent<Line>().endNode.transform.position.y);

        Vector q1 = new Vector(-20, transform.position.y);
        Vector q2 = new Vector(20, transform.position.y);

        Transform startNode = overlap.GetComponent<Line>().startNode.transform;
        Transform endNode = overlap.GetComponent<Line>().endNode.transform;

        if (overlap.transform.rotation.eulerAngles.z < 1 && overlap.transform.rotation.eulerAngles.z > -1)
        {
            q1 = new Vector(transform.position.x, -20);
            q2 = new Vector(transform.position.x, 20);
        }
        else if (overlap.transform.rotation.eulerAngles.z == 180)
        {
            q1 = new Vector(transform.position.x, -20);
            q2 = new Vector(transform.position.x, 20);
        }

        Vector intersectionPoint;
        if (LineSegementsIntersect(p1, p2, q1, q2, out intersectionPoint, true))
        {
            if (!double.IsNaN(intersectionPoint.X) && !double.IsNaN(intersectionPoint.Y))
            {
                transform.position = new Vector3((float)intersectionPoint.X, (float)intersectionPoint.Y, transform.position.z);
                transform.rotation = overlap.rotation;
            }
        }
    }

}