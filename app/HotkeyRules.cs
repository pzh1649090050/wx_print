namespace WeShot;

internal static class HotkeyRules
{
    public static bool IsValid(Keys value)
    {
        var key=value&Keys.KeyCode;
        if((int)key<1 || (int)key>254 || key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu
            or Keys.LControlKey or Keys.RControlKey or Keys.LShiftKey or Keys.RShiftKey
            or Keys.LMenu or Keys.RMenu or Keys.LWin or Keys.RWin)return false;
        if((value&~(Keys.KeyCode|Keys.Control|Keys.Alt|Keys.Shift))!=0)return false;
        return (value&(Keys.Control|Keys.Alt))!=0 || (key>=Keys.F1 && key<=Keys.F24) || key==Keys.PrintScreen;
    }
}
