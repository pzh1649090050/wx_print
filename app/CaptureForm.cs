using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace WeShot;

internal enum CaptureAction {Copy,Save,Pin,Ocr,Translate,Long,Forward}

internal sealed class CaptureForm:Form
{
    readonly Bitmap screen;
    readonly Rectangle desktop;
    readonly List<Rectangle> windows;
    readonly List<Annotation> marks=new();
    readonly Stack<Annotation> redo=new();
    readonly Dictionary<Tool,IconButton> toolButtons=new();
    readonly Panel toolbar=new(){BackColor=Color.FromArgb(250,250,250),Size=new Size(790,46),Visible=false};
    readonly FlowLayoutPanel options=new(){BackColor=Color.FromArgb(250,250,250),Height=44,AutoSize=false,WrapContents=false,Visible=false,Padding=new Padding(8,5,8,5)};
    readonly ToolTip tips=new(){InitialDelay=450,ReshowDelay=150,AutoPopDelay=5000};
    IconButton undoButton=null!;
    Tool tool;
    Color ink=Color.FromArgb(250,81,81);
    int lineWidth=3;
    int fontSize=22;
    string emoji="😀";
    bool selected;
    Rectangle selection,hover;
    Point mouse,anchor;
    Rectangle original;
    string gesture="",handle="";
    Annotation? current;
    TextBox? editor;
    bool leaving;
    public bool HasResult=>leaving;
    string status="";
    public event Action<Bitmap,CaptureAction,Point>? Captured;
    public event Action? Saved;

