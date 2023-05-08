using ElectricSlit.ViewModels;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using ImTools;
using MotorAPIPlus;
using MotorTestDemo.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using MessageBox = System.Windows.MessageBox;
using Task = System.Threading.Tasks.Task;

namespace ElectricSlit.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private const int SC_CLOSE = 0xF060;
        private const int WM_SYSCOMMAND = 0x0112;

        public string portName = "";

        public SerialPortHelper _serialPort_Motor = null;
        public MotorEntity _motorEntity = null;
        public MotorFunc _motorFunc = null;

        public PortSetWindow portSetWindow = null;
        public ToolWindow toolWindow = null;
        public AboutWindow aboutWindow = null;
        private MainWindowViewModel mainWindowviewModel = null;
        private int tableCount = 0;
        public List<double> list_Light = new List<double>();

        public double a, b, c = 0;//映射 二次多项式系数
        public double maxLight = 10000;

        public ObservableCollection<string> PortList { get; set; } = new ObservableCollection<string>();//当前串口列表
        public double CurrentPosition;
        private Thread thread_getPosition = null;


        private static string exePath;
        private static string debugFolderPath;
        private static string projectFolderPath;


        public MainWindow()
        {
            InitializeComponent();

            mainWindowviewModel = new MainWindowViewModel();
            this.Loaded += MainWindow_Loaded;
        }

        #region 初始化配置等
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Init();

            //开启线程
            thread_getPosition = new Thread(new ThreadStart(RefreshPosition));
            thread_getPosition.Priority = ThreadPriority.Lowest;
            thread_getPosition.Start();

            exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            debugFolderPath = System.IO.Path.GetDirectoryName(exePath);
            projectFolderPath = System.IO.Directory.GetParent(debugFolderPath).Parent.FullName;
        }

        #region 窗体事件关闭进程
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
                Process.GetCurrentProcess().Kill();
                //handled = true;
            }
            return IntPtr.Zero;
        }
        #endregion

        //初始化
        private void Init()
        {
            GroupBox_ControlPanel.IsEnabled = false;

            //GetPortList();
            SetUI();

            toolWindow = new ToolWindow(this);
            //toolWindow.Hide();
            aboutWindow = new AboutWindow(this);

            //初始化时打开串口连接窗口
            //portName = cbxSerialPortList.Text.ToString();
            portSetWindow = new PortSetWindow(this);
            portSetWindow.ReadCom();
            portSetWindow.CommonConnect();
            //portSetWindow.Show();

        }

        //界面进度条
        private void SetUI()
        {
            CurrentPosition = 50;//mm
            double sliderWidth = 200;
            //Slider_Position.Width = CurrentPosition / 50 * sliderWidth;
            ProgressBar_Light.Width = CurrentPosition / 50 * sliderWidth;
        }

        //获取实时实际位置
