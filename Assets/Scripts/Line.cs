using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
using TMPro;

public class Line : MonoBehaviour
{

    public GameObject startNode, endNode, sizeLabel;
    public string name;

    public void RenderLineSizeLabel(Vector3 startPoint, Vector3 endPoint, Transform parent)
    {
        TextMeshPro textMesh;
        
        if (sizeLabel == null)
        {
            sizeLabel = new GameObject(name + " Label");
            sizeLabel.transform.parent = parent;
            textMesh = sizeLabel.AddComponent<TextMeshPro>();
        }
        else
        {
            textMesh = sizeLabel.GetComponent<TextMeshPro>();
        }

        if (startPoint == null)
        {
            startPoint = startNode.transform.position;
        }

        textMesh.transform.position = Vector3.Lerp(startPoint, endPoint, 0.5f);
        textMesh.color = Color.black;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = 12;

        textMesh.text = (Vector3.Distance(startPoint, endPoint) / 5).ToString("0.00") + "m";
    }

    private void OnDestroy()
    {
        UnityEngine.Object.Destroy(sizeLabel);
    }
}
