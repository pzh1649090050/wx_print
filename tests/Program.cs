using WeShot;
using System.Drawing;

int count=0;
void Check(bool ok,string name){if(!ok)throw new Exception("失败："+name);Console.WriteLine("通过："+name);count++;}
Check(HotkeyRules.IsValid(System.Windows.Forms.Keys.F6),"允许单独功能键作为全局截图快捷键");
Check(HotkeyRules.IsValid(System.Windows.Forms.Keys.PrintScreen),"允许 PrintScreen 截图键");
Check(!HotkeyRules.IsValid(System.Windows.Forms.Keys.Control|System.Windows.Forms.Keys.ControlKey),"拒绝只有修饰键的无效快捷键");
Check(!HotkeyRules.IsValid(System.Windows.Forms.Keys.A),"拒绝会干扰普通输入的单独字母");
Check(HotkeyRules.IsValid(System.Windows.Forms.Keys.Control|System.Windows.Forms.Keys.Shift|System.Windows.Forms.Keys.Q),"允许 Ctrl+Shift 字母组合");
Check(HotkeyRules.IsValid(System.Windows.Forms.Keys.Control|System.Windows.Forms.Keys.A),"允许用户指定的 Ctrl+A 全局快捷键");
Check(SelectionMath.Normalize(new Point(90,80),new Point(10,20),new Size(100,100))==new Rectangle(10,20,80,60),"反向选区");
Check(SelectionMath.Normalize(new Point(-10,-20),new Point(130,120),new Size(100,100))==new Rectangle(0,0,100,100),"越界拖拽");
Check(SelectionMath.Move(new Rectangle(10,20,30,40),new Point(-50,100),new Size(100,100))==new Rectangle(0,60,30,40),"移动边界");
Check(SelectionMath.Resize(new Rectangle(10,20,30,40),new Point(200,200),"SE",new Size(100,100))==new Rectangle(10,20,90,80),"缩放边界");
using var page=new Bitmap(240,800);
using(var g=Graphics.FromImage(page)){g.Clear(Color.White);var rng=new Random(1928);for(int y=0;y<800;y+=4)for(int x=0;x<240;x+=4){using var b=new SolidBrush(Color.FromArgb(rng.Next(256),rng.Next(256),rng.Next(256)));g.FillRectangle(b,x,y,4,4);}}
using var a=page.Clone(new Rectangle(0,0,240,400),page.PixelFormat);
using var bframe=page.Clone(new Rectangle(0,220,240,400),page.PixelFormat);
using var joined=ImageStitcher.Append(a,bframe,out var added);
Check(added==220 && joined.Height==620,"识别180像素重叠并拼接");
Check(joined.GetPixel(64,600)==page.GetPixel(64,600),"拼接内容连续");
using var duplicate=ImageStitcher.Append(a,a,out var duplicateAdded);
Check(duplicateAdded==0 && duplicate.Height==400,"重复帧不追加");
using var unrelated=new Bitmap(240,400);using(var g=Graphics.FromImage(unrelated))g.Clear(Color.Black);
bool rejected=false;try{using var invalid=ImageStitcher.Append(a,unrelated,out _);}catch(InvalidOperationException){rejected=true;}
Check(rejected,"无可靠重叠时拒绝拼接");
using var source=new Bitmap(200,160);using(var g=Graphics.FromImage(source))g.Clear(Color.White);
var rectangle=new Annotation{Tool=Tool.Rectangle,Color=Color.Red,Width=3};rectangle.Points.Add(new Point(50,50));rectangle.Points.Add(new Point(120,110));
using var rendered=ImageRenderer.Render(source,new Rectangle(30,30,100,100),new[]{rectangle});
Check(rendered.Size==new Size(100,100),"导出保持选区像素尺寸");
Check(rendered.GetPixel(20,20).R>200 && rendered.GetPixel(20,20).G<100,"标注坐标转换到裁剪区域");
Check(rendered.GetPixel(0,0).ToArgb()==Color.White.ToArgb(),"导出不含绿色边框或灰色遮罩");
Check(source.GetPixel(50,50).ToArgb()==Color.White.ToArgb(),"标注不修改冻结原图");
using var privacy=new Bitmap(220,140);using(var g=Graphics.FromImage(privacy)){g.Clear(Color.White);g.FillRectangle(Brushes.Black,100,60,2,40);}
var mosaic=new Annotation{Tool=Tool.Mosaic,Width=3};mosaic.Points.Add(new Point(20,80));mosaic.Points.Add(new Point(200,80));
using var obscured=ImageRenderer.Render(privacy,new Rectangle(Point.Empty,privacy.Size),new[]{mosaic});
Check(obscured.GetPixel(100,80).ToArgb()!=Color.Black.ToArgb(),"快速拖动马赛克也连续覆盖中间区域");
using var clipped=ImageRenderer.Render(source,new Rectangle(-10,-20,60,80),Array.Empty<Annotation>());
Check(clipped.Size==new Size(50,60),"裁剪限制在源图范围内");
using var narrowA=new Bitmap(4,100);using var narrowB=new Bitmap(4,100);using(var g=Graphics.FromImage(narrowA))g.Clear(Color.White);using(var g=Graphics.FromImage(narrowB))g.Clear(Color.White);
using var narrowResult=ImageStitcher.Append(narrowA,narrowB,out int narrowAdded);
Check(narrowAdded==0 && narrowResult.Height==100,"窄区域重复帧正确识别");
using var sparseA=new Bitmap(240,400);using var sparseB=new Bitmap(240,400);using(var g=Graphics.FromImage(sparseA))g.Clear(Color.White);using(var g=Graphics.FromImage(sparseB)){g.Clear(Color.White);g.FillRectangle(Brushes.Black,20,12,200,9);}
bool sparseNotLost=false;try{using var sparseResult=ImageStitcher.Append(sparseA,sparseB,out int sparseAdded);sparseNotLost=sparseAdded>0;}catch(InvalidOperationException){sparseNotLost=true;}
Check(sparseNotLost,"新增稀疏文字不能被当成重复帧静默丢弃");
double longScale=WeShot.ImageLayout.FitScale(1,new Size(200,15000),new Size(1800,1040));
Check(longScale>0 && longScale<=1040d/15000,"超长贴图缩放不抛异常且不越过工作区");
if(OcrService.Languages().Length>0)
{
    using var textImage=new Bitmap(700,120);using(var g=Graphics.FromImage(textImage)){g.Clear(Color.White);using var font=new Font("Arial",40,FontStyle.Regular,GraphicsUnit.Pixel);g.DrawString("WESHOT 12345",font,Brushes.Black,15,30);}
    string text=await OcrService.Recognize(textImage,OcrService.Languages().FirstOrDefault(l=>l.StartsWith("en")));
    Check(text.Contains("12345"),"Windows OCR 识别真实生成图片");
}
else Console.WriteLine("跳过 OCR：系统未安装识别语言包");
Console.WriteLine($"{count} 项测试通过");
