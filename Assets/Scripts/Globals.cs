using UnityEngine;

public static class Globals 
{
    public static float ScaleFactor = 5f;
    public static float SnapProxmityFactor = .2f;

    public class Camera
    {
        public static int MinScale = 1;
        public static int MaxScale = 40;
        public static int StartSize = 20;
        public static int ZoomButtonsStep = 2;
    }

    public class WallParams
    {
        public static float Height = 2.7f;
        public static float Thickness = .15f;
    }
    public class Layers
    {
        public static int Grid = 8;
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

    public class GridLine
    {
        public static Color Color = new(.75f, .75f, .75f, .5f);
        public static Color StrongColor = new(.75f, .75f, .75f, 1f);
        public static Color HighlightColor = new(0, .496f, 1f, 1f);
    }

    public class Line
    {
        public static Color Color = new(0, 0, 0);
        public static Color HighlightColor = new(0, .7f, 1, 1);
    }
    public class Node
    {
        public static float Size = .7f;
        public static Color Color = new(0, .42f, 1, 1);
        public static Color HighlightColor = new(0, .7f, 1, 1);
    }
}