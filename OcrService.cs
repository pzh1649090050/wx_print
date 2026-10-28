using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;

namespace WeShot;

internal static class OcrService
{
    public static string[] Languages()=>OcrEngine.AvailableRecognizerLanguages.Select(l=>l.LanguageTag).ToArray();
    public static async Task<string> Recognize(Bitmap image,string? language=null)
    {
        OcrEngine? engine=language==null?OcrEngine.TryCreateFromUserProfileLanguages():OcrEngine.TryCreateFromLanguage(new Language(language));
        if(engine==null){var first=OcrEngine.AvailableRecognizerLanguages.FirstOrDefault();if(first!=null)engine=OcrEngine.TryCreateFromLanguage(first);}
        if(engine==null)throw new InvalidOperationException("系统尚未安装 OCR 语言。请在 Windows 设置中为中文或英语安装“基本输入/光学字符识别”语言组件后重试。");
        string path=Path.Combine(Path.GetTempPath(),"weshot-"+Guid.NewGuid().ToString("N")+".png");
        try
        {
            int max=(int)OcrEngine.MaxImageDimension;double scale=Math.Min(1,Math.Min(max/(double)image.Width,max/(double)image.Height));
            using(var scaled=new Bitmap(image,Math.Max(1,(int)(image.Width*scale)),Math.Max(1,(int)(image.Height*scale))))scaled.Save(path,System.Drawing.Imaging.ImageFormat.Png);
            var file=await StorageFile.GetFileFromPathAsync(path);using var stream=await file.OpenReadAsync();var decoder=await BitmapDecoder.CreateAsync(stream);
            using var bitmap=await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied);
            var result=await engine.RecognizeAsync(bitmap);return string.Join(Environment.NewLine,result.Lines.Select(l=>l.Text));
        }
        finally{try{File.Delete(path);}catch(IOException){}}
    }
}

internal sealed class OcrForm:Form
{
    readonly Bitmap image;
    readonly TextBox output=new(){Multiline=true,ScrollBars=ScrollBars.Both,WordWrap=true,Dock=DockStyle.Fill,Font=new Font("Microsoft YaHei UI",11),BorderStyle=BorderStyle.FixedSingle,AccessibleName="识别文本"};
    readonly Label status=new(){Dock=DockStyle.Bottom,Height=36,Padding=new Padding(12,9,0,0),ForeColor=Color.DimGray,Text="正在使用 Windows 本地 OCR…"};
    readonly Button retry=new(){Text="重新识别",AutoSize=true};
    readonly ComboBox languages=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=130};
    readonly Button copy=new(){Text="复制文字",AutoSize=true,BackColor=IconPainter.Green,ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Enabled=false};
    readonly Button web=new(){Text="在浏览器翻译",AutoSize=true,Enabled=false};
    bool busy;
    public OcrForm(Bitmap image,bool translate)
    {
        this.image=image;Text=translate?"翻译 · 独立截图":"提取文字 · 独立截图";Icon=IconPainter.AppIcon();Size=new Size(680,580);MinimumSize=new Size(480,360);StartPosition=FormStartPosition.CenterScreen;BackColor=Color.White;Padding=new Padding(16);Font=new Font("Microsoft YaHei UI",9);
        var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=42,WrapContents=false};top.Controls.Add(new Label{Text="识别语言",AutoSize=true,Padding=new Padding(0,7,5,0)});top.Controls.Add(languages);top.Controls.Add(retry);
        languages.Items.Add("自动");foreach(var l in OcrService.Languages())languages.Items.Add(l);languages.SelectedIndex=0;
        var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=translate?76:42,WrapContents=true,Padding=new Padding(0,8,0,0)};
        copy.Click+=(_,_)=>{try{if(output.Text.Length>0)Clipboard.SetText(output.Text);status.Text="文字已复制";}catch(Exception ex){status.Text=ex.Message;}};bottom.Controls.Add(copy);
        web.Click+=(_,_)=>{if(string.IsNullOrWhiteSpace(output.Text))return;try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://www.bing.com/translator?from=auto&to=zh-Hans&text="+Uri.EscapeDataString(output.Text[..Math.Min(1000,output.Text.Length)])){UseShellExecute=true});}catch(Exception ex){status.Text=ex.Message;}};
        if(translate){bottom.Controls.Add(web);bottom.Controls.Add(new Label{Text="点击后将前 1000 字发送至 Bing 在线翻译；识别图片始终留在本机。",AutoSize=true,ForeColor=Color.DimGray,Margin=new Padding(0,8,0,0)});}
        Controls.Add(output);Controls.Add(status);Controls.Add(bottom);Controls.Add(top);retry.Click+=async(_,_)=>await Recognize();Shown+=async(_,_)=>await Recognize();FormClosed+=(_,_)=>image.Dispose();FormClosing+=(_,e)=>{if(busy)e.Cancel=true;};
    }
    async Task Recognize()
    {
        if(busy)return;busy=true;retry.Enabled=false;languages.Enabled=false;copy.Enabled=false;web.Enabled=false;status.Text="正在使用 Windows 本地 OCR…";
        try{output.Text=await OcrService.Recognize(image,languages.SelectedIndex>0?languages.Text:null);status.Text=output.Text.Length==0?"未识别到文字，请选择更清晰的文字区域。":$"已识别 {output.Text.Length} 字 · 图片未上传";}
        catch(Exception ex){output.Clear();status.Height=76;status.Text="识别失败："+ex.Message;}
        finally{busy=false;retry.Enabled=true;languages.Enabled=true;copy.Enabled=!string.IsNullOrWhiteSpace(output.Text);web.Enabled=copy.Enabled;}
    }
}
