namespace WeShot;
internal static class ImageLayout
{
    public static double FitScale(double desired,Size image,Size area)
    {
        double max=Math.Min(4,Math.Min(area.Width/(double)image.Width,area.Height/(double)image.Height));
        return Math.Clamp(desired,Math.Min(0.1,max),max);
    }
    public static Rectangle Fit(Size image,Rectangle area)
    {
        double scale=Math.Min(area.Width/(double)image.Width,area.Height/(double)image.Height);
        int w=Math.Max(1,(int)(image.Width*scale)),h=Math.Max(1,(int)(image.Height*scale));
        return new Rectangle(area.X+(area.Width-w)/2,area.Y+(area.Height-h)/2,w,h);
    }
}
