using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
namespace WeShot;

internal static class ImageRenderer
{
    public static Bitmap Render(Bitmap source,Rectangle selection,IEnumerable<Annotation> marks)
    {
        var r=Rectangle.Intersect(selection,new Rectangle(Point.Empty,source.Size));
        if(r.Width<1 || r.Height<1)throw new InvalidOperationException("请选择截图区域。");
        var output=source.Clone(r,PixelFormat.Format32bppArgb);
        using var g=Graphics.FromImage(output);g.TranslateTransform(-r.X,-r.Y);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
        foreach(var mark in marks)mark.Draw(g,source);return output;
    }
}
