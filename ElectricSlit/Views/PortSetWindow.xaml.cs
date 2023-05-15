using MotorAPIPlus;
using ElectricSlit.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Drawing;
using System.Windows.Media;

namespace MotorTestDemo.Views
{
    /// <summary>
    /// PortSetWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PortSetWindow : Window
    {
        public MainWindow part_mainwindow = null;

        private const int SC_CLOSE = 0xF060;
        private const int WM_SYSCOMMAND = 0x0112;

        public ObservableCollection<string> PortList { get; set; } = new ObservableCollection<string>();

        public bool isConnnected_Motor;

        private string portName_Motor;
        /// <summary>
        /// 电机串口名
        /// </summary>
        public string PortName_Motor
        {
            get { return portName_Motor; }
            set { portName_Motor = value; }
        }

        public PortSetWindow(MainWindow mainWindow)
        {
            InitializeComponent();

            this.part_mainwindow = mainWindow;

            DataContext = this;
            this.Loaded += PortSetWindow_Loaded;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SYSCOMMAND && wParam.ToInt32() == SC_CLOSE)
            {
                // 在此处实现将窗口隐藏而不是关闭的代码
                this.Hide();
                handled = true;
            }
            return IntPtr.Zero;
        }


        private void CboPortName_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void PortSetWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GetPortList();
        }

        public void GetPortList()
        {
            PortList?.Clear();
            SerialPort.GetPortNames().ToList().ForEach(p => PortList.Add(p));
        }

        //连接串口
        private void Btn_Connect_Click(object sender, RoutedEventArgs e)
        {
            portName_Motor = ComboBox_PortName.Text.ToString();//获取电机串口名
            part_mainwindow.portName = portName_Motor;

            CommonConnect();
        }

        //连接操作
        public void CommonConnect()
        {
            part_mainwindow._serialPort_Motor = new SerialPortHelper(part_mainwindow.portName);

            if (part_mainwindow._serialPort_Motor.Connect())
            {
                //串口连接成功 创建实例
                part_mainwindow._motorEntity = new MotorEntity(part_mainwindow._serialPort_Motor);
                part_mainwindow._motorFunc = new MotorFunc(part_mainwindow._motorEntity);

                if (part_mainwindow._motorFunc.CheckAvailable())
                {
                    part_mainwindow.MotorConfig();

                    Btn_Connect.Content = "已连接";
                    Btn_Connect.IsEnabled = false;

                    isConnnected_Motor = true;
                    part_mainwindow.TextBlock_isConnected.Text = "已连接";
                    System.Windows.Media.Brush brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 0));
                    part_mainwindow.TextBlock_isConnected.Foreground = brush;

                    part_mainwindow.TextBlock_CurrentWidth.Text = part_mainwindow._motorFunc.GetCurrentPosition().ToString();

                    part_mainwindow.GroupBox_ControlPanel.IsEnabled = true;//将主窗口控制面板设为可用

                    //成功连接后记录下串口号，之后打开软件尝试自动连接
                    SaveCom();

                    MessageBox.Show("连接成功!");
                    this.Hide();
                }
                else
                {
                    part_mainwindow._serialPort_Motor.Close();//断开串口
                    part_mainwindow._serialPort_Motor = null;
                    part_mainwindow._motorEntity = null;
                    part_mainwindow._motorFunc = null;
                    MessageBox.Show("连接失败!  请检查硬件连接。", "错误");
                }
            }
            else
            {
                MessageBox.Show("无法打开串口, 请检查串口是否被占用。", "错误");
            }
        }

        //保存串口配置
        private void SaveCom()
        {
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"config\com.txt");

            if (filePath != null)
            {
                StreamWriter writer = new StreamWriter(filePath);
                writer.WriteLine(part_mainwindow.portName.ToString());
                writer.Close();

                //MessageBox.Show("保存完成!");
            }
        }

        //读取串口配置
        public void ReadCom()
        {
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"config\com.txt");

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    StreamReader reader = new StreamReader(fs);
                    string line1 = reader.ReadLine();

                    part_mainwindow.portName = line1;
                }
                catch
                {
                    
                }
            }
        }

    }
}
