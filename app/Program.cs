namespace WeShot;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        using var mutex=new Mutex(true,"Local\\WeShot.Standalone.Instance",out bool first);
        if(!first){try{using var signal=EventWaitHandle.OpenExisting("Local\\WeShot.Standalone.Capture");signal.Set();}catch(WaitHandleCannotBeOpenedException){}return;}
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        Application.ThreadException+=(_,e)=>MessageBox.Show(e.Exception.Message,"独立截图",MessageBoxButtons.OK,MessageBoxIcon.Error);
        using var host=new HostContext(args.Contains("--capture"));Application.Run(host);
    }
}
