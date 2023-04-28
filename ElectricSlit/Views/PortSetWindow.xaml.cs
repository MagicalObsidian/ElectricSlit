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

        public event Func<bool, bool> Refresh;

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

        private void Btn_Connect_Click(object sender, RoutedEventArgs e)
        {
            portName_Motor = ComboBox_PortName.Text.ToString();//获取电机串口名
            part_mainwindow.portName = portName_Motor;
                        
            part_mainwindow._serialPort_Motor = new SerialPortHelper(part_mainwindow.portName);

            if(part_mainwindow._serialPort_Motor.Connect())//
            {
                //串口连接成功 创建实例
                part_mainwindow._motorEntity = new MotorEntity(part_mainwindow._serialPort_Motor);
                part_mainwindow._motorFunc = new MotorFunc(part_mainwindow._motorEntity);

                if(part_mainwindow._motorFunc.CheckAvailable())
                {
                    part_mainwindow.MotorConfig();
                    part_mainwindow.GetCurrentPosition();

                    Btn_Connect.Content = "已连接";
                    Btn_Connect.IsEnabled = false;

                    part_mainwindow.GroupBox_ControlPanel.IsEnabled = true;//将主窗口控制面板设为可用

                    MessageBox.Show("连接成功!");
                    this.Hide();
                }
                else
                {
                    part_mainwindow._serialPort_Motor.Close();
                    part_mainwindow._serialPort_Motor = null;
                    MessageBox.Show("连接失败!  请检查串口和电路", "错误");
                }
            }
            else
            {
                MessageBox.Show("无法打开串口", "错误");
            }
        }
    }
}
