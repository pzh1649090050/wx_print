namespace WeShot;

internal sealed class HotkeyBox:TextBox
{
    public bool Recording {get;set;}
    public event Action<Keys>? KeyRecorded;
    public event Action? Accepted;
    public event Action? Cancelled;
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
    {
        if(!Recording)return base.ProcessCmdKey(ref msg,keyData);
        if(keyData==Keys.Escape){Cancelled?.Invoke();return true;}
        if(keyData==Keys.Enter){Accepted?.Invoke();return true;}
        if(keyData==Keys.Tab || keyData==(Keys.Shift|Keys.Tab))return base.ProcessCmdKey(ref msg,keyData);
        Record(keyData);return true;
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if(Recording){Record(e.KeyData);e.SuppressKeyPress=true;}
        base.OnKeyDown(e);
    }
    void Record(Keys value)
    {
        var key=value&Keys.KeyCode;
        if(key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LControlKey or Keys.RControlKey
            or Keys.LShiftKey or Keys.RShiftKey or Keys.LMenu or Keys.RMenu)return;
        KeyRecorded?.Invoke(value);
    }
}
