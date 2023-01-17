using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIActionManager : MonoBehaviour
{
    public GameObject _3DRoot, _2DRoot, viewerCamera, isoCamera;
    public Transform _3DCanvas, _2DCanvas;
    public FloorplanManager wallManager;
    public WallGenerator wallGenerator;

    [SerializeField]
    public Button clearButton, view3DButton;

    private void Awake()
    {
        clearButton.interactable = false;
        view3DButton.interactable = false;

        Reset3DRootToStart();
        Reset2DRootToStart();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && viewerCamera.activeSelf)
        {
            ActivateIsoCamera();
        }

        if(wallManager.didDraw)
        {
            clearButton.interactable = true;
            view3DButton.interactable = true;
        } else
        {
            clearButton.interactable = false;
            view3DButton.interactable = false;
        }
    }

    public void Clicked3DView()
    {
        List<GameObject> nodeList = wallManager.exportNodes();
        List<GameObject> windowList = wallManager.exportWindows();
        List<GameObject> objectList = wallManager.exportObjects();

        _2DRoot.SetActive(false);
        _3DRoot.SetActive(true);

        wallGenerator.generate3D(nodeList, windowList, objectList);

        ActivateIsoCamera();
        ToggleUIMode();
    }

    public void Clicked2DView()
    {
        _2DRoot.SetActive(true);
        _3DRoot.SetActive(false);

        wallGenerator.destroy3D();

        ToggleUIMode();
    }

    public void ClickedFirstPersonView()
    {
        Toggle3DMode();
    }

    public void ClickedClear()
    {
        wallManager.Refresh();
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
