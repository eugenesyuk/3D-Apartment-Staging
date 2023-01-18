public static class Globals 
{
    public static float ScaleFactor = 5f;
    public class WallParams
    {
        public static float Height = 2.7f;
        public static float Thickness = .15f;
    }
    public class Layers
    {
        public static int Floorplan = 9;
        public static int Scene3D = 10;
    }
    public class Window
    {
        public static float Width = 1.3f;
        public static float Height = 1.3f;
    }
    public class Door
    {
        public static float Width = 1f;
        public static float Height = ((WallParams.Height - Window.Height) / 2) + Window.Height;
    }
}