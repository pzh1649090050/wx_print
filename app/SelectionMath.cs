namespace WeShot;

public static class SelectionMath
{
    public static Rectangle Normalize(Point a, Point b, Size limit)
    {
        int x1=Math.Clamp(Math.Min(a.X,b.X),0,limit.Width), y1=Math.Clamp(Math.Min(a.Y,b.Y),0,limit.Height);
        int x2=Math.Clamp(Math.Max(a.X,b.X),0,limit.Width), y2=Math.Clamp(Math.Max(a.Y,b.Y),0,limit.Height);
        return Rectangle.FromLTRB(x1,y1,x2,y2);
    }
    public static Rectangle Move(Rectangle r, Point delta, Size limit) => new(Math.Clamp(r.X+delta.X,0,Math.Max(0,limit.Width-r.Width)),Math.Clamp(r.Y+delta.Y,0,Math.Max(0,limit.Height-r.Height)),r.Width,r.Height);
    public static Rectangle Resize(Rectangle r, Point p, string handle, Size limit)
    {
        var a=new Point(handle.Contains('W')?p.X:r.Left,handle.Contains('N')?p.Y:r.Top);
        var b=new Point(handle.Contains('E')?p.X:r.Right,handle.Contains('S')?p.Y:r.Bottom);
        return Normalize(a,b,limit);
    }
}
