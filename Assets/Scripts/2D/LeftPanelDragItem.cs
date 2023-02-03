using UnityEngine;
using UnityEngine.EventSystems;

public class LeftPanelDragItem : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public GameObject houseObject, attachableHouseObject;
    private Transform houseObjectContainer, attachableObjectContainer;

    GameObject dragObject = null, floorPlanContainer;

    public bool _isDragging = false;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        floorPlanContainer = GameObject.Find("Floorplan Container");
        houseObjectContainer = GameObject.Find("HouseObjectContainer").transform;
        attachableObjectContainer = GameObject.Find("AttachableObjectContainer").transform;

        _isDragging = true;

        if(this.transform.name == "Window Button")
        {
            this.dragObject = GameObject.Instantiate(attachableHouseObject);
            this.dragObject.GetComponent<HouseObject>().Init("window", true);
            this.dragObject.transform.parent = attachableObjectContainer;
        } else if (this.transform.name == "Door Button")
        {
            this.dragObject = GameObject.Instantiate(attachableHouseObject);
            this.dragObject.GetComponent<HouseObject>().Init("door", true);
            this.dragObject.transform.parent = attachableObjectContainer;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        HandleDrag();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;

        if (dragObject != null)
        {
            if (!dragObject.GetComponent<HouseObject>().isPlacable)
            {
                CancelDrag();
            }
            else
            {
                dragObject.transform.position = new Vector3(dragObject.transform.position.x, dragObject.transform.position.y, -10);

                if (!floorPlanContainer.GetComponent<BoxCollider>().bounds.Intersects(dragObject.GetComponent<Renderer>().bounds))
                {
                    CancelDrag();
                }
                else
                {
                    dragObject.transform.position = new Vector3(dragObject.transform.position.x, dragObject.transform.position.y, 0);
                    dragObject.SendMessage("PlaceObject");
                }
            }
        }
    }
    void HandleDrag()
    {
        if (this._isDragging && dragObject != null)
        {
            Vector3 mousePosition = Utils.GetCurrentMousePosition();
            dragObject.transform.position = mousePosition;

            RaycastHit[] hitList = Physics.BoxCastAll(mousePosition,
                dragObject.GetComponent<Renderer>().bounds.extents * 1.1f,
                Vector3.forward,
                transform.rotation,
                float.PositiveInfinity,
                LayerMask.GetMask("Floorplan"));

            Debug.Log(hitList.Length);

            if (hitList.Length > 0)
            {
                if (!dragObject.GetComponent<HouseObject>().isWallAttachable)
                {
                    dragObject.SendMessage("MakeNotPlacable");
                }
                else
                {
                    dragObject.SendMessage("MakePlacable");
                }

                //for (int i = 0; i < hitList.Length; i++)
                //{
                //    print("Hit with object " + hitList[i].transform.name);
                //}
            }
            else
            {
                if (!dragObject.GetComponent<HouseObject>().isWallAttachable)
                {
                    dragObject.SendMessage("MakePlacable");
                }
                else
                {
                    dragObject.SendMessage("MakeNotPlacable");
                }
            }
        }
    }

    void CancelDrag()
    {
        GameObject.Destroy(dragObject);
        dragObject = null;
    }

    void Update()
    {
        if (this._isDragging)
        {
            if (Input.GetKeyDown(KeyCode.R) && dragObject != null)
            {
                print("Hit key R" + dragObject);
                dragObject.transform.Rotate(Vector3.forward, 90f);
            }
        }

        if(Input.GetMouseButtonDown(1))
        {
            onMouseRightClick();
        }
    }

    void onMouseRightClick()
    {
        if (_isDragging && dragObject != null)
        {
             Destroy(dragObject);
        }
    }
}
