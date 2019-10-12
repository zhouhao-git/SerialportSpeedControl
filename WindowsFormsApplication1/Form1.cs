
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;      //用于开发Windows应用程序messagebox\form等
using System.IO.Ports;          //用于操作文件
using System.Collections;
using System.Text.RegularExpressions; //命名空间，包含构造和执行正则表达式的类
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Diagnostics;//进程类对象
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;



namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {

        public static SerialPort serialPort1 = new SerialPort();

        Form4 F4;        
        int COM_MAX = 20;//COM口号最大值

        byte[] speedcmd_data  = new byte[9];
        byte[] acccmd_data = new byte[9];
        byte[] decccmd_data = new byte[9];
        byte[] power_OFF = new byte[9] {0x23, 0x0C, 0x20, 0x00, 0x00,0x00, 0x00, 0x00, 0x4F};
        byte[] power_ON = new byte[9] { 0x23, 0x0C, 0x20, 0x00, 0x01, 0x00, 0x00, 0x00, 0x50 };
        byte[] read_speed_cmd = new byte[9]{0x40, 0x03, 0x21, 0x01, 0x00, 0x00, 0x00,0x00, 0x65};
        byte[] read_vol_cmd = new byte[9] { 0x40, 0x0D, 0x21, 0x01, 0x00, 0x00, 0x00, 0x00, 0x6F };
        byte[] read_mostemp_cmd = new byte[9] { 0x40, 0x0F, 0x21, 0x01, 0x00, 0x00, 0x00, 0x00, 0x71 };
        byte[] read_mottemp_cmd = new byte[9] { 0x40, 0x0F, 0x21, 0x02, 0x00, 0x00, 0x00, 0x00, 0x72 };
        byte[] read_status_cmd = new byte[9] { 0x40, 0x11, 0x21, 0x00, 0x00, 0x00, 0x00, 0x00, 0x72 };
        byte[] read_error_cmd = new byte[9] { 0x40, 0x12, 0x21, 0x00, 0x00, 0x00, 0x00, 0x00, 0x73 };

        const byte WriteCmd = 0x23;        //写命令控制字
        //速度
        const byte Index_Low_speed = 0x00;
        const byte Index_High_speed = 0x20;
        const byte SubIndex_speed= 0x01;
        //加速度
        const byte Index_Low_acc = 0x01;
        const byte Index_High_acc = 0x20;
        const byte SubIndex_acc = 0x01;
        //减速度
        const byte Index_Low_dec = 0x02;
        const byte Index_High_dec = 0x20;
        const byte SubIndex_dec = 0x01;
        //紧急关机\使能
        const byte Index_Low_poweroff = 0x0C;
        const byte Index_High_poweroff = 0x20;
        const byte SubIndex_poweroff = 0x00;

        const byte ReadCmd = 0x43; //读参数控制字
        //读取速度
        const int Index_Rspeed = 0x2103;
        const byte SubIndex_Rspeed = 0x01;
        //读取电压
        const int Index_Rvol = 0x210D;
        const byte SubIndex_Rvol = 0x01;
        //读取温度
        const int Index_Rmostemp = 0x210F;
        const byte SubIndex_Rmostemp = 0x01;
        const int Index_Rmottemp = 0x210F;
        const byte SubIndex_Rmottemp = 0x02;
        //读取控制器状态
        const int Index_Rstatus = 0x2111;
        const byte SubIndex_Rstatus = 0x00;
        //读取错误信息
        const int Index_Rerror = 0x2112;
        const byte SubIndex_Rerror = 0x00;



        public Form1()
        {
            InitializeComponent();
            ShowPara_Init( );         /*显示参数初始化*/

            //由于串口组件双击不能添加串口接收事件，所以必须受到添加串口接收事件，如下语句。
            serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived);
            serialPort1.Encoding = Encoding.GetEncoding("GB2312"); //支持汉字显示

            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;          
        }




        private void Form1_Load(object sender, EventArgs e)
        {

            for (int i = 1; i < COM_MAX; i++)
            {
                comboBox1.Items.Add("COM" + i.ToString());
            }

            //设置默认值
            comboBox1.Text = "COM1";
            comboBox2.Text = "57600";
            comboBox3.Text = "8";
            comboBox4.Text = "NONE";
            comboBox5.Text = "1";


            ovalShape1.FillColor = Color.Gray;   /*初始化灯图形的颜色*/
            button12.BackColor = Color.Red;      /*初始化停止按钮的颜色*/
        }




        private void open_serialport_button_Click(object sender, EventArgs e)
        {
            if (!serialPort1.IsOpen)        /*如果串口未打开*/
            {
                try
                {
                    serialPort1.PortName = comboBox1.Text;
                    serialPort1.BaudRate = Convert.ToInt32(comboBox2.Text, 10);

                    float f = Convert.ToSingle(comboBox5.Text.Trim());
                    if (f == 0)//设置停止位
                        serialPort1.StopBits = StopBits.None;
                    else if (f == 1.5)
                        serialPort1.StopBits = StopBits.OnePointFive;
                    else if (f == 1)
                        serialPort1.StopBits = StopBits.One;
                    else if (f == 2)
                        serialPort1.StopBits = StopBits.Two;
                    else
                        serialPort1.StopBits = StopBits.One;

                    //设置数据位
                    serialPort1.DataBits = Convert.ToInt32(comboBox3.Text.Trim());

                    //设置奇偶校验位
                    string parity_bit = comboBox4.Text.Trim();
                    if (parity_bit.CompareTo("None") == 0)
                        serialPort1.Parity = Parity.None;
                    else if (parity_bit.CompareTo("Odd") == 0)
                        serialPort1.Parity = Parity.Odd;
                    else if (parity_bit.CompareTo("Even") == 0)
                        serialPort1.Parity = Parity.Even;
                    else
                        serialPort1.Parity = Parity.None;

                    serialPort1.Open();     //打开串口
                    ovalShape1.FillColor = Color.Red;
                    comboBox1.Enabled = false;//关闭使能
                    comboBox2.Enabled = false;
                    comboBox3.Enabled = false;
                    comboBox4.Enabled = false;
                    comboBox5.Enabled = false;
                    open_serialport_button.Text = "关闭串口";
                }
                catch
                {
                    MessageBox.Show("串口打开失败，请检查串口！", "提示");
                    return;
                }
            }
            else
            {
                serialPort1.Close();
                ovalShape1.FillColor = Color.Gray;
                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
                comboBox3.Enabled = true;
                comboBox4.Enabled = true;
                comboBox5.Enabled = true;
                open_serialport_button.Text = "打开串口";
            }
        }//end of function open_serialport_button_Click();


        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {       
            string str_receicve = "";
            byte[] buf_data = new byte[32];
            int length;
            int i = 0;
            byte ctrlword = 0;
            int Index = 0, subindex = 0 ;
            uint Error_flag = 0;

            System.Threading.Thread.Sleep( 300 );       /*延时*/
            length = serialPort1.BytesToRead;           /*获取缓冲区字节数*/
            if (!radioButton3.Checked)                  /*如果不是数值模式，即是字符模式*/
            {
                str_receicve = serialPort1.ReadExisting();      /*直接以字符串方式读取*/
                received_data.AppendText(str_receicve + " ");   /*在接收区尾部添加内容  相当于received_data.Text += str;*/
            }
            else            
            {
                try
                {                                      
                    serialPort1.Read( buf_data, 0, length);     /*读数据到缓冲数组*/
                    for (i = 0; i < length; i++)
                    {
						//if (i == 9)
						//{
						//	throw new Exception("出现错误");   /*人为添加异常*/
						//}

                        str_receicve = Convert.ToString( buf_data[i], 16).ToUpper();  /*转换为大写*/
                        received_data.AppendText("0x" + ( str_receicve.Length == 1 ? "0" + str_receicve : str_receicve ) + "" + " ");  //0xA----->0x0A,每个字节之间空格隔开
                    }
                }
                catch(Exception exp)
                {
                    received_data.AppendText(exp.ToString( ));/*显示异常*/
                }
             }

            ctrlword = buf_data[0];         /*辨识控制器返回的控制字*/
            switch(ctrlword)
            {
                case ReadCmd:
                    Index = (buf_data[2] << 8) | buf_data[1];
                    switch (Index)
                    {
                        case Index_Rspeed:      
                            subindex = buf_data[3];
                            if(subindex == SubIndex_Rspeed)
                               textBox2.Text = ((buf_data[5] << 8) | buf_data[4]).ToString() + "rpm";
                            break;
                        case Index_Rvol:
                            subindex = buf_data[3];
                            if(subindex == SubIndex_Rvol)
                                textBox1.Text = ( ( ( buf_data[5] << 8 ) | buf_data[4] ) / 10 ).ToString( ) + "." + ( ( ( buf_data[5] << 8 ) | buf_data[4] ) % 10 ).ToString( ) + "V";
                            break;
                        case Index_Rstatus:
                            subindex = buf_data[3];
                            if(subindex == SubIndex_Rstatus)                              
                            switch (buf_data[4])
                            {
                                case 0:
                                    textBox3.Text = "IDLE";break;
                                case 1:
                                    textBox3.Text = "STARTUP";break;
                                case 2:
                                    textBox3.Text = "PARASET";break;
                                case 3:
                                    textBox3.Text = "RUN";break;
                                case 4:
                                    textBox3.Text = "STOP";break;
                                case 5:
                                    textBox3.Text = "WAIT";break;
                                case 6:
                                    textBox3.Text = "FAULT";break;
                            }
                            break;
                        case Index_Rerror:
                            subindex = buf_data[3];
                            if (subindex == SubIndex_Rerror)
                                Error_flag = (uint)(( buf_data[5] << 8 ) | buf_data[4]);
                                if (( Error_flag & 0x0002 ) == 0x0002)
                                {
                                    textBox4.Text = "霍尔故障";
                                }
                                else if (( Error_flag & 0x0004 ) == 0x0004)
                                    textBox4.Text = "过压";
                                else if (( Error_flag & 0x0008 ) == 0x0008)
                                    textBox4.Text = "欠压";
                                else if (( Error_flag & 0x0010 ) == 0x0010)
                                    textBox4.Text = "控制器过热";
                                else if (( Error_flag & 0x0020 ) == 0x0020)
                                    textBox4.Text = "过流";
                                else if (( Error_flag & 0x0040 ) == 0x0040)
                                    textBox4.Text = "过载";
                                else if (( Error_flag & 0x0080 ) == 0x0080)
                                    textBox4.Text = "堵转";
                                else if (( Error_flag & 0x1000 ) == 0x1000)
                                    textBox4.Text = "电机过热";
                                else
                                    textBox4.Text = "无故障";
                            break;
                        case Index_Rmostemp:
                            subindex = buf_data[3];
                            if(subindex == SubIndex_Rmostemp)
                                textBox5.Text = ( ( buf_data[5] << 8 ) | buf_data[4] ).ToString( ) + "℃";
                            else if (subindex == SubIndex_Rmottemp)
                                textBox6.Text = ( ( buf_data[5] << 8 ) | buf_data[4] ).ToString( ) + "℃";
                            break;
                    }
                break;

            }
        }

        private void clear_button_Click(object sender, EventArgs e)
        {
            received_data.Clear();    /*清空接收框内容*/
        }


        
        //自动扫描串口函数
        private void AutoSearch_SerialPort_Number(SerialPort usefulport,ComboBox usefulBox)
        {
            string[] mystring = new string[COM_MAX];      /*存放所有可用的端口*/
            string buffer;
            usefulBox.Items.Clear();                      /*清除可用的端口号*/

            for (int i = 1; i < (COM_MAX + 1); i++)
            {
                try
                {
                    buffer = "COM" + i.ToString();		  /*串口号放入缓存*/
                    usefulport.PortName = buffer;
                    usefulport.Open();					  /*如果串口可以打开，执行下一步，否则，调到catch*/
                    mystring[i - 1] = buffer;			  /*将可以打开的串口存放在缓存数组中，以防止有多个可用串口*/
                    usefulBox.Items.Add(mystring[i - 1]);
                    usefulport.Close();
                }
                catch
                {
 
                }//catch执行完成后调回for循环
            }
        }

        //搜索串口按钮事件
        private void searchport_button_Click(object sender, EventArgs e)
        {
            AutoSearch_SerialPort_Number(serialPort1, comboBox1);
        }



        //波形显示按键事件
        private void button7_Click(object sender, EventArgs e)
        {
            F4 = new Form4();
            F4.Show();
        }

        private void SendCmd_button_Click(object sender, EventArgs e)
        {
            ButtonBase button_num = (ButtonBase)sender;
            byte button_tag = Convert.ToByte(button_num.Tag);
            if (serialPort1.IsOpen)
            {
                try
                {
                    switch (button_tag)
                    {
                        case 1:
                            WriteCmd_Speeddata(500, speedcmd_data);break;                           
                        case 2:
                            WriteCmd_Speeddata(1000, speedcmd_data);break;                                                   
                        case 3:
                            WriteCmd_Speeddata(1500, speedcmd_data);break;
                        case 4:
                            WriteCmd_Speeddata(2000, speedcmd_data); break;
                        case 5:
                            WriteCmd_Speeddata(2500, speedcmd_data); break;
                        case 6:
                            WriteCmd_Speeddata(3000, speedcmd_data); break;
                        case 7:
                            WriteCmd_Speeddata(3500, speedcmd_data); break;
                        case 8:
                            WriteCmd_Speeddata(4000, speedcmd_data); break;
                        case 9:
                            WriteCmd_Speeddata(4500, speedcmd_data); break;
                        case 10:
                            WriteCmd_Speeddata(5000, speedcmd_data); break;
                        case 11:
                            WriteCmd_Speeddata(5500, speedcmd_data); break;
                        case 12:
                            WriteCmd_Speeddata(6000, speedcmd_data); break;  
                        case 13:
                            WriteCmd_Speeddata(Convert.ToInt32(numericUpDown1.Value), speedcmd_data);break;
                        case 14:
                            WriteCmd_Accdata(Convert.ToInt16(numericUpDown2.Value), acccmd_data);break;
                        case 15:
                            WriteCmd_Decdata(Convert.ToInt16(numericUpDown3.Value), decccmd_data);break;
                        case 16:
                            serialPort1.Write(power_OFF, 0, 9);break;
                        case 17:
                            serialPort1.Write(power_ON, 0, 9);break;
                        case 18:
                            serialPort1.Write(read_speed_cmd, 0, 9);break;
                        case 19:
                            serialPort1.Write(read_vol_cmd, 0, 9);break;
                        case 20:
                            serialPort1.Write(read_mostemp_cmd, 0, 9);break;
                        case 21:
                            serialPort1.Write(read_mottemp_cmd, 0, 9);break;
                        case 22:
                            serialPort1.Write(read_status_cmd, 0, 9);break;
                        case 23:
                            serialPort1.Write(read_error_cmd, 0, 9);break;
                        case 0:
                            WriteCmd_Speeddata(0, speedcmd_data);break;
                          

                    }
                }
                catch
                {
                    System.Media.SystemSounds.Beep.Play();
                    MessageBox.Show("数据写入错误，请重试！", "提示");
                    serialPort1.Close();
                }
            }
            else
            {
                System.Media.SystemSounds.Beep.Play();
                MessageBox.Show("串口未打开", "提示");
            }
        }

        private void WriteCmd_Speeddata(int speedcmd, params byte[] array)
        {
            byte sum = 0;
            int i = 0;
            array[0] = WriteCmd;
            array[1] = Index_Low_speed;
            array[2] = Index_High_speed;
            array[3] = SubIndex_speed;
            array[5] = (byte)((speedcmd & 0xff00) >> 8);
            array[4] = (byte)(speedcmd & 0x00ff);
            array[6] = 0x00;
            array[7] = 0x00;
            for (i = 0; i < array.Length - 1; i++)
            {
                sum += array[i];
            }
            array[8] = sum;
            serialPort1.Write(array, 0, 9);           
        }

        private void WriteCmd_Accdata(int acccmd, params byte[] array)
        {
            byte sum = 0;
            int i = 0;
            array[0] = WriteCmd;
            array[1] = Index_Low_acc;
            array[2] = Index_High_acc;
            array[3] = SubIndex_acc;
            array[4] = (byte)(acccmd & 0xff);
            array[5] = (byte)((acccmd & 0xff00) >> 8);
            array[6] = 0x00;
            array[7] = 0x00;
            for (i = 0; i < array.Length - 1; i++)
            {
                sum += array[i];
            }
            array[8] = sum;
            serialPort1.Write(array, 0, 9);
        }

        private void WriteCmd_Decdata(int deccmd, params byte[] array)
        {
            byte sum = 0;
            int i = 0;
            array[0] = WriteCmd;
            array[1] = Index_Low_dec;
            array[2] = Index_High_dec;
            array[3] = SubIndex_dec;
            array[4] = (byte)(deccmd & 0xff);
            array[5] = (byte)((deccmd & 0xff00) >> 8);
            array[6] = 0x00;
            array[7] = 0x00;
            for (i = 0; i < array.Length - 1; i++)
            {
                sum += array[i];
            }
            array[8] = sum;
            serialPort1.Write(array, 0, 9);
        }


        //显示参数初始化
        private void ShowPara_Init( )
        {
            textBox1.Text = 0 + "V";
            textBox2.Text = 0 + "rpm";
            textBox3.Text = "NULL";
            textBox4.Text = "NULL";
            textBox5.Text = 0 + "℃";
            textBox6.Text = 0 + "℃";

        }

    }


}

