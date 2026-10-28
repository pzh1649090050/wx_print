using System.Drawing.Imaging;
namespace WeShot;

internal static class ImageOutput
{
    public static void Copy(Bitmap image)
    {
        using var png=new MemoryStream();image.Save(png,ImageFormat.Png);png.Position=0;
        var data=new DataObject();data.SetImage(image);data.SetData("PNG",false,png);
        Clipboard.SetDataObject(data,true,5,100);
    }
    public static bool Save(Bitmap image,IWin32Window? owner=null)
    {
        using var dialog=new SaveFileDialog{Title="保存图片",Filter="PNG 图片|*.png|JPEG 图片|*.jpg|BMP 图片|*.bmp",FileName=$"截图_{DateTime.Now:yyyyMMdd_HHmmss}.png",DefaultExt="png",AddExtension=true,OverwritePrompt=true,InitialDirectory=Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)};
        if(dialog.ShowDialog(owner)!=DialogResult.OK)return false;
        string ext=Path.GetExtension(dialog.FileName).ToLowerInvariant();
        if(ext is ".jpg" or ".jpeg")
        {
            using var flat=new Bitmap(image.Width,image.Height);using(var g=Graphics.FromImage(flat)){g.Clear(Color.White);g.DrawImageUnscaled(image,0,0);}flat.Save(dialog.FileName,ImageFormat.Jpeg);
        }
        else image.Save(dialog.FileName,ext==".bmp"?ImageFormat.Bmp:ImageFormat.Png);
        return true;
    }
}
