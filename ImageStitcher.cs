using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace WeShot;

public static class ImageStitcher
{
    // Screen content must overlap vertically. Never silently concatenate unrelated frames.
    public static Bitmap Append(Bitmap previous, Bitmap next, out int added)
    {
        if(previous.Width!=next.Width)throw new InvalidOperationException("截图宽度发生变化，请重新选择固定区域。");
        var a=Pixels(previous);var b=Pixels(next);
        int max=Math.Min(previous.Height,next.Height), min=Math.Min(40,max);
        if(previous.Height>=next.Height && a.AsSpan((previous.Height-next.Height)*previous.Width*4,b.Length).SequenceEqual(b))
        {added=0;return (Bitmap)previous.Clone();}
        double Score(int overlap)
        {
            long error=0;int samples=0,features=0;
            for(int row=0;row<24;row++)
            {
                int y=Math.Min(overlap-1,(int)((row+0.5)*overlap/24));
                for(int x=3;x<next.Width-3;x+=Math.Max(3,next.Width/64))
                {
                    int ai=((previous.Height-overlap+y)*previous.Width+x)*4,bi=(y*next.Width+x)*4;
                    for(int c=0;c<3;c++){error+=Math.Abs(a[ai+c]-b[bi+c]);samples++;}
                    if(x>=6 && (Math.Abs(a[ai]-a[ai-12])+Math.Abs(a[ai+1]-a[ai-11])+Math.Abs(a[ai+2]-a[ai-10]))>45)features++;
                }
            }
            return samples==0 || features<12?double.MaxValue:(double)error/samples;
        }
        int best=0;double bestScore=double.MaxValue;
        for(int overlap=max-1;overlap>=min;overlap--){double score=Score(overlap);if(score<bestScore){bestScore=score;best=overlap;}}
        if(best==0 || bestScore>9)throw new InvalidOperationException("没有找到可靠的重叠区域。请减小滚动幅度，并避开固定标题栏、视频和动画。");
        added=next.Height-best;
        if(previous.Height+added>25000)throw new InvalidOperationException("已达到 25000 像素长度，请先保存当前长截图。");
        var result=new Bitmap(previous.Width,previous.Height+added,PixelFormat.Format32bppArgb);
        using var g=Graphics.FromImage(result);g.DrawImageUnscaled(previous,0,0);
        g.DrawImage(next,new Rectangle(0,previous.Height,next.Width,added),new Rectangle(0,best,next.Width,added),GraphicsUnit.Pixel);
        return result;
    }
    static byte[] Pixels(Bitmap image)
    {
        using var normalized=new Bitmap(image.Width,image.Height,PixelFormat.Format32bppArgb);
        using(var g=Graphics.FromImage(normalized))g.DrawImageUnscaled(image,0,0);
        var data=normalized.LockBits(new Rectangle(Point.Empty,normalized.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        try{var bytes=new byte[image.Width*image.Height*4];Marshal.Copy(data.Scan0,bytes,0,bytes.Length);return bytes;}
        finally{normalized.UnlockBits(data);}
    }
}
