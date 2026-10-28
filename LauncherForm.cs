namespace WeShot;

internal sealed class LauncherForm:Form
{
    readonly HostContext host;
    readonly HotkeyBox hotkey=new(){ReadOnly=true,ShortcutsEnabled=false,BorderStyle=BorderStyle.FixedSingle,TextAlign=HorizontalAlignment.Center,BackColor=Color.White,Font=new Font("Segoe UI",10),AccessibleName="截图快捷键"};
    readonly Button modify=new(){Text="修改",AccessibleName="修改截图快捷键",Bounds=new Rectangle(310,204,80,30),FlatStyle=FlatStyle.Flat};
    readonly Button reset=new(){Text="重置",Bounds=new Rectangle(402,204,86,30),FlatStyle=FlatStyle.Flat};
    Keys candidate;
    Button captureButton=null!;
    readonly Label status=new(){ForeColor=Color.FromArgb(130,130,130),AutoSize=false};
    readonly CheckBox restore=new(){Text="截图后显示主窗口",AutoSize=true,ForeColor=Color.DimGray};
    public bool Exiting {get;set;}
    public bool RestoreAfterCapture=>restore.Checked;
    public LauncherForm(HostContext host)
    {
        this.host=host;Text="独立截图";Icon=IconPainter.AppIcon();StartPosition=FormStartPosition.CenterScreen;
        AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(520,390);MinimumSize=Size;MaximumSize=Size;MaximizeBox=false;BackColor=Color.White;Font=new Font("Microsoft YaHei UI",9);
        var badge=new Panel{Bounds=new Rectangle(32,31,52,52),BackColor=Color.White};badge.Paint+=(_,e)=>{e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using var fill=new SolidBrush(IconPainter.Green);using var path=IconPainter.Round(new Rectangle(0,0,51,51),12);e.Graphics.FillPath(fill,path);IconPainter.Draw(e.Graphics,"Capture",new Rectangle(11,11,30,30),Color.White);};Controls.Add(badge);
        Controls.Add(new Label{Text="独立截图",Font=new Font("Microsoft YaHei UI",20,FontStyle.Bold),ForeColor=Color.FromArgb(30,30,30),Bounds=new Rectangle(98,29,370,38)});
        Controls.Add(new Label{Text="框选、标注，随手贴到桌面。",ForeColor=Color.Gray,Bounds=new Rectangle(100,70,370,24)});
        var capture=new Button{Text="开始截图",BackColor=IconPainter.Green,ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Font=new Font("Microsoft YaHei UI",14),Bounds=new Rectangle(32,120,456,56),Cursor=Cursors.Hand,AccessibleName="开始截图"};captureButton=capture;capture.FlatAppearance.BorderSize=0;capture.Click+=async(_,_)=>await host.StartCapture();Controls.Add(capture);
        Controls.Add(new Label{Text="全局快捷键",ForeColor=Color.DimGray,Bounds=new Rectangle(32,205,110,28),TextAlign=ContentAlignment.MiddleLeft});
        hotkey.Bounds=new Rectangle(155,206,145,28);hotkey.Text=host.HotkeyLabel;hotkey.Click+=(_,_)=>{if(!hotkey.Recording)EditHotkey();};
        hotkey.KeyRecorded+=value=>{if(HotkeyRules.IsValid(value)){candidate=value;hotkey.Text=new KeysConverter().ConvertToString(value);SetStatus("已录入，点击“保存”使快捷键生效；Esc 取消。");}else SetStatus("普通按键需要搭配 Ctrl 或 Alt；也支持 F1–F24、PrintScreen。");};
        hotkey.Accepted+=SaveHotkey;hotkey.Cancelled+=()=>FinishHotkeyEdit(false);Controls.Add(hotkey);
        modify.FlatAppearance.BorderColor=Color.FromArgb(220,220,220);modify.Click+=(_,_)=>{if(hotkey.Recording)SaveHotkey();else EditHotkey();};Controls.Add(modify);
        reset.FlatAppearance.BorderColor=Color.FromArgb(220,220,220);reset.Click+=(_,_)=>{if(hotkey.Recording){FinishHotkeyEdit(false);return;}host.SetHotkey(Keys.Control|Keys.Shift|Keys.A);hotkey.Text=host.HotkeyLabel;SetStatus(host.HotkeyStatus);};Controls.Add(reset);
        restore.Location=new Point(32,256);Controls.Add(restore);
        status.Bounds=new Rectangle(32,290,456,39);status.Text="点击“修改”设置组合键，也支持功能键和 PrintScreen。";Controls.Add(status);
        Controls.Add(new Panel{BackColor=Color.FromArgb(239,239,239),Bounds=new Rectangle(32,337,456,1)});
        Controls.Add(new Label{Text="关闭窗口后驻留托盘  ·  Esc 取消截图",ForeColor=Color.FromArgb(155,155,155),Bounds=new Rectangle(32,354,380,24)});
        var exit=new LinkLabel{Text="退出",Bounds=new Rectangle(442,354,48,24),LinkColor=Color.Gray};exit.LinkClicked+=(_,_)=>host.Quit();Controls.Add(exit);
        FormClosing+=(_,e)=>{if(!Exiting && e.CloseReason==CloseReason.UserClosing){if(hotkey.Recording)FinishHotkeyEdit(false);e.Cancel=true;Hide();host.Notify("已驻留系统托盘，双击托盘图标即可截图。");}};
    }
    public void SetStatus(string message){status.Text=message;}
    void EditHotkey()
    {
        host.BeginHotkeyEdit();candidate=host.Hotkey;hotkey.Recording=true;captureButton.Enabled=false;
        modify.Text="保存";modify.AccessibleName="保存截图快捷键";reset.Text="取消";
        hotkey.BackColor=Color.FromArgb(235,250,241);hotkey.Focus();
        SetStatus("请按下快捷键，再点击“保存”。编辑期间暂停截图热键，Esc 取消。");
    }
    void SaveHotkey()
    {
        if(!host.SetHotkey(candidate)){SetStatus(host.HotkeyStatus);hotkey.Focus();return;}
        FinishHotkeyEdit(true);
    }
    void FinishHotkeyEdit(bool saved)
    {
        hotkey.Recording=false;captureButton.Enabled=true;modify.Text="修改";modify.AccessibleName="修改截图快捷键";reset.Text="重置";
        hotkey.BackColor=Color.White;hotkey.Text=host.HotkeyLabel;
        SetStatus(saved?host.HotkeyStatus:"已取消修改，当前快捷键："+host.HotkeyLabel);host.EndHotkeyEdit();
    }
}
