namespace WeShot;
internal static class AppLog
{
    public static void Write(string message)
    {
        string path=Environment.GetEnvironmentVariable("WESHOT_DIAGNOSTICS")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WeShot","diagnostics.log");
        try{Directory.CreateDirectory(Path.GetDirectoryName(path)!);if(File.Exists(path) && new FileInfo(path).Length>1024*1024)File.WriteAllText(path,"");File.AppendAllText(path,$"{DateTime.UtcNow:O} {message}{Environment.NewLine}");}catch{}
    }
}
