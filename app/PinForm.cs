using System.Drawing.Drawing2D;
namespace WeShot;

internal sealed class PinForm:Form
{
    readonly Bitmap image;
    Point drag;
    bool dragging,hover;
    readonly ToolTip tip=new();
    public PinForm(Bitmap image,Point origin)
    {
        this.image=image;Text="贴图 · 独立截图";Icon=IconPainter.AppIcon();TopMost=true;ShowInTaskbar=true;
        AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;DoubleBuffered=true;KeyPreview=true;
        var area=Screen.FromPoint(origin).WorkingArea;double scale=Math.Min(1,Math.Min((area.Width-40d)/image.Width,(area.Height-40d)/image.Height));
        ClientSize=new Size(Math.Max(50,(int)(image.Width*scale)),Math.Max(40,(int)(image.Height*scale)));
        Location=new Point(Math.Clamp(origin.X,area.Left,Math.Max(area.Left,area.Right-Width)),Math.Clamp(origin.Y,area.Top,Math.Max(area.Top,area.Bottom-Height)));
        var menu=new ContextMenuStrip();menu.Items.Add("复制 (Ctrl+C)",null,(_,_)=>Safe(()=>ImageOutput.Copy(image)));menu.Items.Add("保存 (Ctrl+S)",null,(_,_)=>Safe(()=>ImageOutput.Save(image,this)));menu.Items.Add("原始尺寸",null,(_,_)=>Zoom(1));menu.Items.Add("50%",null,(_,_)=>Zoom(0.5));menu.Items.Add(new ToolStripSeparator());menu.Items.Add("关闭 (Esc)",null,(_,_)=>Close());ContextMenuStrip=menu;
        tip.SetToolTip(this,"拖动移动 · 滚轮缩放 · Ctrl+滚轮调透明度 · 双击复制 · 右键更多");
        FormClosed+=(_,_)=>{image.Dispose();tip.Dispose();menu.Dispose();};
    }
    void Safe(Action a){try{a();}catch(Exception ex){MessageBox.Show(this,ex.Message,"贴图",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
    void Zoom(double scale){var area=Screen.FromControl(this).WorkingArea;scale=ImageLayout.FitScale(scale,image.Size,area.Size);ClientSize=new Size(Math.Max(40,(int)(image.Width*scale)),Math.Max(30,(int)(image.Height*scale)));Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Color.FromArgb(245,245,245));e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;e.Graphics.DrawImage(image,ImageLayout.Fit(image.Size,ClientRectangle));e.Graphics.DrawRectangle(Pens.LightGray,0,0,Width-1,Height-1);
        if(hover){using var bg=new SolidBrush(Color.FromArgb(200,250,250,250));e.Graphics.FillRectangle(bg,Width-29,1,28,28);IconPainter.Draw(e.Graphics,"Cancel",new Rectangle(Width-24,6,18,18),Color.FromArgb(75,75,75));}
    }
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){if(e.X>Width-30 && e.Y<30){Close();return;}drag=e.Location;dragging=true;Capture=true;}}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging)Location=new Point(Left+e.X-drag.X,Top+e.Y-drag.Y);}
    protected override void OnMouseUp(MouseEventArgs e){dragging=false;Capture=false;base.OnMouseUp(e);}
    protected override void OnMouseDoubleClick(MouseEventArgs e){base.OnMouseDoubleClick(e);Safe(()=>ImageOutput.Copy(image));}
    protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);if((ModifierKeys&Keys.Control)!=0)Opacity=Math.Clamp(Opacity+(e.Delta>0?0.05:-0.05),0.2,1);else Zoom(Width/(double)image.Width*(e.Delta>0?1.1:1/1.1));}
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
    {
        if(keyData is Keys.Escape or Keys.Delete){Close();return true;}
        if(keyData==(Keys.Control|Keys.C)){Safe(()=>ImageOutput.Copy(image));return true;}
        if(keyData==(Keys.Control|Keys.S)){Safe(()=>ImageOutput.Save(image,this));return true;}
        return base.ProcessCmdKey(ref msg,keyData);
    }
}
