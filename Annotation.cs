using System.Drawing.Drawing2D;
namespace WeShot;

internal enum Tool {None,Rectangle,Ellipse,Emoji,Arrow,Pen,Mosaic,Text}

internal sealed class Annotation
{
    public Tool Tool {get;init;}
    public Color Color {get;init;}=Color.Red;
    public float Width {get;init;}=3;
    public string Text {get;init;}="";
    public float FontSize {get;init;}=22;
    public int TextWidth {get;init;}
    public List<Point> Points {get;}=new();
    public void Draw(Graphics g,Bitmap source)
    {
        if(Points.Count==0)return;
        var a=Points[0];var b=Points[^1];var r=Rectangle.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y));
        using var pen=new Pen(Color,Width){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round};
        using var brush=new SolidBrush(Color);
        switch(Tool)
        {
            case Tool.Rectangle:if(r.Width>0 && r.Height>0)g.DrawRectangle(pen,r);break;
            case Tool.Ellipse:if(r.Width>0 && r.Height>0)g.DrawEllipse(pen,r);break;
            case Tool.Arrow:
                if(a!=b){using var cap=new AdjustableArrowCap(4,5,true);pen.CustomEndCap=cap;g.DrawLine(pen,a,b);}break;
            case Tool.Pen:
                if(Points.Count>1)g.DrawLines(pen,Points.ToArray());else g.FillEllipse(brush,a.X-Width/2,a.Y-Width/2,Width,Width);break;
            case Tool.Mosaic:
                int block=(int)Width*3+5;
                var visited=new HashSet<(int,int)>();
                IEnumerable<Point> Stroke()
                {
                    yield return Points[0];
                    for(int i=1;i<Points.Count;i++){var from=Points[i-1];var to=Points[i];int steps=Math.Max(1,Math.Max(Math.Abs(to.X-from.X),Math.Abs(to.Y-from.Y))/Math.Max(1,block/2));for(int j=1;j<=steps;j++)yield return new Point(from.X+(to.X-from.X)*j/steps,from.Y+(to.Y-from.Y)*j/steps);}
                }
                foreach(var point in Stroke()){for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++){
                    int x=(point.X/block+ox)*block,y=(point.Y/block+oy)*block;
                    if(x<0 || y<0 || x>=source.Width || y>=source.Height || !visited.Add((x,y)))continue;
                    using var pixel=new SolidBrush(source.GetPixel(Math.Min(x+block/2,source.Width-1),Math.Min(y+block/2,source.Height-1)));
                    g.FillRectangle(pixel,x,y,Math.Min(block,source.Width-x),Math.Min(block,source.Height-y));
                }}break;
            case Tool.Text:
            case Tool.Emoji:
                using(var font=new Font(Tool==Tool.Emoji?"Segoe UI Emoji":"Microsoft YaHei UI",FontSize,FontStyle.Regular,GraphicsUnit.Pixel))
                    if(TextWidth>0)g.DrawString(Text,font,brush,new RectangleF(a.X,a.Y,TextWidth,Math.Max(1,source.Height-a.Y)),StringFormat.GenericTypographic);else g.DrawString(Text,font,brush,a,StringFormat.GenericTypographic);
                break;
        }
    }
}
