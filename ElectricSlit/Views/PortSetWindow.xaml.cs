//using MotorAPIPlus;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace MotorTestDemo.Views
{
    /// <summary>
    /// PortSetWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PortSetWindow : Window
    {
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

        public PortSetWindow()
        {
            InitializeComponent();

            DataContext = this;
            this.Loaded += PortSetWindow_Loaded;
        }

        private void CboPortName_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void PortSetWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GetPortList();
            Init();
        }

        private void Init()
        {

        }

        public void GetPortList()
        {
            PortList?.Clear();
            SerialPort.GetPortNames().ToList().ForEach(p => PortList.Add(p));
        }

        private void Btn_Connect_Click(object sender, RoutedEventArgs e)
        {
            portName_Motor = ComboBox_PortName.Text.ToString();
            Btn_Connect.Content = "已连接";
            Btn_Connect.IsEnabled = false;
            //this.Visibility = Visibility.Collapsed;   
        }
    }
}
