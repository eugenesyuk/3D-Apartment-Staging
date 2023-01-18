struct Point
{
    public float X, Y;
    public static bool operator ==(Point u1, Point u2)
    {
        return u1.Equals(u2);  // use ValueType.Equals() which compares field-by-field.
    }
    public static bool operator !=(Point u1, Point u2)
    {
        return !u1.Equals(u2);  // use ValueType.Equals() which compares field-by-field.
    }
    public Point(float x, float y)
    {
        X = x;
        Y = y;
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return base.ToString();
    }
}