/*        private async Task GetCurrentPositionAsync()
        {
            if (_motorEntity != null)
            {
                GetCurrentPosition();
                await Task.Delay(100);
            }
        }*/

        //间隔 1s 刷新显示位置
        public void RefreshPosition()
        {
            while(true)
            {
                GetCurrentPosition();
                Thread.Sleep(1000);
            }
        }

        public void GetCurrentPosition()
        {
            if(_motorEntity != null)//
            {
                Thread.Sleep(100);
                CurrentPosition = _motorFunc.GetCurrentPosition();
                //CurrentPosition++;

                //TextBox_Position.Text = CurrentPosition.ToString();
                this.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    TextBlock_CurrentWidth.Text = CurrentPosition.ToString();
                });

            }
        }

        //获取串口列表
        public void GetPortList()
        {
            PortList?.Clear();
            SerialPort.GetPortNames().ToList().ForEach(p => PortList.Add(p));
            //cbxSerialPortList.DataContext = PortList;
        }

        //连接串口(未使用)
        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        //电机初始化配置
        public void MotorConfig()
        {
            _motorEntity.SetPS();//设置为上下限位模式
            //_motorFunc.MoveToZero();//初始化置于零位

            //GetCurrentPosition();
            //TextBox_Position.Text = CurrentPosition.ToString();
        }

        #endregion

        #region 按钮方法
        //狭缝调小
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            double singleStep = 0;

            if (TextBox_step != null)
            {
                singleStep = Convert.ToDouble(TextBox_step.Text.ToString());
            }

            if(singleStep >= 0 && singleStep <= 50)
            {
                if(_motorEntity != null)
                {
                    _motorFunc.MoveRight(singleStep);

                    //GetCurrentPosition();
                }
            }
            else
            {
                MessageBox.Show("步距过大！", "错误");
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

            if(singleStep >= 0 && singleStep <= 50)
            { 
                if (_motorEntity != null)
                {
                    _motorFunc.MoveLeft(singleStep);

                    //GetCurrentPosition();
                }
            }
            else
            {
                MessageBox.Show("步距过大！", "错误");
            }
        }

        //移动到指定亮度位置
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            double targetPosition = 0;
/*            if (TextBox_targetPosition != null)
            {
                targetPosition = Convert.ToDouble(TextBox_targetPosition.Text.ToString());
            }*/

            if(_motorEntity != null)
            {
                _motorFunc.MoveToPosition(targetPosition, true);//仅设置可运动路径位原点右侧部分，位置坐标符号位正

                //GetCurrentPosition();
            }
        }

        //狭缝全开
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            if(_motorEntity != null)
            {
                _motorFunc.MoveToLowerLimmit();

                //GetCurrentPosition();
            }
        }

        //狭缝全闭
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            if(_motorEntity != null)
            {
                _motorFunc.MoveToUpperLimmit();

                //GetCurrentPosition();
            }
        }

        //紧急停止
        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            if(_motorEntity != null)
            {
                _motorFunc.DisEnable();

                //GetCurrentPosition();
            }
        }

        //添加光强百分比设定值
        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            LightSetModel lightSetModel = new LightSetModel();

            if(TextBox_LightSet.Text != "")
            {
                tableCount++;
                lightSetModel.Index = tableCount;

                double lightset = Convert.ToDouble(TextBox_LightSet.Text.ToString());
                lightSetModel.Light = lightset;

                ListView_Set.Items.Add(lightSetModel);

                list_Light.Add(lightset);
            }
        }

        //删除选中项
        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            if (ListView_Set.SelectedIndex >= 0)
            {
                int selectedIndex = Convert.ToInt32(ListView_Set.SelectedIndex.ToString());//0,1,2,...
                list_Light.RemoveAt(selectedIndex);
                ListView_Set.Items.RemoveAt(selectedIndex);
                tableCount--;
            }
        }

        //应用一个设定值
        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            int selectedIndex = Convert.ToInt32(ListView_Set.SelectedIndex.ToString());//0,1,2,...

            if(TextBox_LightSet.Text != "" && selectedIndex >= 0)
            {
                if(list_Light[selectedIndex] > maxLight)
                {
                    MessageBox.Show("设定值超出最大值！", "错误");
                }

                else
                {
                    double targetPosition = toolWindow.Gx(list_Light[selectedIndex]);//光强对应的实际位置

                    if (_motorEntity != null)
                    {
                        _motorFunc.MoveToPosition(targetPosition, true);
                        Thread.Sleep(200);

                        //GetCurrentPosition();
                    }
            
                    TextBox_Light.Text = list_Light[selectedIndex].ToString();
                    ProgressBar_Light.Value = (list_Light[selectedIndex] / maxLight) * 100;
                }
            }
        }

        //打开串口连接界面
        private void MenuSerialPort_Click(object sender, RoutedEventArgs e)
        {
            portSetWindow.Show();
        }

        private void ListView_Set_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point position = e.GetPosition(this);
            double x = position.X;
            double y = position.Y;

/*            ContextMenu_listview.PlacementTarget = sender as UIElement;
            ContextMenu_listview.Placement = PlacementMode.MousePoint;
            ContextMenu_listview.IsOpen = true;*/      
        }

        //delete键删除
        private void ListView_Set_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Delete)
            {
                if (ListView_Set.SelectedIndex >= 0)
                {
                    int selectedIndex = Convert.ToInt32(ListView_Set.SelectedIndex.ToString());//0,1,2,...
                    list_Light.RemoveAt(selectedIndex);
                    ListView_Set.Items.RemoveAt(selectedIndex);
                    tableCount--;
                }
            }
        }


        //打开工具界面(狭缝宽度调节)
        private void MenuTool_Click(object sender, RoutedEventArgs e)
        {
            toolWindow.Show();
        }

        //打开关于界面
        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            aboutWindow.Show();
        }


        #endregion

    }
}
