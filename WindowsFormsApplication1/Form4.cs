using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;  //路径绘图


namespace WindowsFormsApplication1
{
    public partial class Form4 : Form
    {
        private const int Unit_lenth = 32;//单位格大小
        private int Drawstep = 8; //默认的绘制单位
        private const int Y_MAX = 512;//y轴最大值
        private const int Max_step = 33;//最大绘制单位
        private const int Min_step = 1; //最小绘制单位
        private const int Startprint = 32;//点坐标偏移量 
        private List<byte> Datalist = new List<byte>();//数据结构------线性链表 ，存放接收到的数据

        private Pen AxislinesPen = new Pen(Color.FromArgb(0xff, 0x00,0x00)); //轴线颜色(黑)
        private Pen WavelinsPen = new Pen(Color.FromArgb(0x00, 0xff, 0x00)); //波形颜色（红）

        bool key_shift, key_serialopen, key_serialclose, key_up, key_down, key_exit;
        //public ShowWindow ShowMainWindow;
        //public HideWindow HideMainWindow;
        //public OpenPort OpenSerialPort;
        //public ClosePort CloseSerialPort;
        //public GetMainPos GetMainFormPos;
        //public GetMainWidth GetMainFormWidth;

        public Form4()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint 
                | ControlStyles.AllPaintingInWmPaint, true);//开启双缓冲，防止数据刷新时闪烁
            this.UpdateStyles();
            InitializeComponent();
            this.BackColor = Color.Black;

        }

        private void Form4_Load(object sender, EventArgs e)
        {
            ResData();
        }


        /**/
        public void AddData(byte[] Data)
        {
            for (int i = 0; i < Data.Length; i++)
            {
                Datalist.Add(Data[i]);  //链表尾部添加数据
            }
            Invalidate();//刷新显示
        }      
        public void ResData()
        {
            try
            {
                byte[] data = new byte[Form1.serialPort1.BytesToRead];
                if (Form1.serialPort1.IsOpen)
                    Form1.serialPort1.Read(data, 0, data.Length);
                AddData(data);
            }
            catch
            { }
        }
        /**/

        private void Draw_Paint(object sender, PaintEventArgs e)
        {
            ResData(); 
            int x, y;
            string str;
            Graphics g = e.Graphics;
            Rectangle rect = new Rectangle(0, Startprint, ClientRectangle.Width, ClientRectangle.Height / 2);
            GraphicsPath path = new GraphicsPath(new Point[]{new Point(0,16),
                                                              //new Point(50,50),
                                                              //new Point(10,80),
                                                              //new Point(50,100),
                                                              new Point(0,448) },
                                                  new byte[]{(byte)PathPointType.Start,
                                                             //(byte)PathPointType.Line,
                                                             //(byte)PathPointType.Line,
                                                             //(byte)PathPointType.Line,
                                                             (byte)PathPointType.CloseSubpath });

            GraphicsPath gp_x = new GraphicsPath();
            GraphicsPath gp_y = new GraphicsPath();
            ResData();          
            try
            {
                int num_x = this.ClientRectangle.Width / Unit_lenth;   //27
                int num_y = this.ClientRectangle.Height / Unit_lenth;  //17
                int list_count = (this.ClientRectangle.Width - Startprint);  //852
                
                for(y = 0; y < this.ClientRectangle.Width / Unit_lenth; y++) //纵轴线
                {
                    g.DrawLine( AxislinesPen, 
                                Startprint + y * Unit_lenth,
                                Startprint, 
                                Startprint+ y * Unit_lenth,  
                                Y_MAX + Startprint );

                    //添加横轴时间
                    if ((y * Drawstep) / 10 == 0)
                    {
                        gp_x.AddString((y * Drawstep).ToString(),
                                      this.Font.FontFamily,
                                      (int)FontStyle.Regular,
                                      12,
                        new RectangleF(Startprint + y * Unit_lenth - 7,
                                       this.ClientRectangle.Height - Startprint + 10,
                                       400, 50),
                                      null);
                    }
                    else if (((y * Drawstep) / 10 != 0) && ((y * Drawstep) / 100 == 0))
                    {
                        gp_x.AddString((y * Drawstep).ToString(),
                                      this.Font.FontFamily,
                                      (int)FontStyle.Regular,
                                      12,
                        new RectangleF(Startprint + y * Unit_lenth - 9,
                                       this.ClientRectangle.Height - Startprint + 10,
                                       400, 50),
                                      null); 
                    }
                    else if (((y * Drawstep) / 100 != 0))
                    {
                        gp_x.AddString((y * Drawstep).ToString(),
                                       this.Font.FontFamily,
                                       (int)FontStyle.Regular,
                                       12,
                             new RectangleF(Startprint + y * Unit_lenth - 11,
                                            this.ClientRectangle.Height - Startprint + 10,
                                            400, 50),
                                       null);  
                    }
                }
                for (x = 0; x < this.ClientRectangle.Height / Unit_lenth; x++)  //横轴线
                {
                    g.DrawLine( AxislinesPen, 
                                Startprint, 
                                (x + 1) * Unit_lenth, 
                                this.ClientRectangle.Width, 
                                (x + 1) * Unit_lenth );

                    str = ((16 - x) * 16).ToString("X");
                    str = "0x" + (str.Length == 1 ? str + "0" : str);
                    if (x == 0)
                        str = "0xFF";
                    if (x == 17)
                        break;
                    gp_y.AddString(str,                                   
                                   this.Font.FontFamily,
                                   (int)FontStyle.Regular, 
                                   12,
                           new RectangleF(0,
                              Startprint + x * Unit_lenth - 8, 
                               400, 
                               50),
                                   null);
                }
                e.Graphics.DrawPath(Pens.White, gp_x);
                e.Graphics.DrawPath(Pens.White, gp_y);
            }
            catch 
            { }

           // 如果数据量大于可容纳的数据量，删除最左的数据   (107)
            if (Datalist.Count - 1 >= (this.ClientRectangle.Width - Startprint) / Drawstep)
            {
                Datalist.RemoveRange(0, Datalist.Count - ((this.ClientRectangle.Width - Startprint) / Drawstep - 1));
            }

            for (int i = 0; i < Datalist.Count - 1; i++)
            {
                g.DrawLine(WavelinsPen, Startprint + i * Drawstep, 17 * Unit_lenth - Datalist[i] * 2, Startprint + (i + 1) * Drawstep, 17 * Unit_lenth - Datalist[i + 1] * 2);
            }

           
        }


        //private void button1_Click(object sender, EventArgs e)
        //{
        //    OpenFileDialog dlg = new OpenFileDialog();
        //    dlg.Title = "open file";
        //    dlg.Filter = "Text documents(*.txt)|*.txt|All Files|*.*";
        //    dlg.ShowDialog();
        //}



        /*快捷键设置*/
        private void keys_down(object sender, KeyEventArgs e) //按键按下
        {
            if (e.Shift)  //shift功能键按下
            {
                key_shift = true;
            }
            switch (e.KeyCode)
            {
                case Keys.E:   //退出波形显示
                    key_exit = true;
                    break;
                case Keys.O:   //打开串口
                    key_serialopen = true;
                    break;
                case Keys.C:   //关闭串口
                    key_serialclose = true;
                    break;
                case Keys.Up:  //波形放大
                    key_up = true;
                    break;
                case Keys.Down: //波形缩小
                    key_down = true;
                    break;
                default:
                    break;
            }
        }

        private void keys_up(object sender, KeyEventArgs e)//按键松开
        {
            if (key_shift)
            {
                if (key_serialopen)
                {
                    Form1.serialPort1.Open();
                    Invalidate();
                    key_serialopen = false;
                }
                else if (key_serialclose)
                {
                    Form1.serialPort1.Close();
                    key_serialclose = false;
                }
                else if (key_exit)
                {
                    this.Close();
                    key_exit = false;
                }
                else if (key_up)
                {
                    if (Drawstep < Max_step)
                        Drawstep++;
                    Invalidate();
                    key_up = false;
                }
                else if (key_down)
                {
                    if (Drawstep > Min_step)
                        Drawstep--;
                    Invalidate();
                    key_down = false;
                }
            }
            else
            {             
                key_serialopen = false;
                key_serialclose = false;
                key_exit = false;
                key_up = false;
                key_down = false;
            }
            key_shift = false;
        }
    }
}
