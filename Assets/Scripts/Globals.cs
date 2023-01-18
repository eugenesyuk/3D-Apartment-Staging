using UnityEngine;
public class Globals : MonoBehaviour
{
    public static Globals Instance { get; private set; }

    public static float ScaleFactor = 5f;
    public static WallParams WallParameters = new WallParams();

    public class WallParams
    {
        public static float Height = 2.3f;
        public static float Thickness = .15f;
    }

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else
        {
            Destroy(gameObject);
        }
    }
}
