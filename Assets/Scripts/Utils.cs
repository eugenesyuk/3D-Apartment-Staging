using UnityEngine;
public static class Utils
{
    public static Vector3? GetCurrentMousePosition(Vector3 screenPosition)
    {
        var ray = Camera.main.ScreenPointToRay(screenPosition);
        var plane = new Plane(Vector3.forward, Vector3.zero);
        return plane.Raycast(ray, out float rayDistance) ? ray.GetPoint(rayDistance) : null;
    }
}
