using ElectricSlit.ViewModels;
using MotorAPIPlus;
using MotorTestDemo.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace ElectricSlit.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int SC_CLOSE = 0xF060;
        private const int WM_SYSCOMMAND = 0x0112;

        public string portName = "";

        public SerialPortHelper _serialPort_Motor = null;
        public MotorEntity _motorEntity = null;
        public MotorFunc _motorFunc = null;

        public PortSetWindow portSetWindow = null;
        public ToolWindow toolWindow = null;
        private MainWindowViewModel mainWindowviewModel = null;
        private int tableCount = 0;
        public List<double> list_Light = new List<double>();

        public ObservableCollection<string> PortList { get; set; } = new ObservableCollection<string>();//当前串口列表
        public double CurrentPosition;

        private static string exePath;
        private static string debugFolderPath;
        private static string projectFolderPath;


        public MainWindow()
        {
            InitializeComponent();

            mainWindowviewModel = new MainWindowViewModel();
            this.Loaded += MainWindow_Loaded;
        }


        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Init();

            exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            debugFolderPath = System.IO.Path.GetDirectoryName(exePath);
            projectFolderPath = System.IO.Directory.GetParent(debugFolderPath).Parent.FullName;
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
                this.Close();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private async Task Init()
        {
            //GroupBox_ControlPanel.IsEnabled = false;

            //GetPortList();
            SetUI();

            toolWindow = new ToolWindow(this);
            //toolWindow.Hide();

            //初始化时打开串口连接窗口
            //portName = cbxSerialPortList.Text.ToString();
            portSetWindow = new PortSetWindow(this);
            portSetWindow.Show();
/*            if (portSetWindow != null)
            {
                portName = portSetWindow.PortName_Motor;
            }

            if (portName != null)
            {
                _serialPort_Motor = new SerialPortHelper(portName);
                if (_serialPort_Motor.Connect())
                {
                    _motorEntity = new MotorEntity(_serialPort_Motor);
                    _motorFunc = new MotorFunc(_motorEntity);
                    if (_motorFunc.CheckAvailable())
                    {
                        MotorConfig();

                        //TextBox_Current.Text = _motorEntity.GetCurrent().ToString();
                        GetCurrentPosition();

                        //GroupBox_ControlPanel.IsEnabled = true;//仅当电机通信正常控制面板可用
                    }
                    else
                    {
                        MessageBox.Show("连接失败!请检查串口和电路");
                    }
                }
            }*/
        }

        private void SetUI()
        {
            CurrentPosition = 50;//mm
            double sliderWidth = 200;
            Slider_Position.Width = CurrentPosition / 50 * sliderWidth;
            //Slider_Light.Width = CurrentPosition / 130 * sliderWidth;
            ProgressBar_Light.Width = CurrentPosition / 50 * sliderWidth;

        }


        //获取实时实际位置
/*        private async Task GetCurrentPosition()
        {
            if (_motorEntity != null)
            {
                CurrentPosition = _motorFunc.GetCurrentPosition();
                await Task.Delay(100);
            }
        }*/

        public void GetCurrentPosition()
        {
            if (_motorEntity != null) 
            {
                Thread.Sleep(100);
                CurrentPosition = _motorFunc.GetCurrentPosition();
                Thread.Sleep(100);
                TextBox_Position.Text = CurrentPosition.ToString();
            }
        }


        //获取串口列表
        public void GetPortList()
        {
            PortList?.Clear();
            SerialPort.GetPortNames().ToList().ForEach(p => PortList.Add(p));
            //cbxSerialPortList.DataContext = PortList;
        }

        //连接串口
        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        //电机初始化配置
        public void MotorConfig()
        {
            _motorEntity.SetPS();
            //_motorFunc.MoveToZero();//初始化置于零位

            GetCurrentPosition();
            //TextBox_Position.Text = CurrentPosition.ToString();
        }

        //狭缝调小
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            double singleStep = 0;

            if (TextBox_step != null)
            {
                singleStep = Convert.ToDouble(TextBox_step.Text.ToString());
            }

            if(_motorEntity != null)
            {
                _motorFunc.MoveRight(singleStep);

                GetCurrentPosition();
            }
        }

        //狭缝调大
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            double singleStep = 0;

            if (TextBox_step != null)
            {
                singleStep = Convert.ToDouble(TextBox_step.Text.ToString());
            }

            if (_motorEntity != null)
            {
                _motorFunc.MoveLeft(singleStep);

                GetCurrentPosition();
            }
        }

        //移动到指定亮度位置
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            double targetPosition = 0;
            if (TextBox_targetPosition != null)
            {
                targetPosition = Convert.ToDouble(TextBox_targetPosition.Text.ToString());
                //targetPosition = f(Convert.ToDouble(TextBox_targetPosition.Text.ToString()));//亮度关于位置长度非线性映射f(x)
            }

            if(_motorEntity != null)
            {
                _motorFunc.MoveToPosition(targetPosition, true);//仅设置可运动路径位原点右侧部分，位置坐标符号位正

                GetCurrentPosition();
            }
        }

        //移动至狭缝全开
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            if(_motorEntity != null)
            {
                _motorFunc.MoveToLowerLimmit();

                GetCurrentPosition();
            }
        }

        //移动值狭缝全闭
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            if(_motorEntity != null)
            {
                _motorFunc.MoveToUpperLimmit();

                GetCurrentPosition();
            }
        }

        //紧急停止
        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            if(_motorEntity != null)
            {
                _motorFunc.DisEnable();

                GetCurrentPosition();
            }
        }

        //添加光强百分比设定值
        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            LightSetModel lightSetModel = new LightSetModel();

            tableCount++;
            lightSetModel.Index = tableCount;

            if(TextBox_LightSet.Text != "")
            {
                double lightset = Convert.ToDouble(TextBox_LightSet.Text.ToString());
                lightSetModel.Light = lightset;

                ListView_Set.Items.Add(lightSetModel);

                list_Light.Add(lightset);
            }
        }

        //应用一个设定值
        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            int selectedIndex = Convert.ToInt32(ListView_Set.SelectedIndex.ToString());//0,1,2,...

            //double targetPosition = g(list_Light[selectedIndex]);//光强对应的实际位置

            if (_motorEntity != null)
            {
                //_motorFunc.MoveToPosition(targetPosition, true);
                Thread.Sleep(200);
                TextBox_Light.Text = list_Light[selectedIndex].ToString();

                GetCurrentPosition();
            }
            

        }

        //打开串口连接界面
        private void MenuSerialPort_Click(object sender, RoutedEventArgs e)
        {
            portSetWindow.Show();
        }

        //打开工具界面(狭缝距离调节)
        private void MenuTool_Click(object sender, RoutedEventArgs e)
        {
            toolWindow.Show();
        }

        //删除选中项
        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            if(ListView_Set.SelectedIndex >= 0)
            {
                int selectedIndex = Convert.ToInt32(ListView_Set.SelectedIndex.ToString());//0,1,2,...
                list_Light.RemoveAt(selectedIndex);
                ListView_Set.Items.RemoveAt(selectedIndex);
                tableCount--;
            }
        }
    }
}