    public CaptureForm(Bitmap screen,Rectangle desktop,List<Rectangle> windows)
    {
        this.screen=screen;this.desktop=desktop;this.windows=windows;
        AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;
        Bounds=desktop;TopMost=true;ShowInTaskbar=true;KeyPreview=true;Cursor=Cursors.Cross;Text="独立截图 · 选择区域";
        SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.UserPaint,true);
        BuildToolbar();Controls.Add(toolbar);Controls.Add(options);
        Shown+=(_,_)=>{Native.SetWindowPos(Handle,new IntPtr(-1),desktop.X,desktop.Y,desktop.Width,desktop.Height,0x40);Activate();mouse=PointToClient(Cursor.Position);hover=HoverBounds(mouse);Invalidate();};
        FormClosed+=(_,_)=>{tips.Dispose();screen.Dispose();};
    }
    void BuildToolbar()
    {
        int x=7;
        void Sep(){var s=new Panel{BackColor=Color.FromArgb(225,225,225),Bounds=new Rectangle(x+2,12,1,22)};toolbar.Controls.Add(s);x+=8;}
        IconButton Add(string icon,string label,Action action,Color? color=null)
        {
            var button=new IconButton{IconName=icon,Location=new Point(x,0),AccessibleName=label,IconColor=color??Color.FromArgb(65,65,65)};
            tips.SetToolTip(button,label);button.Click+=(_,_)=>action();toolbar.Controls.Add(button);x+=48;return button;
        }
        foreach(var (kind,label) in new[]{(Tool.Rectangle,"矩形"),(Tool.Ellipse,"椭圆"),(Tool.Emoji,"表情"),(Tool.Arrow,"箭头"),(Tool.Pen,"画笔"),(Tool.Mosaic,"马赛克"),(Tool.Text,"文字")})
        {
            var k=kind;toolButtons[k]=Add(k.ToString(),label,()=>SelectTool(k));
        }
        Sep();Add("Translate","翻译",()=>Complete(CaptureAction.Translate));Add("Ocr","提取文字",()=>Complete(CaptureAction.Ocr));Add("Long","滚动截图",()=>Complete(CaptureAction.Long));
        Sep();undoButton=Add("Undo","撤销 (Ctrl+Z)",Undo);undoButton.Enabled=false;
        Add("Save","保存图片 (Ctrl+S)",()=>Complete(CaptureAction.Save));Add("Pin","置顶在桌面",()=>Complete(CaptureAction.Pin));
        Add("Forward","复制后到其他应用粘贴转发",()=>Complete(CaptureAction.Forward));Add("Cancel","取消 (Esc)",Close,Color.FromArgb(250,81,81));Add("Done","完成 (Enter)",()=>Complete(CaptureAction.Copy),IconPainter.Green);
        toolbar.Width=x+7;using var path=IconPainter.Round(new Rectangle(0,0,toolbar.Width,toolbar.Height),8);toolbar.Region=new Region(path);
    }
    void SelectTool(Tool value)
    {
        CommitText();tool=tool==value?Tool.None:value;
        foreach(var pair in toolButtons){pair.Value.Selected=pair.Key==tool;pair.Value.Invalidate();}
        Cursor=tool==Tool.None?Cursors.SizeAll:tool==Tool.Text?Cursors.IBeam:Cursors.Cross;
        BuildOptions();PositionToolbar();Invalidate();
    }
    void BuildOptions()
    {
        foreach(Control c in options.Controls.Cast<Control>().ToArray())c.Dispose();options.Controls.Clear();
        options.Visible=selected && tool!=Tool.None;
        if(!options.Visible)return;
        Button Choice(string text,int width,Action action)
        {
            var b=new Button{Text=text,Width=width,Height=32,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(250,250,250),Margin=new Padding(2,0,2,0),TabStop=false,Cursor=Cursors.Hand,Font=new Font("Microsoft YaHei UI",9)};
            b.FlatAppearance.BorderSize=0;b.Click+=(_,_)=>action();options.Controls.Add(b);return b;
        }
        if(tool==Tool.Emoji)
        {
            foreach(string value in new[]{"😀","😂","👍","❤️","⭐","❓","✅","🔥"}){string e=value;var b=Choice(e,38,()=>{emoji=e;BuildOptions();PositionToolbar();});b.Font=new Font("Segoe UI Emoji",17);if(emoji==e)b.BackColor=Color.FromArgb(224,245,233);}
        }
        else
        {
            foreach(int value in new[]{2,4,7}){int v=value;var b=Choice(value==2?"•":value==4?"●":"⬤",30,()=>{lineWidth=v;BuildOptions();PositionToolbar();});if(lineWidth==v || lineWidth==3 && v==2)b.ForeColor=IconPainter.Green;}
            if(tool==Tool.Text){foreach(int size in new[]{16,22,30}){int s=size;var b=Choice(size.ToString(),35,()=>{fontSize=s;BuildOptions();PositionToolbar();});if(fontSize==s)b.ForeColor=IconPainter.Green;}}
            if(tool!=Tool.Mosaic)foreach(Color color in new[]{Color.White,Color.FromArgb(60,60,60),Color.FromArgb(250,81,81),Color.FromArgb(255,170,0),Color.FromArgb(255,224,70),Color.FromArgb(7,193,96),Color.FromArgb(16,174,255),Color.FromArgb(100,103,240)})
            {
                var c=color;var b=Choice("",28,()=>{ink=c;BuildOptions();PositionToolbar();});b.Paint+=(_,e)=>{using var fill=new SolidBrush(c);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.FillRectangle(fill,6,8,16,16);using var border=new Pen(c==ink?IconPainter.Green:Color.FromArgb(210,210,210),c==ink?2:1);e.Graphics.DrawRectangle(border,4,6,20,20);};
            }
        }
        options.Width=options.Controls.Cast<Control>().Sum(c=>c.Width+c.Margin.Horizontal)+20;
        using var path=IconPainter.Round(new Rectangle(0,0,options.Width,options.Height),8);options.Region?.Dispose();options.Region=new Region(path);
    }
    Rectangle HoverBounds(Point p)
    {
        foreach(var r in windows)if(r.Contains(p))return r;
        var monitor=Screen.FromPoint(new Point(p.X+desktop.X,p.Y+desktop.Y)).Bounds;monitor.Offset(-desktop.X,-desktop.Y);return Rectangle.Intersect(ClientRectangle,monitor);
    }
    void PositionToolbar()
    {
        if(!selected)return;
        var global=new Point(selection.Right+desktop.X,selection.Bottom+desktop.Y);
        var area=Screen.FromPoint(global).Bounds;area.Offset(-desktop.X,-desktop.Y);
        int minX=Math.Max(0,area.Left+8),maxX=Math.Max(minX,Math.Min(Width-toolbar.Width-8,area.Right-toolbar.Width-8));
        int x=Math.Clamp(selection.Right-toolbar.Width,minX,maxX);
        int y=selection.Bottom+14;
        int extra=options.Visible?options.Height+6:0;
        if(y+toolbar.Height+extra>area.Bottom-8)y=selection.Top-toolbar.Height-extra-14;
        if(y<area.Top+8)y=Math.Clamp(selection.Bottom-toolbar.Height-extra-8,area.Top+8,Math.Max(area.Top+8,area.Bottom-toolbar.Height-extra-8));
        toolbar.Location=new Point(x,y);toolbar.Visible=true;
        options.Location=new Point(Math.Clamp(x,0,Math.Max(0,Width-options.Width)),y+toolbar.Height+6);
        toolbar.BringToFront();options.BringToFront();editor?.BringToFront();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.DrawImageUnscaled(screen,0,0);
        var r=selected?selection:hover;
        using(var dim=new SolidBrush(Color.FromArgb(140,0,0,0)))using(var region=new Region(ClientRectangle)){if(r.Width>0 && r.Height>0)region.Exclude(r);g.FillRegion(dim,region);}
        if(selected && selection.Width>0 && selection.Height>0)
        {
            var state=g.Save();g.SetClip(selection);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
            foreach(var mark in marks)mark.Draw(g,screen);current?.Draw(g,screen);g.Restore(state);
        }
        if(r.Width>0 && r.Height>0)
        {
            using var p=new Pen(Color.FromArgb(24,195,139),2);g.DrawRectangle(p,r.X,r.Y,Math.Max(0,r.Width-1),Math.Max(0,r.Height-1));
            using var green=new SolidBrush(p.Color);foreach(var point in Handles(r).Values)g.FillRectangle(green,point.X-3,point.Y-3,7,7);
            using var font=new Font("Microsoft YaHei UI",10,FontStyle.Regular,GraphicsUnit.Pixel);
            string size=$"{r.Width} × {r.Height}";var measure=g.MeasureString(size,font);
            int labelY=r.Top>=24?r.Top-25:r.Top+8;
            using var dark=new SolidBrush(Color.FromArgb(170,35,35,35));g.FillRectangle(dark,r.Left+2,labelY,measure.Width+12,21);g.DrawString(size,font,Brushes.White,r.Left+8,labelY+3);
        }
        if(!selected && gesture=="")DrawMagnifier(g);
        if(status.Length>0)
        {
            var area=Screen.FromPoint(Cursor.Position).Bounds;area.Offset(-desktop.X,-desktop.Y);using var f=new Font("Microsoft YaHei UI",11);
            var size=g.MeasureString(status,f);var rect=new Rectangle(area.Left+(area.Width-(int)size.Width)/2-20,area.Top+35,(int)size.Width+40,42);using var bg=new SolidBrush(Color.FromArgb(220,30,30,30));using var path=IconPainter.Round(rect,8);g.FillPath(bg,path);g.DrawString(status,f,Brushes.White,rect.Left+20,rect.Top+11);
        }
        base.OnPaint(e);
    }
    void DrawMagnifier(Graphics g)
    {
        int x=mouse.X+24,y=mouse.Y+25;
        var area=Screen.FromPoint(new Point(mouse.X+desktop.X,mouse.Y+desktop.Y)).Bounds;area.Offset(-desktop.X,-desktop.Y);
        if(x+142>area.Right)x=mouse.X-158;if(y+205>area.Bottom)y=mouse.Y-222;x=Math.Max(area.Left+4,x);y=Math.Max(area.Top+4,y);
        var box=new Rectangle(x,y,140,198);using var path=IconPainter.Round(box,7);g.FillPath(Brushes.White,path);g.DrawPath(Pens.LightGray,path);
        var old=g.Save();g.SetClip(new Rectangle(x+4,y+4,132,132));g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
        g.DrawImage(screen,new Rectangle(x+4,y+4,132,132),new Rectangle(mouse.X-8,mouse.Y-8,17,17),GraphicsUnit.Pixel);g.Restore(old);
        using var green=new Pen(IconPainter.Green);g.DrawLine(green,x+70,y+4,x+70,y+136);g.DrawLine(green,x+4,y+70,x+136,y+70);
        var color=PixelColor();using var f=new Font("Microsoft YaHei UI",10,FontStyle.Regular,GraphicsUnit.Pixel);
        g.DrawString($"坐标     {mouse.X+desktop.X}, {mouse.Y+desktop.Y}",f,Brushes.Black,x+8,y+145);
        g.DrawString($"色值     #{color.R:X2}{color.G:X2}{color.B:X2}",f,Brushes.Black,x+8,y+162);g.DrawString("按 Ctrl+C 复制色值",f,Brushes.Gray,x+8,y+179);
    }
    Color PixelColor()=>screen.GetPixel(Math.Clamp(mouse.X,0,screen.Width-1),Math.Clamp(mouse.Y,0,screen.Height-1));
    Dictionary<string,Point> Handles(Rectangle r)=>new(){["NW"]=new(r.Left,r.Top),["N"]=new(r.Left+r.Width/2,r.Top),["NE"]=new(r.Right,r.Top),["E"]=new(r.Right,r.Top+r.Height/2),["SE"]=new(r.Right,r.Bottom),["S"]=new(r.Left+r.Width/2,r.Bottom),["SW"]=new(r.Left,r.Bottom),["W"]=new(r.Left,r.Top+r.Height/2)};
    string HitHandle(Point p){foreach(var pair in Handles(selection))if(Math.Abs(p.X-pair.Value.X)<=7 && Math.Abs(p.Y-pair.Value.Y)<=7)return pair.Key;return "";}
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);Focus();mouse=e.Location;
        if(e.Button==MouseButtons.Right){if(selected){CommitText();selected=false;marks.Clear();redo.Clear();tool=Tool.None;undoButton.Enabled=false;foreach(var button in toolButtons.Values){button.Selected=false;button.Invalidate();}toolbar.Hide();options.Hide();hover=HoverBounds(mouse);Invalidate();}else Close();return;}
        if(e.Button!=MouseButtons.Left)return;
        CommitText();anchor=e.Location;original=selection;
        if(!selected){gesture="select";selection=new Rectangle(anchor,Size.Empty);Capture=true;return;}
        handle=HitHandle(e.Location);
        if(handle.Length>0 && tool==Tool.None){gesture="resize";Capture=true;return;}
        if(!selection.Contains(e.Location))return;
        if(tool==Tool.None){gesture="move";Capture=true;return;}
        if(tool==Tool.Text){BeginText(e.Location);return;}
        current=new Annotation{Tool=tool,Color=ink,Width=lineWidth,FontSize=tool==Tool.Emoji?36:fontSize,Text=tool==Tool.Emoji?emoji:""};current.Points.Add(e.Location);
        if(tool==Tool.Emoji){marks.Add(current);current=null;Changed();return;}
        current.Points.Add(e.Location);gesture="draw";Capture=true;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);mouse=e.Location;
        switch(gesture)
        {
            case "select":selection=SelectionMath.Normalize(anchor,e.Location,screen.Size);selected=selection.Width>2 && selection.Height>2;toolbar.Hide();options.Hide();break;
            case "move":
                var moved=SelectionMath.Move(original,new Point(e.X-anchor.X,e.Y-anchor.Y),screen.Size);var delta=new Size(moved.X-selection.X,moved.Y-selection.Y);
                foreach(var mark in marks.Concat(redo))for(int i=0;i<mark.Points.Count;i++)mark.Points[i]=Point.Add(mark.Points[i],delta);
                selection=moved;PositionToolbar();break;
            case "resize":selection=SelectionMath.Resize(original,e.Location,handle,screen.Size);PositionToolbar();break;
            case "draw":
                var p=new Point(Math.Clamp(e.X,selection.Left,selection.Right-1),Math.Clamp(e.Y,selection.Top,selection.Bottom-1));
                if(current!=null){if(tool is Tool.Pen or Tool.Mosaic)current.Points.Add(p);else current.Points[^1]=p;}break;
            default:
                if(!selected)hover=HoverBounds(e.Location);
                else if(tool==Tool.None){string h=HitHandle(e.Location);Cursor=h is "NW" or "SE"?Cursors.SizeNWSE:h is "NE" or "SW"?Cursors.SizeNESW:h is "N" or "S"?Cursors.SizeNS:h is "E" or "W"?Cursors.SizeWE:selection.Contains(e.Location)?Cursors.SizeAll:Cursors.Default;}
                break;
        }
        Invalidate();
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);if(e.Button!=MouseButtons.Left || gesture.Length==0)return;
        if(gesture=="select" && (selection.Width<3 || selection.Height<3)){selection=HoverBounds(e.Location);selected=true;}
        if(gesture=="draw" && current!=null){marks.Add(current);current=null;Changed();}
        gesture="";Capture=false;PositionToolbar();Invalidate();
    }
    protected override void OnMouseDoubleClick(MouseEventArgs e){base.OnMouseDoubleClick(e);if(selected && tool==Tool.None && selection.Contains(e.Location))Complete(CaptureAction.Copy);}
    void BeginText(Point p)
    {
        editor=new TextBox{Multiline=true,BorderStyle=BorderStyle.FixedSingle,ForeColor=ink,BackColor=Color.White,Location=p,Width=Math.Min(260,Math.Max(30,selection.Right-p.X)),Height=Math.Min(90,Math.Max(30,selection.Bottom-p.Y)),Font=new Font("Microsoft YaHei UI",fontSize,FontStyle.Regular,GraphicsUnit.Pixel),AccessibleName="标注文字"};
        editor.LostFocus+=(_,_)=>{if(!leaving)CommitText();};
        editor.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Escape){editor.Text="";CommitText();e.SuppressKeyPress=true;}else if(e.Control && e.KeyCode==Keys.Enter){CommitText();e.SuppressKeyPress=true;}};
        Controls.Add(editor);editor.BringToFront();editor.Focus();
    }
    void CommitText()
    {
        if(editor==null)return;var box=editor;editor=null;
        if(!string.IsNullOrWhiteSpace(box.Text)){var mark=new Annotation{Tool=Tool.Text,Color=box.ForeColor,FontSize=box.Font.Size,Text=box.Text,TextWidth=Math.Max(1,box.ClientSize.Width-4)};mark.Points.Add(box.Location);marks.Add(mark);Changed();}
        Controls.Remove(box);box.Dispose();Invalidate();
    }
    void Changed(){redo.Clear();undoButton.Enabled=marks.Count>0;Invalidate();}
    void Undo(){CommitText();if(marks.Count>0){redo.Push(marks[^1]);marks.RemoveAt(marks.Count-1);}undoButton.Enabled=marks.Count>0;Invalidate();}
    void Redo(){if(redo.Count>0){marks.Add(redo.Pop());undoButton.Enabled=true;Invalidate();}}
    internal Bitmap RenderSelection()
    {
        return ImageRenderer.Render(screen,selection,marks);
    }
    void Complete(CaptureAction action)
    {
        if(!selected || leaving)return;CommitText();
        if(action==CaptureAction.Save)
        {
            try
            {
                using var output=RenderSelection();TopMost=false;
                if(ImageOutput.Save(output,this)){leaving=true;Close();Saved?.Invoke();}
            }
            catch(Exception ex){MessageBox.Show(this,ex.Message,"保存图片失败",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
            finally{if(!IsDisposed){TopMost=true;Activate();Invalidate();}}
            return;
        }
        try{var image=RenderSelection();leaving=true;var origin=new Point(desktop.X+selection.X,desktop.Y+selection.Y);Hide();Close();if(Captured!=null)Captured(image,action,origin);else image.Dispose();}
        catch(Exception ex){leaving=false;MessageBox.Show(this,ex.Message,"截图失败",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
    {
        if(editor!=null)return base.ProcessCmdKey(ref msg,keyData);
        switch(keyData)
        {
            case Keys.Escape:Close();return true;
            case Keys.Enter:Complete(CaptureAction.Copy);return true;
            case Keys.Control|Keys.C:
                if(selected)Complete(CaptureAction.Copy);else{var c=PixelColor();try{Clipboard.SetText($"#{c.R:X2}{c.G:X2}{c.B:X2}");status="色值已复制";}catch{status="剪贴板暂时被占用，请重试";}Invalidate();}return true;
            case Keys.Control|Keys.S:Complete(CaptureAction.Save);return true;
            case Keys.Control|Keys.Z:Undo();return true;
            case Keys.Control|Keys.Y:Redo();return true;
            case Keys.Control|Keys.Shift|Keys.Z:Redo();return true;
            case Keys.Tab:SelectTool(Tool.None);return true;
        }
        Keys key=keyData&Keys.KeyCode;
        if(selected && key is Keys.Left or Keys.Right or Keys.Up or Keys.Down)
        {
            int d=(keyData&Keys.Shift)!=0?10:1;
            selection=SelectionMath.Move(selection,new Point(key==Keys.Left?-d:key==Keys.Right?d:0,key==Keys.Up?-d:key==Keys.Down?d:0),screen.Size);PositionToolbar();Invalidate();return true;
        }
        return base.ProcessCmdKey(ref msg,keyData);
    }
}
