using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIActionManager : MonoBehaviour
{
    [SerializeField]
    GameObject _3DRoot, _2DRoot, viewerCamera, isoCamera;

    [SerializeField]
    Transform _3DCanvas, _2DCanvas;

    [SerializeField]
    FloorplanManager Floorplan;

    [SerializeField]
    WallGenerator WallGenerator;

    [SerializeField] 
    Button ClearButton, View3DButton, DeleteNode, DeleteLine, ResizeLine, AddNode, ApplyResize;

    [SerializeField]
    GameObject NodeActionsPanel, LineActionsPanel, ResizePanel;

    [SerializeField]
    TMP_InputField MetersInput, CentimetersInput;

    [SerializeField]  
    RectTransform _2DCanvasRt;
  

    private void Awake()
    {
        ClearButton.interactable = false;
        View3DButton.interactable = false;

        HideNodePanel();
        HideLinePanel();
        HideResizePanel();

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

    public void ClickedRemoveNode()
    {
        Floorplan.RemoveSelectedNode();
    }

    public void ClickedDrawLine()
    {
        Floorplan.DrawLineFromNode();
    }

    public void ClickedRemoveLine()
    {
        Floorplan.RemoveSelectedLine();
    }

    public void ClickedOpenResize()
    {
        float lineLength = Floorplan.GetSelectedLineLength();
        int meters = (int)lineLength;
        int centimeters = (int)Math.Round((lineLength * 100) % 100);

        SetResizeDefaults(meters, centimeters);
        ShowResizePanel();
    }

    private void SetResizeDefaults(int meters, int centimeters)
    {
        MetersInput.text = meters.ToString();
        CentimetersInput.text = centimeters.ToString();
    }

    public void CentimetersChanged(string value)
    {
    }

    public void MetersChanged(String value)
    {

    }

    public void ClickedResizeLine()
    {
        string meters = MetersInput.text;
        string centimeters = CentimetersInput.text;
        float length = float.Parse($"{meters},{centimeters}");
        Floorplan.ResizeSelectedLine(length);
    }

    public void ClickedAddNode()
    {
        Floorplan.AddNode();
    }

    public void ShowNodePanel(Vector3 position)
    {
        Vector2 localPoint;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_2DCanvasRt, screenPos, null, out localPoint);
        NodeActionsPanel.transform.localPosition = localPoint;
        NodeActionsPanel.gameObject.SetActive(true);
    }

    public void HideNodePanel()
    {
        NodeActionsPanel.gameObject.SetActive(false);
    }

    public void ShowLinePanel()
    {
        Vector2 localPoint;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(Utils.GetCurrentMousePosition());
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_2DCanvasRt, screenPos, null, out localPoint);
        LineActionsPanel.transform.localPosition = localPoint;
        LineActionsPanel.gameObject.SetActive(true);
    }

    public void HideLinePanel()
    {
        LineActionsPanel.gameObject.SetActive(false);
    }

    public void HideResizePanel()
    {
        ResizePanel.gameObject.SetActive(false);
    }

    public void ShowResizePanel()
    {
        ResizePanel.gameObject.SetActive(true);
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
