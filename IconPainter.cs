using System.Drawing.Drawing2D;
namespace WeShot;

internal static class IconPainter
{
    public static readonly Color Green=Color.FromArgb(7,193,96);
    public static void Draw(Graphics g,string name,Rectangle bounds,Color color)
    {
        var save=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;
        g.TranslateTransform(bounds.X+bounds.Width/2f,bounds.Y+bounds.Height/2f);g.ScaleTransform(bounds.Width/24f,bounds.Height/24f);
        using var p=new Pen(color,1.6f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round};
        using var b=new SolidBrush(color);
        switch(name)
        {
            case "Rectangle":g.DrawRectangle(p,-8,-7,16,14);break;
            case "Ellipse":g.DrawEllipse(p,-8,-8,16,16);break;
            case "Emoji":g.DrawEllipse(p,-9,-9,18,18);g.FillEllipse(b,-4,-3,2,2);g.FillEllipse(b,3,-3,2,2);g.DrawArc(p,-5,-2,10,8,10,160);break;
            case "Arrow":g.DrawLine(p,-7,7,7,-7);g.DrawLines(p,new[]{new Point(-2,-7),new Point(7,-7),new Point(7,2)});break;
            case "Pen":g.DrawPolygon(p,new[]{new Point(-8,8),new Point(-6,2),new Point(5,-9),new Point(9,-5),new Point(-2,6)});g.DrawLine(p,3,-7,7,-3);break;
            case "Mosaic":g.DrawRectangle(p,-8,-8,16,16);for(int x=-6;x<7;x+=4)for(int y=-6;y<7;y+=4)if((x+y)%8==4 || (x+y)%8==-4)g.FillRectangle(b,x,y,3,3);break;
            case "Text":g.DrawRectangle(p,-8,-8,16,16);g.DrawLine(p,-4,-4,4,-4);g.DrawLine(p,0,-4,0,5);g.DrawLine(p,-2,5,2,5);break;
            case "Translate":g.DrawRectangle(p,-8,-2,11,10);g.DrawRectangle(p,-2,-8,10,10);using(var f=new Font("Segoe UI",7,FontStyle.Regular,GraphicsUnit.Pixel)){g.DrawString("文",f,b,-8,-2);g.DrawString("A",f,b,-1,-8);}break;
            case "Ocr":
                g.DrawLines(p,new[]{new Point(-8,-3),new Point(-8,-8),new Point(-3,-8)});g.DrawLines(p,new[]{new Point(3,-8),new Point(8,-8),new Point(8,-3)});
                g.DrawLines(p,new[]{new Point(-8,3),new Point(-8,8),new Point(-3,8)});g.DrawLines(p,new[]{new Point(3,8),new Point(8,8),new Point(8,3)});g.DrawLine(p,-4,-3,4,-3);g.DrawLine(p,0,-3,0,5);break;
            case "Long":
                g.DrawRectangle(p,-6,-9,12,18);g.DrawLine(p,0,-5,0,5);g.DrawLines(p,new[]{new Point(-3,-2),new Point(0,-5),new Point(3,-2)});g.DrawLines(p,new[]{new Point(-3,2),new Point(0,5),new Point(3,2)});break;
            case "Undo":g.DrawArc(p,-5,-4,13,12,220,250);g.DrawLines(p,new[]{new Point(-1,-8),new Point(-6,-3),new Point(0,-2)});break;
            case "Save":g.DrawLines(p,new[]{new Point(-7,-4),new Point(-7,8),new Point(7,8),new Point(7,-4)});g.DrawLine(p,0,-9,0,3);g.DrawLines(p,new[]{new Point(-4,-1),new Point(0,3),new Point(4,-1)});break;
            case "Pin":g.DrawPolygon(p,new[]{new Point(-4,-9),new Point(4,-9),new Point(3,-2),new Point(6,3),new Point(-6,3),new Point(-3,-2)});g.DrawLine(p,0,3,0,10);break;
            case "Forward":g.DrawLines(p,new[]{new Point(-8,6),new Point(-7,0),new Point(-2,-4),new Point(2,-4),new Point(2,-9),new Point(9,-2),new Point(2,5),new Point(2,0),new Point(-2,0),new Point(-8,6)});break;
            case "Cancel":g.DrawLine(p,-6,-6,6,6);g.DrawLine(p,6,-6,-6,6);break;
            case "Done":g.DrawLines(p,new[]{new Point(-7,0),new Point(-2,5),new Point(8,-6)});break;
            case "Capture":g.DrawLines(p,new[]{new Point(-10,-4),new Point(-4,-4),new Point(-4,-10)});g.DrawLines(p,new[]{new Point(10,4),new Point(4,4),new Point(4,10)});g.DrawRectangle(p,-7,-7,14,14);break;
        }
        g.Restore(save);
    }
    public static GraphicsPath Round(Rectangle r,int radius)
    {
        int d=radius*2;var path=new GraphicsPath();path.AddArc(r.Left,r.Top,d,d,180,90);path.AddArc(r.Right-d,r.Top,d,d,270,90);path.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);path.AddArc(r.Left,r.Bottom-d,d,d,90,90);path.CloseFigure();return path;
    }
    public static Icon AppIcon()
    {
        using var bitmap=new Bitmap(64,64);using(var g=Graphics.FromImage(bitmap)){g.SmoothingMode=SmoothingMode.AntiAlias;using var bg=new SolidBrush(Green);using var path=Round(new Rectangle(1,1,62,62),14);g.FillPath(bg,path);Draw(g,"Capture",new Rectangle(12,12,40,40),Color.White);}
        IntPtr h=bitmap.GetHicon();using var original=Icon.FromHandle(h);var clone=(Icon)original.Clone();Native.DestroyIcon(h);return clone;
    }
}

internal sealed class IconButton:Control
{
    public string IconName {get;set;}="Capture";
    public bool Selected {get;set;}
    public Color IconColor {get;set;}=Color.FromArgb(65,65,65);
    bool hover;
    public IconButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);Size=new Size(48,46);Cursor=Cursors.Hand;TabStop=false;}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Selected?Color.FromArgb(224,245,233):hover?Color.FromArgb(236,236,236):Color.FromArgb(250,250,250));
        IconPainter.Draw(e.Graphics,IconName,new Rectangle((Width-22)/2,(Height-22)/2,22,22),Enabled?(Selected?IconPainter.Green:IconColor):Color.FromArgb(185,185,185));
    }
}
