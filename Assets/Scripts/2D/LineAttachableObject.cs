﻿using UnityEngine;
using System.Collections;

public class LineAttachableObject : HouseObject
{
    public GameObject startNode, endNode;

    public float Width;
    public float Height;
    public float Elevation;

    private LayerMask layerMask;

    // Use this for initialization
    protected override void Start()
    {
        layerMask = LayerMask.GetMask("Floorplan");
        isWallAttachable = true;
        base.Start();
    }

    public override void Init(string name, bool isWallAttachable)
    {
        if (name.Contains("window"))
        {
            Width = Globals.Window.Width;
            Height = Globals.Window.Height;
            Elevation = Globals.WallParams.Height / 2;
        }

        else if (name.Contains("door"))
        {
            Width = Globals.Door.Width;
            Height = Globals.Door.Height;
            Elevation = Height * 0.5f + 0.001f;
        }

        base.Init(name, isWallAttachable);

        transform.localScale = new Vector3(Width * Globals.ScaleFactor, .7f, 0);
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
                        AdjustPosition(hitList[i].transform);
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
            AdjustPosition(hitList[firstWallPos].transform);
        }

        wallManager.WindowList.Add(gameObject);
        gameObject.name += (wallManager.WindowList.Count - 1);
        base.PlaceObject();
    }

    public void AdjustPosition(Transform overlap)
    {
        Vector p1 = new(overlap.GetComponent<Line>().startNode.transform.position.x, overlap.GetComponent<Line>().startNode.transform.position.y);
        Vector p2 = new(overlap.GetComponent<Line>().endNode.transform.position.x, overlap.GetComponent<Line>().endNode.transform.position.y);

        Vector q1 = new(-20, transform.position.y);
        Vector q2 = new(20, transform.position.y);

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

        if (Utils.LineSegementsIntersect(p1, p2, q1, q2, out Vector intersectionPoint, true))
        {
            if (!double.IsNaN(intersectionPoint.X) && !double.IsNaN(intersectionPoint.Y))
            {
                transform.position = new Vector3((float)intersectionPoint.X, (float)intersectionPoint.Y, transform.position.z);
                transform.rotation = overlap.rotation;
            }
        }
    }

}