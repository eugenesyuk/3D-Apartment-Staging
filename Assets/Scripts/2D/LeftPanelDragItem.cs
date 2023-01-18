using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LeftPanelDragItem : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public GameObject houseObject, attachableHouseObject;
    private Transform houseObjectContainer, attachableObjectContainer;

    GameObject realWorldItem = null, floorPlanContainer;
    bool isDragging = false;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("OnPointerDown");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("OnBeginDrag");

        floorPlanContainer = GameObject.Find("Floorplan Container");
        houseObjectContainer = GameObject.Find("HouseObjectContainer").transform;
        attachableObjectContainer = GameObject.Find("AttachableObjectContainer").transform;

        isDragging = true;

        if(this.transform.name == "Window Button")
        {
            this.realWorldItem = GameObject.Instantiate(attachableHouseObject);
            this.realWorldItem.GetComponent<HouseObject>().init("window", true);
            this.realWorldItem.transform.parent = attachableObjectContainer;
        } else if (this.transform.name == "Door Button")
        {
            this.realWorldItem = GameObject.Instantiate(attachableHouseObject);
            this.realWorldItem.GetComponent<HouseObject>().init("door", true);
            this.realWorldItem.transform.parent = attachableObjectContainer;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        if (realWorldItem != null)
        {
            if (!realWorldItem.GetComponent<HouseObject>().isPlacable)
            {
                GameObject.Destroy(realWorldItem);
            }
            else
            {
                realWorldItem.transform.position = new Vector3(realWorldItem.transform.position.x, realWorldItem.transform.position.y, -10);

                if (!floorPlanContainer.GetComponent<BoxCollider>().bounds.Intersects(realWorldItem.GetComponent<Renderer>().bounds))
                {
                    GameObject.Destroy(realWorldItem);
                }
                else
                {
                    realWorldItem.transform.position = new Vector3(realWorldItem.transform.position.x, realWorldItem.transform.position.y, 0);
                    realWorldItem.SendMessage("PlaceObject");
                }
            }
        }
        Debug.Log("OnEndDrag");
    }

    public void OnDrag(PointerEventData eventData)
    {
        HandleDrag();
        //rectTransform.anchoredPosition += eventData.delta / canvas._scaleFactor;
    }

    void HandleDrag()
    {
        if (this.isDragging && realWorldItem != null)
        {
            print(realWorldItem);
            Vector3 mousePosition = Utils.GetCurrentMousePosition(Input.mousePosition).GetValueOrDefault();

            realWorldItem.transform.position = mousePosition;
            RaycastHit[] hitList = Physics.BoxCastAll(mousePosition,
                realWorldItem.GetComponent<Renderer>().bounds.extents * 1.1f,
                Vector3.forward,
                transform.rotation,
                float.PositiveInfinity,
                LayerMask.GetMask("Floorplan"));

            Debug.Log(hitList.Length);

            if (hitList.Length > 0)
            {
                if (!realWorldItem.GetComponent<HouseObject>().isWallAttachable)
                {
                    realWorldItem.SendMessage("MakeNotPlacable");
                }
                else
                {
                    realWorldItem.SendMessage("MakePlacable");
                }

                //for (int i = 0; i < hitList.Length; i++)
                //{
                //    print("Hit with object " + hitList[i].transform.name);
                //}
            }
            else
            {
                if (!realWorldItem.GetComponent<HouseObject>().isWallAttachable)
                {
                    realWorldItem.SendMessage("MakePlacable");
                }
                else
                {
                    realWorldItem.SendMessage("MakeNotPlacable");
                }
            }
        }
    }

    void Update()
    {
        if (this.isDragging)
        {
            if (Input.GetKeyDown(KeyCode.R) && realWorldItem != null)
            {
                print("Hit key R" + realWorldItem);
                realWorldItem.transform.Rotate(Vector3.forward, 90f);
            }
        }
        DetectRightClick();
    }
    void DetectRightClick()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (realWorldItem != null)
            {
                Destroy(realWorldItem);
            }
        }
    }
}
