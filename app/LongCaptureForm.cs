namespace WeShot;

internal sealed class LongCaptureForm:Form
{
    Bitmap stitched;
    readonly Rectangle captureBounds;
    readonly PictureBox preview=new(){Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(238,238,238)};
    readonly Label status=new(){Dock=DockStyle.Bottom,Height=55,Padding=new Padding(8),ForeColor=Color.DimGray};
    readonly FlowLayoutPanel controls=new(){Dock=DockStyle.Bottom,AutoSize=true,Padding=new Padding(6),WrapContents=true};
    readonly Stack<Bitmap> history=new();
    readonly IntPtr target;
    bool busy;
    public LongCaptureForm(Bitmap image,Point origin)
    {
        stitched=image;captureBounds=new Rectangle(origin,image.Size);target=Native.GetAncestor(Native.WindowFromPoint(new Point(origin.X+image.Width/2,origin.Y+image.Height/2)),2);
        Text="滚动截图 · 独立截图";Icon=IconPainter.AppIcon();Size=new Size(330,510);MinimumSize=new Size(300,350);TopMost=true;StartPosition=FormStartPosition.Manual;Font=new Font("Microsoft YaHei UI",9);
        var area=Screen.FromPoint(origin).WorkingArea;int x=captureBounds.Right+16;if(x+Width>area.Right)x=captureBounds.Left-Width-16;if(x<area.Left)x=area.Right-Width-10;Location=new Point(x,Math.Clamp(origin.Y,area.Top,Math.Max(area.Top,area.Bottom-Height)));
        Button Add(string label,Action action){var b=new Button{Text=label,AutoSize=true,Height=30};b.Click+=(_,_)=>action();controls.Controls.Add(b);return b;}
        Add("向下滚动并采集",async()=>await CaptureNext(true));Add("采集当前画面",async()=>await CaptureNext(false));
        Add("撤销上次",()=>{if(history.Count>0){stitched.Dispose();stitched=history.Pop();RefreshPreview("已撤销");}});
        Add("保存",()=>Safe(()=>ImageOutput.Save(stitched,this)));Add("复制",()=>Safe(()=>{ImageOutput.Copy(stitched);status.Text="长截图已复制";}));Add("贴图",()=>new PinForm((Bitmap)stitched.Clone(),captureBounds.Location).Show());
        Controls.Add(preview);Controls.Add(status);Controls.Add(controls);RefreshPreview("手动滚动目标页面后采集，或点击自动滚动。选区须避开固定标题栏。");
        FormClosing+=(_,e)=>{if(busy)e.Cancel=true;};FormClosed+=(_,_)=>{preview.Image=null;stitched.Dispose();foreach(var item in history)item.Dispose();};
    }
    void Safe(Action a){try{a();}catch(Exception ex){MessageBox.Show(this,ex.Message,"长截图",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
    void RefreshPreview(string message){preview.Image=stitched;status.Text=$"{stitched.Width} × {stitched.Height}\n{message}";}
    async Task CaptureNext(bool scroll)
    {
        if(busy)return;busy=true;controls.Enabled=false;
        try
        {
            Hide();await Task.Delay(180);
            if(scroll){Native.SetForegroundWindow(target);Cursor.Position=new Point(captureBounds.Left+captureBounds.Width/2,captureBounds.Top+captureBounds.Height/2);Native.mouse_event(0x0800,0,0,-240,UIntPtr.Zero);await Task.Delay(650);}
            using var frame=DesktopCapture.Grab(captureBounds);using var previous=(Bitmap)stitched.Clone();
            var result=await Task.Run(()=>{var image=ImageStitcher.Append(previous,frame,out int added);return(image,added);});
            if(result.added>0){history.Push(stitched);if(history.Count>3){var items=history.ToArray();history.Clear();for(int i=2;i>=0;i--)history.Push(items[i]);for(int i=3;i<items.Length;i++)items[i].Dispose();}stitched=result.image;RefreshPreview($"已拼接新增 {result.added} 像素");}
            else{result.image.Dispose();RefreshPreview("画面未变化，可能已滚动到底。");}
        }
        catch(Exception ex){RefreshPreview(ex.Message);}
        finally{busy=false;controls.Enabled=true;Show();Activate();}
    }
}
