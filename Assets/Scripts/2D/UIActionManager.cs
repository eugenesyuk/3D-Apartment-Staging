using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIActionManager : MonoBehaviour
{
    public GameObject _3DRoot, _2DRoot, viewerCamera, isoCamera;
    public Transform _3DCanvas, _2DCanvas;
    public FloorplanManager Floorplan;
    public WallGenerator WallGenerator;
    public Button ClearButton, View3DButton;

    private void Awake()
    {
        ClearButton.interactable = false;
        View3DButton.interactable = false;

        Reset3DRootToStart();
        Reset2DRootToStart();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && viewerCamera.activeSelf)
        {
            ActivateIsoCamera();
        }

        if(Floorplan.DidDraw)
        {
            ClearButton.interactable = true;
            View3DButton.interactable = true;
        } else
        {
            ClearButton.interactable = false;
            View3DButton.interactable = false;
        }
    }

    public void Clicked3DView()
    {
        List<GameObject> lineList = Floorplan.ExportLines();
        List<GameObject> windowList = Floorplan.ExportWindows();
        List<GameObject> objectList = Floorplan.ExportObjects();

        _2DRoot.SetActive(false);
        _3DRoot.SetActive(true);

        WallGenerator.Generate3D(lineList, windowList, objectList);

        ActivateIsoCamera();
        ToggleUIMode();
    }

    public void Clicked2DView()
    {
        _2DRoot.SetActive(true);
        _3DRoot.SetActive(false);

        WallGenerator.Destroy3D();

        ToggleUIMode();
    }

    public void ClickedFirstPersonView()
    {
        Toggle3DMode();
    }

    public void ClickedClear()
    {
        Floorplan.Refresh();
    }
    private void Reset3DRootToStart()
    {
        _3DRoot.SetActive(false);
        _3DCanvas.gameObject.SetActive(false);
    }

    private void Reset2DRootToStart()
    {
        _2DRoot.SetActive(true);
        _2DCanvas.gameObject.SetActive(true);
    }

    void ActivateIsoCamera()
    {
        isoCamera.SetActive(true);
        viewerCamera.SetActive(false);
    }
    void ActivateViewerCamera()
    {
        viewerCamera.SetActive(true);
        isoCamera.SetActive(false);
    }

    void Toggle3DMode()
    {
        if (viewerCamera.activeSelf)
        {
            ActivateIsoCamera();
        }
        else
        {
            ActivateViewerCamera();
        }
    }
    void ToggleUIMode()
    {
        bool toogle2D = !_2DCanvas.gameObject.activeInHierarchy;

        _2DCanvas.gameObject.SetActive(toogle2D);
        _3DCanvas.gameObject.SetActive(!toogle2D);
    }
}
