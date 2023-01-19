using UnityEngine;
using System.Collections;

public class HouseObject : MonoBehaviour {

    Transform background;
    public bool isPlacable = true;
    public bool isWallAttachable = false;
    public Color placable, notPlacable;
    protected FloorplanManager wallManager;
    public string category;

    void OnEnable()
    {
        background = transform.GetChild(0);
    }

    // Use this for initialization
    protected virtual void Start () {
        wallManager = GameObject.Find("Floorplan Container").GetComponent<FloorplanManager>();
        print("Line manager is " + wallManager);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    protected virtual void MakeNotPlacable()
    {
        print("Objct is unplacable");
        background.GetComponent<Renderer>().material.color = notPlacable;
        isPlacable = false;
    }

    protected virtual void MakePlacable()
    {
        print("Object is placable");
        background.GetComponent<Renderer>().material.color = placable;
        isPlacable = true;
    }

    protected virtual void PlaceObject()
    {
        //background.GetComponent<Renderer>().material.color = Color.white;
        //GetComponent<BoxCollider>().enabled = true;
    }

    public virtual void init(string name, bool isWallAttachable)
    {
        GetComponent<Renderer>().material.mainTexture = Resources.Load(name) as Texture2D;

        float height = GetComponent<Renderer>().material.mainTexture.height;
        float width = GetComponent<Renderer>().material.mainTexture.width;

        float aspect = width / height; //2
        float multiplier = 2;

        if (aspect > 1)
        {
            multiplier = scaleDown(aspect);
            print("Multiplier is " + multiplier);
            if (multiplier < 0.5f)
            {
                multiplier *= scaleUp(multiplier);
            }
        }

        transform.localScale = new Vector3(multiplier *  aspect, multiplier, 1f);
        transform.name = name;
        this.isWallAttachable = isWallAttachable;
    }

    float scaleDown(float f)
    {
        return 1/f;
    }

    float scaleUp(float f)
    {
        return 0.5f/f;
    }
}
