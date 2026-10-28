using System.Runtime.InteropServices;
namespace WeShot;

internal static class Native
{
    [StructLayout(LayoutKind.Sequential)]internal struct RECT {public int Left,Top,Right,Bottom;public Rectangle Rectangle=>Rectangle.FromLTRB(Left,Top,Right,Bottom);}
    internal delegate bool EnumProc(IntPtr h,IntPtr p);
    [DllImport("user32.dll")]internal static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
    [DllImport("user32.dll")]internal static extern bool UnregisterHotKey(IntPtr h,int id);
    [DllImport("user32.dll")]internal static extern bool EnumWindows(EnumProc callback,IntPtr param);
    [DllImport("user32.dll")]internal static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")]internal static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")]internal static extern bool GetWindowRect(IntPtr h,out RECT r);
    [DllImport("user32.dll")]internal static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
    [DllImport("dwmapi.dll")]internal static extern int DwmGetWindowAttribute(IntPtr h,int attribute,out RECT r,int size);
    [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")]internal static extern int GetCloaked(IntPtr h,int attribute,out int value,int size);
    [DllImport("user32.dll")]internal static extern IntPtr WindowFromPoint(Point p);
    [DllImport("user32.dll")]internal static extern IntPtr GetAncestor(IntPtr h,uint flags);
    [DllImport("user32.dll")]internal static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")]internal static extern void mouse_event(uint flags,uint dx,uint dy,int data,UIntPtr extra);
    [DllImport("user32.dll")]internal static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int cx,int cy,uint flags);
    [DllImport("user32.dll")]internal static extern bool DestroyIcon(IntPtr icon);

    internal static List<Rectangle> WindowBounds(Rectangle desktop)
    {
        var list=new List<Rectangle>();
        EnumWindows((h,p)=>{GetWindowThreadProcessId(h,out uint pid);if(pid==Environment.ProcessId || !IsWindowVisible(h) || IsIconic(h))return true;
            if(GetCloaked(h,14,out int cloaked,4)==0 && cloaked!=0)return true;
            if(DwmGetWindowAttribute(h,9,out var r,16)!=0)GetWindowRect(h,out r);
            var bounds=Rectangle.Intersect(r.Rectangle,desktop);if(bounds.Width>20 && bounds.Height>20){bounds.Offset(-desktop.X,-desktop.Y);list.Add(bounds);}return true;},IntPtr.Zero);
        return list;
    }
}

internal static class DesktopCapture
{
    public static Bitmap Grab(Rectangle bounds)
    {
        var image=new Bitmap(bounds.Width,bounds.Height,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try{using var g=Graphics.FromImage(image);g.CopyFromScreen(bounds.Location,Point.Empty,bounds.Size,CopyPixelOperation.SourceCopy);return image;}
        catch{image.Dispose();throw;}
    }
}
