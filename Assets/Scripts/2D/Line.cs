﻿using UnityEngine;
using TMPro;

public class Line : MonoBehaviour
{

    public GameObject startNode, endNode, sizeLabel;
    public string name;

    public void RenderLineSizeLabel(Vector3 endPosition)
    {
        renderSizeLabel(startNode.transform.position, endPosition);
    }

    public void RenderLineSizeLabel()
    {
        renderSizeLabel(startNode.transform.position, endNode.transform.position);
    }

    void renderSizeLabel(Vector3 startPoint, Vector3 endPoint)
    {
        TextMeshPro textMesh = getSizeLabelComponent();
        float distance = Vector3.Distance(startPoint, endPoint) / Globals.ScaleFactor;

        textMesh.transform.position = Vector3.Lerp(startPoint, endPoint, 0.5f);
        textMesh.color = Color.black;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = 10;

        textMesh.text = distance > 0 ? distance.ToString("0.##") + "m" : "";
    }

    TextMeshPro getSizeLabelComponent()
    {
        if (sizeLabel == null)
        {
            sizeLabel = new GameObject(name + " Label");
            sizeLabel.transform.parent = gameObject.transform.parent;
            return sizeLabel.AddComponent<TextMeshPro>();
        }
        else
        {
            return sizeLabel.GetComponent<TextMeshPro>();
        }
    }

    public void AdjustLine(Vector3 endPosition)
    {
        AdjustPositionScaleRotation(this.startNode.transform.position, endPosition);
        RenderLineSizeLabel(endPosition);
    }

    public void AdjustLine()
    {
        if (this.endNode == null) return;

        AdjustPositionScaleRotation(this.startNode.transform.position, this.endNode.transform.position);
        RenderLineSizeLabel();
    }

    private void AdjustPositionScaleRotation(Vector3 startPosition, Vector3 endPosition)
    {
        GameObject line = gameObject;
        Vector3 direction = endPosition - startPosition ;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion newRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        Vector3 newScale = new(direction.magnitude, line.transform.localScale.y, line.transform.localScale.z);

        line.transform.position = startPosition;
        line.transform.rotation = newRotation;
        line.transform.localScale = newScale;
    }

    private void OnDestroy()
    {
        UnityEngine.Object.Destroy(sizeLabel);
    }
}
