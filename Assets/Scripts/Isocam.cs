using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEditor.Experimental.GraphView.GraphView;

public class Isocam : MonoBehaviour
{
    public Vector2 turn;
    public float sensitivity = 5F;

    public GameObject UITopPanel;

    // Start is called before the first frame update
    void Start()
    {   
        // Set initial values to match current rotation
        turn.x = transform.localEulerAngles.y;
        turn.y = -1 * transform.localEulerAngles.x;        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0) && !IsPointerOverElement(UITopPanel))
        {
            Cursor.lockState = CursorLockMode.Locked;

            turn.x += Input.GetAxis("Mouse X") * sensitivity;
            turn.y += Input.GetAxis("Mouse Y") * sensitivity;

            transform.localRotation = Quaternion.Euler(-turn.y, turn.x, 0);


        }

        if (Input.GetMouseButtonUp(0))
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    //Returns 'true' if we touched or hovering on Unity UI element.
    private bool IsPointerOverElement(GameObject targetElement)
    {
        List<RaycastResult> eventSystemRaysastResults = GetEventSystemRaycastResults();

        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults[index];

            Debug.Log(curRaysastResult.gameObject.layer);
            Debug.Log(curRaysastResult.gameObject);
            Debug.Log(curRaysastResult.gameObject.name);
            Debug.Log(curRaysastResult.gameObject.tag);

            if (curRaysastResult.gameObject.layer == targetElement.layer || curRaysastResult.gameObject == targetElement)
            {
                return true;
            }
        }

        return false;
    }

    static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }
}
