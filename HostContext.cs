using System.Text.Json;
namespace WeShot;

internal sealed class HostContext:ApplicationContext
{
    readonly NotifyIcon tray;
    readonly LauncherForm launcher;
    readonly HotkeyWindow hotkeys;
    readonly EventWaitHandle signal=new(false,EventResetMode.AutoReset,"Local\\WeShot.Standalone.Capture");
    readonly RegisteredWaitHandle waiter;
    readonly string settingsPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WeShot","settings.json");
    CaptureForm? capture;
    bool starting,quitting,restoreMain,editingHotkey;
    Keys hotkey=Keys.Control|Keys.Shift|Keys.A;
    public string HotkeyLabel=>new KeysConverter().ConvertToString(hotkey)??"Ctrl+Shift+A";
    public Keys Hotkey=>hotkey;
    public string HotkeyStatus {get;private set;}="";
    public HostContext(bool immediate)
    {
        try{if(File.Exists(settingsPath)){var saved=JsonSerializer.Deserialize<Dictionary<string,int>>(File.ReadAllText(settingsPath));if(saved!=null && saved.TryGetValue("hotkey",out int key) && HotkeyRules.IsValid((Keys)key))hotkey=(Keys)key;}}catch{}
        hotkeys=new HotkeyWindow(()=>{_ = StartCapture();});
        launcher=new LauncherForm(this);launcher.Show();
        var menu=new ContextMenuStrip();menu.Items.Add("开始截图",null,async(_,_)=>await StartCapture());menu.Items.Add("主窗口 / 快捷键",null,(_,_)=>{launcher.Show();launcher.WindowState=FormWindowState.Normal;launcher.Activate();});menu.Items.Add(new ToolStripSeparator());menu.Items.Add("退出",null,(_,_)=>Quit());
        tray=new NotifyIcon{Icon=IconPainter.AppIcon(),Text="独立截图 · "+HotkeyLabel,ContextMenuStrip=menu,Visible=true};tray.DoubleClick+=async(_,_)=>await StartCapture();
        bool registered=hotkeys.Register(hotkey);if(!registered)launcher.SetStatus("快捷键被其他程序占用；可点击开始截图，或设置新组合。");
        waiter=ThreadPool.RegisterWaitForSingleObject(signal,(_,_)=>{if(!quitting && !launcher.IsDisposed)try{launcher.BeginInvoke(new Action(async()=>await StartCapture()));}catch(InvalidOperationException){}},null,Timeout.Infinite,false);
        if(immediate)launcher.BeginInvoke(new Action(async()=>await StartCapture()));
    }
    public bool SetHotkey(Keys value)
    {
        if(!HotkeyRules.IsValid(value)){HotkeyStatus="普通按键需要搭配 Ctrl 或 Alt；也可使用功能键或 PrintScreen。";return false;}
        hotkeys.Unregister();if(!hotkeys.Register(value)){if(!editingHotkey)hotkeys.Register(hotkey);HotkeyStatus="该快捷键被其他程序占用或被系统保留，请换一个组合。";return false;}
        hotkey=value;tray.Text="独立截图 · "+HotkeyLabel;
        HotkeyStatus="快捷键已保存："+HotkeyLabel;
        try{Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);File.WriteAllText(settingsPath,JsonSerializer.Serialize(new Dictionary<string,int>{{"hotkey",(int)value}}));}catch(Exception ex){HotkeyStatus="快捷键已生效，但未能保存："+ex.Message;}return true;
    }
    public void BeginHotkeyEdit(){editingHotkey=true;hotkeys.Unregister();}
    public void EndHotkeyEdit(){hotkeys.Unregister();editingHotkey=false;if(!hotkeys.Register(hotkey))launcher.SetStatus("快捷键被其他程序占用，请重新修改。");}
    public void Notify(string message){if(!quitting)tray.ShowBalloonTip(2000,"独立截图",message,ToolTipIcon.Info);}
    public async Task StartCapture()
    {
        AppLog.Write($"capture request: starting={starting}, active={capture!=null}");
        if(starting || capture!=null || quitting || editingHotkey)return;starting=true;restoreMain=launcher.Visible && launcher.RestoreAfterCapture;launcher.Hide();
        try
        {
            await Task.Delay(180);if(quitting)return;
            var bounds=SystemInformation.VirtualScreen;AppLog.Write($"desktop bounds={bounds}");var windows=Native.WindowBounds(bounds);var bitmap=DesktopCapture.Grab(bounds);AppLog.Write("desktop captured");
            var form=new CaptureForm(bitmap,bounds,windows);capture=form;form.Captured+=OnCaptured;form.Saved+=()=>{Notify("图片已保存");if(restoreMain){launcher.Show();launcher.Activate();}};form.FormClosed+=(_,_)=>{AppLog.Write("capture closed");capture=null;if(!quitting && !form.HasResult && restoreMain){launcher.Show();launcher.Activate();}};form.Show();form.Activate();AppLog.Write($"capture shown: visible={form.Visible}, bounds={form.Bounds}, handle={form.Handle}");
        }
        catch(Exception ex){AppLog.Write(ex.ToString());launcher.Show();launcher.SetStatus("截图失败："+ex.Message);}
        finally{starting=false;}
    }
    void OnCaptured(Bitmap image,CaptureAction action,Point origin)
    {
        AppLog.Write($"capture result: action={action}, size={image.Size}");
        try
        {
            switch(action)
            {
                case CaptureAction.Pin:new PinForm(image,origin).Show();image=null!;break;
                case CaptureAction.Ocr:
                case CaptureAction.Translate:new OcrForm(image,action==CaptureAction.Translate).Show();image=null!;break;
                case CaptureAction.Long:new LongCaptureForm(image,origin).Show();image=null!;break;
                case CaptureAction.Save:if(ImageOutput.Save(image))Notify("图片已保存");break;
                default:ImageOutput.Copy(image);Notify(action==CaptureAction.Forward?"图片已复制，请在目标应用粘贴转发。":"截图已复制");break;
            }
            if(restoreMain){launcher.Show();launcher.Activate();}
        }
        catch(Exception ex){launcher.Show();MessageBox.Show(launcher,ex.Message,"截图结果处理失败",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        finally{image?.Dispose();}
    }
    public void Quit(){if(quitting)return;quitting=true;launcher.Exiting=true;tray.Visible=false;waiter.Unregister(null);signal.Dispose();hotkeys.Dispose();foreach(Form form in Application.OpenForms.Cast<Form>().ToArray()){form.Close();form.Dispose();}ExitThread();}
    protected override void Dispose(bool disposing){if(disposing){tray.Dispose();hotkeys.Dispose();}base.Dispose(disposing);}
}

internal sealed class HotkeyWindow:NativeWindow,IDisposable
{
    readonly Action action;
    public HotkeyWindow(Action action){this.action=action;CreateHandle(new CreateParams{Caption="WeShot hotkey"});}
    public bool Register(Keys keys)
    {
        uint modifiers=0x4000;if((keys&Keys.Control)!=0)modifiers|=2;if((keys&Keys.Shift)!=0)modifiers|=4;if((keys&Keys.Alt)!=0)modifiers|=1;
        return Native.RegisterHotKey(Handle,1,modifiers,(uint)(keys&Keys.KeyCode));
    }
    public void Unregister(){if(Handle!=IntPtr.Zero)Native.UnregisterHotKey(Handle,1);}
    protected override void WndProc(ref Message m){if(m.Msg==0x0312){action();return;}base.WndProc(ref m);}
    public void Dispose(){Unregister();if(Handle!=IntPtr.Zero)DestroyHandle();}
}
