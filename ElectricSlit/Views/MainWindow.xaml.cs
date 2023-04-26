using ElectricSlit.ViewModels;
using MotorAPIPlus;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ElectricSlit.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string portName = "";

        private SerialPortHelper _serialPort_Motor = null;
        private MotorEntity _motorEntity = null;
        private MotorFunc _motorFunc = null;

        private MainWindowViewModel mainWindowviewModel = null;
        private int tableCount = 0;
        public List<double> list_Light = new List<double>();

        public ObservableCollection<string> PortList { get; set; } = new ObservableCollection<string>();//当前串口列表
        public double CurrentPosition;

        string exePath;
        string debugFolderPath;
        string projectFolderPath;


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

        private async Task Init()
        {
            GetPortList();

            SetUI();

            await GetCurrentPosition();
        }

        private void SetUI()
        {
            CurrentPosition = 130;//mm
            double sliderWidth = 200;
            Slider_Position.Width = CurrentPosition / 130 * sliderWidth;
            //Slider_Light.Width = CurrentPosition / 130 * sliderWidth;
            ProgressBar_Light.Width = CurrentPosition / 130 * sliderWidth;

        }


        //获取实时实际位置
        private async Task GetCurrentPosition()
        {
            if(_motorEntity != null)
            {
                CurrentPosition = _motorFunc.GetCurrentPosition();
                await Task.Delay(200);
            }
        }


        //获取串口列表
        public void GetPortList()
        {

            PortList?.Clear();
            SerialPort.GetPortNames().ToList().ForEach(p => PortList.Add(p));
            cbxSerialPortList.DataContext = PortList;
        }

        //连接串口
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            portName = cbxSerialPortList.Text.ToString();

            if (portName != "")
            {
                _serialPort_Motor = new SerialPortHelper(portName);
                if(_serialPort_Motor.Connect())
                {
                    _motorEntity = new MotorEntity(_serialPort_Motor);
                    _motorFunc = new MotorFunc(_motorEntity);
                    if(_motorFunc.CheckAvailable())
                    {
                        MotorConfig();

                        //TextBox_Current.Text = _motorEntity.GetCurrent().ToString();
                    }
                }
                else
                {
                    MessageBox.Show("连接失败");
                }
            }

            if (_motorEntity != null && _motorFunc.CheckAvailable())
            {
                Btn_Connect.Content = "已连接";
                Btn_Connect.IsEnabled = false;
            }
        }

        //电机初始化配置
        private async void MotorConfig()
        {
            _motorEntity.SetPS();
            //_motorFunc.MoveToZero();//初始化置于零位

            await GetCurrentPosition();
            TextBox_Position.Text = CurrentPosition.ToString();
        }

        //狭缝调小
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            double singleStep = 0;

            if (TextBox_step != null)
            {
                singleStep = Convert.ToDouble(TextBox_step.Text.ToString());
            }

            if(_motorEntity != null)
            {
                _motorFunc.MoveLeft(singleStep);

                await GetCurrentPosition();
                TextBox_Position.Text = CurrentPosition.ToString();
            }
        }

        //狭缝调大
        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            double singleStep = 0;

            if (TextBox_step != null)
            {
                singleStep = Convert.ToDouble(TextBox_step.Text.ToString());
            }

            if (_motorEntity != null)
            {
                _motorFunc.MoveRight(singleStep);

                await GetCurrentPosition();
                TextBox_Position.Text = CurrentPosition.ToString();
            }
        }

        //移动到指定亮度位置
        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            double targetPosition = 0;
            if (TextBox_targetPosition != null)
            {
                targetPosition = Convert.ToDouble(TextBox_targetPosition.Text.ToString());
                //targetPosition = f(Convert.ToDouble(TextBox_targetPosition.Text.ToString()));//亮度关于位置长度非线性映射
            }

            if(_motorEntity != null)
            {
                _motorFunc.MoveToPosition(targetPosition, true);//仅设置可运动路径位原点右侧部分，位置坐标符号位正
                
                await GetCurrentPosition();
                TextBox_Position.Text = CurrentPosition.ToString();
            }
        }

        //移动至狭缝全开
        private async void Button_Click_4(object sender, RoutedEventArgs e)
        {
            if(_motorEntity != null)
            {
                _motorFunc.MoveToLowerLimmit();

                await GetCurrentPosition();
                TextBox_Position.Text = CurrentPosition.ToString();
            }
        }

        //移动值狭缝全闭
        private async void Button_Click_5(object sender, RoutedEventArgs e)
        {
            if(_motorEntity != null)
            {
                _motorFunc.MoveToUpperLimmit();

                await GetCurrentPosition();
                TextBox_Position.Text = CurrentPosition.ToString();
            }
        }

        //紧急停止
        private async void Button_Click_6(object sender, RoutedEventArgs e)
        {
            if(_motorEntity != null)
            {
                _motorFunc.DisEnable();

                await GetCurrentPosition();
                TextBox_Position.Text = CurrentPosition.ToString();
            }
        }

        //添加光强百分比设定值
        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            LightSetModel lightSetModel = new LightSetModel();

            tableCount++;
            lightSetModel.Index = tableCount;

            double lightset = Convert.ToDouble(TextBox_LightSet.Text.ToString());
            lightSetModel.Light = lightset;

            ListView_Set.Items.Add(lightSetModel);

            list_Light.Add(lightset);
        }

        //应用一个设定值
        private async void Button_Click_8(object sender, RoutedEventArgs e)
        {
            int selectedIndex = Convert.ToInt32(ListView_Set.SelectedIndex.ToString());//0,1,2,...

            //double targetPosition = g(list_Light[selectedIndex]);//光强百分比对应的实际位置

            if (_motorEntity != null)
            {
                //_motorFunc.MoveToPosition(targetPosition, true);
                Thread.Sleep(200);
                TextBox_Light.Text = list_Light[selectedIndex].ToString();

                await GetCurrentPosition();
                TextBox_Position.Text = CurrentPosition.ToString();
            }



        }
    }
}
