using System.Drawing.Drawing2D;
internal static class Fixture
{
    [STAThread]static void Main(){Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();Application.Run(new Canvas());}
    sealed class Canvas:Form
    {
        public Canvas(){Text="截图验收画布";StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(900,650);BackColor=Color.White;var panel=new Panel{Dock=DockStyle.Fill,AutoScroll=true,AutoScrollMinSize=new Size(0,4000)};panel.Paint+=(_,e)=>Draw(e.Graphics,panel.AutoScrollPosition);Controls.Add(panel);}
        static void Draw(Graphics g,Point offset)
        {
            g.TranslateTransform(offset.X,offset.Y);g.SmoothingMode=SmoothingMode.AntiAlias;using var heading=new Font("Microsoft YaHei UI",26,FontStyle.Bold);using var body=new Font("Microsoft YaHei UI",16);
            g.DrawString("独立截图 · 验收画布",heading,Brushes.Black,35,25);g.DrawString("WESHOT 12345  |  中文识别与像素尺寸",body,Brushes.DimGray,35,83);
            for(int i=0;i<32;i++){int y=140+i*110;using var fill=new SolidBrush(Color.FromArgb(220+(i*7)%30,230+(i*3)%20,225+(i*11)%25));g.FillRectangle(fill,35,y,795,92);using var ink=new SolidBrush(Color.FromArgb(30+(i*17)%150,45+(i*31)%130,70+(i*13)%140));g.FillRectangle(ink,52,y+20,54+i%4*8,48);g.DrawString($"第 {i+1:D2} 行    内容编号 {(i+1)*173:D5}",body,Brushes.Black,140,y+15);g.DrawString($"截图、矩形、箭头、马赛克  /  Row {(i+1)*241}",body,Brushes.DimGray,140,y+49);}
        }
    }
}
