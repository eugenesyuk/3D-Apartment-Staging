
using UnityEngine;
public class Hole
{
    private int holeStartIndex;
    private int holeEndIndex;

    private float holeLength = 1;
    private float holeHeight = 1;
    private float holeElevation = 1;

    private Vector3 position;
    public int HoleEndIndex
    {
        get
        {
            return this.holeEndIndex;
        }
        set
        {
            holeEndIndex = value;
        }
    }
    public int HoleStartIndex
    {
        get
        {
            return this.holeStartIndex;
        }
        set
        {
            holeStartIndex = value;
            holeEndIndex = value + 4;
        }
    }

    public float HoleLength
    {
        get
        {
            return this.holeLength;
        }
        set
        {
            holeLength = value;
        }
    }

    public float HoleHeight
    {
        get
        {
            return this.holeHeight;
        }
        set
        {
            holeHeight = value;
        }
    }
    public float HoleElevation
    {
        get
        {
            return this.holeElevation;
        }
        set
        {
            holeElevation = value;
        }
    }
    public Vector3 Position
    {
        get
        {
            return this.position;
        }
        set
        {
            position = value;
        }
    }
}